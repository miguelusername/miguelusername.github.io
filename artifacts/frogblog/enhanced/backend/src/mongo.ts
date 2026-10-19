import { MongoClient } from "mongodb";
import { config } from "./config.ts";

const { host, port, database, replicaSet, username, password } = config.mongo;

// Credentials are optional for local development against an unauthenticated replica set.
const credentials = username && password
    ? `${encodeURIComponent(username)}:${encodeURIComponent(password)}@`
    : "";

const DB_URL = `mongodb://${credentials}${host}:${port}/${database}?replicaSet=${replicaSet}`;

export const client = new MongoClient(DB_URL);
export const db = client.db();
