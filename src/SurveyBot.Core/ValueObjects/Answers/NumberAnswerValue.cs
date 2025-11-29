using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a numeric answer.
/// Supports integers and decimals with optional range and decimal places validation.
/// Immutable with value semantics.
/// </summary>
public sealed class NumberAnswerValue : AnswerValue
{
    /// <summary>
    /// Default minimum allowed value (no limit by default).
    /// </summary>
    public const decimal DefaultMinValue = decimal.MinValue;

    /// <summary>
    /// Default maximum allowed value (no limit by default).
    /// </summary>
    public const decimal DefaultMaxValue = decimal.MaxValue;

    /// <summary>
    /// Default decimal places (unlimited by default).
    /// </summary>
    public const int DefaultDecimalPlaces = -1;

    /// <summary>
    /// Gets the numeric value.
    /// </summary>
    [JsonPropertyName("number")]
    public decimal Value { get; private set; }

    /// <summary>
    /// Gets the minimum allowed value (optional, for validation context).
    /// </summary>
    [JsonPropertyName("minValue")]
    public decimal? MinValue { get; private set; }

    /// <summary>
    /// Gets the maximum allowed value (optional, for validation context).
    /// </summary>
    [JsonPropertyName("maxValue")]
    public decimal? MaxValue { get; private set; }

    /// <summary>
    /// Gets the number of decimal places (optional, for validation context).
    /// -1 means unlimited.
    /// </summary>
    [JsonPropertyName("decimalPlaces")]
    public int? DecimalPlaces { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Number;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => FormatNumber(Value, DecimalPlaces);

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private NumberAnswerValue(decimal value, decimal? minValue, decimal? maxValue, int? decimalPlaces)
    {
        Value = value;
        MinValue = minValue;
        MaxValue = maxValue;
        DecimalPlaces = decimalPlaces;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private NumberAnswerValue() : this(0m, null, null, null)
    {
    }

    /// <summary>
    /// Creates a new number answer with validation.
    /// </summary>
    /// <param name="value">Numeric value</param>
    /// <param name="minValue">Optional minimum value for validation</param>
    /// <param name="maxValue">Optional maximum value for validation</param>
    /// <param name="decimalPlaces">Optional decimal places limit (-1 for unlimited)</param>
    /// <returns>Validated number answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If value is out of range or has too many decimal places</exception>
    public static NumberAnswerValue Create(
        decimal value,
        decimal? minValue = null,
        decimal? maxValue = null,
        int? decimalPlaces = null)
    {
        // Validate range
        if (minValue.HasValue && value < minValue.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Number,
                $"Value {value} is less than minimum {minValue.Value}");

        if (maxValue.HasValue && value > maxValue.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Number,
                $"Value {value} is greater than maximum {maxValue.Value}");

        // Validate decimal places
        if (decimalPlaces.HasValue && decimalPlaces.Value >= 0)
        {
            var actualDecimalPlaces = GetDecimalPlaces(value);
            if (actualDecimalPlaces > decimalPlaces.Value)
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.Number,
                    $"Value {value} has {actualDecimalPlaces} decimal places, maximum allowed is {decimalPlaces.Value}");
        }

        return new NumberAnswerValue(value, minValue, maxValue, decimalPlaces);
    }

    /// <summary>
    /// Creates a number answer from a question's configuration.
    /// Extracts min/max/decimal places from question's OptionsJson.
    /// </summary>
    /// <param name="value">Numeric value</param>
    /// <param name="question">The question being answered</param>
    /// <returns>Validated number answer</returns>
    public static NumberAnswerValue CreateForQuestion(decimal value, Question question)
    {
        if (question.QuestionType != QuestionType.Number)
            throw new InvalidAnswerFormatException(
                question.Id,
                question.QuestionType,
                "Question is not a number type question");

        var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question.OptionsJson);
        return Create(value, minValue, maxValue, decimalPlaces);
    }

    /// <summary>
    /// Parses a string value into a NumberAnswerValue.
    /// Accepts both period and comma as decimal separators.
    /// </summary>
    /// <param name="text">String representation of number</param>
    /// <param name="minValue">Optional minimum value for validation</param>
    /// <param name="maxValue">Optional maximum value for validation</param>
    /// <param name="decimalPlaces">Optional decimal places limit</param>
    /// <returns>Validated number answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If text is not a valid number</exception>
    public static NumberAnswerValue Parse(
        string text,
        decimal? minValue = null,
        decimal? maxValue = null,
        int? decimalPlaces = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidAnswerFormatException(0, QuestionType.Number, "Number value is required");

        // Normalize decimal separator (accept both comma and period)
        var normalizedText = text.Trim().Replace(',', '.');

        if (!decimal.TryParse(normalizedText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Number,
                $"'{text}' is not a valid number");
        }

        return Create(value, minValue, maxValue, decimalPlaces);
    }

    /// <summary>
    /// Tries to parse a string value into a NumberAnswerValue without throwing.
    /// </summary>
    /// <param name="text">String representation of number</param>
    /// <param name="result">Parsed result if successful</param>
    /// <param name="minValue">Optional minimum value for validation</param>
    /// <param name="maxValue">Optional maximum value for validation</param>
    /// <param name="decimalPlaces">Optional decimal places limit</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParse(
        string text,
        out NumberAnswerValue? result,
        decimal? minValue = null,
        decimal? maxValue = null,
        int? decimalPlaces = null)
    {
        result = null;

        try
        {
            result = Parse(text, minValue, maxValue, decimalPlaces);
            return true;
        }
        catch (InvalidAnswerFormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Parses number answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed number answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static NumberAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Number, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<NumberData>(json);

            if (data == null)
                throw new InvalidAnswerFormatException(0, QuestionType.Number, "Invalid JSON format");

            // When parsing from DB, we trust the stored data (already validated)
            return new NumberAnswerValue(data.Number, data.MinValue, data.MaxValue, data.DecimalPlaces);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for number answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new NumberData
        {
            Number = Value,
            MinValue = MinValue,
            MaxValue = MaxValue,
            DecimalPlaces = DecimalPlaces
        });

    /// <inheritdoc />
    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.Number)
            return false;

        var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question.OptionsJson);

        if (minValue.HasValue && Value < minValue.Value)
            return false;

        if (maxValue.HasValue && Value > maxValue.Value)
            return false;

        if (decimalPlaces.HasValue && decimalPlaces.Value >= 0)
        {
            var actualDecimalPlaces = GetDecimalPlaces(Value);
            if (actualDecimalPlaces > decimalPlaces.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Parses the number configuration from question's OptionsJson.
    /// </summary>
    /// <param name="optionsJson">Question's OptionsJson field</param>
    /// <returns>Tuple of (minValue, maxValue, decimalPlaces)</returns>
    private static (decimal? minValue, decimal? maxValue, int? decimalPlaces) ParseNumberConfig(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return (null, null, null);

        try
        {
            var options = JsonSerializer.Deserialize<NumberOptions>(optionsJson);
            if (options != null)
            {
                return (options.MinValue, options.MaxValue, options.DecimalPlaces);
            }
        }
        catch (JsonException)
        {
            // Fall through to defaults
        }

        return (null, null, null);
    }

    /// <summary>
    /// Gets the number of decimal places in a decimal value.
    /// </summary>
    private static int GetDecimalPlaces(decimal value)
    {
        // Remove trailing zeros and count decimal places
        value = value / 1.000000000000000000000000000000000m;
        var text = value.ToString(CultureInfo.InvariantCulture);
        var decimalIndex = text.IndexOf('.');
        if (decimalIndex < 0)
            return 0;
        return text.Length - decimalIndex - 1;
    }

    /// <summary>
    /// Formats a number for display, respecting decimal places if specified.
    /// </summary>
    private static string FormatNumber(decimal value, int? decimalPlaces)
    {
        if (decimalPlaces.HasValue && decimalPlaces.Value >= 0)
        {
            return value.ToString($"F{decimalPlaces.Value}", CultureInfo.InvariantCulture);
        }

        // Remove trailing zeros
        return value.ToString("G29", CultureInfo.InvariantCulture);
    }

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other) =>
        other is NumberAnswerValue number && Value == number.Value;

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Value);

    #endregion

    /// <inheritdoc />
    public override string ToString() => $"Number: {DisplayValue}";

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// </summary>
    private sealed class NumberData
    {
        [JsonPropertyName("number")]
        public decimal Number { get; set; }

        [JsonPropertyName("minValue")]
        public decimal? MinValue { get; set; }

        [JsonPropertyName("maxValue")]
        public decimal? MaxValue { get; set; }

        [JsonPropertyName("decimalPlaces")]
        public int? DecimalPlaces { get; set; }
    }

    /// <summary>
    /// Number options from question configuration.
    /// </summary>
    private sealed class NumberOptions
    {
        [JsonPropertyName("MinValue")]
        public decimal? MinValue { get; set; }

        [JsonPropertyName("MaxValue")]
        public decimal? MaxValue { get; set; }

        [JsonPropertyName("DecimalPlaces")]
        public int? DecimalPlaces { get; set; }
    }
}
