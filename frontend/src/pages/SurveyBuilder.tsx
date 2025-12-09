import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import { v4 as uuidv4 } from 'uuid';
import {
  Box,
  Stepper,
  Step,
  StepLabel,
  Button,
  Container,
  Paper,
  Typography,
  Alert,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  ArrowForward as ArrowForwardIcon,
  Save as SaveIcon,
} from '@mui/icons-material';
import { PageContainer } from '@/components/PageContainer';
import { LoadingSpinner } from '@/components/LoadingSpinner';
import BasicInfoStep from '@/components/SurveyBuilder/BasicInfoStep';
import QuestionsStep from '@/components/SurveyBuilder/QuestionsStep';
import ReviewStep from '@/components/SurveyBuilder/ReviewStep';
import { basicInfoSchema, type BasicInfoFormData } from '@/schemas/surveySchemas';
import type { QuestionDraft } from '@/schemas/questionSchemas';
import surveyService from '@/services/surveyService';
import type { StepConfig, Question } from '@/types';

// Step configuration
const STEPS: StepConfig[] = [
  {
    id: 'basic-info',
    label: 'Basic Info',
    description: 'Survey title and settings',
    isValid: false,
  },
  {
    id: 'questions',
    label: 'Questions',
    description: 'Add and configure questions',
    isValid: false,
  },
  {
    id: 'review',
    label: 'Review & Publish',
    description: 'Review and publish your survey',
    isValid: false,
  },
];

// Helper function to transform backend Question[] to QuestionDraft[]
const transformQuestionsFromBackend = (
  questions: Question[]
): QuestionDraft[] => {
  // Create a mapping of backend question IDs to frontend UUIDs
  const idMapping = new Map<number, string>();
  questions.forEach((q) => {
    idMapping.set(q.id, uuidv4());
  });

  return questions.map((q) => {
    // Parse mediaContent JSON string if present
    let parsedMediaContent: import('@/types/media').MediaContentDto | null = null;
    if (q.mediaContent) {
      try {
        parsedMediaContent = JSON.parse(q.mediaContent);
      } catch {
        console.warn(`Failed to parse mediaContent for question ${q.id}`);
      }
    }

    // Convert defaultNext to defaultNextQuestionId string
    let defaultNextQuestionId: string | null = null;
    if (q.defaultNext) {
      if (q.defaultNext.type === 1) {
        // EndSurvey
        defaultNextQuestionId = null;
      } else if (q.defaultNext.type === 0 && q.defaultNext.nextQuestionId) {
        // GoToQuestion - map to UUID
        const mappedId = idMapping.get(q.defaultNext.nextQuestionId);
        defaultNextQuestionId = mappedId || null;
      }
    }

    // Convert optionDetails to optionNextQuestions mapping
    console.log('[transformQuestionsFromBackend] Processing question:', {
      id: q.id,
      questionType: q.questionType,
      isRating: q.questionType === 3, // QuestionType.Rating
      hasOptionDetails: !!q.optionDetails,
      optionDetailsLength: q.optionDetails?.length ?? 0,
      optionDetails: q.optionDetails,
    });

    let optionNextQuestions: Record<number, string | null> | undefined;
    if (q.optionDetails && q.optionDetails.length > 0) {
      optionNextQuestions = {};
      q.optionDetails.forEach((opt) => {
        if (opt.next) {
          if (opt.next.type === 1) {
            // EndSurvey
            optionNextQuestions![opt.orderIndex] = null;
          } else if (opt.next.type === 0 && opt.next.nextQuestionId) {
            // GoToQuestion - map to UUID
            const mappedId = idMapping.get(opt.next.nextQuestionId);
            optionNextQuestions![opt.orderIndex] = mappedId || null;
          }
        }
      });
    }

    console.log('[transformQuestionsFromBackend] Resulting optionNextQuestions:', {
      id: q.id,
      questionType: q.questionType,
      optionNextQuestions,
      keys: optionNextQuestions ? Object.keys(optionNextQuestions) : 'undefined',
    });

    const draft: QuestionDraft = {
      id: idMapping.get(q.id)!,
      questionText: q.questionText,
      questionType: q.questionType,
      isRequired: q.isRequired,
      options: q.options || [],
      orderIndex: q.orderIndex,
      mediaContent: parsedMediaContent,
      defaultNextQuestionId,
      optionNextQuestions,
    };

    return draft;
  });
};

const SurveyBuilder: React.FC = () => {
  const { id } = useParams<{ id?: string }>();
  const navigate = useNavigate();
  const isEditMode = !!id;

  // State
  const [activeStep, setActiveStep] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [questions, setQuestions] = useState<QuestionDraft[]>([]);

  // Form setup
  const {
    control,
    handleSubmit,
    formState: { errors, isValid },
    watch,
    
    reset,
  } = useForm<BasicInfoFormData>({
    resolver: yupResolver(basicInfoSchema),
    mode: 'onChange',
    defaultValues: {
      title: '',
      description: '',
      allowMultipleResponses: false,
      showResults: true,
    },
  });

  // Watch form values for auto-save
  const formValues = watch();

  // LocalStorage key
  const getDraftKey = () => `survey_draft_${id || 'new'}`;

  // Load draft from localStorage
  useEffect(() => {
    const loadDraft = () => {
      try {
        const draftKey = getDraftKey();
        const savedDraft = localStorage.getItem(draftKey);

        if (savedDraft) {
          const draft = JSON.parse(savedDraft);
          console.log('Loading draft from localStorage:', draft);

          // Restore form values
          reset({
            title: draft.title || '',
            description: draft.description || '',
            allowMultipleResponses: draft.allowMultipleResponses || false,
            showResults: draft.showResults !== undefined ? draft.showResults : true,
          });

          // Restore questions
          if (draft.questions && Array.isArray(draft.questions)) {
            setQuestions(draft.questions);
          }

          // Restore current step
          if (draft.currentStep !== undefined) {
            setActiveStep(draft.currentStep);
          }
        }
      } catch (err) {
        console.error('Failed to load draft:', err);
      }
    };

    loadDraft();
  }, [id, reset]);

  // Load existing survey in edit mode
  useEffect(() => {
    const loadSurvey = async () => {
      if (!isEditMode || !id) return;

      setIsLoading(true);
      setError(null);

      try {
        const survey = await surveyService.getSurveyById(parseInt(id));

        reset({
          title: survey.title,
          description: survey.description || '',
          allowMultipleResponses: survey.allowMultipleResponses,
          showResults: survey.showResults,
        });

        // Check if localStorage draft already has questions (user has unsaved work)
        const draftKey = getDraftKey();
        const savedDraft = localStorage.getItem(draftKey);
        let hasLocalDraftQuestions = false;

        if (savedDraft) {
          try {
            const draft = JSON.parse(savedDraft);
            hasLocalDraftQuestions = draft.questions && Array.isArray(draft.questions) && draft.questions.length > 0;
          } catch {
            // Ignore parse errors
          }
        }

        // Only load questions from API if no local draft exists with questions
        // This preserves user's unsaved work when re-opening the editor
        if (!hasLocalDraftQuestions && survey.questions && survey.questions.length > 0) {
          const transformedQuestions = transformQuestionsFromBackend(survey.questions);
          setQuestions(transformedQuestions);
          console.log('Loaded and transformed questions from API:', transformedQuestions);
        } else if (hasLocalDraftQuestions) {
          console.log('Using existing questions from localStorage draft');
        }

        console.log('Loaded survey for editing:', survey);
      } catch (err: any) {
        console.error('Failed to load survey:', err);
        setError(err.response?.data?.message || 'Failed to load survey');
      } finally {
        setIsLoading(false);
      }
    };

    loadSurvey();
  }, [id, isEditMode, reset]);

  // Auto-save draft to localStorage
  useEffect(() => {
    const saveDraft = () => {
      try {
        const draftKey = getDraftKey();
        const draft = {
          title: formValues.title,
          description: formValues.description,
          allowMultipleResponses: formValues.allowMultipleResponses,
          showResults: formValues.showResults,
          questions: questions,
          currentStep: activeStep,
          lastSaved: new Date().toISOString(),
        };

        localStorage.setItem(draftKey, JSON.stringify(draft));
        console.log('Draft auto-saved to localStorage');
      } catch (err) {
        console.error('Failed to save draft:', err);
      }
    };

    // Debounce auto-save
    const timeoutId = setTimeout(saveDraft, 1000);
    return () => clearTimeout(timeoutId);
  }, [formValues, activeStep, questions, id]);

  // Clear draft from localStorage
  const clearDraft = () => {
    try {
      const draftKey = getDraftKey();
      localStorage.removeItem(draftKey);
      console.log('Draft cleared from localStorage');
    } catch (err) {
      console.error('Failed to clear draft:', err);
    }
  };

  // Handle step navigation
  const handleNext = () => {
    if (activeStep === 0) {
      // Validate basic info before proceeding
      handleSubmit(
        () => {
          setActiveStep((prev) => Math.min(prev + 1, STEPS.length - 1));
        },
        (errors) => {
          console.log('Validation errors:', errors);
        }
      )();
    } else {
      setActiveStep((prev) => Math.min(prev + 1, STEPS.length - 1));
    }
  };

  const handleBack = () => {
    setActiveStep((prev) => Math.max(prev - 1, 0));
  };

  // Handle save draft (manual)
  const handleSaveDraft = async () => {
    setIsSaving(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const formData = formValues;

      if (isEditMode && id) {
        // Update existing survey
        await surveyService.updateSurvey(parseInt(id), {
          title: formData.title,
          description: formData.description || undefined,
          allowMultipleResponses: formData.allowMultipleResponses,
          showResults: formData.showResults,
        });
        setSuccessMessage('Survey updated successfully!');
      } else {
        // Create new survey
        const survey = await surveyService.createSurvey({
          title: formData.title,
          description: formData.description || undefined,
          allowMultipleResponses: formData.allowMultipleResponses,
          showResults: formData.showResults,
        });

        setSuccessMessage('Survey created successfully!');
        clearDraft();

        // Navigate to edit mode with the new survey ID
        setTimeout(() => {
          navigate(`/dashboard/surveys/${survey.id}/edit`, { replace: true });
        }, 1500);
      }
    } catch (err: any) {
      console.error('Failed to save survey:', err);
      setError(err.response?.data?.message || 'Failed to save survey');
    } finally {
      setIsSaving(false);
    }
  };

  // Handle cancel
  const handleCancel = () => {
    if (window.confirm('Are you sure you want to cancel? Unsaved changes will be lost.')) {
      clearDraft();
      navigate('/dashboard/surveys');
    }
  };

  // Handle questions update
  const handleUpdateQuestions = (updatedQuestions: QuestionDraft[]) => {
    setQuestions(updatedQuestions);
  };

  // Handle publish success
  const handlePublishSuccess = (survey: any) => {
    console.log('Survey published successfully:', survey);
    clearDraft();
    // The ReviewStep component will show the success screen
  };

  // Render step content
  const renderStepContent = () => {
    switch (activeStep) {
      case 0:
        return <BasicInfoStep control={control} errors={errors} isLoading={isLoading} />;
      case 1:
        return (
          <QuestionsStep
            questions={questions}
            onUpdateQuestions={handleUpdateQuestions}
            onNext={handleNext}
            onBack={handleBack}
          />
        );
      case 2:
        return (
          <ReviewStep
            surveyData={formValues}
            questions={questions}
            onBack={handleBack}
            onPublishSuccess={handlePublishSuccess}
            isEditMode={isEditMode}
            existingSurveyId={id ? parseInt(id) : undefined}
          />
        );
      default:
        return null;
    }
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading survey..." />;
  }

  return (
    <PageContainer
      title={isEditMode ? 'Edit Survey' : 'Create New Survey'}
      breadcrumbs={[
        { label: 'Dashboard', path: '/dashboard' },
        { label: 'Surveys', path: '/dashboard/surveys' },
        { label: isEditMode ? 'Edit Survey' : 'Create Survey' },
      ]}
    >
      <Container maxWidth="lg">
        <Paper elevation={2} sx={{ p: { xs: 2, sm: 3, md: 4 }, mb: 4 }}>
          {/* Stepper */}
          <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
            {STEPS.map((step) => (
              <Step key={step.id}>
                <StepLabel>
                  <Box>
                    <Typography variant="subtitle2" fontWeight={600}>
                      {step.label}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {step.description}
                    </Typography>
                  </Box>
                </StepLabel>
              </Step>
            ))}
          </Stepper>

          {/* Error Alert */}
          {error && (
            <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {/* Success Alert */}
          {successMessage && (
            <Alert severity="success" onClose={() => setSuccessMessage(null)} sx={{ mb: 3 }}>
              {successMessage}
            </Alert>
          )}

          {/* Step Content */}
          <Box sx={{ minHeight: 400 }}>{renderStepContent()}</Box>

          {/* Navigation Buttons - Hide on Questions Step and Review Step (they have their own) */}
          {activeStep !== 1 && activeStep !== 2 && (
            <Box
              sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                justifyContent: 'space-between',
                gap: 2,
                mt: 4,
                pt: 3,
                borderTop: '1px solid',
                borderColor: 'divider',
              }}
            >
            {/* Left Side - Cancel */}
            <Button
              variant="outlined"
              color="secondary"
              onClick={handleCancel}
              disabled={isSaving}
            >
              Cancel
            </Button>

            {/* Right Side - Navigation */}
            <Box
              sx={{
                display: 'flex',
                gap: 2,
                flexDirection: { xs: 'column-reverse', sm: 'row' },
              }}
            >
              {/* Save Draft Button */}
              <Button
                variant="outlined"
                startIcon={<SaveIcon />}
                onClick={handleSaveDraft}
                disabled={!isValid || isSaving}
              >
                {isSaving ? 'Saving...' : 'Save Draft'}
              </Button>

              {/* Back Button */}
              {activeStep > 0 && (
                <Button
                  variant="outlined"
                  startIcon={<ArrowBackIcon />}
                  onClick={handleBack}
                  disabled={isSaving}
                >
                  Back
                </Button>
              )}

              {/* Next Button */}
              {activeStep < STEPS.length - 1 && (
                <Button
                  variant="contained"
                  endIcon={<ArrowForwardIcon />}
                  onClick={handleNext}
                  disabled={activeStep === 0 && !isValid}
                >
                  Next
                </Button>
              )}
            </Box>
          </Box>
          )}
        </Paper>

        {/* Draft Info */}
        <Alert severity="info" sx={{ mb: 2 }}>
          <Typography variant="caption">
            Your progress is automatically saved to your browser. You can return to this survey
            later to continue editing.
          </Typography>
        </Alert>
      </Container>
    </PageContainer>
  );
};

export default SurveyBuilder;
