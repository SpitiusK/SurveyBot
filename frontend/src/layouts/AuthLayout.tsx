import React from 'react';
import { Outlet } from 'react-router-dom';

const AuthLayout: React.FC = () => {
  return (
    <div className="auth-layout">
      <div className="auth-container">
        <div className="auth-header">
          <h1>SurveyBot</h1>
          <p>Admin Panel</p>
        </div>
        <div className="auth-content">
          <Outlet />
        </div>
        <div className="auth-footer">
          <p>&copy; 2025 SurveyBot. All rights reserved.</p>
        </div>
      </div>
    </div>
  );
};

export default AuthLayout;
