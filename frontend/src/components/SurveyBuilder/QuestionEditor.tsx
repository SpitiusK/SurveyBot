import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
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
  Divider,
} from '@mui/material';
import {
  TextFields as TextIcon,
  RadioButtonChecked as SingleChoiceIcon,
  CheckBox as MultipleChoiceIcon,
  Star as RatingIcon,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { QuestionType } from '../../types';
import {
  questionEditorFormSchema,
  type QuestionDraft,
} from '../../schemas/questionSchemas';
import OptionManager from './OptionManager';

interface QuestionEditorProps {
  open: boolean;
  onClose: () => void;
  onSave: (question: QuestionDraft) => void;
  question?: QuestionDraft | null; // For editing existing question
  orderIndex: number; // For new questions
}

const QuestionEditor: React.FC<QuestionEditorProps> = ({
  open,
  onClose,
  onSave,
  question,
  orderIndex,
}) => {
  const isEditMode = !!question;
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  const {
    control,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors, isDirty },
  } = useForm({
    resolver: zodResolver(questionEditorFormSchema),
    defaultValues: {
      questionText: question?.questionText || '',
      questionType: question?.questionType ?? QuestionType.Text,
      isRequired: question?.isRequired ?? true,
      options: question?.options || [],
    },
  });

  const questionType = watch('questionType');
  const questionText = watch('questionText');
  const options = watch('options');

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
      });
    } else if (open && !question) {
      reset({
        questionText: '',
        questionType: QuestionType.Text,
        isRequired: true,
        options: [],
      });
    }
  }, [open, question, reset]);

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

  const onSubmit = (data: any) => {
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
    };

    onSave(questionDraft);
    setHasUnsavedChanges(false);
    onClose();
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
    }
  };

  const requiresOptions =
    questionType === QuestionType.SingleChoice ||
    questionType === QuestionType.MultipleChoice;

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

            {/* Question Text */}
            <Controller
              name="questionText"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Question Text"
                  placeholder="Enter your question here..."
                  multiline
                  rows={3}
                  fullWidth
                  required
                  error={!!errors.questionText}
                  helperText={
                    errors.questionText?.message ||
                    `${questionText.length}/500 characters`
                  }
                  inputProps={{ maxLength: 500 }}
                />
              )}
            />

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

            {/* Rating Info */}
            {questionType === QuestionType.Rating && (
              <Alert severity="info">
                This question will use a 5-star rating scale (1-5).
                Respondents will select one rating value.
              </Alert>
            )}
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button onClick={handleClose} color="inherit">
            Cancel
          </Button>
          <Button type="submit" variant="contained" color="primary">
            {isEditMode ? 'Update Question' : 'Add Question'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default QuestionEditor;
