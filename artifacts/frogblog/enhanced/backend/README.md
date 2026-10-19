# FrogBlog Backend

Express + TypeScript API for the FrogBlog application, backed by MongoDB.

## Tech Stack

- **Node.js** 20+ (developed on 24)
- **Express** 5 — async handlers route rejected promises to the error middleware automatically
- **TypeScript** 5.9, run directly with **tsx** (no build step)
- **MongoDB** 7+ via the native driver, running as a single-node replica set (needed for transactions)
- **Better Auth** — email/password sessions stored in MongoDB
- **Zod** — request validation

## Setup

### 1. MongoDB as a replica set

The app uses a transaction when deleting a post, which requires a replica set. In your `mongod.cfg` add:

```yaml
replication:
  replSetName: rs0
```

Restart MongoDB, then initiate the set once (e.g. `rs.initiate()` in `mongosh`).

### 2. Install and configure

```bash
npm install
cp .env.example .env   # optional — every setting has a default
```

### 3. Run

```bash
npm run dev        # dev server with reload on http://localhost:3000
npm start          # same without file watching
npm test           # unit tests (node:test)
npm run typecheck  # tsc --noEmit
npm run migrate    # one-time upgrade of data written by the original schema
```

Indexes are created automatically on startup (`ensureIndexes`).

## Project Structure

```
backend/
├── scripts/
│   └── migrate.ts           # Legacy-schema → current-schema data migration
└── src/
    ├── index.ts             # Entry point: registers endpoints, fallbacks, starts server
    ├── app.ts               # Express app: CORS, Better Auth mount, body parsing
    ├── config.ts            # All environment-driven settings
    ├── auth.ts              # Better Auth configuration
    ├── mongo.ts             # MongoClient / db handle
    ├── errors.ts            # HttpError + centralized error / 404 middleware
    ├── validation.ts        # Zod helpers (validate(), objectId)
    ├── listener.ts          # Endpoint base class (method, path, middleware, handler)
    ├── middleware/
    │   └── auth.ts          # requireAuth / optionalAuth
    ├── models/              # Document types, Zod input schemas, typed collections, indexes
    ├── services/            # Business logic and all database access
    ├── utils/               # Pure algorithms (pagination, tree building) + tests
    └── listeners/           # One class per route; validates input and calls a service
```

Request flow: `listener` (validate) → `service` (logic + DB) → `models` (typed collections).

## Data Model

| Collection | Key fields | Notes |
|---|---|---|
| `user` | `_id`, `name`, `email` | Managed by Better Auth |
| `profile` | `_id` (= user id), `bio`, `birthday`, `updatedAt` | One per user, upserted |
| `post` | `_id`, `author`, `title`, `content`, `createdAt`, `updatedAt` | Index: `{author, _id}` |
| `comment` | `_id`, `postId`, `parentId` (null = top level), `author`, `content`, `createdAt` | Index: `{postId, createdAt}` |
| `reaction` | `_id`, `userId`, `postId`, `type` (`like`/`dislike`), `createdAt` | **Unique** `{userId, postId}`; index `{postId, type}` |

Like/dislike counts are derived from `reaction`, never stored on the post. Deleting a post removes its comments and reactions in one transaction.

## API

All bodies and responses are JSON. Authenticated routes need the Better Auth session cookie (`credentials: 'include'` on the client). Errors have the shape `{ "error": "...", "details"?: [...] }`.

### Auth

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/register` | `{ email, username, password }` (password ≥ 8 chars) | `201 { id }` |
| POST | `/login` | `{ email, password }` | `200 { id, username }` + session cookie |

Better Auth's own routes are also available under `/api/auth/*`.

### Posts

| Method | Path | Auth | Body / Query | Response |
|---|---|---|---|---|
| GET | `/posts` | optional | `?author=<id>&cursor=<id>&limit=<n>` | `{ posts: [PostView], nextCursor }` |
| GET | `/post/:id` | optional | — | `PostView` |
| POST | `/post` | required | `{ title?, content }` | `201 { id }` |
| PATCH | `/post/:id` | owner | `{ title?, content? }` | updated post |
| DELETE | `/post/:id` | owner | — | `204` |

`PostView` = post fields + `authorName`, `likes`, `dislikes`, `myReaction` (the caller's reaction or `null`).

Pagination is cursor-based: pass the returned `nextCursor` back as `?cursor=` to get the next page; `null` means you've reached the end.

### Comments

| Method | Path | Auth | Body | Response |
|---|---|---|---|---|
| GET | `/post/:id/comments` | — | — | `{ comments: [Comment & { authorName, replies: [...] }] }` (nested) |
| POST | `/post/:id/comment` | required | `{ content, parentId? }` | `201 { id }` |

### Reactions

| Method | Path | Auth | Body | Response |
|---|---|---|---|---|
| PUT | `/post/:id/reaction` | required | `{ type: "like" \| "dislike" }` | `{ likes, dislikes, myReaction }` |
| DELETE | `/post/:id/reaction` | required | — | `{ likes, dislikes, myReaction }` |

Setting a reaction is idempotent; sending a different `type` switches it.

### Profile

| Method | Path | Auth | Body | Response |
|---|---|---|---|---|
| GET | `/profile` | required | — | `{ id, bio, birthday: { day, month, year } }` |
| PUT | `/profile` | required | `{ bio?, birthday_day, birthday_month, birthday_year }` | same as GET |
| POST | `/profile` | required | same as PUT | same as PUT (compatibility alias) |

## Adding an Endpoint

1. Add a Zod schema for the input in `src/models/`.
2. Put the logic in a service under `src/services/`.
3. Create a class extending `Endpoint` in `src/listeners/`; override `get_middlewares()` to return `[requireAuth]` for protected routes.
4. Register it in `src/index.ts`.

Handlers can `throw notFound()`, `forbidden()`, `badRequest()` etc. from `errors.ts` — the error middleware turns them into responses.
