using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.Entities;

namespace SurveyBot.Core.DTOs.Question;

/// <summary>
/// Data transfer object for creating a new question.
/// </summary>
public class CreateQuestionDto
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
    /// Defaults to true.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets the options for choice-based questions.
    /// Required for SingleChoice and MultipleChoice question types.
    /// Should be null or empty for Text and Rating questions.
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Gets or sets the media content to attach to this question.
    /// Optional - can be null if no media is provided during creation.
    /// Expected format: JSON string representation of MediaContentDto (e.g., {"version":"1.0","items":[]})
    /// </summary>
    public string? MediaContent { get; set; }

    // NEW: Conditional flow configuration

    /// <summary>
    /// Gets or sets the default navigation behavior for non-branching questions (Text, MultipleChoice, Rating).
    /// For branching questions (SingleChoice), this is used as fallback when option doesn't define navigation.
    /// Null means sequential flow (next question by OrderIndex).
    /// </summary>
    public NextQuestionDeterminantDto? DefaultNext { get; set; }

    /// <summary>
    /// Gets or sets option-specific navigation for branching (SingleChoice questions).
    /// Dictionary key is option index (0-based), value is navigation determinant.
    /// Only applicable when QuestionType is SingleChoice.
    /// </summary>
    public Dictionary<int, NextQuestionDeterminantDto>? OptionNextDeterminants { get; set; }

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

        // Validate conditional flow configuration
        if (OptionNextDeterminants != null && OptionNextDeterminants.Any())
        {
            // OptionNextDeterminants only valid for SingleChoice
            if (QuestionType != QuestionType.SingleChoice)
            {
                yield return new ValidationResult(
                    "OptionNextDeterminants can only be used with SingleChoice questions",
                    new[] { nameof(OptionNextDeterminants) });
            }
            // Validate option indices match options count
            else if (Options != null)
            {
                var maxIndex = OptionNextDeterminants.Keys.Max();
                if (maxIndex >= Options.Count)
                {
                    yield return new ValidationResult(
                        $"OptionNextDeterminants contains invalid option index {maxIndex}. Maximum valid index is {Options.Count - 1}",
                        new[] { nameof(OptionNextDeterminants) });
                }

                var minIndex = OptionNextDeterminants.Keys.Min();
                if (minIndex < 0)
                {
                    yield return new ValidationResult(
                        "OptionNextDeterminants indices must be non-negative",
                        new[] { nameof(OptionNextDeterminants) });
                }

                // Validate each determinant
                foreach (var kvp in OptionNextDeterminants)
                {
                    var validationError = ValidateDeterminant(kvp.Value, $"option {kvp.Key}");
                    if (validationError != null)
                    {
                        yield return new ValidationResult(
                            validationError,
                            new[] { nameof(OptionNextDeterminants) });
                    }
                }
            }
        }

        // Validate DefaultNext if provided
        if (DefaultNext != null)
        {
            var validationError = ValidateDeterminant(DefaultNext, "default navigation");
            if (validationError != null)
            {
                yield return new ValidationResult(
                    validationError,
                    new[] { nameof(DefaultNext) });
            }
        }
    }

    /// <summary>
    /// Validates a NextQuestionDeterminantDto and returns error message if invalid.
    /// </summary>
    /// <param name="determinant">The determinant to validate.</param>
    /// <param name="context">Context description for error message.</param>
    /// <returns>Error message if invalid, null if valid.</returns>
    private static string? ValidateDeterminant(NextQuestionDeterminantDto? determinant, string context)
    {
        if (determinant == null)
        {
            return null;
        }

        try
        {
            determinant.Validate();
            return null;
        }
        catch (ArgumentException ex)
        {
            return $"Invalid navigation for {context}: {ex.Message}";
        }
    }
}
