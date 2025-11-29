import React, { useEffect, useState, useCallback } from 'react';
import {
  Box,
  Button,
  Typography,
  Pagination,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Skeleton,
  Alert,
  Grid,
  useMediaQuery,
  useTheme,
  Snackbar,
} from '@mui/material';
import { Add } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { Breadcrumb } from '@/components/Breadcrumb';
import { EmptyState } from '@/components/EmptyState';
import { SurveyFiltersComponent } from '@/components/SurveyFilters';
import type { SurveyFilters } from '@/components/SurveyFilters';
import { SurveyTable } from '@/components/SurveyTable';
import { SurveyCard } from '@/components/SurveyCard';
import { DeleteConfirmDialog } from '@/components/DeleteConfirmDialog';
import { ConfirmDialog } from '@/components/ConfirmDialog';
import surveyService from '@/services/surveyService';
import type { SurveyListItem, SurveyFilterParams } from '@/types';

// Debounce hook
const useDebounce = <T,>(value: T, delay: number): T => {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
};

const SurveyList: React.FC = () => {
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  // State
  const [surveys, setSurveys] = useState<SurveyListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Pagination
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);

  // Filters
  const [filters, setFilters] = useState<SurveyFilters>({
    searchTerm: '',
    statusFilter: 'all',
  });

  // Sorting
  const [sortBy, setSortBy] = useState<'createdAt' | 'title' | 'totalResponses'>('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  // Dialogs
  const [deleteDialog, setDeleteDialog] = useState<{
    open: boolean;
    survey: SurveyListItem | null;
    isDeleting: boolean;
  }>({
    open: false,
    survey: null,
    isDeleting: false,
  });

  const [statusDialog, setStatusDialog] = useState<{
    open: boolean;
    survey: SurveyListItem | null;
    isToggling: boolean;
  }>({
    open: false,
    survey: null,
    isToggling: false,
  });

  // Toast
  const [toast, setToast] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'info';
  }>({
    open: false,
    message: '',
    severity: 'success',
  });

  // Debounced search term
  const debouncedSearchTerm = useDebounce(filters.searchTerm, 300);

  // Load surveys
  const loadSurveys = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const params: SurveyFilterParams = {
        pageNumber: page,
        pageSize: pageSize,
        sortBy,
        sortOrder,
      };

      // Add search term if present
      if (debouncedSearchTerm) {
        params.searchTerm = debouncedSearchTerm;
      }

      // Add status filter if not 'all'
      if (filters.statusFilter !== 'all') {
        params.isActive = filters.statusFilter === 'active';
      }

      const data = await surveyService.getAllSurveys(params);

      setSurveys(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error('Failed to load surveys:', err);
      setError('Failed to load surveys. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, sortBy, sortOrder, debouncedSearchTerm, filters.statusFilter]);

  useEffect(() => {
    loadSurveys();
  }, [loadSurveys]);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearchTerm, filters.statusFilter]);

  // Handlers
  const handleFiltersChange = (newFilters: SurveyFilters) => {
    setFilters(newFilters);
  };

  const handleClearFilters = () => {
    setFilters({
      searchTerm: '',
      statusFilter: 'all',
    });
  };

  const handleSort = (column: 'createdAt' | 'title' | 'totalResponses') => {
    if (sortBy === column) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(column);
      setSortOrder(column === 'createdAt' ? 'desc' : 'asc');
    }
  };

  const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
    setPage(value);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handlePageSizeChange = (event: any) => {
    setPageSize(event.target.value);
    setPage(1);
  };

  const handleEdit = (survey: SurveyListItem) => {
    navigate(`/dashboard/surveys/${survey.id}/edit`);
  };

  const handleViewStats = (survey: SurveyListItem) => {
    navigate(`/dashboard/surveys/${survey.id}/statistics`);
  };

  const handleCopyCode = async (code: string) => {
    try {
      await navigator.clipboard.writeText(code);
      setToast({
        open: true,
        message: `Survey code "${code}" copied to clipboard!`,
        severity: 'success',
      });
    } catch (err) {
      setToast({
        open: true,
        message: 'Failed to copy code to clipboard',
        severity: 'error',
      });
    }
  };

  const handleDeleteClick = (survey: SurveyListItem) => {
    setDeleteDialog({
      open: true,
      survey,
      isDeleting: false,
    });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.survey) return;

    try {
      setDeleteDialog((prev) => ({ ...prev, isDeleting: true }));
      await surveyService.deleteSurvey(deleteDialog.survey.id);

      setToast({
        open: true,
        message: `Survey "${deleteDialog.survey.title}" deleted successfully`,
        severity: 'success',
      });

      setDeleteDialog({ open: false, survey: null, isDeleting: false });
      loadSurveys();
    } catch (err) {
      console.error('Failed to delete survey:', err);
      setToast({
        open: true,
        message: 'Failed to delete survey. Please try again.',
        severity: 'error',
      });
      setDeleteDialog((prev) => ({ ...prev, isDeleting: false }));
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialog({ open: false, survey: null, isDeleting: false });
  };

  const handleToggleStatusClick = (survey: SurveyListItem) => {
    setStatusDialog({
      open: true,
      survey,
      isToggling: false,
    });
  };

  const handleToggleStatusConfirm = async () => {
    if (!statusDialog.survey) return;

    try {
      setStatusDialog((prev) => ({ ...prev, isToggling: true }));
      await surveyService.toggleSurveyStatus(statusDialog.survey.id, statusDialog.survey.isActive);

      setToast({
        open: true,
        message: `Survey ${statusDialog.survey.isActive ? 'deactivated' : 'activated'} successfully`,
        severity: 'success',
      });

      setStatusDialog({ open: false, survey: null, isToggling: false });
      loadSurveys();
    } catch (err) {
      console.error('Failed to toggle survey status:', err);
      setToast({
        open: true,
        message: 'Failed to update survey status. Please try again.',
        severity: 'error',
      });
      setStatusDialog((prev) => ({ ...prev, isToggling: false }));
    }
  };

  const handleToggleStatusCancel = () => {
    setStatusDialog({ open: false, survey: null, isToggling: false });
  };

  const handleToastClose = () => {
    setToast((prev) => ({ ...prev, open: false }));
  };

  return (
    <Box sx={{ maxWidth: 1400, mx: 'auto' }}>
      <Breadcrumb />

        {/* Header */}
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: { xs: 'flex-start', sm: 'center' },
            flexDirection: { xs: 'column', sm: 'row' },
            gap: 2,
            mb: 4,
          }}
        >
          <Box>
            <Typography variant="h4" fontWeight={700} gutterBottom>
              All Surveys
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Manage and view all your surveys
            </Typography>
          </Box>
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => navigate('/dashboard/surveys/new')}
            size="large"
          >
            Create Survey
          </Button>
        </Box>

        {/* Error Alert */}
        {error && (
          <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {/* Filters */}
        <SurveyFiltersComponent
          filters={filters}
          onFiltersChange={handleFiltersChange}
          onClear={handleClearFilters}
        />

        {/* Loading State */}
        {isLoading ? (
          <Box>
            {isMobile ? (
              <Grid container spacing={2}>
                {[...Array(3)].map((_, i) => (
                  <Grid item xs={12} key={i}>
                    <Skeleton variant="rectangular" height={200} />
                  </Grid>
                ))}
              </Grid>
            ) : (
              <Box>
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} variant="rectangular" height={60} sx={{ mb: 1 }} />
                ))}
              </Box>
            )}
          </Box>
        ) : surveys.length === 0 ? (
          /* Empty State */
          <EmptyState
            title={
              filters.searchTerm || filters.statusFilter !== 'all'
                ? 'No surveys found'
                : 'No surveys yet'
            }
            description={
              filters.searchTerm || filters.statusFilter !== 'all'
                ? 'Try adjusting your filters or search term'
                : 'Create your first survey to get started'
            }
            actionLabel={
              filters.searchTerm || filters.statusFilter !== 'all'
                ? 'Clear Filters'
                : 'Create Survey'
            }
            onAction={
              filters.searchTerm || filters.statusFilter !== 'all'
                ? handleClearFilters
                : () => navigate('/dashboard/surveys/new')
            }
          />
        ) : (
          <>
            {/* Survey List */}
            {isMobile ? (
              <Grid container spacing={2} sx={{ mb: 3 }}>
                {surveys.map((survey) => (
                  <Grid item xs={12} key={survey.id}>
                    <SurveyCard
                      survey={survey}
                      onEdit={handleEdit}
                      onDelete={handleDeleteClick}
                      onToggleStatus={handleToggleStatusClick}
                      onViewStats={handleViewStats}
                      onCopyCode={handleCopyCode}
                    />
                  </Grid>
                ))}
              </Grid>
            ) : (
              <Box sx={{ mb: 3 }}>
                <SurveyTable
                  surveys={surveys}
                  onEdit={handleEdit}
                  onDelete={handleDeleteClick}
                  onToggleStatus={handleToggleStatusClick}
                  onViewStats={handleViewStats}
                  onCopyCode={handleCopyCode}
                  sortBy={sortBy}
                  sortOrder={sortOrder}
                  onSort={handleSort}
                />
              </Box>
            )}

            {/* Pagination */}
            <Box
              sx={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 2,
                mt: 3,
              }}
            >
              <Typography variant="body2" color="text.secondary">
                Showing {surveys.length} of {totalCount} surveys
              </Typography>

              <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                <FormControl size="small" sx={{ minWidth: 100 }}>
                  <InputLabel>Per page</InputLabel>
                  <Select value={pageSize} onChange={handlePageSizeChange} label="Per page">
                    <MenuItem value={10}>10</MenuItem>
                    <MenuItem value={25}>25</MenuItem>
                    <MenuItem value={50}>50</MenuItem>
                  </Select>
                </FormControl>

                <Pagination
                  count={totalPages}
                  page={page}
                  onChange={handlePageChange}
                  color="primary"
                  showFirstButton
                  showLastButton
                />
              </Box>
            </Box>
          </>
        )}

      {/* Delete Confirmation Dialog */}
      {deleteDialog.survey && (
        <DeleteConfirmDialog
          open={deleteDialog.open}
          surveyTitle={deleteDialog.survey.title}
          hasResponses={deleteDialog.survey.totalResponses > 0}
          responseCount={deleteDialog.survey.totalResponses}
          isDeleting={deleteDialog.isDeleting}
          onConfirm={handleDeleteConfirm}
          onCancel={handleDeleteCancel}
        />
      )}

      {/* Status Toggle Confirmation Dialog */}
      {statusDialog.survey && (
        <ConfirmDialog
          open={statusDialog.open}
          title={`${statusDialog.survey.isActive ? 'Deactivate' : 'Activate'} Survey`}
          message={`Are you sure you want to ${statusDialog.survey.isActive ? 'deactivate' : 'activate'} the survey "${statusDialog.survey.title}"?`}
          confirmLabel={statusDialog.survey.isActive ? 'Deactivate' : 'Activate'}
          onConfirm={handleToggleStatusConfirm}
          onCancel={handleToggleStatusCancel}
          severity={statusDialog.survey.isActive ? 'warning' : 'info'}
        />
      )}

      {/* Toast Notifications */}
      <Snackbar
        open={toast.open}
        autoHideDuration={4000}
        onClose={handleToastClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={handleToastClose} severity={toast.severity} sx={{ width: '100%' }}>
          {toast.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default SurveyList;
