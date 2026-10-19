# FrogBlog Backend

Express + TypeScript backend API for the FrogBlog application.

## Tech Stack

- **Node.js** v20.18.0
- **Express** 5.1.0
- **TypeScript** 5.9.3
- **Better Auth** 1.3.34+ (Authentication)
- **tsx** (TypeScript execution for development)

## Setup Instructions

### 1. Install Dependencies

```bash
npm install
```

### 2. Environment Configuration

Create a `.env` file in the backend directory:

```bash
PORT=3000
```

For production, you'll also need to add database configuration variables here.

### 3. Run Development Server

```bash
npm run dev
```

The server will start on `http://localhost:3000` with auto-reload on file changes.

## Project Structure

```
backend/
├── src/
│   ├── index.ts          # Server entry point
│   ├── app.ts            # Express app configuration
│   ├── auth.ts           # Better Auth configuration
│   ├── listener.ts       # Endpoint interface
│   ├── listeners/        # Custom endpoint handlers
│   │   └── login.ts
│   └── posts/            # Blog post related code
│       ├── posts.model.ts
│       ├── posts.routes.ts
│       └── posts.service.ts
├── package.json
├── tsconfig.json
└── .env                  # Environment variables (create this)
```

## API Endpoints

### Authentication (via Better Auth)

#### Register User
```
POST /api/auth/sign-up
Content-Type: application/json

{
  "name": "string",
  "email": "string",
  "password": "string"
}
```

#### Login User
```
POST /api/auth/sign-in
Content-Type: application/json

{
  "email": "string",
  "password": "string"
}
```

### Blog Endpoints (Coming Soon)

- `GET /api/blogs` - Get all public blogs
- `GET /api/blog/:id` - Get specific blog
- `POST /api/blog` - Create new blog post (authenticated)
- `PUT /api/blog/:id` - Update blog post (authenticated)
- `DELETE /api/blog/:id` - Delete blog post (authenticated)

### User Endpoints (Coming Soon)

- `GET /api/user/:id` - Get user profile

## CORS Configuration

The backend is configured to allow requests from the frontend running on `http://localhost:5173`:

```typescript
app.use(cors({
  origin: 'http://localhost:5173',
  credentials: true
}))
```

For production, update this to your deployed frontend URL.

## Database Setup (Required)

Better Auth requires a database to store user information. The current configuration is incomplete and needs:

1. Choose a database (PostgreSQL, MySQL, or SQLite)
2. Install the appropriate database driver
3. Configure the connection in `auth.ts`
4. Run migrations to create user tables

Example configuration for Better Auth with a database will be added once the database is selected.

## Development

### Adding New Endpoints

1. Create a new file in `src/listeners/` or relevant directory
2. Implement the `Endpoint` interface from `listener.ts`
3. Register the endpoint in `src/index.ts`

Example:
```typescript
import { Endpoint } from "./listener.ts";
import type { Application } from "express";

export class MyEndpoint implements Endpoint {
  register(app: Application): void {
    app.get("/api/my-route", (req, res) => {
      res.json({ message: "Hello!" });
    });
  }
}
```

### TypeScript Configuration

The `tsconfig.json` is configured for ES modules with strict type checking. The `tsx` package handles TypeScript compilation and execution in development.

## Troubleshooting

### Port Already in Use

If port 3000 is already in use, change the `PORT` value in your `.env` file.

### Better Auth Errors

Better Auth requires a database connection. Until the database is configured, authentication endpoints may not work as expected. You'll see errors related to missing database configuration.

### CORS Errors

If the frontend can't connect to the backend:
1. Verify the backend is running on port 3000
2. Check that CORS origin matches your frontend URL
3. Ensure `credentials: true` is set in both CORS config and fetch requests

### TypeScript Errors

Run `npm install` to ensure all TypeScript types are installed:
```bash
npm install
```

## Next Steps

- [ ] Configure database for Better Auth
- [ ] Implement blog post CRUD operations
- [ ] Add user profile endpoints
- [ ] Implement authorization middleware
- [ ] Add input validation
- [ ] Set up error handling middleware
- [ ] Add logging
- [ ] Write API tests
