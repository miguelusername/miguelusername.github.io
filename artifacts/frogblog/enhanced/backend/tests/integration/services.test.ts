import { after, before, beforeEach, describe, test } from "node:test";
import assert from "node:assert/strict";
import { ObjectId } from "mongodb";
import {
    collections,
    clearData,
    makePost,
    makeUser,
    setupDatabase,
    teardownDatabase,
} from "./helpers.ts";
import * as posts from "../../src/services/post.service.ts";
import * as comments from "../../src/services/comment.service.ts";
import * as reactions from "../../src/services/reaction.service.ts";
import { HttpError } from "../../src/errors.ts";

/**
 * The data-access layer exercised against a real MongoDB.
 *
 * These cover the behaviour that only shows up against a live database:
 * aggregation results, upsert semantics, cascading deletes inside a
 * transaction, and the ordering guarantees pagination depends on.
 */
describe("database services", () => {
    before(setupDatabase);
    beforeEach(clearData);
    after(teardownDatabase);

    describe("reactions", () => {
        test("setting the same reaction twice counts once", async () => {
            const user = await makeUser("Alice");
            const post = await makePost(user);

            await reactions.setReaction(user, post, "like");
            await reactions.setReaction(user, post, "like");

            const summary = (await reactions.summarizeReactions([post], user)).get(post.toHexString());
            assert.equal(summary?.likes, 1);
            assert.equal(await collections.reactions().countDocuments({}), 1);
        });

        test("switching a reaction moves the count rather than adding one", async () => {
            const user = await makeUser("Alice");
            const post = await makePost(user);

            await reactions.setReaction(user, post, "like");
            await reactions.setReaction(user, post, "dislike");

            const summary = (await reactions.summarizeReactions([post], user)).get(post.toHexString());
            assert.equal(summary?.likes, 0);
            assert.equal(summary?.dislikes, 1);
            assert.equal(summary?.myReaction, "dislike");
            assert.equal(await collections.reactions().countDocuments({}), 1);
        });

        test("counts aggregate across users and myReaction stays per-viewer", async () => {
            const [alice, bob, carol] = [await makeUser("Alice"), await makeUser("Bob"), await makeUser("Carol")];
            const post = await makePost(alice);

            await reactions.setReaction(alice, post, "like");
            await reactions.setReaction(bob, post, "like");
            await reactions.setReaction(carol, post, "dislike");

            const forBob = (await reactions.summarizeReactions([post], bob)).get(post.toHexString());
            assert.equal(forBob?.likes, 2);
            assert.equal(forBob?.dislikes, 1);
            assert.equal(forBob?.myReaction, "like");

            const anonymous = (await reactions.summarizeReactions([post], null)).get(post.toHexString());
            assert.equal(anonymous?.likes, 2);
            assert.equal(anonymous?.myReaction, null, "A logged-out viewer has no reaction of their own.");
        });

        test("clearing a reaction removes it from the counts", async () => {
            const user = await makeUser("Alice");
            const post = await makePost(user);

            await reactions.setReaction(user, post, "like");
            await reactions.clearReaction(user, post);

            const summary = (await reactions.summarizeReactions([post], user)).get(post.toHexString());
            assert.equal(summary?.likes, 0);
            assert.equal(summary?.myReaction, null);
        });

        test("a post nobody reacted to summarizes as zero rather than missing", async () => {
            const user = await makeUser("Alice");
            const post = await makePost(user);

            const summary = (await reactions.summarizeReactions([post], user)).get(post.toHexString());
            assert.deepEqual(summary, { likes: 0, dislikes: 0, myReaction: null });
        });
    });

    describe("cascading delete", () => {
        test("deleting a post removes its comments and reactions", async () => {
            const [alice, bob] = [await makeUser("Alice"), await makeUser("Bob")];
            const post = await posts.createPost(alice, { title: "Doomed", content: "Body" });
            const survivor = await posts.createPost(alice, { title: "Survivor", content: "Body" });

            await comments.createComment(post._id, bob, { content: "A comment" });
            await reactions.setReaction(bob, post._id, "like");
            await comments.createComment(survivor._id, bob, { content: "Unrelated" });
            await reactions.setReaction(bob, survivor._id, "like");

            await posts.deletePost(post._id, alice);

            assert.equal(await collections.posts().countDocuments({ _id: post._id }), 0);
            assert.equal(await collections.comments().countDocuments({ postId: post._id }), 0);
            assert.equal(await collections.reactions().countDocuments({ postId: post._id }), 0);

            // Nothing belonging to another post may be caught in the blast.
            assert.equal(await collections.comments().countDocuments({ postId: survivor._id }), 1);
            assert.equal(await collections.reactions().countDocuments({ postId: survivor._id }), 1);
        });

        test("a failed authorization check deletes nothing at all", async () => {
            const [alice, bob] = [await makeUser("Alice"), await makeUser("Bob")];
            const post = await posts.createPost(alice, { title: "Alice's", content: "Body" });
            await comments.createComment(post._id, bob, { content: "A comment" });
            await reactions.setReaction(bob, post._id, "like");

            await assert.rejects(
                () => posts.deletePost(post._id, bob),
                (error: unknown) => error instanceof HttpError && error.status === 403,
            );

            assert.equal(await collections.posts().countDocuments({ _id: post._id }), 1);
            assert.equal(await collections.comments().countDocuments({ postId: post._id }), 1);
            assert.equal(await collections.reactions().countDocuments({ postId: post._id }), 1);
        });
    });

    describe("comments", () => {
        test("a thread comes back nested by parent", async () => {
            const [alice, bob] = [await makeUser("Alice"), await makeUser("Bob")];
            const post = await makePost(alice);

            const top = await comments.createComment(post, alice, { content: "Top" });
            const reply = await comments.createComment(post, bob, { content: "Reply", parentId: top._id });
            await comments.createComment(post, alice, { content: "Reply to reply", parentId: reply._id });

            const thread = await comments.getCommentThread(post);

            assert.equal(thread.length, 1);
            assert.equal(thread[0]!.content, "Top");
            assert.equal(thread[0]!.replies.length, 1);
            assert.equal(thread[0]!.replies[0]!.replies.length, 1);
            assert.equal(thread[0]!.replies[0]!.authorName, "Bob");
        });

        test("a reply cannot point at a comment on a different post", async () => {
            const alice = await makeUser("Alice");
            const [first, second] = [await makePost(alice), await makePost(alice)];

            const onFirst = await comments.createComment(first, alice, { content: "On first" });

            await assert.rejects(
                () => comments.createComment(second, alice, { content: "Cross-post", parentId: onFirst._id }),
                (error: unknown) => error instanceof HttpError && error.status === 400,
            );
        });

        test("commenting on a post that does not exist is a 404", async () => {
            const alice = await makeUser("Alice");

            await assert.rejects(
                () => comments.createComment(new ObjectId(), alice, { content: "Nowhere" }),
                (error: unknown) => error instanceof HttpError && error.status === 404,
            );
        });
    });

    describe("feed pagination", () => {
        test("pages cover every post exactly once, newest first", async () => {
            const alice = await makeUser("Alice");

            const created: string[] = [];
            for (let i = 0; i < 12; i++) {
                const post = await posts.createPost(alice, { title: `Post ${i}`, content: "Body" });
                created.push(post._id.toHexString());
            }

            const seen: string[] = [];
            let cursor: ObjectId | undefined;

            for (let page = 0; page < 10; page++) {
                const result = await posts.listPosts(
                    { limit: 5, ...(cursor ? { cursor } : {}) },
                    null,
                );
                seen.push(...result.items.map((p) => p._id.toHexString()));

                if (!result.nextCursor) break;
                cursor = new ObjectId(result.nextCursor);
            }

            assert.equal(seen.length, 12, "Every post should appear.");
            assert.equal(new Set(seen).size, 12, "No post should appear on two pages.");
            assert.deepEqual(seen, [...created].reverse(), "Feed order should be newest first.");
        });

        test("filtering by author excludes other people's posts", async () => {
            const [alice, bob] = [await makeUser("Alice"), await makeUser("Bob")];
            await posts.createPost(alice, { title: "A1", content: "Body" });
            await posts.createPost(bob, { title: "B1", content: "Body" });
            await posts.createPost(alice, { title: "A2", content: "Body" });

            const page = await posts.listPosts({ author: alice, limit: 20 }, null);

            assert.equal(page.items.length, 2);
            assert.ok(page.items.every((p) => p.author.equals(alice)));
        });

        test("author names resolve in one batch and survive a deleted user", async () => {
            const alice = await makeUser("Alice");
            await posts.createPost(alice, { title: "A1", content: "Body" });
            await posts.createPost(alice, { title: "A2", content: "Body" });

            await collections.users().deleteOne({ _id: alice });

            const page = await posts.listPosts({ limit: 20 }, null);

            assert.equal(page.items.length, 2);
            assert.ok(page.items.every((p) => p.authorName === "Unknown User"),
                "A missing author should degrade to a placeholder, not break the feed.");
        });
    });

    describe("profiles", () => {
        test("saving a profile twice updates instead of failing", async () => {
            const alice = await makeUser("Alice");
            const birthday = new Date(Date.UTC(2000, 5, 5));

            await collections.profiles().findOneAndUpdate(
                { _id: alice },
                { $set: { bio: "first", birthday, updatedAt: new Date() } },
                { upsert: true, returnDocument: "after" },
            );
            const second = await collections.profiles().findOneAndUpdate(
                { _id: alice },
                { $set: { bio: "second", birthday, updatedAt: new Date() } },
                { upsert: true, returnDocument: "after" },
            );

            assert.equal(second?.bio, "second");
            assert.equal(await collections.profiles().countDocuments({}), 1);
        });
    });
});
