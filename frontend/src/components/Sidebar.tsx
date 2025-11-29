import React, { useState } from 'react';
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Divider,
  Box,
  useTheme,
  useMediaQuery,
  Typography,
} from '@mui/material';
import {
  Dashboard,
  BarChart,
  Add,
  List as ListIcon,
  Settings,
  Help,
  Logout,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ConfirmDialog } from './ConfirmDialog';

const drawerWidth = 240;

interface MenuItem {
  text: string;
  icon: React.ReactNode;
  path: string;
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

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ open, onClose }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const { logout } = useAuth();
  const [showLogoutDialog, setShowLogoutDialog] = useState(false);

  const handleNavigate = (path: string) => {
    navigate(path);
    if (isMobile) {
      onClose();
    }
  };

  const handleLogoutClick = () => {
    setShowLogoutDialog(true);
  };

  const handleLogoutConfirm = () => {
    logout();
    setShowLogoutDialog(false);
    navigate('/login');
    if (isMobile) {
      onClose();
    }
  };

  const handleLogoutCancel = () => {
    setShowLogoutDialog(false);
  };

  const renderMenuItem = (item: MenuItem) => {
    const isActive =
      location.pathname === item.path ||
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

  const drawerContent = (
    <Box sx={{ overflow: 'auto', height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Toolbar />
      <Divider />

      {/* Main Menu */}
      <List>
        <Box sx={{ px: 2, py: 1.5 }}>
          <Typography variant="overline" color="text.secondary" fontWeight={600}>
            Main Menu
          </Typography>
        </Box>
        {primaryMenuItems.map(renderMenuItem)}
      </List>

      <Divider sx={{ my: 2 }} />

      {/* Other Menu */}
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
      <List sx={{ mt: 'auto' }}>
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
  );

  return (
    <>
      {/* Mobile drawer */}
      <Drawer
        variant="temporary"
        open={open}
        onClose={onClose}
        ModalProps={{
          keepMounted: true, // Better mobile performance
        }}
        sx={{
          display: { xs: 'block', sm: 'none' },
          '& .MuiDrawer-paper': {
            boxSizing: 'border-box',
            width: drawerWidth,
          },
        }}
      >
        {drawerContent}
      </Drawer>

      {/* Desktop drawer */}
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: 'none', sm: 'block' },
          width: drawerWidth,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: drawerWidth,
            boxSizing: 'border-box',
          },
        }}
        open
      >
        {drawerContent}
      </Drawer>

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
