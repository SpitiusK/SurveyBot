import React, { useState, useEffect } from 'react';
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
  List,
  ListItem,
  Checkbox,
  FormControlLabel,
} from '@mui/material';
import { WarningAmber } from '@mui/icons-material';

interface UpdateConfirmationDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  surveyTitle: string;
  responseCount: number;
}

export const UpdateConfirmationDialog: React.FC<UpdateConfirmationDialogProps> = ({
  open,
  onClose,
  onConfirm,
  surveyTitle,
  responseCount,
}) => {
  const [confirmed, setConfirmed] = useState(false);

  // Reset confirmation state when dialog closes
  useEffect(() => {
    if (!open) {
      setConfirmed(false);
    }
  }, [open]);

  const handleConfirm = () => {
    if (confirmed) {
      onConfirm();
      setConfirmed(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <WarningAmber color="warning" />
          Update Survey?
        </Box>
      </DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          You are about to update the survey <strong>"{surveyTitle}"</strong>.
        </DialogContentText>

        <Alert severity="warning" sx={{ mb: 2 }}>
          This survey has <strong>{responseCount}</strong> response
          {responseCount !== 1 ? 's' : ''}. Updating this survey will have serious consequences.
        </Alert>

        <Typography variant="subtitle2" color="error" sx={{ mb: 1, fontWeight: 600 }}>
          The following will happen:
        </Typography>

        <List sx={{ pl: 2 }}>
          <ListItem sx={{ display: 'list-item', listStyleType: 'disc', p: 0, mb: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              All existing questions will be <strong>permanently deleted</strong>
            </Typography>
          </ListItem>
          <ListItem sx={{ display: 'list-item', listStyleType: 'disc', p: 0, mb: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              All <strong>{responseCount}</strong> response{responseCount !== 1 ? 's' : ''} will be{' '}
              <strong>permanently deleted</strong>
            </Typography>
          </ListItem>
          <ListItem sx={{ display: 'list-item', listStyleType: 'disc', p: 0, mb: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              All answer data will be <strong>permanently lost</strong>
            </Typography>
          </ListItem>
          <ListItem sx={{ display: 'list-item', listStyleType: 'disc', p: 0, mb: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              Question IDs will change (existing links may break)
            </Typography>
          </ListItem>
        </List>

        <Box
          sx={{
            mt: 3,
            p: 2,
            backgroundColor: 'error.lighter',
            border: '1px solid',
            borderColor: 'error.light',
            borderRadius: 1,
          }}
        >
          <FormControlLabel
            control={
              <Checkbox
                checked={confirmed}
                onChange={(e) => setConfirmed(e.target.checked)}
                color="error"
              />
            }
            label={
              <Typography variant="body2" fontWeight={500}>
                I understand this action cannot be undone
              </Typography>
            }
          />
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} variant="outlined" color="inherit">
          Cancel
        </Button>
        <Button
          onClick={handleConfirm}
          variant="contained"
          color="error"
          disabled={!confirmed}
          autoFocus
        >
          Update & Delete All Data
        </Button>
      </DialogActions>
    </Dialog>
  );
};
