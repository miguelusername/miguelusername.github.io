import type { Db } from "mongodb";
import { db as defaultDb } from "../mongo.ts";
import { POST_CONTENT_MAX, POST_TITLE_MAX } from "./post.model.ts";
import { COMMENT_CONTENT_MAX } from "./comment.model.ts";
import { REACTION_TYPES } from "./reaction.model.ts";
import { PROFILE_BIO_MAX } from "./profile.model.ts";

/**
 * Collection-level JSON Schema validators.
 *
 * Zod checks input at the API boundary, but it only protects writes that come
 * through the API. A migration script, a console session, or a second service
 * written later can all write straight to the collection. These validators live
 * in the database itself, so the shape of a document is guaranteed no matter
 * what client produced it.
 *
 * `validationLevel: "moderate"` applies the rules to inserts and to updates of
 * documents that already conform, but leaves pre-existing non-conforming
 * documents writable. That keeps a schema change from locking out legacy data;
 * `scripts/migrate.ts` is what brings old documents into conformance.
 */

const objectId = { bsonType: "objectId" } as const;

const postSchema = {
    bsonType: "object",
    required: ["_id", "author", "title", "content", "createdAt", "updatedAt"],
    additionalProperties: false,
    properties: {
        _id: objectId,
        author: objectId,
        title: { bsonType: "string", minLength: 1, maxLength: POST_TITLE_MAX },
        content: { bsonType: "string", minLength: 1, maxLength: POST_CONTENT_MAX },
        createdAt: { bsonType: "date" },
        updatedAt: { bsonType: "date" },
    },
};

const commentSchema = {
    bsonType: "object",
    required: ["_id", "postId", "parentId", "author", "content", "createdAt"],
    additionalProperties: false,
    properties: {
        _id: objectId,
        postId: objectId,
        // Null marks a top-level comment; anything else must reference a comment.
        parentId: { bsonType: ["objectId", "null"] },
        author: objectId,
        content: { bsonType: "string", minLength: 1, maxLength: COMMENT_CONTENT_MAX },
        createdAt: { bsonType: "date" },
    },
};

const reactionSchema = {
    bsonType: "object",
    required: ["_id", "userId", "postId", "type", "createdAt"],
    additionalProperties: false,
    properties: {
        _id: objectId,
        userId: objectId,
        postId: objectId,
        type: { enum: [...REACTION_TYPES] },
        createdAt: { bsonType: "date" },
    },
};

const profileSchema = {
    bsonType: "object",
    required: ["_id", "bio", "birthday", "updatedAt"],
    additionalProperties: false,
    properties: {
        _id: objectId,
        bio: { bsonType: "string", maxLength: PROFILE_BIO_MAX },
        birthday: { bsonType: "date" },
        updatedAt: { bsonType: "date" },
    },
};

/**
 * The `user` collection is deliberately absent: Better Auth owns that shape and
 * may add fields across versions, so pinning a schema to it would break logins
 * on an upgrade.
 */
const validators: ReadonlyArray<readonly [string, object]> = [
    ["post", postSchema],
    ["comment", commentSchema],
    ["reaction", reactionSchema],
    ["profile", profileSchema],
];

const NAMESPACE_EXISTS = 48;

/**
 * Applies every validator, creating the collection when it does not exist yet
 * and amending it when it does. Safe to run on each startup.
 */
export async function ensureSchemas(database: Db = defaultDb): Promise<void> {
    for (const [name, schema] of validators) {
        const options = {
            validator: { $jsonSchema: schema },
            validationLevel: "moderate",
            validationAction: "error",
        };

        try {
            await database.createCollection(name, options);
        } catch (error: unknown) {
            const code = (error as { code?: number }).code;
            if (code !== NAMESPACE_EXISTS) throw error;

            // Collection already exists, so amend its validator in place.
            await database.command({ collMod: name, ...options });
        }
    }
}
