using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.DTOs.Media;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.DTOs.Question;

/// <summary>
/// Data transfer object for creating a question with INDEX-BASED flow references.
/// Used in batch survey update operations where questions don't have database IDs yet.
///
/// KEY DIFFERENCE from CreateQuestionDto:
/// - CreateQuestionDto uses database IDs for flow references (for single question creation)
/// - CreateQuestionWithFlowDto uses ARRAY INDEXES for flow references (for batch operations)
///
/// The backend transforms indexes to database IDs after creating all questions.
/// </summary>
public class CreateQuestionWithFlowDto : IValidatableObject
{
    /// <summary>
    /// Gets or sets the question text (supports HTML from ReactQuill).
    /// </summary>
    [Required(ErrorMessage = "Question text is required")]
    [MaxLength(5000, ErrorMessage = "Question text cannot exceed 5000 characters")]
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
    /// Gets or sets the position of this question in the survey (0-based).
    /// Questions are displayed in ascending order by OrderIndex.
    /// </summary>
    [Required(ErrorMessage = "Order index is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Order index must be non-negative")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// Gets or sets the options for choice-based questions.
    /// Required for SingleChoice and MultipleChoice question types.
    /// Each option can have its own flow configuration via OptionFlows.
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Gets or sets the media content to attach to this question.
    /// </summary>
    public MediaContentDto? MediaContent { get; set; }

    // === INDEX-BASED FLOW CONFIGURATION ===
    // These use array indexes (0, 1, 2...) instead of database IDs
    // Backend transforms to database IDs after question creation

    /// <summary>
    /// Gets or sets the DEFAULT next question INDEX for non-branching questions.
    /// - For Text, MultipleChoice, Rating, Number, Date, Location: All answers go here
    /// - For SingleChoice: Used as fallback when option doesn't define flow
    /// - null: End survey after this question (no next question)
    /// - -1: Sequential flow (use OrderIndex + 1)
    /// </summary>
    /// <remarks>
    /// INDEX-BASED: This is an array index (0-based), NOT a database ID.
    /// Value -1 means "continue to next question by order".
    /// Value null means "end survey".
    /// </remarks>
    public int? DefaultNextQuestionIndex { get; set; }

    /// <summary>
    /// Gets or sets per-option flow configuration for SingleChoice questions.
    /// Key: Option index (0-based, matching Options array)
    /// Value: Next question index (0-based) or null (end survey) or -1 (sequential)
    /// </summary>
    /// <remarks>
    /// INDEX-BASED: Both keys and values are array indexes, NOT database IDs.
    /// Example: { 0: 2, 1: null, 2: 3 } means:
    /// - Option 0 goes to question at index 2
    /// - Option 1 ends the survey
    /// - Option 2 goes to question at index 3
    /// </remarks>
    public Dictionary<int, int?>? OptionNextQuestionIndexes { get; set; }

    /// <summary>
    /// Validates the DTO ensuring options and flow configuration are consistent.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate options for choice-based questions
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
            else if (Options.Any(o => o.Length > 500))
            {
                yield return new ValidationResult(
                    "Option text cannot exceed 500 characters",
                    new[] { nameof(Options) });
            }
        }
        else if (Options != null && Options.Any())
        {
            yield return new ValidationResult(
                "Text, Rating, Number, Date, and Location questions should not have options",
                new[] { nameof(Options) });
        }

        // Validate OptionNextQuestionIndexes only for SingleChoice and Rating
        if (OptionNextQuestionIndexes != null && OptionNextQuestionIndexes.Any())
        {
            if (QuestionType != QuestionType.SingleChoice && QuestionType != QuestionType.Rating)
            {
                yield return new ValidationResult(
                    "OptionNextQuestionIndexes can only be used with SingleChoice and Rating questions",
                    new[] { nameof(OptionNextQuestionIndexes) });
            }
            else if (Options != null)
            {
                // Validate option indexes are within bounds
                foreach (var kvp in OptionNextQuestionIndexes)
                {
                    if (kvp.Key < 0 || kvp.Key >= Options.Count)
                    {
                        yield return new ValidationResult(
                            $"Invalid option index {kvp.Key}. Valid range: 0-{Options.Count - 1}",
                            new[] { nameof(OptionNextQuestionIndexes) });
                    }

                    // Note: kvp.Value validation (question index) is done at service level
                    // because we need to know total question count
                }
            }
        }

        // DefaultNextQuestionIndex validation is done at service level
        // because we need to know total question count in the survey
    }

    /// <summary>
    /// Returns true if this question type supports per-option branching.
    /// </summary>
    public bool SupportsBranching => QuestionType == QuestionType.SingleChoice || QuestionType == QuestionType.Rating;
}
