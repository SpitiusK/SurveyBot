namespace SurveyBot.Core.DTOs.Response;

/// <summary>
/// DTO for the response when saving an answer.
/// Includes the answer ID and next question information for branching support.
/// </summary>
public class SaveAnswerResponseDto
{
    /// <summary>
    /// The ID of the saved answer
    /// </summary>
    public int AnswerId { get; set; }

    /// <summary>
    /// The ID of the next question to display (if branching rules apply), or null
    /// </summary>
    public int? NextQuestionId { get; set; }

    /// <summary>
    /// Whether the survey is complete (no more questions)
    /// </summary>
    public bool IsComplete { get; set; }
}
