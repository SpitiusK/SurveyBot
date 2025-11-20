import React, { useState } from 'react';
import {
  Box,
  Typography,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
} from '@mui/material';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import QuestionCard from './QuestionCard';
import type { QuestionDraft } from '../../schemas/questionSchemas';

interface BranchingRuleDraft {
  sourceQuestionId: string | number;
  targetQuestionId: string | number;
  condition: {
    operator: string;
    value?: string;
    values?: string[];
    questionType: string;
  };
}

interface QuestionListProps {
  questions: QuestionDraft[];
  branchingRules?: BranchingRuleDraft[];
  onReorder: (questions: QuestionDraft[]) => void;
  onEdit: (question: QuestionDraft) => void;
  onDelete: (questionId: string) => void;
  onConfigureBranching?: (question: QuestionDraft) => void;
}

const QuestionList: React.FC<QuestionListProps> = ({
  questions,
  branchingRules = [],
  onReorder,
  onEdit,
  onDelete,
  onConfigureBranching,
}) => {
  const [activeId, setActiveId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [questionToDelete, setQuestionToDelete] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(String(event.active.id));
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveId(null);

    if (over && active.id !== over.id) {
      const oldIndex = questions.findIndex((q) => q.id === active.id);
      const newIndex = questions.findIndex((q) => q.id === over.id);

      const reorderedQuestions = arrayMove(questions, oldIndex, newIndex);

      // Update orderIndex for all questions
      const updatedQuestions = reorderedQuestions.map((q, index) => ({
        ...q,
        orderIndex: index,
      }));

      onReorder(updatedQuestions);
    }
  };

  const handleDeleteClick = (questionId: string) => {
    setQuestionToDelete(questionId);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = () => {
    if (questionToDelete) {
      onDelete(questionToDelete);
    }
    setDeleteDialogOpen(false);
    setQuestionToDelete(null);
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
    setQuestionToDelete(null);
  };

  if (questions.length === 0) {
    return (
      <Alert severity="info">
        No questions added yet. Click "Add Question" to create your first
        question.
      </Alert>
    );
  }

  const questionToDeleteObj = questions.find((q) => q.id === questionToDelete);

  return (
    <>
      <Box>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 2 }}>
          {questions.length} {questions.length === 1 ? 'question' : 'questions'} added. Drag to reorder.
        </Typography>

        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <SortableContext
            items={questions.map((q) => q.id)}
            strategy={verticalListSortingStrategy}
          >
            {questions.map((question, index) => {
              // Count branching rules where this question is the source
              const branchingRulesCount = branchingRules.filter(
                (rule) =>
                  rule.sourceQuestionId === question.id ||
                  String(rule.sourceQuestionId) === String(question.id)
              ).length;

              return (
                <QuestionCard
                  key={question.id}
                  question={question}
                  index={index}
                  onEdit={onEdit}
                  onDelete={handleDeleteClick}
                  onConfigureBranching={onConfigureBranching}
                  branchingRulesCount={branchingRulesCount}
                  isDragging={question.id === activeId}
                />
              );
            })}
          </SortableContext>
        </DndContext>
      </Box>

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteDialogOpen}
        onClose={handleDeleteCancel}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Delete Question?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete this question?
          </DialogContentText>
          {questionToDeleteObj && (
            <Box
              sx={{
                mt: 2,
                p: 2,
                bgcolor: 'action.hover',
                borderRadius: 1,
              }}
            >
              <Typography variant="body2" fontWeight="medium">
                {questionToDeleteObj.questionText}
              </Typography>
            </Box>
          )}
          <DialogContentText sx={{ mt: 2 }}>
            This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleDeleteCancel} color="inherit">
            Cancel
          </Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default QuestionList;
