import React from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
  Card,
  CardContent,
  Typography,
  IconButton,
  Box,
  Chip,
  Stack,
  Tooltip,
} from '@mui/material';
import {
  DragIndicator as DragIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  TextFields as TextIcon,
  RadioButtonChecked as SingleChoiceIcon,
  CheckBox as MultipleChoiceIcon,
  Star as RatingIcon,
  AccountTree as BranchIcon,
} from '@mui/icons-material';
import { QuestionType } from '../../types';
import type { QuestionDraft } from '../../schemas/questionSchemas';

interface QuestionCardProps {
  question: QuestionDraft;
  index: number;
  onEdit: (question: QuestionDraft) => void;
  onDelete: (questionId: string) => void;
  onConfigureBranching?: (question: QuestionDraft) => void;
  branchingRulesCount?: number;
  isDragging?: boolean;
}

const QuestionCard: React.FC<QuestionCardProps> = ({
  question,
  index,
  onEdit,
  onDelete,
  onConfigureBranching,
  branchingRulesCount = 0,
  isDragging = false,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging: isSortableDragging,
  } = useSortable({ id: question.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isSortableDragging ? 0.5 : 1,
  };

  const getQuestionTypeIcon = (type: QuestionType) => {
    switch (type) {
      case QuestionType.Text:
        return <TextIcon fontSize="small" />;
      case QuestionType.SingleChoice:
        return <SingleChoiceIcon fontSize="small" />;
      case QuestionType.MultipleChoice:
        return <MultipleChoiceIcon fontSize="small" />;
      case QuestionType.Rating:
        return <RatingIcon fontSize="small" />;
      default:
        return <TextIcon fontSize="small" />;
    }
  };

  const getQuestionTypeLabel = (type: QuestionType): string => {
    switch (type) {
      case QuestionType.Text:
        return 'Text';
      case QuestionType.SingleChoice:
        return 'Single Choice';
      case QuestionType.MultipleChoice:
        return 'Multiple Choice';
      case QuestionType.Rating:
        return 'Rating';
      default:
        return 'Unknown';
    }
  };

  const getQuestionTypeColor = (type: QuestionType) => {
    switch (type) {
      case QuestionType.Text:
        return 'primary';
      case QuestionType.SingleChoice:
        return 'success';
      case QuestionType.MultipleChoice:
        return 'info';
      case QuestionType.Rating:
        return 'warning';
      default:
        return 'default';
    }
  };

  return (
    <Card
      ref={setNodeRef}
      style={style}
      sx={{
        mb: 2,
        cursor: isDragging ? 'grabbing' : 'default',
        '&:hover': {
          boxShadow: 3,
        },
      }}
    >
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="flex-start">
          {/* Drag Handle */}
          <Box
            {...attributes}
            {...listeners}
            sx={{
              cursor: 'grab',
              display: 'flex',
              alignItems: 'center',
              color: 'text.secondary',
              '&:active': {
                cursor: 'grabbing',
              },
            }}
          >
            <DragIcon />
          </Box>

          {/* Question Content */}
          <Box sx={{ flex: 1, minWidth: 0 }}>
            {/* Question Number and Type */}
            <Stack
              direction="row"
              spacing={1}
              alignItems="center"
              sx={{ mb: 1 }}
            >
              <Chip
                label={`Q${index + 1}`}
                size="small"
                color="default"
                variant="outlined"
              />
              <Chip
                icon={getQuestionTypeIcon(question.questionType)}
                label={getQuestionTypeLabel(question.questionType)}
                size="small"
                color={getQuestionTypeColor(question.questionType)}
              />
              {question.isRequired && (
                <Chip
                  label="Required"
                  size="small"
                  color="error"
                  variant="outlined"
                />
              )}
              {branchingRulesCount > 0 && (
                <Chip
                  icon={<BranchIcon fontSize="small" />}
                  label={`${branchingRulesCount} branch${branchingRulesCount > 1 ? 'es' : ''}`}
                  size="small"
                  color="secondary"
                  variant="outlined"
                />
              )}
            </Stack>

            {/* Question Text */}
            <Typography
              variant="body1"
              sx={{
                mb: 1,
                wordWrap: 'break-word',
                fontWeight: 500,
              }}
            >
              {question.questionText}
            </Typography>

            {/* Options Preview (for choice questions) */}
            {(question.questionType === QuestionType.SingleChoice ||
              question.questionType === QuestionType.MultipleChoice) &&
              question.options &&
              question.options.length > 0 && (
                <Box sx={{ pl: 2 }}>
                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ display: 'block', mb: 0.5 }}
                  >
                    Options:
                  </Typography>
                  <Stack spacing={0.5}>
                    {question.options.slice(0, 3).map((option: string, idx: number) => (
                      <Typography
                        key={idx}
                        variant="body2"
                        color="text.secondary"
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 1,
                        }}
                      >
                        {question.questionType === QuestionType.SingleChoice
                          ? '○'
                          : '☐'}{' '}
                        {option}
                      </Typography>
                    ))}
                    {question.options.length > 3 && (
                      <Typography variant="caption" color="text.secondary">
                        +{question.options.length - 3} more
                      </Typography>
                    )}
                  </Stack>
                </Box>
              )}

            {/* Rating Info */}
            {question.questionType === QuestionType.Rating && (
              <Typography variant="body2" color="text.secondary" sx={{ pl: 2 }}>
                Rating scale: 1-5 stars
              </Typography>
            )}
          </Box>

          {/* Action Buttons */}
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="Edit question">
              <IconButton
                size="small"
                color="primary"
                onClick={() => onEdit(question)}
              >
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            {onConfigureBranching && question.questionType === QuestionType.SingleChoice && (
              <Tooltip title="Configure branching">
                <IconButton
                  size="small"
                  color="secondary"
                  onClick={() => onConfigureBranching(question)}
                >
                  <BranchIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            <Tooltip title="Delete question">
              <IconButton
                size="small"
                color="error"
                onClick={() => onDelete(question.id)}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default QuestionCard;
