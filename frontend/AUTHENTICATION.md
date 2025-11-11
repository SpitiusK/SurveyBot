# Authentication Implementation Guide

## Overview
This document describes the authentication system implemented in the SurveyBot admin panel frontend.

## Architecture

### Components
1. **AuthContext** - Manages authentication state globally
2. **Login Page** - User interface for authentication
3. **ProtectedRoute** - Wrapper component for protected pages
4. **AuthService** - API calls for authentication
5. **API Interceptor** - Handles token refresh on 401 errors

## Authentication Flow

### Login Flow
```
1. User enters Telegram ID (and optional username, firstName, lastName)
2. Form validates input using react-hook-form + yup
3. Submit calls authService.login(dto)
4. AuthService calls POST /api/auth/login
5. On success:
   - Store JWT token in localStorage ('authToken')
   - Store user data in localStorage ('user')
   - Update AuthContext state
   - Redirect to dashboard
6. On failure:
   - Display error message
   - Keep user on login page
```

### Protected Route Flow
```
1. User navigates to protected route (e.g., /dashboard)
2. ProtectedRoute component checks isAuthenticated
3. If authenticated:
   - Render the protected page
4. If not authenticated:
   - Save attempted location
   - Redirect to /login
5. After login:
   - Redirect to originally attempted location
```

### Token Refresh Flow
```
1. API request receives 401 Unauthorized
2. API interceptor catches the error
3. If not already refreshing:
   - Call POST /api/auth/refresh with current token
   - On success:
     - Update token in localStorage
     - Retry original request
     - Process any queued requests
   - On failure:
     - Clear tokens
     - Redirect to /login
4. If already refreshing:
   - Queue the request
   - Wait for refresh to complete
   - Then retry with new token
```

### Logout Flow
```
1. User clicks logout
2. Call authService.logout()
3. Clear tokens from localStorage
4. Clear user from AuthContext
5. Redirect to /login
```

## File Structure

```
src/
├── context/
│   └── AuthContext.tsx           # Global auth state management
├── hooks/
│   └── useAuth.ts                # Hook for accessing auth context
├── components/
│   └── ProtectedRoute.tsx        # Protected route wrapper
├── pages/
│   └── Login.tsx                 # Login page UI
├── services/
│   ├── api.ts                    # Axios instance with interceptors
│   └── authService.ts            # Authentication API calls
├── schemas/
│   └── authSchemas.ts            # Form validation schemas
└── types/
    └── index.ts                  # TypeScript types
```

## Key Features

### 1. JWT Token Management
- Tokens stored in localStorage
- Automatically attached to all API requests via interceptor
- Automatic refresh on 401 response
- Token cleared on logout or refresh failure

### 2. Form Validation
- React Hook Form for form state management
- Yup for validation schema
- Real-time validation feedback
- Disabled submit button during loading

### 3. User Experience
- Loading states during async operations
- Clear error messages
- Auto-redirect if already authenticated
- Return to originally attempted page after login
- Responsive design for all screen sizes

### 4. State Persistence
- Auth state persists across page reloads
- Token and user stored in localStorage
- AuthContext initialized from localStorage on mount

## Usage

### Using Authentication in Components

```typescript
import { useAuth } from '@/hooks/useAuth';

function MyComponent() {
  const { user, isAuthenticated, logout } = useAuth();

  if (!isAuthenticated) {
    return <div>Please login</div>;
  }

  return (
    <div>
      <p>Welcome, {user?.firstName}!</p>
      <button onClick={logout}>Logout</button>
    </div>
  );
}
```

### Creating Protected Routes

```typescript
import ProtectedRoute from '@/components/ProtectedRoute';

<Route
  path="/dashboard"
  element={
    <ProtectedRoute>
      <Dashboard />
    </ProtectedRoute>
  }
/>
```

### Making Authenticated API Calls

```typescript
import api from '@/services/api';

// Token is automatically attached by interceptor
const response = await api.get('/surveys');
```

## Security Considerations

### Current Implementation (MVP)
- JWT tokens stored in localStorage
- HTTPS recommended in production
- Token refresh on expiration
- Auto-logout on authentication failure

### Production Recommendations
1. **Token Storage**: Consider httpOnly cookies for enhanced security
2. **HTTPS**: Always use HTTPS in production
3. **CSRF Protection**: Implement CSRF tokens for state-changing operations
4. **Rate Limiting**: Implement login attempt rate limiting
5. **Session Management**: Add session timeout
6. **Token Rotation**: Implement refresh token rotation

## API Endpoints

### POST /api/auth/login
Request:
```json
{
  "telegramId": 123456789,
  "username": "testuser",
  "firstName": "Test",
  "lastName": "User"
}
```

Response:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "user": {
      "id": 1,
      "telegramId": 123456789,
      "username": "testuser",
      "firstName": "Test",
      "lastName": "User",
      "lastLoginAt": "2025-01-01T00:00:00Z",
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-01-01T00:00:00Z"
    },
    "expiresAt": "2025-01-01T01:00:00Z"
  }
}
```

### POST /api/auth/refresh
Request:
```
Authorization: Bearer <current_token>
```

Response:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "user": { ... },
    "expiresAt": "2025-01-01T02:00:00Z"
  }
}
```

## Testing

### Manual Testing Checklist
- [ ] Login with valid Telegram ID
- [ ] Login with invalid credentials shows error
- [ ] Form validation works (required fields, min/max length)
- [ ] Loading state shows during login
- [ ] Successful login redirects to dashboard
- [ ] Protected routes redirect to login when not authenticated
- [ ] After login, redirects to originally attempted page
- [ ] Logout clears tokens and redirects to login
- [ ] Page reload maintains authentication state
- [ ] Token refresh works on 401 response
- [ ] Expired token triggers re-authentication
- [ ] Responsive design on mobile/tablet/desktop

## Troubleshooting

### Issue: Login fails with network error
**Solution**: Check that backend API is running on http://localhost:5000

### Issue: Token not persisting across page reloads
**Solution**: Check browser's localStorage in DevTools

### Issue: 401 errors not triggering logout
**Solution**: Check API interceptor is properly configured

### Issue: Form validation not working
**Solution**: Ensure yup schema matches LoginDto type

## Future Enhancements
1. Remember me functionality (longer token expiration)
2. Telegram Widget login integration
3. Two-factor authentication
4. Social login providers
5. Password recovery flow
6. User profile management
7. Session management dashboard
8. Login activity history

## Related Files
- `C:\Users\User\Desktop\SurveyBot\frontend\src\context\AuthContext.tsx`
- `C:\Users\User\Desktop\SurveyBot\frontend\src\pages\Login.tsx`
- `C:\Users\User\Desktop\SurveyBot\frontend\src\components\ProtectedRoute.tsx`
- `C:\Users\User\Desktop\SurveyBot\frontend\src\services\authService.ts`
- `C:\Users\User\Desktop\SurveyBot\frontend\src\services\api.ts`
- `C:\Users\User\Desktop\SurveyBot\frontend\src\schemas\authSchemas.ts`
