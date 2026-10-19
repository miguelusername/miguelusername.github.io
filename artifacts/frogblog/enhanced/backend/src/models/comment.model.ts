import type { ObjectId } from "mongodb";
import { z } from "zod";
import { objectId } from "../validation.ts";

export const COMMENT_CONTENT_MAX = 2_000;

/**
 * Shape of a document in the `comment` collection. Comments always belong to
 * a post; `parentId` is null for top-level comments and points at another
 * comment (on the same post) for replies.
 */
export interface CommentDocument {
    _id: ObjectId;
    postId: ObjectId;
    parentId: ObjectId | null;
    author: ObjectId;
    content: string;
    createdAt: Date;
}

export const createCommentSchema = z.object({
    content: z.string().trim().min(1, "Content is required").max(COMMENT_CONTENT_MAX),
    parentId: objectId.optional(),
});
export type CreateCommentInput = z.infer<typeof createCommentSchema>;
