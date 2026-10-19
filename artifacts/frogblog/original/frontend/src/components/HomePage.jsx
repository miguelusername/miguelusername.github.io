import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './HomePage.css';

const HomePage = () => {
  const navigate = useNavigate();
  const [posts, setPosts] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPosts = async () => {
      try {
        const response = await fetch('http://localhost:3000/posts', {
          method: 'GET',
          credentials: 'include',
        });

        if (!response.ok) {
          throw new Error('Failed to fetch posts');
        }

        const data = await response.json();
        
        // Transform backend data to match frontend format
        const transformedPosts = data.map(post => ({
          id: post._id,
          title: post.title || 'Untitled Post',
          content: post.content,
          author: post.authorName,
          authorId: post.author,
          date: new Date(post.upload_date).toLocaleDateString(),
          likes: post.likes || 0
        }));

        setPosts(transformedPosts);
      } catch (err) {
        console.error('Error fetching posts:', err);
        // Fallback to empty array if fetch fails
        setPosts([]);
      } finally {
        setLoading(false);
      }
    };

    fetchPosts();
  }, []);

  const handleLogout = () => {
    // TODO: Implement actual logout with backend
    navigate('/');
  };

  const handleCreatePost = () => {
    navigate('/create-post');
  };

  if (loading) {
    return (
      <div className="home-page">
        <div className="loading">Loading your feed... 🐸</div>
      </div>
    );
  }

  return (
    <div className="home-page">
      <nav className="home-nav">
        <div className="nav-brand">
          <img src="/Frog Blog Logo.png" alt="Frog" className="nav-logo" />
          <h1>Frog Blog</h1>
        </div>
        <div className="nav-actions">
          <button className="btn-create" onClick={handleCreatePost}>
            + New Post
          </button>
          <button className="btn-logout" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </nav>

      <div className="home-container">
        <div className="feed-header">
          <h2>Latest Posts</h2>
          <p>Check out what the community is sharing!</p>
        </div>

        <div className="posts-feed">
          {posts.map(post => (
            <div key={post.id} className="post-card">
              <div className="post-header">
                <div className="post-author">
                  <div className="author-avatar">🐸</div>
                  <div className="author-info">
                    <h3 
                      onClick={() => navigate(`/profile/${post.authorId}`)}
                      style={{ cursor: 'pointer' }}
                    >
                      {post.author}
                    </h3>
                    <span className="post-date">{post.date}</span>
                  </div>
                </div>
              </div>
              
              <div className="post-content">
                <h2 className="post-title">{post.title}</h2>
                <p className="post-text">{post.content}</p>
              </div>

              <div className="post-footer">
                <button className="btn-like">
                  ❤️ {post.likes} likes
                </button>
                <button className="btn-comment">
                  💬 Comment
                </button>
                <button className="btn-share">
                  🔗 Share
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default HomePage;
