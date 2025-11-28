using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a text answer.
/// Immutable with value semantics.
/// </summary>
public sealed class TextAnswerValue : AnswerValue
{
    /// <summary>
    /// Maximum allowed length for text answers.
    /// </summary>
    public const int MaxLength = 5000;

    /// <summary>
    /// Gets the text answer content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Text;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => Text;

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private TextAnswerValue(string text)
    {
        Text = text;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private TextAnswerValue() : this(string.Empty)
    {
    }

    /// <summary>
    /// Creates a new text answer with validation.
    /// </summary>
    /// <param name="text">The answer text (1-5000 characters)</param>
    /// <returns>Validated text answer instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If text is invalid</exception>
    public static TextAnswerValue Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Text,
                "Text answer cannot be empty");

        var trimmed = text.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Text,
                $"Text answer cannot exceed {MaxLength} characters (got {trimmed.Length})");

        return new TextAnswerValue(trimmed);
    }

    /// <summary>
    /// Creates a text answer allowing empty/null (for optional questions).
    /// </summary>
    /// <param name="text">The answer text (may be null or empty)</param>
    /// <returns>Text answer instance, or null if input is empty</returns>
    public static TextAnswerValue? CreateOptional(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return Create(text);
    }

    /// <summary>
    /// Parses text answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed text answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static TextAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Text, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<TextAnswerData>(json);

            if (data == null || string.IsNullOrWhiteSpace(data.Text))
                throw new InvalidAnswerFormatException(0, QuestionType.Text, "Text property missing in JSON");

            return Create(data.Text);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for text answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new TextAnswerData { Text = Text });

    /// <inheritdoc />
    public override bool IsValidFor(Question question) =>
        question.QuestionType == QuestionType.Text;

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other) =>
        other is TextAnswerValue text && Text == text.Text;

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Text);

    #endregion

    /// <inheritdoc />
    public override string ToString() => $"TextAnswer: \"{Text}\"";

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// </summary>
    private sealed class TextAnswerData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
