import dotenv from "dotenv";

dotenv.config({ quiet: true });

/**
 * Every environment-driven setting lives here so the rest of the app never
 * reads process.env directly or hardcodes a "magic" URL/port.
 */
function envInt(name: string, fallback: number): number {
    const raw = process.env[name];
    if (raw === undefined || raw === "") return fallback;
    const parsed = Number.parseInt(raw, 10);
    if (Number.isNaN(parsed)) {
        throw new Error(`Environment variable ${name} must be an integer, got "${raw}"`);
    }
    return parsed;
}

export const config = {
    port: envInt("PORT", 3000),

    /** Origin allowed by CORS (the Vite dev server by default). */
    corsOrigin: process.env.CORS_ORIGIN || "http://localhost:5173",

    mongo: {
        host: process.env.MONGODB_HOST || "127.0.0.1",
        port: envInt("MONGODB_PORT", 27017),
        database: process.env.MONGODB_DATABASE || "frogblog",
        replicaSet: process.env.MONGODB_REPLICA_SET || "rs0",
        username: process.env.MONGODB_USERNAME || "",
        password: process.env.MONGODB_PASSWORD || "",
    },

    pagination: {
        defaultPageSize: envInt("POSTS_PAGE_SIZE", 20),
        maxPageSize: envInt("POSTS_MAX_PAGE_SIZE", 50),
    },
} as const;
