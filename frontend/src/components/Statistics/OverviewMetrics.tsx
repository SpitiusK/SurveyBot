import { Box, Paper, Typography, Grid } from '@mui/material';
import {
  Poll as PollIcon,
  CheckCircle as CheckCircleIcon,
  ShowChart as ShowChartIcon,
  Timer as TimerIcon,
  People as PeopleIcon,
  CalendarToday as CalendarTodayIcon,
  TrendingUp as TrendingUpIcon,
  Event as EventIcon,
} from '@mui/icons-material';
import type { SurveyStatistics } from '../../types';
import { format } from 'date-fns';

interface OverviewMetricsProps {
  statistics: SurveyStatistics;
}

interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ReactNode;
  color: string;
}

const MetricCard = ({ title, value, subtitle, icon, color }: MetricCardProps) => (
  <Paper
    sx={{
      p: 3,
      display: 'flex',
      flexDirection: 'column',
      height: '100%',
      position: 'relative',
      overflow: 'hidden',
    }}
  >
    <Box
      sx={{
        position: 'absolute',
        top: -10,
        right: -10,
        opacity: 0.1,
        transform: 'scale(2)',
        color: color,
      }}
    >
      {icon}
    </Box>
    <Box sx={{ position: 'relative', zIndex: 1 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1, color: color }}>
        {icon}
        <Typography variant="body2" sx={{ ml: 1, fontWeight: 500 }}>
          {title}
        </Typography>
      </Box>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 0.5 }}>
        {value}
      </Typography>
      {subtitle && (
        <Typography variant="body2" color="text.secondary">
          {subtitle}
        </Typography>
      )}
    </Box>
  </Paper>
);

const OverviewMetrics = ({ statistics }: OverviewMetricsProps) => {
  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'N/A';
    try {
      return format(new Date(dateString), 'MMM d, yyyy');
    } catch {
      return 'N/A';
    }
  };

  const formatTime = (minutes: number | null) => {
    if (minutes === null || minutes === 0) return 'N/A';
    if (minutes < 1) return '< 1 min';
    if (minutes < 60) return `${Math.round(minutes)} min`;
    const hours = Math.floor(minutes / 60);
    const mins = Math.round(minutes % 60);
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  };

  const metrics = [
    {
      title: 'Total Responses',
      value: statistics.totalResponses,
      subtitle: 'All survey responses',
      icon: <PollIcon />,
      color: '#1976d2',
    },
    {
      title: 'Completed',
      value: statistics.completedResponses,
      subtitle: `${statistics.incompleteResponses} incomplete`,
      icon: <CheckCircleIcon />,
      color: '#2e7d32',
    },
    {
      title: 'Completion Rate',
      value: `${statistics.completionRate.toFixed(1)}%`,
      subtitle: 'Finished surveys',
      icon: <ShowChartIcon />,
      color: '#ed6c02',
    },
    {
      title: 'Avg. Time',
      value: formatTime(statistics.averageCompletionTimeMinutes),
      subtitle: 'To complete',
      icon: <TimerIcon />,
      color: '#9c27b0',
    },
    {
      title: 'Unique Respondents',
      value: statistics.uniqueRespondents,
      subtitle: 'Different users',
      icon: <PeopleIcon />,
      color: '#0288d1',
    },
    {
      title: 'Created',
      value: formatDate(statistics.createdAt),
      subtitle: 'Survey launch date',
      icon: <CalendarTodayIcon />,
      color: '#00796b',
    },
    {
      title: 'First Response',
      value: formatDate(statistics.firstResponseAt),
      subtitle: 'Initial submission',
      icon: <TrendingUpIcon />,
      color: '#5e35b1',
    },
    {
      title: 'Latest Response',
      value: formatDate(statistics.lastResponseAt),
      subtitle: 'Most recent',
      icon: <EventIcon />,
      color: '#c62828',
    },
  ];

  return (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ mb: 2 }}>
        Overview Metrics
      </Typography>
      <Grid container spacing={2}>
        {metrics.map((metric, index) => (
          <Grid item xs={12} sm={6} md={3} key={index}>
            <MetricCard {...metric} />
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default OverviewMetrics;
