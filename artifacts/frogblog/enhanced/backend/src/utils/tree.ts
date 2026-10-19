import type { ObjectId } from "mongodb";

interface Node {
    _id: ObjectId;
    parentId: ObjectId | null;
}

export type TreeNode<T extends Node> = T & { replies: TreeNode<T>[] };

/**
 * Assemble a flat adjacency list (each row knows only its parent) into a
 * forest of nested nodes.
 *
 * Two linear passes: the first indexes every node by id in a Map, the second
 * links each node onto its parent's `replies` (or the root list). O(n) time
 * and O(n) extra space, versus O(n²) for the naive "filter the array for my
 * children" approach at every level.
 *
 * Rows whose parent is missing (e.g. the parent was deleted) are promoted to
 * roots rather than silently dropped. Input order is preserved within each
 * sibling list, so pass rows pre-sorted (e.g. by createdAt).
 */
export function buildTree<T extends Node>(rows: readonly T[]): TreeNode<T>[] {
    const byId = new Map<string, TreeNode<T>>();
    for (const row of rows) {
        byId.set(row._id.toHexString(), { ...row, replies: [] });
    }

    const roots: TreeNode<T>[] = [];
    for (const row of rows) {
        const node = byId.get(row._id.toHexString())!;
        const parent = row.parentId ? byId.get(row.parentId.toHexString()) : undefined;
        if (parent) {
            parent.replies.push(node);
        } else {
            roots.push(node);
        }
    }
    return roots;
}
