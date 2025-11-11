# UI Components Guide - SurveyBot Admin Panel

This guide provides comprehensive documentation for the UI component library setup and usage.

## Table of Contents
- [Installed Packages](#installed-packages)
- [Theme Configuration](#theme-configuration)
- [Layout Components](#layout-components)
- [UI Components](#ui-components)
- [Responsive Design](#responsive-design)
- [Usage Examples](#usage-examples)

---

## Installed Packages

### Core UI Libraries
- **@mui/material** ^6.x - Material-UI component library
- **@emotion/react** ^11.x - CSS-in-JS styling for MUI
- **@emotion/styled** ^11.x - Styled components for MUI
- **@mui/icons-material** ^6.x - Material Design icons

### Styling
- **tailwindcss** ^3.x - Utility-first CSS framework
- **postcss** ^8.x - CSS transformation tool
- **autoprefixer** ^10.x - Automatic vendor prefixes

---

## Theme Configuration

### Colors
**Primary**: Blue (#1976d2)
- Light: #42a5f5
- Dark: #1565c0

**Secondary**: Pink (#dc004e)
- Light: #f33371
- Dark: #9a0036

**Status Colors**:
- Success: Green (#2e7d32)
- Error: Red (#d32f2f)
- Warning: Orange (#ed6c02)
- Info: Blue (#0288d1)

### Typography
- **Font Family**: System fonts (-apple-system, BlinkMacSystemFont, Segoe UI, Roboto)
- **Headings**: h1 (2.5rem) to h6 (1rem)
- **Body**: body1 (1rem), body2 (0.875rem)
- **Button**: No text transform, weight 500

### Spacing
- **Base Unit**: 8px
- **Border Radius**: 8px (buttons, inputs), 12px (cards, papers)

### Breakpoints
- **xs**: 0px - Mobile
- **sm**: 600px - Small tablets
- **md**: 960px - Tablets
- **lg**: 1280px - Desktop
- **xl**: 1920px - Large desktop

---

## Layout Components

### AppShell
Main layout wrapper with header, sidebar, and footer.

```tsx
import { AppShell } from '../components';

function MyPage() {
  return (
    <AppShell>
      <YourContent />
    </AppShell>
  );
}
```

**Features**:
- Responsive sidebar (drawer on mobile, permanent on desktop)
- Fixed header with navigation
- Auto-managed footer
- Theme toggle in header
- Account menu

### PageContainer
Content wrapper with title, breadcrumbs, and actions.

```tsx
import { PageContainer } from '../components';

function MyPage() {
  return (
    <AppShell>
      <PageContainer
        title="Page Title"
        breadcrumbs={[
          { label: 'Home', path: '/dashboard' },
          { label: 'Current Page' }
        ]}
        actions={
          <Button variant="contained">Action</Button>
        }
      >
        <YourContent />
      </PageContainer>
    </AppShell>
  );
}
```

### Header
Top navigation bar (used internally by AppShell).

**Features**:
- Menu toggle for mobile
- Theme switcher
- Account menu
- Fixed position

### Sidebar
Navigation sidebar (used internally by AppShell).

**Features**:
- Responsive (temporary drawer on mobile, permanent on desktop)
- Active route highlighting
- Icon + text navigation items
- Auto-close on mobile after navigation

### Footer
Page footer with copyright info.

---

## UI Components

### LoadingSpinner
Display loading state with spinner and optional message.

```tsx
import { LoadingSpinner } from '../components';

<LoadingSpinner message="Loading surveys..." size={40} />
```

### EmptyState
Display when no data is available.

```tsx
import { EmptyState } from '../components';
import { Assignment } from '@mui/icons-material';

<EmptyState
  icon={<Assignment />}
  title="No surveys yet"
  description="Create your first survey to get started"
  actionLabel="Create Survey"
  onAction={() => navigate('/surveys/new')}
/>
```

### ErrorAlert
Display error messages with optional retry.

```tsx
import { ErrorAlert } from '../components';

<ErrorAlert
  error={error}
  title="Failed to load data"
  onRetry={fetchData}
/>
```

### ConfirmDialog
Confirmation dialog for destructive actions.

```tsx
import { ConfirmDialog } from '../components';

const [open, setOpen] = useState(false);

<ConfirmDialog
  open={open}
  title="Delete Survey"
  message="Are you sure you want to delete this survey? This action cannot be undone."
  confirmLabel="Delete"
  cancelLabel="Cancel"
  severity="error"
  onConfirm={handleDelete}
  onCancel={() => setOpen(false)}
/>
```

---

## Responsive Design

### Using MUI Breakpoints

```tsx
import { Box } from '@mui/material';

<Box
  sx={{
    display: { xs: 'block', md: 'flex' }, // block on mobile, flex on desktop
    padding: { xs: 2, sm: 3, md: 4 }, // 16px, 24px, 32px
    fontSize: { xs: '0.875rem', md: '1rem' }
  }}
>
  Content
</Box>
```

### Using useMediaQuery Hook

```tsx
import { useTheme, useMediaQuery } from '@mui/material';

const theme = useTheme();
const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
const isTablet = useMediaQuery(theme.breakpoints.between('sm', 'md'));
const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
```

### Grid System

```tsx
import { Grid, Card } from '@mui/material';

<Grid container spacing={3}>
  <Grid item xs={12} sm={6} md={4}>
    <Card>Card 1</Card>
  </Grid>
  <Grid item xs={12} sm={6} md={4}>
    <Card>Card 2</Card>
  </Grid>
  <Grid item xs={12} sm={6} md={4}>
    <Card>Card 3</Card>
  </Grid>
</Grid>
```

---

## Usage Examples

### Complete Page Example

```tsx
import React, { useState } from 'react';
import {
  Button,
  Card,
  CardContent,
  Grid,
  Typography,
} from '@mui/material';
import { Add } from '@mui/icons-material';
import {
  AppShell,
  PageContainer,
  LoadingSpinner,
  EmptyState,
  ErrorAlert,
} from '../components';

const SurveyList: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [surveys, setSurveys] = useState([]);

  if (loading) return <LoadingSpinner message="Loading surveys..." />;
  if (error) return <ErrorAlert error={error} onRetry={fetchSurveys} />;

  return (
    <AppShell>
      <PageContainer
        title="All Surveys"
        breadcrumbs={[
          { label: 'Dashboard', path: '/dashboard' },
          { label: 'Surveys' }
        ]}
        actions={
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => navigate('/surveys/new')}
          >
            Create Survey
          </Button>
        }
      >
        {surveys.length === 0 ? (
          <EmptyState
            title="No surveys yet"
            description="Create your first survey to get started"
            actionLabel="Create Survey"
            onAction={() => navigate('/surveys/new')}
          />
        ) : (
          <Grid container spacing={3}>
            {surveys.map((survey) => (
              <Grid item xs={12} sm={6} md={4} key={survey.id}>
                <Card>
                  <CardContent>
                    <Typography variant="h6">{survey.title}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {survey.description}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </PageContainer>
    </AppShell>
  );
};

export default SurveyList;
```

### Using Theme Mode Toggle

```tsx
import { useThemeMode } from '../theme/ThemeProvider';

function MyComponent() {
  const { mode, toggleTheme } = useThemeMode();

  return (
    <Button onClick={toggleTheme}>
      Current theme: {mode}
    </Button>
  );
}
```

### Custom Styled Components

```tsx
import { styled } from '@mui/material/styles';
import { Card } from '@mui/material';

const StyledCard = styled(Card)(({ theme }) => ({
  padding: theme.spacing(3),
  borderRadius: theme.shape.borderRadius * 2,
  boxShadow: theme.shadows[3],
  '&:hover': {
    boxShadow: theme.shadows[6],
    transform: 'translateY(-4px)',
    transition: 'all 0.3s ease',
  },
}));
```

### Combining MUI and Tailwind

```tsx
import { Box } from '@mui/material';

// MUI for component structure, Tailwind for utilities
<Box className="flex items-center gap-4">
  <Button variant="contained">MUI Button</Button>
  <div className="text-sm text-gray-600">Tailwind text</div>
</Box>
```

---

## Theme Customization

To customize the theme, edit `src/theme/theme.ts`:

```typescript
// Change primary color
primary: {
  main: '#your-color',
  light: '#lighter-shade',
  dark: '#darker-shade',
}

// Change typography
typography: {
  fontFamily: 'Your Font, sans-serif',
  h1: { fontSize: '3rem' },
}

// Change spacing
spacing: 10, // 10px base unit instead of 8px

// Change border radius
shape: {
  borderRadius: 12,
}
```

---

## Next Steps

1. ✅ UI library and theme setup complete
2. ✅ Base layout components created
3. ✅ Reusable UI components ready
4. NEXT: Implement authentication pages (TASK-050)
5. TODO: Connect to API endpoints
6. TODO: Add form validation
7. TODO: Implement survey builder UI

---

## Component Status

| Component | Status | Location |
|-----------|--------|----------|
| AppShell | ✅ Ready | src/layouts/AppShell.tsx |
| Header | ✅ Ready | src/components/Header.tsx |
| Sidebar | ✅ Ready | src/components/Sidebar.tsx |
| Footer | ✅ Ready | src/components/Footer.tsx |
| PageContainer | ✅ Ready | src/components/PageContainer.tsx |
| LoadingSpinner | ✅ Ready | src/components/LoadingSpinner.tsx |
| EmptyState | ✅ Ready | src/components/EmptyState.tsx |
| ErrorAlert | ✅ Ready | src/components/ErrorAlert.tsx |
| ConfirmDialog | ✅ Ready | src/components/ConfirmDialog.tsx |
| ThemeProvider | ✅ Ready | src/theme/ThemeProvider.tsx |
| Theme Config | ✅ Ready | src/theme/theme.ts |

---

## Troubleshooting

### Issue: MUI styles not loading
**Solution**: Ensure ThemeProvider wraps your app in main.tsx

### Issue: Tailwind not working
**Solution**: Check that index.css includes @tailwind directives and postcss.config.js exists

### Issue: Dark mode not persisting
**Solution**: ThemeProvider saves to localStorage automatically - check browser console for errors

### Issue: Icons not displaying
**Solution**: Ensure @mui/icons-material is installed and imported correctly

---

**End of Guide**
