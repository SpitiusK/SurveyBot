import type { Survey, Response, Answer, Question } from '../../types';
import { QuestionType as QT } from '../../types';
import { stripHtml } from '../../utils/stringUtils';
import { encodeWindows1251 } from '../../utils/encodingUtils';

export interface ExportOptions {
  includeMetadata: boolean;
  includeTimestamps: boolean;
  exportFormat: 'all' | 'completed' | 'incomplete';
  questionFilter: 'all' | 'statistics_only';
  delimiter: ',' | ';';  // CSV delimiter - semicolon recommended for Russian/European Excel
  encoding: 'utf-8' | 'windows-1251';  // File encoding - Windows-1251 for Russian Excel
}

/**
 * CSV Generator for Survey Responses
 * Handles conversion of survey responses to CSV format with proper escaping
 */
export class CSVGenerator {
  private static readonly CHUNK_SIZE = 500; // Process responses in chunks for large datasets
  private static readonly UTF8_BOM = '\uFEFF'; // UTF-8 Byte Order Mark for Windows Excel compatibility
  // Note: SEP_DIRECTIVE is now generated dynamically based on options.delimiter

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

    // Combine with sep directive (for Excel locale compatibility), headers, and rows
    const sepDirective = `sep=${options.delimiter}\n`;
    return sepDirective + [headers, ...rows].join('\n');
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

    let blob: Blob;
    if (options.encoding === 'windows-1251') {
      // Windows-1251 encoding for Russian Excel compatibility
      const bytes = encodeWindows1251(csvContent);
      blob = new Blob([bytes], { type: 'text/csv;charset=windows-1251' });
    } else {
      // UTF-8 with BOM for universal compatibility
      const encoder = new TextEncoder();
      const bom = new Uint8Array([0xEF, 0xBB, 0xBF]); // UTF-8 BOM as raw bytes
      const contentBytes = encoder.encode(csvContent);
      // Combine BOM and content bytes
      const combinedBytes = new Uint8Array(bom.length + contentBytes.length);
      combinedBytes.set(bom, 0);
      combinedBytes.set(contentBytes, bom.length);
      blob = new Blob([combinedBytes], { type: 'text/csv;charset=utf-8' });
    }

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

    // Add sep directive for Excel locale compatibility
    const sepDirective = `sep=${options.delimiter}\n`;
    const csvContent = sepDirective + chunks.join('\n');

    let blob: Blob;
    if (options.encoding === 'windows-1251') {
      // Windows-1251 encoding for Russian Excel compatibility
      const bytes = encodeWindows1251(csvContent);
      blob = new Blob([bytes], { type: 'text/csv;charset=windows-1251' });
    } else {
      // UTF-8 with BOM for universal compatibility
      const encoder = new TextEncoder();
      const bom = new Uint8Array([0xEF, 0xBB, 0xBF]); // UTF-8 BOM as raw bytes
      const contentBytes = encoder.encode(csvContent);
      // Combine BOM and content bytes
      const combinedBytes = new Uint8Array(bom.length + contentBytes.length);
      combinedBytes.set(bom, 0);
      combinedBytes.set(contentBytes, bom.length);
      blob = new Blob([combinedBytes], { type: 'text/csv;charset=utf-8' });
    }

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
   * Filter questions based on includeInStatistics flag
   */
  private static filterQuestions(
    questions: Question[],
    filter: 'all' | 'statistics_only'
  ): Question[] {
    if (filter === 'statistics_only') {
      return questions.filter(q => q.includeInStatistics !== false);
    }
    return questions;
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

    // Filter questions based on option
    const questions = this.filterQuestions(survey.questions, options.questionFilter);

    // Add question columns (use original orderIndex to preserve survey question numbers)
    questions.forEach((question) => {
      const columnName = this.sanitizeQuestionText(question.questionText, question.orderIndex);
      headers.push(columnName);
    });

    return this.escapeCSVRow(headers, options.delimiter);
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

    // Filter questions based on option
    const questions = this.filterQuestions(survey.questions, options.questionFilter);

    // Add answers for each filtered question
    questions.forEach(question => {
      const answer = response.answers?.find(a => a.questionId === question.id);
      const answerValue = this.formatAnswer(question, answer);
      cells.push(answerValue);
    });

    return this.escapeCSVRow(cells, options.delimiter);
  }

  /**
   * Format answer based on question type
   * Supports all question types: Text, SingleChoice, MultipleChoice, Rating, Number, Date, Location
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

        case QT.Number:
          return this.formatNumberAnswer(answer);

        case QT.Date:
          return this.formatDateAnswer(answer);

        case QT.Location:
          return this.formatLocationAnswer(answer);

        default:
          // Fallback to displayValue for any unhandled types
          return answer.displayValue || answer.answerText || '';
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
   * Uses selectedOptions array from AnswerDto (first element for single choice)
   */
  private static formatSingleChoiceAnswer(answer: Answer): string {
    // Use selectedOptions array (first element for single choice)
    if (answer.selectedOptions && answer.selectedOptions.length > 0) {
      return String(answer.selectedOptions[0]);
    }
    // Fallback to displayValue if available
    if (answer.displayValue) {
      return answer.displayValue;
    }
    // Legacy fallback for answerText
    if (answer.answerText) {
      return answer.answerText;
    }
    return '';
  }

  /**
   * Format multiple choice answer
   * Uses selectedOptions array from AnswerDto (joined with semicolons)
   */
  private static formatMultipleChoiceAnswer(answer: Answer): string {
    // Use selectedOptions array directly
    if (answer.selectedOptions && Array.isArray(answer.selectedOptions) && answer.selectedOptions.length > 0) {
      return answer.selectedOptions.join('; ');
    }
    // Fallback to displayValue if available
    if (answer.displayValue) {
      return answer.displayValue;
    }
    return '';
  }

  /**
   * Format rating answer
   * Uses ratingValue property from AnswerDto (numeric 1-5)
   */
  private static formatRatingAnswer(answer: Answer): string {
    // Use ratingValue property directly
    if (answer.ratingValue !== null && answer.ratingValue !== undefined) {
      return String(answer.ratingValue);
    }
    // Fallback to displayValue if available
    if (answer.displayValue) {
      return answer.displayValue;
    }
    // Legacy fallback for answerText
    if (answer.answerText) {
      return answer.answerText;
    }
    return '';
  }

  /**
   * Format number answer
   * Uses numberValue property from AnswerDto
   */
  private static formatNumberAnswer(answer: Answer): string {
    if (answer.numberValue !== null && answer.numberValue !== undefined) {
      return String(answer.numberValue);
    }
    if (answer.displayValue) {
      return answer.displayValue;
    }
    if (answer.answerText) {
      return answer.answerText;
    }
    return '';
  }

  /**
   * Format date answer
   * Uses dateValue property from AnswerDto, formats as DD.MM.YYYY
   */
  private static formatDateAnswer(answer: Answer): string {
    if (answer.dateValue) {
      try {
        const date = new Date(answer.dateValue);
        if (!isNaN(date.getTime())) {
          // Format as DD.MM.YYYY (Russian/European format)
          const day = date.getDate().toString().padStart(2, '0');
          const month = (date.getMonth() + 1).toString().padStart(2, '0');
          const year = date.getFullYear();
          return `${day}.${month}.${year}`;
        }
      } catch {
        // Return raw value if parsing fails
        return answer.dateValue;
      }
    }
    if (answer.displayValue) {
      return answer.displayValue;
    }
    if (answer.answerText) {
      return answer.answerText;
    }
    return '';
  }

  /**
   * Format location answer
   * Uses latitude/longitude properties from AnswerDto
   */
  private static formatLocationAnswer(answer: Answer): string {
    if (answer.latitude !== null && answer.latitude !== undefined &&
        answer.longitude !== null && answer.longitude !== undefined) {
      return `${answer.latitude}, ${answer.longitude}`;
    }
    if (answer.displayValue) {
      return answer.displayValue;
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
   * Strips HTML tags to prevent Excel encoding issues with <p> tags
   */
  private static sanitizeQuestionText(text: string, index: number): string {
    // Strip HTML tags first (prevents Excel from ignoring UTF-8 BOM when it sees <p> tags)
    const stripped = stripHtml(text);

    // Truncate long questions
    const maxLength = 50;
    let sanitized = stripped.length > maxLength
      ? stripped.substring(0, maxLength) + '...'
      : stripped;

    // Add question number prefix
    sanitized = `Q${index + 1}: ${sanitized}`;

    return sanitized;
  }

  /**
   * Escape a single CSV cell
   * @param cell - The cell content to escape
   * @param delimiter - The delimiter being used (to check if it needs escaping)
   */
  private static escapeCSVCell(cell: string, delimiter: ',' | ';' = ','): string {
    // Convert to string if not already
    const str = String(cell);

    // Check if escaping is needed (include the delimiter in the check)
    const needsEscaping = str.includes(delimiter) ||
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
   * @param cells - Array of cell values
   * @param delimiter - The delimiter to use between cells
   */
  private static escapeCSVRow(cells: string[], delimiter: ',' | ';' = ','): string {
    return cells.map(cell => this.escapeCSVCell(cell, delimiter)).join(delimiter);
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
