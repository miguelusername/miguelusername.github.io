import dotenv from "dotenv"
import { MongoClient } from "mongodb"

dotenv.config();
const DB_NAME = process.env.MONGODB_DATABASE || "frogblog"
const DB_USER = process.env.MONGODB_USERNAME || ""
const DB_PASS = process.env.MONGODB_PASSWORD || ""

var DB_AUTH = `${DB_USER}:${DB_PASS}@`
if (DB_USER == "" || DB_PASS == "") {
    DB_AUTH = ""
}

// Local dev connection with replica set, no authentication
const DB_URL = `mongodb://${DB_AUTH}127.0.0.1:27017/${DB_NAME}?replicaSet=rs0`
export const client = new MongoClient(DB_URL);
export const db = client.db();