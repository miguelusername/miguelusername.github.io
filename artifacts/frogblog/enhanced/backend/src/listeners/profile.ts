import type express from "express";
import { ObjectId } from "mongodb";
import { Endpoint, RequestType } from "../listener.ts";
import { requireAuth, currentUser } from "../middleware/auth.ts";
import { validate } from "../validation.ts";
import { collections } from "../models/index.ts";
import { upsertProfileSchema, type ProfileDocument } from "../models/profile.model.ts";
import { notFound } from "../errors.ts";

function serializeProfile(profile: ProfileDocument) {
    return {
        id: profile._id.toHexString(),
        bio: profile.bio,
        birthday: {
            day: profile.birthday.getUTCDate(),
            month: profile.birthday.getUTCMonth() + 1,
            year: profile.birthday.getUTCFullYear(),
        },
    };
}

/** GET /profile — the logged-in user's profile. */
export class GetProfileEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.GET; }
    get_endpoint(): string { return "/profile"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(_request: express.Request, response: express.Response): Promise<void> {
        const userId = new ObjectId(currentUser(response).id);
        const profile = await collections.profiles().findOne({ _id: userId });
        if (!profile) throw notFound("Profile not found");
        response.status(200).json(serializeProfile(profile));
    }
}

/**
 * PUT /profile — create or replace the logged-in user's profile.
 * An upsert keyed on the user id, so resubmitting updates the existing profile
 * rather than failing on a duplicate key.
 */
export class PutProfileEndpoint extends Endpoint {
    get_type(): RequestType { return RequestType.PUT; }
    get_endpoint(): string { return "/profile"; }
    override get_middlewares() { return [requireAuth]; }

    async handle(request: express.Request, response: express.Response): Promise<void> {
        const input = validate(upsertProfileSchema, request.body);
        const userId = new ObjectId(currentUser(response).id);

        const profile = await collections.profiles().findOneAndUpdate(
            { _id: userId },
            { $set: { bio: input.bio, birthday: input.birthday, updatedAt: new Date() } },
            { upsert: true, returnDocument: "after" },
        );
        response.status(200).json(serializeProfile(profile!));
    }
}

/** POST /profile — alias for PUT, so either verb creates or replaces the profile. */
export class PostProfileEndpoint extends PutProfileEndpoint {
    override get_type(): RequestType { return RequestType.POST; }
}
