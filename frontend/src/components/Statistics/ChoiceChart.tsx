import { Box, Typography } from '@mui/material';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip, BarChart, Bar, XAxis, YAxis, CartesianGrid } from 'recharts';
import type { Question, QuestionStatistics } from '../../types';

interface ChoiceChartProps {
  questionStat: QuestionStatistics;
  question: Question;
  isSingleChoice: boolean;
}

const COLORS = [
  '#1976d2', // Blue
  '#2e7d32', // Green
  '#ed6c02', // Orange
  '#9c27b0', // Purple
  '#d32f2f', // Red
  '#0288d1', // Light Blue
  '#f57c00', // Dark Orange
  '#7b1fa2', // Dark Purple
  '#c62828', // Dark Red
  '#00796b', // Teal
];

const ChoiceChart = ({ questionStat, question: _question, isSingleChoice }: ChoiceChartProps) => {
  const { choiceDistribution } = questionStat;

  if (!choiceDistribution || Object.keys(choiceDistribution).length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">No responses yet</Typography>
      </Box>
    );
  }

  // Prepare data for charts
  // choiceDistribution is Record<string, { option, count, percentage }>
  const chartData = Object.values(choiceDistribution).map((choice) => ({
    name: choice.option,
    value: choice.count,
    percentage: choice.percentage.toFixed(1),
  }));

  // Sort by value descending
  chartData.sort((a, b) => b.value - a.value);

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
            {payload[0].name}
          </Typography>
          <Typography variant="body2" color="primary">
            Count: {payload[0].value}
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
      <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap', alignItems: 'center' }}>
        {/* Chart */}
        <Box sx={{ flex: '1 1 400px', minWidth: 300 }}>
          {isSingleChoice ? (
            // Pie Chart for Single Choice
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={chartData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={(entry: any) => `${entry.payload.percentage}%`}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {chartData.map((_entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip content={<CustomTooltip />} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            // Bar Chart for Multiple Choice
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" angle={-45} textAnchor="end" height={100} />
                <YAxis />
                <Tooltip content={<CustomTooltip />} />
                <Bar dataKey="value" fill="#1976d2">
                  {chartData.map((_entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </Box>

        {/* Statistics Table */}
        <Box sx={{ flex: '1 1 300px' }}>
          <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
            Distribution
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
            {chartData.map((item, index) => (
              <Box
                key={index}
                sx={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  p: 1.5,
                  bgcolor: 'action.hover',
                  borderRadius: 1,
                  borderLeft: 4,
                  borderColor: COLORS[index % COLORS.length],
                }}
              >
                <Box sx={{ flex: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {item.name}
                  </Typography>
                </Box>
                <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                  <Typography variant="body2" color="text.secondary">
                    {item.value} responses
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      fontWeight: 'bold',
                      color: COLORS[index % COLORS.length],
                      minWidth: 50,
                      textAlign: 'right',
                    }}
                  >
                    {item.percentage}%
                  </Typography>
                </Box>
              </Box>
            ))}
          </Box>
        </Box>
      </Box>
    </Box>
  );
};

export default ChoiceChart;
