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
import { QuestionType } from '../../types';
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

  const handleNext = () => {
    if (questions.length === 0) {
      setValidationError('Please add at least one question before proceeding.');
      return;
    }
    setValidationError('');
    onNext();
  };

  const getQuestionTypeCounts = () => {
    const counts = {
      text: 0,
      singleChoice: 0,
      multipleChoice: 0,
      rating: 0,
    };

    questions.forEach((q) => {
      switch (q.questionType) {
        case QuestionType.Text:
          counts.text++;
          break;
        case QuestionType.SingleChoice:
          counts.singleChoice++;
          break;
        case QuestionType.MultipleChoice:
          counts.multipleChoice++;
          break;
        case QuestionType.Rating:
          counts.rating++;
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
              {typeCounts.text > 0 && (
                <Chip
                  icon={<TextIcon />}
                  label={`${typeCounts.text} Text`}
                  size="small"
                />
              )}
              {typeCounts.singleChoice > 0 && (
                <Chip
                  icon={<SingleChoiceIcon />}
                  label={`${typeCounts.singleChoice} Single Choice`}
                  size="small"
                />
              )}
              {typeCounts.multipleChoice > 0 && (
                <Chip
                  icon={<MultipleChoiceIcon />}
                  label={`${typeCounts.multipleChoice} Multiple Choice`}
                  size="small"
                />
              )}
              {typeCounts.rating > 0 && (
                <Chip
                  icon={<RatingIcon />}
                  label={`${typeCounts.rating} Rating`}
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
      />
    </Box>
  );
};

export default QuestionsStep;
