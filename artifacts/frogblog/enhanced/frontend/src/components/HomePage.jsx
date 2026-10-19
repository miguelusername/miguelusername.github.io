import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiFetch } from '../api';
import './HomePage.css';

// Transform backend data to match frontend format
const toPostCard = (post) => ({
  id: post._id,
  title: post.title || 'Untitled Post',
  content: post.content,
  author: post.authorName,
  authorId: post.author,
  date: new Date(post.createdAt).toLocaleDateString(),
  likes: post.likes || 0,
  liked: post.myReaction === 'like',
});

const HomePage = () => {
  const navigate = useNavigate();
  const [posts, setPosts] = useState([]);
  const [nextCursor, setNextCursor] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);

  const fetchPage = async (cursor) => {
    const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
    const data = await apiFetch(`/posts${query}`);
    return { posts: data.posts.map(toPostCard), nextCursor: data.nextCursor };
  };

  useEffect(() => {
    const fetchPosts = async () => {
      try {
        const page = await fetchPage(null);
        setPosts(page.posts);
        setNextCursor(page.nextCursor);
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

  const handleLoadMore = async () => {
    if (!nextCursor || loadingMore) return;
    setLoadingMore(true);
    try {
      const page = await fetchPage(nextCursor);
      setPosts(prev => [...prev, ...page.posts]);
      setNextCursor(page.nextCursor);
    } catch (err) {
      console.error('Error fetching more posts:', err);
    } finally {
      setLoadingMore(false);
    }
  };

  const handleToggleLike = async (post) => {
    try {
      const summary = post.liked
        ? await apiFetch(`/post/${post.id}/reaction`, { method: 'DELETE' })
        : await apiFetch(`/post/${post.id}/reaction`, { method: 'PUT', body: { type: 'like' } });
      setPosts(prev => prev.map(p => (
        p.id === post.id ? { ...p, likes: summary.likes, liked: summary.myReaction === 'like' } : p
      )));
    } catch (err) {
      if (err.status === 401) {
        navigate('/login');
      } else {
        console.error('Error updating reaction:', err);
      }
    }
  };

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
                <button
                  className={`btn-like${post.liked ? ' liked' : ''}`}
                  onClick={() => handleToggleLike(post)}
                >
                  {post.liked ? '❤️' : '🤍'} {post.likes} likes
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

        {nextCursor && (
          <button className="btn-load-more" onClick={handleLoadMore} disabled={loadingMore}>
            {loadingMore ? 'Loading...' : 'Load more posts'}
          </button>
        )}
      </div>
    </div>
  );
};

export default HomePage;
