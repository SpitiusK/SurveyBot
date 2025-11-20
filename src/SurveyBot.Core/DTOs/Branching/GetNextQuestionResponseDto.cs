namespace SurveyBot.Core.DTOs.Branching;

/// <summary>
/// DTO representing the response for getting the next question.
/// </summary>
public class GetNextQuestionResponseDto
{
    /// <summary>
    /// The ID of the next question to display, or null if no specific branch matches
    /// </summary>
    public int? NextQuestionId { get; set; }

    /// <summary>
    /// Whether the survey is complete (no more questions)
    /// </summary>
    public bool IsComplete { get; set; }
}
