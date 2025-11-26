using System.Text.Json.Serialization;
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.ValueObjects;

/// <summary>
/// Value object representing the next step after answering a question.
/// Enforces business rules: GoToQuestion requires valid ID > 0, EndSurvey has null ID.
/// Immutable with value semantics (equality based on Type and NextQuestionId).
/// Part of DDD clean architecture - eliminates magic value 0.
/// </summary>
public sealed class NextQuestionDeterminant : IEquatable<NextQuestionDeterminant>
{
    /// <summary>
    /// The type of next step (GoToQuestion or EndSurvey).
    /// </summary>
    [JsonPropertyName("type")]
    public NextStepType Type { get; private set; }

    /// <summary>
    /// The ID of the next question (only when Type is GoToQuestion).
    /// Null when Type is EndSurvey.
    /// </summary>
    [JsonPropertyName("nextQuestionId")]
    public int? NextQuestionId { get; private set; }

    /// <summary>
    /// Private constructor to enforce factory pattern.
    /// Use ToQuestion() or End() factory methods.
    /// </summary>
    [JsonConstructor]
    private NextQuestionDeterminant(NextStepType type, int? nextQuestionId)
    {
        Type = type;
        NextQuestionId = nextQuestionId;

        // Enforce invariants
        ValidateInvariants();
    }

    /// <summary>
    /// Factory method: Create a determinant that navigates to a specific question.
    /// </summary>
    /// <param name="questionId">The ID of the question to navigate to (must be > 0).</param>
    /// <returns>NextQuestionDeterminant with GoToQuestion type.</returns>
    /// <exception cref="ArgumentException">If questionId is not greater than 0.</exception>
    public static NextQuestionDeterminant ToQuestion(int questionId)
    {
        if (questionId <= 0)
        {
            throw new ArgumentException("Question ID must be greater than 0.", nameof(questionId));
        }

        return new NextQuestionDeterminant(NextStepType.GoToQuestion, questionId);
    }

    /// <summary>
    /// Factory method: Create a determinant that ends the survey.
    /// </summary>
    /// <returns>NextQuestionDeterminant with EndSurvey type.</returns>
    public static NextQuestionDeterminant End()
    {
        return new NextQuestionDeterminant(NextStepType.EndSurvey, null);
    }

    /// <summary>
    /// Validates the invariants of the value object.
    /// </summary>
    /// <exception cref="InvalidOperationException">If invariants are violated.</exception>
    private void ValidateInvariants()
    {
        switch (Type)
        {
            case NextStepType.GoToQuestion:
                if (!NextQuestionId.HasValue || NextQuestionId.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "GoToQuestion type requires a valid NextQuestionId greater than 0.");
                }
                break;

            case NextStepType.EndSurvey:
                if (NextQuestionId.HasValue)
                {
                    throw new InvalidOperationException(
                        "EndSurvey type must not have a NextQuestionId.");
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown NextStepType: {Type}");
        }
    }

    #region Equality (Value Semantics)

    /// <summary>
    /// Determines equality based on Type and NextQuestionId values.
    /// </summary>
    public bool Equals(NextQuestionDeterminant? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Type == other.Type && NextQuestionId == other.NextQuestionId;
    }

    /// <summary>
    /// Determines equality based on Type and NextQuestionId values.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as NextQuestionDeterminant);
    }

    /// <summary>
    /// Generates hash code based on Type and NextQuestionId.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, NextQuestionId);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(NextQuestionDeterminant? left, NextQuestionDeterminant? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(NextQuestionDeterminant? left, NextQuestionDeterminant? right)
    {
        return !(left == right);
    }

    #endregion

    /// <summary>
    /// Returns a string representation of the determinant.
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
