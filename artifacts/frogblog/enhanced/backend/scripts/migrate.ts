/**
 * One-time migration from the original schema to the current one.
 *
 *   post.parent  →  moved into the `comment` collection (as top-level comments)
 *   post.upload_date  →  post.createdAt / post.updatedAt
 *   post.likes / post.dislikes  →  removed (counts now derive from `reaction`)
 *
 * Idempotent: re-running finds nothing left to migrate. Run with `npm run migrate`.
 */
import { ObjectId } from "mongodb";
import { client, db } from "../src/mongo.ts";
import { ensureIndexes } from "../src/models/index.ts";

interface LegacyPost {
    _id: ObjectId;
    author: ObjectId;
    title?: string;
    content: string;
    upload_date?: Date;
    likes?: number;
    dislikes?: number;
    parent?: ObjectId;
}

async function migrate(): Promise<void> {
    await client.connect();
    const posts = db.collection<LegacyPost>("post");
    const comments = db.collection("comment");

    // 1. Legacy replies (posts with a parent) become comments on that parent.
    const replies = await posts.find({ parent: { $exists: true } }).toArray();
    if (replies.length > 0) {
        await comments.insertMany(replies.map((reply) => ({
            _id: reply._id,
            postId: reply.parent!,
            parentId: null,
            author: reply.author,
            content: reply.content,
            createdAt: reply.upload_date ?? reply._id.getTimestamp(),
        })));
        await posts.deleteMany({ _id: { $in: replies.map((reply) => reply._id) } });
    }
    console.log(`Moved ${replies.length} reply post(s) into the comment collection`);

    // 2. Rename the timestamp and drop the dead counters on remaining posts.
    const renamed = await posts.updateMany(
        { upload_date: { $exists: true } },
        [
            { $set: { createdAt: "$upload_date", updatedAt: "$upload_date" } },
            { $unset: ["upload_date", "likes", "dislikes"] },
        ],
    );
    console.log(`Updated ${renamed.modifiedCount} post(s) to the new field names`);

    await ensureIndexes();
    console.log("Indexes are in place");
}

migrate()
    .catch((error: unknown) => {
        console.error("Migration failed:", error);
        process.exitCode = 1;
    })
    .finally(() => client.close());
