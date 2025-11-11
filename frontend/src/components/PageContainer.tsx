import React from 'react';
import { Container, Box, Typography, Breadcrumbs, Link } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { NavigateNext } from '@mui/icons-material';

interface BreadcrumbItem {
  label: string;
  path?: string;
}

interface PageContainerProps {
  children: React.ReactNode;
  title?: string;
  breadcrumbs?: BreadcrumbItem[];
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | false;
  actions?: React.ReactNode;
}

export const PageContainer: React.FC<PageContainerProps> = ({
  children,
  title,
  breadcrumbs,
  maxWidth = 'lg',
  actions,
}) => {
  return (
    <Container maxWidth={maxWidth}>
      {(breadcrumbs || title) && (
        <Box sx={{ mb: 3 }}>
          {breadcrumbs && breadcrumbs.length > 0 && (
            <Breadcrumbs
              separator={<NavigateNext fontSize="small" />}
              aria-label="breadcrumb"
              sx={{ mb: 1 }}
            >
              {breadcrumbs.map((item, index) => {
                const isLast = index === breadcrumbs.length - 1;
                return isLast || !item.path ? (
                  <Typography key={index} color="text.primary">
                    {item.label}
                  </Typography>
                ) : (
                  <Link
                    key={index}
                    component={RouterLink}
                    to={item.path}
                    color="inherit"
                    underline="hover"
                  >
                    {item.label}
                  </Link>
                );
              })}
            </Breadcrumbs>
          )}

          {title && (
            <Box
              sx={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 2,
              }}
            >
              <Typography variant="h4" component="h1" fontWeight={600}>
                {title}
              </Typography>
              {actions && <Box>{actions}</Box>}
            </Box>
          )}
        </Box>
      )}
      <Box>{children}</Box>
    </Container>
  );
};
