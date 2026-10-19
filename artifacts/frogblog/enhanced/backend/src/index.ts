import { app } from "./app.ts";
import { config } from "./config.ts";
import { client } from "./mongo.ts";
import { ensureIndexes } from "./models/index.ts";
import { ensureSchemas } from "./models/schema.ts";
import { errorHandler, notFoundHandler } from "./errors.ts";
import type { Endpoint } from "./listener.ts";
import { LoginEndpoint } from "./listeners/login.ts";
import { RegisterEndpoint } from "./listeners/register.ts";
import { GetProfileEndpoint, PutProfileEndpoint, PostProfileEndpoint } from "./listeners/profile.ts";
import {
    GetPostsEndpoint,
    GetPostEndpoint,
    PostPostEndpoint,
    PatchPostEndpoint,
    DeletePostEndpoint,
} from "./listeners/post.ts";
import { GetCommentsEndpoint, PostCommentEndpoint } from "./listeners/comment.ts";
import { PutReactionEndpoint, DeleteReactionEndpoint } from "./listeners/reaction.ts";

const endpoints: Endpoint[] = [
    new LoginEndpoint(),
    new RegisterEndpoint(),
    new GetProfileEndpoint(),
    new PutProfileEndpoint(),
    new PostProfileEndpoint(),
    new GetPostsEndpoint(),
    new GetPostEndpoint(),
    new PostPostEndpoint(),
    new PatchPostEndpoint(),
    new DeletePostEndpoint(),
    new GetCommentsEndpoint(),
    new PostCommentEndpoint(),
    new PutReactionEndpoint(),
    new DeleteReactionEndpoint(),
];

for (const endpoint of endpoints) {
    endpoint.register(app);
}

// Fallbacks go last so every real route is matched first.
app.use(notFoundHandler);
app.use(errorHandler);

async function main(): Promise<void> {
    await client.connect();
    await ensureSchemas();
    await ensureIndexes();

    app.listen(config.port, () => {
        console.log(`Server running at http://localhost:${config.port}`);
    }).on("error", (error: Error) => {
        console.error("Failed to start server:", error.message);
        process.exit(1);
    });
}

main().catch((error: unknown) => {
    console.error("Startup failed:", error);
    process.exit(1);
});
