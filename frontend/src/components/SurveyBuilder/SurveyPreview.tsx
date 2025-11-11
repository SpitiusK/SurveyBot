import React from 'react';
import {
  Box,
  Typography,
  Stack,
  Paper,
  Divider,
  Chip,
  Grid,
} from '@mui/material';
import {
  Group as MultipleResponsesIcon,
  BarChart as ShowResultsIcon,
} from '@mui/icons-material';
import type { BasicInfoFormData } from '@/schemas/surveySchemas';
import type { QuestionDraft } from '@/schemas/questionSchemas';
import QuestionPreview from './QuestionPreview';

interface SurveyPreviewProps {
  surveyData: BasicInfoFormData;
  questions: QuestionDraft[];
}

const SurveyPreview: React.FC<SurveyPreviewProps> = ({ surveyData, questions }) => {
  const requiredCount = questions.filter((q) => q.isRequired).length;
  const optionalCount = questions.length - requiredCount;

  return (
    <Box>
      {/* Survey Header */}
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
        <Typography
          variant="h5"
          gutterBottom
          sx={{ color: 'primary.main', fontWeight: 600 }}
        >
          {surveyData.title}
        </Typography>
        {surveyData.description && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {surveyData.description}
          </Typography>
        )}
      </Paper>

      {/* Survey Statistics */}
      <Paper
        elevation={0}
        sx={{
          p: 2.5,
          mb: 3,
          backgroundColor: 'background.default',
          border: '1px solid',
          borderColor: 'divider',
        }}
      >
        <Typography variant="subtitle2" fontWeight={600} gutterBottom>
          Survey Overview
        </Typography>
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Box>
              <Typography variant="h4" color="primary.main" fontWeight="bold">
                {questions.length}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Total Questions
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Box>
              <Typography variant="h4" color="error.main" fontWeight="bold">
                {requiredCount}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Required
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Box>
              <Typography variant="h4" color="success.main" fontWeight="bold">
                {optionalCount}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Optional
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Box>
              <Typography variant="h4" color="info.main" fontWeight="bold">
                ~{Math.ceil(questions.length * 1.5)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Est. Minutes
              </Typography>
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* Survey Settings */}
      <Paper
        elevation={0}
        sx={{
          p: 2.5,
          mb: 3,
          backgroundColor: 'background.default',
          border: '1px solid',
          borderColor: 'divider',
        }}
      >
        <Typography variant="subtitle2" fontWeight={600} gutterBottom>
          Settings
        </Typography>
        <Stack direction="row" spacing={1.5} flexWrap="wrap" sx={{ mt: 1.5 }}>
          <Chip
            icon={<ShowResultsIcon />}
            label={surveyData.showResults ? 'Show Results' : 'Hide Results'}
            color={surveyData.showResults ? 'success' : 'default'}
            variant={surveyData.showResults ? 'filled' : 'outlined'}
          />
          <Chip
            icon={<MultipleResponsesIcon />}
            label={
              surveyData.allowMultipleResponses
                ? 'Multiple Responses'
                : 'Single Response Only'
            }
            color={surveyData.allowMultipleResponses ? 'info' : 'default'}
            variant={surveyData.allowMultipleResponses ? 'filled' : 'outlined'}
          />
        </Stack>
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            {surveyData.showResults
              ? 'Respondents will be able to view survey results after submission.'
              : 'Survey results will be hidden from respondents.'}
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
            {surveyData.allowMultipleResponses
              ? 'Users can submit multiple responses to this survey.'
              : 'Users can only submit one response to this survey.'}
          </Typography>
        </Box>
      </Paper>

      <Divider sx={{ my: 3 }} />

      {/* Questions Section */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Questions ({questions.length})
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Review all questions below. Make sure they are clear and in the correct order.
        </Typography>

        <Stack spacing={2}>
          {questions.length > 0 ? (
            questions.map((question, index) => (
              <QuestionPreview key={question.id} question={question} index={index} />
            ))
          ) : (
            <Paper
              elevation={0}
              sx={{
                p: 4,
                textAlign: 'center',
                backgroundColor: 'background.default',
                border: '1px dashed',
                borderColor: 'divider',
              }}
            >
              <Typography variant="body2" color="text.secondary">
                No questions added yet
              </Typography>
            </Paper>
          )}
        </Stack>
      </Box>
    </Box>
  );
};

export default SurveyPreview;
