using System.ComponentModel.DataAnnotations;
using SurveyBot.Core.DTOs.Answer;

namespace SurveyBot.Core.DTOs.Response;

/// <summary>
/// Data transfer object for submitting an answer to a question in an ongoing response.
/// </summary>
public class SubmitAnswerDto
{
    /// <summary>
    /// Gets or sets the answer data.
    /// </summary>
    [Required(ErrorMessage = "Answer data is required")]
    public CreateAnswerDto Answer { get; set; } = null!;
}
