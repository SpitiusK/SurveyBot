import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControl,
  Select,
  MenuItem,
  TextField,
  Stack,
  Box,
  Typography,
  Alert,
  Divider,
  Chip,
  InputLabel,
} from '@mui/material';
import {
  AccountTree as BranchIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { QuestionType } from '@/types';
import type {
  BranchingRule,
  BranchingOperator,
  BranchingCondition,
  Question,
} from '@/types';

// Validation schema
const branchingRuleSchema = z.object({
  targetQuestionId: z.union([z.number(), z.string()]).refine(
    (val) => {
      if (typeof val === 'number') return val > 0;
      if (typeof val === 'string') return val.length > 0;
      return false;
    },
    'Target question is required'
  ),
  operator: z.enum([
    'Equals',
    'Contains',
    'In',
    'GreaterThan',
    'LessThan',
    'GreaterThanOrEqual',
    'LessThanOrEqual',
  ]),
  value: z.string().optional(),
  values: z.array(z.string()).optional(),
});

type BranchingRuleFormData = z.infer<typeof branchingRuleSchema>;

interface BranchingRuleEditorProps {
  sourceQuestion: Question;
  targetQuestions: Question[]; // All other questions in survey
  onSave: (rule: Partial<BranchingRule>) => Promise<void>;
  onCancel: () => void;
  onDelete?: (ruleId: number) => Promise<void>;
  initialRule?: BranchingRule;
  open: boolean;
}

const BranchingRuleEditor: React.FC<BranchingRuleEditorProps> = ({
  sourceQuestion,
  targetQuestions,
  onSave,
  onCancel,
  onDelete,
  initialRule,
  open,
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isEditMode = !!initialRule;

  const {
    control,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<BranchingRuleFormData>({
    resolver: zodResolver(branchingRuleSchema),
    defaultValues: {
      targetQuestionId: initialRule?.targetQuestionId || '',
      operator: initialRule?.condition.operator || 'Equals',
      value: initialRule?.condition.value || '',
      values: initialRule?.condition.values || [],
    },
  });

  const operator = watch('operator');
  const value = watch('value');
  const values = watch('values');

  useEffect(() => {
    if (open && initialRule) {
      reset({
        targetQuestionId: initialRule.targetQuestionId,
        operator: initialRule.condition.operator,
        value: initialRule.condition.value || '',
        values: initialRule.condition.values || [],
      });
    } else if (open && !initialRule) {
      reset({
        targetQuestionId: '',
        operator: 'Equals',
        value: '',
        values: [],
      });
    }
  }, [open, initialRule, reset]);

  const handleClose = () => {
    setError(null);
    onCancel();
  };

  const handleDelete = async () => {
    if (!initialRule || !onDelete) return;

    if (
      window.confirm('Are you sure you want to delete this branching rule?')
    ) {
      try {
        setLoading(true);
        setError(null);
        await onDelete(initialRule.id);
        handleClose();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to delete rule');
      } finally {
        setLoading(false);
      }
    }
  };

  const onSubmit = async (data: BranchingRuleFormData) => {
    try {
      setLoading(true);
      setError(null);

      // Build condition based on operator
      const condition: BranchingCondition = {
        operator: data.operator,
        questionType: getQuestionTypeName(sourceQuestion.questionType),
      };

      if (data.operator === 'In') {
        condition.values = data.values;
      } else {
        condition.value = data.value;
      }

      const ruleData: Partial<BranchingRule> = {
        sourceQuestionId: sourceQuestion.id,
        targetQuestionId: data.targetQuestionId,
        condition,
      };

      await onSave(ruleData);
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save rule');
    } finally {
      setLoading(false);
    }
  };

  const getQuestionTypeName = (type: QuestionType): string => {
    switch (type) {
      case QuestionType.Text:
        return 'Text';
      case QuestionType.SingleChoice:
        return 'SingleChoice';
      case QuestionType.MultipleChoice:
        return 'MultipleChoice';
      case QuestionType.Rating:
        return 'Rating';
      default:
        return 'Unknown';
    }
  };

  const getOperatorLabel = (op: BranchingOperator): string => {
    switch (op) {
      case 'Equals':
        return 'Equals';
      case 'Contains':
        return 'Contains';
      case 'In':
        return 'Is one of (multiple)';
      case 'GreaterThan':
        return 'Greater than';
      case 'LessThan':
        return 'Less than';
      case 'GreaterThanOrEqual':
        return 'Greater than or equal';
      case 'LessThanOrEqual':
        return 'Less than or equal';
    }
  };

  // Get available operators based on question type
  const getAvailableOperators = (): BranchingOperator[] => {
    switch (sourceQuestion.questionType) {
      case QuestionType.Text:
        return ['Equals', 'Contains'];
      case QuestionType.SingleChoice:
        return ['Equals', 'In'];
      case QuestionType.MultipleChoice:
        return ['Contains', 'In'];
      case QuestionType.Rating:
        return [
          'Equals',
          'GreaterThan',
          'LessThan',
          'GreaterThanOrEqual',
          'LessThanOrEqual',
        ];
      default:
        return ['Equals'];
    }
  };

  const requiresMultipleValues = operator === 'In';
  const requiresSingleValue = !requiresMultipleValues;

  // Get target question name
  const getTargetQuestionName = (id: number | string): string => {
    const question = targetQuestions.find((q) => q.id === id);
    return question
      ? `Q${question.orderIndex + 1}: ${question.questionText.substring(0, 50)}${question.questionText.length > 50 ? '...' : ''}`
      : '';
  };

  // Build rule summary
  const buildRuleSummary = (): string => {
    const targetName = getTargetQuestionName(watch('targetQuestionId'));
    if (!targetName) return '';

    let conditionText = '';
    if (requiresMultipleValues) {
      conditionText = values && values.length > 0 ? values.join(', ') : '(select values)';
    } else {
      conditionText = value || '(enter value)';
    }

    return `If answer ${getOperatorLabel(operator).toLowerCase()} "${conditionText}" → ${targetName}`;
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="md"
      fullWidth
      scroll="paper"
    >
      <DialogTitle>
        <Stack direction="row" spacing={1} alignItems="center">
          <BranchIcon />
          <Typography variant="h6">
            {isEditMode ? 'Edit Branching Rule' : 'Create Branching Rule'}
          </Typography>
        </Stack>
      </DialogTitle>

      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent dividers>
          <Stack spacing={3}>
            {/* Source Question Display */}
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                Source Question
              </Typography>
              <Box
                sx={{
                  p: 2,
                  bgcolor: 'action.hover',
                  borderRadius: 1,
                  border: 1,
                  borderColor: 'divider',
                }}
              >
                <Typography variant="body1" fontWeight="medium">
                  Q{sourceQuestion.orderIndex + 1}: {sourceQuestion.questionText}
                </Typography>
                <Chip
                  label={getQuestionTypeName(sourceQuestion.questionType)}
                  size="small"
                  sx={{ mt: 1 }}
                />
              </Box>
            </Box>

            <Divider />

            {/* Operator Selection */}
            <FormControl fullWidth error={!!errors.operator}>
              <InputLabel id="operator-label">Condition Operator</InputLabel>
              <Controller
                name="operator"
                control={control}
                render={({ field }) => (
                  <Select
                    {...field}
                    labelId="operator-label"
                    label="Condition Operator"
                  >
                    {getAvailableOperators().map((op) => (
                      <MenuItem key={op} value={op}>
                        {getOperatorLabel(op)}
                      </MenuItem>
                    ))}
                  </Select>
                )}
              />
              {errors.operator && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                  {errors.operator.message}
                </Typography>
              )}
            </FormControl>

            {/* Value Input - Single Value */}
            {requiresSingleValue && sourceQuestion.questionType === QuestionType.SingleChoice && sourceQuestion.options && (
              <FormControl fullWidth error={!!errors.value}>
                <InputLabel id="value-label">Answer Value</InputLabel>
                <Controller
                  name="value"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      labelId="value-label"
                      label="Answer Value"
                    >
                      {sourceQuestion.options!.map((option, index) => (
                        <MenuItem key={index} value={option}>
                          {option}
                        </MenuItem>
                      ))}
                    </Select>
                  )}
                />
                {errors.value && (
                  <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                    {errors.value.message}
                  </Typography>
                )}
              </FormControl>
            )}

            {requiresSingleValue && sourceQuestion.questionType !== QuestionType.SingleChoice && (
              <Controller
                name="value"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    fullWidth
                    label="Answer Value"
                    placeholder={
                      sourceQuestion.questionType === QuestionType.Rating
                        ? 'Enter a number (1-5)'
                        : 'Enter the answer value'
                    }
                    type={sourceQuestion.questionType === QuestionType.Rating ? 'number' : 'text'}
                    error={!!errors.value}
                    helperText={errors.value?.message}
                  />
                )}
              />
            )}

            {/* Value Input - Multiple Values */}
            {requiresMultipleValues && sourceQuestion.options && (
              <FormControl fullWidth error={!!errors.values}>
                <InputLabel id="values-label">Answer Values (select multiple)</InputLabel>
                <Controller
                  name="values"
                  control={control}
                  render={({ field }) => (
                    <Select
                      {...field}
                      labelId="values-label"
                      label="Answer Values (select multiple)"
                      multiple
                      renderValue={(selected) => (
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                          {selected.map((value) => (
                            <Chip key={value} label={value} size="small" />
                          ))}
                        </Box>
                      )}
                    >
                      {sourceQuestion.options!.map((option, index) => (
                        <MenuItem key={index} value={option}>
                          {option}
                        </MenuItem>
                      ))}
                    </Select>
                  )}
                />
                {errors.values && (
                  <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                    {errors.values.message}
                  </Typography>
                )}
              </FormControl>
            )}

            <Divider />

            {/* Target Question Selection */}
            <FormControl fullWidth error={!!errors.targetQuestionId}>
              <InputLabel id="target-label">Jump to Question</InputLabel>
              <Controller
                name="targetQuestionId"
                control={control}
                render={({ field }) => (
                  <Select
                    {...field}
                    labelId="target-label"
                    label="Jump to Question"
                    onChange={(e) => {
                      // MUI Select always returns strings, accept both UUID strings and numeric string IDs
                      // Keep values as-is without type conversion to avoid Select value mismatches
                      field.onChange(e.target.value);
                    }}
                  >
                    <MenuItem value={''} disabled>
                      Select target question...
                    </MenuItem>
                    {targetQuestions.map((question) => (
                      <MenuItem key={question.id} value={question.id}>
                        {getTargetQuestionName(question.id)}
                      </MenuItem>
                    ))}
                  </Select>
                )}
              />
              {errors.targetQuestionId && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                  {errors.targetQuestionId.message}
                </Typography>
              )}
            </FormControl>

            {/* Rule Summary */}
            {buildRuleSummary() && (
              <Alert severity="info" icon={<BranchIcon />}>
                <Typography variant="body2" fontWeight="medium">
                  Rule Preview:
                </Typography>
                <Typography variant="body2">{buildRuleSummary()}</Typography>
              </Alert>
            )}

            {/* Error Display */}
            {error && (
              <Alert severity="error" onClose={() => setError(null)}>
                {error}
              </Alert>
            )}
          </Stack>
        </DialogContent>

        <DialogActions>
          {isEditMode && onDelete && (
            <Button
              onClick={handleDelete}
              color="error"
              startIcon={<DeleteIcon />}
              disabled={loading}
              sx={{ mr: 'auto' }}
            >
              Delete
            </Button>
          )}
          <Button onClick={handleClose} color="inherit" disabled={loading}>
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            color="primary"
            disabled={loading}
          >
            {loading ? 'Saving...' : isEditMode ? 'Update Rule' : 'Create Rule'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default BranchingRuleEditor;
