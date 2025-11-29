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
import type { Survey, CreateQuestionDto, Question, UpdateQuestionFlowDto, NextStepType } from '@/types';
import { isNonBranchingType, isBranchingType, QuestionType } from '@/types';
import SurveyPreview from './SurveyPreview';
import PublishSuccess from './PublishSuccess';
import FlowVisualization from './FlowVisualization';
import surveyService from '@/services/surveyService';
import questionService from '@/services/questionService';
import questionFlowService from '@/services/questionFlowService';
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
      if (question.questionType === QuestionType.SingleChoice || question.questionType === QuestionType.MultipleChoice) {
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

  const handlePublish = async () => {
    setIsPublishing(true);
    setPublishError(null);

    const startTime = Date.now();

    // ========== LOGGING: SURVEY PUBLISH STARTED ==========
    console.group('🚀 SURVEY PUBLISH STARTED');
    console.log('Timestamp:', new Date().toISOString());
    console.log('Survey Title:', surveyData.title);
    console.log('Total Questions:', questions.length);
    console.log('Questions with Conditional Flow:', questions.filter(q =>
      q.defaultNextQuestionId !== undefined ||
      (q.optionNextQuestions && Object.keys(q.optionNextQuestions).length > 0)
    ).length);
    console.log('Is Edit Mode:', isEditMode);
    console.groupEnd();

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

      console.log('✅ Survey created/updated. ID:', survey.id);

      // Step 2: Create all questions (Two-Pass Approach)
      // PASS 1: Create questions WITHOUT flow configuration
      // This allows us to build a mapping from temporary UUIDs to actual database IDs

      // ========== LOGGING: PASS 1 START ==========
      console.group('📝 PASS 1: Creating Questions (Without Flow)');
      for (const [index, question] of questions.entries()) {
        console.log(`Question ${index + 1}:`, {
          tempId: question.id,
          text: question.questionText.substring(0, 50) + (question.questionText.length > 50 ? '...' : ''),
          type: question.questionType,
          hasOptions: question.options?.length || 0,
          hasDefaultFlow: question.defaultNextQuestionId !== undefined,
          hasOptionFlow: question.optionNextQuestions ? Object.keys(question.optionNextQuestions).length : 0,
        });
      }
      console.groupEnd();

      const questionIdMap = new Map<string, number>(); // UUID -> Database Question ID
      const createdQuestions: Question[] = [];

      for (let i = 0; i < questions.length; i++) {
        const question = questions[i];

        const questionDto: CreateQuestionDto = {
          questionText: question.questionText,
          questionType: question.questionType,
          isRequired: question.isRequired,
          options:
            question.questionType === QuestionType.SingleChoice || question.questionType === QuestionType.MultipleChoice
              ? question.options
              : undefined,
          mediaContent: question.mediaContent
            ? JSON.stringify(question.mediaContent)
            : null,
          // No flow fields in PASS 1
        };

        console.log(`Creating question ${i + 1}/${questions.length} (UUID: ${question.id})`);
        console.log('🔍 DEBUG: About to call questionService.createQuestion...');
        console.log('🔍 DEBUG: surveyId:', survey.id);
        console.log('🔍 DEBUG: questionDto:', questionDto);

        const createdQuestion = await questionService.createQuestion(
          survey.id,
          questionDto
        );

        console.log('🔍 DEBUG: API call completed successfully');
        console.log('🔍 DEBUG: createdQuestion object:', createdQuestion);
        console.log('🔍 DEBUG: createdQuestion.id:', createdQuestion.id);
        console.log('🔍 DEBUG: typeof createdQuestion.id:', typeof createdQuestion.id);

        createdQuestions.push(createdQuestion);

        // Store mapping: UUID → Database ID
        questionIdMap.set(question.id, createdQuestion.id);
        console.log(`  ✓ Created with DB ID: ${createdQuestion.id}`);
      }

      // ========== LOGGING: PASS 1 COMPLETE ==========
      console.group('✅ PASS 1 COMPLETE: Question ID Mapping');
      console.table(Array.from(questionIdMap.entries()).map(([uuid, dbId]) => ({
        UUID: uuid,
        'Database ID': dbId,
      })));
      console.groupEnd();

      // PASS 1.5: Fetch created questions WITH their options to build option ID mapping

      // ========== LOGGING: PASS 1.5 START ==========
      console.group('🔍 PASS 1.5: Fetching Option Database IDs');

      // Fetch all questions for the survey to get OptionDetails
      const questionsWithOptions = await questionService.getQuestionsBySurveyId(survey.id);

      // Build option index → option database ID mapping
      // Structure: Map<questionDbId, Map<optionIndex, optionDbId>>
      const optionMappings = new Map<number, Map<number, number>>();

      questionsWithOptions.forEach((q) => {
        // Check if this question has conditional flow configured in draft
        const questionDraft = questions.find(draft => questionIdMap.get(draft.id) === q.id);

        if (questionDraft?.optionNextQuestions &&
            Object.keys(questionDraft.optionNextQuestions).length > 0) {

          // Build mapping from option indexes to option database IDs
          if (q.optionDetails && q.optionDetails.length > 0) {
            const optionMap = new Map<number, number>();
            q.optionDetails.forEach((opt) => {
              optionMap.set(opt.orderIndex, opt.id);
            });
            optionMappings.set(q.id, optionMap);

            console.log(`Question ${q.id}:`, {
              questionText: q.questionText.substring(0, 30) + '...',
              optionCount: q.optionDetails.length,
              options: q.optionDetails.map((opt, idx) => ({
                index: idx,
                databaseId: opt.id,
                text: opt.text,
              })),
            });
          } else {
            console.warn(`⚠️ Question ${q.id} has flow config but no optionDetails from API`);
          }
        }
      });

      console.groupEnd();
      console.log('✅ PASS 1.5 complete. Option ID mappings built.');

      // PASS 2: Update flow configuration using actual question IDs and option IDs

      // ========== LOGGING: PASS 2 START ==========
      console.group('🔄 PASS 2: UUID → Database ID Transformations');

      let flowUpdateCount = 0;
      let successCount = 0;
      let failCount = 0;

      for (let i = 0; i < questions.length; i++) {
        const question = questions[i];
        const questionDbId = questionIdMap.get(question.id)!;
        const isLastQuestion = i === questions.length - 1;

        console.group(`Question ${i + 1} (UUID: ${question.id.substring(0, 8)}...)`);
        console.log('Database ID:', questionDbId);

        // Convert UUID references to actual database IDs
        let defaultNextQuestionId: number | null | undefined;

        if (question.defaultNextQuestionId !== undefined) {
          console.group('Default Flow Transformation:');
          console.log('Original Value (UUID or marker):', question.defaultNextQuestionId);

          if (question.defaultNextQuestionId === null ||
              question.defaultNextQuestionId === undefined ||
              question.defaultNextQuestionId === '0') {
            // Explicit end survey
            console.log('✅ Explicit end-of-survey marker → Will send 0');
            console.info('ℹ️ End Survey: defaultNextQuestionId = 0 (survey ends after this question)');
            defaultNextQuestionId = 0;
          } else {
            // Convert UUID to actual question ID with validation
            const resolvedId = questionIdMap.get(question.defaultNextQuestionId);
            console.log('UUID Lookup Result:', resolvedId);

            if (resolvedId === undefined) {
              console.error('❌ UUID NOT FOUND in questionIdMap!');
              console.error('Available UUIDs:', Array.from(questionIdMap.keys()));
              console.warn('⚠️ FALLBACK TRIGGERED: UUID not in mapping', {
                missingUuid: question.defaultNextQuestionId,
                availableUuids: Array.from(questionIdMap.keys()),
                fallbackValue: 0,
                reason: 'UUID→ID lookup returned undefined',
              });
              console.log('Fallback: Setting to 0 (end survey)');
              defaultNextQuestionId = 0;
            } else {
              console.log('✅ Resolved to Database ID:', resolvedId);
              defaultNextQuestionId = resolvedId;
            }
          }
          console.groupEnd();
        } else if (isLastQuestion) {
          // Last question defaults to end survey if no explicit flow
          console.log('ℹ️ Last question without explicit flow → Defaulting to end survey (0)');
          defaultNextQuestionId = 0;
        } else {
          // No flow configured, will use sequential flow (undefined = skip update)
          console.log('ℹ️ No default flow configured → Will use sequential flow (undefined)');
          defaultNextQuestionId = undefined;
        }

        if (defaultNextQuestionId === null) {
          console.info('ℹ️ Sequential Flow: defaultNextQuestionId = null (use next question in order)');
        }

        // Convert option-specific flows (for SingleChoice branching)
        // CRITICAL: Use option DATABASE IDs, not option indexes
        let optionNextQuestions: Record<number, number> | undefined;

        if (question.optionNextQuestions && Object.keys(question.optionNextQuestions).length > 0) {
          console.group('Option Flow Transformations:');

          const optionIdMap = optionMappings.get(questionDbId);

          console.log('Option Index → DB ID Mapping:',
            optionIdMap ? Array.from(optionIdMap.entries()).map(([idx, id]) => ({ index: idx, dbId: id })) : 'NOT FOUND'
          );

          if (!optionIdMap) {
            console.error(`❌ No option mapping found for question ${questionDbId}`);
            console.error(`   Question type: ${question.questionType}, Has options: ${question.options?.length || 0}`);
          } else {
            optionNextQuestions = {};

            for (const [optionIndexStr, nextQuestionUuid] of Object.entries(question.optionNextQuestions)) {
              const optionIndex = parseInt(optionIndexStr, 10);

              console.group(`Option ${optionIndex}:`);
              console.log('Option Index:', optionIndex);

              const optionDbId = optionIdMap.get(optionIndex);
              console.log('Option Database ID:', optionDbId || '❌ NOT FOUND');

              if (!optionDbId) {
                console.error('❌ Option index not found in mapping! Skipping this option.');
                console.warn('⚠️ OPTION SKIPPED: Index not in mapping', {
                  questionDbId,
                  optionIndex,
                  availableIndexes: Array.from(optionIdMap.keys()),
                  reason: 'Option index not found in fetched question data',
                });
                console.groupEnd();
                continue;
              }

              // Resolve next question UUID to database ID
              let nextQuestionId: number;
              console.log('Next Question UUID:', nextQuestionUuid);

              if (nextQuestionUuid === null || nextQuestionUuid === '0') {
                // End survey
                nextQuestionId = 0;
                console.log('Next Question DB ID: 0 (end survey)');
              } else {
                // Convert UUID to actual question ID
                const resolvedNextId = questionIdMap.get(nextQuestionUuid);
                if (resolvedNextId === undefined) {
                  console.error(`❌ Invalid next question UUID: ${nextQuestionUuid} not found in questionIdMap`);
                  console.error('Available UUIDs:', Array.from(questionIdMap.keys()));
                  // Default to end survey
                  nextQuestionId = 0;
                  console.log('Fallback: Next Question DB ID: 0 (end survey)');
                } else {
                  nextQuestionId = resolvedNextId;
                  console.log('Next Question DB ID:', nextQuestionId);
                }
              }

              // KEY FIX: Use option database ID, not option index
              optionNextQuestions[optionDbId] = nextQuestionId;
              console.log('✅ Will send:', { optionId: optionDbId, nextQuestionId });

              console.groupEnd();
            }

            console.log('✓ Converted option flows:', optionNextQuestions);
          }

          console.groupEnd();
        }

        // Only update flow if there's something to configure
        if (defaultNextQuestionId !== undefined || optionNextQuestions) {
          flowUpdateCount++;

          // Transform to NextQuestionDeterminant structure
          // Backend expects: type as INTEGER (0 = GoToQuestion, 1 = EndSurvey)
          // Backend expects: optionNextDeterminants as EMPTY OBJECT {}, not null
          // Frontend uses: -1 or 0 = "End Survey", positive integer = question ID, null/undefined = sequential
          const payload: UpdateQuestionFlowDto = {
            defaultNext: defaultNextQuestionId === undefined || defaultNextQuestionId === null ? null : (
              defaultNextQuestionId <= 0
                ? { type: 1 as NextStepType, nextQuestionId: null }  // EndSurvey = 1 (when -1 or 0)
                : { type: 0 as NextStepType, nextQuestionId: defaultNextQuestionId }  // GoToQuestion = 0
            ),
            optionNextDeterminants: optionNextQuestions ? Object.fromEntries(
              Object.entries(optionNextQuestions).map(([optionId, nextId]) => [
                optionId,
                nextId <= 0
                  ? { type: 1 as NextStepType, nextQuestionId: null }  // EndSurvey = 1 (when -1 or 0)
                  : { type: 0 as NextStepType, nextQuestionId: nextId }  // GoToQuestion = 0
              ])
            ) : {},  // Empty object {} instead of null (backend expects Dictionary, not null)
          };

          console.group(`🌐 API REQUEST: Update Flow for Question ${questionDbId}`);
          console.log('Endpoint:', `PUT /api/surveys/${survey.id}/questions/${questionDbId}/flow`);
          console.log('Payload:', {
            defaultNext: payload.defaultNext,
            optionNextDeterminants: payload.optionNextDeterminants,
            _analysis: {
              defaultFlowType: !payload.defaultNext ? 'null (sequential)' :
                                payload.defaultNext.type === 1 ? 'EndSurvey' :
                                `GoToQuestion ${payload.defaultNext.nextQuestionId}`,
              optionFlowCount: payload.optionNextDeterminants ? Object.keys(payload.optionNextDeterminants).length : 0,
              optionFlowDetails: payload.optionNextDeterminants ? Object.entries(payload.optionNextDeterminants).map(([k, v]) => ({
                optionDbId: k,
                next: v,
                flowType: v.type === 1 ? 'end survey' : `question ${v.nextQuestionId}`,
              })) : [],
            },
          });

          try {
            const response = await questionFlowService.updateQuestionFlow(
              survey.id,
              questionDbId,
              payload
            );
            console.log('✅ Response:', response);
            successCount++;
          } catch (error: any) {
            console.error('❌ API Error:', error);
            console.error('Error Details:', {
              status: error.response?.status,
              statusText: error.response?.statusText,
              data: error.response?.data,
              sentPayload: payload,
            });
            failCount++;
            throw error; // Re-throw to stop publish process
          }
          console.groupEnd();

          console.log(`  ✓ Flow updated for question ${questionDbId}`);
        } else {
          console.log(`Skipping flow update for question ${questionDbId} (no flow configured)`);
        }

        console.groupEnd(); // End Question group
      }

      console.groupEnd(); // End PASS 2 group
      console.log('✅ PASS 2 complete. All conditional flows configured.');

      // Step 3: Activate the survey
      const activatedSurvey = await surveyService.activateSurvey(survey.id);

      // Step 4: Get full survey details with questions
      const fullSurvey = await surveyService.getSurveyById(activatedSurvey.id);

      setPublishedSurvey(fullSurvey);

      // ========== LOGGING: PUBLISH SUMMARY ==========
      const duration = Date.now() - startTime;
      console.group('📊 PUBLISH SUMMARY');
      console.log('Total Questions Created:', createdQuestions.length);
      console.log('Questions with Flow Configuration:', flowUpdateCount);
      console.log('Flow Updates Successful:', successCount);
      console.log('Flow Updates Failed:', failCount);
      console.log('Duration:', `${duration}ms (${(duration / 1000).toFixed(2)}s)`);
      console.groupEnd();

      console.log('🎉 SURVEY PUBLISH COMPLETED');

      // Callback
      if (onPublishSuccess) {
        onPublishSuccess(fullSurvey);
      }
    } catch (err: any) {
      console.error('❌ SURVEY PUBLISH FAILED');
      console.error('Error:', err);
      console.error('Error Type:', err.constructor.name);
      console.error('Error Message:', err.message);
      console.error('Error Response:', err.response?.data);
      console.error('Error Status:', err.response?.status);
      console.error('Validation Errors:', err.response?.data?.errors);
      console.error('Full Error Object:', JSON.stringify(err.response?.data, null, 2));

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
