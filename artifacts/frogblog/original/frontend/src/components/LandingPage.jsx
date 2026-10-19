import React from 'react';
import { useNavigate } from 'react-router-dom';
import './LandingPage.css';

const LandingPage = () => {
  const navigate = useNavigate();

  const handleGetStarted = () => {
    navigate('/register');
  };

  const handleLearnMore = () => {
    // Scroll to features section or navigate somewhere
    document.querySelector('.features').scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <div className="landing-page">
      <header className="hero-section">
        <img src="/Frog Blog Logo.png" alt="Frog" className="hero-logo" />
        <h1>Frog Blog</h1>
        <p>Ribbit.</p>
        <div className="cta-buttons">
          <button className="btn-primary" onClick={handleGetStarted}>Get Started</button>
          <button className="btn-secondary" onClick={handleLearnMore}>Learn More</button>
        </div>
      </header>
      
      <section className="features">
        <h2>Why Frog Blog?</h2>
        <div className="feature-grid">
          <div className="feature">
            <h3>📝 Easy Blogging</h3>
            <p>It's so easy, a frog could do it! Probably</p>
          </div>
          <div className="feature">
            <h3>📸 Photo Sharing</h3>
            <p>Got some cool photos of frogs? Share them here! Or any photos for that matter.</p>
          </div>
          <div className="feature">
            <h3>🌍 Public Timeline</h3>
            <p>You're not alone! Look at your fellow froggers blogs!</p>
          </div>
        </div>
      </section>
      
      <footer className="image-credit">
        <p>Background photo from <a href="https://pixnio.com" target="_blank" rel="noopener noreferrer">PIXNIO</a> - CC0 Public Domain</p>
      </footer>
    </div>
  );
};

export default LandingPage;