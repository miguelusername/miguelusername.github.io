import type { ObjectId } from "mongodb";
import { collections } from "../models/index.ts";

export const UNKNOWN_AUTHOR = "Unknown User";

/**
 * Resolve display names for a set of user ids with a single `$in` query.
 *
 * Ids are de-duplicated first, so a page of 50 posts written by 3 authors
 * costs one query returning 3 rows rather than one query per post.
 */
export async function lookupAuthorNames(ids: readonly ObjectId[]): Promise<Map<string, string>> {
    const unique = [...new Map(ids.map((id) => [id.toHexString(), id])).values()];
    const names = new Map<string, string>();
    if (unique.length === 0) return names;

    const users = await collections.users()
        .find({ _id: { $in: unique } }, { projection: { name: 1 } })
        .toArray();

    for (const user of users) {
        names.set(user._id.toHexString(), user.name);
    }
    return names;
}

/** Attach `authorName` to each row using a single batched lookup. */
export async function withAuthorNames<T extends { author: ObjectId }>(
    rows: readonly T[],
): Promise<Array<T & { authorName: string }>> {
    const names = await lookupAuthorNames(rows.map((row) => row.author));
    return rows.map((row) => ({
        ...row,
        authorName: names.get(row.author.toHexString()) ?? UNKNOWN_AUTHOR,
    }));
}
