import React, { useState } from 'react';
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
  Logout,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ConfirmDialog } from './ConfirmDialog';

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
  const { logout } = useAuth();
  const [showLogoutDialog, setShowLogoutDialog] = useState(false);

  const handleNavigate = (path: string) => {
    navigate(path);
    if (onItemClick) {
      onItemClick();
    }
  };

  const handleLogoutClick = () => {
    setShowLogoutDialog(true);
  };

  const handleLogoutConfirm = () => {
    logout();
    setShowLogoutDialog(false);
    navigate('/login');
    if (onItemClick) {
      onItemClick();
    }
  };

  const handleLogoutCancel = () => {
    setShowLogoutDialog(false);
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
    <>
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

        <Divider sx={{ my: 2 }} />

        {/* Logout Button */}
        <List>
          <ListItem disablePadding>
            <ListItemButton
              onClick={handleLogoutClick}
              sx={{
                py: 1.5,
                borderRadius: 1,
                mb: 0.5,
                mx: 1,
                color: 'error.main',
                '&:hover': {
                  backgroundColor: 'error.light',
                  opacity: 0.1,
                },
              }}
            >
              <ListItemIcon
                sx={{
                  minWidth: 40,
                  color: 'error.main',
                }}
              >
                <Logout />
              </ListItemIcon>
              <ListItemText
                primary="Logout"
                primaryTypographyProps={{
                  fontWeight: 500,
                  fontSize: '0.95rem',
                }}
              />
            </ListItemButton>
          </ListItem>
        </List>
      </Box>

      {/* Logout Confirmation Dialog */}
      <ConfirmDialog
        open={showLogoutDialog}
        title="Confirm Logout"
        message="Are you sure you want to log out?"
        confirmLabel="Logout"
        cancelLabel="Cancel"
        onConfirm={handleLogoutConfirm}
        onCancel={handleLogoutCancel}
        severity="warning"
      />
    </>
  );
};

export default Navigation;
