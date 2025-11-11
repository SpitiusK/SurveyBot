import { Box, TextField, MenuItem, Button, Typography } from '@mui/material';
import { FilterList as FilterListIcon, Refresh as RefreshIcon } from '@mui/icons-material';

interface StatisticsFiltersProps {
  filters: {
    status: 'all' | 'complete' | 'incomplete';
    dateFrom: Date | null;
    dateTo: Date | null;
  };
  onFilterChange: (filters: any) => void;
  onReset: () => void;
}

const StatisticsFilters = ({ filters, onFilterChange, onReset }: StatisticsFiltersProps) => {
  const handleStatusChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    onFilterChange({ ...filters, status: event.target.value });
  };

  const handleDateFromChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value ? new Date(event.target.value) : null;
    onFilterChange({ ...filters, dateFrom: value });
  };

  const handleDateToChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value ? new Date(event.target.value) : null;
    onFilterChange({ ...filters, dateTo: value });
  };

  const formatDateForInput = (date: Date | null) => {
    if (!date) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const hasActiveFilters =
    filters.status !== 'all' || filters.dateFrom !== null || filters.dateTo !== null;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <FilterListIcon sx={{ mr: 1 }} />
        <Typography variant="h6">Filters</Typography>
      </Box>

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField
          select
          label="Response Status"
          value={filters.status}
          onChange={handleStatusChange}
          size="small"
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="all">All Responses</MenuItem>
          <MenuItem value="complete">Complete Only</MenuItem>
          <MenuItem value="incomplete">Incomplete Only</MenuItem>
        </TextField>

        <TextField
          label="Date From"
          type="date"
          value={formatDateForInput(filters.dateFrom)}
          onChange={handleDateFromChange}
          size="small"
          InputLabelProps={{ shrink: true }}
          sx={{ minWidth: 180 }}
        />

        <TextField
          label="Date To"
          type="date"
          value={formatDateForInput(filters.dateTo)}
          onChange={handleDateToChange}
          size="small"
          InputLabelProps={{ shrink: true }}
          sx={{ minWidth: 180 }}
        />

        {hasActiveFilters && (
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={onReset}
            size="small"
          >
            Reset Filters
          </Button>
        )}
      </Box>

      {hasActiveFilters && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            Active filters applied - results are filtered based on your criteria
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default StatisticsFilters;
