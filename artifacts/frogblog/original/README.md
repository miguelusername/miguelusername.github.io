# Frog Blog 

A blog platform with a swamp-themed interface where users can share their stories and pictures.

## Team Members

- **Miguel** - Frontend Development
- **Melody** - Backend Development  
- **Julian** - Full-stack (Frontend/Backend)

## Project Structure

```
FrogBlog/
├── frontend/          # React + Vite frontend application
├── backend/           # Express + TypeScript backend API
├── DESIGN.md          # Design documentation
├── TESTING.md         # Testing documentation
└── README.md          # This file
```

## Prerequisites

- Node.js v20.18.0 or higher
- npm 10.8.2 or higher
- MongoDB 8.0 or higher (with replica set enabled)
  - Download from: https://www.mongodb.com/try/download/community
  - Install as a Windows Service
  - Replica set configuration required for Better Auth

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/miguelusername/FrogBlog.git
cd FrogBlog
```

### 2. Install Dependencies

Install dependencies for both frontend and backend:

```bash
# Install frontend dependencies
cd frontend
npm install

# Install backend dependencies
cd ../backend
npm install
```

### 3. Set Up MongoDB

**Install MongoDB:**
1. Download MongoDB Community Server from https://www.mongodb.com/try/download/community
2. Install as a Windows Service
3. Choose "Run service as Network Service user"

**Configure Replica Set:**
Open PowerShell as Administrator and run:
```powershell
# Add replica set to MongoDB config
Add-Content -Path "C:\Program Files\MongoDB\Server\8.2\bin\mongod.cfg" -Value "`nreplication:`n  replSetName: rs0"

# Restart MongoDB
Restart-Service MongoDB

# Initialize replica set
mongosh --eval "rs.initiate({_id: 'rs0', members: [{_id: 0, host: 'localhost:27017'}]})"
```

### 4. Set Up Environment Variables

Create a `.env` file in the `backend` directory:

```bash
cd backend
# Create .env file with the following content:
PORT=3000
MONGODB_DATABASE=frogblog
```

### 5. Run the Application

You'll need two terminal windows:

**Terminal 1 - Backend:**
```bash
cd backend
npm run dev
```
Backend will run on `http://localhost:3000`

**Terminal 2 - Frontend:**
```bash
cd frontend
npm run dev
```
Frontend will run on `http://localhost:5173`

### 5. Access the Application

Open your browser and navigate to `http://localhost:5173`

## Features

- **Landing Page** - Swamp-themed welcome page with navigation
- **User Authentication** - Register and login functionality
- **Home Feed** - View blog posts from all users
- **User Profiles** - Personal blogs with profile pictures, banners, and bios
- **Blog Posts** - Create and share stories with the community

## Technology Stack

### Frontend
- React 19.2.0
- Vite 7.2.2
- React Router DOM 7.9.6
- MSW 2.12.2 (for API mocking)

### Backend
- Node.js with TypeScript
- Express 5.1.0
- Better Auth 1.3.34+ (authentication)
- CORS support

## API Endpoints

- `POST /api/auth/sign-up` - User registration
- `POST /api/auth/sign-in` - User login
- `GET /api/blogs` - Get all public blogs (coming soon)
- `GET /api/blog/:id` - Get specific blog details (coming soon)
- `GET /api/user/:id` - Get user information (coming soon)

## Development Status

Completed:
-   Frontend React setup with Vite  
-   Landing page with swamp theme  
-   User authentication UI (Login/Register)  
-   Backend Express + TypeScript setup  
-   Better Auth integration  
-   CORS configuration  
-   Home page feed with mock data 

TODO: 
-   Database integration  
-   Blog CRUD operations  
-   User profile pages  
-   Image upload functionality  

## Contributing

This is a class project for CS465. All team members should:

1. Never commit `node_modules/` to git
2. Update this README when adding new features
3. Document API changes in DESIGN.md
4. Test changes before pushing

## Troubleshooting

### Backend won't start
- Make sure you have a `.env` file in the `backend` directory with `PORT=3000`
- Check that port 3000 isn't already in use
- Run `npm install` in the backend directory

### Frontend shows network errors
- Verify the backend is running on port 3000
- Check browser console for CORS errors
- Ensure `.env.development` in frontend has `VITE_API_URL=http://localhost:3000`

### Dependencies issues
- Delete `node_modules` and `package-lock.json`, then run `npm install` again
- Make sure you're using Node v20.18.0 or higher

## License

This is a student project for educational purposes.

## Credits

- Swamp background image: [PIXNIO](https://pixnio.com/nature-landscapes/creek-stream/swamp-landscape-leaf-tree-wood-wetland-land-nature) (CC0 Public Domain)