import { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Typography,
  Paper,
  Alert,
  Button,
  Skeleton,
  Divider,
  Snackbar,
  Tooltip,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Refresh as RefreshIcon,
  GetApp as ExportIcon,
} from '@mui/icons-material';
import api from '../services/api';
import type { Survey, SurveyStatistics as SurveyStatisticsType, Response } from '../types';
import { PageContainer } from '../components/PageContainer';
import Breadcrumb from '../components/Breadcrumb';
import OverviewMetrics from '../components/Statistics/OverviewMetrics';
import ResponsesTable from '../components/Statistics/ResponsesTable';
import QuestionStatistics from '../components/Statistics/QuestionStatistics';
import StatisticsFilters from '../components/Statistics/StatisticsFilters';
import ExportDialog from '../components/Statistics/ExportDialog';
import { CSVGenerator } from '../components/Statistics/CSVGenerator';
import type { ExportOptions } from '../components/Statistics/ExportDialog';

interface StatisticsFilters {
  status: 'all' | 'complete' | 'incomplete';
  dateFrom: Date | null;
  dateTo: Date | null;
  questionDisplay: 'all' | 'included' | 'excluded'; // NEW v1.6.3: Filter by includeInStatistics
}

const SurveyStatistics = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [survey, setSurvey] = useState<Survey | null>(null);
  const [statistics, setStatistics] = useState<SurveyStatisticsType | null>(null);
  const [responses, setResponses] = useState<Response[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [filters, setFilters] = useState<StatisticsFilters>({
    status: 'all',
    dateFrom: null,
    dateTo: null,
    questionDisplay: 'all', // NEW v1.6.3: Default to showing all questions
  });

  const [exportDialogOpen, setExportDialogOpen] = useState(false);
  const [exportSuccess, setExportSuccess] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);

  const fetchData = async () => {
    if (!id) return;

    try {
      setLoading(true);
      setError(null);

      // Fetch survey details, statistics, and responses in parallel
      const [surveyRes, statsRes, responsesRes] = await Promise.all([
        api.get(`/surveys/${id}`),
        api.get(`/surveys/${id}/statistics?includeAllQuestions=true`),
        api.get(`/surveys/${id}/responses`),
      ]);

      setSurvey(surveyRes.data.data);
      setStatistics(statsRes.data.data);
      setResponses(responsesRes.data.data.items || responsesRes.data.data);
    } catch (err: any) {
      console.error('Error fetching statistics:', err);
      setError(err.response?.data?.message || 'Failed to load statistics');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [id]);

  const handleFilterChange = (newFilters: StatisticsFilters) => {
    setFilters(newFilters);
  };

  const handleResetFilters = () => {
    setFilters({
      status: 'all',
      dateFrom: null,
      dateTo: null,
      questionDisplay: 'all', // NEW v1.6.3
    });
  };

  const handleExport = () => {
    setExportDialogOpen(true);
  };

  const handleExportConfirm = async (options: ExportOptions) => {
    if (!survey) return;

    try {
      setExportError(null);

      // Check for large dataset
      const isLargeDataset = responses.length > 1000;

      if (isLargeDataset) {
        // Use chunked export for large datasets
        await CSVGenerator.downloadLargeCSV(
          survey,
          responses,
          options,
          (progress) => {
            console.log(`Export progress: ${progress}%`);
          }
        );
      } else {
        // Standard export
        await CSVGenerator.downloadCSV(survey, responses, options);
      }

      setExportSuccess(true);
      setExportDialogOpen(false);
    } catch (err: any) {
      console.error('Export error:', err);
      setExportError(err.message || 'Failed to export data');
      throw err; // Re-throw so dialog can show error
    }
  };

  const handleCloseExportDialog = () => {
    setExportDialogOpen(false);
    setExportError(null);
  };

  const handleCloseSuccessSnackbar = () => {
    setExportSuccess(false);
  };

  const handleCloseErrorSnackbar = () => {
    setExportError(null);
  };

  // Filter responses based on current filters
  const filteredResponses = responses.filter((response) => {
    // Status filter
    if (filters.status === 'complete' && !response.isComplete) return false;
    if (filters.status === 'incomplete' && response.isComplete) return false;

    // Date range filter
    if (filters.dateFrom && response.submittedAt) {
      const submittedDate = new Date(response.submittedAt);
      if (submittedDate < filters.dateFrom) return false;
    }
    if (filters.dateTo && response.submittedAt) {
      const submittedDate = new Date(response.submittedAt);
      if (submittedDate > filters.dateTo) return false;
    }

    return true;
  });

  // NEW v1.6.3: Filter questions based on includeInStatistics setting
  const filteredQuestionStats = useMemo(() => {
    if (!statistics) return [];

    return statistics.questionStatistics.filter((questionStat) => {
      const question = survey?.questions.find((q) => q.id === questionStat.questionId);
      if (!question) return false;

      // Apply includeInStatistics filter
      switch (filters.questionDisplay) {
        case 'all':
          return true; // Show all questions
        case 'included':
          return question.includeInStatistics !== false; // Show only included (true or undefined)
        case 'excluded':
          return question.includeInStatistics === false; // Show only excluded
        default:
          return true;
      }
    });
  }, [statistics, survey, filters.questionDisplay]);


  if (loading) {
    return (
      <PageContainer>
        <Container maxWidth="xl">
          <Box sx={{ mb: 3 }}>
            <Skeleton variant="text" width={200} height={40} />
            <Skeleton variant="text" width={400} height={30} />
          </Box>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 2, mb: 3 }}>
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} variant="rectangular" height={120} />
            ))}
          </Box>
          <Skeleton variant="rectangular" height={400} />
        </Container>
      </PageContainer>
    );
  }

  if (error) {
    return (
      <PageContainer>
        <Container maxWidth="xl">
          <Breadcrumb />
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={fetchData}>
                Retry
              </Button>
            }
          >
            {error}
          </Alert>
        </Container>
      </PageContainer>
    );
  }

  if (!survey || !statistics) {
    return (
      <PageContainer>
        <Container maxWidth="xl">
          <Breadcrumb />
          <Alert severity="warning">Survey not found</Alert>
        </Container>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <Container maxWidth="xl">
        <Breadcrumb />

        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
          <Box>
            <Typography variant="h4" gutterBottom>
              {survey.title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Survey Statistics & Analytics
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={<ArrowBackIcon />}
              onClick={() => navigate('/dashboard/surveys')}
            >
              Back to Surveys
            </Button>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={fetchData}
            >
              Refresh
            </Button>
            <Tooltip
              title={
                responses.length === 0
                  ? 'No responses to export'
                  : 'Export survey responses to CSV file'
              }
            >
              <span>
                <Button
                  variant="contained"
                  startIcon={<ExportIcon />}
                  onClick={handleExport}
                  disabled={responses.length === 0}
                >
                  Export CSV
                </Button>
              </span>
            </Tooltip>
          </Box>
        </Box>

        {/* Filters */}
        <Paper sx={{ p: 2, mb: 3 }}>
          <StatisticsFilters
            filters={filters}
            onFilterChange={handleFilterChange}
            onReset={handleResetFilters}
          />
        </Paper>

        {/* Overview Metrics */}
        <OverviewMetrics statistics={statistics} />

        <Divider sx={{ my: 4 }} />

        {/* Responses Table */}
        <Box sx={{ mb: 4 }}>
          <Typography variant="h5" gutterBottom>
            Survey Responses
          </Typography>
          <ResponsesTable
            responses={filteredResponses}
            survey={survey}
          />
        </Box>

        <Divider sx={{ my: 4 }} />

        {/* Question Statistics */}
        <Box>
          <Typography variant="h5" gutterBottom>
            Question-Level Statistics
          </Typography>
          {/* NEW v1.6.3: Show empty state or filtered question statistics */}
          {filteredQuestionStats.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 4 }}>
              <Typography variant="h6" color="text.secondary">
                No questions match the current filter
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {filters.questionDisplay === 'excluded' &&
                  'No questions are excluded from statistics in this survey'}
                {filters.questionDisplay === 'included' &&
                  'All questions are excluded from statistics in this survey'}
              </Typography>
            </Box>
          ) : (
            <QuestionStatistics
              statistics={{
                ...statistics,
                questionStatistics: filteredQuestionStats,
              }}
              survey={survey}
              responses={filteredResponses}
            />
          )}
        </Box>

        {/* Export Dialog */}
        <ExportDialog
          open={exportDialogOpen}
          onClose={handleCloseExportDialog}
          onExport={handleExportConfirm}
          responseCount={responses.length}
          completedCount={responses.filter(r => r.isComplete).length}
          incompleteCount={responses.filter(r => !r.isComplete).length}
          surveyTitle={survey.title}
          totalQuestionCount={survey.questions?.length ?? 0}
          statisticsQuestionCount={survey.questions?.filter(q => q.includeInStatistics !== false).length ?? 0}
        />

        {/* Success Snackbar */}
        <Snackbar
          open={exportSuccess}
          autoHideDuration={6000}
          onClose={handleCloseSuccessSnackbar}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        >
          <Alert
            onClose={handleCloseSuccessSnackbar}
            severity="success"
            sx={{ width: '100%' }}
          >
            Survey data exported successfully!
          </Alert>
        </Snackbar>

        {/* Error Snackbar */}
        <Snackbar
          open={!!exportError}
          autoHideDuration={6000}
          onClose={handleCloseErrorSnackbar}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        >
          <Alert
            onClose={handleCloseErrorSnackbar}
            severity="error"
            sx={{ width: '100%' }}
          >
            {exportError}
          </Alert>
        </Snackbar>
      </Container>
    </PageContainer>
  );
};

export default SurveyStatistics;
