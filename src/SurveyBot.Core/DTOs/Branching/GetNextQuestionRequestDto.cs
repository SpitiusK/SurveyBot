using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Branching;

/// <summary>
/// DTO for requesting the next question based on an answer.
/// </summary>
public class GetNextQuestionRequestDto
{
    /// <summary>
    /// The answer value to evaluate against branching rules
    /// </summary>
    [Required(ErrorMessage = "Answer is required")]
    public string Answer { get; set; } = string.Empty;
}
