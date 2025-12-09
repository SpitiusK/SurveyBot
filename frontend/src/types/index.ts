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
  QuestionType.Location,       // 4
  QuestionType.Number,         // 5
  QuestionType.Date,           // 6
];

// Branching: Different answers can flow to different questions
// Uses optionNextQuestions Record
export const BRANCHING_QUESTION_TYPES: QuestionType[] = [
  QuestionType.SingleChoice,   // 1 - Each option can lead to different next question
  QuestionType.Rating,         // 3 - Each star rating (1-5) can lead to different next question (NEW v1.7.0)
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

// Complete Survey Update DTOs (TASK-FRONTEND-002)
// NEW: Complete survey update with all questions (index-based flow)

/**
 * DTO for creating a question with conditional flow in a complete survey update.
 * Uses index-based references to support creation of questions that don't have IDs yet.
 * Matches backend CreateQuestionWithFlowDto structure exactly.
 */
export interface CreateQuestionWithFlowDto {
  questionText: string;
  questionType: QuestionType; // 0=Text, 1=SingleChoice, 2=MultipleChoice, 3=Rating, 4=Location, 5=Number, 6=Date
  isRequired: boolean;
  orderIndex: number;
  options?: string[] | null; // For choice questions
  mediaContent?: MediaContentDto | null; // Deserialized MediaContentDto object (will be serialized by backend)
  defaultNextQuestionIndex?: number | null; // Index-based: -1=sequential, null=end, 0+=goto
  optionNextQuestionIndexes?: Record<number, number | null>; // For SingleChoice: optionIndex -> questionIndex
}

/**
 * DTO for completely replacing survey metadata and all questions in a single atomic transaction.
 * WARNING: This deletes ALL existing questions, responses, and answers before creating new ones.
 * Matches backend UpdateSurveyWithQuestionsDto structure exactly.
 */
export interface UpdateSurveyWithQuestionsDto {
  title: string;
  description?: string | null;
  allowMultipleResponses: boolean;
  showResults: boolean;
  activateAfterUpdate?: boolean; // Default true in backend
  questions: CreateQuestionWithFlowDto[];
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
  displayValue?: string | null;
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

// Rating Statistics (backend provides comprehensive rating analysis)
export interface RatingDistribution {
  rating: number;
  count: number;
  percentage: number;
}

export interface RatingStatistics {
  averageRating: number;
  medianRating: number;
  modeRating: number;
  minRating: number;
  maxRating: number;
  distribution: Record<number, RatingDistribution>;
}

// Number Statistics (backend calculates statistical metrics)
export interface NumberStatistics {
  minimum: number;
  maximum: number;
  average: number;
  median: number;
  standardDeviation: number;
  count: number;
  sum: number;
}

// Date Statistics (backend provides date range and distribution)
export interface DateFrequency {
  date: string; // ISO format date from backend (DateTime)
  count: number;
  percentage: number;
  formattedDate: string; // DD.MM.YYYY format
}

export interface DateStatistics {
  earliestDate: string | null; // ISO format date
  latestDate: string | null; // ISO format date
  dateDistribution: DateFrequency[];
  count: number;
}

// Location Statistics
export interface LocationDataPoint {
  latitude: number;
  longitude: number;
  accuracy?: number | null;
  timestamp?: string | null;
  responseId: number;
}

export interface LocationStatistics {
  totalLocations: number;
  minLatitude?: number | null;
  maxLatitude?: number | null;
  minLongitude?: number | null;
  maxLongitude?: number | null;
  centerLatitude?: number | null;
  centerLongitude?: number | null;
  locations: LocationDataPoint[];
}

// Text Statistics (backend provides text analysis)
export interface TextStatistics {
  totalAnswers: number;
  averageLength: number;
  minLength: number;
  maxLength: number;
  sampleAnswers: string[];
}

export interface QuestionStatistics {
  questionId: number;
  questionText: string;
  questionType: QuestionType;
  totalAnswers: number;
  skippedCount?: number;
  responseRate?: number;

  // Type-specific statistics (backend provides different stats based on question type)
  choiceDistribution?: Record<string, ChoiceStatistics>; // SingleChoice, MultipleChoice
  ratingStatistics?: RatingStatistics; // Rating questions
  textStatistics?: TextStatistics; // Text questions
  numberStatistics?: NumberStatistics; // Number questions
  dateStatistics?: DateStatistics; // Date questions
  locationStatistics?: LocationStatistics; // Location questions
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
  options: string[]; // For SingleChoice/MultipleChoice; empty for Rating (uses implicit 1-5 values)
  orderIndex: number;
  mediaContent?: import('./media').MediaContentDto | null; // Deserialized MediaContentDto object
  defaultNextQuestionId?: string | null; // For non-branching questions (Text/MultipleChoice/Location/Number/Date)
  optionNextQuestions?: Record<number, string | null>;
  // For branching questions:
  //   - SingleChoice: optionIndex → nextQuestionId
  //   - Rating: rating value index (0=1 star, 1=2 stars, 2=3 stars, 3=4 stars, 4=5 stars) → nextQuestionId
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
