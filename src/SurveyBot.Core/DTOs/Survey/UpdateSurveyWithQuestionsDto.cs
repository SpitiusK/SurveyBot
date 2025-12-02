using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.DTOs.Question;

namespace SurveyBot.Core.DTOs.Survey;

/// <summary>
/// Data transfer object for COMPLETE survey update with all questions.
/// Used for the single-endpoint atomic update pattern (delete and recreate).
///
/// This DTO represents the ENTIRE survey state that will replace existing data:
/// - All existing questions are deleted (CASCADE deletes responses/answers)
/// - All questions in this DTO are created as new (with new database IDs)
/// - Question flow is configured using INDEX-BASED references
///
/// IMPORTANT: This operation is DESTRUCTIVE for existing responses!
/// Frontend should warn users when responses exist before calling this endpoint.
/// </summary>
public class UpdateSurveyWithQuestionsDto : IValidatableObject
{
    /// <summary>
    /// Gets or sets the survey title.
    /// </summary>
    [Required(ErrorMessage = "Survey title is required")]
    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the survey description (optional).
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether users can submit multiple responses.
    /// </summary>
    public bool AllowMultipleResponses { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether results should be shown to respondents.
    /// </summary>
    public bool ShowResults { get; set; }

    /// <summary>
    /// Gets or sets whether to activate the survey after update.
    /// If true, survey becomes active and accepting responses.
    /// Validation (cycle detection, endpoints) is performed before activation.
    /// </summary>
    public bool ActivateAfterUpdate { get; set; } = true;

    /// <summary>
    /// Gets or sets the COMPLETE list of questions for this survey.
    /// These will REPLACE all existing questions (delete old, create new).
    ///
    /// IMPORTANT:
    /// - Order in this list determines OrderIndex (0, 1, 2, ...)
    /// - Flow references use array indexes (not database IDs)
    /// - All questions get NEW database IDs after creation
    /// - Minimum 1 question required
    /// </summary>
    [Required(ErrorMessage = "At least one question is required")]
    [MinLength(1, ErrorMessage = "Survey must have at least one question")]
    public List<CreateQuestionWithFlowDto> Questions { get; set; } = new();

    /// <summary>
    /// Validates the DTO ensuring survey and question constraints are met.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate minimum questions
        if (Questions == null || Questions.Count == 0)
        {
            yield return new ValidationResult(
                "Survey must have at least one question",
                new[] { nameof(Questions) });
            yield break; // Cannot continue validation without questions
        }

        // Validate maximum questions
        if (Questions.Count > 100)
        {
            yield return new ValidationResult(
                "Survey cannot have more than 100 questions",
                new[] { nameof(Questions) });
        }

        // Validate each question
        for (int i = 0; i < Questions.Count; i++)
        {
            var question = Questions[i];
            if (question == null)
            {
                yield return new ValidationResult(
                    $"Question at index {i} is null",
                    new[] { nameof(Questions) });
                continue;
            }

            // Validate question's own properties
            var questionContext = new ValidationContext(question);
            var questionResults = new List<ValidationResult>();
            Validator.TryValidateObject(question, questionContext, questionResults, true);

            foreach (var result in questionResults)
            {
                yield return new ValidationResult(
                    $"Question {i}: {result.ErrorMessage}",
                    new[] { nameof(Questions) });
            }

            // Validate IValidatableObject
            var customResults = question.Validate(questionContext);
            foreach (var result in customResults)
            {
                yield return new ValidationResult(
                    $"Question {i}: {result.ErrorMessage}",
                    new[] { nameof(Questions) });
            }

            // Validate flow references are within bounds
            if (question.DefaultNextQuestionIndex.HasValue)
            {
                var idx = question.DefaultNextQuestionIndex.Value;
                // -1 means sequential (valid), null is handled elsewhere
                if (idx != -1 && (idx < 0 || idx >= Questions.Count))
                {
                    yield return new ValidationResult(
                        $"Question {i}: DefaultNextQuestionIndex ({idx}) is out of bounds. Valid range: -1 (sequential), 0-{Questions.Count - 1}, or null (end survey)",
                        new[] { nameof(Questions) });
                }

                // Self-reference check
                if (idx == i)
                {
                    yield return new ValidationResult(
                        $"Question {i}: DefaultNextQuestionIndex cannot reference itself",
                        new[] { nameof(Questions) });
                }
            }

            // Validate option flow references
            if (question.OptionNextQuestionIndexes != null)
            {
                foreach (var kvp in question.OptionNextQuestionIndexes)
                {
                    if (kvp.Value.HasValue)
                    {
                        var nextIdx = kvp.Value.Value;
                        // -1 means sequential (valid), null is handled (end survey)
                        if (nextIdx != -1 && (nextIdx < 0 || nextIdx >= Questions.Count))
                        {
                            yield return new ValidationResult(
                                $"Question {i}, Option {kvp.Key}: NextQuestionIndex ({nextIdx}) is out of bounds. Valid range: -1 (sequential), 0-{Questions.Count - 1}, or null (end survey)",
                                new[] { nameof(Questions) });
                        }

                        // Self-reference check
                        if (nextIdx == i)
                        {
                            yield return new ValidationResult(
                                $"Question {i}, Option {kvp.Key}: NextQuestionIndex cannot reference itself",
                                new[] { nameof(Questions) });
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the total number of questions in this update request.
    /// </summary>
    public int QuestionCount => Questions?.Count ?? 0;
}
