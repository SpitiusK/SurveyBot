# Getting Started - SurveyBot Admin Panel Frontend

## Quick Start

1. **Install dependencies**
   ```bash
   npm install
   ```

2. **Start development server**
   ```bash
   npm run dev
   ```

   Server will run at: http://localhost:3000

## Available Scripts

- `npm run dev` - Start development server with hot reload
- `npm run build` - Build for production
- `npm run preview` - Preview production build locally
- `npm run lint` - Run ESLint

## Project Overview

### Core Features Implemented

1. **TypeScript Configuration**
   - Full TypeScript support with strict mode
   - Path aliases configured (`@/*` maps to `src/*`)
   - Type-safe API client

2. **React Router Setup**
   - Client-side routing configured
   - Protected routes (require authentication)
   - Public routes (redirect if authenticated)
   - Route structure:
     - `/login` - Login page
     - `/dashboard` - Main dashboard
     - `/dashboard/surveys` - Survey list
     - `/dashboard/surveys/new` - Create survey
     - `/dashboard/surveys/:id/edit` - Edit survey
     - `/dashboard/surveys/:id/statistics` - View statistics

3. **API Client (Axios)**
   - Configured base URL: http://localhost:5000/api
   - Automatic JWT token handling
   - Global error handling
   - Request/response interceptors
   - 401 redirect to login

4. **Services**
   - `authService` - Authentication (login, logout, token management)
   - `surveyService` - Survey CRUD operations
   - `questionService` - Question management
   - All services are TypeScript-typed

5. **Environment Variables**
   - `.env.development` - Development configuration
   - `.env.production` - Production configuration
   - `.env.example` - Template file

### TypeScript Types

All API types are defined in `src/types/index.ts`:
- Survey, Question, Response, Answer
- DTOs for create/update operations
- API response wrappers
- Pagination types
- Statistics types

### Layouts

- `DashboardLayout` - Main application layout with navigation
- `AuthLayout` - Login page layout

### Pages (Placeholders)

- Dashboard
- Login
- SurveyList
- SurveyBuilder
- SurveyEdit
- SurveyStatistics
- NotFound (404)

## Next Steps

Ready for TASK-049 (UI Library Setup):

1. Install UI component library:
   - Material-UI (recommended): `npm install @mui/material @emotion/react @emotion/styled`
   - OR Ant Design: `npm install antd`

2. Implement page components:
   - Login form with validation
   - Dashboard with statistics cards
   - Survey list with table/cards
   - Survey builder with form
   - Statistics page with charts

3. Add additional dependencies:
   - Form validation: `npm install react-hook-form yup`
   - Charts: `npm install recharts` or use Material-UI Charts
   - Date handling: `npm install date-fns`

## Configuration Files

- `vite.config.ts` - Vite configuration with proxy and aliases
- `tsconfig.json` - TypeScript base configuration
- `tsconfig.app.json` - App TypeScript configuration
- `package.json` - Dependencies and scripts

## Environment Variables

Current variables in `.env.development`:
```
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=SurveyBot Admin Panel
VITE_APP_VERSION=1.0.0
```

Access in code: `import.meta.env.VITE_API_BASE_URL`

## Verification

Build status: SUCCESS
All TypeScript errors: RESOLVED
Dependencies installed: YES
Dev server ready: YES
