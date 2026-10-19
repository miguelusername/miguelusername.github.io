import type { Collection, Db, ObjectId } from "mongodb";
import { db } from "../mongo.ts";
import type { PostDocument } from "./post.model.ts";
import type { CommentDocument } from "./comment.model.ts";
import type { ReactionDocument } from "./reaction.model.ts";
import type { ProfileDocument } from "./profile.model.ts";

/** The parts of a Better Auth user document that this app reads. */
export interface UserDocument {
    _id: ObjectId;
    name: string;
    email: string;
    username?: string | null;
    createdAt: Date;
}

/**
 * Typed accessors for every collection. Using these (instead of
 * `db.collection("...")` with a string) gives every read and write a schema.
 */
export const collections = {
    users: (): Collection<UserDocument> => db.collection<UserDocument>("user"),
    profiles: (): Collection<ProfileDocument> => db.collection<ProfileDocument>("profile"),
    posts: (): Collection<PostDocument> => db.collection<PostDocument>("post"),
    comments: (): Collection<CommentDocument> => db.collection<CommentDocument>("comment"),
    reactions: (): Collection<ReactionDocument> => db.collection<ReactionDocument>("reaction"),
};

/**
 * Create every index the query patterns depend on. Safe to run on each
 * startup — MongoDB treats an existing identical index as a no-op.
 *
 * `one_per_user_post` is doing more than making a query fast: being unique, it
 * is what actually makes a second reaction from the same user impossible. The
 * application checks too, but the index is the guarantee.
 */
export async function ensureIndexes(database: Db = db): Promise<void> {
    await Promise.all([
        // Feed pagination sorts by _id (time-ordered) — covered by the default index.
        // Profile pages list a single author's posts, newest first.
        database.collection("post").createIndex({ author: 1, _id: -1 }, { name: "author_recent" }),

        // Loading a post's comment thread.
        database.collection("comment").createIndex({ postId: 1, createdAt: 1 }, { name: "post_thread" }),

        // One reaction per user per post, and fast "count reactions for these posts".
        database.collection("reaction").createIndex({ userId: 1, postId: 1 }, { name: "one_per_user_post", unique: true }),
        database.collection("reaction").createIndex({ postId: 1, type: 1 }, { name: "post_counts" }),
    ]);
}
