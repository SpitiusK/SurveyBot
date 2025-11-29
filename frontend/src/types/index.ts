// Core API Types

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  metadata?: Record<string, unknown>;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// User Types
export interface User {
  id: number;
  telegramId: number;
  username: string | null;
  firstName: string | null;
  lastName: string | null;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string;
}

// Survey Types
export const QuestionType = {
  Text: 0,
  SingleChoice: 1,
  MultipleChoice: 2,
  Rating: 3,
  Location: 4,
  Number: 5,
  Date: 6,
} as const;

export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

// Question type categorization for conditional flow logic
// Non-branching: All answers flow to same next question (or end)
// Uses defaultNextQuestionId field
export const NON_BRANCHING_QUESTION_TYPES: QuestionType[] = [
  QuestionType.Text,           // 0
  QuestionType.MultipleChoice, // 2
  QuestionType.Rating,         // 3 - Rating uses defaultNextQuestionId, not optionNextQuestions
  QuestionType.Location,       // 4
  QuestionType.Number,         // 5
  QuestionType.Date,           // 6
];

// Branching: Different answers can flow to different questions
// Uses optionNextQuestions Record
export const BRANCHING_QUESTION_TYPES: QuestionType[] = [
  QuestionType.SingleChoice,   // 1 - Only SingleChoice uses optionNextQuestions
];

// Helper functions for type checking
export const isNonBranchingType = (type: QuestionType): boolean =>
  NON_BRANCHING_QUESTION_TYPES.includes(type);

export const isBranchingType = (type: QuestionType): boolean =>
  BRANCHING_QUESTION_TYPES.includes(type);

export interface Question {
  id: number;
  surveyId: number;
  questionText: string;
  questionType: QuestionType;
  orderIndex: number;
  isRequired: boolean;
  options: string[] | null;
  optionDetails?: QuestionOption[] | null; // Detailed option info with IDs
  defaultNext?: NextQuestionDeterminant | null;
  supportsBranching?: boolean;
  mediaContent?: string | null; // JSON string of MediaContentDto
  createdAt: string;
  updatedAt: string;
}

export interface Survey {
  id: number;
  title: string;
  description: string | null;
  code: string | null;
  creatorId: number;
  creator?: User;
  isActive: boolean;
  allowMultipleResponses: boolean;
  showResults: boolean;
  questions: Question[];
  totalResponses: number;
  completedResponses: number;
  createdAt: string;
  updatedAt: string;
}

export interface SurveyListItem {
  id: number;
  title: string;
  description: string | null;
  code: string | null;
  isActive: boolean;
  totalResponses: number;
  completedResponses: number;
  createdAt: string;
}

// Survey DTOs
export interface CreateSurveyDto {
  title: string;
  description?: string;
  allowMultipleResponses?: boolean;
  showResults?: boolean;
}

export interface UpdateSurveyDto {
  title?: string;
  description?: string;
  allowMultipleResponses?: boolean;
  showResults?: boolean;
}

export interface CreateQuestionDto {
  questionText: string;
  questionType: QuestionType;
  isRequired: boolean;
  options?: string[];
  defaultNext?: NextQuestionDeterminant | null; // For conditional flow
  mediaContent?: string | null; // JSON string of MediaContentDto
}

export interface UpdateQuestionDto {
  questionText?: string;
  isRequired?: boolean;
  options?: string[];
  defaultNext?: NextQuestionDeterminant | null; // For conditional flow
  mediaContent?: string | null; // JSON string of MediaContentDto
}

// Response Types
export interface Answer {
  id: number;
  responseId: number;
  questionId: number;
  answerText: string | null;
  answerData: Record<string, unknown>;
  createdAt: string;
}

export interface Response {
  id: number;
  surveyId: number;
  respondentTelegramId: number;
  isComplete: boolean;
  startedAt: string | null;
  submittedAt: string | null;
  answers: Answer[];
}

// Statistics Types
export interface ChoiceStatistics {
  option: string;
  count: number;
  percentage: number;
}

export interface QuestionStatistics {
  questionId: number;
  questionText: string;
  questionType: QuestionType;
  totalAnswers: number;
  choiceDistribution?: Record<string, ChoiceStatistics>;
  averageRating?: number;
  textAnswers?: string[];
}

export interface SurveyStatistics {
  surveyId: number;
  surveyTitle: string;
  totalResponses: number;
  completedResponses: number;
  incompleteResponses: number;
  completionRate: number;
  averageCompletionTimeMinutes: number | null;
  uniqueRespondents: number;
  questionStatistics: QuestionStatistics[];
  createdAt: string;
  firstResponseAt: string | null;
  lastResponseAt: string | null;
}

// Auth Types
export interface LoginDto {
  telegramId: number;
  username?: string;
  firstName?: string;
  lastName?: string;
}

export interface AuthResponse {
  token: string;
  user: User;
  expiresAt: string;
}

// Pagination
export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
}

// Survey Filter Params
export interface SurveyFilterParams extends PaginationParams {
  searchTerm?: string;
  isActive?: boolean;
  sortBy?: 'createdAt' | 'title' | 'totalResponses';
  sortOrder?: 'asc' | 'desc';
}

// Survey Builder Types
export interface SurveyDraft {
  title: string;
  description?: string;
  allowMultipleResponses: boolean;
  showResults: boolean;
  questions: QuestionDraft[];
  currentStep: number;
}

export interface QuestionDraft {
  id: string; // Temporary ID for draft questions (UUID)
  questionText: string;
  questionType: QuestionType;
  isRequired: boolean;
  options: string[];
  orderIndex: number;
  mediaContent?: import('./media').MediaContentDto | null; // Deserialized MediaContentDto object
  defaultNextQuestionId?: string | null; // For Text/MultipleChoice/Rating questions
  optionNextQuestions?: Record<number, string | null>; // For SingleChoice: optionIndex -> nextQuestionId
}

// Wizard Step Types
export type WizardStep = 'basic-info' | 'questions' | 'review';

export interface StepConfig {
  id: WizardStep;
  label: string;
  description: string;
  isValid: boolean;
}

// Re-export media types
export * from './media';

// Conditional Question Flow Types (Phase 5)

// Next step types for conditional flow
// Backend expects INTEGER enum values: 0 = GoToQuestion, 1 = EndSurvey
export type NextStepType = 0 | 1;

// Value object for determining next question
export interface NextQuestionDeterminant {
  type: NextStepType;  // 0 = GoToQuestion, 1 = EndSurvey
  nextQuestionId?: number | null;
}

export interface QuestionOption {
  id: number;
  text: string;
  orderIndex: number;
  next?: NextQuestionDeterminant | null;
}

export interface OptionFlowDto {
  optionId: number;
  optionText: string;
  next?: NextQuestionDeterminant | null;
}

export interface ConditionalFlowDto {
  questionId: number;
  supportsBranching: boolean;
  defaultNext?: NextQuestionDeterminant | null;
  optionFlows: OptionFlowDto[];
}

export interface UpdateQuestionFlowDto {
  defaultNext?: NextQuestionDeterminant | null;
  optionNextDeterminants?: Record<number, NextQuestionDeterminant>; // optionId -> NextQuestionDeterminant
}

export interface SurveyValidationResult {
  valid: boolean;
  errors?: string[];
  cyclePath?: number[]; // Question IDs forming the cycle
}
