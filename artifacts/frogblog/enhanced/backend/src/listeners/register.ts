import type express from "express";
import { z } from "zod";
import { Endpoint, RequestType } from "../listener.ts";
import { auth } from "../auth.ts";
import { validate } from "../validation.ts";

const USERNAME_MIN = 3;
const USERNAME_MAX = 30;
const PASSWORD_MIN = 8;

const registerSchema = z.object({
    email: z.string().trim().email(),
    username: z.string().trim().min(USERNAME_MIN).max(USERNAME_MAX),
    password: z.string().min(PASSWORD_MIN, `Password must be at least ${PASSWORD_MIN} characters`),
});

/** POST /register — create an account. Better Auth rejects duplicate emails with a 4xx. */
export class RegisterEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.POST; }
    get_endpoint(): string { return "/register"; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const { email, username, password } = validate(registerSchema, request.body);

        // The frontend's "Name" field is sent as `username`; it becomes the display name.
        const result = await auth.api.signUpEmail({
            body: { email, password, name: username },
        });

        response.status(201).json({ id: result.user.id });
    }
}
