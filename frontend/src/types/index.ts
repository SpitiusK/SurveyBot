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
} as const;

export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

export interface Question {
  id: number;
  surveyId: number;
  questionText: string;
  questionType: QuestionType;
  orderIndex: number;
  isRequired: boolean;
  options: string[] | null;
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
}

export interface UpdateQuestionDto {
  questionText?: string;
  isRequired?: boolean;
  options?: string[];
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
export interface QuestionStatistics {
  questionId: number;
  questionText: string;
  questionType: QuestionType;
  totalAnswers: number;
  choiceDistribution?: Record<string, number>;
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
}

// Wizard Step Types
export type WizardStep = 'basic-info' | 'questions' | 'review';

export interface StepConfig {
  id: WizardStep;
  label: string;
  description: string;
  isValid: boolean;
}
