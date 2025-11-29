using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a date answer.
/// Uses DD.MM.YYYY format with optional range validation.
/// Immutable with value semantics.
/// </summary>
public sealed class DateAnswerValue : AnswerValue
{
    /// <summary>
    /// The expected date format for input and display.
    /// </summary>
    public const string DateFormat = "dd.MM.yyyy";

    /// <summary>
    /// Gets the date value (time component is always midnight).
    /// </summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(DateFormatJsonConverter))]
    public DateTime Date { get; private set; }

    /// <summary>
    /// Gets the minimum allowed date (optional, for validation context).
    /// </summary>
    [JsonPropertyName("minDate")]
    [JsonConverter(typeof(NullableDateFormatJsonConverter))]
    public DateTime? MinDate { get; private set; }

    /// <summary>
    /// Gets the maximum allowed date (optional, for validation context).
    /// </summary>
    [JsonPropertyName("maxDate")]
    [JsonConverter(typeof(NullableDateFormatJsonConverter))]
    public DateTime? MaxDate { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Date;

    /// <inheritdoc />
    [JsonIgnore]
    public override string DisplayValue => Date.ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private DateAnswerValue(DateTime date, DateTime? minDate, DateTime? maxDate)
    {
        // Strip time component
        Date = date.Date;
        MinDate = minDate?.Date;
        MaxDate = maxDate?.Date;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private DateAnswerValue() : this(DateTime.MinValue, null, null)
    {
    }

    /// <summary>
    /// Creates a new date answer with validation.
    /// </summary>
    /// <param name="date">Date value (time component will be stripped)</param>
    /// <param name="minDate">Optional minimum date for validation</param>
    /// <param name="maxDate">Optional maximum date for validation</param>
    /// <returns>Validated date answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If date is out of range</exception>
    public static DateAnswerValue Create(
        DateTime date,
        DateTime? minDate = null,
        DateTime? maxDate = null)
    {
        // Strip time component before validation
        var dateOnly = date.Date;
        var minDateOnly = minDate?.Date;
        var maxDateOnly = maxDate?.Date;

        // Validate range
        if (minDateOnly.HasValue && dateOnly < minDateOnly.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Date,
                $"Date {dateOnly:dd.MM.yyyy} is before minimum {minDateOnly.Value:dd.MM.yyyy}");

        if (maxDateOnly.HasValue && dateOnly > maxDateOnly.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Date,
                $"Date {dateOnly:dd.MM.yyyy} is after maximum {maxDateOnly.Value:dd.MM.yyyy}");

        return new DateAnswerValue(dateOnly, minDateOnly, maxDateOnly);
    }

    /// <summary>
    /// Creates a date answer from a question's configuration.
    /// Extracts min/max dates from question's OptionsJson.
    /// </summary>
    /// <param name="date">Date value</param>
    /// <param name="question">The question being answered</param>
    /// <returns>Validated date answer</returns>
    public static DateAnswerValue CreateForQuestion(DateTime date, Question question)
    {
        if (question.QuestionType != QuestionType.Date)
            throw new InvalidAnswerFormatException(
                question.Id,
                question.QuestionType,
                "Question is not a date type question");

        var (minDate, maxDate) = ParseDateConfig(question.OptionsJson);
        return Create(date, minDate, maxDate);
    }

    /// <summary>
    /// Parses a string value in DD.MM.YYYY format into a DateAnswerValue.
    /// </summary>
    /// <param name="text">String representation of date in DD.MM.YYYY format</param>
    /// <param name="minDate">Optional minimum date for validation</param>
    /// <param name="maxDate">Optional maximum date for validation</param>
    /// <returns>Validated date answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If text is not a valid date or wrong format</exception>
    public static DateAnswerValue Parse(
        string text,
        DateTime? minDate = null,
        DateTime? maxDate = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidAnswerFormatException(0, QuestionType.Date, "Date value is required");

        var trimmedText = text.Trim();

        // Use ParseExact with strict format matching
        if (!DateTime.TryParseExact(
            trimmedText,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date))
        {
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Date,
                $"'{trimmedText}' is not a valid date. Please use format DD.MM.YYYY (e.g., {DateTime.Today:dd.MM.yyyy})");
        }

        return Create(date, minDate, maxDate);
    }

    /// <summary>
    /// Tries to parse a string value into a DateAnswerValue without throwing.
    /// </summary>
    /// <param name="text">String representation of date in DD.MM.YYYY format</param>
    /// <param name="result">Parsed result if successful</param>
    /// <param name="minDate">Optional minimum date for validation</param>
    /// <param name="maxDate">Optional maximum date for validation</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParse(
        string text,
        out DateAnswerValue? result,
        DateTime? minDate = null,
        DateTime? maxDate = null)
    {
        result = null;

        try
        {
            result = Parse(text, minDate, maxDate);
            return true;
        }
        catch (InvalidAnswerFormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Parses date answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed date answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON is invalid</exception>
    public static DateAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Date, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<DateData>(json);

            if (data == null || string.IsNullOrWhiteSpace(data.Date))
                throw new InvalidAnswerFormatException(0, QuestionType.Date, "Invalid JSON format");

            // Parse date string in DD.MM.YYYY format
            if (!DateTime.TryParseExact(
                data.Date,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.Date,
                    $"Date '{data.Date}' is not in DD.MM.YYYY format");
            }

            // Parse optional min/max dates
            DateTime? minDate = null;
            if (!string.IsNullOrWhiteSpace(data.MinDate))
            {
                if (DateTime.TryParseExact(
                    data.MinDate,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedMinDate))
                {
                    minDate = parsedMinDate;
                }
            }

            DateTime? maxDate = null;
            if (!string.IsNullOrWhiteSpace(data.MaxDate))
            {
                if (DateTime.TryParseExact(
                    data.MaxDate,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedMaxDate))
                {
                    maxDate = parsedMaxDate;
                }
            }

            // Return without validation since we trust stored data
            return new DateAnswerValue(date, minDate, maxDate);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(
                $"Invalid JSON for date answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override string ToJson() =>
        JsonSerializer.Serialize(new
        {
            date = Date.ToString(DateFormat, CultureInfo.InvariantCulture),
            minDate = MinDate?.ToString(DateFormat, CultureInfo.InvariantCulture),
            maxDate = MaxDate?.ToString(DateFormat, CultureInfo.InvariantCulture)
        });

    /// <inheritdoc />
    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.Date)
            return false;

        var (minDate, maxDate) = ParseDateConfig(question.OptionsJson);

        if (minDate.HasValue && Date < minDate.Value.Date)
            return false;

        if (maxDate.HasValue && Date > maxDate.Value.Date)
            return false;

        return true;
    }

    /// <summary>
    /// Parses the date configuration from question's OptionsJson.
    /// </summary>
    /// <param name="optionsJson">Question's OptionsJson field</param>
    /// <returns>Tuple of (minDate, maxDate)</returns>
    private static (DateTime? minDate, DateTime? maxDate) ParseDateConfig(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return (null, null);

        try
        {
            var options = JsonSerializer.Deserialize<DateOptions>(optionsJson);
            if (options != null)
            {
                return (options.MinDate, options.MaxDate);
            }
        }
        catch (JsonException)
        {
            // Fall through to defaults
        }

        return (null, null);
    }

    #region Equality

    /// <inheritdoc />
    public override bool Equals(AnswerValue? other) =>
        other is DateAnswerValue dateAnswer && Date == dateAnswer.Date;

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Date);

    #endregion

    /// <inheritdoc />
    public override string ToString() => $"Date: {DisplayValue}";

    /// <summary>
    /// Internal DTO for JSON serialization.
    /// Uses string properties to match DD.MM.YYYY format from ToJson().
    /// </summary>
    private sealed class DateData
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("minDate")]
        public string? MinDate { get; set; }

        [JsonPropertyName("maxDate")]
        public string? MaxDate { get; set; }
    }

    /// <summary>
    /// Date options from question configuration.
    /// </summary>
    private sealed class DateOptions
    {
        [JsonPropertyName("MinDate")]
        public DateTime? MinDate { get; set; }

        [JsonPropertyName("MaxDate")]
        public DateTime? MaxDate { get; set; }
    }
}
