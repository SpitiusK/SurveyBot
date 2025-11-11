import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
  Alert,
  Typography,
  Box,
} from '@mui/material';
import { Warning } from '@mui/icons-material';

interface DeleteConfirmDialogProps {
  open: boolean;
  surveyTitle: string;
  hasResponses: boolean;
  responseCount: number;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const DeleteConfirmDialog: React.FC<DeleteConfirmDialogProps> = ({
  open,
  surveyTitle,
  hasResponses,
  responseCount,
  isDeleting,
  onConfirm,
  onCancel,
}) => {
  return (
    <Dialog open={open} onClose={onCancel} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Warning color="error" />
          Delete Survey
        </Box>
      </DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          Are you sure you want to delete the survey <strong>"{surveyTitle}"</strong>?
        </DialogContentText>

        {hasResponses && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            This survey has <strong>{responseCount}</strong> response
            {responseCount !== 1 ? 's' : ''}. Deleting this survey will permanently remove all
            associated responses and cannot be undone.
          </Alert>
        )}

        <Typography variant="body2" color="text.secondary">
          This action cannot be undone.
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={isDeleting}>
          Cancel
        </Button>
        <Button
          onClick={onConfirm}
          variant="contained"
          color="error"
          disabled={isDeleting}
          autoFocus
        >
          {isDeleting ? 'Deleting...' : 'Delete Survey'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
