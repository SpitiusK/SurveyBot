using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a rating answer.
/// Immutable with value semantics.
/// </summary>
public sealed class RatingAnswerValue : AnswerValue
{
    /// <summary>
    /// Default minimum rating value.
    /// </summary>
    public const int DefaultMinRating = 1;

    /// <summary>
    /// Default maximum rating value.
    /// </summary>
    public const int DefaultMaxRating = 5;

    /// <summary>
    /// Gets the rating value.
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; private set; }

    /// <summary>
    /// Gets the minimum allowed rating value used for validation.
    /// </summary>
    [JsonPropertyName("minRating")]
    public int MinRating { get; private set; }

    /// <summary>
    /// Gets the maximum allowed rating value used for validation.
    /// </summary>
    [JsonPropertyName("maxRating")]
    public int MaxRating { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Rating;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => $"{Rating}/{MaxRating}";

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private RatingAnswerValue(int rating, int minRating, int maxRating)
    {
        Rating = rating;
        MinRating = minRating;
        MaxRating = maxRating;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private RatingAnswerValue() : this(0, DefaultMinRating, DefaultMaxRating)
    {
    }

    /// <summary>
    /// Creates a new rating answer with validation (default 1-5 scale).
    /// </summary>
    /// <param name="rating">Rating value (must be 1-5 by default)</param>
    /// <returns>Validated rating answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If rating is out of range</exception>
    public static RatingAnswerValue Create(int rating)
    {
        return Create(rating, DefaultMinRating, DefaultMaxRating);
    }

    /// <summary>
    /// Creates a new rating answer with custom validation range.
    /// </summary>
    /// <param name="rating">Rating value</param>
    /// <param name="minRating">Minimum allowed rating</param>
    /// <param name="maxRating">Maximum allowed rating</param>
    /// <returns>Validated rating answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If rating is out of range</exception>
    public static RatingAnswerValue Create(int rating, int minRating, int maxRating)
    {
        if (minRating >= maxRating)
            throw new ArgumentException($"Min rating ({minRating}) must be less than max rating ({maxRating})");

        if (rating < minRating || rating > maxRating)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Rating,
                $"Rating must be between {minRating} and {maxRating}, got {rating}");

        return new RatingAnswerValue(rating, minRating, maxRating);
    }

    /// <summary>
    /// Creates a rating answer from a question's configuration.
    /// Extracts min/max from question's OptionsJson if available.
    /// </summary>
    /// <param name="rating">Rating value</param>
    /// <param name="question">The question being answered</param>
    /// <returns>Validated rating answer</returns>
    public static RatingAnswerValue CreateForQuestion(int rating, Question question)
    {
        if (question.QuestionType != QuestionType.Rating)
            throw new InvalidAnswerFormatException(
                question.Id,
                question.QuestionType,
                "Question is not a rating type question");

        var (minRating, maxRating) = ParseRatingRange(question.OptionsJson);
        return Create(rating, minRating, maxRating);
    }

    /// <summary>
    /// Creates without full validation (for parsing from trusted database storage).
    /// Still validates basic range consistency.
    /// </summary>
    /// <param name="rating">Rating value</param>
    /// <param name="minRating">Minimum rating (default: 1)</param>
    /// <param name="maxRating">Maximum rating (default: 5)</param>
    /// <returns>Rating answer</returns>
    internal static RatingAnswerValue CreateTrusted(int rating, int minRating = DefaultMinRating, int maxRating = DefaultMaxRating)
    {
        // Basic validation for trusted sources
        if (rating < 1 || rating > 10)
            throw new InvalidAnswerFormatException(0, QuestionType.Rating, $"Rating {rating} is unreasonable");

        return new RatingAnswerValue(rating, minRating, maxRating);
    }

    /// <summary>
    /// Parses rating answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed rating answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static RatingAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Rating, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<RatingData>(json);

            if (data == null)
                throw new InvalidAnswerFormatException(0, QuestionType.Rating, "Invalid JSON format");

            // Use stored min/max if available, otherwise use defaults
            var minRating = data.MinRating ?? DefaultMinRating;
            var maxRating = data.MaxRating ?? DefaultMaxRating;

            // When parsing from DB, we trust the stored data for min/max
            // but still validate the rating is within reasonable bounds
            if (data.Rating < 1 || data.Rating > 10)
                throw new InvalidAnswerFormatException(0, QuestionType.Rating, $"Rating {data.Rating} is unreasonable");

            return new RatingAnswerValue(data.Rating, minRating, maxRating);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for rating answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new RatingData
        {
            Rating = Rating,
            MinRating = MinRating != DefaultMinRating ? MinRating : null,
            MaxRating = MaxRating != DefaultMaxRating ? MaxRating : null
        });

    /// <inheritdoc />
    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.Rating)
            return false;

        var (minRating, maxRating) = ParseRatingRange(question.OptionsJson);
        return Rating >= minRating && Rating <= maxRating;
    }

    /// <summary>
    /// Parses the rating range from question's OptionsJson.
    /// </summary>
    /// <param name="optionsJson">Question's OptionsJson field</param>
    /// <returns>Tuple of (minRating, maxRating)</returns>
    private static (int minRating, int maxRating) ParseRatingRange(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return (DefaultMinRating, DefaultMaxRating);

        try
        {
            var options = JsonSerializer.Deserialize<RatingOptions>(optionsJson);
            if (options != null)
            {
                var min = options.MinValue ?? DefaultMinRating;
                var max = options.MaxValue ?? DefaultMaxRating;
                return (min, max);
            }
        }
        catch (JsonException)
        {
            // Fall through to defaults
        }

        return (DefaultMinRating, DefaultMaxRating);
    }

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other) =>
        other is RatingAnswerValue rating && Rating == rating.Rating;

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Rating);

    #endregion

    /// <inheritdoc />
    public override string ToString() => $"Rating: {Rating}/{MaxRating}";

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// </summary>
    private sealed class RatingData
    {
        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("minRating")]
        public int? MinRating { get; set; }

        [JsonPropertyName("maxRating")]
        public int? MaxRating { get; set; }
    }

    /// <summary>
    /// Rating options from question configuration.
    /// </summary>
    private sealed class RatingOptions
    {
        [JsonPropertyName("MinValue")]
        public int? MinValue { get; set; }

        [JsonPropertyName("MaxValue")]
        public int? MaxValue { get; set; }
    }
}
