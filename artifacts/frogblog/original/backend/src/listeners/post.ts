import type express from "express";
import { Endpoint, RequestType } from "../listener.ts";
import { auth } from "../auth.ts";
import { fromNodeHeaders } from "better-auth/node";
import { client, db } from "../mongo.ts";
import { ObjectId } from "mongodb";
import { getSession } from "better-auth/api";

export class GetPostsEndpoint extends Endpoint {

    get_type(): RequestType {
        return RequestType.GET
    }

    get_endpoint(): string {
        return "/posts"
    }

    async handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void> {
        // GET requests use query parameters, not body
        var author: string | undefined = request.query.author as string | undefined
        var parent: string | undefined = request.query.parent as string | undefined

        var query: any = {}

        if (author) {
            query.author = new ObjectId(author)
        }

        if (parent) {
            query.parent = new ObjectId(parent)
        }

        let posts = await db.collection("post").find(query).toArray()

        // Lookup usernames for each post
        const postsWithUsernames = await Promise.all(
            posts.map(async (post) => {
                const user = await db.collection("user").findOne({ _id: post.author })
                return {
                    ...post,
                    authorName: user?.name || "Unknown User"
                }
            })
        )

        response.status(200).json(postsWithUsernames)
    }
    
}

export class PostPostEndpoint extends Endpoint {

    get_type(): RequestType {
        return RequestType.POST
    }

    get_endpoint(): string {
        return "/post"
    }

    async handle(request: express.Request, response: express.Response, next: express.NextFunction): Promise<void> {

        var body = request.body

        var title: string = body.title || "Untitled Post"
        var content: string = body.content
        var parent: string | undefined = body.parent

        const session = await auth.api.getSession({
            headers: fromNodeHeaders(request.headers)
        }) 
        if (!session) {
            response.status(401).json({})
            return;
        }
        const user = session.user

        var obj: any = {
            _id: new ObjectId(),
            author: new ObjectId(user.id),
            title,
            content,
            upload_date: new Date(),
            likes: 0,
            dislikes: 0
        }

        if (parent) {
            obj.parent = new ObjectId(parent)
        }

        await db.collection("post").insertOne(obj)

        response.status(200).json({id: obj._id})
    }
    
}