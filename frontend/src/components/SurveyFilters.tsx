import React from 'react';
import {
  Box,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  InputAdornment,
} from '@mui/material';
import type { SelectChangeEvent } from '@mui/material';
import { Search, Clear } from '@mui/icons-material';

export interface SurveyFilters {
  searchTerm: string;
  statusFilter: 'all' | 'active' | 'inactive';
}

interface SurveyFiltersProps {
  filters: SurveyFilters;
  onFiltersChange: (filters: SurveyFilters) => void;
  onClear: () => void;
}

export const SurveyFiltersComponent: React.FC<SurveyFiltersProps> = ({
  filters,
  onFiltersChange,
  onClear,
}) => {
  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    onFiltersChange({
      ...filters,
      searchTerm: event.target.value,
    });
  };

  const handleStatusChange = (event: SelectChangeEvent) => {
    onFiltersChange({
      ...filters,
      statusFilter: event.target.value as 'all' | 'active' | 'inactive',
    });
  };

  const hasActiveFilters = filters.searchTerm !== '' || filters.statusFilter !== 'all';

  return (
    <Box
      sx={{
        display: 'flex',
        gap: 2,
        mb: 3,
        flexWrap: { xs: 'wrap', md: 'nowrap' },
      }}
    >
      {/* Search Field */}
      <TextField
        fullWidth
        placeholder="Search surveys by title..."
        value={filters.searchTerm}
        onChange={handleSearchChange}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Search />
            </InputAdornment>
          ),
        }}
        sx={{ flex: { xs: '1 1 100%', md: '1 1 auto' } }}
      />

      {/* Status Filter */}
      <FormControl sx={{ minWidth: 150, flex: { xs: '1 1 100%', sm: '0 0 auto' } }}>
        <InputLabel>Status</InputLabel>
        <Select value={filters.statusFilter} onChange={handleStatusChange} label="Status">
          <MenuItem value="all">All Surveys</MenuItem>
          <MenuItem value="active">Active</MenuItem>
          <MenuItem value="inactive">Inactive</MenuItem>
        </Select>
      </FormControl>

      {/* Clear Filters Button */}
      {hasActiveFilters && (
        <Button
          variant="outlined"
          startIcon={<Clear />}
          onClick={onClear}
          sx={{ flex: { xs: '1 1 100%', sm: '0 0 auto' }, whiteSpace: 'nowrap' }}
        >
          Clear Filters
        </Button>
      )}
    </Box>
  );
};
