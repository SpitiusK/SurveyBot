namespace SurveyBot.Core.Enums;

/// <summary>
/// Defines the type of action to take after answering a question.
/// Part of the conditional question flow feature.
/// </summary>
public enum NextStepType
{
    /// <summary>
    /// Navigate to a specific question identified by NextQuestionId.
    /// </summary>
    GoToQuestion = 0,

    /// <summary>
    /// End the survey immediately (no more questions).
    /// </summary>
    EndSurvey = 1
}
