import { test } from "node:test";
import assert from "node:assert/strict";
import { ObjectId } from "mongodb";
import { clampPageSize, paginateById } from "./pagination.ts";

const rows = (n: number) => Array.from({ length: n }, () => ({ _id: new ObjectId() }));

test("returns no cursor when the rows fit in one page", () => {
    const page = paginateById(rows(3), 5);
    assert.equal(page.items.length, 3);
    assert.equal(page.nextCursor, null);
});

test("trims the extra row and uses the last kept row as the cursor", () => {
    const input = rows(6); // limit + 1
    const page = paginateById(input, 5);
    assert.equal(page.items.length, 5);
    assert.equal(page.nextCursor, input[4]!._id.toHexString());
});

test("exactly `limit` rows means no further page", () => {
    const page = paginateById(rows(5), 5);
    assert.equal(page.nextCursor, null);
});

test("clampPageSize applies default, floor, ceiling and truncation", () => {
    assert.equal(clampPageSize(undefined, 20, 50), 20);
    assert.equal(clampPageSize(Number.NaN, 20, 50), 20);
    assert.equal(clampPageSize(0, 20, 50), 1);
    assert.equal(clampPageSize(-7, 20, 50), 1);
    assert.equal(clampPageSize(999, 20, 50), 50);
    assert.equal(clampPageSize(7.9, 20, 50), 7);
});
