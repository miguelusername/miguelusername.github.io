import { ObjectId, type Filter } from "mongodb";
import { client } from "../mongo.ts";
import { collections } from "../models/index.ts";
import type { CreatePostInput, PostDocument, UpdatePostInput } from "../models/post.model.ts";
import type { ReactionSummary } from "../models/reaction.model.ts";
import { forbidden, notFound } from "../errors.ts";
import { paginateById, type Page } from "../utils/pagination.ts";
import { withAuthorNames } from "./author.service.ts";
import { summarizeReactions } from "./reaction.service.ts";

/** A post as returned by the API: the document plus derived display fields. */
export type PostView = PostDocument & { authorName: string } & ReactionSummary;

export interface ListPostsOptions {
    author?: ObjectId;
    cursor?: ObjectId;
    limit: number;
}

/** Decorate raw post documents with author names and reaction summaries. */
async function toViews(posts: readonly PostDocument[], viewerId: ObjectId | null): Promise<PostView[]> {
    const [named, reactions] = await Promise.all([
        withAuthorNames(posts),
        summarizeReactions(posts.map((post) => post._id), viewerId),
    ]);
    return named.map((post) => ({
        ...post,
        ...(reactions.get(post._id.toHexString()) ?? { likes: 0, dislikes: 0, myReaction: null }),
    }));
}

/**
 * Newest-first feed, optionally filtered to one author, one page at a time.
 * Costs a constant number of queries per page: posts, authors, counts, and
 * (for a logged-in viewer) their own reactions.
 */
export async function listPosts(options: ListPostsOptions, viewerId: ObjectId | null): Promise<Page<PostView>> {
    const filter: Filter<PostDocument> = {};
    if (options.author) filter.author = options.author;
    if (options.cursor) filter._id = { $lt: options.cursor };

    const rows = await collections.posts()
        .find(filter)
        .sort({ _id: -1 })
        .limit(options.limit + 1)
        .toArray();

    const page = paginateById(rows, options.limit);
    return { items: await toViews(page.items, viewerId), nextCursor: page.nextCursor };
}

export async function getPost(id: ObjectId, viewerId: ObjectId | null): Promise<PostView> {
    const post = await collections.posts().findOne({ _id: id });
    if (!post) throw notFound("Post not found");
    const [view] = await toViews([post], viewerId);
    return view!;
}

export async function createPost(authorId: ObjectId, input: CreatePostInput): Promise<PostDocument> {
    const now = new Date();
    const post: PostDocument = {
        _id: new ObjectId(),
        author: authorId,
        title: input.title,
        content: input.content,
        createdAt: now,
        updatedAt: now,
    };
    await collections.posts().insertOne(post);
    return post;
}

/** Load a post and verify the acting user owns it (404 if missing, 403 if not theirs). */
async function requireOwnedPost(id: ObjectId, userId: ObjectId): Promise<PostDocument> {
    const post = await collections.posts().findOne({ _id: id });
    if (!post) throw notFound("Post not found");
    if (!post.author.equals(userId)) throw forbidden("You can only modify your own posts");
    return post;
}

export async function updatePost(id: ObjectId, userId: ObjectId, input: UpdatePostInput): Promise<PostDocument> {
    await requireOwnedPost(id, userId);
    const changes: Partial<PostDocument> = { updatedAt: new Date() };
    if (input.title !== undefined) changes.title = input.title;
    if (input.content !== undefined) changes.content = input.content;

    const updated = await collections.posts().findOneAndUpdate(
        { _id: id },
        { $set: changes },
        { returnDocument: "after" },
    );
    if (!updated) throw notFound("Post not found");
    return updated;
}

/**
 * Delete a post and everything that references it. MongoDB has no cascading
 * foreign keys, so the comments and reactions are removed explicitly inside a
 * transaction — either all three deletes commit or none do.
 */
export async function deletePost(id: ObjectId, userId: ObjectId): Promise<void> {
    await requireOwnedPost(id, userId);

    const session = client.startSession();
    try {
        await session.withTransaction(async () => {
            await collections.comments().deleteMany({ postId: id }, { session });
            await collections.reactions().deleteMany({ postId: id }, { session });
            await collections.posts().deleteOne({ _id: id }, { session });
        });
    } finally {
        await session.endSession();
    }
}
