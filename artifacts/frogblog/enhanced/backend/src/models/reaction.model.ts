import type { ObjectId } from "mongodb";
import { z } from "zod";

export const REACTION_TYPES = ["like", "dislike"] as const;
export type ReactionType = (typeof REACTION_TYPES)[number];

/**
 * Shape of a document in the `reaction` collection. One document per
 * (user, post) pair, enforced by a unique compound index, so a user can never
 * react to the same post twice. Like and dislike counts are derived from this
 * collection rather than stored as mutable counters on the post, which keeps
 * the counts and the reactions from drifting apart.
 */
export interface ReactionDocument {
    _id: ObjectId;
    userId: ObjectId;
    postId: ObjectId;
    type: ReactionType;
    createdAt: Date;
}

export const setReactionSchema = z.object({
    type: z.enum(REACTION_TYPES),
});
export type SetReactionInput = z.infer<typeof setReactionSchema>;

/** Aggregated counts attached to a post in API responses. */
export interface ReactionSummary {
    likes: number;
    dislikes: number;
    /** The requesting user's own reaction, if they are logged in and have one. */
    myReaction: ReactionType | null;
}
