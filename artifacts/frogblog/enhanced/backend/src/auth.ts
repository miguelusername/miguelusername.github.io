import { betterAuth } from "better-auth"
import { mongodbAdapter } from "better-auth/adapters/mongodb"
import { username } from "better-auth/plugins"
import { client, db } from "./mongo.ts"

export const auth = betterAuth({
    emailAndPassword: {
        enabled: true
    },
    plugins: [
        username()
    ],
    database: mongodbAdapter(db, {
        client: client
    })
});