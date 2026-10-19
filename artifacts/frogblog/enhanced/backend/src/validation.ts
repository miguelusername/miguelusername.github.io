import { ObjectId } from "mongodb";
import { z } from "zod";
import { badRequest } from "./errors.ts";

/** A 24-character hex string that MongoDB will accept as an ObjectId. */
export const objectIdString = z
    .string()
    .refine((value) => ObjectId.isValid(value) && String(new ObjectId(value)) === value, {
        message: "Must be a valid ObjectId",
    });

/** Same as objectIdString, but hands back an actual ObjectId instance. */
export const objectId = objectIdString.transform((value) => new ObjectId(value));

/**
 * Parse `data` against `schema`, throwing a 400 HttpError (with the field
 * issues) on failure. Handlers call this instead of reading request fields raw.
 */
export function validate<T extends z.ZodType>(schema: T, data: unknown): z.infer<T> {
    const result = schema.safeParse(data);
    if (!result.success) {
        const issues = result.error.issues.map((issue) => ({
            path: issue.path.join("."),
            message: issue.message,
        }));
        throw badRequest("Validation failed", issues);
    }
    return result.data;
}
