import * as yup from 'yup';
import type { CreateSurveyDto } from '@/types';

// Basic Info Step validation schema
export const basicInfoSchema = yup.object({
  title: yup
    .string()
    .trim()
    .required('Survey title is required')
    .min(3, 'Title must be at least 3 characters')
    .max(500, 'Title must be at most 500 characters'),
  description: yup
    .string()
    .trim()
    .max(1000, 'Description must be at most 1000 characters')
    .notRequired()
    .default(''),
  allowMultipleResponses: yup
    .boolean()
    .required()
    .default(false),
  showResults: yup
    .boolean()
    .required()
    .default(true),
}).required();

// Full survey creation schema (extends basic info with questions)
export const createSurveySchema = yup.object({
  title: yup
    .string()
    .trim()
    .required('Survey title is required')
    .min(3, 'Title must be at least 3 characters')
    .max(500, 'Title must be at most 500 characters'),
  description: yup
    .string()
    .trim()
    .max(1000, 'Description must be at most 1000 characters')
    .notRequired()
    .default(''),
  allowMultipleResponses: yup
    .boolean()
    .required()
    .default(false),
  showResults: yup
    .boolean()
    .required()
    .default(true),
}).required();

// Type inference from schema
export type BasicInfoFormData = yup.InferType<typeof basicInfoSchema>;
export type CreateSurveyFormData = yup.InferType<typeof createSurveySchema>;

// Helper to convert form data to DTO
export const toCreateSurveyDto = (data: BasicInfoFormData): CreateSurveyDto => ({
  title: data.title,
  description: data.description || undefined,
  allowMultipleResponses: data.allowMultipleResponses,
  showResults: data.showResults,
});
