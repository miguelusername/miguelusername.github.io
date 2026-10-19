import { test } from "node:test";
import assert from "node:assert/strict";
import { ObjectId } from "mongodb";
import { buildTree } from "./tree.ts";

const id = () => new ObjectId();

test("nests replies under their parents and keeps sibling order", () => {
    const a = id(), b = id(), a1 = id(), a2 = id(), a1x = id();
    const rows = [
        { _id: a, parentId: null, label: "a" },
        { _id: b, parentId: null, label: "b" },
        { _id: a1, parentId: a, label: "a.1" },
        { _id: a2, parentId: a, label: "a.2" },
        { _id: a1x, parentId: a1, label: "a.1.x" },
    ];

    const tree = buildTree(rows);

    assert.deepEqual(tree.map((n) => n.label), ["a", "b"]);
    assert.deepEqual(tree[0]!.replies.map((n) => n.label), ["a.1", "a.2"]);
    assert.deepEqual(tree[0]!.replies[0]!.replies.map((n) => n.label), ["a.1.x"]);
    assert.equal(tree[1]!.replies.length, 0);
});

test("promotes orphans (parent missing) to roots instead of dropping them", () => {
    const orphan = { _id: id(), parentId: id(), label: "orphan" };
    const tree = buildTree([orphan]);
    assert.equal(tree.length, 1);
    assert.equal(tree[0]!.label, "orphan");
});

test("handles an empty list", () => {
    assert.deepEqual(buildTree([]), []);
});

test("does not mutate the input rows", () => {
    const row = { _id: id(), parentId: null };
    buildTree([row]);
    assert.equal("replies" in row, false);
});
