import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Stack,
  Typography,
  Paper,
  Alert,
  Chip,
  Divider,
} from '@mui/material';
import {
  Add as AddIcon,
  NavigateNext as NextIcon,
  NavigateBefore as BackIcon,
  TextFields as TextIcon,
  RadioButtonChecked as SingleChoiceIcon,
  CheckBox as MultipleChoiceIcon,
  Star as RatingIcon,
} from '@mui/icons-material';
import { QuestionType, isNonBranchingType, isBranchingType } from '../../types';
import type { QuestionDraft } from '../../schemas/questionSchemas';
import QuestionEditor from './QuestionEditor';
import QuestionList from './QuestionList';

interface QuestionsStepProps {
  questions: QuestionDraft[];
  onUpdateQuestions: (questions: QuestionDraft[]) => void;
  onNext: () => void;
  onBack: () => void;
}

const QuestionsStep: React.FC<QuestionsStepProps> = ({
  questions,
  onUpdateQuestions,
  onNext,
  onBack,
}) => {
  const [editorOpen, setEditorOpen] = useState(false);
  const [editingQuestion, setEditingQuestion] = useState<QuestionDraft | null>(
    null
  );
  const [validationError, setValidationError] = useState<string>('');

  useEffect(() => {
    // Clear validation error when questions change
    if (questions.length > 0) {
      setValidationError('');
    }
  }, [questions]);

  const handleAddQuestion = () => {
    setEditingQuestion(null);
    setEditorOpen(true);
  };

  const handleEditQuestion = (question: QuestionDraft) => {
    setEditingQuestion(question);
    setEditorOpen(true);
  };

  const handleSaveQuestion = (question: QuestionDraft) => {
    if (editingQuestion) {
      // Update existing question
      const updatedQuestions = questions.map((q) =>
        q.id === question.id ? question : q
      );
      onUpdateQuestions(updatedQuestions);
    } else {
      // Add new question
      const newQuestion = {
        ...question,
        orderIndex: questions.length,
      };
      onUpdateQuestions([...questions, newQuestion]);
    }
    setEditorOpen(false);
    setEditingQuestion(null);
  };

  const handleDeleteQuestion = (questionId: string) => {
    const updatedQuestions = questions
      .filter((q) => q.id !== questionId)
      .map((q, index) => ({
        ...q,
        orderIndex: index,
      }));
    onUpdateQuestions(updatedQuestions);
  };

  const handleReorderQuestions = (reorderedQuestions: QuestionDraft[]) => {
    onUpdateQuestions(reorderedQuestions);
  };

  // Helper: Ensure last question ends survey (if no conditional flow configured)
  const ensureLastQuestionEndsSurvey = (questionList: QuestionDraft[]): QuestionDraft[] => {
    if (questionList.length === 0) return questionList;

    const updated = [...questionList];
    const lastQuestion = updated[updated.length - 1];

    // Check if last question already has conditional flow configured
    const hasConditionalFlow = isBranchingType(lastQuestion.questionType)
        ? lastQuestion.optionNextQuestions && Object.keys(lastQuestion.optionNextQuestions).length > 0
        : lastQuestion.defaultNextQuestionId !== undefined;

    // If no conditional flow is configured, ensure last question ends survey
    if (!hasConditionalFlow) {
      if (isNonBranchingType(lastQuestion.questionType)) {
        // Non-branching: set defaultNextQuestionId = null
        lastQuestion.defaultNextQuestionId = null;
      } else if (isBranchingType(lastQuestion.questionType)) {
        // Branching: set first option to end survey if no options configured
        if (!lastQuestion.optionNextQuestions || Object.keys(lastQuestion.optionNextQuestions).length === 0) {
          lastQuestion.defaultNextQuestionId = null;
        }
      }
    }

    return updated;
  };

  const handleNext = () => {
    if (questions.length === 0) {
      setValidationError('Please add at least one question before proceeding.');
      return;
    }

    // Auto-fix: ensure last question ends survey
    const fixedQuestions = ensureLastQuestionEndsSurvey(questions);
    if (JSON.stringify(fixedQuestions) !== JSON.stringify(questions)) {
      onUpdateQuestions(fixedQuestions);
    }

    setValidationError('');
    onNext();
  };

  const getQuestionTypeCounts = () => {
    const counts: Record<string, number> = {
      Text: 0,
      SingleChoice: 0,
      MultipleChoice: 0,
      Rating: 0,
      Location: 0,
      Number: 0,
      Date: 0,
    };

    questions.forEach((q) => {
      switch (q.questionType) {
        case QuestionType.Text:
          counts.Text++;
          break;
        case QuestionType.SingleChoice:
          counts.SingleChoice++;
          break;
        case QuestionType.MultipleChoice:
          counts.MultipleChoice++;
          break;
        case QuestionType.Rating:
          counts.Rating++;
          break;
        case QuestionType.Location:
          counts.Location++;
          break;
        case QuestionType.Number:
          counts.Number++;
          break;
        case QuestionType.Date:
          counts.Date++;
          break;
      }
    });

    return counts;
  };

  const typeCounts = getQuestionTypeCounts();
  const requiredCount = questions.filter((q) => q.isRequired).length;

  return (
    <Box>
      {/* Header Section */}
      <Paper elevation={0} sx={{ p: 3, mb: 3, bgcolor: 'background.default' }}>
        <Stack spacing={2}>
          <Typography variant="h5" fontWeight="bold">
            Add Questions
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Create questions for your survey. You can add different types of
            questions and reorder them by dragging.
          </Typography>

          {/* Question Statistics */}
          {questions.length > 0 && (
            <Stack direction="row" spacing={1} flexWrap="wrap" sx={{ mt: 2 }}>
              <Chip
                label={`${questions.length} Total`}
                color="primary"
                variant="outlined"
              />
              <Chip
                label={`${requiredCount} Required`}
                color="error"
                variant="outlined"
              />
              {typeCounts.Text > 0 && (
                <Chip
                  icon={<TextIcon />}
                  label={`${typeCounts.Text} Text`}
                  size="small"
                />
              )}
              {typeCounts.SingleChoice > 0 && (
                <Chip
                  icon={<SingleChoiceIcon />}
                  label={`${typeCounts.SingleChoice} Single Choice`}
                  size="small"
                />
              )}
              {typeCounts.MultipleChoice > 0 && (
                <Chip
                  icon={<MultipleChoiceIcon />}
                  label={`${typeCounts.MultipleChoice} Multiple Choice`}
                  size="small"
                />
              )}
              {typeCounts.Rating > 0 && (
                <Chip
                  icon={<RatingIcon />}
                  label={`${typeCounts.Rating} Rating`}
                  size="small"
                />
              )}
              {typeCounts.Location > 0 && (
                <Chip
                  label={`${typeCounts.Location} Location`}
                  size="small"
                />
              )}
              {typeCounts.Number > 0 && (
                <Chip
                  label={`${typeCounts.Number} Number`}
                  size="small"
                />
              )}
              {typeCounts.Date > 0 && (
                <Chip
                  label={`${typeCounts.Date} Date`}
                  size="small"
                />
              )}
            </Stack>
          )}

          {/* Add Question Button */}
          <Box>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={handleAddQuestion}
              size="large"
            >
              Add Question
            </Button>
          </Box>
        </Stack>
      </Paper>

      {/* Validation Error */}
      {validationError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {validationError}
        </Alert>
      )}

      {/* Question List */}
      <Box sx={{ mb: 3 }}>
        <QuestionList
          questions={questions}
          onReorder={handleReorderQuestions}
          onEdit={handleEditQuestion}
          onDelete={handleDeleteQuestion}
        />
      </Box>

      <Divider sx={{ my: 3 }} />

      {/* Navigation Buttons */}
      <Stack
        direction="row"
        spacing={2}
        justifyContent="space-between"
        alignItems="center"
      >
        <Button
          variant="outlined"
          startIcon={<BackIcon />}
          onClick={onBack}
          size="large"
        >
          Back
        </Button>

        <Stack direction="row" spacing={1} alignItems="center">
          {questions.length === 0 && (
            <Typography variant="caption" color="error">
              At least 1 question required
            </Typography>
          )}
          <Button
            variant="contained"
            endIcon={<NextIcon />}
            onClick={handleNext}
            disabled={questions.length === 0}
            size="large"
          >
            Next: Review & Publish
          </Button>
        </Stack>
      </Stack>

      {/* Question Editor Dialog */}
      <QuestionEditor
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditingQuestion(null);
        }}
        onSave={handleSaveQuestion}
        question={editingQuestion}
        orderIndex={questions.length}
        allQuestions={questions}
      />
    </Box>
  );
};

export default QuestionsStep;
