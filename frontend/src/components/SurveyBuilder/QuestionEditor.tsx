import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControl,
  FormControlLabel,
  FormLabel,
  RadioGroup,
  Radio,
  Switch,
  Stack,
  Box,
  Typography,
  Alert,
  AlertTitle,
  Divider,
  Select,
  MenuItem,
  CircularProgress,
} from '@mui/material';
import {
  TextFields as TextIcon,
  RadioButtonChecked as SingleChoiceIcon,
  CheckBox as MultipleChoiceIcon,
  Star as RatingIcon,
  LocationOn as LocationIcon,
  Numbers as NumberIcon,
  CalendarToday as DateIcon,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { QuestionType } from '../../types';
import {
  questionEditorFormSchema,
  type QuestionDraft,
} from '../../schemas/questionSchemas';
import OptionManager from './OptionManager';
import { RichTextEditor } from '../RichTextEditor';
import { MediaGallery } from '../MediaGallery';
import type { MediaContentDto, MediaItemDto } from '../../types/media';
import { stripHtml, stripHtmlAndTruncate } from '../../utils/stringUtils';

interface QuestionEditorProps {
  open: boolean;
  onClose: () => void;
  onSave: (question: QuestionDraft) => void;
  question?: QuestionDraft | null; // For editing existing question
  orderIndex: number; // For new questions
  allQuestions: QuestionDraft[]; // All questions in the survey
}

const QuestionEditor: React.FC<QuestionEditorProps> = ({
  open,
  onClose,
  onSave,
  question,
  orderIndex,
  allQuestions,
}) => {
  const isEditMode = !!question;
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [mediaContent, setMediaContent] = useState<MediaContentDto | undefined>(
    question?.mediaContent || undefined
  );

  const {
    control,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors, isDirty, isValid },
  } = useForm({
    resolver: zodResolver(questionEditorFormSchema),
    mode: 'onChange', // Enable real-time validation
    defaultValues: {
      questionText: question?.questionText || '',
      questionType: question?.questionType ?? QuestionType.Text,
      isRequired: question?.isRequired ?? true,
      options: question?.options || [],
      defaultNextQuestionId: question?.defaultNextQuestionId || null,
      optionNextQuestions: question?.optionNextQuestions || {},
    },
  });

  // stripHtml is now imported from utils/stringUtils

  const questionType = watch('questionType');
  const questionText = watch('questionText');
  const options = watch('options');

  // Debug logging for form state
  useEffect(() => {
    if (open) {
      const optionNextQuestions = watch('optionNextQuestions');
      console.log('Form validation state:', {
        isValid,
        isDirty,
        isSubmitting,
        errorCount: Object.keys(errors).length,
        errors,
        questionTextLength: questionText?.length || 0,
        actualTextLength: stripHtml(questionText || '').length,
        optionNextQuestions: {
          value: optionNextQuestions,
          type: typeof optionNextQuestions,
          isEmptyObject: optionNextQuestions &&
            typeof optionNextQuestions === 'object' &&
            Object.keys(optionNextQuestions).length === 0,
        },
      });
    }
  }, [errors, isValid, isDirty, isSubmitting, questionText, open, watch]);

  useEffect(() => {
    setHasUnsavedChanges(isDirty);
  }, [isDirty]);

  useEffect(() => {
    if (open && question) {
      reset({
        questionText: question.questionText,
        questionType: question.questionType,
        isRequired: question.isRequired,
        options: question.options,
        defaultNextQuestionId: question.defaultNextQuestionId || null,
        optionNextQuestions: question.optionNextQuestions || {},
      });
      setMediaContent(question.mediaContent || undefined);
    } else if (open && !question) {
      reset({
        questionText: '',
        questionType: QuestionType.Text,
        isRequired: true,
        options: [],
        defaultNextQuestionId: null,
        optionNextQuestions: {},
      });
      setMediaContent(undefined);
    }
  }, [open, question, reset]);

  // APPROACH 2: Synchronize optionNextQuestions with options array for SingleChoice
  useEffect(() => {
    if (questionType === QuestionType.SingleChoice && options && options.length > 0) {
      const currentFlow = watch('optionNextQuestions') || {};
      const updatedFlow: Record<number, string | null> = {};

      // Preserve existing values, initialize missing indices to null
      options.forEach((_, index) => {
        updatedFlow[index] = currentFlow[index] !== undefined ? currentFlow[index] : null;
      });

      // Only update if structure changed (different number of keys)
      if (Object.keys(updatedFlow).length !== Object.keys(currentFlow).length) {
        console.log('Synchronizing optionNextQuestions:', {
          before: currentFlow,
          after: updatedFlow,
          optionsCount: options.length,
        });
        setValue('optionNextQuestions', updatedFlow, { shouldValidate: true });
      }
    }
  }, [questionType, options, setValue, watch]);

  const handleClose = () => {
    if (hasUnsavedChanges) {
      if (
        window.confirm(
          'You have unsaved changes. Are you sure you want to close?'
        )
      ) {
        onClose();
        setHasUnsavedChanges(false);
      }
    } else {
      onClose();
    }
  };

  const handleAddMedia = (media: MediaItemDto) => {
    const updatedMedia: MediaContentDto = {
      version: '1.0',
      items: [...(mediaContent?.items || []), media],
    };
    setMediaContent(updatedMedia);
    setHasUnsavedChanges(true);
  };

  const handleRemoveMedia = (mediaId: string) => {
    if (!mediaContent?.items) return;

    const updatedMedia: MediaContentDto = {
      version: '1.0',
      items: mediaContent.items.filter((m) => m.id !== mediaId),
    };
    setMediaContent(updatedMedia);
    setHasUnsavedChanges(true);
  };

  const handleReorderMedia = (items: MediaItemDto[]) => {
    const updatedMedia: MediaContentDto = {
      version: '1.0',
      items: items,
    };
    setMediaContent(updatedMedia);
    setHasUnsavedChanges(true);
  };

  const onSubmit = async (data: any) => {
    console.log('Form submitted with data:', data);
    console.log('Current validation errors:', errors);

    // DEBUG: Log optionNextQuestions before save
    console.log('🔍 DEBUG: optionNextQuestions before save:', {
      value: data.optionNextQuestions,
      type: typeof data.optionNextQuestions,
      keys: Object.keys(data.optionNextQuestions || {}),
      values: Object.values(data.optionNextQuestions || {}),
      entries: Object.entries(data.optionNextQuestions || {}),
      isEmptyObject: data.optionNextQuestions &&
        typeof data.optionNextQuestions === 'object' &&
        Object.keys(data.optionNextQuestions).length === 0,
    });

    try {
      setIsSubmitting(true);

      // Validate that question text has actual content (not just HTML tags)
      const actualText = stripHtml(data.questionText || '');
      if (actualText.length < 5) {
        console.error('Question text validation failed: actual text too short', {
          html: data.questionText,
          actualText,
          actualLength: actualText.length,
        });
        return; // Validation will show error
      }

      const questionDraft: QuestionDraft = {
        id: question?.id || crypto.randomUUID(),
        questionText: data.questionText,
        questionType: data.questionType,
        isRequired: data.isRequired ?? true,
        options:
          data.questionType === QuestionType.SingleChoice ||
          data.questionType === QuestionType.MultipleChoice
            ? data.options || []
            : [],
        orderIndex: question?.orderIndex ?? orderIndex,
        mediaContent: mediaContent || null,
        defaultNextQuestionId: data.defaultNextQuestionId || null,
        optionNextQuestions: data.optionNextQuestions || {},
      };

      console.log('✅ Saving question draft:', questionDraft);
      console.log('✅ Final optionNextQuestions in draft:', questionDraft.optionNextQuestions);
      onSave(questionDraft);
      setHasUnsavedChanges(false);
      onClose();
    } catch (error) {
      console.error('Error saving question:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleQuestionTypeChange = (newType: QuestionType) => {
    setValue('questionType', newType, { shouldDirty: true });

    // Initialize options for choice questions
    if (
      newType === QuestionType.SingleChoice ||
      newType === QuestionType.MultipleChoice
    ) {
      if (!options || options.length === 0) {
        setValue('options', ['', ''], { shouldDirty: true });
      }
    } else {
      setValue('options', [], { shouldDirty: true });
    }

    // Clear conditional flow fields for question types that don't use option-based flow
    if (newType === QuestionType.Text || newType === QuestionType.MultipleChoice || newType === QuestionType.Location || newType === QuestionType.Number || newType === QuestionType.Date) {
      // Text, MultipleChoice, Location, Number, and Date only use defaultNextQuestionId
      setValue('optionNextQuestions', {}, { shouldDirty: true });
    } else if (newType === QuestionType.SingleChoice) {
      // APPROACH 1: Initialize optionNextQuestions with all option indices
      const currentOptions = watch('options') || [];
      if (currentOptions.length > 0) {
        const initialFlow: Record<number, string | null> = {};
        currentOptions.forEach((_, index) => {
          initialFlow[index] = null; // Initialize all to "End Survey"
        });
        console.log('Initializing optionNextQuestions on type change:', {
          questionType: 'SingleChoice',
          optionsCount: currentOptions.length,
          initialFlow,
        });
        setValue('optionNextQuestions', initialFlow, { shouldDirty: true });
      }
    } else if (newType === QuestionType.Rating) {
      // Rating uses defaultNextQuestionId
      setValue('optionNextQuestions', {}, { shouldDirty: true });
    }
  };

  const getQuestionTypeIcon = (type: QuestionType) => {
    switch (type) {
      case QuestionType.Text:
        return <TextIcon />;
      case QuestionType.SingleChoice:
        return <SingleChoiceIcon />;
      case QuestionType.MultipleChoice:
        return <MultipleChoiceIcon />;
      case QuestionType.Rating:
        return <RatingIcon />;
      case QuestionType.Location:
        return <LocationIcon />;
      case QuestionType.Number:
        return <NumberIcon />;
      case QuestionType.Date:
        return <DateIcon />;
    }
  };

  const getQuestionTypeDescription = (type: QuestionType): string => {
    switch (type) {
      case QuestionType.Text:
        return 'Respondents can enter free-form text answers';
      case QuestionType.SingleChoice:
        return 'Respondents can select one option from a list';
      case QuestionType.MultipleChoice:
        return 'Respondents can select multiple options from a list';
      case QuestionType.Rating:
        return 'Respondents can rate on a scale of 1-5 stars';
      case QuestionType.Location:
        return 'Respondents can share their GPS location';
      case QuestionType.Number:
        return 'Respondents can enter a numeric value (integer or decimal)';
      case QuestionType.Date:
        return 'Respondents can enter a date in DD.MM.YYYY format';
    }
  };

  const requiresOptions =
    questionType === QuestionType.SingleChoice ||
    questionType === QuestionType.MultipleChoice;

  // Get list of questions that can be selected as "next question"
  // Exclude current question to prevent self-reference
  const getAvailableNextQuestions = (): QuestionDraft[] => {
    return allQuestions.filter(q => q.id !== question?.id);
  };

  const availableQuestions = getAvailableNextQuestions();

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="md"
      fullWidth
      scroll="paper"
    >
      <DialogTitle>
        {isEditMode ? 'Edit Question' : 'Add New Question'}
      </DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent dividers>
          <Stack spacing={3}>
            {/* Validation Error Display */}
            {Object.keys(errors).length > 0 && (
              <Alert severity="error" sx={{ mb: 2 }}>
                <AlertTitle>Please fix the following errors:</AlertTitle>
                <ul style={{ margin: 0, paddingLeft: '20px' }}>
                  {errors.questionText && (
                    <li>
                      <strong>Question Text:</strong> {errors.questionText.message}
                    </li>
                  )}
                  {errors.options && (
                    <li>
                      <strong>Options:</strong>{' '}
                      {typeof errors.options === 'object' && 'message' in errors.options
                        ? errors.options.message
                        : 'Invalid options configuration'}
                    </li>
                  )}
                  {errors.questionType && (
                    <li>
                      <strong>Question Type:</strong> {errors.questionType.message}
                    </li>
                  )}
                  {Object.entries(errors).map(([field, error]) => {
                    // Skip already displayed errors
                    if (['questionText', 'options', 'questionType'].includes(field)) {
                      return null;
                    }

                    // Provide better field names for display
                    const fieldDisplayName = field === 'optionNextQuestions'
                      ? 'Conditional Flow'
                      : field === 'defaultNextQuestionId'
                      ? 'Next Question'
                      : field;

                    return (
                      <li key={field}>
                        <strong>{fieldDisplayName}:</strong>{' '}
                        {error && typeof error === 'object' && 'message' in error
                          ? String(error.message)
                          : 'Invalid value'}
                      </li>
                    );
                  })}
                </ul>
              </Alert>
            )}

            {/* Question Type Selector */}
            <FormControl component="fieldset">
              <FormLabel component="legend">Question Type</FormLabel>
              <RadioGroup
                value={questionType}
                onChange={(e) =>
                  handleQuestionTypeChange(Number(e.target.value) as QuestionType)
                }
              >
                <Stack spacing={1} sx={{ mt: 1 }}>
                  {[
                    QuestionType.Text,
                    QuestionType.SingleChoice,
                    QuestionType.MultipleChoice,
                    QuestionType.Rating,
                    QuestionType.Location,
                    QuestionType.Number,
                    QuestionType.Date,
                  ].map((type) => (
                    <Box
                      key={type}
                      sx={{
                        border: 1,
                        borderColor:
                          questionType === type
                            ? 'primary.main'
                            : 'divider',
                        borderRadius: 1,
                        p: 1.5,
                        cursor: 'pointer',
                        '&:hover': {
                          borderColor: 'primary.main',
                          bgcolor: 'action.hover',
                        },
                        bgcolor:
                          questionType === type
                            ? 'primary.light'
                            : 'transparent',
                        transition: 'all 0.2s',
                      }}
                      onClick={() => handleQuestionTypeChange(type)}
                    >
                      <FormControlLabel
                        value={type}
                        control={<Radio />}
                        label={
                          <Stack direction="row" spacing={1} alignItems="center">
                            {getQuestionTypeIcon(type)}
                            <Box>
                              <Typography variant="body1" fontWeight="medium">
                                {type === QuestionType.Text && 'Text'}
                                {type === QuestionType.SingleChoice &&
                                  'Single Choice'}
                                {type === QuestionType.MultipleChoice &&
                                  'Multiple Choice'}
                                {type === QuestionType.Rating && 'Rating'}
                                {type === QuestionType.Location && 'Location'}
                                {type === QuestionType.Number && 'Number'}
                                {type === QuestionType.Date && 'Date'}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {getQuestionTypeDescription(type)}
                              </Typography>
                            </Box>
                          </Stack>
                        }
                        sx={{ m: 0, width: '100%' }}
                      />
                    </Box>
                  ))}
                </Stack>
              </RadioGroup>
            </FormControl>

            <Divider />

            {/* Question Text with Rich Editor */}
            <Box>
              <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 500 }}>
                Question Text *
              </Typography>
              <Controller
                name="questionText"
                control={control}
                render={({ field }) => {
                  const actualTextLength = stripHtml(field.value || '').length;
                  const hasError = !!errors.questionText || (field.value && actualTextLength < 5);

                  return (
                    <Box>
                      <RichTextEditor
                        value={field.value}
                        onChange={(content, media) => {
                          console.log('RichTextEditor onChange:', {
                            html: content,
                            actualText: stripHtml(content),
                            actualLength: stripHtml(content).length,
                          });
                          field.onChange(content);
                          setMediaContent(media);
                          setHasUnsavedChanges(true);
                        }}
                        placeholder="Enter your question with optional media..."
                        mediaType="image"
                        acceptedTypes={['image', 'video', 'audio', 'document', 'archive']}
                        initialMedia={mediaContent?.items || []}
                        readOnly={false}
                      />
                      {errors.questionText && (
                        <Typography
                          variant="caption"
                          color="error"
                          sx={{ mt: 1, display: 'block' }}
                        >
                          {errors.questionText.message}
                        </Typography>
                      )}
                      {!errors.questionText && actualTextLength > 0 && actualTextLength < 5 && (
                        <Typography
                          variant="caption"
                          color="error"
                          sx={{ mt: 1, display: 'block' }}
                        >
                          Question text must be at least 5 characters (currently {actualTextLength})
                        </Typography>
                      )}
                      {!hasError && (
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{ mt: 1, display: 'block' }}
                        >
                          {actualTextLength}/500 characters (actual text content)
                        </Typography>
                      )}
                    </Box>
                  );
                }}
              />
            </Box>

            {/* Media Gallery Section */}
            <Divider />
            <Box>
              <MediaGallery
                mediaItems={mediaContent?.items || []}
                onAddMedia={handleAddMedia}
                onRemoveMedia={handleRemoveMedia}
                onReorderMedia={handleReorderMedia}
                mediaType="image"
                acceptedTypes={['image', 'video', 'audio', 'document', 'archive']}
                readOnly={false}
              />
              <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                Attach media (images, videos, audio, documents, or archives) to provide context for your question. Maximum file sizes: Images 10MB, Videos 50MB, Audio 20MB, Documents 25MB, Archives 100MB.
              </Typography>
            </Box>

            {/* Required Toggle */}
            <Controller
              name="isRequired"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  control={<Switch {...field} checked={field.value} />}
                  label="Required question"
                />
              )}
            />

            {/* Options Manager (for choice questions) */}
            {requiresOptions && (
              <>
                <Divider />
                <Box>
                  <Typography variant="h6" gutterBottom>
                    Answer Options
                  </Typography>
                  <Controller
                    name="options"
                    control={control}
                    render={({ field }) => (
                      <OptionManager
                        options={field.value || []}
                        onChange={field.onChange}
                        error={errors.options?.message}
                        helperText="Add answer choices for respondents to select from. Drag to reorder."
                      />
                    )}
                  />
                </Box>
              </>
            )}

            {/* Conditional Flow Configuration for SingleChoice and Rating */}
            {(questionType === QuestionType.SingleChoice || questionType === QuestionType.Rating) && (
              <>
                <Divider />
                <Box>
                  <Typography variant="h6" gutterBottom>
                    Conditional Flow
                  </Typography>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Configure which question to show next based on the respondent's answer.
                  </Typography>

                  {questionType === QuestionType.SingleChoice && options && options.length > 0 ? (
                    <Stack spacing={2} sx={{ mt: 2 }}>
                      {options.map((option, index) => {
                        // Create a safe field name for the nested field
                        const fieldName = `optionNextQuestions.${index}` as const;

                        return (
                          <FormControl key={index} fullWidth>
                            <FormLabel sx={{ mb: 0.5, fontSize: '0.875rem' }}>
                              Next question after "{option || `Option ${index + 1}`}"
                            </FormLabel>
                            <Controller
                              name={fieldName}
                              control={control}
                              render={({ field }) => (
                                <Select
                                  {...field}
                                  value={field.value ?? ''}
                                  onChange={(e) => field.onChange(e.target.value === '' ? null : e.target.value)}
                                  displayEmpty
                                >
                                  <MenuItem value="">
                                    <em>End Survey</em>
                                  </MenuItem>
                                  {availableQuestions.map((q) => (
                                    <MenuItem key={q.id} value={q.id}>
                                      Q{q.orderIndex + 1}: {stripHtmlAndTruncate(q.questionText, 50)}
                                    </MenuItem>
                                  ))}
                                  {availableQuestions.length === 0 && (
                                    <MenuItem disabled>
                                      <em>No other questions available</em>
                                    </MenuItem>
                                  )}
                                </Select>
                              )}
                            />
                          </FormControl>
                        );
                      })}
                    </Stack>
                  ) : questionType === QuestionType.Rating ? (
                    <FormControl fullWidth sx={{ mt: 2 }}>
                      <FormLabel sx={{ mb: 0.5, fontSize: '0.875rem' }}>
                        Next question after any rating
                      </FormLabel>
                      <Controller
                        name="defaultNextQuestionId"
                        control={control}
                        render={({ field }) => (
                          <Select
                            {...field}
                            value={field.value ?? ''}
                            onChange={(e) => field.onChange(e.target.value === '' ? null : e.target.value)}
                            displayEmpty
                          >
                            <MenuItem value="">
                              <em>End Survey</em>
                            </MenuItem>
                            {availableQuestions.map((q) => (
                              <MenuItem key={q.id} value={q.id}>
                                Q{q.orderIndex + 1}: {stripHtmlAndTruncate(q.questionText, 50)}
                              </MenuItem>
                            ))}
                            {availableQuestions.length === 0 && (
                              <MenuItem disabled>
                                <em>No other questions available</em>
                              </MenuItem>
                            )}
                          </Select>
                        )}
                      />
                    </FormControl>
                  ) : null}

                  <Alert severity="info" sx={{ mt: 2 }}>
                    <Typography variant="caption">
                      <strong>Conditional Flow:</strong> Select "End Survey" to complete the survey after this question, or choose the next question to continue the flow.
                      At least one option must lead to "End Survey" for the survey to be valid.
                    </Typography>
                  </Alert>
                </Box>
              </>
            )}

            {/* Conditional Flow for Text, MultipleChoice, Location, Number, and Date */}
            {(questionType === QuestionType.Text || questionType === QuestionType.MultipleChoice || questionType === QuestionType.Location || questionType === QuestionType.Number || questionType === QuestionType.Date) && (
              <>
                <Divider />
                <Box>
                  <Typography variant="h6" gutterBottom>
                    Next Question
                  </Typography>
                  <FormControl fullWidth>
                    <FormLabel sx={{ mb: 0.5, fontSize: '0.875rem' }}>
                      Which question should appear next?
                    </FormLabel>
                    <Controller
                      name="defaultNextQuestionId"
                      control={control}
                      render={({ field }) => (
                        <Select
                          {...field}
                          value={field.value ?? ''}
                          onChange={(e) => field.onChange(e.target.value === '' ? null : e.target.value)}
                          displayEmpty
                        >
                          <MenuItem value="">
                            <em>End Survey</em>
                          </MenuItem>
                          {availableQuestions.map((q) => (
                            <MenuItem key={q.id} value={q.id}>
                              Q{q.orderIndex + 1}: {stripHtmlAndTruncate(q.questionText, 50)}
                            </MenuItem>
                          ))}
                          {availableQuestions.length === 0 && (
                            <MenuItem disabled>
                              <em>No other questions available</em>
                            </MenuItem>
                          )}
                        </Select>
                      )}
                    />
                  </FormControl>
                  <Alert severity="info" sx={{ mt: 2 }}>
                    <Typography variant="caption">
                      <strong>Linear Flow:</strong> All answers to this question will navigate to the selected question or end the survey.
                      Select "End Survey" if this is the last question in your survey.
                    </Typography>
                  </Alert>
                </Box>
              </>
            )}

            {/* Rating Info */}
            {questionType === QuestionType.Rating && (
              <Alert severity="info">
                This question will use a 5-star rating scale (1-5).
                Respondents will select one rating value.
              </Alert>
            )}

            {/* Location Info */}
            {questionType === QuestionType.Location && (
              <Alert severity="info">
                This question will prompt respondents to share their GPS location.
                In the Telegram bot, users will see a "Share Location" button.
              </Alert>
            )}

            {/* Number Info */}
            {questionType === QuestionType.Number && (
              <Alert severity="info">
                This question will accept numeric input (integers or decimals).
                Respondents can enter values with optional decimal places.
                Both period (.) and comma (,) are accepted as decimal separators.
              </Alert>
            )}

            {/* Date Info */}
            {questionType === QuestionType.Date && (
              <Alert severity="info">
                This question will prompt respondents to enter a date in DD.MM.YYYY format.
                The bot will display format instructions and validate the date entered.
                Example: 28.11.2025
              </Alert>
            )}
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button onClick={handleClose} color="inherit" disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            color="primary"
            disabled={isSubmitting || Object.keys(errors).length > 0}
            startIcon={isSubmitting ? <CircularProgress size={20} /> : null}
          >
            {isSubmitting
              ? 'Saving...'
              : isEditMode
              ? 'Update Question'
              : 'Add Question'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default QuestionEditor;
