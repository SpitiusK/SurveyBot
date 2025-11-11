import api from './api';
import type {
  ApiResponse,
  Question,
  CreateQuestionDto,
  UpdateQuestionDto,
} from '@/types';

class QuestionService {
  // Get all questions for a survey
  async getQuestionsBySurveyId(surveyId: number): Promise<Question[]> {
    const response = await api.get<ApiResponse<Question[]>>(
      `/surveys/${surveyId}/questions`
    );
    return response.data.data!;
  }

  // Get question by ID
  async getQuestionById(id: number): Promise<Question> {
    const response = await api.get<ApiResponse<Question>>(`/questions/${id}`);
    return response.data.data!;
  }

  // Create question
  async createQuestion(surveyId: number, dto: CreateQuestionDto): Promise<Question> {
    const response = await api.post<ApiResponse<Question>>(
      `/surveys/${surveyId}/questions`,
      dto
    );
    return response.data.data!;
  }

  // Update question
  async updateQuestion(id: number, dto: UpdateQuestionDto): Promise<Question> {
    const response = await api.put<ApiResponse<Question>>(`/questions/${id}`, dto);
    return response.data.data!;
  }

  // Delete question
  async deleteQuestion(id: number): Promise<void> {
    await api.delete(`/questions/${id}`);
  }

  // Reorder questions
  async reorderQuestions(surveyId: number, questionIds: number[]): Promise<Question[]> {
    const response = await api.post<ApiResponse<Question[]>>(
      `/surveys/${surveyId}/questions/reorder`,
      { questionIds }
    );
    return response.data.data!;
  }
}

export default new QuestionService();
