# React + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.


History:

    *Created vite frontend template
    *Installed npm
    *Installed MSW for API mocking
    *Tested npm run dev
    *Created LandingPage.jsx, LandingPage.css, and updated App.jsx to work with them
    *Installed npm react-router-dom for handling navigation between pages
    *Created Login and Register jsx 
    *Updated App.jsx to work with React Router
    *Updated LandingPage to use proper navigation
    *Created Auth.css for styling the forms
    *Updated Register page to have a back button (I hate having to reload the site)
    *Found a creative commons background of a swamp, applied to landing page and gave proper credit
    *Applied same swamp background styling to Register and Login pages for consistent theme
    *Connected Login component to backend Better Auth /api/auth/sign-in endpoint
    *Connected Register component to backend Better Auth /api/auth/sign-up endpoint
    *Changed Register field from "username" to "name" to match Better Auth requirements
    *Added error handling and loading states to both Login and Register forms
    *Created .env.development file with backend API URL (http://localhost:3000)
    *Added error message styling to Auth.css for displaying authentication errors
    *Frontend readied for backend integration once CORS and database are configured
    *Installed cors in the backend
    *Added CORS configuration to backend/src/app.ts to allow frontend origin
    *Updated Login component to redirect to /home on successful authentication
    *Fixed npm audit vulnerabilities (better-auth and body-parser updated to latest)
    *Created HomePage.jsx and HomePage.css with navbar and mock blog post feed
    *Updated App.jsx to include /home route for authenticated users
    *Fixed input text color in Auth.css (was white on white, changed to #333)
    *Added error message display to Login component (now shows authentication errors to user)
    *Installed tsx in backend for TypeScript development support
    *Updated backend dev script to use tsx watch for auto-reload on file changes
    *Fixed Better Auth route pattern in backend/src/app.ts for Express 5 compatibility (changed to app.use)
    *Moved express.json() middleware before auth routes to ensure request body parsing
    *Created backend/.env file with PORT=3000 to configure server port
    *Resolved merge conflicts in backend package.json and app.ts
    *Backend now includes MongoDB driver and body-parser middleware (added by Melody)
    *Created CreatePost.jsx and CreatePost.css for blog post creation UI
    *Updated App.jsx to include /create-post route
    *Updated HomePage "New Post" button to navigate to create post page
    *Create post form includes title (100 char limit) and content fields
    *Form currently shows success alert since backend endpoint not ready yet
    *Created UserProfile.jsx and UserProfile.css for user profile pages
    *Updated App.jsx to include /profile/:userId route with dynamic userId parameter
    *Updated HomePage to make author names clickable - clicking navigates to that user's profile
    *Added authorId to mock post data for profile navigation
    *Profile page displays user info (name, email, bio, stats) and their blog posts
    *Profile includes banner image, avatar, post count, follower count, and join date
    *Added "Edit Profile" button (visible when viewing own profile)
    *Added CSS hover effects to make author names visually indicate they're clickable


    Note:

    Frontend keeps getting 404s with the backend. Some kind of issue there.
