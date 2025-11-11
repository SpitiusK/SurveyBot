import React, { useEffect, useState } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Paper,
  Skeleton,
  Alert,
} from '@mui/material';
import {
  Assignment,
  People,
  CheckCircle,
  TrendingUp,
  Add,
  ArrowForward,
  Timeline,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { AppShell } from '@/layouts/AppShell';
import { Breadcrumb } from '@/components/Breadcrumb';
import surveyService from '@/services/surveyService';
import { useAuth } from '@/hooks/useAuth';
import type { SurveyListItem } from '@/types';

interface DashboardStats {
  totalSurveys: number;
  totalResponses: number;
  activeSurveys: number;
  completionRate: number;
}

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [stats, setStats] = useState<DashboardStats>({
    totalSurveys: 0,
    totalResponses: 0,
    activeSurveys: 0,
    completionRate: 0,
  });
  const [recentSurveys, setRecentSurveys] = useState<SurveyListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch surveys with pagination (first 5 for recent surveys)
      const surveysData = await surveyService.getAllSurveys({
        pageNumber: 1,
        pageSize: 5,
      });

      // Calculate statistics
      const totalSurveys = surveysData.totalCount;
      const activeSurveys = surveysData.items.filter((s) => s.isActive).length;

      // Calculate total responses and completion rate
      let totalResponses = 0;
      let completedResponses = 0;

      surveysData.items.forEach((survey) => {
        totalResponses += survey.totalResponses || 0;
        completedResponses += survey.completedResponses || 0;
      });

      const completionRate =
        totalResponses > 0 ? Math.round((completedResponses / totalResponses) * 100) : 0;

      setStats({
        totalSurveys,
        totalResponses,
        activeSurveys,
        completionRate,
      });

      setRecentSurveys(surveysData.items);
    } catch (err) {
      console.error('Failed to load dashboard data:', err);
      setError('Failed to load dashboard data. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const statCards = [
    {
      title: 'Total Surveys',
      value: stats.totalSurveys.toString(),
      icon: <Assignment />,
      color: '#1976d2',
      bgColor: '#e3f2fd',
    },
    {
      title: 'Total Responses',
      value: stats.totalResponses.toString(),
      icon: <People />,
      color: '#2e7d32',
      bgColor: '#e8f5e9',
    },
    {
      title: 'Active Surveys',
      value: stats.activeSurveys.toString(),
      icon: <CheckCircle />,
      color: '#ed6c02',
      bgColor: '#fff3e0',
    },
    {
      title: 'Completion Rate',
      value: `${stats.completionRate}%`,
      icon: <TrendingUp />,
      color: '#9c27b0',
      bgColor: '#f3e5f5',
    },
  ];

  const handleViewSurvey = (surveyId: number) => {
    navigate(`/dashboard/surveys/${surveyId}/statistics`);
  };

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <AppShell>
      <Box sx={{ maxWidth: 1400, mx: 'auto' }}>
        <Breadcrumb />

        {/* Welcome Section */}
        <Box sx={{ mb: 4 }}>
          <Typography variant="h4" fontWeight={700} gutterBottom>
            Welcome back, {user?.firstName || user?.username || 'Admin'}!
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Here's an overview of your survey activity
          </Typography>
        </Box>

        {/* Error Alert */}
        {error && (
          <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {/* Stats Cards */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          {statCards.map((stat, index) => (
            <Grid item xs={12} sm={6} md={3} key={index}>
              <Card
                elevation={0}
                sx={{
                  border: '1px solid',
                  borderColor: 'divider',
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 3,
                  },
                }}
              >
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Box
                      sx={{
                        backgroundColor: stat.bgColor,
                        color: stat.color,
                        borderRadius: 2,
                        p: 1.5,
                        mr: 2,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      {React.cloneElement(stat.icon, { fontSize: 'large' })}
                    </Box>
                    <Box>
                      <Typography variant="body2" color="text.secondary" gutterBottom>
                        {stat.title}
                      </Typography>
                      {isLoading ? (
                        <Skeleton variant="text" width={60} height={40} />
                      ) : (
                        <Typography variant="h4" fontWeight={700}>
                          {stat.value}
                        </Typography>
                      )}
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {/* Quick Actions */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} md={6}>
            <Card
              elevation={0}
              sx={{
                border: '1px solid',
                borderColor: 'divider',
                height: '100%',
              }}
            >
              <CardContent>
                <Box
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    mb: 2,
                  }}
                >
                  <Typography variant="h6" fontWeight={600}>
                    Quick Actions
                  </Typography>
                </Box>
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                  <Button
                    variant="contained"
                    startIcon={<Add />}
                    onClick={() => navigate('/dashboard/surveys/new')}
                    size="large"
                  >
                    Create Survey
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<Assignment />}
                    onClick={() => navigate('/dashboard/surveys')}
                    size="large"
                  >
                    View All Surveys
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<Timeline />}
                    onClick={() => navigate('/dashboard/surveys')}
                    size="large"
                  >
                    View Statistics
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card
              elevation={0}
              sx={{
                border: '1px solid',
                borderColor: 'divider',
                height: '100%',
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                color: 'white',
              }}
            >
              <CardContent>
                <Typography variant="h6" fontWeight={600} gutterBottom>
                  Getting Started
                </Typography>
                <Typography variant="body2" sx={{ mb: 2, opacity: 0.9 }}>
                  Create your first survey and start collecting responses from your audience.
                </Typography>
                <Button
                  variant="contained"
                  sx={{
                    bgcolor: 'white',
                    color: '#667eea',
                    '&:hover': { bgcolor: 'rgba(255,255,255,0.9)' },
                  }}
                  endIcon={<ArrowForward />}
                  onClick={() => navigate('/dashboard/surveys/new')}
                >
                  Get Started
                </Button>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Recent Surveys */}
        <Card
          elevation={0}
          sx={{
            border: '1px solid',
            borderColor: 'divider',
          }}
        >
          <CardContent>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                mb: 3,
              }}
            >
              <Typography variant="h6" fontWeight={600}>
                Recent Surveys
              </Typography>
              <Button
                size="small"
                endIcon={<ArrowForward />}
                onClick={() => navigate('/dashboard/surveys')}
              >
                View All
              </Button>
            </Box>

            {isLoading ? (
              <Box>
                {[...Array(3)].map((_, i) => (
                  <Skeleton key={i} variant="rectangular" height={60} sx={{ mb: 1 }} />
                ))}
              </Box>
            ) : recentSurveys.length === 0 ? (
              <Box sx={{ textAlign: 'center', py: 6 }}>
                <Assignment sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
                <Typography variant="body1" color="text.secondary" gutterBottom>
                  No surveys yet
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                  Create your first survey to get started
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => navigate('/dashboard/surveys/new')}
                >
                  Create Survey
                </Button>
              </Box>
            ) : (
              <TableContainer component={Paper} variant="outlined">
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Title</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell align="center">Responses</TableCell>
                      <TableCell>Created</TableCell>
                      <TableCell align="right">Action</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {recentSurveys.map((survey) => (
                      <TableRow
                        key={survey.id}
                        hover
                        sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                      >
                        <TableCell>
                          <Typography variant="body2" fontWeight={500}>
                            {survey.title}
                          </Typography>
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
                            {survey.completedResponses || 0} / {survey.totalResponses || 0}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" color="text.secondary">
                            {formatDate(survey.createdAt)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Button
                            size="small"
                            onClick={() => handleViewSurvey(survey.id)}
                            endIcon={<ArrowForward />}
                          >
                            View
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>
      </Box>
    </AppShell>
  );
};

export default Dashboard;
