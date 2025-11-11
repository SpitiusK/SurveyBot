# SurveyBot Admin Panel - Frontend

React + TypeScript + Vite frontend application for managing SurveyBot surveys.

## Prerequisites

- Node.js 18+ and npm
- Backend API running at http://localhost:5000

## Installation

```bash
npm install
```

## Environment Variables

Copy `.env.example` to `.env.development` and configure:

```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=SurveyBot Admin Panel
VITE_APP_VERSION=1.0.0
```

## Development

Start development server:

```bash
npm run dev
```

The application will open at http://localhost:3000

## Build

Build for production:

```bash
npm run build
```

Preview production build:

```bash
npm run preview
```

## Project Structure

```
frontend/
├── src/
│   ├── components/       # Reusable UI components
│   ├── layouts/          # Layout components (Dashboard, Auth)
│   ├── pages/            # Page components
│   ├── routes/           # React Router configuration
│   ├── services/         # API services (Axios)
│   ├── types/            # TypeScript type definitions
│   ├── App.tsx           # Root component
│   ├── main.tsx          # Entry point
│   └── vite-env.d.ts     # Environment variable types
├── .env.development      # Development environment variables
├── .env.production       # Production environment variables
├── vite.config.ts        # Vite configuration
├── tsconfig.json         # TypeScript configuration
└── package.json
```

## Available Routes

- `/login` - Login page
- `/dashboard` - Dashboard overview
- `/dashboard/surveys` - Survey list
- `/dashboard/surveys/new` - Create new survey
- `/dashboard/surveys/:id/edit` - Edit survey
- `/dashboard/surveys/:id/statistics` - View statistics

## Technology Stack

- **React 19** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **React Router 7** - Client-side routing
- **Axios** - HTTP client
- **ESLint** - Code linting

## API Integration

All API calls are configured to use the backend API at `http://localhost:5000/api`.

The Axios client automatically:
- Adds JWT token to requests
- Handles authentication errors
- Redirects to login on 401 responses

## Next Steps

1. Install UI component library (Material-UI or Ant Design)
2. Implement page components
3. Add form validation
4. Implement survey builder
5. Add charts for statistics
