import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './CreatePost.css';

const CreatePost = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    title: '',
    content: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!formData.title.trim() || !formData.content.trim()) {
      setError('Title and content are required');
      return;
    }

    setLoading(true);

    try {
      const response = await fetch('http://localhost:3000/post', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          title: formData.title,
          content: formData.content,
        }),
        credentials: 'include',
      });

      if (!response.ok) {
        setError('Failed to create post. Please make sure you are logged in.');
        setLoading(false);
        return;
      }

      const data = await response.json();
      console.log('Post created successfully:', data);
      navigate('/home');
    } catch (err) {
      setError('Network error. Please make sure the backend is running.');
      console.error('Create post error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleCancel = () => {
    navigate('/home');
  };

  return (
    <div className="create-post-container">
      <div className="create-post-card">
        <div className="create-post-header">
          <img src="/Frog Blog Logo.png" alt="Frog" className="page-logo" />
          <h2>Create New Blog Post</h2>
        </div>
        
        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="title">Post Title</label>
            <input
              type="text"
              id="title"
              name="title"
              placeholder="Enter your post title..."
              value={formData.title}
              onChange={handleChange}
              maxLength="100"
              required
            />
            <span className="char-count">{formData.title.length}/100</span>
          </div>

          <div className="form-group">
            <label htmlFor="content">Post Content</label>
            <textarea
              id="content"
              name="content"
              placeholder="Share your story..."
              value={formData.content}
              onChange={handleChange}
              rows="12"
              required
            />
          </div>

          <div className="button-group">
            <button 
              type="button" 
              className="btn-cancel" 
              onClick={handleCancel}
              disabled={loading}
            >
              Cancel
            </button>
            <button 
              type="submit" 
              className="btn-submit" 
              disabled={loading}
            >
              {loading ? 'Creating...' : 'Publish Post'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreatePost;
