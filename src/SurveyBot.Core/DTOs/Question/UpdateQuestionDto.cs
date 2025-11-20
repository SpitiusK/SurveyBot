using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.Entities;

namespace SurveyBot.Core.DTOs.Question;

/// <summary>
/// Data transfer object for updating an existing question.
/// </summary>
public class UpdateQuestionDto
{
    /// <summary>
    /// Gets or sets the question text.
    /// </summary>
    [Required(ErrorMessage = "Question text is required")]
    [MaxLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters")]
    [MinLength(3, ErrorMessage = "Question text must be at least 3 characters")]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question type.
    /// </summary>
    [Required(ErrorMessage = "Question type is required")]
    [EnumDataType(typeof(QuestionType), ErrorMessage = "Invalid question type")]
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this question is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the options for choice-based questions.
    /// Required for SingleChoice and MultipleChoice question types.
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Gets or sets the media content to attach to this question.
    /// Optional - can be null to keep existing media, or set to null to remove media.
    /// Expected format: JSON string representation of MediaContentDto (e.g., {"version":"1.0","items":[]})
    /// </summary>
    public string? MediaContent { get; set; }

    /// <summary>
    /// Validates that options are provided for choice-based questions.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (QuestionType == QuestionType.SingleChoice || QuestionType == QuestionType.MultipleChoice)
        {
            if (Options == null || Options.Count < 2)
            {
                yield return new ValidationResult(
                    "Choice-based questions must have at least 2 options",
                    new[] { nameof(Options) });
            }
            else if (Options.Count > 10)
            {
                yield return new ValidationResult(
                    "Questions cannot have more than 10 options",
                    new[] { nameof(Options) });
            }
            else if (Options.Any(string.IsNullOrWhiteSpace))
            {
                yield return new ValidationResult(
                    "All options must have text",
                    new[] { nameof(Options) });
            }
            else if (Options.Any(o => o.Length > 200))
            {
                yield return new ValidationResult(
                    "Option text cannot exceed 200 characters",
                    new[] { nameof(Options) });
            }
        }
        else if (Options != null && Options.Any())
        {
            yield return new ValidationResult(
                "Text and Rating questions should not have options",
                new[] { nameof(Options) });
        }
    }
}
