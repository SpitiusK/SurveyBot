import * as z from 'zod';
import { QuestionType } from '../types';

// Question text validation
export const questionTextSchema = z
  .string()
  .min(5, 'Question text must be at least 5 characters')
  .max(500, 'Question text must not exceed 500 characters')
  .trim();

// Option validation (for choice questions)
export const optionSchema = z
  .string()
  .min(1, 'Option cannot be empty')
  .max(200, 'Option must not exceed 200 characters')
  .trim();

// Options array validation
export const optionsArraySchema = z
  .array(optionSchema)
  .min(2, 'At least 2 options are required for choice questions')
  .max(10, 'Maximum 10 options allowed')
  .refine(
    (options) => {
      // Check for duplicate options (case-insensitive)
      const lowercaseOptions = options.map((opt) => opt.toLowerCase());
      return new Set(lowercaseOptions).size === options.length;
    },
    {
      message: 'Options must be unique',
    }
  );

// Rating scale validation
export const ratingScaleSchema = z.number().int().min(1).max(10);

// Base question schema
const baseQuestionSchema = z.object({
  questionText: questionTextSchema,
  questionType: z.nativeEnum(QuestionType),
  isRequired: z.boolean(),
  orderIndex: z.number().int().min(0),
});

// Text question schema
export const textQuestionSchema = baseQuestionSchema.extend({
  questionType: z.literal(QuestionType.Text),
  options: z.array(z.string()).length(0).optional(),
});

// Single choice question schema
export const singleChoiceQuestionSchema = baseQuestionSchema.extend({
  questionType: z.literal(QuestionType.SingleChoice),
  options: optionsArraySchema,
});

// Multiple choice question schema
export const multipleChoiceQuestionSchema = baseQuestionSchema.extend({
  questionType: z.literal(QuestionType.MultipleChoice),
  options: optionsArraySchema,
});

// Rating question schema
export const ratingQuestionSchema = baseQuestionSchema.extend({
  questionType: z.literal(QuestionType.Rating),
  options: z
    .array(z.string())
    .length(0)
    .optional()
    .or(z.undefined()),
});

// Union of all question types
export const questionSchema = z.discriminatedUnion('questionType', [
  textQuestionSchema,
  singleChoiceQuestionSchema,
  multipleChoiceQuestionSchema,
  ratingQuestionSchema,
]);

// Question draft schema (for local state management)
export const questionDraftSchema = z.object({
  id: z.string().uuid(), // Temporary UUID for draft questions
  questionText: questionTextSchema,
  questionType: z.nativeEnum(QuestionType),
  isRequired: z.boolean(),
  options: z.array(optionSchema).optional(),
  orderIndex: z.number().int().min(0),
});

// Questions array validation
export const questionsArraySchema = z
  .array(questionDraftSchema)
  .min(1, 'At least one question is required')
  .max(50, 'Maximum 50 questions allowed');

// Question editor form validation
export const questionEditorFormSchema = z
  .object({
    questionText: questionTextSchema,
    questionType: z.nativeEnum(QuestionType),
    isRequired: z.boolean().default(true),
    options: z.array(optionSchema).optional(),
  })
  .refine(
    (data) => {
      // For choice questions, options are required
      if (
        data.questionType === QuestionType.SingleChoice ||
        data.questionType === QuestionType.MultipleChoice
      ) {
        return (
          data.options &&
          data.options.length >= 2 &&
          data.options.length <= 10
        );
      }
      return true;
    },
    {
      message: 'Choice questions must have 2-10 options',
      path: ['options'],
    }
  )
  .refine(
    (data) => {
      // Check for duplicate options (case-insensitive)
      if (
        data.options &&
        (data.questionType === QuestionType.SingleChoice ||
          data.questionType === QuestionType.MultipleChoice)
      ) {
        const lowercaseOptions = data.options.map((opt) => opt.toLowerCase());
        return new Set(lowercaseOptions).size === data.options.length;
      }
      return true;
    },
    {
      message: 'Options must be unique',
      path: ['options'],
    }
  );

// Export types
export type QuestionEditorFormData = z.infer<typeof questionEditorFormSchema>;
export type QuestionDraft = z.infer<typeof questionDraftSchema>;
export type QuestionsArray = z.infer<typeof questionsArraySchema>;
