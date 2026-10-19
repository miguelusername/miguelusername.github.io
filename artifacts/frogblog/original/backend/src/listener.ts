import express from "express";
import { auth } from "./auth.ts";
import { fromNodeHeaders } from "better-auth/node";

export const RequestType = {
    GET: "get",
    POST: "post",
    DELETE: "delete"
} as const;

export type RequestType = (typeof RequestType)[keyof typeof RequestType]

export abstract class Endpoint {

    register(app: express.Application) {
        var func = this.get_func(app)
        var endpoint = this.get_endpoint()
        func(endpoint, (req, res, next) => this.handle(req, res, next)); 
    }

    get_func(app: express.Application) {
        switch (this.get_type()) {
            case RequestType.GET:
                return app.get.bind(app)
            case RequestType.POST:
                return app.post.bind(app)
            case RequestType.DELETE:
                return app.delete.bind(app)
        }
    }

    abstract get_type(): RequestType;
    abstract get_endpoint(): string;
    abstract handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void>;

}