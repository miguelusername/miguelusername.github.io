import { after, before, beforeEach, describe, test } from "node:test";
import assert from "node:assert/strict";
import { ObjectId } from "mongodb";
import {
    collections,
    clearData,
    isDuplicateKeyError,
    isValidationError,
    makePost,
    makeUser,
    setupDatabase,
    teardownDatabase,
} from "./helpers.ts";

/**
 * Guarantees the database enforces on its own, independent of application code.
 *
 * These matter because the API is not the only thing that can write here. A
 * migration, a console session or a future service all bypass the Zod layer,
 * so anything the data model genuinely depends on has to be enforced below it.
 */
describe("database constraints", () => {
    before(setupDatabase);
    beforeEach(clearData);
    after(teardownDatabase);

    test("a user cannot react twice to the same post", async () => {
        const user = await makeUser("Alice");
        const post = await makePost(user);

        await collections.reactions().insertOne({
            _id: new ObjectId(), userId: user, postId: post, type: "like", createdAt: new Date(),
        });

        await assert.rejects(
            () => collections.reactions().insertOne({
                _id: new ObjectId(), userId: user, postId: post, type: "dislike", createdAt: new Date(),
            }),
            isDuplicateKeyError,
            "The unique compound index should make a second reaction impossible.",
        );

        assert.equal(await collections.reactions().countDocuments({}), 1);
    });

    test("different users may each react to the same post", async () => {
        const [alice, bob] = [await makeUser("Alice"), await makeUser("Bob")];
        const post = await makePost(alice);

        await collections.reactions().insertOne({
            _id: new ObjectId(), userId: alice, postId: post, type: "like", createdAt: new Date(),
        });
        await collections.reactions().insertOne({
            _id: new ObjectId(), userId: bob, postId: post, type: "like", createdAt: new Date(),
        });

        assert.equal(await collections.reactions().countDocuments({}), 2);
    });

    test("the same user may react to different posts", async () => {
        const alice = await makeUser("Alice");
        const [first, second] = [await makePost(alice), await makePost(alice)];

        await collections.reactions().insertOne({
            _id: new ObjectId(), userId: alice, postId: first, type: "like", createdAt: new Date(),
        });
        await collections.reactions().insertOne({
            _id: new ObjectId(), userId: alice, postId: second, type: "like", createdAt: new Date(),
        });

        assert.equal(await collections.reactions().countDocuments({}), 2);
    });

    test("a post with no content is rejected by the database", async () => {
        const author = await makeUser("Alice");

        await assert.rejects(
            () => collections.posts().insertOne({
                _id: new ObjectId(), author, title: "No body", createdAt: new Date(), updatedAt: new Date(),
            } as never),
            isValidationError,
        );
    });

    test("a post with a wrongly typed field is rejected", async () => {
        const author = await makeUser("Alice");

        await assert.rejects(
            () => collections.posts().insertOne({
                _id: new ObjectId(),
                author,
                title: "Bad date",
                content: "Body",
                createdAt: "yesterday",
                updatedAt: new Date(),
            } as never),
            isValidationError,
        );
    });

    test("an unexpected field is rejected rather than silently stored", async () => {
        const author = await makeUser("Alice");

        await assert.rejects(
            () => collections.posts().insertOne({
                _id: new ObjectId(),
                author,
                title: "Stowaway",
                content: "Body",
                createdAt: new Date(),
                updatedAt: new Date(),
                isAdmin: true,
            } as never),
            isValidationError,
            "additionalProperties:false should stop writes the model does not describe.",
        );
    });

    test("a reaction type outside the allowed set is rejected", async () => {
        const user = await makeUser("Alice");
        const post = await makePost(user);

        await assert.rejects(
            () => collections.reactions().insertOne({
                _id: new ObjectId(), userId: user, postId: post, type: "meh", createdAt: new Date(),
            } as never),
            isValidationError,
        );
    });

    test("a comment may have a null parent but not a missing one", async () => {
        const user = await makeUser("Alice");
        const post = await makePost(user);

        await collections.comments().insertOne({
            _id: new ObjectId(), postId: post, parentId: null, author: user,
            content: "Top level", createdAt: new Date(),
        });

        await assert.rejects(
            () => collections.comments().insertOne({
                _id: new ObjectId(), postId: post, author: user,
                content: "No parentId key", createdAt: new Date(),
            } as never),
            isValidationError,
        );
    });

    test("every index the query patterns rely on exists", async () => {
        const postIndexes = await collections.posts().indexes();
        const commentIndexes = await collections.comments().indexes();
        const reactionIndexes = await collections.reactions().indexes();

        assert.ok(postIndexes.some((i) => i.name === "author_recent"),
            "Profile pages filter by author and sort newest first.");
        assert.ok(commentIndexes.some((i) => i.name === "post_thread"),
            "Comment threads are loaded by postId in creation order.");

        const unique = reactionIndexes.find((i) => i.name === "one_per_user_post");
        assert.ok(unique, "The one-reaction-per-user rule needs its index.");
        assert.equal(unique.unique, true, "That index must be unique or the rule is unenforced.");

        assert.ok(reactionIndexes.some((i) => i.name === "post_counts"),
            "Reaction counts aggregate by postId and type.");
    });
});
