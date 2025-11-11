import React, { useMemo } from 'react';
import { Breadcrumbs, Link, Typography, Box } from '@mui/material';
import { NavigateNext, Home } from '@mui/icons-material';
import { Link as RouterLink, useLocation } from 'react-router-dom';

interface BreadcrumbItem {
  label: string;
  path?: string;
}

// Map route segments to human-readable labels
const routeLabels: Record<string, string> = {
  dashboard: 'Dashboard',
  surveys: 'Surveys',
  new: 'Create Survey',
  edit: 'Edit Survey',
  statistics: 'Statistics',
  settings: 'Settings',
  profile: 'Profile',
  help: 'Help',
};

export const Breadcrumb: React.FC = () => {
  const location = useLocation();

  const breadcrumbs = useMemo(() => {
    const paths = location.pathname.split('/').filter(Boolean);
    const items: BreadcrumbItem[] = [];

    // Always add home as first item
    items.push({ label: 'Home', path: '/dashboard' });

    // Build breadcrumb items from path segments
    let currentPath = '';
    paths.forEach((segment, index) => {
      currentPath += `/${segment}`;

      // Skip dashboard as it's already added as Home
      if (segment === 'dashboard') {
        return;
      }

      // Check if this is an ID (numeric or alphanumeric)
      const isId = /^[0-9a-zA-Z-]+$/.test(segment) && !routeLabels[segment];

      if (isId) {
        // For IDs, use the previous segment's label or 'Details'
        const prevSegment = paths[index - 1];
        const label = prevSegment ? `${routeLabels[prevSegment] || prevSegment} Details` : 'Details';
        items.push({ label, path: undefined }); // Don't make IDs clickable
      } else {
        // Use mapped label or capitalize segment
        const label = routeLabels[segment] || segment.charAt(0).toUpperCase() + segment.slice(1);

        // Don't add path for last item (current page)
        const isLastItem = index === paths.length - 1;
        items.push({
          label,
          path: isLastItem ? undefined : currentPath,
        });
      }
    });

    return items;
  }, [location.pathname]);

  // Don't show breadcrumbs if only home
  if (breadcrumbs.length <= 1) {
    return null;
  }

  return (
    <Box sx={{ mb: 3 }}>
      <Breadcrumbs
        separator={<NavigateNext fontSize="small" />}
        aria-label="breadcrumb"
        sx={{
          '& .MuiBreadcrumbs-separator': {
            mx: 1,
          },
        }}
      >
        {breadcrumbs.map((item, index) => {
          const isLast = index === breadcrumbs.length - 1;
          const isHome = index === 0;

          if (isLast || !item.path) {
            return (
              <Typography
                key={item.label}
                color="text.primary"
                fontWeight={600}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  fontSize: '0.9rem',
                }}
              >
                {isHome && <Home sx={{ mr: 0.5, fontSize: '1.1rem' }} />}
                {item.label}
              </Typography>
            );
          }

          return (
            <Link
              key={item.label}
              component={RouterLink}
              to={item.path}
              underline="hover"
              color="inherit"
              sx={{
                display: 'flex',
                alignItems: 'center',
                fontSize: '0.9rem',
                '&:hover': {
                  color: 'primary.main',
                },
              }}
            >
              {isHome && <Home sx={{ mr: 0.5, fontSize: '1.1rem' }} />}
              {item.label}
            </Link>
          );
        })}
      </Breadcrumbs>
    </Box>
  );
};

export default Breadcrumb;
