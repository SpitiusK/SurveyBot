using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs;

/// <summary>
/// DTO for updating question flow configuration (conditional navigation).
/// Implements IValidatableObject for complex validation rules.
/// </summary>
public class UpdateQuestionFlowDto : IValidatableObject
{
    /// <summary>
    /// Default navigation behavior for all responses.
    /// - null: Sequential flow (next question by OrderIndex)
    /// - NextQuestionDeterminantDto with EndSurvey: End of survey
    /// - NextQuestionDeterminantDto with GoToQuestion: Jump to specific question
    /// </summary>
    public NextQuestionDeterminantDto? DefaultNext { get; set; }

    /// <summary>
    /// For branching questions (SingleChoice, Rating),
    /// mapping of option ID to navigation determinant.
    /// - Key: QuestionOption.Id (must be positive integer)
    /// - Value: Navigation determinant (EndSurvey or GoToQuestion)
    /// </summary>
    public Dictionary<int, NextQuestionDeterminantDto> OptionNextDeterminants { get; set; } = new();

    /// <summary>
    /// Custom validation logic for complex rules.
    /// Validates OptionNextDeterminants structure and determinant validity.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate OptionNextDeterminants keys and values
        if (OptionNextDeterminants != null && OptionNextDeterminants.Any())
        {
            foreach (var kvp in OptionNextDeterminants)
            {
                // Validate option ID (key) is positive
                if (kvp.Key <= 0)
                {
                    results.Add(new ValidationResult(
                        $"Invalid option ID: {kvp.Key}. Option IDs must be positive integers.",
                        new[] { nameof(OptionNextDeterminants) }));
                }

                // Validate determinant (value) is valid
                var validationError = ValidateDeterminant(kvp.Value, $"option {kvp.Key}");
                if (validationError != null)
                {
                    results.Add(new ValidationResult(
                        validationError,
                        new[] { nameof(OptionNextDeterminants) }));
                }
            }
        }

        // Validate DefaultNext if provided
        if (DefaultNext != null)
        {
            var validationError = ValidateDeterminant(DefaultNext, "default navigation");
            if (validationError != null)
            {
                results.Add(new ValidationResult(
                    validationError,
                    new[] { nameof(DefaultNext) }));
            }
        }

        return results;
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
