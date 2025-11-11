import React from 'react';
import {
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  Box,
  Typography,
} from '@mui/material';
import {
  Dashboard,
  Add,
  List as ListIcon,
  Settings,
  Help,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';

export interface MenuItem {
  text: string;
  icon: React.ReactNode;
  path: string;
  divider?: boolean;
}

const primaryMenuItems: MenuItem[] = [
  { text: 'Dashboard', icon: <Dashboard />, path: '/dashboard' },
  { text: 'All Surveys', icon: <ListIcon />, path: '/dashboard/surveys' },
  { text: 'Create Survey', icon: <Add />, path: '/dashboard/surveys/new' },
];

const secondaryMenuItems: MenuItem[] = [
  { text: 'Settings', icon: <Settings />, path: '/dashboard/settings' },
  { text: 'Help', icon: <Help />, path: '/dashboard/help' },
];

interface NavigationProps {
  onItemClick?: () => void;
}

export const Navigation: React.FC<NavigationProps> = ({ onItemClick }) => {
  const navigate = useNavigate();
  const location = useLocation();

  const handleNavigate = (path: string) => {
    navigate(path);
    if (onItemClick) {
      onItemClick();
    }
  };

  const renderMenuItem = (item: MenuItem) => {
    const isActive = location.pathname === item.path ||
                     (item.path === '/dashboard/surveys' && location.pathname.startsWith('/dashboard/surveys'));

    return (
      <ListItem key={item.text} disablePadding>
        <ListItemButton
          selected={isActive}
          onClick={() => handleNavigate(item.path)}
          sx={{
            py: 1.5,
            borderRadius: 1,
            mb: 0.5,
            mx: 1,
            '&.Mui-selected': {
              backgroundColor: 'primary.main',
              color: 'primary.contrastText',
              '&:hover': {
                backgroundColor: 'primary.dark',
              },
              '& .MuiListItemIcon-root': {
                color: 'primary.contrastText',
              },
            },
          }}
        >
          <ListItemIcon
            sx={{
              minWidth: 40,
              color: isActive ? 'inherit' : 'text.secondary',
            }}
          >
            {item.icon}
          </ListItemIcon>
          <ListItemText
            primary={item.text}
            primaryTypographyProps={{
              fontWeight: isActive ? 600 : 400,
              fontSize: '0.95rem',
            }}
          />
        </ListItemButton>
      </ListItem>
    );
  };

  return (
    <Box sx={{ overflow: 'auto', height: '100%' }}>
      <List>
        <Box sx={{ px: 2, py: 1.5 }}>
          <Typography variant="overline" color="text.secondary" fontWeight={600}>
            Main Menu
          </Typography>
        </Box>
        {primaryMenuItems.map(renderMenuItem)}
      </List>

      <Divider sx={{ my: 2 }} />

      <List>
        <Box sx={{ px: 2, py: 1.5 }}>
          <Typography variant="overline" color="text.secondary" fontWeight={600}>
            Other
          </Typography>
        </Box>
        {secondaryMenuItems.map(renderMenuItem)}
      </List>
    </Box>
  );
};

export default Navigation;
