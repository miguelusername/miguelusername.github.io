import type { ObjectId } from "mongodb";
import { z } from "zod";

export const POST_TITLE_MAX = 100;
export const POST_CONTENT_MAX = 10_000;
export const DEFAULT_POST_TITLE = "Untitled Post";

/** Shape of a document in the `post` collection. */
export interface PostDocument {
    _id: ObjectId;
    author: ObjectId;
    title: string;
    content: string;
    createdAt: Date;
    updatedAt: Date;
}

export const createPostSchema = z.object({
    title: z.string().trim().min(1).max(POST_TITLE_MAX).default(DEFAULT_POST_TITLE),
    content: z.string().trim().min(1, "Content is required").max(POST_CONTENT_MAX),
});
export type CreatePostInput = z.infer<typeof createPostSchema>;

export const updatePostSchema = z
    .object({
        title: z.string().trim().min(1).max(POST_TITLE_MAX).optional(),
        content: z.string().trim().min(1).max(POST_CONTENT_MAX).optional(),
    })
    .refine((data) => data.title !== undefined || data.content !== undefined, {
        message: "Provide a title or content to update",
    });
export type UpdatePostInput = z.infer<typeof updatePostSchema>;
