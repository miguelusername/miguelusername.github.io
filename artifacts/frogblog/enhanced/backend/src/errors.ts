import type express from "express";
import { MongoServerError } from "mongodb";

/**
 * An error that carries an HTTP status. Handlers throw these and the
 * centralized error middleware turns them into a consistent JSON response.
 */
export class HttpError extends Error {
    constructor(
        public readonly status: number,
        message: string,
        public readonly details?: unknown,
    ) {
        super(message);
        this.name = "HttpError";
    }
}

export const badRequest = (message: string, details?: unknown) => new HttpError(400, message, details);
export const unauthorized = (message = "Authentication required") => new HttpError(401, message);
export const forbidden = (message = "You do not have permission to do that") => new HttpError(403, message);
export const notFound = (message = "Not found") => new HttpError(404, message);

const MONGO_DUPLICATE_KEY = 11000;

/** Better Auth throws APIError objects that expose a numeric statusCode and a body.message. */
interface BetterAuthLikeError {
    statusCode: number;
    body?: { message?: string };
}

function isBetterAuthError(err: unknown): err is BetterAuthLikeError {
    return typeof err === "object" && err !== null && typeof (err as BetterAuthLikeError).statusCode === "number";
}

/** 404 for any route that no endpoint claimed. Registered after every endpoint. */
export function notFoundHandler(request: express.Request, response: express.Response): void {
    response.status(404).json({ error: `No route for ${request.method} ${request.path}` });
}

/**
 * Centralized error handler. Express 5 forwards rejected promises from async
 * handlers here automatically, so individual handlers don't need try/catch.
 */
export function errorHandler(
    err: unknown,
    _request: express.Request,
    response: express.Response,
    _next: express.NextFunction,
): void {
    if (err instanceof HttpError) {
        response.status(err.status).json({
            error: err.message,
            ...(err.details !== undefined ? { details: err.details } : {}),
        });
        return;
    }

    if (isBetterAuthError(err)) {
        response.status(err.statusCode).json({ error: err.body?.message || "Authentication error" });
        return;
    }

    if (err instanceof MongoServerError && err.code === MONGO_DUPLICATE_KEY) {
        response.status(409).json({ error: "That record already exists" });
        return;
    }

    console.error("Unhandled error:", err);
    response.status(500).json({ error: "Internal server error" });
}
