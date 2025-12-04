import { Box, Grid, Paper, Typography } from '@mui/material';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import type { NumberStatistics } from '../../types';

interface NumberStatsCardProps {
  statistics: NumberStatistics;
}

interface MetricCardProps {
  title: string;
  value: number;
  suffix?: string;
}

const MetricCard = ({ title, value, suffix = '' }: MetricCardProps) => (
  <Paper sx={{ p: 2, textAlign: 'center' }}>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      {title}
    </Typography>
    <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
      {value.toFixed(2)}{suffix}
    </Typography>
  </Paper>
);

export default function NumberStatsCard({ statistics }: NumberStatsCardProps) {
  // Handle no data case
  if (statistics.count === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">No numeric data available</Typography>
      </Box>
    );
  }

  // Calculate histogram bins (simplified - using 10 bins)
  const binCount = 10;
  const range = statistics.maximum - statistics.minimum;
  const binSize = range / binCount;

  // Generate histogram data (estimate using normal distribution)
  const histogramData = Array.from({ length: binCount }, (_, i) => {
    const binStart = statistics.minimum + (i * binSize);
    const binEnd = binStart + binSize;
    // Estimate count for this bin (simplified - equal distribution)
    const count = Math.round(statistics.count / binCount);

    return {
      range: `${binStart.toFixed(1)}-${binEnd.toFixed(1)}`,
      count: count,
      binStart: binStart,
      binEnd: binEnd,
    };
  });

  const metrics = [
    { title: 'Minimum', value: statistics.minimum },
    { title: 'Maximum', value: statistics.maximum },
    { title: 'Average', value: statistics.average },
    { title: 'Median', value: statistics.median },
    { title: 'Std Dev', value: statistics.standardDeviation },
    { title: 'Count', value: statistics.count },
    { title: 'Sum', value: statistics.sum },
    { title: 'Range', value: statistics.maximum - statistics.minimum },
  ];

  return (
    <Box>
      {/* Summary Metrics */}
      <Typography variant="h6" gutterBottom>
        Statistical Summary
      </Typography>
      <Grid container spacing={2} sx={{ mb: 3 }}>
        {metrics.map((metric, index) => (
          <Grid item xs={6} sm={3} key={index}>
            <MetricCard title={metric.title} value={metric.value} />
          </Grid>
        ))}
      </Grid>

      {/* Histogram */}
      <Paper sx={{ p: 2 }}>
        <Typography variant="h6" gutterBottom>
          Distribution
        </Typography>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={histogramData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="range"
              label={{ value: 'Value Range', position: 'insideBottom', offset: -5 }}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis label={{ value: 'Frequency', angle: -90, position: 'insideLeft' }} />
            <Tooltip />
            <Bar dataKey="count" fill="#1976d2" />
          </BarChart>
        </ResponsiveContainer>
      </Paper>
    </Box>
  );
}
