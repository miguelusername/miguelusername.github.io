import type express from "express";
import { fromNodeHeaders } from "better-auth/node";
import { auth } from "../auth.ts";
import { unauthorized } from "../errors.ts";

/** The subset of the Better Auth session user that handlers rely on. */
export interface AuthUser {
    id: string;
    name: string;
    email: string;
}

async function loadSessionUser(request: express.Request): Promise<AuthUser | null> {
    const session = await auth.api.getSession({ headers: fromNodeHeaders(request.headers) });
    return session ? session.user : null;
}

/**
 * Rejects the request with 401 unless a valid session cookie is present.
 * On success the user is exposed to the handler via `response.locals.user`.
 */
export async function requireAuth(
    request: express.Request,
    response: express.Response,
    next: express.NextFunction,
): Promise<void> {
    const user = await loadSessionUser(request);
    if (!user) throw unauthorized();
    response.locals.user = user;
    next();
}

/**
 * Like requireAuth, but lets anonymous requests through. Used by public
 * routes that can personalize their response (e.g. "did I like this?").
 */
export async function optionalAuth(
    request: express.Request,
    response: express.Response,
    next: express.NextFunction,
): Promise<void> {
    response.locals.user = await loadSessionUser(request);
    next();
}

/** Typed accessor for the user attached by requireAuth. */
export function currentUser(response: express.Response): AuthUser {
    const user = response.locals.user as AuthUser | null | undefined;
    if (!user) throw unauthorized();
    return user;
}

/** Typed accessor for the user attached by optionalAuth (may be null). */
export function maybeUser(response: express.Response): AuthUser | null {
    return (response.locals.user as AuthUser | null | undefined) ?? null;
}
