import { app } from "./app.ts";
import type { Endpoint } from "./listener.ts";
import dotenv from "dotenv";
import { LoginEndpoint } from "./listeners/login.ts";
import { RegisterEndpoint } from "./listeners/register.ts";
import { GetProfileEndpoint, PostProfileEndpoint } from "./listeners/profile.ts";
import { GetPostsEndpoint, PostPostEndpoint } from "./listeners/post.ts";

dotenv.config();
const PORT = process.env.PORT;


const endpoints: Endpoint[] = [
  new LoginEndpoint(),
  new RegisterEndpoint(),
  new GetProfileEndpoint(),
  new PostProfileEndpoint(),
  new GetPostsEndpoint(),
  new PostPostEndpoint()
];

for (let endpoint of endpoints) {
  endpoint.register(app);
}

app.listen(PORT, () => { 
  console.log("Server running at PORT: ", PORT); 
}).on("error", (error: Error) => {
  throw new Error(error.message);
})