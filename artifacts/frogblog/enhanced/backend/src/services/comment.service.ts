import { ObjectId } from "mongodb";
import { collections } from "../models/index.ts";
import type { CommentDocument, CreateCommentInput } from "../models/comment.model.ts";
import { badRequest, notFound } from "../errors.ts";
import { buildTree, type TreeNode } from "../utils/tree.ts";
import { withAuthorNames } from "./author.service.ts";

export type CommentView = CommentDocument & { authorName: string };
export type CommentThread = TreeNode<CommentView>[];

async function requirePostExists(postId: ObjectId): Promise<void> {
    const exists = await collections.posts().countDocuments({ _id: postId }, { limit: 1 });
    if (exists === 0) throw notFound("Post not found");
}

/**
 * The full comment thread for a post as nested replies.
 * One query for the comments (index-covered, oldest first), one for the
 * author names, then an O(n) in-memory tree build.
 */
export async function getCommentThread(postId: ObjectId): Promise<CommentThread> {
    await requirePostExists(postId);
    const rows = await collections.comments()
        .find({ postId })
        .sort({ createdAt: 1, _id: 1 })
        .toArray();
    return buildTree(await withAuthorNames(rows));
}

export async function createComment(
    postId: ObjectId,
    authorId: ObjectId,
    input: CreateCommentInput,
): Promise<CommentDocument> {
    await requirePostExists(postId);

    if (input.parentId) {
        // A reply must point at a comment on the same post.
        const parent = await collections.comments().findOne(
            { _id: input.parentId },
            { projection: { postId: 1 } },
        );
        if (!parent || !parent.postId.equals(postId)) {
            throw badRequest("parentId does not refer to a comment on this post");
        }
    }

    const comment: CommentDocument = {
        _id: new ObjectId(),
        postId,
        parentId: input.parentId ?? null,
        author: authorId,
        content: input.content,
        createdAt: new Date(),
    };
    await collections.comments().insertOne(comment);
    return comment;
}
