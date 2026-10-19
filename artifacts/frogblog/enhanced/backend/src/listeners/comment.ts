import type express from "express";
import { ObjectId } from "mongodb";
import { Endpoint, RequestType } from "../listener.ts";
import { requireAuth, currentUser } from "../middleware/auth.ts";
import { objectId, validate } from "../validation.ts";
import { createCommentSchema } from "../models/comment.model.ts";
import * as comments from "../services/comment.service.ts";

/** GET /post/:id/comments — the post's comments as a nested reply tree. */
export class GetCommentsEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.GET; }
    get_endpoint(): string { return "/post/:id/comments"; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const postId = validate(objectId, request.params.id);
        response.status(200).json({ comments: await comments.getCommentThread(postId) });
    }
}

/** POST /post/:id/comment — comment on a post (or reply to a comment via parentId). */
export class PostCommentEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.POST; }
    get_endpoint(): string { return "/post/:id/comment"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const postId = validate(objectId, request.params.id);
        const input = validate(createCommentSchema, request.body);
        const authorId = new ObjectId(currentUser(response).id);
        const comment = await comments.createComment(postId, authorId, input);
        response.status(201).json({ id: comment._id });
    }
}
