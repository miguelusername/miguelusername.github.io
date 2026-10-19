import { toNodeHandler } from "better-auth/node";
import express from "express";
import cors from "cors";
import { auth } from "./auth.ts";
import { config } from "./config.ts";

export const app = express();

app.use(cors({
    origin: config.corsOrigin,
    credentials: true,
}));

// Better Auth's own routes must see the raw body, so they're mounted before the JSON parser.
app.all(/^\/api\/auth\/.*/, toNodeHandler(auth));

app.use(express.json());
app.use(express.urlencoded({ extended: false }));
