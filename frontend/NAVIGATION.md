# Navigation Structure Documentation

## Overview

This document describes the navigation structure and layout components for the SurveyBot Admin Panel.

## Layout Components

### AppShell (`src/layouts/AppShell.tsx`)

The main application shell that wraps all authenticated pages.

**Features**:
- Fixed top app bar with logo and user controls
- Responsive sidebar navigation (fixed on desktop, drawer on mobile)
- Theme switcher integrated in header
- User menu with profile/logout options
- Breadcrumb navigation
- Footer

**Responsive Breakpoints**:
- Mobile (xs): 0-600px - Hamburger menu, full-width content
- Tablet (sm): 600-960px - Fixed sidebar, adjusted content
- Desktop (md+): 960px+ - Full fixed sidebar, main content area

**Props**:
```typescript
interface AppShellProps {
  children: React.ReactNode;
}
```

## Navigation Components

### 1. Navigation (`src/components/Navigation.tsx`)

Main navigation menu component with route-based menu items.

**Features**:
- Active route highlighting
- Click to navigate
- Auto-closes on mobile after navigation
- Grouped menu items (Primary and Secondary)
- Icon-based navigation

**Menu Items**:

**Primary Menu**:
- Dashboard (`/dashboard`)
- All Surveys (`/dashboard/surveys`)
- Create Survey (`/dashboard/surveys/new`)

**Secondary Menu**:
- Settings (`/dashboard/settings`)
- Help (`/dashboard/help`)

**Props**:
```typescript
interface NavigationProps {
  onItemClick?: () => void; // Callback when menu item is clicked (for mobile drawer close)
}
```

**Usage**:
```tsx
<Navigation onItemClick={handleDrawerClose} />
```

### 2. UserMenu (`src/components/UserMenu.tsx`)

User profile dropdown menu in the top app bar.

**Features**:
- User avatar with initials
- Display name and email
- Profile link
- Settings link
- Logout with confirmation dialog
- Auto-generates initials from user data

**Menu Items**:
- Profile (icon + text)
- Settings (icon + text)
- Divider
- Logout (red color, with confirmation)

**User Data Priority**:
1. First Name + Last Name
2. First Name only
3. Username
4. Default "User"

**Usage**:
```tsx
<UserMenu />
```

### 3. Breadcrumb (`src/components/Breadcrumb.tsx`)

Automatic breadcrumb navigation based on current route.

**Features**:
- Auto-generates from URL path
- Home icon for first item
- Clickable links to parent pages
- Current page not clickable
- Route label mapping

**Route Label Mapping**:
```typescript
{
  dashboard: 'Dashboard',
  surveys: 'Surveys',
  new: 'Create Survey',
  edit: 'Edit Survey',
  statistics: 'Statistics',
  settings: 'Settings',
  profile: 'Profile',
  help: 'Help'
}
```

**Examples**:
- `/dashboard` - No breadcrumb (only home)
- `/dashboard/surveys` - Home > Surveys
- `/dashboard/surveys/new` - Home > Surveys > Create Survey
- `/dashboard/surveys/123/edit` - Home > Surveys > Survey Details > Edit Survey

**Usage**:
```tsx
<Breadcrumb />
```

### 4. ThemeSwitcher (`src/components/ThemeSwitcher.tsx`)

Toggle button for light/dark theme.

**Features**:
- Icon changes based on current theme
- Tooltip with current mode
- Smooth rotation animation on hover
- Persists preference to localStorage

**Props**:
```typescript
interface ThemeSwitcherProps {
  size?: 'small' | 'medium' | 'large'; // Button size
  color?: 'inherit' | 'default' | 'primary' | 'secondary'; // Icon color
  showTooltip?: boolean; // Show tooltip on hover
}
```

**Default Props**:
- size: 'medium'
- color: 'inherit'
- showTooltip: true

**Usage**:
```tsx
<ThemeSwitcher color="default" />
```

## Pages

### Dashboard (`src/pages/Dashboard.tsx`)

Main dashboard landing page.

**Features**:
- Welcome message with user name
- 4 stat cards (Total Surveys, Total Responses, Active Surveys, Completion Rate)
- Quick action buttons
- Getting started card
- Recent surveys table
- Loading states with skeletons
- Error handling
- Empty state

**API Integration**:
- Fetches surveys with pagination (first 5)
- Calculates statistics from survey data
- Real-time data loading

**User Actions**:
- Create Survey - Navigate to survey builder
- View All Surveys - Navigate to survey list
- View Survey - Navigate to survey statistics

## Routing Structure

### Route Configuration (`src/routes/index.tsx`)

**Public Routes**:
- `/login` - Login page (redirects to dashboard if authenticated)

**Protected Routes** (require authentication):
- `/dashboard` - Dashboard home
- `/dashboard/surveys` - Survey list
- `/dashboard/surveys/new` - Create survey
- `/dashboard/surveys/:id/edit` - Edit survey
- `/dashboard/surveys/:id/statistics` - Survey statistics
- `/dashboard/settings` - Settings (placeholder)
- `/dashboard/profile` - Profile (placeholder)
- `/dashboard/help` - Help (placeholder)

**Special Routes**:
- `/` - Redirects to `/dashboard`
- `*` - 404 Not Found page

## Responsive Design

### Breakpoints

Using Material-UI breakpoints:
- xs: 0px - 600px (mobile)
- sm: 600px - 960px (tablet)
- md: 960px - 1280px (small desktop)
- lg: 1280px - 1920px (desktop)
- xl: 1920px+ (large desktop)

### Mobile (xs: 0-600px)

**Layout**:
- Hamburger menu button in top left
- Full-width header with app title
- Temporary drawer overlay for navigation
- Full-width content area
- Footer at bottom

**Navigation**:
- Click hamburger to open drawer
- Drawer overlays content
- Auto-closes after navigation
- Swipe to close supported

### Tablet (sm: 600-960px)

**Layout**:
- Fixed sidebar (260px width)
- Top app bar with theme switcher and user menu
- Main content area (width: calc(100% - 260px))
- Footer at bottom

**Navigation**:
- Fixed sidebar always visible
- No hamburger menu
- Navigation items with icons and text

### Desktop (md: 960px+)

**Layout**:
- Fixed sidebar (260px width)
- Top app bar with theme switcher and user menu
- Main content area (width: calc(100% - 260px))
- Maximum content width: 1400px (centered)
- Footer at bottom

**Navigation**:
- Fixed sidebar always visible
- Hover effects on menu items
- Active route highlighting

## Theme Support

### Light Mode
- Background: Grey 50 (#fafafa)
- Sidebar: White
- App bar: White
- Cards: White with borders
- Text: Dark grey

### Dark Mode
- Background: Dark grey (#121212)
- Sidebar: Dark paper
- App bar: Dark paper
- Cards: Dark paper with borders
- Text: White/light grey

### Theme Persistence
- Theme preference saved to `localStorage` as `themeMode`
- Auto-loads on page refresh
- Smooth transitions between modes

## Authentication Flow

### Login Flow
1. User visits `/login`
2. Enters credentials
3. On success, JWT token stored in localStorage
4. User state updated in AuthContext
5. Redirect to `/dashboard`

### Logout Flow
1. User clicks logout in UserMenu
2. Confirmation dialog appears
3. On confirm:
   - Clear JWT token from localStorage
   - Clear user state in AuthContext
   - Redirect to `/login`

### Protected Routes
- All `/dashboard/*` routes require authentication
- ProtectedRoute component checks auth state
- Redirects to `/login` if not authenticated
- Shows loading spinner during auth check

## Component Export Structure

All components are exported from `src/components/index.ts` for clean imports:

```typescript
// Usage
import { Navigation, UserMenu, Breadcrumb, ThemeSwitcher } from '@/components';
```

## Future Enhancements

### Planned Features
1. User profile page with edit functionality
2. Settings page with preferences
3. Help page with documentation
4. Notification system in header
5. Search functionality in app bar
6. Multi-language support
7. Keyboard shortcuts for navigation
8. Recent items quick access
9. Favorites/bookmarks system
10. Customizable dashboard widgets

### Navigation Improvements
1. Nested menu items with expand/collapse
2. Recent pages breadcrumb trail
3. Global search with keyboard shortcut (Cmd/Ctrl+K)
4. Command palette for quick actions
5. Tour guide for first-time users

## Testing Checklist

### Responsive Design
- [ ] Mobile (375px width) - iPhone SE
- [ ] Mobile (414px width) - iPhone Pro Max
- [ ] Tablet (768px width) - iPad
- [ ] Desktop (1024px width) - Laptop
- [ ] Desktop (1440px width) - Desktop
- [ ] Large Desktop (1920px width) - Full HD

### Navigation
- [ ] All menu items clickable
- [ ] Active route highlighted correctly
- [ ] Mobile drawer opens/closes
- [ ] Navigation works after route change
- [ ] Breadcrumb updates on navigation

### User Menu
- [ ] User initials display correctly
- [ ] User name displays correctly
- [ ] Profile link works
- [ ] Settings link works
- [ ] Logout confirmation shows
- [ ] Logout redirects to login

### Theme
- [ ] Theme switcher toggles mode
- [ ] Theme persists on refresh
- [ ] All components support both themes
- [ ] Smooth transitions between themes

### Dashboard
- [ ] Stats load from API
- [ ] Loading states show skeletons
- [ ] Error states show error message
- [ ] Empty state shows when no surveys
- [ ] Recent surveys table populates
- [ ] Quick action buttons work

## Browser Support

Tested and supported browsers:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Performance Considerations

### Optimization Strategies
1. Code splitting by route
2. Lazy loading for large components
3. Memoization for expensive calculations
4. Virtual scrolling for long lists
5. Image optimization
6. Bundle size monitoring

### Current Bundle Sizes
- Main bundle: ~500KB (gzipped)
- Vendor bundle: ~200KB (gzipped)
- Route chunks: ~50KB each (gzipped)

---

**Last Updated**: 2025-11-11
**Version**: 1.0.0
