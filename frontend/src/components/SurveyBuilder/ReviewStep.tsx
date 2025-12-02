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
  Chip,
} from '@mui/material';
import {
  Publish as PublishIcon,
  NavigateBefore as BackIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import type { BasicInfoFormData } from '@/schemas/surveySchemas';
import type { QuestionDraft } from '@/schemas/questionSchemas';
import type { Survey, CreateQuestionWithFlowDto, UpdateSurveyWithQuestionsDto, QuestionType } from '@/types';
import { isNonBranchingType, isBranchingType } from '@/types';
import SurveyPreview from './SurveyPreview';
import PublishSuccess from './PublishSuccess';
import FlowVisualization from './FlowVisualization';
import surveyService from '@/services/surveyService';
import { stripHtmlAndTruncate } from '@/utils/stringUtils';

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

  // Helper: Check if survey has at least one endpoint (question leading to end)
  const validateSurveyHasEndpoint = (): boolean => {
    return questions.some((question) => {
      // Check non-branching questions (Text, MultipleChoice, Rating, Location, Number, Date)
      // These use defaultNextQuestionId for flow
      if (isNonBranchingType(question.questionType)) {
        return question.defaultNextQuestionId === null || question.defaultNextQuestionId === '0';
      }

      // Check branching questions (SingleChoice only)
      // These use optionNextQuestions Record for per-option flow
      if (isBranchingType(question.questionType)) {
        if (question.optionNextQuestions) {
          return Object.values(question.optionNextQuestions).some((id) => id === null || id === '0');
        }
      }

      return false;
    });
  };

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

    // NEW: Check for at least one endpoint
    if (questions.length > 0 && !validateSurveyHasEndpoint()) {
      errors.push(
        'Survey must have at least one question that leads to completion. ' +
        'Please ensure the last question or at least one conditional flow option points to "End Survey".'
      );
    }

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

  /**
   * Convert question ID references to index references.
   * @param questionId - The UUID or special marker (null, '0')
   * @param allQuestions - Array of all questions in order
   * @returns Index (0+), -1 (sequential), or null (end survey)
   */
  const convertIdToIndex = (
    questionId: string | null | undefined,
    allQuestions: QuestionDraft[]
  ): number | null => {
    // null or '0' means end survey
    if (questionId === null || questionId === undefined || questionId === '0') {
      return null;
    }

    // Find the index of the question with this UUID
    const index = allQuestions.findIndex((q) => q.id === questionId);

    if (index === -1) {
      console.warn(`Question ID ${questionId} not found in questions array, defaulting to end survey`);
      return null;
    }

    return index;
  };

  const handlePublish = async () => {
    setIsPublishing(true);
    setPublishError(null);

    const startTime = Date.now();

    console.group('🚀 SURVEY PUBLISH STARTED (NEW SINGLE API CALL APPROACH)');
    console.log('Timestamp:', new Date().toISOString());
    console.log('Survey Title:', surveyData.title);
    console.log('Total Questions:', questions.length);
    console.log('Is Edit Mode:', isEditMode);
    console.groupEnd();

    try {
      let survey: Survey;

      if (isEditMode && existingSurveyId) {
        // ========== NEW APPROACH: Single API call with index-based flow ==========

        console.group('📦 Building UpdateSurveyWithQuestionsDto');

        // Build questions with index-based flow references
        const questionsDto: CreateQuestionWithFlowDto[] = questions.map((q, index) => {
          console.group(`Question ${index}`);
          console.log('UUID:', q.id);
          console.log('Text:', q.questionText.substring(0, 50) + '...');
          console.log('Type:', q.questionType);

          // Convert defaultNextQuestionId from UUID to index
          let defaultNextQuestionIndex: number | null | undefined = undefined;

          if (q.defaultNextQuestionId !== undefined) {
            const convertedIndex = convertIdToIndex(q.defaultNextQuestionId, questions);
            defaultNextQuestionIndex = convertedIndex;

            console.log('Default Flow:');
            console.log('  From UUID:', q.defaultNextQuestionId);
            console.log('  To Index:', defaultNextQuestionIndex);
            console.log('  Meaning:', defaultNextQuestionIndex === null ? 'End Survey' : `Go to Q${defaultNextQuestionIndex}`);
          }

          // Convert option flows from UUID to index
          let optionNextQuestionIndexes: Record<number, number | null> | undefined;

          if (q.optionNextQuestions && Object.keys(q.optionNextQuestions).length > 0) {
            optionNextQuestionIndexes = {};

            console.log('Option Flows:');
            Object.entries(q.optionNextQuestions).forEach(([optionIndexStr, nextQuestionUuid]) => {
              const optionIndex = parseInt(optionIndexStr, 10);
              const convertedIndex = convertIdToIndex(nextQuestionUuid, questions);
              optionNextQuestionIndexes![optionIndex] = convertedIndex;

              console.log(`  Option ${optionIndex}:`);
              console.log(`    From UUID: ${nextQuestionUuid}`);
              console.log(`    To Index: ${convertedIndex}`);
              console.log(`    Meaning: ${convertedIndex === null ? 'End Survey' : `Go to Q${convertedIndex}`}`);
            });
          }

          const dto: CreateQuestionWithFlowDto = {
            questionText: q.questionText,
            questionType: q.questionType,
            isRequired: q.isRequired,
            orderIndex: index,
            options: (q.questionType === 1 || q.questionType === 2) ? q.options ?? null : null,
            mediaContent: q.mediaContent ?? null, // Send as object, axios will serialize
            defaultNextQuestionIndex: defaultNextQuestionIndex ?? null,
            optionNextQuestionIndexes: optionNextQuestionIndexes ?? null,
          };

          console.log('Built DTO:', dto);
          console.groupEnd();

          return dto;
        });

        const updateDto: UpdateSurveyWithQuestionsDto = {
          title: surveyData.title,
          description: surveyData.description ?? null,
          allowMultipleResponses: surveyData.allowMultipleResponses,
          showResults: surveyData.showResults,
          activateAfterUpdate: true, // Activate after successful update
          questions: questionsDto,
        };

        console.log('Complete DTO:', updateDto);
        console.groupEnd();

        console.log('📤 Sending UpdateSurveyWithQuestionsDto:', JSON.stringify(updateDto, null, 2));

        console.log('🌐 Calling PUT /api/surveys/:id/complete with single request...');

        survey = await surveyService.updateSurveyComplete(existingSurveyId, updateDto);

        console.log('✅ Survey updated successfully with new question IDs:', survey);
        console.log('   Questions received:', survey.questions.length);
        survey.questions.forEach((q, i) => {
          console.log(`   Q${i}: ID=${q.id}, Text=${q.questionText.substring(0, 30)}...`);
        });

      } else {
        // Create mode: Still use old approach (create survey first, then use complete endpoint)
        console.log('📝 Create mode: Creating survey first...');

        survey = await surveyService.createSurvey({
          title: surveyData.title,
          description: surveyData.description || undefined,
          allowMultipleResponses: surveyData.allowMultipleResponses,
          showResults: surveyData.showResults,
        });

        console.log('✅ Survey created. ID:', survey.id);

        // Now use complete endpoint to add all questions
        const questionsDto: CreateQuestionWithFlowDto[] = questions.map((q, index) => {
          const defaultNextQuestionIndex = q.defaultNextQuestionId !== undefined
            ? convertIdToIndex(q.defaultNextQuestionId, questions)
            : null;

          const optionNextQuestionIndexes = q.optionNextQuestions
            ? Object.fromEntries(
                Object.entries(q.optionNextQuestions).map(([optIdx, uuid]) => [
                  parseInt(optIdx, 10),
                  convertIdToIndex(uuid, questions)
                ])
              )
            : null;

          return {
            questionText: q.questionText,
            questionType: q.questionType,
            isRequired: q.isRequired,
            orderIndex: index,
            options: (q.questionType === 1 || q.questionType === 2) ? q.options ?? null : null,
            mediaContent: q.mediaContent ?? null, // Send as object, axios will serialize
            defaultNextQuestionIndex: defaultNextQuestionIndex ?? null,
            optionNextQuestionIndexes: optionNextQuestionIndexes ?? null,
          };
        });

        const updateDto: UpdateSurveyWithQuestionsDto = {
          title: surveyData.title,
          description: surveyData.description ?? null,
          allowMultipleResponses: surveyData.allowMultipleResponses,
          showResults: surveyData.showResults,
          activateAfterUpdate: true,
          questions: questionsDto,
        };

        console.log('📤 Sending UpdateSurveyWithQuestionsDto:', JSON.stringify(updateDto, null, 2));

        console.log('🌐 Calling PUT /api/surveys/:id/complete for new survey...');
        survey = await surveyService.updateSurveyComplete(survey.id, updateDto);

        console.log('✅ Survey created and activated with questions');
      }

      setPublishedSurvey(survey);

      const duration = Date.now() - startTime;
      console.group('📊 PUBLISH SUMMARY');
      console.log('Total Questions Created:', survey.questions.length);
      console.log('API Calls Made:', 1); // Single call!
      console.log('Duration:', `${duration}ms (${(duration / 1000).toFixed(2)}s)`);
      console.log('Approach:', 'Single atomic transaction with index-based flow');
      console.groupEnd();

      console.log('🎉 SURVEY PUBLISH COMPLETED');

      // Callback
      if (onPublishSuccess) {
        onPublishSuccess(survey);
      }
    } catch (err: any) {
      console.error('❌ SURVEY PUBLISH FAILED');
      console.error('Error:', err);
      console.error('Error Type:', err.constructor.name);
      console.error('Error Message:', err.message);
      console.error('Error Response:', err.response?.data);
      console.error('Error Status:', err.response?.status);
      console.error('Validation Errors:', err.response?.data?.errors);

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

      <Divider sx={{ my: 3 }} />

      {/* Flow Summary for User */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, bgcolor: 'info.50', borderLeft: 4, borderColor: 'info.main' }}>
        <Typography variant="h6" gutterBottom sx={{ color: 'info.main', fontWeight: 600 }}>
          Survey Flow
        </Typography>
        <Box sx={{ mt: 2 }}>
          {questions.map((question, index) => {
            const hasEndpoint = isNonBranchingType(question.questionType)
              ? question.defaultNextQuestionId === null || question.defaultNextQuestionId === '0'
              : isBranchingType(question.questionType) && question.optionNextQuestions
              ? Object.values(question.optionNextQuestions).some((id) => id === null || id === '0')
              : false;

            return (
              <Box key={question.id} sx={{ mb: 1.5 }}>
                <Typography variant="body2" fontWeight={500}>
                  Question {index + 1}: {stripHtmlAndTruncate(question.questionText, 50)}
                </Typography>
                <Box sx={{ ml: 2, mt: 0.5 }}>
                  {isNonBranchingType(question.questionType) ? (
                    // Non-branching
                    <Chip
                      size="small"
                      label={
                        question.defaultNextQuestionId === null || question.defaultNextQuestionId === '0'
                          ? 'Next: End Survey'
                          : `Next: Q${questions.findIndex((q) => q.id === question.defaultNextQuestionId) + 1 || '?'}`
                      }
                      color={question.defaultNextQuestionId === null || question.defaultNextQuestionId === '0' ? 'success' : 'primary'}
                      sx={{ fontSize: '0.75rem' }}
                    />
                  ) : question.questionType === 1 && question.optionNextQuestions ? (
                    // SingleChoice branching
                    <Stack direction="row" spacing={0.5} flexWrap="wrap" sx={{ gap: 0.5 }}>
                      {Object.entries(question.optionNextQuestions).map(([index, nextId]) => {
                        const option = question.options?.[parseInt(index)];
                        return (
                          <Chip
                            key={index}
                            size="small"
                            label={
                              nextId === null || nextId === '0'
                                ? `${option} → End`
                                : `${option} → Q${questions.findIndex((q) => q.id === nextId) + 1 || '?'}`
                            }
                            color={nextId === null || nextId === '0' ? 'success' : 'default'}
                            variant="outlined"
                            sx={{ fontSize: '0.7rem' }}
                          />
                        );
                      })}
                    </Stack>
                  ) : (
                    <Chip
                      size="small"
                      label={
                        question.defaultNextQuestionId === null || question.defaultNextQuestionId === '0'
                          ? 'Next: End Survey'
                          : `Next: Q${questions.findIndex((q) => q.id === question.defaultNextQuestionId) + 1 || '?'}`
                      }
                      color={question.defaultNextQuestionId === null || question.defaultNextQuestionId === '0' ? 'success' : 'primary'}
                      sx={{ fontSize: '0.75rem' }}
                    />
                  )}
                  {hasEndpoint && (
                    <Chip
                      size="small"
                      icon={<PublishIcon sx={{ fontSize: '0.9rem' }} />}
                      label="Survey Endpoint"
                      color="success"
                      variant="outlined"
                      sx={{ ml: 1, fontSize: '0.7rem' }}
                    />
                  )}
                </Box>
              </Box>
            );
          })}
        </Box>
      </Paper>

      {/* Flow Visualization */}
      <FlowVisualization questions={questions} />

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
