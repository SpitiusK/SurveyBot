# SurveyBot Admin Panel - Frontend Documentation

**Last Updated**: 2025-11-12
**Version**: 1.0.0
**Framework**: React 19.2.0 + TypeScript + Vite 7.2.2
**UI Library**: Material-UI 6.5.0

---

## Table of Contents
- [Project Overview](#project-overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Setup & Installation](#setup--installation)
- [Development Workflow](#development-workflow)
- [API Integration](#api-integration)
- [Core Features](#core-features)
- [Components Architecture](#components-architecture)
- [State Management](#state-management)
- [Authentication](#authentication)
- [Forms & Validation](#forms--validation)
- [Styling](#styling)
- [TypeScript Configuration](#typescript-configuration)
- [Build & Deployment](#build--deployment)
- [Development Tools](#development-tools)
- [Troubleshooting](#troubleshooting)

---

## Project Overview

**SurveyBot Admin Panel** is a React-based web application that provides a comprehensive user interface for managing surveys created via the SurveyBot Telegram bot. The application communicates with the SurveyBot.API backend to enable survey creation, management, response viewing, and analytics.

### Key Capabilities
- **Survey Management**: Create, edit, activate/deactivate, and delete surveys
- **Interactive Survey Builder**: Multi-step wizard with drag-and-drop question reordering
- **Response Analytics**: View responses, statistics, and export to CSV
- **Dashboard**: Overview metrics and recent activity
- **Authentication**: Telegram-based login with JWT tokens
- **Responsive Design**: Works on desktop, tablet, and mobile devices

### Target Users
- Survey creators who need a web interface (alternative to Telegram bot)
- Administrators managing multiple surveys
- Analysts reviewing survey responses and statistics

---

## Technology Stack

### Core Technologies
- **React 19.2.0** - UI library with latest features (use transitions, concurrent rendering)
- **TypeScript 5.9.3** - Type-safe development
- **Vite 7.2.2** - Build tool (fast HMR, optimized builds)
- **React Router DOM 7.9.5** - Client-side routing with data loading

### UI & Styling
- **Material-UI (MUI) 6.5.0** - Component library with theming
- **@emotion/react & @emotion/styled** - CSS-in-JS styling
- **Tailwind CSS 4.1.17** - Utility-first CSS (preflight disabled to avoid MUI conflicts)
- **@mui/icons-material 6.5.0** - Material Design icons

### Forms & Validation
- **React Hook Form 7.66.0** - Form state management with minimal re-renders
- **Yup 1.7.1** - Schema validation
- **@hookform/resolvers 5.2.2** - Yup integration for React Hook Form

### Data & Visualization
- **Axios 1.13.2** - HTTP client with interceptors
- **Recharts 3.4.1** - Chart library for statistics visualization
- **date-fns 4.1.0** - Date formatting and manipulation

### Drag & Drop
- **@dnd-kit/core 6.3.1** - Modern drag-and-drop toolkit
- **@dnd-kit/sortable 10.0.0** - Sortable list implementation
- **@dnd-kit/utilities 3.2.2** - Utility functions

### Development Tools
- **ESLint 9.39.1** - Code linting with TypeScript support
- **TypeScript ESLint 8.46.3** - TypeScript-specific linting rules
- **eslint-plugin-react-hooks 5.2.0** - React Hooks linting
- **autoprefixer 10.4.22** - CSS vendor prefixing

---

## Project Structure

```
frontend/
├── public/                          # Static assets
│   └── vite.svg                     # Favicon
├── src/
│   ├── assets/                      # Images, fonts, etc.
│   ├── components/                  # Reusable UI components
│   │   ├── Statistics/              # Statistics-related components
│   │   ├── SurveyBuilder/           # Survey creation wizard
│   │   ├── Breadcrumb.tsx
│   │   ├── ConfirmDialog.tsx
│   │   ├── Navigation.tsx
│   │   ├── Sidebar.tsx
│   │   └── index.ts
│   ├── context/                     # React Context providers
│   │   └── AuthContext.tsx
│   ├── hooks/                       # Custom React hooks
│   │   └── useAuth.ts
│   ├── layouts/                     # Layout components
│   │   ├── AppShell.tsx
│   │   ├── AuthLayout.tsx
│   │   └── DashboardLayout.tsx
│   ├── pages/                       # Page components (routes)
│   │   ├── Dashboard.tsx
│   │   ├── Login.tsx
│   │   ├── SurveyList.tsx
│   │   ├── SurveyBuilder.tsx
│   │   ├── SurveyStatistics.tsx
│   │   └── NotFound.tsx
│   ├── routes/                      # Router configuration
│   │   └── index.tsx
│   ├── schemas/                     # Yup validation schemas
│   │   ├── authSchemas.ts
│   │   ├── surveySchemas.ts
│   │   └── questionSchemas.ts
│   ├── services/                    # API service layer
│   │   ├── api.ts
│   │   ├── authService.ts
│   │   ├── surveyService.ts
│   │   └── questionService.ts
│   ├── theme/                       # MUI theme configuration
│   │   ├── theme.ts
│   │   ├── ThemeProvider.tsx
│   │   └── globalStyles.tsx
│   ├── types/                       # TypeScript type definitions
│   │   └── index.ts
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── docs/                            # Documentation files
├── .env.development                 # Development environment variables
├── .env.example                     # Environment template
├── .gitignore
├── eslint.config.js
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
└── README.md
```

---

## Setup & Installation

### Prerequisites

- **Node.js**: 18.x or higher (LTS recommended)
- **npm**: 9.x or higher (comes with Node.js)
- **Backend API**: Running at `http://localhost:5000` (or configured URL)

### Installation Steps

1. **Navigate to frontend directory**:
   ```bash
   cd frontend
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Configure environment variables**:
   Copy `.env.example` to `.env.development`:
   ```bash
   cp .env.example .env.development
   ```

   Edit `.env.development`:
   ```env
   VITE_API_BASE_URL=http://localhost:5000/api
   VITE_APP_NAME=SurveyBot Admin Panel
   VITE_APP_VERSION=1.0.0
   ```

4. **Verify backend is running**:
   Ensure the SurveyBot.API backend is running at the configured URL.

5. **Start development server**:
   ```bash
   npm run dev
   ```

6. **Access the application**:
   Open browser to `http://localhost:3000`

---

## Development Workflow

### Starting Development Server

```bash
npm run dev
```

Features: Hot Module Replacement (HMR), auto-reload, port 3000

### Building for Production

```bash
npm run build
```

Process:
1. TypeScript compilation check
2. Vite optimized build
3. Output to `dist/` directory
4. Source maps generated

### Linting

```bash
npm run lint
```

Runs ESLint with TypeScript and React Hooks rules.

---

## API Integration

### API Client Configuration

**Location**: `src/services/api.ts`

Centralized Axios instance with:
- Base URL configuration
- Request/response interceptors
- Automatic JWT token attachment
- Token refresh on 401 errors

### Service Layer

All API calls are abstracted into service classes:

#### AuthService
- `login(dto)` - Login with Telegram credentials
- `logout()` - Clear tokens
- `getCurrentUser()` - Get user from localStorage
- `isAuthenticated()` - Check if user is logged in

#### SurveyService
- `getAllSurveys(params)` - List surveys with pagination
- `getSurveyById(id)` - Get survey details
- `createSurvey(dto)` - Create new survey
- `updateSurvey(id, dto)` - Update survey
- `deleteSurvey(id)` - Delete survey
- `activateSurvey(id)` - Activate survey
- `getSurveyStatistics(id)` - Get statistics
- `exportSurveyResponses(id)` - Export CSV

#### QuestionService
- `createQuestion(surveyId, dto)` - Add question
- `updateQuestion(id, dto)` - Update question
- `deleteQuestion(id)` - Delete question
- `reorderQuestions(surveyId, questionIds)` - Reorder questions

### CORS Configuration

Vite dev server includes proxy for `/api` requests (no CORS issues during development).

---

## Core Features

### Dashboard
Main landing page with:
- Stats cards (total surveys, responses, active surveys, completion rate)
- Quick action buttons
- Recent surveys table

### Survey List
- Search by title
- Filter by status (active/inactive)
- Pagination with configurable page size
- Bulk actions

### Survey Builder (Wizard)
Multi-step wizard with:
1. **Basic Info Step** - Title, description, settings
2. **Questions Step** - Add and reorder questions
3. **Review Step** - Preview and publish

Features:
- Drag-and-drop question reordering
- Auto-save draft to localStorage
- Real-time preview
- Multiple question types support

### Survey Statistics
Comprehensive analytics with:
- Overview metrics (total, completed, completion rate)
- Question-level statistics with charts
- Choice distribution for choice questions
- Rating distribution for rating questions
- Text response list
- Export to CSV

### Authentication
Telegram-based login with:
- Form validation
- Token storage in localStorage
- Automatic token refresh
- Protected routes

---

## Components Architecture

### Component Patterns

**Page Components** (`src/pages/`):
- Top-level route components
- Handle data fetching
- Manage page-level state
- Use service layer for API calls

**Layout Components** (`src/layouts/`):
- DashboardLayout - Main app shell
- AuthLayout - Auth pages wrapper
- AppShell - Sidebar, header, footer

**Presentational Components** (`src/components/`):
- Pure display components
- Receive data via props
- No API calls
- Reusable across pages

### Reusable Components Library

| Component | Purpose |
|-----------|---------|
| `Breadcrumb` | Navigation breadcrumbs |
| `ConfirmDialog` | Generic confirmation dialog |
| `DeleteConfirmDialog` | Delete confirmation |
| `EmptyState` | Empty state placeholder |
| `ErrorAlert` | Error display |
| `LoadingSpinner` | Loading indicator |
| `PageContainer` | Page wrapper with title |
| `Sidebar` | Dashboard sidebar |
| `Navigation` | Navigation menu |
| `UserMenu` | User dropdown menu |
| `ProtectedRoute` | Auth route guard |

---

## State Management

### Global State (Context API)

#### AuthContext
Manages user authentication state:
- `user` - Current user
- `isAuthenticated` - Auth status
- `isLoading` - Auth loading state
- `login(dto)` - Login function
- `logout()` - Logout function

#### ThemeContext
Manages light/dark theme:
- `mode` - Current theme ('light' | 'dark')
- `toggleTheme()` - Toggle function

### Local State (useState)
Component-specific state for form data, loading, errors.

### Form State (React Hook Form)
All forms use React Hook Form with Yup validation.

### Server State
Data fetching with `useEffect` + `useState` pattern.

---

## Authentication

### Authentication Flow

1. User enters Telegram credentials on login page
2. Frontend sends `POST /api/auth/login` request
3. Backend validates and returns JWT token
4. Frontend stores token in localStorage
5. AuthContext updates state
6. User is redirected to dashboard

### Token Management

**Storage**: localStorage
- `authToken` - JWT token
- `user` - User object JSON

**Refresh**: Automatic on 401 response via response interceptor

**Logout**: Clears localStorage and redirects to login

### Protected Routes

Routes check authentication via `ProtectedRoute` component. Unauthenticated users are redirected to login.

---

## Forms & Validation

### Validation Library: Yup

All forms use Yup schema-based validation with React Hook Form integration.

### Validation Schemas

**Location**: `src/schemas/`

Examples:
- `loginSchema` - Login form validation
- `basicInfoSchema` - Survey basic info validation
- `questionSchema` - Question validation

### Form Usage Pattern

```typescript
const {
  control,
  handleSubmit,
  formState: { errors, isValid },
} = useForm({
  resolver: yupResolver(schema),
  mode: 'onChange', // Validate on change
});
```

### Error Display

Material-UI TextField integration with:
- Red border on error
- Error message below field
- Accessible ARIA attributes

---

## Styling

### Styling Approach

Hybrid styling approach:
1. **Material-UI (MUI) Theming** - Primary component styling with `sx` prop
2. **Tailwind CSS Utilities** - Spacing, layout (preflight disabled)
3. **Emotion CSS-in-JS** - Component-scoped styles when needed
4. **Global CSS** - Minimal global styles

### MUI Theme Configuration

**Location**: `src/theme/theme.ts`

Includes:
- Light and dark theme definitions
- Color palettes
- Typography settings
- Component style overrides

### MUI `sx` Prop Pattern

Primary styling method with theme access:

```typescript
<Box
  sx={{
    display: 'flex',
    gap: 2,
    p: 3,
    backgroundColor: 'background.paper',
    '&:hover': { boxShadow: 6 },
  }}
>
  {/* Content */}
</Box>
```

### Responsive Design

Mobile-first approach with breakpoints: xs, sm, md, lg, xl

---

## TypeScript Configuration

### TypeScript Version: 5.9.3

### Key Compiler Options

- `strict: true` - Full type checking
- `jsx: "react-jsx"` - React 17+ JSX transform
- `baseUrl: "."` with path alias `@/*` for imports

### Type Definitions

**Location**: `src/types/index.ts`

Centralized type definitions for:
- API responses
- User, Survey, Question, Response types
- DTOs and enums
- Auth, pagination, and form types

### Type Inference from Schemas

Types are inferred from Yup validation schemas using `yup.InferType<>`.

---

## Build & Deployment

### Build Process

```bash
npm run build
```

Steps:
1. TypeScript compilation check
2. Vite optimized build
3. Outputs to `dist/` directory
4. Source maps generated

### Output Directory

```
dist/
├── assets/
│   ├── index-[hash].js
│   ├── index-[hash].css
│   └── ...
├── index.html
└── vite.svg
```

### Deploy Targets

**Static Hosting** (Netlify, Vercel):
1. Build app: `npm run build`
2. Deploy `dist/` folder
3. Configure SPA routing to redirect all routes to `/index.html`

**Environment Variables**:
Set in hosting platform build settings.

---

## Development Tools

### ESLint

Configuration: `eslint.config.js`

Plugins:
- TypeScript ESLint
- React Hooks
- React Refresh

**Run linting**:
```bash
npm run lint
npm run lint -- --fix
```

### Debugging

**React DevTools**:
- Inspect component tree
- View props and state
- Track re-renders

**Network Tab**:
- Monitor API requests
- Inspect request/response payloads
- Check authentication headers

---

## Troubleshooting

### CORS Issues

**Error**: CORS policy blocked

**Solutions**:
- Ensure Vite proxy is configured for `/api`
- Check backend CORS headers for production
- Use relative paths (`/api/surveys` not `http://localhost:5000/api/surveys`)

### Authentication Issues

**401 Unauthorized**:
1. Check token in localStorage: `localStorage.getItem('authToken')`
2. Token may be expired - refresh should handle automatically
3. Try logout and login again

### Build Errors

**TypeScript errors**: Run `npm run build` to see all type errors

**Module not found**: Check path aliases in `tsconfig.app.json`

### Runtime Errors

**Cannot read property of undefined**: Use optional chaining (`user?.profile?.name`)

**Infinite re-renders**: Check useEffect dependencies array

### Performance Issues

Use code splitting with `lazy()` and `Suspense` for heavy components.

---

**End of Frontend Documentation**
