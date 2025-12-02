import api from './api';
import type {
  ApiResponse,
  PagedResult,
  Survey,
  SurveyListItem,
  CreateSurveyDto,
  UpdateSurveyDto,
  UpdateSurveyWithQuestionsDto,
  SurveyStatistics,
  SurveyFilterParams,
} from '@/types';

class SurveyService {
  private basePath = '/surveys';

  // Get all surveys with pagination and filters
  async getAllSurveys(params?: SurveyFilterParams): Promise<PagedResult<SurveyListItem>> {
    const response = await api.get<ApiResponse<PagedResult<SurveyListItem>>>(
      this.basePath,
      { params }
    );
    return response.data.data!;
  }

  // Get survey by ID
  async getSurveyById(id: number): Promise<Survey> {
    const response = await api.get<ApiResponse<Survey>>(`${this.basePath}/${id}`);
    return response.data.data!;
  }

  // Get survey by code
  async getSurveyByCode(code: string): Promise<Survey> {
    const response = await api.get<ApiResponse<Survey>>(`${this.basePath}/code/${code}`);
    return response.data.data!;
  }

  // Create new survey
  async createSurvey(dto: CreateSurveyDto): Promise<Survey> {
    const response = await api.post<ApiResponse<Survey>>(this.basePath, dto);
    return response.data.data!;
  }

  // Update survey
  async updateSurvey(id: number, dto: UpdateSurveyDto): Promise<Survey> {
    const response = await api.put<ApiResponse<Survey>>(`${this.basePath}/${id}`, dto);
    return response.data.data!;
  }

  /**
   * Completely replaces survey metadata and all questions in a single atomic transaction.
   * WARNING: This deletes ALL existing questions, responses, and answers before creating new ones.
   *
   * @param surveyId - Survey ID to update
   * @param dto - Complete survey data with questions and flow configuration
   * @returns Updated survey with new question IDs
   */
  async updateSurveyComplete(surveyId: number, dto: UpdateSurveyWithQuestionsDto): Promise<Survey> {
    const response = await api.put<ApiResponse<Survey>>(
      `${this.basePath}/${surveyId}/complete`,
      dto
    );
    return response.data.data!;
  }

  // Delete survey
  async deleteSurvey(id: number): Promise<void> {
    await api.delete(`${this.basePath}/${id}`);
  }

  // Activate survey
  async activateSurvey(id: number): Promise<Survey> {
    const response = await api.post<ApiResponse<Survey>>(`${this.basePath}/${id}/activate`);
    return response.data.data!;
  }

  // Deactivate survey
  async deactivateSurvey(id: number): Promise<Survey> {
    const response = await api.post<ApiResponse<Survey>>(`${this.basePath}/${id}/deactivate`);
    return response.data.data!;
  }

  // Toggle survey status
  async toggleSurveyStatus(id: number, currentStatus: boolean): Promise<Survey> {
    return currentStatus ? this.deactivateSurvey(id) : this.activateSurvey(id);
  }

  // Get survey statistics
  async getSurveyStatistics(id: number): Promise<SurveyStatistics> {
    const response = await api.get<ApiResponse<SurveyStatistics>>(
      `${this.basePath}/${id}/statistics`
    );
    return response.data.data!;
  }

  // Export survey responses to CSV
  async exportSurveyResponses(id: number): Promise<Blob> {
    const response = await api.get(`${this.basePath}/${id}/export`, {
      responseType: 'blob',
    });
    return response.data;
  }
}

export default new SurveyService();
