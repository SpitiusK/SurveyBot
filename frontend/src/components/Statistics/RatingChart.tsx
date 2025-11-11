import { Box, Typography, LinearProgress } from '@mui/material';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';
import { Star as StarIcon } from '@mui/icons-material';
import type { Question, QuestionStatistics } from '../../types';

interface RatingChartProps {
  questionStat: QuestionStatistics;
  question: Question;
}

const RATING_COLORS = {
  1: '#d32f2f', // Red
  2: '#f57c00', // Orange
  3: '#fbc02d', // Yellow
  4: '#7cb342', // Light Green
  5: '#388e3c', // Green
};

const RatingChart = ({ questionStat, question: _question }: RatingChartProps) => {
  const { choiceDistribution, averageRating } = questionStat;

  if (!choiceDistribution || Object.keys(choiceDistribution).length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">No ratings yet</Typography>
      </Box>
    );
  }

  // Prepare data for all ratings 1-5
  const chartData = [1, 2, 3, 4, 5].map((rating) => ({
    rating,
    count: choiceDistribution[rating.toString()] || 0,
    percentage:
      questionStat.totalAnswers > 0
        ? (((choiceDistribution[rating.toString()] || 0) / questionStat.totalAnswers) * 100).toFixed(
            1
          )
        : '0',
  }));

  const maxCount = Math.max(...chartData.map((d) => d.count));

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      return (
        <Box
          sx={{
            bgcolor: 'background.paper',
            p: 1.5,
            border: 1,
            borderColor: 'divider',
            borderRadius: 1,
          }}
        >
          <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
            {payload[0].payload.rating} Star{payload[0].payload.rating > 1 ? 's' : ''}
          </Typography>
          <Typography variant="body2" color="primary">
            {payload[0].value} responses
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {payload[0].payload.percentage}%
          </Typography>
        </Box>
      );
    }
    return null;
  };

  return (
    <Box>
      {/* Average Rating Display */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          mb: 3,
          p: 3,
          bgcolor: 'action.hover',
          borderRadius: 2,
        }}
      >
        <Box sx={{ textAlign: 'center' }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Average Rating
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
              {averageRating?.toFixed(2) || '0.00'}
            </Typography>
            <StarIcon sx={{ fontSize: 40, color: '#fbc02d' }} />
          </Box>
          <Typography variant="body2" color="text.secondary">
            out of 5.0 ({questionStat.totalAnswers} ratings)
          </Typography>
        </Box>
      </Box>

      <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
        {/* Bar Chart */}
        <Box sx={{ flex: '1 1 400px', minWidth: 300 }}>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis
                dataKey="rating"
                label={{ value: 'Rating', position: 'insideBottom', offset: -5 }}
              />
              <YAxis label={{ value: 'Count', angle: -90, position: 'insideLeft' }} />
              <Tooltip content={<CustomTooltip />} />
              <Bar dataKey="count" fill="#1976d2">
                {chartData.map((entry, index) => (
                  <Cell
                    key={`cell-${index}`}
                    fill={RATING_COLORS[entry.rating as keyof typeof RATING_COLORS]}
                  />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </Box>

        {/* Rating Distribution */}
        <Box sx={{ flex: '1 1 300px' }}>
          <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
            Rating Distribution
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            {[5, 4, 3, 2, 1].map((rating) => {
              const data = chartData.find((d) => d.rating === rating);
              const count = data?.count || 0;
              const percentage = parseFloat(data?.percentage || '0');

              return (
                <Box key={rating}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', minWidth: 80 }}>
                      <Typography variant="body2" sx={{ fontWeight: 500, mr: 0.5 }}>
                        {rating}
                      </Typography>
                      <StarIcon sx={{ fontSize: 16, color: '#fbc02d' }} />
                    </Box>
                    <Box sx={{ flex: 1 }}>
                      <LinearProgress
                        variant="determinate"
                        value={maxCount > 0 ? (count / maxCount) * 100 : 0}
                        sx={{
                          height: 8,
                          borderRadius: 1,
                          bgcolor: 'action.hover',
                          '& .MuiLinearProgress-bar': {
                            bgcolor: RATING_COLORS[rating as keyof typeof RATING_COLORS],
                          },
                        }}
                      />
                    </Box>
                    <Typography
                      variant="body2"
                      sx={{ minWidth: 80, textAlign: 'right', color: 'text.secondary' }}
                    >
                      {count} ({percentage}%)
                    </Typography>
                  </Box>
                </Box>
              );
            })}
          </Box>
        </Box>
      </Box>
    </Box>
  );
};

export default RatingChart;
