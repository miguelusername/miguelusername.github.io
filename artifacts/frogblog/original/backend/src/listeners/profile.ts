import type express from "express";
import { Endpoint, RequestType } from "../listener.ts";
import { auth } from "../auth.ts";
import { fromNodeHeaders } from "better-auth/node";
import { client, db } from "../mongo.ts";
import { ObjectId } from "mongodb";
import { getSession } from "better-auth/api";

export class GetProfileEndpoint extends Endpoint {

    get_type(): RequestType {
        return RequestType.GET
    }

    get_endpoint(): string {
        return "/profile"
    }

    async handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void> {

        const session = await auth.api.getSession({
            headers: fromNodeHeaders(request.headers)
        })
        if (session == null) {
            response.status(401).json({})
            return;
        }
        const user = session.user

        let result = await db.collection("profile").findOne({
            _id: new ObjectId(user.id)
        })

        if (!result) {
            response.status(404).json({})
            return;
        }

        var birthday: Date = result.birthday;

        response.status(200).json({
            id: result._id.toString(),
            bio: result.bio,
            birthday: {
                day: birthday.getDate(),
                month: birthday.getMonth() + 1,
                year: birthday.getFullYear()
            }
        })
    }
    
}

export class PostProfileEndpoint extends Endpoint {

    get_type(): RequestType {
        return RequestType.POST
    }

    get_endpoint(): string {
        return "/profile"
    }

    async handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void> {

        const body = request.body

        var bio: string = body.bio
        var birthday_day: number = body.birthday_day
        var birthday_month: number = body.birthday_month
        var birthday_year: number = body.birthday_year

        const session = await auth.api.getSession({
            headers: fromNodeHeaders(request.headers)
        }) 
        if (!session) {
            response.status(401).json({})
            return;
        }
        const user = session.user

        await db.collection("profile").insertOne({
            _id: new ObjectId(user.id),
            bio: bio,
            birthday: new Date(birthday_year, birthday_month - 1, birthday_day)
        })

        response.status(200).json({id: user.id})
    }
    
}