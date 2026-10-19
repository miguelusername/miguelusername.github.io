# Frog Blog Design Document

## Project Overview

**FrogBlog** is a blog platform where users can create accounts, write blog posts, and share stories or pictures with the online community. Ribbit.

## Project Motivation

The purpose for developing Frog Blog is so that as individual teammembers we learn how to work an end and work with one another as a team to develop something practical in the real world. In this case, a full-stack project making a blogging site. What's different about our site compared to all the rest on the internet is really just the name and theme. We are re-inventing the wheel with this, but the main purpose behind it is to further develop our skills.

## What we plan to show at the showcase

We plan to show to show our fellow peers and professors at the showcase a completed full-stack project of a blogging website that has the functionalities you would see on any blog site.


### Frontend Development
**Lead: Miguel**
- React components and UI/UX
- Responsive design implementation
- Client-side routing with React Router
- Integration with backend APIs
- Swamp-themed styling and assets

### Backend Development
**Lead: Melody**
- Express server setup and configuration
- RESTful API endpoints
- Database schema and operations
- Authentication system (Better Auth)
- CORS and security configurations

### Full-Stack 
**Lead: Julian**
- Frontend and/or backend tasks as needed
- Database integration
- Testing and quality assurance
- Deployment and infrastructure

## User Experience Design

### User Journey

1. **First-Time Visitor**
   - Lands on swamp-themed landing page
   - Sees featured content preview
   - Options to "Get Started" (register) or "Login"

2. **Registration Flow**
   - User enters name, email, and password
   - Password confirmation validation
   - Account created → redirected to login

3. **Login Flow**
   - User enters email and password
   - Authentication via Better Auth
   - Success → redirected to home feed
   - Failure → error message displayed

4. **Authenticated User**
   - Views home feed with blog posts from all users
   - Can navigate to their profile
   - Can create new blog posts
   - Can view other users' profiles and blogs

### Page Layouts

#### Landing Page
- Hero section with swamp background
- Call-to-action buttons (Get Started, Login)
- Features overview grid
- Smooth scrolling navigation

#### Home Feed
- Sticky navigation bar with logout
- Grid/list of blog post cards showing:
  - Author name and avatar
  - Post title and preview text
  - Timestamp
  - Like/comment/share buttons (placeholders)

#### Blog Profile Page (Future)
- Profile banner image
- Profile picture
- User bio
- List of user's blog posts

#### Individual Blog Post Page (Future)
- Full post content
- Author information
- Comments section
- Like/share functionality

### Design Principles

- **Theme Consistency**: Swamp/frog theme throughout with green color palette (#4a7c59, #90EE90, #2d5a27)
- **Accessibility**: Clear contrast, readable fonts, proper ARIA labels
- **Responsiveness**: Mobile-first design that works on all screen sizes
- **User Feedback**: Loading states, error messages, success confirmations

## Technical Architecture

### Frontend Stack
- **Framework**: React 19.2.0
- **Build Tool**: Vite 7.2.2
- **Routing**: React Router DOM 7.9.6
- **HTTP Client**: Fetch API with credentials
- **Styling**: Plain CSS with component-scoped styles

### Backend Stack
- **Runtime**: Node.js v20.18.0
- **Framework**: Express 5.1.0
- **Language**: TypeScript 5.9.3
- **Authentication**: Better Auth 1.3.34+
- **Middleware**: CORS, JSON body parser

### Database (Planned)
- Better Auth requires database configuration
- Options: PostgreSQL, MySQL, or SQLite
- Schema will include: Users, BlogPosts, Comments (future)

## API Endpoints

### Authentication
- `POST /api/auth/sign-up` - Register new user
  - Body: `{ name, email, password }`
  - Returns: User object or error
  
- `POST /api/auth/sign-in` - Authenticate user
  - Body: `{ email, password }`
  - Returns: Session token or error

### Blogs (Planned)
- `GET /api/blogs` - Get all public blogs
  - Returns: Array of blog post summaries

- `GET /api/blog/:id` - Get specific blog details
  - Returns: Full blog post with author info

- `POST /api/blog` - Create new blog post (authenticated)
  - Body: `{ title, content, images? }`
  - Returns: Created blog post

- `PUT /api/blog/:id` - Update blog post (authenticated, owner only)
  - Body: `{ title?, content?, images? }`
  - Returns: Updated blog post

- `DELETE /api/blog/:id` - Delete blog post (authenticated, owner only)
  - Returns: Success confirmation

### User Management (Planned)
- `GET /api/user/:id` - Get user profile information
  - Returns: User profile with public blog posts

- `PUT /api/user/:id` - Update user profile (authenticated, self only)
  - Body: `{ name?, bio?, profilePic?, banner? }`
  - Returns: Updated user object

## External Dependencies & Services

### Development Tools
- **MSW (Mock Service Worker)** 2.12.2 - For API mocking during development
- **tsx** - TypeScript execution for backend development
- **Vite** - Fast frontend build tool with HMR

### Libraries
- **Better Auth** - Authentication library handling user sessions
- **Express** - Web framework for Node.js
- **CORS** - Cross-Origin Resource Sharing middleware
- **React Router** - Client-side routing

### Assets
- **Background Image**: [Swamp landscape](https://pixnio.com/nature-landscapes/creek-stream/swamp-landscape-leaf-tree-wood-wetland-land-nature) from PIXNIO (CC0 Public Domain)

## Testing Strategy

- Frontend: Component testing with Vitest (planned)
- Backend: API endpoint testing (planned)
- Integration: End-to-end testing with Playwright (planned)
- Manual: User acceptance testing for UX flows

## Known Issues & Future Improvements

- Backend requires database configuration before authentication works
- Home feed currently uses mock data
- No image upload functionality yet
- Profile pages not yet implemented
- Need to implement actual blog CRUD operations


