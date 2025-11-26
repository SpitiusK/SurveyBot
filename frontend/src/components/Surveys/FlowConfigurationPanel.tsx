import { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  Alert,
  CircularProgress,
  Divider,
  Stack,
  Chip,
  type SelectChangeEvent,
} from '@mui/material';
import {
  Save as SaveIcon,
  Cancel as CancelIcon,
  Delete as DeleteIcon,
  CheckCircle as CheckIcon,
} from '@mui/icons-material';
import questionFlowService from '@/services/questionFlowService';
import type {
  Question,
  ConditionalFlowDto,
  UpdateQuestionFlowDto,
} from '@/types';

interface FlowConfigurationPanelProps {
  surveyId: number;
  question: Question;
  allQuestions: Question[];
  onFlowUpdated: () => void;
}

/**
 * FlowConfigurationPanel Component
 *
 * Configures conditional question flow (branching logic).
 *
 * Features:
 * - Branching questions (SingleChoice, Rating): Set next question per option
 * - Non-branching questions (Text, MultipleChoice): Set default next question
 * - Cycle detection and validation
 * - Support for "End Survey" option
 *
 * @param surveyId - The survey ID
 * @param question - The question to configure
 * @param allQuestions - All questions in the survey (for selector)
 * @param onFlowUpdated - Callback when flow is updated successfully
 */
export default function FlowConfigurationPanel({
  surveyId,
  question,
  allQuestions,
  onFlowUpdated,
}: FlowConfigurationPanelProps) {
  const [flowConfig, setFlowConfig] = useState<ConditionalFlowDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Local state for flow configuration
  const [defaultNextQuestionId, setDefaultNextQuestionId] = useState<number | null>(null);
  const [optionFlows, setOptionFlows] = useState<Record<number, number | null>>({});

  const isBranchingQuestion =
    question.questionType === 1 || question.questionType === 3; // SingleChoice or Rating

  // Load flow configuration on mount or when question changes
  useEffect(() => {
    loadFlowConfig();
  }, [question.id]);

  const loadFlowConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      const config = await questionFlowService.getQuestionFlow(surveyId, question.id);
      setFlowConfig(config);

      // Initialize local state
      const defaultNext = config.defaultNext;
      setDefaultNextQuestionId(
        !defaultNext ? null :
        defaultNext.type === 'EndSurvey' ? -1 :
        defaultNext.nextQuestionId ?? null
      );

      const flows: Record<number, number | null> = {};
      config.optionFlows.forEach((optionFlow) => {
        const next = optionFlow.next;
        flows[optionFlow.optionId] = !next ? null :
          next.type === 'EndSurvey' ? -1 :
          next.nextQuestionId ?? null;
      });
      setOptionFlows(flows);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load flow configuration');
    } finally {
      setLoading(false);
    }
  };

  const handleDefaultNextQuestionChange = (event: SelectChangeEvent<number>) => {
    const value = event.target.value as number;
    setDefaultNextQuestionId(value === -1 ? -1 : value);
  };

  const handleOptionFlowChange = (optionId: number, nextQuestionId: number) => {
    setOptionFlows((prev) => ({
      ...prev,
      [optionId]: nextQuestionId === -1 ? -1 : nextQuestionId,
    }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError(null);
      setSuccessMessage(null);

      const dto: UpdateQuestionFlowDto = {};

      if (isBranchingQuestion) {
        // For branching questions, save option flows (filter out null values)
        const filteredFlows: Record<number, import('@/types').NextQuestionDeterminant> = {};
        Object.entries(optionFlows).forEach(([key, value]) => {
          if (value !== null) {
            filteredFlows[parseInt(key)] = value === -1
              ? { type: 'EndSurvey' }
              : { type: 'GoToQuestion', questionId: value };
          }
        });
        dto.optionNextDeterminants = filteredFlows;
      } else {
        // For non-branching questions, save default next question
        dto.defaultNext = defaultNextQuestionId === null ? null :
          defaultNextQuestionId === -1
            ? { type: 'EndSurvey' }
            : { type: 'GoToQuestion', questionId: defaultNextQuestionId };
      }

      await questionFlowService.updateQuestionFlow(surveyId, question.id, dto);
      setSuccessMessage('Flow configuration saved successfully!');
      onFlowUpdated();

      // Reload config to get updated data
      await loadFlowConfig();
    } catch (err: any) {
      if (err.response?.data?.message) {
        setError(err.response.data.message);
      } else {
        setError(err instanceof Error ? err.message : 'Failed to save flow configuration');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    // Reset to loaded values
    if (flowConfig) {
      const defaultNext = flowConfig.defaultNext;
      setDefaultNextQuestionId(
        !defaultNext ? null :
        defaultNext.type === 'EndSurvey' ? -1 :
        defaultNext.nextQuestionId ?? null
      );

      const flows: Record<number, number | null> = {};
      flowConfig.optionFlows.forEach((optionFlow) => {
        const next = optionFlow.next;
        flows[optionFlow.optionId] = !next ? null :
          next.type === 'EndSurvey' ? -1 :
          next.nextQuestionId ?? null;
      });
      setOptionFlows(flows);
    }
    setError(null);
    setSuccessMessage(null);
  };

  const handleDelete = async () => {
    if (!window.confirm('Are you sure you want to remove all flow configuration for this question?')) {
      return;
    }

    try {
      setSaving(true);
      setError(null);
      await questionFlowService.deleteQuestionFlow(surveyId, question.id);
      setSuccessMessage('Flow configuration removed successfully!');
      onFlowUpdated();
      await loadFlowConfig();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete flow configuration');
    } finally {
      setSaving(false);
    }
  };

  // Filter out current question from available next questions
  const availableNextQuestions = allQuestions.filter((q) => q.id !== question.id);

  if (loading) {
    return (
      <Card sx={{ mt: 2 }}>
        <CardContent>
          <Box display="flex" justifyContent="center" alignItems="center" minHeight={200}>
            <CircularProgress />
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ mt: 2 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">Conditional Flow Configuration</Typography>
          <Chip
            label={isBranchingQuestion ? 'Branching Question' : 'Non-Branching Question'}
            color={isBranchingQuestion ? 'primary' : 'default'}
            size="small"
          />
        </Box>

        <Typography variant="body2" color="text.secondary" mb={2}>
          {isBranchingQuestion
            ? 'Configure which question to show next based on the selected option.'
            : 'Configure which question to show next after this question.'}
        </Typography>

        <Divider sx={{ mb: 2 }} />

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {successMessage && (
          <Alert
            severity="success"
            sx={{ mb: 2 }}
            icon={<CheckIcon />}
            onClose={() => setSuccessMessage(null)}
          >
            {successMessage}
          </Alert>
        )}

        {isBranchingQuestion ? (
          // Branching questions: Show option-specific next question selectors
          <Stack spacing={2}>
            {flowConfig?.optionFlows.map((optionFlow, index) => (
              <FormControl key={optionFlow.optionId} fullWidth>
                <InputLabel>
                  Option {index + 1}: "{optionFlow.optionText}"
                </InputLabel>
                <Select
                  value={optionFlows[optionFlow.optionId] ?? ''}
                  onChange={(e) =>
                    handleOptionFlowChange(optionFlow.optionId, e.target.value as number)
                  }
                  label={`Option ${index + 1}: "${optionFlow.optionText}"`}
                >
                  <MenuItem value="">
                    <em>No next question (use default order)</em>
                  </MenuItem>
                  <MenuItem value={-1}>
                    <strong>End Survey</strong>
                  </MenuItem>
                  <Divider />
                  {availableNextQuestions.map((q) => (
                    <MenuItem key={q.id} value={q.id}>
                      Q{q.orderIndex + 1}: {q.questionText}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            ))}

            {(!flowConfig?.optionFlows || flowConfig.optionFlows.length === 0) && (
              <Alert severity="info">
                This question has no options configured yet. Add options to the question first.
              </Alert>
            )}
          </Stack>
        ) : (
          // Non-branching questions: Show single default next question selector
          <FormControl fullWidth>
            <InputLabel>Next Question</InputLabel>
            <Select
              value={defaultNextQuestionId ?? ''}
              onChange={handleDefaultNextQuestionChange}
              label="Next Question"
            >
              <MenuItem value="">
                <em>No next question (use default order)</em>
              </MenuItem>
              <MenuItem value={-1}>
                <strong>End Survey</strong>
              </MenuItem>
              <Divider />
              {availableNextQuestions.map((q) => (
                <MenuItem key={q.id} value={q.id}>
                  Q{q.orderIndex + 1}: {q.questionText}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}

        <Box mt={3} display="flex" gap={2} justifyContent="flex-end">
          <Button
            variant="outlined"
            startIcon={<CancelIcon />}
            onClick={handleReset}
            disabled={saving}
          >
            Reset
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<DeleteIcon />}
            onClick={handleDelete}
            disabled={saving}
          >
            Remove Flow
          </Button>
          <Button
            variant="contained"
            startIcon={<SaveIcon />}
            onClick={handleSave}
            disabled={saving}
          >
            {saving ? 'Saving...' : 'Save Flow'}
          </Button>
        </Box>
      </CardContent>
    </Card>
  );
}
