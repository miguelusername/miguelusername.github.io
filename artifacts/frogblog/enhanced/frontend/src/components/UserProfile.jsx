import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { apiFetch } from '../api';
import './UserProfile.css';

const UserProfile = () => {
  const navigate = useNavigate();
  const { userId } = useParams();
  const [user, setUser] = useState(null);
  const [posts, setPosts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [isOwnProfile, setIsOwnProfile] = useState(false);

  useEffect(() => {
    const fetchUserData = async () => {
      try {
        // Get current logged-in user from localStorage
        const currentUser = JSON.parse(localStorage.getItem('currentUser') || '{}');
        setIsOwnProfile(currentUser.id === userId);

        // Fetch posts by this user
        const { posts: postsData } = await apiFetch(`/posts?author=${encodeURIComponent(userId)}`);

        // Transform backend data to match frontend format
        const transformedPosts = postsData.map(post => ({
          id: post._id,
          title: post.title || 'Untitled Post',
          content: post.content,
          author: post.authorName,
          date: post.createdAt,
          likes: post.likes || 0
        }));

        // Mock user data - TODO: fetch from backend /user/:id endpoint
        const mockUser = {
          id: userId,
          name: transformedPosts[0]?.author || currentUser.username || 'User',
          email: 'user@frogblog.com',
          bio: 'Frog Blog enthusiast 🐸',
          profilePic: '🐸',
          banner: 'https://pixnio.com/free-images/2017/10/26/2017-10-26-09-22-44-900x600.jpg',
          joinDate: '2025-12-01',
          postCount: transformedPosts.length,
          followerCount: 42
        };

        setUser(mockUser);
        setPosts(transformedPosts);
      } catch (err) {
        console.error('Error fetching user data:', err);
        setPosts([]);
      } finally {
        setLoading(false);
      }
    };

    fetchUserData();
  }, [userId]);

  const handleBack = () => {
    navigate('/home');
  };

  const handleEditProfile = () => {
    // TODO: Navigate to edit profile page when built
    alert('Edit profile feature coming soon!');
  };

  if (loading) {
    return (
      <div className="profile-page">
        <div className="loading">Loading profile... 🐸</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="profile-page">
        <div className="error">User not found</div>
      </div>
    );
  }

  return (
    <div className="profile-page">
      <nav className="profile-nav">
        <button className="btn-back" onClick={handleBack}>
          ← Back to Feed
        </button>
        {isOwnProfile && (
          <button className="btn-edit" onClick={handleEditProfile}>
            Edit Profile
          </button>
        )}
      </nav>

      <div className="profile-container">
        <div className="profile-header">
          <div 
            className="profile-banner"
            style={{ backgroundImage: `url(${user.banner})` }}
          />
          <div className="profile-info">
            <div className="profile-avatar">{user.profilePic}</div>
            <div className="profile-details">
              <h1>{user.name}</h1>
              <p className="profile-email">{user.email}</p>
              <p className="profile-bio">{user.bio}</p>
              <div className="profile-stats">
                <div className="stat">
                  <span className="stat-value">{user.postCount}</span>
                  <span className="stat-label">Posts</span>
                </div>
                <div className="stat">
                  <span className="stat-value">{user.followerCount}</span>
                  <span className="stat-label">Followers</span>
                </div>
                <div className="stat">
                  <span className="stat-value">
                    {new Date(user.joinDate).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })}
                  </span>
                  <span className="stat-label">Joined</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="profile-posts">
          <h2>Blog Posts</h2>
          {posts.length === 0 ? (
            <div className="no-posts">
              <p>No posts yet. Start sharing your stories! 🐸</p>
            </div>
          ) : (
            <div className="posts-grid">
              {posts.map(post => (
                <div key={post.id} className="post-card">
                  <div className="post-header">
                    <div className="post-date">
                      {new Date(post.date).toLocaleDateString('en-US', { 
                        month: 'short', 
                        day: 'numeric',
                        year: 'numeric'
                      })}
                    </div>
                  </div>
                  <h3>{post.title}</h3>
                  <p className="post-preview">{post.content}</p>
                  <div className="post-footer">
                    <div className="post-likes">
                      ❤️ {post.likes} likes
                    </div>
                    <button className="btn-read-more">Read More</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default UserProfile;
