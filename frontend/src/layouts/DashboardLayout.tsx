import React from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import authService from '@/services/authService';

const DashboardLayout: React.FC = () => {
  const navigate = useNavigate();
  const user = authService.getCurrentUser();

  const handleLogout = () => {
    authService.logout();
    navigate('/login');
  };

  return (
    <div className="dashboard-layout">
      <header className="dashboard-header">
        <div className="header-content">
          <h1 className="logo">SurveyBot Admin</h1>
          <nav className="main-nav">
            <Link to="/dashboard">Dashboard</Link>
            <Link to="/dashboard/surveys">Surveys</Link>
            <Link to="/dashboard/surveys/new">Create Survey</Link>
          </nav>
          <div className="user-menu">
            <span>Welcome, {user?.firstName || user?.username || 'User'}</span>
            <button onClick={handleLogout}>Logout</button>
          </div>
        </div>
      </header>

      <main className="dashboard-main">
        <div className="container">
          <Outlet />
        </div>
      </main>

      <footer className="dashboard-footer">
        <p>&copy; 2025 SurveyBot. All rights reserved.</p>
      </footer>
    </div>
  );
};

export default DashboardLayout;
