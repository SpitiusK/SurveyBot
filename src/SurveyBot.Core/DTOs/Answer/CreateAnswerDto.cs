using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Answer;

/// <summary>
/// Data transfer object for creating or updating an answer.
/// Used when submitting individual answers during survey response.
/// </summary>
public class CreateAnswerDto
{
    /// <summary>
    /// Gets or sets the ID of the question being answered.
    /// </summary>
    [Required(ErrorMessage = "Question ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Question ID must be positive")]
    public int QuestionId { get; set; }

    /// <summary>
    /// Gets or sets the text answer for text-based questions.
    /// Required for Text type questions.
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Answer text cannot exceed 5000 characters")]
    public string? AnswerText { get; set; }

    /// <summary>
    /// Gets or sets the selected option(s) for choice-based questions.
    /// Required for SingleChoice and MultipleChoice questions.
    /// </summary>
    public List<string>? SelectedOptions { get; set; }

    /// <summary>
    /// Gets or sets the rating value for rating questions.
    /// Required for Rating type questions. Must be between 1 and 5.
    /// </summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int? RatingValue { get; set; }

    /// <summary>
    /// Gets or sets the JSON answer for questions requiring structured data.
    /// Used for Location questions (latitude, longitude) and other types with complex answer formats.
    /// </summary>
    public string? AnswerJson { get; set; }
}
