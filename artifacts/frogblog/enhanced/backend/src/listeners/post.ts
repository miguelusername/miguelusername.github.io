import type express from "express";
import { ObjectId } from "mongodb";
import { z } from "zod";
import { Endpoint, RequestType } from "../listener.ts";
import { config } from "../config.ts";
import { requireAuth, optionalAuth, currentUser, maybeUser } from "../middleware/auth.ts";
import { objectId, validate } from "../validation.ts";
import { createPostSchema, updatePostSchema } from "../models/post.model.ts";
import { clampPageSize } from "../utils/pagination.ts";
import * as posts from "../services/post.service.ts";

const listPostsQuerySchema = z.object({
    author: objectId.optional(),
    cursor: objectId.optional(),
    limit: z.coerce.number().optional(),
});

const viewerId = (response: express.Response): ObjectId | null => {
    const user = maybeUser(response);
    return user ? new ObjectId(user.id) : null;
};

/** GET /posts?author=&cursor=&limit= — paginated feed, newest first. */
export class GetPostsEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.GET; }
    get_endpoint(): string { return "/posts"; }
    override get_middlewares() { return [optionalAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const query = validate(listPostsQuerySchema, request.query);
        const { defaultPageSize, maxPageSize } = config.pagination;

        const page = await posts.listPosts({
            ...(query.author ? { author: query.author } : {}),
            ...(query.cursor ? { cursor: query.cursor } : {}),
            limit: clampPageSize(query.limit, defaultPageSize, maxPageSize),
        }, viewerId(response));

        response.status(200).json({ posts: page.items, nextCursor: page.nextCursor });
    }
}

/** GET /post/:id — a single post with author name and reaction counts. */
export class GetPostEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.GET; }
    get_endpoint(): string { return "/post/:id"; }
    override get_middlewares() { return [optionalAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const id = validate(objectId, request.params.id);
        response.status(200).json(await posts.getPost(id, viewerId(response)));
    }
}

/** POST /post — create a post as the logged-in user. */
export class PostPostEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.POST; }
    get_endpoint(): string { return "/post"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const input = validate(createPostSchema, request.body);
        const authorId = new ObjectId(currentUser(response).id);
        const post = await posts.createPost(authorId, input);
        response.status(201).json({ id: post._id });
    }
}

/** PATCH /post/:id — edit your own post. */
export class PatchPostEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.PATCH; }
    get_endpoint(): string { return "/post/:id"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const id = validate(objectId, request.params.id);
        const input = validate(updatePostSchema, request.body);
        const userId = new ObjectId(currentUser(response).id);
        response.status(200).json(await posts.updatePost(id, userId, input));
    }
}

/** DELETE /post/:id — delete your own post and its comments/reactions. */
export class DeletePostEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.DELETE; }
    get_endpoint(): string { return "/post/:id"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const id = validate(objectId, request.params.id);
        const userId = new ObjectId(currentUser(response).id);
        await posts.deletePost(id, userId);
        response.status(204).end();
    }
}
