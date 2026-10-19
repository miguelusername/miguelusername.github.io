import type express from "express";
import { ObjectId } from "mongodb";
import { Endpoint, RequestType } from "../listener.ts";
import { requireAuth, currentUser } from "../middleware/auth.ts";
import { objectId, validate } from "../validation.ts";
import { setReactionSchema } from "../models/reaction.model.ts";
import { collections } from "../models/index.ts";
import { notFound } from "../errors.ts";
import * as reactions from "../services/reaction.service.ts";

async function requirePost(id: ObjectId): Promise<void> {
    const exists = await collections.posts().countDocuments({ _id: id }, { limit: 1 });
    if (exists === 0) throw notFound("Post not found");
}

/** PUT /post/:id/reaction { type: "like" | "dislike" } — set (or change) your reaction. */
export class PutReactionEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.PUT; }
    get_endpoint(): string { return "/post/:id/reaction"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const postId = validate(objectId, request.params.id);
        const { type } = validate(setReactionSchema, request.body);
        await requirePost(postId);

        const userId = new ObjectId(currentUser(response).id);
        await reactions.setReaction(userId, postId, type);

        const summary = (await reactions.summarizeReactions([postId], userId)).get(postId.toHexString());
        response.status(200).json(summary);
    }
}

/** DELETE /post/:id/reaction — remove your reaction. */
export class DeleteReactionEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.DELETE; }
    get_endpoint(): string { return "/post/:id/reaction"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const postId = validate(objectId, request.params.id);
        await requirePost(postId);

        const userId = new ObjectId(currentUser(response).id);
        await reactions.clearReaction(userId, postId);

        const summary = (await reactions.summarizeReactions([postId], userId)).get(postId.toHexString());
        response.status(200).json(summary);
    }
}
