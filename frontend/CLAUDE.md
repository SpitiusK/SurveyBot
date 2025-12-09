# SurveyBot Frontend - React Admin Panel Documentation

[← Back to Main Documentation](../CLAUDE.md)

**Layer**: Presentation Layer (React SPA)
**Framework**: React 19.2.0 + TypeScript 5.9.3 + Vite 7.2.2
**UI Library**: Material-UI 6.5.0
**Last Updated**: 2025-11-28

---

## Overview

The SurveyBot Frontend is a React-based Single Page Application (SPA) providing a comprehensive web interface for survey management. It communicates with the SurveyBot.API backend via REST API and serves as an alternative to the Telegram bot interface.

### Purpose
- Web-based survey creation and management
- Response analytics and visualization
- Dashboard with overview metrics
- Multi-step survey builder with drag-and-drop
- Multimedia upload and management for questions (NEW in v1.3.0)

---

## Recent Changes & Bug Fixes

### v1.5.1 - Survey Publish Validation Fix (2025-11-28)

**Issue**: SingleChoice questions with "End Survey" option couldn't be published because the empty string value (`''`) was stored instead of `null`, causing the backend validation to fail with error: "Survey must have at least one question that leads to completion".

**Root Cause**:
- Material-UI `Select` component with React Hook Form was incorrectly handling empty string to null conversion
- Previous implementation used `value={field.value || ''}` which converted both `null` AND `''` to `''`
- Previous implementation used `onChange={(e) => field.onChange(e.target.value || null)}` which didn't properly convert empty strings in nested Record fields

**Fix Applied** in `QuestionEditor.tsx`:

Changed at **3 locations** (lines ~608, ~644, ~696):

```typescript
// BEFORE (Incorrect)
<Select
  value={field.value || ''}
  onChange={(e) => field.onChange(e.target.value || null)}
>

// AFTER (Correct)
<Select
  value={field.value ?? ''}
  onChange={(e) => field.onChange(e.target.value === '' ? null : e.target.value)}
>
```

**Why This Works**:
- **Nullish coalescing operator (`??`)**: Only converts `undefined`/`null` to `''`, preserving the distinction between null and empty string
- **Explicit empty string check (`=== ''`)**: Properly converts "End Survey" selection (represented as empty string in the select) to `null` for the form state
- **Backend compatibility**: ReviewStep.tsx validation checks for `null` or `'0'` to recognize "End Survey", matching backend expectations

**Files Affected**:
- `frontend/src/components/SurveyBuilder/QuestionEditor.tsx` - Fixed 3 Select components for nextQuestionId fields

**Validation Flow**:
1. User selects "End Survey" → Select component emits `''`
2. onChange handler converts `'' → null`
3. Form state stores `null` in `nextQuestionId` field
4. ReviewStep validation recognizes `null` as valid "End Survey"
5. Backend receives `null` and correctly identifies completion path

---

### Technology Stack

| Category | Technology | Version | Purpose |
|----------|-----------|---------|---------|
| **Core** | React | 19.2.0 | UI framework |
| | TypeScript | 5.9.3 | Type safety |
| | Vite | 7.2.2 | Build tool & dev server |
| | React Router DOM | 7.9.5 | Client-side routing |
| **UI** | Material-UI | 6.5.0 | Component library |
| | Emotion | 11.14.0 | CSS-in-JS |
| | Tailwind CSS | 4.1.17 | Utility classes |
| **Forms** | React Hook Form | 7.66.0 | Form state management |
| | Yup | 1.7.1 | Validation schemas |
| **Data** | Axios | 1.13.2 | HTTP client |
| | Recharts | 3.4.1 | Charts & visualization |
| | date-fns | 4.1.0 | Date utilities |
| **DnD** | @dnd-kit | 6.3.1 | Drag & drop |

---

## Project Structure

```
frontend/
├── src/
│   ├── components/              # Reusable UI components
│   │   ├── Statistics/          # Statistics visualizations
│   │   │   ├── ChoiceChart.tsx
│   │   │   ├── OverviewMetrics.tsx
│   │   │   ├── QuestionStatistics.tsx
│   │   │   └── ResponsesTable.tsx
│   │   ├── SurveyBuilder/       # Survey creation wizard
│   │   │   ├── BasicInfoStep.tsx
│   │   │   ├── QuestionsStep.tsx
│   │   │   ├── ReviewStep.tsx
│   │   │   ├── QuestionEditor.tsx
│   │   │   └── QuestionCard.tsx
│   │   ├── Breadcrumb.tsx
│   │   ├── ConfirmDialog.tsx
│   │   ├── LoadingSpinner.tsx
│   │   ├── Navigation.tsx
│   │   ├── ProtectedRoute.tsx
│   │   └── Sidebar.tsx
│   ├── context/                 # React Context providers
│   │   └── AuthContext.tsx      # Authentication state
│   ├── hooks/                   # Custom React hooks
│   │   └── useAuth.ts
│   ├── layouts/                 # Layout components
│   │   ├── AppShell.tsx         # Main app wrapper
│   │   ├── AuthLayout.tsx       # Auth pages wrapper
│   │   └── DashboardLayout.tsx
│   ├── pages/                   # Route components
│   │   ├── Dashboard.tsx
│   │   ├── Login.tsx
│   │   ├── SurveyList.tsx
│   │   ├── SurveyBuilder.tsx
│   │   ├── SurveyEdit.tsx
│   │   ├── SurveyStatistics.tsx
│   │   └── NotFound.tsx
│   ├── routes/                  # Router configuration
│   │   └── index.tsx
│   ├── schemas/                 # Yup validation schemas
│   │   ├── authSchemas.ts
│   │   ├── surveySchemas.ts
│   │   └── questionSchemas.ts
│   ├── services/                # API service layer
│   │   ├── api.ts               # Axios instance
│   │   ├── authService.ts
│   │   ├── surveyService.ts
│   │   └── questionService.ts
│   ├── theme/                   # MUI theme
│   │   ├── theme.ts
│   │   └── ThemeProvider.tsx
│   ├── types/                   # TypeScript types
│   │   └── index.ts
│   └── App.tsx
├── .env.development             # Dev environment config
├── .env.example                 # Environment template
├── package.json
├── tsconfig.json
└── vite.config.ts
```

---

## Quick Start

### Prerequisites
- Node.js 18+ and npm 9+
- Backend API running (default: http://localhost:5000)

### Setup Steps

1. **Install dependencies**:
   ```bash
   cd frontend
   npm install
   ```

2. **Configure environment**:
   ```bash
   cp .env.example .env.development
   ```

   Edit `.env.development`:
   ```env
   VITE_API_BASE_URL=http://localhost:5000/api
   VITE_APP_NAME=SurveyBot Admin Panel
   ```

3. **Start dev server**:
   ```bash
   npm run dev
   ```

4. **Access app**: http://localhost:3000

### Available Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start dev server with HMR |
| `npm run build` | Build for production → `dist/` |
| `npm run preview` | Preview production build |
| `npm run lint` | Run ESLint |

---

## API Integration

### Centralized Configuration (`src/config/ngrok.config.ts`)

**Primary configuration file for all API URLs:**

The `ngrok.config.ts` file centralizes all ngrok and API base URL configuration. Update this **single file** when your ngrok URL changes:

```typescript
// Update this constant with your ngrok URL
export const BACKEND_NGROK_URL = 'https://abc123.ngrok-free.app';

// If running frontend on ngrok (optional)
export const FRONTEND_NGROK_URL = '';

// Get API base URL - automatically checks ngrok config first
export const getApiBaseUrl = (): string => { ... }

// Get allowed hosts for Vite dev server
export const getAllowedHosts = (): string[] => { ... }
```

**All references use this file**:
- ✅ `src/services/api.ts` - Uses `getApiBaseUrl()`
- ✅ `vite.config.ts` - Uses `getAllowedHosts()`
- ✅ Environment detection - Automatically fallback to localhost

**Setup Guide**: See [docs/NGROK_SETUP.md](./docs/NGROK_SETUP.md) for detailed instructions on configuring ngrok.

### Axios Client (`src/services/api.ts`)

Centralized HTTP client with interceptors:

**Features**:
- Base URL from `getApiBaseUrl()` (uses ngrok.config.ts)
- Automatic JWT token attachment
- Token refresh on 401 errors
- Request queueing during refresh
- ngrok bypass header for remote access
- Error handling and logging

**Key Configuration**:
```typescript
import { getApiBaseUrl } from '@/config/ngrok.config';

const api = axios.create({
  baseURL: getApiBaseUrl(), // Uses ngrok.config.ts
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'ngrok-skip-browser-warning': 'true', // For ngrok access
  },
});
```

**Request Interceptor**: Adds `Authorization: Bearer <token>` header from localStorage

**Response Interceptor**:
- Handles 401 by refreshing token
- Queues failed requests during refresh
- Redirects to login if refresh fails

### Service Layer Pattern

All API calls abstracted into service classes:

**Example - SurveyService** (`src/services/surveyService.ts`):
```typescript
class SurveyService {
  async getAllSurveys(params?: SurveyFilterParams): Promise<PagedResult<SurveyListItem>> {
    const response = await api.get<ApiResponse<PagedResult<SurveyListItem>>>(
      '/surveys',
      { params }
    );
    return response.data.data!;
  }

  async createSurvey(dto: CreateSurveyDto): Promise<Survey> {
    const response = await api.post<ApiResponse<Survey>>('/surveys', dto);
    return response.data.data!;
  }

  async getSurveyStatistics(id: number): Promise<SurveyStatistics> {
    const response = await api.get<ApiResponse<SurveyStatistics>>(
      `/surveys/${id}/statistics`
    );
    return response.data.data!;
  }
}

export default new SurveyService();
```

**Available Services**:
- **authService**: Login, logout, token refresh, user info
- **surveyService**: CRUD operations, statistics, export CSV
- **questionService**: CRUD, reordering
- **mediaService**: Upload files, delete media, retrieve media metadata (NEW in v1.3.0)

---

## Component Architecture

### Component Hierarchy

**Page Components** → **Layout Components** → **Presentational Components**

### Pattern: Page Components

Pages are route-level components that:
- Fetch data from services
- Manage page-level state
- Handle side effects
- Coordinate child components

**Example** (`pages/SurveyList.tsx`):
```typescript
export default function SurveyList() {
  const [surveys, setSurveys] = useState<SurveyListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 10 });

  useEffect(() => {
    loadSurveys();
  }, [pagination]);

  const loadSurveys = async () => {
    try {
      setLoading(true);
      const data = await surveyService.getAllSurveys(pagination);
      setSurveys(data.items);
    } catch (error) {
      // Handle error
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer title="Surveys">
      {loading ? <LoadingSpinner /> : <SurveyTable surveys={surveys} />}
    </PageContainer>
  );
}
```

### Pattern: Presentational Components

Pure components that:
- Receive data via props
- Emit events via callbacks
- No API calls or side effects
- Reusable across pages

**Example** (`components/SurveyCard.tsx`):
```typescript
interface SurveyCardProps {
  survey: SurveyListItem;
  onEdit: (id: number) => void;
  onDelete: (id: number) => void;
  onToggleStatus: (id: number) => void;
}

export default function SurveyCard({
  survey,
  onEdit,
  onDelete,
  onToggleStatus
}: SurveyCardProps) {
  return (
    <Card>
      <CardContent>
        <Typography variant="h6">{survey.title}</Typography>
        <Chip label={survey.isActive ? 'Active' : 'Inactive'} />
      </CardContent>
      <CardActions>
        <Button onClick={() => onEdit(survey.id)}>Edit</Button>
        <Button onClick={() => onToggleStatus(survey.id)}>
          {survey.isActive ? 'Deactivate' : 'Activate'}
        </Button>
        <Button onClick={() => onDelete(survey.id)} color="error">Delete</Button>
      </CardActions>
    </Card>
  );
}
```

### Reusable Components Library

| Component | Purpose | Props |
|-----------|---------|-------|
| `Breadcrumb` | Navigation breadcrumbs | `items: {label, path}[]` |
| `ConfirmDialog` | Generic confirmation | `open, title, message, onConfirm, onCancel` |
| `DeleteConfirmDialog` | Delete confirmation | `open, entityName, onConfirm, onCancel` |
| `EmptyState` | Empty state placeholder | `title, message, icon, action?` |
| `ErrorAlert` | Error display | `error: string \| Error, onClose?` |
| `LoadingSpinner` | Loading indicator | `size?: 'small' \| 'medium' \| 'large'` |
| `PageContainer` | Page wrapper | `title, children, actions?` |
| `ProtectedRoute` | Auth guard | `children` |

---

## State Management

### 1. Global State: AuthContext

**Location**: `src/context/AuthContext.tsx`

Manages authentication state across the app using React Context API.

**Provided Values**:
```typescript
interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (dto: LoginDto) => Promise<void>;
  logout: () => void;
  refreshAuth: () => Promise<void>;
}
```

**Usage**:
```typescript
import { useAuth } from '@/hooks/useAuth';

function MyComponent() {
  const { user, isAuthenticated, logout } = useAuth();

  return (
    <div>
      {isAuthenticated && <p>Welcome, {user?.firstName}!</p>}
      <Button onClick={logout}>Logout</Button>
    </div>
  );
}
```

**Implementation Pattern**:
- Initializes from localStorage on mount
- Persists token and user to localStorage
- Provides login/logout/refresh methods
- Used by ProtectedRoute for auth checks

### 2. Local Component State

Use `useState` for component-specific state:
```typescript
const [surveys, setSurveys] = useState<Survey[]>([]);
const [loading, setLoading] = useState(false);
const [error, setError] = useState<string | null>(null);
```

### 3. Form State: React Hook Form

All forms use React Hook Form with Yup validation:
```typescript
const { control, handleSubmit, formState: { errors } } = useForm({
  resolver: yupResolver(loginSchema),
  defaultValues: { telegramId: '', password: '' }
});
```

---

## Authentication Flow

### Login Process

1. **User submits login form** (`pages/Login.tsx`)
2. **Form validation** (Yup schema)
3. **API call** via `authService.login(dto)`
4. **Backend validates** credentials, returns JWT token + user
5. **Store in localStorage**: `authToken`, `user` (JSON)
6. **Update AuthContext**: `setUser(authData.user)`
7. **Redirect** to dashboard

### Token Management

**Storage**: localStorage
- `authToken`: JWT token string
- `user`: User object serialized as JSON

**Automatic Attachment**: Request interceptor adds `Authorization: Bearer <token>` header

**Automatic Refresh**: Response interceptor catches 401 errors:
```typescript
// On 401 response
1. Check if already refreshing → queue request
2. Call POST /api/auth/refresh with current token
3. Get new token from response
4. Update localStorage
5. Retry original request with new token
6. If refresh fails → clear auth, redirect to /login
```

**Request Queueing**: Multiple simultaneous 401s are queued and retried after refresh completes

### Protected Routes

**ProtectedRoute Component** (`components/ProtectedRoute.tsx`):
```typescript
function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return <LoadingSpinner />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  return <>{children}</>;
}
```

**Usage in Routes**:
```typescript
<Route element={<ProtectedRoute><DashboardLayout /></ProtectedRoute>}>
  <Route path="/dashboard" element={<Dashboard />} />
  <Route path="/surveys" element={<SurveyList />} />
</Route>
```

---

## Forms & Validation

### Validation Library: Yup + React Hook Form

**Schema Definition** (`schemas/surveySchemas.ts`):
```typescript
import * as yup from 'yup';

export const basicInfoSchema = yup.object({
  title: yup.string()
    .required('Title is required')
    .min(3, 'Title must be at least 3 characters')
    .max(500, 'Title cannot exceed 500 characters'),
  description: yup.string()
    .max(2000, 'Description cannot exceed 2000 characters'),
  allowMultipleResponses: yup.boolean(),
  showResults: yup.boolean(),
});

export type BasicInfoFormData = yup.InferType<typeof basicInfoSchema>;
```

**Form Usage**:
```typescript
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';

function SurveyForm() {
  const { control, handleSubmit, formState: { errors } } = useForm({
    resolver: yupResolver(basicInfoSchema),
    mode: 'onChange', // Validate on change
  });

  const onSubmit = async (data: BasicInfoFormData) => {
    await surveyService.createSurvey(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <Controller
        name="title"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            label="Survey Title"
            error={!!errors.title}
            helperText={errors.title?.message}
            fullWidth
          />
        )}
      />
      <Button type="submit">Create Survey</Button>
    </form>
  );
}
```

**Validation Schemas**:
- `authSchemas.ts`: Login form
- `surveySchemas.ts`: Survey basic info, update
- `questionSchemas.ts`: Question creation/edit with optional media
- `mediaSchemas.ts`: Media upload validation (NEW in v1.3.0)

---

## Styling Approach

### Hybrid Styling Strategy

1. **Primary: MUI `sx` Prop** - Component styling with theme access
2. **Secondary: Tailwind Utilities** - Spacing, layout (preflight disabled)
3. **Minimal: Global CSS** - CSS resets and base styles

### MUI Theme Configuration

**Location**: `src/theme/theme.ts`

**Features**:
- Light and dark mode definitions
- Custom color palette
- Typography scale
- Component style overrides
- Responsive breakpoints

**Usage**:
```typescript
import { useTheme } from '@mui/material/styles';

function MyComponent() {
  const theme = useTheme();

  return (
    <Box
      sx={{
        p: 3, // padding: theme.spacing(3)
        backgroundColor: 'background.paper', // from palette
        borderRadius: 1, // theme.shape.borderRadius
        boxShadow: theme.shadows[2],
        [theme.breakpoints.down('md')]: {
          p: 2, // responsive padding
        },
      }}
    >
      Content
    </Box>
  );
}
```

### Common Patterns

**Responsive Layout**:
```typescript
<Grid container spacing={3}>
  <Grid item xs={12} md={6} lg={4}>
    <SurveyCard />
  </Grid>
</Grid>
```

**Conditional Styling**:
```typescript
<Chip
  label={survey.isActive ? 'Active' : 'Inactive'}
  color={survey.isActive ? 'success' : 'default'}
/>
```

**Hover Effects**:
```typescript
<Card
  sx={{
    '&:hover': {
      boxShadow: 6,
      transform: 'translateY(-2px)',
      transition: 'all 0.2s',
    },
  }}
>
```

---

## Key Features

### Survey Builder (Multi-Step Wizard)

**Location**: `pages/SurveyBuilder.tsx`

**Steps**:
1. **Basic Info** (`BasicInfoStep.tsx`): Title, description, settings
2. **Questions** (`QuestionsStep.tsx`): Add/edit/reorder questions with drag-and-drop
3. **Review** (`ReviewStep.tsx`): Preview and publish

**Features**:
- Draft auto-save to localStorage
- Real-time validation
- Question drag-and-drop reordering (@dnd-kit)
- Multi-question type support (Text, SingleChoice, MultipleChoice, Rating)

**Question Editor** (`QuestionEditor.tsx`):
- Dynamic option management for choice questions
- Required field toggle
- Question type selector
- **Media upload and attachment** (NEW in v1.3.0)
- **Media preview with delete option** (NEW in v1.3.0)
- **Conditional flow for Rating questions** (NEW in v1.7.0)
- Validation with Yup

**Conditional Flow** (Enhanced in v1.7.0):
- **SingleChoice**: Each option can lead to different next question
- **Rating**: Each star rating (1-5) can lead to different next question (NEW)
  - Use cases:
    - Low ratings (1-2 stars) → Feedback question ("What went wrong?")
    - Mid ratings (3-4 stars) → Improvement suggestions ("How can we improve?")
    - High ratings (5 stars) → Thank you message or end survey
  - Star visualization: ⭐ (1 star), ⭐⭐ (2 stars), ..., ⭐⭐⭐⭐⭐ (5 stars)
  - Data structure: `optionNextQuestions` with keys 0-4 (rating value - 1)
  - Frontend displays 5 dropdowns (one per star rating)
- **Non-branching** (Text, MultipleChoice, Number, Date, Location): Single next question for all answers

### Media Upload (NEW in v1.3.0)

**Location**: `components/SurveyBuilder/MediaUpload.tsx`

**Features**:
- Drag-and-drop file upload
- Click to browse file selection
- Auto-detection of file type
- Real-time validation feedback
- File size limits by type
- Preview for images with thumbnail
- Upload progress indicator
- Remove uploaded media

**Supported File Types**:
- Images: jpg, png, gif, webp, bmp, tiff, svg (max 10 MB)
- Videos: mp4, webm, mov, avi, mkv, flv, wmv (max 50 MB)
- Audio: mp3, wav, ogg, m4a, flac, aac (max 20 MB)
- Documents: pdf, doc, docx, xls, xlsx, ppt, pptx, txt, csv (max 25 MB)
- Archives: zip, rar, 7z, tar, gz, bz2 (max 100 MB)

**Usage in Question Editor**:
```typescript
<MediaUpload
  onUploadSuccess={(mediaItem) => {
    // Attach media to question
    setQuestionMedia(mediaItem);
  }}
  onRemove={() => {
    // Remove media from question
    setQuestionMedia(null);
  }}
  currentMedia={questionMedia}
/>
```

**API Integration**:
```typescript
// services/mediaService.ts
class MediaService {
  async uploadMedia(file: File, mediaType?: string): Promise<MediaItemDto> {
    const formData = new FormData();
    formData.append('file', file);
    if (mediaType) formData.append('mediaType', mediaType);

    const response = await api.post<ApiResponse<MediaItemDto>>(
      '/media/upload',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' }
      }
    );
    return response.data.data!;
  }

  async deleteMedia(mediaId: string): Promise<void> {
    await api.delete(`/media/${mediaId}`);
  }
}
```

### Statistics Dashboard

**Location**: `pages/SurveyStatistics.tsx`

**Components**:
- **OverviewMetrics**: Total/completed responses, completion rate, avg time
- **QuestionStatistics**: Per-question breakdown with charts
- **ChoiceChart**: Bar/pie charts for choice questions
- **RatingChart**: Distribution chart for rating questions
- **ResponsesTable**: Individual response details
- **ExportDialog**: CSV export with filters

**Visualization**: Recharts library with custom styling

### Dashboard

**Location**: `pages/Dashboard.tsx`

**Sections**:
- Stats cards: Total surveys, responses, active surveys, completion rate
- Quick actions: Create survey, view all surveys
- Recent surveys table

---

## Environment Configuration

### Environment Files

**`.env.development`** (Development):
```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME=SurveyBot Admin Panel
VITE_APP_VERSION=1.0.0
```

**`.env.production`** (Production):
```env
VITE_API_BASE_URL=https://api.yourdomain.com/api
VITE_APP_NAME=SurveyBot Admin Panel
VITE_APP_VERSION=1.0.0
```

### Accessing Environment Variables

```typescript
const apiUrl = import.meta.env.VITE_API_BASE_URL;
const appName = import.meta.env.VITE_APP_NAME;
```

**IMPORTANT**: All env vars must be prefixed with `VITE_` to be exposed to client

### Remote Access (ngrok)

For accessing from any machine:
```env
VITE_API_BASE_URL=https://abc123.ngrok-free.app/api
```

Axios client automatically includes `ngrok-skip-browser-warning` header.

---

## Build & Deployment

### Production Build

```bash
npm run build
```

**Output**: `dist/` directory with optimized assets

**Process**:
1. TypeScript type checking
2. Vite build optimization
3. Code splitting
4. Asset hashing
5. Source map generation

### Preview Build

```bash
npm run preview
```

Serves production build locally for testing.

### Deployment Targets

**Static Hosting (Netlify, Vercel, Cloudflare Pages)**:
1. Build: `npm run build`
2. Deploy `dist/` folder
3. Configure SPA routing: Redirect all routes to `/index.html`
4. Set environment variables in platform settings

**Example netlify.toml**:
```toml
[build]
  command = "npm run build"
  publish = "dist"

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

---

## Common Patterns

### API Call Pattern

```typescript
const [data, setData] = useState<Survey[]>([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState<string | null>(null);

useEffect(() => {
  loadData();
}, []);

const loadData = async () => {
  try {
    setLoading(true);
    setError(null);
    const result = await surveyService.getAllSurveys();
    setData(result.items);
  } catch (err) {
    setError(err instanceof Error ? err.message : 'An error occurred');
  } finally {
    setLoading(false);
  }
};
```

### Error Handling Pattern

```typescript
try {
  await surveyService.deleteSurvey(id);
  // Success feedback
  enqueueSnackbar('Survey deleted successfully', { variant: 'success' });
  loadSurveys(); // Refresh data
} catch (error) {
  // Error feedback
  const message = error instanceof Error ? error.message : 'Delete failed';
  enqueueSnackbar(message, { variant: 'error' });
}
```

### Conditional Rendering Pattern

```typescript
return (
  <>
    {loading && <LoadingSpinner />}
    {error && <ErrorAlert error={error} onClose={() => setError(null)} />}
    {!loading && !error && data.length === 0 && (
      <EmptyState title="No surveys found" message="Create your first survey" />
    )}
    {!loading && !error && data.length > 0 && (
      <SurveyTable surveys={data} />
    )}
  </>
);
```

---

## Troubleshooting

### CORS Issues

**Problem**: CORS policy blocked request

**Solutions**:
1. Ensure backend CORS is configured for frontend origin
2. Check `VITE_API_BASE_URL` is correct
3. For development, use Vite proxy in `vite.config.ts`:
   ```typescript
   server: {
     proxy: {
       '/api': 'http://localhost:5000'
     }
   }
   ```

### Authentication Errors

**401 Unauthorized**:
- Check token in localStorage: `localStorage.getItem('authToken')`
- Token may be expired (auto-refresh should handle)
- Try logout → login

**Token Refresh Loop**:
- Check refresh endpoint is working: `POST /api/auth/refresh`
- Verify refresh response format matches expected structure
- Clear localStorage and login again

### Build Errors

**TypeScript errors**: `npm run build` shows all type errors
- Check imports and type definitions
- Verify types match API response structure

**Module not found**: Check path aliases in `tsconfig.json`
- Ensure `@/*` alias resolves to `src/*`

### Performance Issues

**Slow rendering**:
- Use React DevTools Profiler
- Check for unnecessary re-renders
- Memoize expensive computations with `useMemo`
- Memoize callbacks with `useCallback`

**Large bundle size**:
- Code split with `React.lazy()` and `Suspense`
- Analyze bundle: `npm run build -- --analyze`

### Runtime Errors

**Cannot read property of undefined**:
- Use optional chaining: `user?.profile?.name`
- Add null checks before accessing nested properties

**Infinite loop**:
- Check `useEffect` dependency arrays
- Ensure state updates don't trigger unnecessary re-renders

---

## Development Tips

### TypeScript Best Practices

1. **Define types for all props**:
   ```typescript
   interface MyComponentProps {
     title: string;
     onSave: (data: FormData) => void;
   }
   ```

2. **Use type inference from schemas**:
   ```typescript
   type FormData = yup.InferType<typeof mySchema>;
   ```

3. **Avoid `any`** - Use `unknown` and type guards instead

### React Best Practices

1. **Extract custom hooks** for reusable logic
2. **Keep components small** - Split large components
3. **Use composition** over prop drilling
4. **Memoize expensive calculations** with `useMemo`
5. **Avoid inline object/array creation** in JSX (causes re-renders)

### Debugging Tools

- **React DevTools**: Inspect component tree, props, state
- **Network Tab**: Monitor API requests, check payloads
- **Console**: `console.log()` for quick debugging
- **Breakpoints**: Use browser debugger with source maps

---

## Related Documentation

### Documentation Hub

For comprehensive project documentation, see the **centralized documentation folder**:

**Main Documentation**:
- [Project Root CLAUDE.md](../CLAUDE.md) - Overall project overview and quick start
- [Documentation Index](../documentation/INDEX.md) - Complete documentation catalog
- [Navigation Guide](../documentation/NAVIGATION.md) - Role-based navigation

**API Documentation** (Backend consumed by Frontend):
- [API Layer CLAUDE.md](../src/SurveyBot.API/CLAUDE.md) - REST API implementation
- [API Quick Reference](../documentation/api/QUICK-REFERENCE.md) - Quick endpoint reference
- [API Reference](../documentation/api/API_REFERENCE.md) - Complete API documentation
- [Authentication Flow](../documentation/auth/AUTHENTICATION_FLOW.md) - JWT authentication

**Related Layer Documentation**:
- [Core Layer](../src/SurveyBot.Core/CLAUDE.md) - DTOs and entities used in API responses
- [Infrastructure Layer](../src/SurveyBot.Infrastructure/CLAUDE.md) - Backend services
- [Bot Layer](../src/SurveyBot.Bot/CLAUDE.md) - Telegram bot (alternative interface)

**User Flow Documentation**:
- [Survey Creation Flow](../documentation/flows/SURVEY_CREATION_FLOW.md) - Creating surveys
- [Survey Taking Flow](../documentation/flows/SURVEY_TAKING_FLOW.md) - Taking surveys

**Development Resources**:
- [Developer Onboarding](../documentation/DEVELOPER_ONBOARDING.md) - Getting started guide
- [Troubleshooting](../documentation/TROUBLESHOOTING.md) - Common issues

**Deployment**:
- [Docker Startup Guide](../documentation/deployment/DOCKER-STARTUP-GUIDE.md) - Docker setup
- [Docker README](../documentation/deployment/DOCKER-README.md) - Production deployment

**Testing**:
- [Test Summary](../documentation/testing/TEST_SUMMARY.md) - Test coverage
- [Manual Testing Checklist](../documentation/testing/MANUAL_TESTING_MEDIA_CHECKLIST.md) - Media testing

### Documentation Maintenance

**When updating Frontend**:
1. Update this CLAUDE.md file with component/architecture changes
2. Update [API Reference](../documentation/api/API_REFERENCE.md) if discovering API issues
3. Update [User Flow Documentation](../documentation/flows/) if user experience changes
4. Update [Main CLAUDE.md](../CLAUDE.md) if frontend features significantly change
5. Update [Documentation Index](../documentation/INDEX.md) if adding significant documentation

**Where to save Frontend-related documentation**:
- Technical implementation details → This file
- Component architecture → This file
- API integration patterns → This file
- User flows → `documentation/flows/`
- Testing procedures → `documentation/testing/`
- Deployment guides → `documentation/deployment/`

**API Integration Updates**:
- When API changes, check [API Layer CLAUDE.md](../src/SurveyBot.API/CLAUDE.md)
- Update service files (`src/services/`) to match new API contracts
- Update TypeScript types (`src/types/`) to match DTOs from Core layer
- Test authentication flow if auth endpoints change

**UI/UX Documentation**:
- Document new components in this file
- Document new routes and navigation patterns
- Keep component prop types documented inline
- Document Material-UI customizations in theme files

---

**End of Frontend Documentation**

**Last Updated**: 2025-11-28 | **Version**: 1.5.1

[← Back to Main Documentation](../CLAUDE.md)
