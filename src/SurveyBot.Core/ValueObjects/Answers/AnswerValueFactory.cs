using System.Text.Json;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Factory for parsing stored JSON into appropriate AnswerValue subtype.
/// Provides both strict parsing (throws exceptions) and lenient parsing (returns null on failure).
/// </summary>
public static class AnswerValueFactory
{
    /// <summary>
    /// Parses JSON from database into the correct AnswerValue subtype.
    /// </summary>
    /// <param name="json">JSON string from Answer.AnswerJson or Answer.AnswerValueJson column</param>
    /// <param name="questionType">Type of question this answer is for</param>
    /// <returns>Parsed AnswerValue instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON cannot be parsed</exception>
    /// <exception cref="InvalidQuestionTypeException">If question type is not supported</exception>
    public static AnswerValue Parse(string json, QuestionType questionType)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(
                0,
                questionType,
                "Answer JSON cannot be empty");

        return questionType switch
        {
            QuestionType.Text => TextAnswerValue.FromJson(json),
            QuestionType.SingleChoice => SingleChoiceAnswerValue.FromJson(json),
            QuestionType.MultipleChoice => MultipleChoiceAnswerValue.FromJson(json),
            QuestionType.Rating => RatingAnswerValue.FromJson(json),
            QuestionType.Location => LocationAnswerValue.FromJson(json),
            _ => throw new InvalidQuestionTypeException(questionType)
        };
    }

    /// <summary>
    /// Attempts to parse JSON, returning null on failure instead of throwing.
    /// Useful for backward compatibility and lenient parsing scenarios.
    /// </summary>
    /// <param name="json">JSON string to parse</param>
    /// <param name="questionType">Type of question this answer is for</param>
    /// <returns>Parsed AnswerValue, or null if parsing fails</returns>
    public static AnswerValue? TryParse(string? json, QuestionType questionType)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return Parse(json, questionType);
        }
        catch (InvalidAnswerFormatException)
        {
            return null;
        }
        catch (InvalidQuestionTypeException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses JSON with automatic type detection from the JSON content.
    /// Uses polymorphic type discriminator if present.
    /// </summary>
    /// <param name="json">JSON string with type discriminator</param>
    /// <returns>Parsed AnswerValue instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON cannot be parsed or type cannot be determined</exception>
    public static AnswerValue ParseWithTypeDiscriminator(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException("Answer JSON cannot be empty");

        try
        {
            // Try to deserialize using System.Text.Json polymorphic deserialization
            var result = JsonSerializer.Deserialize<AnswerValue>(json);
            if (result != null)
                return result;

            throw new InvalidAnswerFormatException("Failed to deserialize answer value");
        }
        catch (JsonException ex)
        {
            // Fall back to trying to detect type from content
            return ParseWithTypeDetection(json, ex);
        }
    }

    /// <summary>
    /// Detects the answer type from JSON content structure.
    /// Handles $type discriminator in any position (PostgreSQL JSONB may reorder properties).
    /// Used by EF Core HasConversion for database deserialization.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <param name="innerException">Optional inner exception for error context</param>
    /// <returns>Parsed AnswerValue instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON cannot be parsed or type cannot be determined</exception>
    public static AnswerValue ParseWithTypeDetection(string json, Exception? innerException = null)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Check for type discriminator first
            if (root.TryGetProperty("$type", out var typeProperty))
            {
                var typeName = typeProperty.GetString();
                return typeName switch
                {
                    "Text" => TextAnswerValue.FromJson(json),
                    "SingleChoice" => SingleChoiceAnswerValue.FromJson(json),
                    "MultipleChoice" => MultipleChoiceAnswerValue.FromJson(json),
                    "Rating" => RatingAnswerValue.FromJson(json),
                    "Location" => LocationAnswerValue.FromJson(json),
                    _ => throw new InvalidAnswerFormatException($"Unknown answer type: {typeName}")
                };
            }

            // Detect type from content structure
            if (root.TryGetProperty("text", out _))
                return TextAnswerValue.FromJson(json);

            if (root.TryGetProperty("selectedOptions", out _))
                return MultipleChoiceAnswerValue.FromJson(json);

            if (root.TryGetProperty("selectedOption", out _))
                return SingleChoiceAnswerValue.FromJson(json);

            if (root.TryGetProperty("rating", out _))
                return RatingAnswerValue.FromJson(json);

            if (root.TryGetProperty("latitude", out _) && root.TryGetProperty("longitude", out _))
                return LocationAnswerValue.FromJson(json);

            throw new InvalidAnswerFormatException("Could not determine answer type from JSON content");
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON format: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an appropriate AnswerValue from raw input data.
    /// This is the main entry point for creating answers from user input.
    /// </summary>
    /// <param name="questionType">Type of question being answered</param>
    /// <param name="textAnswer">Text answer (for Text questions)</param>
    /// <param name="selectedOptions">Selected options (for SingleChoice/MultipleChoice)</param>
    /// <param name="ratingValue">Rating value (for Rating questions)</param>
    /// <param name="question">Optional question for validation context</param>
    /// <returns>Appropriate AnswerValue instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If answer data is invalid</exception>
    /// <exception cref="InvalidQuestionTypeException">If question type is not supported</exception>
    public static AnswerValue CreateFromInput(
        QuestionType questionType,
        string? textAnswer = null,
        IEnumerable<string>? selectedOptions = null,
        int? ratingValue = null,
        Question? question = null)
    {
        return questionType switch
        {
            QuestionType.Text => CreateTextAnswer(textAnswer),
            QuestionType.SingleChoice => CreateSingleChoiceAnswer(selectedOptions, question),
            QuestionType.MultipleChoice => CreateMultipleChoiceAnswer(selectedOptions, question),
            QuestionType.Rating => CreateRatingAnswer(ratingValue, question),
            QuestionType.Location => throw new InvalidOperationException(
                "Use LocationAnswerValue.Create() for location answers"),
            _ => throw new InvalidQuestionTypeException(questionType)
        };
    }

    private static TextAnswerValue CreateTextAnswer(string? textAnswer)
    {
        if (string.IsNullOrWhiteSpace(textAnswer))
            throw new InvalidAnswerFormatException(0, QuestionType.Text, "Text answer is required");

        return TextAnswerValue.Create(textAnswer);
    }

    private static SingleChoiceAnswerValue CreateSingleChoiceAnswer(
        IEnumerable<string>? selectedOptions,
        Question? question)
    {
        var options = selectedOptions?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

        if (options == null || options.Count == 0)
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "No option selected");

        if (options.Count > 1)
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "Single choice allows only one selection");

        var selectedOption = options[0];

        if (question != null && question.Options.Any())
        {
            return SingleChoiceAnswerValue.Create(selectedOption, question.Options);
        }

        // Legacy: no structured options available
        return SingleChoiceAnswerValue.CreateTrusted(selectedOption);
    }

    private static MultipleChoiceAnswerValue CreateMultipleChoiceAnswer(
        IEnumerable<string>? selectedOptions,
        Question? question)
    {
        var options = selectedOptions?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

        if (options == null || options.Count == 0)
            throw new InvalidAnswerFormatException(0, QuestionType.MultipleChoice, "At least one option must be selected");

        if (question != null && question.Options.Any())
        {
            return MultipleChoiceAnswerValue.Create(options, question.Options);
        }

        // Legacy: no structured options available
        return MultipleChoiceAnswerValue.CreateTrusted(options);
    }

    private static RatingAnswerValue CreateRatingAnswer(int? ratingValue, Question? question)
    {
        if (!ratingValue.HasValue)
            throw new InvalidAnswerFormatException(0, QuestionType.Rating, "Rating value is required");

        if (question != null)
        {
            return RatingAnswerValue.CreateForQuestion(ratingValue.Value, question);
        }

        return RatingAnswerValue.Create(ratingValue.Value);
    }

    /// <summary>
    /// Converts legacy Answer format (AnswerText + AnswerJson) to AnswerValue.
    /// Used for migration from old schema to new value object-based storage.
    /// </summary>
    /// <param name="answerText">Legacy AnswerText field</param>
    /// <param name="answerJson">Legacy AnswerJson field</param>
    /// <param name="questionType">Type of question</param>
    /// <returns>Converted AnswerValue</returns>
    public static AnswerValue? ConvertFromLegacy(
        string? answerText,
        string? answerJson,
        QuestionType questionType)
    {
        // Text questions use AnswerText
        if (questionType == QuestionType.Text)
        {
            if (!string.IsNullOrWhiteSpace(answerText))
                return TextAnswerValue.Create(answerText);
            return null;
        }

        // Other types use AnswerJson
        if (!string.IsNullOrWhiteSpace(answerJson))
        {
            return TryParse(answerJson, questionType);
        }

        return null;
    }
}
