import React from 'react';
import { IconButton, Tooltip } from '@mui/material';
import { Brightness4, Brightness7 } from '@mui/icons-material';
import { useThemeMode } from '@/theme/ThemeProvider';

interface ThemeSwitcherProps {
  size?: 'small' | 'medium' | 'large';
  color?: 'inherit' | 'default' | 'primary' | 'secondary';
  showTooltip?: boolean;
}

export const ThemeSwitcher: React.FC<ThemeSwitcherProps> = ({
  size = 'medium',
  color = 'inherit',
  showTooltip = true,
}) => {
  const { mode, toggleTheme } = useThemeMode();

  const button = (
    <IconButton
      onClick={toggleTheme}
      color={color}
      size={size}
      aria-label={`Switch to ${mode === 'light' ? 'dark' : 'light'} mode`}
      sx={{
        transition: 'transform 0.3s ease-in-out',
        '&:hover': {
          transform: 'rotate(20deg)',
        },
      }}
    >
      {mode === 'light' ? (
        <Brightness4 fontSize={size} />
      ) : (
        <Brightness7 fontSize={size} />
      )}
    </IconButton>
  );

  if (showTooltip) {
    return (
      <Tooltip title={`Switch to ${mode === 'light' ? 'dark' : 'light'} mode`}>
        {button}
      </Tooltip>
    );
  }

  return button;
};

export default ThemeSwitcher;
