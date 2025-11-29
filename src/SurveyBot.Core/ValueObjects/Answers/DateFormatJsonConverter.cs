using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Custom JSON converter for DateTime that serializes to DD.MM.YYYY format
/// and deserializes from both DD.MM.YYYY and ISO 8601 formats for backward compatibility.
/// </summary>
public class DateFormatJsonConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "dd.MM.yyyy";

    /// <summary>
    /// Reads DateTime from JSON, accepting both DD.MM.YYYY and ISO 8601 formats.
    /// </summary>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        if (string.IsNullOrWhiteSpace(dateString))
            throw new JsonException("Date value cannot be empty");

        // Try DD.MM.YYYY format first (preferred format)
        if (DateTime.TryParseExact(
            dateString,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date))
        {
            return date.Date; // Strip time component
        }

        // Fallback to ISO 8601 format for backward compatibility with existing data
        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date))
        {
            return date.Date; // Strip time component
        }

        throw new JsonException($"Unable to parse date '{dateString}'. Expected format: {DateFormat} or ISO 8601");
    }

    /// <summary>
    /// Writes DateTime to JSON in DD.MM.YYYY format.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }
}
