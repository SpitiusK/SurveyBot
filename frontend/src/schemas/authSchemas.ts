import * as yup from 'yup';
import type { LoginDto } from '@/types';

// Login validation schema
export const loginSchema = yup.object({
  telegramId: yup
    .number()
    .typeError('Telegram ID must be a number')
    .positive('Telegram ID must be positive')
    .integer('Telegram ID must be an integer')
    .required('Telegram ID is required'),
  username: yup
    .string()
    .trim()
    .min(3, 'Username must be at least 3 characters')
    .max(255, 'Username must be at most 255 characters')
    .transform((value) => (value === '' ? undefined : value))
    .optional()
    .default(undefined),
  firstName: yup
    .string()
    .trim()
    .min(1, 'First name must be at least 1 character')
    .max(255, 'First name must be at most 255 characters')
    .transform((value) => (value === '' ? undefined : value))
    .optional()
    .default(undefined),
  lastName: yup
    .string()
    .trim()
    .min(1, 'Last name must be at least 1 character')
    .max(255, 'Last name must be at most 255 characters')
    .transform((value) => (value === '' ? undefined : value))
    .optional()
    .default(undefined),
}).required();

export type LoginFormData = LoginDto;
