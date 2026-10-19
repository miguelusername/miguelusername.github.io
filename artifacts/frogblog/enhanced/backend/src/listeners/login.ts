import type express from "express";
import { z } from "zod";
import { Endpoint, RequestType } from "../listener.ts";
import { auth } from "../auth.ts";
import { validate } from "../validation.ts";
import { HttpError } from "../errors.ts";

const loginSchema = z.object({
    email: z.string().trim().email(),
    password: z.string().min(1, "Password is required"),
});

/** POST /login — email/password sign-in; sets the Better Auth session cookie. */
export class LoginEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.POST; }
    get_endpoint(): string { return "/login"; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const { email, password } = validate(loginSchema, request.body);

        // asResponse gives us the raw Response so the session cookie can be forwarded.
        const result = await auth.api.signInEmail({
            asResponse: true,
            body: { email, password, rememberMe: true },
        });

        if (!result.ok) {
            const body = (await result.json().catch(() => ({}))) as { message?: string };
            throw new HttpError(result.status, body.message || "Invalid email or password");
        }

        const setCookie = result.headers.get("set-cookie");
        if (setCookie) response.setHeader("Set-Cookie", setCookie);

        const data = await result.json();
        response.status(200).json({ id: data.user.id, username: data.user.username ?? data.user.name });
    }
}
