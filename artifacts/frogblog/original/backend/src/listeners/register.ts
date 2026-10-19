import type { NextFunction, Request, Response } from "express";
import { Endpoint, RequestType } from "../listener.ts";
import {auth} from "../auth.ts";

export class RegisterEndpoint extends Endpoint {

    get_type(): RequestType {
        return RequestType.POST
    }

    get_endpoint(): string {
        return "/register"
    }

    async handle(request: Request, response: Response, next: NextFunction): Promise<void> {
        var b = request.body
        var email = b.email
        var name = b.username
        var password = b.password

        var out = await auth.api.signUpEmail({
            body: {
                email,
                password,
                name
            }
        })

        response.status(200).json({id: out.user.id})
    }
    
}