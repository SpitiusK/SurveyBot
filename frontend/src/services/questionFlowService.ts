import api from './api';
import type {
  ApiResponse,
  ConditionalFlowDto,
  UpdateQuestionFlowDto,
  SurveyValidationResult,
} from '@/types';

/**
 * Service for managing conditional question flow configuration.
 *
 * Provides methods for:
 * - Getting question flow configuration
 * - Updating flow configuration (branching and default next question)
 * - Validating survey flow for cycles and logical errors
 *
 * @class QuestionFlowService
 */
class QuestionFlowService {
  private basePath = '/surveys';

  /**
   * Get the conditional flow configuration for a specific question.
   *
   * @param surveyId - The ID of the survey
   * @param questionId - The ID of the question
   * @returns Promise<ConditionalFlowDto> - Flow configuration with branching info
   *
   * @example
   * const flow = await questionFlowService.getQuestionFlow('1', '5');
   * console.log(flow.supportsBranching); // true for SingleChoice/Rating
   * console.log(flow.optionFlows); // Array of option -> next question mappings
   */
  async getQuestionFlow(
    surveyId: string | number,
    questionId: string | number
  ): Promise<ConditionalFlowDto> {
    try {
      const response = await api.get<ApiResponse<ConditionalFlowDto>>(
        `${this.basePath}/${surveyId}/questions/${questionId}/flow`
      );
      return response.data.data!;
    } catch (error) {
      console.error('Error fetching question flow:', error);
      throw error;
    }
  }

  /**
   * Update the conditional flow configuration for a question.
   *
   * For branching questions (SingleChoice, Rating):
   * - Set specific next questions for each option
   * - Set -1 for "End Survey" on any option
   *
   * For non-branching questions (Text, MultipleChoice):
   * - Set defaultNextQuestionId only
   *
   * @param surveyId - The ID of the survey
   * @param questionId - The ID of the question
   * @param dto - Flow update data (defaultNextQuestionId or optionNextQuestions)
   * @returns Promise<ConditionalFlowDto> - Updated flow configuration
   *
   * @example
   * // For SingleChoice question with 2 options
   * await questionFlowService.updateQuestionFlow('1', '5', {
   *   optionNextQuestions: {
   *     10: 6,  // Option 10 → Question 6
   *     11: -1  // Option 11 → End Survey
   *   }
   * });
   *
   * @example
   * // For Text question (non-branching)
   * await questionFlowService.updateQuestionFlow('1', '3', {
   *   defaultNextQuestionId: 4  // → Question 4
   * });
   */
  async updateQuestionFlow(
    surveyId: string | number,
    questionId: string | number,
    dto: UpdateQuestionFlowDto
  ): Promise<ConditionalFlowDto> {
    try {
      const response = await api.put<ApiResponse<ConditionalFlowDto>>(
        `${this.basePath}/${surveyId}/questions/${questionId}/flow`,
        dto
      );
      return response.data.data!;
    } catch (error) {
      console.error('Error updating question flow:', error);
      throw error;
    }
  }

  /**
   * Validate the entire survey's question flow.
   *
   * Checks for:
   * - Circular references (cycles) in the question flow
   * - Logical errors (questions with no path to completion)
   *
   * @param surveyId - The ID of the survey to validate
   * @returns Promise<SurveyValidationResult> - Validation result with errors if any
   *
   * @example
   * const result = await questionFlowService.validateSurveyFlow('1');
   * if (!result.valid) {
   *   console.error('Validation errors:', result.errors);
   *   if (result.cyclePath) {
   *     console.error('Cycle detected:', result.cyclePath.join(' → '));
   *   }
   * }
   */
  async validateSurveyFlow(surveyId: string | number): Promise<SurveyValidationResult> {
    try {
      const response = await api.post<ApiResponse<SurveyValidationResult>>(
        `${this.basePath}/${surveyId}/questions/validate`
      );
      return response.data.data!;
    } catch (error) {
      console.error('Error validating survey flow:', error);
      throw error;
    }
  }

  /**
   * Delete/reset the flow configuration for a specific question.
   * This removes all next question assignments (both default and option-specific).
   *
   * @param surveyId - The ID of the survey
   * @param questionId - The ID of the question
   * @returns Promise<void>
   *
   * @example
   * await questionFlowService.deleteQuestionFlow('1', '5');
   * // Question 5 now has no next question configured
   */
  async deleteQuestionFlow(
    surveyId: string | number,
    questionId: string | number
  ): Promise<void> {
    try {
      await api.delete(`${this.basePath}/${surveyId}/questions/${questionId}/flow`);
    } catch (error) {
      console.error('Error deleting question flow:', error);
      throw error;
    }
  }
}

export default new QuestionFlowService();
