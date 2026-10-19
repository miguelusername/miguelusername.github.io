import { toNodeHandler } from "better-auth/node";
import express from "express";
import cors from "cors";
import { auth } from "./auth.ts";
import bodyParser from "body-parser";

let app: express.Application

const getApp = () => {
    app = express()
    
    app.use(express.json())
    app.use(bodyParser.urlencoded())
    app.use(cors({
        origin: 'http://localhost:5173',
        credentials: true
    }))
    
    app.all(/^\/api\/auth\/.*/, async (req, res) => {
        return toNodeHandler(auth)(req, res);
    });
}

getApp()

export { app }