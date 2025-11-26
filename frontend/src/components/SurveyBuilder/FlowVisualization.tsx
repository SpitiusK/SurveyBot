import React from 'react';
import {
  Box,
  Typography,
  Paper,
  Stack,
  Chip,
  Alert,
} from '@mui/material';
import {
  ArrowForward as ArrowIcon,
  CheckCircle as EndIcon,
} from '@mui/icons-material';
import type { QuestionDraft } from '@/schemas/questionSchemas';
import { QuestionType } from '@/types';

interface FlowVisualizationProps {
  questions: QuestionDraft[];
}

const FlowVisualization: React.FC<FlowVisualizationProps> = ({ questions }) => {
  // Helper to get question by ID
  const getQuestionById = (id: string | null): QuestionDraft | null => {
    if (!id) return null;
    return questions.find((q) => q.id === id) || null;
  };

  // Helper to strip HTML from question text
  const stripHtml = (html: string): string => {
    return html.replace(/<[^>]*>/g, '');
  };

  // Helper to check if question supports branching
  const supportsBranching = (type: QuestionType): boolean => {
    return type === QuestionType.SingleChoice || type === QuestionType.Rating;
  };

  // Check if flow is configured
  const hasFlowConfiguration = (): boolean => {
    return questions.some(
      (q) =>
        q.defaultNextQuestionId ||
        (q.optionNextQuestions && Object.keys(q.optionNextQuestions).length > 0)
    );
  };

  if (!hasFlowConfiguration()) {
    return (
      <Alert severity="info" sx={{ mt: 2 }}>
        <Typography variant="body2" fontWeight="medium">
          Sequential Flow (No Branching)
        </Typography>
        <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
          Questions will appear in order: Q1 → Q2 → Q3 → ... → End
        </Typography>
      </Alert>
    );
  }

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Survey Flow Diagram
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        This diagram shows how respondents will navigate through your survey based on their
        answers.
      </Typography>

      <Paper variant="outlined" sx={{ p: 2, mt: 2, bgcolor: 'grey.50' }}>
        <Stack spacing={2}>
          {questions.map((question, index) => (
            <Box key={question.id}>
              {/* Question Header */}
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1,
                  p: 1.5,
                  bgcolor: 'background.paper',
                  borderRadius: 1,
                  border: 1,
                  borderColor: 'divider',
                }}
              >
                <Chip
                  label={`Q${index + 1}`}
                  size="small"
                  color="primary"
                  sx={{ fontWeight: 'bold' }}
                />
                <Typography variant="body2" sx={{ flex: 1 }}>
                  {stripHtml(question.questionText).substring(0, 60)}
                  {stripHtml(question.questionText).length > 60 ? '...' : ''}
                </Typography>
                <Chip
                  label={
                    question.questionType === QuestionType.Text
                      ? 'Text'
                      : question.questionType === QuestionType.SingleChoice
                      ? 'SingleChoice'
                      : question.questionType === QuestionType.MultipleChoice
                      ? 'MultipleChoice'
                      : 'Rating'
                  }
                  size="small"
                  variant="outlined"
                />
              </Box>

              {/* Flow Configuration */}
              <Box sx={{ ml: 6, mt: 1 }}>
                {supportsBranching(question.questionType) && question.optionNextQuestions ? (
                  // Branching question - show per-option flow
                  <Stack spacing={1}>
                    {Object.entries(question.optionNextQuestions).map(([optionIndex, nextId]) => {
                      const optionText =
                        question.options?.[parseInt(optionIndex)] || `Option ${parseInt(optionIndex) + 1}`;
                      const nextQuestion = nextId ? getQuestionById(nextId) : null;

                      return (
                        <Box
                          key={optionIndex}
                          sx={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 1,
                            pl: 2,
                            borderLeft: 2,
                            borderColor: 'primary.light',
                          }}
                        >
                          <Typography variant="caption" sx={{ minWidth: 80, color: 'text.secondary' }}>
                            &quot;{optionText}&quot;
                          </Typography>
                          <ArrowIcon fontSize="small" color="action" />
                          {nextQuestion ? (
                            <Chip
                              label={`Q${nextQuestion.orderIndex + 1}`}
                              size="small"
                              color="primary"
                              variant="outlined"
                            />
                          ) : (
                            <Chip
                              label="End Survey"
                              size="small"
                              color="success"
                              variant="outlined"
                              icon={<EndIcon />}
                            />
                          )}
                        </Box>
                      );
                    })}
                  </Stack>
                ) : question.defaultNextQuestionId !== undefined ? (
                  // Non-branching question - show default next
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1,
                      pl: 2,
                      borderLeft: 2,
                      borderColor: 'grey.400',
                    }}
                  >
                    <Typography variant="caption" color="text.secondary">
                      All answers
                    </Typography>
                    <ArrowIcon fontSize="small" color="action" />
                    {(() => {
                      const nextQuestion = getQuestionById(question.defaultNextQuestionId ?? null);
                      return nextQuestion ? (
                        <Chip
                          label={`Q${nextQuestion.orderIndex + 1}`}
                          size="small"
                          color="primary"
                          variant="outlined"
                        />
                      ) : (
                        <Chip
                          label="End Survey"
                          size="small"
                          color="success"
                          variant="outlined"
                          icon={<EndIcon />}
                        />
                      );
                    })()}
                  </Box>
                ) : (
                  // No flow configured - sequential
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1,
                      pl: 2,
                    }}
                  >
                    <Typography variant="caption" color="text.secondary" fontStyle="italic">
                      Sequential (next question in order)
                    </Typography>
                  </Box>
                )}
              </Box>
            </Box>
          ))}
        </Stack>
      </Paper>

      {/* Summary */}
      <Alert severity="success" sx={{ mt: 2 }}>
        <Typography variant="body2">
          Flow configuration complete. Survey will follow these paths based on respondent answers.
        </Typography>
      </Alert>
    </Box>
  );
};

export default FlowVisualization;
