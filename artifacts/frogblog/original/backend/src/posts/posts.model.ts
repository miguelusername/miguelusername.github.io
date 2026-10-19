/**
 * Defines the structure and types for a Blog Post object.
 * This is crucial for TypeScript to ensure type safety across the entire application.
 */
export interface IPost {
    id: number; 
    slug: string; 
    title: string; 
    content: string; 
    authorId: number; 
    tags: string[]; 
    createdAt: Date;
    updatedAt: Date;
}