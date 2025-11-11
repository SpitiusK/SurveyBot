import React from 'react';
import { Alert, AlertTitle, Box } from '@mui/material';

interface ErrorAlertProps {
  error: Error | string | null;
  title?: string;
  onRetry?: () => void;
}

export const ErrorAlert: React.FC<ErrorAlertProps> = ({
  error,
  title = 'Error',
  onRetry,
}) => {
  if (!error) return null;

  const errorMessage = typeof error === 'string' ? error : error.message;

  return (
    <Box sx={{ mb: 2 }}>
      <Alert
        severity="error"
        onClose={onRetry ? onRetry : undefined}
        action={onRetry ? undefined : null}
      >
        <AlertTitle>{title}</AlertTitle>
        {errorMessage}
      </Alert>
    </Box>
  );
};
