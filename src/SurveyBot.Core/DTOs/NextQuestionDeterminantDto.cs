using System.Text.Json.Serialization;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.DTOs;

/// <summary>
/// Data transfer object for NextQuestionDeterminant value object.
/// Used in API requests/responses to represent the next step after answering a question.
/// Enforces business rules: GoToQuestion requires valid ID > 0, EndSurvey has null ID.
/// </summary>
public class NextQuestionDeterminantDto
{
    /// <summary>
    /// Gets or sets the type of next step (GoToQuestion or EndSurvey).
    /// </summary>
    [JsonPropertyName("type")]
    public NextStepType Type { get; set; }

    /// <summary>
    /// Gets or sets the ID of the next question (only when Type is GoToQuestion).
    /// Null when Type is EndSurvey.
    /// Must be greater than 0 when Type is GoToQuestion.
    /// </summary>
    [JsonPropertyName("nextQuestionId")]
    public int? NextQuestionId { get; set; }

    /// <summary>
    /// Default constructor for deserialization and validation.
    /// </summary>
    public NextQuestionDeterminantDto()
    {
    }

    /// <summary>
    /// Constructor with validation to enforce business rules.
    /// </summary>
    /// <param name="type">The type of next step.</param>
    /// <param name="nextQuestionId">The ID of the next question (if applicable).</param>
    /// <exception cref="ArgumentException">If business rules are violated.</exception>
    public NextQuestionDeterminantDto(NextStepType type, int? nextQuestionId)
    {
        Type = type;
        NextQuestionId = nextQuestionId;
        Validate();
    }

    /// <summary>
    /// Factory method: Create a DTO that navigates to a specific question.
    /// </summary>
    /// <param name="questionId">The ID of the question to navigate to (must be > 0).</param>
    /// <returns>NextQuestionDeterminantDto with GoToQuestion type.</returns>
    /// <exception cref="ArgumentException">If questionId is not greater than 0.</exception>
    public static NextQuestionDeterminantDto ToQuestion(int questionId)
    {
        if (questionId <= 0)
        {
            throw new ArgumentException("Question ID must be greater than 0.", nameof(questionId));
        }

        return new NextQuestionDeterminantDto
        {
            Type = NextStepType.GoToQuestion,
            NextQuestionId = questionId
        };
    }

    /// <summary>
    /// Factory method: Create a DTO that ends the survey.
    /// </summary>
    /// <returns>NextQuestionDeterminantDto with EndSurvey type.</returns>
    public static NextQuestionDeterminantDto End()
    {
        return new NextQuestionDeterminantDto
        {
            Type = NextStepType.EndSurvey,
            NextQuestionId = null
        };
    }

    /// <summary>
    /// Validates the DTO to ensure business rules are met.
    /// </summary>
    /// <exception cref="ArgumentException">If business rules are violated.</exception>
    public void Validate()
    {
        switch (Type)
        {
            case NextStepType.GoToQuestion:
                if (!NextQuestionId.HasValue || NextQuestionId.Value <= 0)
                {
                    throw new ArgumentException(
                        "GoToQuestion type requires a valid NextQuestionId greater than 0.",
                        nameof(NextQuestionId));
                }
                break;

            case NextStepType.EndSurvey:
                if (NextQuestionId.HasValue)
                {
                    throw new ArgumentException(
                        "EndSurvey type must not have a NextQuestionId.",
                        nameof(NextQuestionId));
                }
                break;

            default:
                throw new ArgumentException($"Unknown NextStepType: {Type}", nameof(Type));
        }
    }

    /// <summary>
    /// Returns a string representation of the DTO.
    /// </summary>
    public override string ToString()
    {
        return Type switch
        {
            NextStepType.GoToQuestion => $"GoToQuestion(Id: {NextQuestionId})",
            NextStepType.EndSurvey => "EndSurvey",
            _ => $"Unknown({Type})"
        };
    }
}
