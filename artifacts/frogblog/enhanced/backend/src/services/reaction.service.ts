import { ObjectId } from "mongodb";
import { collections } from "../models/index.ts";
import type { ReactionSummary, ReactionType } from "../models/reaction.model.ts";

const emptySummary = (): ReactionSummary => ({ likes: 0, dislikes: 0, myReaction: null });

/**
 * Like/dislike counts for a set of posts, plus the viewer's own reaction.
 *
 * Two queries total regardless of how many posts are passed: one `$group`
 * aggregation for the counts and (if there is a viewer) one `$in` lookup for
 * their reactions. Results come back keyed by post id.
 */
export async function summarizeReactions(
    postIds: readonly ObjectId[],
    viewerId: ObjectId | null,
): Promise<Map<string, ReactionSummary>> {
    const summaries = new Map<string, ReactionSummary>();
    if (postIds.length === 0) return summaries;
    for (const id of postIds) summaries.set(id.toHexString(), emptySummary());

    const counts = await collections.reactions()
        .aggregate<{ _id: { postId: ObjectId; type: ReactionType }; count: number }>([
            { $match: { postId: { $in: [...postIds] } } },
            { $group: { _id: { postId: "$postId", type: "$type" }, count: { $sum: 1 } } },
        ])
        .toArray();

    for (const row of counts) {
        const summary = summaries.get(row._id.postId.toHexString());
        if (!summary) continue;
        if (row._id.type === "like") summary.likes = row.count;
        else summary.dislikes = row.count;
    }

    if (viewerId) {
        const mine = await collections.reactions()
            .find({ userId: viewerId, postId: { $in: [...postIds] } }, { projection: { postId: 1, type: 1 } })
            .toArray();
        for (const reaction of mine) {
            const summary = summaries.get(reaction.postId.toHexString());
            if (summary) summary.myReaction = reaction.type;
        }
    }

    return summaries;
}

/**
 * Create or change the viewer's reaction on a post. The unique
 * (userId, postId) index plus `upsert` make this idempotent: repeating a
 * like is a no-op and switching like→dislike updates the same row.
 */
export async function setReaction(userId: ObjectId, postId: ObjectId, type: ReactionType): Promise<void> {
    await collections.reactions().updateOne(
        { userId, postId },
        { $set: { type }, $setOnInsert: { _id: new ObjectId(), userId, postId, createdAt: new Date() } },
        { upsert: true },
    );
}

export async function clearReaction(userId: ObjectId, postId: ObjectId): Promise<boolean> {
    const result = await collections.reactions().deleteOne({ userId, postId });
    return result.deletedCount === 1;
}
