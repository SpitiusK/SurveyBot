import React from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Paper,
  Chip,
  IconButton,
  Tooltip,
  Typography,
  Box,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  MoreVert,
  Edit,
  Delete,
  BarChart,
  ContentCopy,
  ToggleOn,
  ToggleOff,
} from '@mui/icons-material';
import type { SurveyListItem } from '@/types';

interface SurveyTableProps {
  surveys: SurveyListItem[];
  onEdit: (survey: SurveyListItem) => void;
  onDelete: (survey: SurveyListItem) => void;
  onToggleStatus: (survey: SurveyListItem) => void;
  onViewStats: (survey: SurveyListItem) => void;
  onCopyCode: (code: string) => void;
  sortBy: 'createdAt' | 'title' | 'totalResponses';
  sortOrder: 'asc' | 'desc';
  onSort: (column: 'createdAt' | 'title' | 'totalResponses') => void;
}

export const SurveyTable: React.FC<SurveyTableProps> = ({
  surveys,
  onEdit,
  onDelete,
  onToggleStatus,
  onViewStats,
  onCopyCode,
  sortBy,
  sortOrder,
  onSort,
}) => {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const [selectedSurvey, setSelectedSurvey] = React.useState<SurveyListItem | null>(null);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, survey: SurveyListItem) => {
    setAnchorEl(event.currentTarget);
    setSelectedSurvey(survey);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedSurvey(null);
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

  const createSortHandler = (property: 'createdAt' | 'title' | 'totalResponses') => () => {
    onSort(property);
  };

  return (
    <>
      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>
                <TableSortLabel
                  active={sortBy === 'title'}
                  direction={sortBy === 'title' ? sortOrder : 'asc'}
                  onClick={createSortHandler('title')}
                >
                  Title
                </TableSortLabel>
              </TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="center">
                <TableSortLabel
                  active={sortBy === 'totalResponses'}
                  direction={sortBy === 'totalResponses' ? sortOrder : 'desc'}
                  onClick={createSortHandler('totalResponses')}
                >
                  Responses
                </TableSortLabel>
              </TableCell>
              <TableCell>Code</TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortBy === 'createdAt'}
                  direction={sortBy === 'createdAt' ? sortOrder : 'desc'}
                  onClick={createSortHandler('createdAt')}
                >
                  Created
                </TableSortLabel>
              </TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {surveys.map((survey) => (
              <TableRow key={survey.id} hover>
                <TableCell>
                  <Box>
                    <Typography variant="body2" fontWeight={500}>
                      {survey.title}
                    </Typography>
                    {survey.description && (
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{
                          display: '-webkit-box',
                          WebkitLineClamp: 1,
                          WebkitBoxOrient: 'vertical',
                          overflow: 'hidden',
                        }}
                      >
                        {survey.description}
                      </Typography>
                    )}
                  </Box>
                </TableCell>
                <TableCell>
                  <Chip
                    label={survey.isActive ? 'Active' : 'Inactive'}
                    color={survey.isActive ? 'success' : 'default'}
                    size="small"
                  />
                </TableCell>
                <TableCell align="center">
                  <Typography variant="body2">
                    <strong>{survey.completedResponses || 0}</strong> /{' '}
                    {survey.totalResponses || 0}
                  </Typography>
                </TableCell>
                <TableCell>
                  {survey.code ? (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
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
                      <Tooltip title="Copy code">
                        <IconButton
                          size="small"
                          onClick={() => onCopyCode(survey.code!)}
                          sx={{ ml: 0.5 }}
                        >
                          <ContentCopy fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </Box>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      -
                    </Typography>
                  )}
                </TableCell>
                <TableCell>
                  <Typography variant="body2" color="text.secondary">
                    {formatDate(survey.createdAt)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 0.5 }}>
                    <Tooltip title="View Statistics">
                      <IconButton size="small" onClick={() => onViewStats(survey)}>
                        <BarChart fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit Survey">
                      <IconButton size="small" onClick={() => onEdit(survey)}>
                        <Edit fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <IconButton size="small" onClick={(e) => handleMenuOpen(e, survey)}>
                      <MoreVert fontSize="small" />
                    </IconButton>
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Actions Menu */}
      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
        {selectedSurvey && [
          <MenuItem key="toggle" onClick={() => handleMenuAction(() => onToggleStatus(selectedSurvey))}>
            <ListItemIcon>
              {selectedSurvey.isActive ? <ToggleOff /> : <ToggleOn />}
            </ListItemIcon>
            <ListItemText>
              {selectedSurvey.isActive ? 'Deactivate' : 'Activate'} Survey
            </ListItemText>
          </MenuItem>,
          <MenuItem
            key="delete"
            onClick={() => handleMenuAction(() => onDelete(selectedSurvey))}
            sx={{ color: 'error.main' }}
          >
            <ListItemIcon>
              <Delete color="error" />
            </ListItemIcon>
            <ListItemText>Delete Survey</ListItemText>
          </MenuItem>
        ]}
      </Menu>
    </>
  );
};
