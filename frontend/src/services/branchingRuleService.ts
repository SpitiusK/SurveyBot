import api from './api';
import type {
  ApiResponse,
  BranchingRule,
  CreateBranchingRuleDto,
  GetNextQuestionResponse,
} from '@/types';

/**
 * Service for managing branching rules in surveys
 * Handles creating, updating, and deleting conditional question flows
 */
class BranchingRuleService {
  /**
   * Get all branching rules for a specific source question
   * @param surveyId - The ID of the survey
   * @param sourceQuestionId - The ID of the source question
   * @returns Array of branching rules
   */
  async getBranchingRules(
    surveyId: number,
    sourceQuestionId: number
  ): Promise<BranchingRule[]> {
    const response = await api.get<ApiResponse<BranchingRule[]>>(
      `/surveys/${surveyId}/questions/${sourceQuestionId}/branches`
    );
    return response.data.data || [];
  }

  /**
   * Create a new branching rule
   * @param surveyId - The ID of the survey
   * @param sourceQuestionId - The ID of the source question
   * @param dto - Branching rule creation data
   * @returns Created branching rule
   */
  async createBranchingRule(
    surveyId: number,
    sourceQuestionId: number,
    dto: CreateBranchingRuleDto
  ): Promise<BranchingRule> {
    const response = await api.post<ApiResponse<BranchingRule>>(
      `/surveys/${surveyId}/questions/${sourceQuestionId}/branches`,
      dto
    );
    return response.data.data!;
  }

  /**
   * Update an existing branching rule
   * @param surveyId - The ID of the survey
   * @param sourceQuestionId - The ID of the source question
   * @param targetQuestionId - The ID of the target question
   * @param dto - Updated branching rule data
   * @returns Updated branching rule
   */
  async updateBranchingRule(
    surveyId: number,
    sourceQuestionId: number,
    targetQuestionId: number,
    dto: Partial<CreateBranchingRuleDto>
  ): Promise<BranchingRule> {
    const response = await api.put<ApiResponse<BranchingRule>>(
      `/surveys/${surveyId}/questions/${sourceQuestionId}/branches/${targetQuestionId}`,
      dto
    );
    return response.data.data!;
  }

  /**
   * Delete a branching rule
   * @param surveyId - The ID of the survey
   * @param sourceQuestionId - The ID of the source question
   * @param targetQuestionId - The ID of the target question
   */
  async deleteBranchingRule(
    surveyId: number,
    sourceQuestionId: number,
    targetQuestionId: number
  ): Promise<void> {
    await api.delete(
      `/surveys/${surveyId}/questions/${sourceQuestionId}/branches/${targetQuestionId}`
    );
  }

  /**
   * Get the next question based on an answer (for response flow)
   * @param surveyId - The ID of the survey
   * @param questionId - The current question ID
   * @param answer - The user's answer
   * @returns Next question information or completion status
   */
  async getNextQuestion(
    surveyId: number,
    questionId: number,
    answer: string
  ): Promise<GetNextQuestionResponse> {
    const response = await api.post<ApiResponse<GetNextQuestionResponse>>(
      `/surveys/${surveyId}/questions/${questionId}/next`,
      { answer }
    );
    return response.data.data!;
  }
}

export default new BranchingRuleService();
