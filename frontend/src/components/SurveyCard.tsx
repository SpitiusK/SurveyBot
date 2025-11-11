import React from 'react';
import {
  Card,
  CardContent,
  CardActions,
  Typography,
  Box,
  Chip,
  IconButton,
  Button,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Divider,
} from '@mui/material';
import {
  MoreVert,
  Edit,
  Delete,
  BarChart,
  ContentCopy,
  ToggleOn,
  ToggleOff,
  Schedule,
  CheckCircle,
} from '@mui/icons-material';
import type { SurveyListItem } from '@/types';

interface SurveyCardProps {
  survey: SurveyListItem;
  onEdit: (survey: SurveyListItem) => void;
  onDelete: (survey: SurveyListItem) => void;
  onToggleStatus: (survey: SurveyListItem) => void;
  onViewStats: (survey: SurveyListItem) => void;
  onCopyCode: (code: string) => void;
}

export const SurveyCard: React.FC<SurveyCardProps> = ({
  survey,
  onEdit,
  onDelete,
  onToggleStatus,
  onViewStats,
  onCopyCode,
}) => {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleMenuAction = (action: () => void) => {
    action();
    handleMenuClose();
  };

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <Card
      variant="outlined"
      sx={{
        '&:hover': {
          boxShadow: 2,
        },
      }}
    >
      <CardContent>
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 2 }}>
          <Box sx={{ flex: 1, mr: 1 }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              {survey.title}
            </Typography>
            {survey.description && (
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{
                  display: '-webkit-box',
                  WebkitLineClamp: 2,
                  WebkitBoxOrient: 'vertical',
                  overflow: 'hidden',
                }}
              >
                {survey.description}
              </Typography>
            )}
          </Box>
          <IconButton size="small" onClick={handleMenuOpen}>
            <MoreVert />
          </IconButton>
        </Box>

        {/* Status */}
        <Box sx={{ mb: 2 }}>
          <Chip
            label={survey.isActive ? 'Active' : 'Inactive'}
            color={survey.isActive ? 'success' : 'default'}
            size="small"
          />
        </Box>

        {/* Stats */}
        <Box sx={{ display: 'flex', gap: 3, mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <CheckCircle fontSize="small" color="action" />
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Responses
              </Typography>
              <Typography variant="body2" fontWeight={600}>
                {survey.completedResponses || 0} / {survey.totalResponses || 0}
              </Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Schedule fontSize="small" color="action" />
            <Box>
              <Typography variant="caption" color="text.secondary" display="block">
                Created
              </Typography>
              <Typography variant="body2" fontWeight={600}>
                {formatDate(survey.createdAt)}
              </Typography>
            </Box>
          </Box>
        </Box>

        {/* Survey Code */}
        {survey.code && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
            <Typography variant="caption" color="text.secondary">
              Code:
            </Typography>
            <Typography
              variant="body2"
              sx={{
                fontFamily: 'monospace',
                bgcolor: 'action.hover',
                px: 1,
                py: 0.5,
                borderRadius: 1,
              }}
            >
              {survey.code}
            </Typography>
            <IconButton size="small" onClick={() => onCopyCode(survey.code!)}>
              <ContentCopy fontSize="small" />
            </IconButton>
          </Box>
        )}
      </CardContent>

      <Divider />

      <CardActions sx={{ justifyContent: 'space-between', px: 2 }}>
        <Button size="small" startIcon={<Edit />} onClick={() => onEdit(survey)}>
          Edit
        </Button>
        <Button size="small" startIcon={<BarChart />} onClick={() => onViewStats(survey)}>
          Stats
        </Button>
      </CardActions>

      {/* Actions Menu */}
      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
        <MenuItem onClick={() => handleMenuAction(() => onEdit(survey))}>
          <ListItemIcon>
            <Edit fontSize="small" />
          </ListItemIcon>
          <ListItemText>Edit Survey</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => handleMenuAction(() => onViewStats(survey))}>
          <ListItemIcon>
            <BarChart fontSize="small" />
          </ListItemIcon>
          <ListItemText>View Statistics</ListItemText>
        </MenuItem>
        {survey.code && (
          <MenuItem onClick={() => handleMenuAction(() => onCopyCode(survey.code!))}>
            <ListItemIcon>
              <ContentCopy fontSize="small" />
            </ListItemIcon>
            <ListItemText>Copy Code</ListItemText>
          </MenuItem>
        )}
        <MenuItem onClick={() => handleMenuAction(() => onToggleStatus(survey))}>
          <ListItemIcon>
            {survey.isActive ? <ToggleOff fontSize="small" /> : <ToggleOn fontSize="small" />}
          </ListItemIcon>
          <ListItemText>{survey.isActive ? 'Deactivate' : 'Activate'} Survey</ListItemText>
        </MenuItem>
        <MenuItem
          onClick={() => handleMenuAction(() => onDelete(survey))}
          sx={{ color: 'error.main' }}
        >
          <ListItemIcon>
            <Delete fontSize="small" color="error" />
          </ListItemIcon>
          <ListItemText>Delete Survey</ListItemText>
        </MenuItem>
      </Menu>
    </Card>
  );
};
