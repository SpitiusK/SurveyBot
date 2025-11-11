import type { Survey, Response, Answer, Question } from '../../types';
import { QuestionType as QT } from '../../types';

export interface ExportOptions {
  includeMetadata: boolean;
  includeTimestamps: boolean;
  exportFormat: 'all' | 'completed' | 'incomplete';
}

/**
 * CSV Generator for Survey Responses
 * Handles conversion of survey responses to CSV format with proper escaping
 */
export class CSVGenerator {
  private static readonly CHUNK_SIZE = 500; // Process responses in chunks for large datasets

  /**
   * Generate CSV content from survey responses
   */
  static generateCSV(
    survey: Survey,
    responses: Response[],
    options: ExportOptions
  ): string {
    // Filter responses based on export format
    const filteredResponses = this.filterResponses(responses, options.exportFormat);

    if (filteredResponses.length === 0) {
      throw new Error('No responses to export');
    }

    // Build CSV structure
    const headers = this.buildHeaders(survey, options);
    const rows = this.buildRows(survey, filteredResponses, options);

    // Combine headers and rows
    return [headers, ...rows].join('\n');
  }

  /**
   * Generate and download CSV file
   */
  static async downloadCSV(
    survey: Survey,
    responses: Response[],
    options: ExportOptions
  ): Promise<void> {
    const csvContent = this.generateCSV(survey, responses, options);
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);

    // Generate filename
    const filename = this.generateFilename(survey);

    // Create download link and trigger download
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();

    // Cleanup
    setTimeout(() => {
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }, 100);
  }

  /**
   * Generate and download CSV for large datasets using chunking
   */
  static async downloadLargeCSV(
    survey: Survey,
    responses: Response[],
    options: ExportOptions,
    onProgress?: (percent: number) => void
  ): Promise<void> {
    const filteredResponses = this.filterResponses(responses, options.exportFormat);

    if (filteredResponses.length === 0) {
      throw new Error('No responses to export');
    }

    const headers = this.buildHeaders(survey, options);
    const chunks: string[] = [headers];

    // Process responses in chunks
    const totalChunks = Math.ceil(filteredResponses.length / this.CHUNK_SIZE);

    for (let i = 0; i < totalChunks; i++) {
      const start = i * this.CHUNK_SIZE;
      const end = Math.min(start + this.CHUNK_SIZE, filteredResponses.length);
      const chunkResponses = filteredResponses.slice(start, end);

      const chunkRows = this.buildRows(survey, chunkResponses, options);
      chunks.push(...chunkRows);

      // Report progress
      if (onProgress) {
        const progress = Math.round(((i + 1) / totalChunks) * 100);
        onProgress(progress);
      }

      // Allow UI to breathe
      await new Promise(resolve => setTimeout(resolve, 0));
    }

    const csvContent = chunks.join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const filename = this.generateFilename(survey);

    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();

    setTimeout(() => {
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }, 100);
  }

  /**
   * Filter responses based on completion status
   */
  private static filterResponses(
    responses: Response[],
    format: 'all' | 'completed' | 'incomplete'
  ): Response[] {
    switch (format) {
      case 'completed':
        return responses.filter(r => r.isComplete);
      case 'incomplete':
        return responses.filter(r => !r.isComplete);
      case 'all':
      default:
        return responses;
    }
  }

  /**
   * Build CSV header row
   */
  private static buildHeaders(survey: Survey, options: ExportOptions): string {
    const headers: string[] = [];

    // Add metadata columns
    if (options.includeMetadata) {
      headers.push('Response ID', 'Respondent ID', 'Status');
    }

    // Add timestamp columns
    if (options.includeTimestamps) {
      headers.push('Started At', 'Submitted At');
    }

    // Add question columns
    survey.questions.forEach((question, index) => {
      const columnName = this.sanitizeQuestionText(question.questionText, index);
      headers.push(columnName);
    });

    return this.escapeCSVRow(headers);
  }

  /**
   * Build CSV data rows
   */
  private static buildRows(
    survey: Survey,
    responses: Response[],
    options: ExportOptions
  ): string[] {
    return responses.map(response => this.buildRow(survey, response, options));
  }

  /**
   * Build a single CSV row for a response
   */
  private static buildRow(
    survey: Survey,
    response: Response,
    options: ExportOptions
  ): string {
    const cells: string[] = [];

    // Add metadata
    if (options.includeMetadata) {
      cells.push(
        response.id.toString(),
        response.respondentTelegramId.toString(),
        response.isComplete ? 'Complete' : 'Incomplete'
      );
    }

    // Add timestamps
    if (options.includeTimestamps) {
      cells.push(
        this.formatDate(response.startedAt),
        this.formatDate(response.submittedAt)
      );
    }

    // Add answers for each question
    survey.questions.forEach(question => {
      const answer = response.answers?.find(a => a.questionId === question.id);
      const answerValue = this.formatAnswer(question, answer);
      cells.push(answerValue);
    });

    return this.escapeCSVRow(cells);
  }

  /**
   * Format answer based on question type
   */
  private static formatAnswer(question: Question, answer?: Answer): string {
    if (!answer) {
      return '';
    }

    try {
      switch (question.questionType) {
        case QT.Text:
          return this.formatTextAnswer(answer);

        case QT.SingleChoice:
          return this.formatSingleChoiceAnswer(answer);

        case QT.MultipleChoice:
          return this.formatMultipleChoiceAnswer(answer);

        case QT.Rating:
          return this.formatRatingAnswer(answer);

        default:
          return '';
      }
    } catch (error) {
      console.error('Error formatting answer:', error);
      return '';
    }
  }

  /**
   * Format text answer
   */
  private static formatTextAnswer(answer: Answer): string {
    if (answer.answerText) {
      return answer.answerText;
    }

    if (answer.answerData && typeof answer.answerData === 'object' && 'text' in answer.answerData) {
      return String(answer.answerData.text || '');
    }

    return '';
  }

  /**
   * Format single choice answer
   */
  private static formatSingleChoiceAnswer(answer: Answer): string {
    if (answer.answerData && typeof answer.answerData === 'object') {
      if ('selectedOption' in answer.answerData) {
        return String(answer.answerData.selectedOption || '');
      }
    }
    return '';
  }

  /**
   * Format multiple choice answer
   */
  private static formatMultipleChoiceAnswer(answer: Answer): string {
    if (answer.answerData && typeof answer.answerData === 'object') {
      if ('selectedOptions' in answer.answerData) {
        const options = answer.answerData.selectedOptions;
        if (Array.isArray(options)) {
          return options.join('; ');
        }
      }
    }
    return '';
  }

  /**
   * Format rating answer
   */
  private static formatRatingAnswer(answer: Answer): string {
    if (answer.answerData && typeof answer.answerData === 'object') {
      if ('rating' in answer.answerData) {
        return String(answer.answerData.rating || '');
      }
    }
    return '';
  }

  /**
   * Format date for CSV
   */
  private static formatDate(dateString: string | null): string {
    if (!dateString) {
      return '';
    }

    try {
      const date = new Date(dateString);
      return date.toISOString();
    } catch {
      return '';
    }
  }

  /**
   * Sanitize question text for use as column header
   */
  private static sanitizeQuestionText(text: string, index: number): string {
    // Truncate long questions
    const maxLength = 50;
    let sanitized = text.length > maxLength
      ? text.substring(0, maxLength) + '...'
      : text;

    // Add question number prefix
    sanitized = `Q${index + 1}: ${sanitized}`;

    return sanitized;
  }

  /**
   * Escape a single CSV cell
   */
  private static escapeCSVCell(cell: string): string {
    // Convert to string if not already
    const str = String(cell);

    // Check if escaping is needed
    const needsEscaping = str.includes(',') ||
                         str.includes('"') ||
                         str.includes('\n') ||
                         str.includes('\r');

    if (!needsEscaping) {
      return str;
    }

    // Escape double quotes by doubling them
    const escaped = str.replace(/"/g, '""');

    // Wrap in double quotes
    return `"${escaped}"`;
  }

  /**
   * Escape an entire CSV row
   */
  private static escapeCSVRow(cells: string[]): string {
    return cells.map(cell => this.escapeCSVCell(cell)).join(',');
  }

  /**
   * Generate filename for CSV export
   */
  private static generateFilename(survey: Survey): string {
    const sanitizedTitle = survey.title
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '_')
      .replace(/^_+|_+$/g, '')
      .substring(0, 30);

    const date = new Date().toISOString().split('T')[0];
    const timestamp = Date.now();

    return `survey_${survey.id}_${sanitizedTitle}_${date}_${timestamp}.csv`;
  }
}

export default CSVGenerator;
