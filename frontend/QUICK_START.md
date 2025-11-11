# Quick Start Guide - SurveyBot Frontend

## Installation Complete ✅

All UI libraries and components are installed and ready to use.

---

## Running the Application

```bash
# Development mode
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint code
npm run lint
```

---

## Quick Component Import Guide

### Basic Page Structure

```tsx
import React from 'react';
import { Button, Card, CardContent, Typography } from '@mui/material';
import { AppShell, PageContainer } from '../components';

const MyPage: React.FC = () => {
  return (
    <AppShell>
      <PageContainer title="My Page Title">
        <Card>
          <CardContent>
            <Typography variant="h6">Hello World</Typography>
            <Button variant="contained">Click Me</Button>
          </CardContent>
        </Card>
      </PageContainer>
    </AppShell>
  );
};

export default MyPage;
```

---

## Component Cheat Sheet

### Layout
```tsx
import { AppShell, PageContainer } from '../components';
```

### UI Components
```tsx
import {
  LoadingSpinner,
  EmptyState,
  ErrorAlert,
  ConfirmDialog
} from '../components';
```

### MUI Components (Most Used)
```tsx
import {
  Button,
  TextField,
  Card,
  CardContent,
  Grid,
  Typography,
  Box,
  Paper,
  Alert,
  Dialog,
  Menu,
  MenuItem,
  IconButton,
} from '@mui/material';
```

### MUI Icons
```tsx
import {
  Add,
  Edit,
  Delete,
  Save,
  Close,
  Menu as MenuIcon,
  Dashboard,
  Assignment,
  BarChart,
} from '@mui/icons-material';
```

### Theme Hook
```tsx
import { useThemeMode } from '../theme/ThemeProvider';

function MyComponent() {
  const { mode, toggleTheme } = useThemeMode();
  // mode is 'light' or 'dark'
  // toggleTheme() switches between them
}
```

---

## Common Patterns

### Form with Validation
```tsx
const [formData, setFormData] = useState({ name: '', email: '' });
const [errors, setErrors] = useState({});

<TextField
  label="Name"
  value={formData.name}
  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
  error={!!errors.name}
  helperText={errors.name}
  fullWidth
/>
```

### Loading State
```tsx
const [loading, setLoading] = useState(false);

if (loading) return <LoadingSpinner message="Loading..." />;
```

### Error Handling
```tsx
const [error, setError] = useState<Error | null>(null);

if (error) return <ErrorAlert error={error} onRetry={fetchData} />;
```

### Empty State
```tsx
if (items.length === 0) {
  return (
    <EmptyState
      title="No items found"
      description="Create your first item to get started"
      actionLabel="Create Item"
      onAction={() => navigate('/create')}
    />
  );
}
```

### Confirmation Dialog
```tsx
const [confirmOpen, setConfirmOpen] = useState(false);

<ConfirmDialog
  open={confirmOpen}
  title="Delete Item"
  message="Are you sure you want to delete this item?"
  confirmLabel="Delete"
  cancelLabel="Cancel"
  severity="error"
  onConfirm={handleDelete}
  onCancel={() => setConfirmOpen(false)}
/>
```

---

## Responsive Design

### Using sx prop
```tsx
<Box
  sx={{
    padding: { xs: 2, md: 4 }, // 16px on mobile, 32px on desktop
    display: { xs: 'block', md: 'flex' },
  }}
>
```

### Using Grid
```tsx
<Grid container spacing={3}>
  <Grid item xs={12} sm={6} md={4}>
    {/* Full width on mobile, half on tablet, third on desktop */}
  </Grid>
</Grid>
```

### Using Media Query Hook
```tsx
import { useTheme, useMediaQuery } from '@mui/material';

const theme = useTheme();
const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

{isMobile ? <MobileView /> : <DesktopView />}
```

---

## Styling Options

### 1. MUI sx prop (Recommended)
```tsx
<Box sx={{ p: 2, bgcolor: 'primary.main', color: 'white' }}>
  Content
</Box>
```

### 2. Styled Components
```tsx
import { styled } from '@mui/material/styles';

const StyledCard = styled(Card)(({ theme }) => ({
  padding: theme.spacing(3),
  '&:hover': {
    boxShadow: theme.shadows[6],
  },
}));
```

### 3. Tailwind Classes
```tsx
<div className="flex items-center gap-4 p-4">
  Content
</div>
```

---

## Theme Colors

Use these in sx prop or styled components:

```tsx
sx={{
  color: 'primary.main',      // #1976d2
  bgcolor: 'secondary.main',  // #dc004e
  borderColor: 'success.main', // #2e7d32
}}
```

Available palette colors:
- primary (blue)
- secondary (pink)
- error (red)
- warning (orange)
- info (blue)
- success (green)
- text.primary
- text.secondary
- background.default
- background.paper

---

## Spacing System

MUI uses 8px base unit. Use spacing numbers (not px):

```tsx
sx={{
  p: 2,    // padding: 16px (2 * 8px)
  m: 3,    // margin: 24px (3 * 8px)
  px: 4,   // padding-left & padding-right: 32px
  mt: 1,   // margin-top: 8px
}}
```

---

## Next Steps

1. ✅ UI Components Ready
2. ➡️ Implement Login/Register pages (TASK-050)
3. TODO: Create API service layer
4. TODO: Add form validation
5. TODO: Implement survey builder

---

## Documentation

- **Full Guide**: See `UI_COMPONENTS_GUIDE.md`
- **Task Summary**: See `TASK-049-SUMMARY.md`
- **MUI Docs**: https://mui.com/material-ui/
- **Tailwind Docs**: https://tailwindcss.com/docs

---

## Troubleshooting

**Build fails**: Run `npm install` to ensure all dependencies are installed

**Type errors**: Make sure TypeScript can find @mui/material types

**Styles not applying**: Check that ThemeProvider wraps your app in main.tsx

**Hot reload not working**: Restart dev server with `npm run dev`

---

Ready to start building! 🚀
