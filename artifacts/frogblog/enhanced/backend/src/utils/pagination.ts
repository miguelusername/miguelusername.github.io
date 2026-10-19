import type { ObjectId } from "mongodb";

export interface Page<T> {
    items: T[];
    /** Pass back as `?cursor=` to fetch the next page; null when there is no more. */
    nextCursor: string | null;
}

/**
 * Cursor pagination over documents sorted by `_id` descending.
 *
 * ObjectIds embed their creation time, so sorting by `_id` is a newest-first
 * sort that never needs a separate index, and "everything older than the last
 * thing I saw" is a single `_id < cursor` range scan — O(page size) regardless
 * of how large the collection grows, unlike skip/offset paging.
 *
 * Callers fetch `limit + 1` rows; the extra row only tells us whether another
 * page exists and becomes the cursor.
 */
export function paginateById<T extends { _id: ObjectId }>(rows: T[], limit: number): Page<T> {
    if (rows.length <= limit) {
        return { items: rows, nextCursor: null };
    }
    const items = rows.slice(0, limit);
    const last = items[items.length - 1];
    return { items, nextCursor: last ? last._id.toHexString() : null };
}

/** Clamp a requested page size into [1, max], falling back to the default. */
export function clampPageSize(requested: number | undefined, fallback: number, max: number): number {
    if (requested === undefined || Number.isNaN(requested)) return fallback;
    return Math.min(Math.max(Math.trunc(requested), 1), max);
}
