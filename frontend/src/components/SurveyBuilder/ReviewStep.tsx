import React, { useState } from 'react';
import {
  Box,
  Button,
  Stack,
  Typography,
  Paper,
  Alert,
  CircularProgress,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  Publish as PublishIcon,
  NavigateBefore as BackIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import type { BasicInfoFormData } from '@/schemas/surveySchemas';
import type { QuestionDraft } from '@/schemas/questionSchemas';
import type { Survey, CreateQuestionDto } from '@/types';
import SurveyPreview from './SurveyPreview';
import PublishSuccess from './PublishSuccess';
import surveyService from '@/services/surveyService';
import questionService from '@/services/questionService';

interface ReviewStepProps {
  surveyData: BasicInfoFormData;
  questions: QuestionDraft[];
  onBack: () => void;
  onPublishSuccess?: (survey: Survey) => void;
  isEditMode?: boolean;
  existingSurveyId?: number;
}

const ReviewStep: React.FC<ReviewStepProps> = ({
  surveyData,
  questions,
  onBack,
  onPublishSuccess,
  isEditMode = false,
  existingSurveyId,
}) => {
  const [isPublishing, setIsPublishing] = useState(false);
  const [publishError, setPublishError] = useState<string | null>(null);
  const [publishedSurvey, setPublishedSurvey] = useState<Survey | null>(null);
  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);

  // Validation
  const hasValidationErrors = (): string[] => {
    const errors: string[] = [];

    if (!surveyData.title || surveyData.title.trim().length < 3) {
      errors.push('Survey title must be at least 3 characters');
    }

    if (questions.length === 0) {
      errors.push('At least one question is required');
    }

    questions.forEach((question, index) => {
      if (!question.questionText || question.questionText.trim().length < 5) {
        errors.push(`Question ${index + 1}: Question text must be at least 5 characters`);
      }

      // Validate choice questions have options
      if (question.questionType === 1 || question.questionType === 2) {
        if (!question.options || question.options.length < 2) {
          errors.push(`Question ${index + 1}: Choice questions must have at least 2 options`);
        }
      }
    });

    return errors;
  };

  const validationErrors = hasValidationErrors();
  const canPublish = validationErrors.length === 0;

  const handlePublishClick = () => {
    if (!canPublish) {
      setPublishError('Please fix validation errors before publishing');
      return;
    }
    setConfirmDialogOpen(true);
  };

  const handleConfirmPublish = async () => {
    setConfirmDialogOpen(false);
    await handlePublish();
  };

  const handlePublish = async () => {
    setIsPublishing(true);
    setPublishError(null);

    try {
      let survey: Survey;

      // Step 1: Create or update the survey
      if (isEditMode && existingSurveyId) {
        // Update existing survey
        survey = await surveyService.updateSurvey(existingSurveyId, {
          title: surveyData.title,
          description: surveyData.description || undefined,
          allowMultipleResponses: surveyData.allowMultipleResponses,
          showResults: surveyData.showResults,
        });
      } else {
        // Create new survey
        survey = await surveyService.createSurvey({
          title: surveyData.title,
          description: surveyData.description || undefined,
          allowMultipleResponses: surveyData.allowMultipleResponses,
          showResults: surveyData.showResults,
        });
      }

      // Step 2: Create all questions
      const createdQuestions = [];
      for (const question of questions) {
        const questionDto: CreateQuestionDto = {
          questionText: question.questionText,
          questionType: question.questionType,
          isRequired: question.isRequired,
          options:
            question.questionType === 1 || question.questionType === 2
              ? question.options
              : undefined,
        };

        const createdQuestion = await questionService.createQuestion(
          survey.id,
          questionDto
        );
        createdQuestions.push(createdQuestion);
      }

      // Step 3: Activate the survey
      const activatedSurvey = await surveyService.activateSurvey(survey.id);

      // Step 4: Get full survey details with questions
      const fullSurvey = await surveyService.getSurveyById(activatedSurvey.id);

      setPublishedSurvey(fullSurvey);

      // Callback
      if (onPublishSuccess) {
        onPublishSuccess(fullSurvey);
      }
    } catch (err: any) {
      console.error('Failed to publish survey:', err);
      const errorMessage =
        err.response?.data?.message ||
        err.message ||
        'Failed to publish survey. Please try again.';
      setPublishError(errorMessage);
    } finally {
      setIsPublishing(false);
    }
  };

  // If successfully published, show success screen
  if (publishedSurvey) {
    return <PublishSuccess survey={publishedSurvey} />;
  }

  return (
    <Box>
      {/* Step Description */}
      <Paper
        elevation={0}
        sx={{
          p: 3,
          mb: 3,
          backgroundColor: 'success.50',
          borderLeft: 4,
          borderColor: 'success.main',
        }}
      >
        <Typography variant="h6" gutterBottom sx={{ color: 'success.main', fontWeight: 600 }}>
          Review & Publish
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Review your survey details and questions below. Once you publish, the survey will be
          activated and ready to receive responses. You can still edit it later if needed.
        </Typography>
      </Paper>

      {/* Validation Errors */}
      {validationErrors.length > 0 && (
        <Alert severity="error" icon={<WarningIcon />} sx={{ mb: 3 }}>
          <Typography variant="body2" fontWeight={600} gutterBottom>
            Please fix the following errors before publishing:
          </Typography>
          <ul style={{ margin: '0.5rem 0 0 0', paddingLeft: '1.5rem' }}>
            {validationErrors.map((error, index) => (
              <li key={index}>
                <Typography variant="body2">{error}</Typography>
              </li>
            ))}
          </ul>
        </Alert>
      )}

      {/* Publish Error */}
      {publishError && (
        <Alert severity="error" onClose={() => setPublishError(null)} sx={{ mb: 3 }}>
          {publishError}
        </Alert>
      )}

      {/* Survey Preview */}
      <SurveyPreview surveyData={surveyData} questions={questions} />

      <Divider sx={{ my: 4 }} />

      {/* Navigation Buttons */}
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        justifyContent="space-between"
        alignItems="center"
      >
        <Button
          variant="outlined"
          startIcon={<BackIcon />}
          onClick={onBack}
          disabled={isPublishing}
          size="large"
        >
          Back to Questions
        </Button>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="center">
          {!canPublish && (
            <Typography variant="caption" color="error" sx={{ textAlign: 'center' }}>
              Please fix validation errors
            </Typography>
          )}
          <Button
            variant="contained"
            color="success"
            size="large"
            startIcon={
              isPublishing ? <CircularProgress size={20} color="inherit" /> : <PublishIcon />
            }
            onClick={handlePublishClick}
            disabled={!canPublish || isPublishing}
            sx={{ minWidth: 200 }}
          >
            {isPublishing ? 'Publishing...' : isEditMode ? 'Update & Activate' : 'Publish Survey'}
          </Button>
        </Stack>
      </Stack>

      {/* Publishing Info */}
      {!isPublishing && canPublish && (
        <Alert severity="info" sx={{ mt: 3 }}>
          <Typography variant="body2" fontWeight={600} gutterBottom>
            What happens when you publish?
          </Typography>
          <Typography variant="caption" component="div">
            • Your survey will be created and activated
            <br />
            • A unique survey code will be generated
            <br />
            • Respondents can start taking the survey immediately
            <br />• You can deactivate or edit the survey anytime
          </Typography>
        </Alert>
      )}

      {/* Confirm Dialog */}
      <Dialog
        open={confirmDialogOpen}
        onClose={() => setConfirmDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Stack direction="row" spacing={1} alignItems="center">
            <PublishIcon color="success" />
            <Typography variant="h6" fontWeight={600}>
              Confirm Publish
            </Typography>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Typography variant="body1" gutterBottom>
            Are you ready to publish your survey?
          </Typography>
          <Box sx={{ mt: 2, p: 2, backgroundColor: 'grey.100', borderRadius: 1 }}>
            <Typography variant="body2" fontWeight={600}>
              {surveyData.title}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {questions.length} question{questions.length !== 1 ? 's' : ''}
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
            Once published, your survey will be active and ready to receive responses. You can
            still edit or deactivate it later if needed.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ p: 2.5 }}>
          <Button onClick={() => setConfirmDialogOpen(false)} variant="outlined">
            Cancel
          </Button>
          <Button
            onClick={handleConfirmPublish}
            variant="contained"
            color="success"
            startIcon={<PublishIcon />}
            autoFocus
          >
            Publish Now
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ReviewStep;
