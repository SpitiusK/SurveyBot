import React, { useState } from 'react';
import {
  Box,
  TextField,
  IconButton,
  Stack,
  Typography,
  Button,
  Alert,
  Paper,
} from '@mui/material';
import {
  Delete as DeleteIcon,
  Add as AddIcon,
  DragIndicator as DragIcon,
} from '@mui/icons-material';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

interface OptionManagerProps {
  options: string[];
  onChange: (options: string[]) => void;
  error?: string;
  helperText?: string;
  minOptions?: number;
  maxOptions?: number;
}

interface OptionItemProps {
  id: string;
  value: string;
  index: number;
  onUpdate: (index: number, value: string) => void;
  onDelete: (index: number) => void;
  canDelete: boolean;
}

const OptionItem: React.FC<OptionItemProps> = ({
  id,
  value,
  index,
  onUpdate,
  onDelete,
  canDelete,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <Paper
      ref={setNodeRef}
      style={style}
      elevation={1}
      sx={{
        p: 1,
        mb: 1,
        display: 'flex',
        alignItems: 'center',
        gap: 1,
      }}
    >
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

      {/* Option Number */}
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ minWidth: 30 }}
      >
        {index + 1}.
      </Typography>

      {/* Option Text Field */}
      <TextField
        fullWidth
        size="small"
        value={value}
        onChange={(e) => onUpdate(index, e.target.value)}
        placeholder={`Option ${index + 1}`}
        inputProps={{ maxLength: 200 }}
      />

      {/* Delete Button */}
      <IconButton
        size="small"
        color="error"
        onClick={() => onDelete(index)}
        disabled={!canDelete}
      >
        <DeleteIcon fontSize="small" />
      </IconButton>
    </Paper>
  );
};

const OptionManager: React.FC<OptionManagerProps> = ({
  options,
  onChange,
  error,
  helperText,
  minOptions = 2,
  maxOptions = 10,
}) => {
  const [localError, setLocalError] = useState<string>('');

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const oldIndex = options.findIndex((_, idx) => idx === active.id);
      const newIndex = options.findIndex((_, idx) => idx === over.id);
      onChange(arrayMove(options, oldIndex, newIndex));
    }
  };

  const handleAddOption = () => {
    setLocalError('');
    if (options.length >= maxOptions) {
      setLocalError(`Maximum ${maxOptions} options allowed`);
      return;
    }
    onChange([...options, '']);
  };

  const handleUpdateOption = (index: number, value: string) => {
    setLocalError('');
    const newOptions = [...options];
    newOptions[index] = value;
    onChange(newOptions);

    // Check for duplicates (case-insensitive)
    const lowercaseOptions = newOptions
      .filter((opt) => opt.trim() !== '')
      .map((opt) => opt.toLowerCase());
    if (new Set(lowercaseOptions).size !== lowercaseOptions.length) {
      setLocalError('Duplicate options are not allowed');
    }
  };

  const handleDeleteOption = (index: number) => {
    setLocalError('');
    if (options.length <= minOptions) {
      setLocalError(`At least ${minOptions} options are required`);
      return;
    }
    const newOptions = options.filter((_, idx) => idx !== index);
    onChange(newOptions);
  };

  const canDelete = options.length > minOptions;

  return (
    <Box>
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        sx={{ mb: 2 }}
      >
        <Typography variant="subtitle2" color="text.secondary">
          Options ({options.length}/{maxOptions})
        </Typography>
        <Button
          size="small"
          startIcon={<AddIcon />}
          onClick={handleAddOption}
          disabled={options.length >= maxOptions}
        >
          Add Option
        </Button>
      </Stack>

      {(error || localError) && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error || localError}
        </Alert>
      )}

      {helperText && (
        <Typography variant="caption" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
          {helperText}
        </Typography>
      )}

      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragEnd={handleDragEnd}
      >
        <SortableContext
          items={options.map((_, idx) => idx)}
          strategy={verticalListSortingStrategy}
        >
          {options.map((option, index) => (
            <OptionItem
              key={index}
              id={String(index)}
              value={option}
              index={index}
              onUpdate={handleUpdateOption}
              onDelete={handleDeleteOption}
              canDelete={canDelete}
            />
          ))}
        </SortableContext>
      </DndContext>

      {options.length === 0 && (
        <Alert severity="info">
          Click "Add Option" to start adding choices for this question.
        </Alert>
      )}

      {options.length > 0 && options.length < minOptions && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          Add at least {minOptions} options to save this question.
        </Alert>
      )}
    </Box>
  );
};

export default OptionManager;
