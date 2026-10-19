import type express from "express";
import { Endpoint, RequestType } from "../listener.ts";
import { auth } from "../auth.ts";

export class LoginEndpoint extends Endpoint {

    get_type(): RequestType {
        return RequestType.POST
    }

    get_endpoint(): string {
        return "/login"
    }

    async handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void> {
        var body = request.body
        var email = body.email
        var password = body.password

        var user = await auth.api.signInEmail({
            asResponse: true,
            body: {
                email,
                password,
                rememberMe: true,
                callbackURL: "https://localhost:3000/callback"
            }
        })

        const userData = await user.json()

        const setCookieHeader = user.headers.get("set-cookie")
        if (setCookieHeader) {
            response.setHeader("Set-Cookie", setCookieHeader)
        }

        response.status(200).json({id: userData.user.id, username: userData.user.username, uri: userData.url})
    }
    
}