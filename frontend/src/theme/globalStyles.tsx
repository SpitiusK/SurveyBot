import React from 'react';
import { GlobalStyles as MuiGlobalStyles } from '@mui/material';

export const GlobalStyles: React.FC = () => {
  return (
    <MuiGlobalStyles
      styles={(theme) => ({
        '*': {
          margin: 0,
          padding: 0,
          boxSizing: 'border-box',
        },
        html: {
          WebkitFontSmoothing: 'antialiased',
          MozOsxFontSmoothing: 'grayscale',
          height: '100%',
          width: '100%',
        },
        body: {
          height: '100%',
          width: '100%',
          backgroundColor: theme.palette.background.default,
          color: theme.palette.text.primary,
        },
        '#root': {
          height: '100%',
          width: '100%',
        },
        a: {
          textDecoration: 'none',
          color: 'inherit',
        },
        img: {
          display: 'block',
          maxWidth: '100%',
        },
        '::-webkit-scrollbar': {
          width: '8px',
          height: '8px',
        },
        '::-webkit-scrollbar-track': {
          backgroundColor: theme.palette.mode === 'light' ? '#f1f1f1' : '#2b2b2b',
        },
        '::-webkit-scrollbar-thumb': {
          backgroundColor: theme.palette.mode === 'light' ? '#888' : '#555',
          borderRadius: '4px',
          '&:hover': {
            backgroundColor: theme.palette.mode === 'light' ? '#555' : '#777',
          },
        },
        // Accessibility: Support for inert attribute
        '[inert]': {
          pointerEvents: 'none',
          cursor: 'default',
        },
        '[inert] *': {
          pointerEvents: 'none',
          cursor: 'default',
        },
        // Prevent focus on inert elements
        '[inert]:focus, [inert] *:focus': {
          outline: 'none',
        },
      })}
    />
  );
};
