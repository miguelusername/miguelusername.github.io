import type express from "express";

export const RequestType = {
    GET: "get",
    POST: "post",
    PUT: "put",
    PATCH: "patch",
    DELETE: "delete",
} as const;

export type RequestType = (typeof RequestType)[keyof typeof RequestType];

/**
 * Base class for every route. Subclasses declare the HTTP method, the path,
 * any middleware that should run first (e.g. requireAuth), and the handler.
 *
 * Handlers are async and may simply `throw` an HttpError — Express 5 routes
 * rejected promises to the centralized error handler.
 */
export abstract class Endpoint {

    register(app: express.Application): void {
        const method = this.get_type();
        const handler: express.RequestHandler = (req, res, next) => this.handle(req, res, next);
        app[method](this.get_endpoint(), ...this.get_middlewares(), handler);
    }

    /** Middleware to run before `handle`. Override to protect a route. */
    get_middlewares(): express.RequestHandler[] {
        return [];
    }

    abstract get_type(): RequestType;
    abstract get_endpoint(): string;
    abstract handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void>;

}
