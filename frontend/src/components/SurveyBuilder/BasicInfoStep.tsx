import React from 'react';
import {
  TextField,
  FormControlLabel,
  Checkbox,
  Box,
  Typography,
  Paper,
  Alert,
} from '@mui/material';
import { Controller, type Control } from 'react-hook-form';
import type { BasicInfoFormData } from '@/schemas/surveySchemas';

interface BasicInfoStepProps {
  control: Control<BasicInfoFormData>;
  errors: Record<string, any>;
  isLoading?: boolean;
}

const BasicInfoStep: React.FC<BasicInfoStepProps> = ({
  control,
  errors,
  isLoading = false,
}) => {
  return (
    <Box>
      {/* Step Description */}
      <Paper
        elevation={0}
        sx={{
          p: 3,
          mb: 3,
          backgroundColor: 'primary.50',
          borderLeft: 4,
          borderColor: 'primary.main',
        }}
      >
        <Typography variant="h6" gutterBottom sx={{ color: 'primary.main', fontWeight: 600 }}>
          Basic Information
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Start by providing the basic details for your survey. Give it a clear title and
          description so respondents understand what the survey is about.
        </Typography>
      </Paper>

      {/* Form Fields */}
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
        {/* Title Field */}
        <Controller
          name="title"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Survey Title"
              placeholder="Enter a clear, descriptive title"
              fullWidth
              required
              disabled={isLoading}
              error={!!fieldState.error}
              helperText={
                fieldState.error?.message ||
                `${field.value?.length || 0}/500 characters`
              }
              inputProps={{
                maxLength: 500,
              }}
              sx={{
                '& .MuiInputBase-root': {
                  backgroundColor: 'background.paper',
                },
              }}
            />
          )}
        />

        {/* Description Field */}
        <Controller
          name="description"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Description"
              placeholder="Provide additional context or instructions (optional)"
              fullWidth
              multiline
              rows={4}
              disabled={isLoading}
              error={!!fieldState.error}
              helperText={
                fieldState.error?.message ||
                `${field.value?.length || 0}/1000 characters`
              }
              inputProps={{
                maxLength: 1000,
              }}
              sx={{
                '& .MuiInputBase-root': {
                  backgroundColor: 'background.paper',
                },
              }}
            />
          )}
        />

        {/* Settings Section */}
        <Paper
          elevation={0}
          sx={{
            p: 2.5,
            backgroundColor: 'grey.50',
            border: '1px solid',
            borderColor: 'divider',
          }}
        >
          <Typography
            variant="subtitle1"
            gutterBottom
            sx={{ fontWeight: 600, mb: 2 }}
          >
            Survey Settings
          </Typography>

          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            {/* Show Results Checkbox */}
            <Controller
              name="showResults"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  control={
                    <Checkbox
                      {...field}
                      checked={field.value}
                      disabled={isLoading}
                    />
                  }
                  label={
                    <Box>
                      <Typography variant="body2" fontWeight={500}>
                        Show results to respondents
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Allow respondents to view survey results after submission
                      </Typography>
                    </Box>
                  }
                />
              )}
            />

            {/* Allow Multiple Responses Checkbox */}
            <Controller
              name="allowMultipleResponses"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  control={
                    <Checkbox
                      {...field}
                      checked={field.value}
                      disabled={isLoading}
                    />
                  }
                  label={
                    <Box>
                      <Typography variant="body2" fontWeight={500}>
                        Allow multiple responses
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Users can submit multiple responses to this survey
                      </Typography>
                    </Box>
                  }
                />
              )}
            />
          </Box>
        </Paper>

        {/* Validation Summary */}
        {Object.keys(errors).length > 0 && (
          <Alert severity="error" sx={{ mt: 1 }}>
            <Typography variant="body2" fontWeight={500} gutterBottom>
              Please fix the following errors:
            </Typography>
            <ul style={{ margin: '0.5rem 0 0 0', paddingLeft: '1.5rem' }}>
              {Object.entries(errors).map(([field, error]: [string, any]) => (
                <li key={field}>
                  <Typography variant="caption">
                    {field.charAt(0).toUpperCase() + field.slice(1)}:{' '}
                    {error?.message}
                  </Typography>
                </li>
              ))}
            </ul>
          </Alert>
        )}
      </Box>
    </Box>
  );
};

export default BasicInfoStep;
