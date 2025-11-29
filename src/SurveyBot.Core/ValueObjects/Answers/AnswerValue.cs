using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Abstract base class for all answer value objects.
/// Implements polymorphic answer storage with type-safe access.
/// Follows DDD value object pattern - immutable with value semantics.
/// </summary>
[JsonDerivedType(typeof(TextAnswerValue), typeDiscriminator: "Text")]
[JsonDerivedType(typeof(SingleChoiceAnswerValue), typeDiscriminator: "SingleChoice")]
[JsonDerivedType(typeof(MultipleChoiceAnswerValue), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(RatingAnswerValue), typeDiscriminator: "Rating")]
[JsonDerivedType(typeof(LocationAnswerValue), typeDiscriminator: "Location")]
[JsonDerivedType(typeof(NumberAnswerValue), typeDiscriminator: "Number")]
[JsonDerivedType(typeof(DateAnswerValue), typeDiscriminator: "Date")]
public abstract class AnswerValue : IEquatable<AnswerValue>
{
    /// <summary>
    /// Gets the question type this answer is valid for.
    /// </summary>
    [JsonIgnore]
    public abstract QuestionType QuestionType { get; }

    /// <summary>
    /// Gets a human-readable display value for the answer.
    /// Useful for UI display and CSV export.
    /// </summary>
    [JsonIgnore]
    public abstract string DisplayValue { get; }

    /// <summary>
    /// Converts the answer value to JSON for database storage.
    /// Uses System.Text.Json with polymorphic serialization.
    /// </summary>
    /// <returns>JSON representation of the answer</returns>
    public abstract string ToJson();

    /// <summary>
    /// Validates that this answer is appropriate for the given question.
    /// </summary>
    /// <param name="question">The question being answered</param>
    /// <returns>True if answer is valid for the question</returns>
    public abstract bool IsValidFor(Question question);

    #region Equality (Value Semantics)

    /// <summary>
    /// Determines equality based on answer content.
    /// </summary>
    public abstract bool Equals(AnswerValue? other);

    /// <summary>
    /// Determines equality based on answer content.
    /// </summary>
    public override bool Equals(object? obj) => Equals(obj as AnswerValue);

    /// <summary>
    /// Generates hash code based on answer content.
    /// </summary>
    public abstract override int GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(AnswerValue? left, AnswerValue? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(AnswerValue? left, AnswerValue? right)
    {
        return !(left == right);
    }

    #endregion

    /// <summary>
    /// Factory method to parse JSON into appropriate AnswerValue subtype.
    /// Delegates to AnswerValueFactory.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <param name="questionType">Type of question this answer is for</param>
    /// <returns>Parsed AnswerValue instance</returns>
    public static AnswerValue FromJson(string json, QuestionType questionType) =>
        AnswerValueFactory.Parse(json, questionType);
}
