import { ObjectId } from "mongodb";
import { client, db } from "../../src/mongo.ts";
import { config } from "../../src/config.ts";
import { collections, ensureIndexes } from "../../src/models/index.ts";
import { ensureSchemas } from "../../src/models/schema.ts";

export { client, db, collections };

/** Databases this suite is allowed to wipe. */
const TEST_DATABASE_SUFFIX = "_test";

/**
 * Connect, apply schemas and indexes, and start from an empty database.
 *
 * Refuses to run unless the configured database name ends in `_test`, because
 * every test here deletes documents and the teardown drops the database. Run
 * via `npm run test:db`, which supplies `.env.test`.
 *
 * That script also pins `--test-concurrency=1`: these files share one database,
 * so running them in parallel lets one file's cleanup delete another file's
 * fixtures mid-test.
 */
export async function setupDatabase(): Promise<void> {
    if (!config.mongo.database.endsWith(TEST_DATABASE_SUFFIX)) {
        throw new Error(
            `Refusing to run integration tests against "${config.mongo.database}". `
            + `Use "npm run test:db" so MONGODB_DATABASE ends in "${TEST_DATABASE_SUFFIX}".`,
        );
    }

    await client.connect();
    await ensureSchemas();
    await ensureIndexes();
    await clearData();
}

/** Remove documents but keep collections, so validators and indexes survive. */
export async function clearData(): Promise<void> {
    await Promise.all([
        collections.posts().deleteMany({}),
        collections.comments().deleteMany({}),
        collections.reactions().deleteMany({}),
        collections.profiles().deleteMany({}),
        collections.users().deleteMany({}),
    ]);
}

export async function teardownDatabase(): Promise<void> {
    await db.dropDatabase();
    await client.close();
}

/** Insert a user directly; Better Auth owns this collection in the real app. */
export async function makeUser(name: string): Promise<ObjectId> {
    const id = new ObjectId();
    await collections.users().insertOne({
        _id: id,
        name,
        email: `${name.toLowerCase().replace(/\s+/g, "")}@test.local`,
        createdAt: new Date(),
    });
    return id;
}

export async function makePost(author: ObjectId, title = "Test Post", content = "Body"): Promise<ObjectId> {
    const id = new ObjectId();
    const now = new Date();
    await collections.posts().insertOne({ _id: id, author, title, content, createdAt: now, updatedAt: now });
    return id;
}

/** True when the error is a MongoDB document-validation failure. */
export function isValidationError(error: unknown): boolean {
    const err = error as { code?: number; codeName?: string };
    return err.code === 121 || err.codeName === "DocumentValidationFailure";
}

/** True when the error is a duplicate-key violation. */
export function isDuplicateKeyError(error: unknown): boolean {
    return (error as { code?: number }).code === 11000;
}
