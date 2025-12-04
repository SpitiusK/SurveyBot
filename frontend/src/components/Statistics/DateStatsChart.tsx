import { Box, Grid, Paper, Typography } from '@mui/material';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { format, parseISO } from 'date-fns';
import type { DateStatistics } from '../../types';

interface DateStatsChartProps {
  statistics: DateStatistics;
}

interface MetricCardProps {
  title: string;
  value: string;
}

const MetricCard = ({ title, value }: MetricCardProps) => (
  <Paper sx={{ p: 2, textAlign: 'center' }}>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      {title}
    </Typography>
    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
      {value}
    </Typography>
  </Paper>
);

export default function DateStatsChart({ statistics }: DateStatsChartProps) {
  // Handle no data case
  if (statistics.count === 0 || !statistics.dateDistribution || statistics.dateDistribution.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">No date data available</Typography>
      </Box>
    );
  }

  // Format date helper
  const formatDate = (dateString: string | null, formatString: string): string => {
    if (!dateString) return 'N/A';
    try {
      return format(parseISO(dateString), formatString);
    } catch {
      return 'Invalid Date';
    }
  };

  // Prepare chart data (sort chronologically for timeline)
  const chartData = [...statistics.dateDistribution]
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
    .map(item => ({
      date: formatDate(item.date, 'dd.MM.yy'), // Short format for axis
      fullDate: formatDate(item.date, 'dd.MM.yyyy'), // Full format for tooltip
      count: item.count,
      percentage: item.percentage,
    }));

  return (
    <Box>
      {/* Date Range Cards */}
      <Typography variant="h6" gutterBottom>
        Date Range
      </Typography>
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6}>
          <MetricCard
            title="Earliest Date"
            value={formatDate(statistics.earliestDate, 'dd.MM.yyyy')}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <MetricCard
            title="Latest Date"
            value={formatDate(statistics.latestDate, 'dd.MM.yyyy')}
          />
        </Grid>
      </Grid>

      {/* Timeline Chart */}
      <Paper sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Date Distribution
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {statistics.count} total responses across {statistics.dateDistribution.length} unique dates
        </Typography>
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="date"
              label={{ value: 'Date', position: 'insideBottom', offset: -5 }}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis label={{ value: 'Responses', angle: -90, position: 'insideLeft' }} />
            <Tooltip
              labelFormatter={(value, payload) => {
                if (payload && payload.length > 0) {
                  return `Date: ${payload[0].payload.fullDate}`;
                }
                return value;
              }}
              formatter={(value: number, name: string) => {
                if (name === 'count') return [`${value} responses`, 'Count'];
                if (name === 'percentage') return [`${value.toFixed(1)}%`, 'Percentage'];
                return [value, name];
              }}
            />
            <Line
              type="monotone"
              dataKey="count"
              stroke="#1976d2"
              strokeWidth={2}
              dot={{ fill: '#1976d2', r: 4 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </Paper>
    </Box>
  );
}
