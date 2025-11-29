# Number and Date Question Types - Implementation Plan

**Version**: 1.5.1
**Date**: 2025-11-28
**Status**: Ready for Implementation
**Estimated Effort**: 20-28 hours

---

## Executive Summary

This document provides a comprehensive, bug-proof implementation plan for adding **Number** and **Date** question types to SurveyBot. The plan is based on deep architectural analysis of existing patterns and includes all critical integration points.

### Key Decisions

- ✅ **Number Question**: Text input with server-side numeric validation
- ✅ **Date Question**: Text input with DD.MM.YYYY format validation
- ✅ **Format Hint**: Automatically appended to date question text
- ✅ **Value Objects**: Type-safe storage via NumberAnswerValue and DateAnswerValue
- ✅ **OptionsJson**: Configuration storage for min/max ranges and formats

### Implementation Strategy

Follow existing architectural patterns:
- **Number**: Similar to Rating (value object with min/max validation)
- **Date**: Similar to Location (value object with range validation)
- **Both**: Text input like TextQuestionHandler (no complex UI)

---

## Architecture Overview

### Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    NUMBER/DATE QUESTION FLOW                     │
└─────────────────────────────────────────────────────────────────┘

1. CREATION (API)
   POST /api/surveys/{id}/questions
   ├── QuestionType: Number (5) or Date (6)
   ├── OptionsJson: {"MinValue": 0, "MaxValue": 100, "DecimalPlaces": 2}
   │            OR: {"MinDate": "2020-01-01", "MaxDate": "2030-12-31", "Format": "DD.MM.YYYY"}
   └── Saved to PostgreSQL questions table

2. DISPLAY (Bot)
   User starts survey → Bot fetches questions
   ├── NumberQuestionHandler selected for Number type
   ├── DateQuestionHandler selected for Date type
   ├── Sends message with text input instructions
   │   └── Date: Appends "(this question must be answered in the format DD.MM.YYYY)"
   └── User types answer (text message, not callback)

3. VALIDATION (Bot + API)
   User submits text → Handler validates
   ├── Number: decimal.TryParse(), range check, decimal places
   ├── Date: DateTime.TryParseExact("dd.MM.yyyy"), range check
   └── Creates value object: NumberAnswerValue or DateAnswerValue

4. STORAGE (Database)
   Answer entity saved with polymorphic AnswerValue
   ├── answer_value_json: {"$type": "Number", "number": 42, "minValue": 0, "maxValue": 100}
   │                  OR: {"$type": "Date", "date": "2025-11-28", "minDate": "2020-01-01"}
   └── Legacy fields: AnswerText (null), AnswerJson (null)

5. RETRIEVAL (API/Bot)
   GET /api/surveys/{id}/statistics
   ├── Deserializes AnswerValue automatically via JSON discriminator
   ├── Number: Calculates min, max, average, median, std dev
   └── Date: Displays formatted dates, calculates date ranges
```

---

## Critical Files and Modifications

### Phase 1: Core Layer (6-9.5 hours)

#### File 1: `src/SurveyBot.Core/Enums/QuestionType.cs` (0.5 hours)

**Current State**:
```csharp
public enum QuestionType
{
    Text = 0,
    SingleChoice = 1,
    MultipleChoice = 2,
    Rating = 3,
    Location = 4
}
```

**Required Changes**:
```csharp
public enum QuestionType
{
    Text = 0,
    SingleChoice = 1,
    MultipleChoice = 2,
    Rating = 3,
    Location = 4,
    Number = 5,      // NEW: Numeric input with validation
    Date = 6         // NEW: Date input in DD.MM.YYYY format
}
```

**Testing**:
- ✅ Verify enum values are unique
- ✅ Verify no existing code breaks (backward compatible)

---

#### File 2: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValue.cs` (0.5 hours)

**Current State**:
```csharp
[JsonDerivedType(typeof(TextAnswerValue), typeDiscriminator: "Text")]
[JsonDerivedType(typeof(SingleChoiceAnswerValue), typeDiscriminator: "SingleChoice")]
[JsonDerivedType(typeof(MultipleChoiceAnswerValue), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(RatingAnswerValue), typeDiscriminator: "Rating")]
[JsonDerivedType(typeof(LocationAnswerValue), typeDiscriminator: "Location")]
public abstract class AnswerValue : IEquatable<AnswerValue>
{
    // ...
}
```

**Required Changes**:
```csharp
[JsonDerivedType(typeof(TextAnswerValue), typeDiscriminator: "Text")]
[JsonDerivedType(typeof(SingleChoiceAnswerValue), typeDiscriminator: "SingleChoice")]
[JsonDerivedType(typeof(MultipleChoiceAnswerValue), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(RatingAnswerValue), typeDiscriminator: "Rating")]
[JsonDerivedType(typeof(LocationAnswerValue), typeDiscriminator: "Location")]
[JsonDerivedType(typeof(NumberAnswerValue), typeDiscriminator: "Number")]    // NEW
[JsonDerivedType(typeof(DateAnswerValue), typeDiscriminator: "Date")]        // NEW
public abstract class AnswerValue : IEquatable<AnswerValue>
{
    // ...
}
```

**Testing**:
- ✅ Verify JSON serialization includes $type discriminator
- ✅ Verify deserialization routes to correct subtype

---

#### File 3: `src/SurveyBot.Core/ValueObjects/Answers/NumberAnswerValue.cs` (2-3 hours)

**CREATE NEW FILE**

**Implementation Pattern**: Follow RatingAnswerValue pattern

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a numeric answer.
/// Supports integers and decimals with optional min/max range and decimal places.
/// Immutable with value semantics.
/// </summary>
public sealed class NumberAnswerValue : AnswerValue
{
    [JsonPropertyName("number")]
    public decimal Value { get; private set; }

    [JsonPropertyName("minValue")]
    public decimal? MinValue { get; private set; }

    [JsonPropertyName("maxValue")]
    public decimal? MaxValue { get; private set; }

    [JsonPropertyName("decimalPlaces")]
    public int? DecimalPlaces { get; private set; }

    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Number;

    [JsonIgnore]
    public override string DisplayValue => DecimalPlaces.HasValue
        ? Value.ToString($"F{DecimalPlaces.Value}")
        : Value.ToString("G"); // General format (no trailing zeros)

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
    private NumberAnswerValue() : this(0, null, null, null) { }

    /// <summary>
    /// Creates a new number answer with validation.
    /// </summary>
    /// <param name="value">The numeric value</param>
    /// <param name="minValue">Optional minimum allowed value</param>
    /// <param name="maxValue">Optional maximum allowed value</param>
    /// <param name="decimalPlaces">Optional number of decimal places (null = any)</param>
    /// <returns>Validated number answer instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If value is out of range or has too many decimals</exception>
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
                $"Value {value} is below minimum {minValue.Value}");

        if (maxValue.HasValue && value > maxValue.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Number,
                $"Value {value} exceeds maximum {maxValue.Value}");

        // Validate decimal places
        if (decimalPlaces.HasValue)
        {
            if (decimalPlaces.Value < 0 || decimalPlaces.Value > 10)
                throw new ArgumentException($"DecimalPlaces must be between 0 and 10, got {decimalPlaces.Value}");

            // Check actual decimal places in value
            var actualDecimalPlaces = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
            if (actualDecimalPlaces > decimalPlaces.Value)
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.Number,
                    $"Value has too many decimal places (max {decimalPlaces.Value})");
        }

        return new NumberAnswerValue(value, minValue, maxValue, decimalPlaces);
    }

    /// <summary>
    /// Creates a number answer from a question's configuration.
    /// Extracts min/max/decimals from question's OptionsJson.
    /// </summary>
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
    /// Parses number configuration from question's OptionsJson.
    /// </summary>
    private static (decimal? minValue, decimal? maxValue, int? decimalPlaces) ParseNumberConfig(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return (null, null, null);

        try
        {
            var options = JsonSerializer.Deserialize<NumberOptions>(optionsJson);
            return (options?.MinValue, options?.MaxValue, options?.DecimalPlaces);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    /// <summary>
    /// Parses number answer from JSON storage format.
    /// </summary>
    public static NumberAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Number, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<NumberData>(json);
            if (data == null)
                throw new InvalidAnswerFormatException(0, QuestionType.Number, "Invalid JSON format");

            return new NumberAnswerValue(data.Number, data.MinValue, data.MaxValue, data.DecimalPlaces);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException($"Invalid JSON for number answer: {ex.Message}");
        }
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new NumberData
        {
            Number = Value,
            MinValue = MinValue,
            MaxValue = MaxValue,
            DecimalPlaces = DecimalPlaces
        });

    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.Number)
            return false;

        var (minValue, maxValue, _) = ParseNumberConfig(question.OptionsJson);

        if (minValue.HasValue && Value < minValue.Value)
            return false;

        if (maxValue.HasValue && Value > maxValue.Value)
            return false;

        return true;
    }

    #region Equality

    public override bool Equals(AnswerValue? other) =>
        other is NumberAnswerValue number && Value == number.Value;

    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Value);

    #endregion

    public override string ToString() => $"Number: {DisplayValue}";

    #region Internal DTOs

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

    private sealed class NumberOptions
    {
        [JsonPropertyName("MinValue")]
        public decimal? MinValue { get; set; }

        [JsonPropertyName("MaxValue")]
        public decimal? MaxValue { get; set; }

        [JsonPropertyName("DecimalPlaces")]
        public int? DecimalPlaces { get; set; }
    }

    #endregion
}
```

**Testing Requirements**:
```csharp
// NumberAnswerValueTests.cs
[Theory]
[InlineData("42", 42)]
[InlineData("3.14", 3.14)]
[InlineData("0", 0)]
[InlineData("-5.5", -5.5)]
public void Create_ValidNumber_Success(string input, decimal expected)

[Theory]
[InlineData(5, 10, null, false)]  // Below min
[InlineData(15, null, 10, false)] // Above max
[InlineData(3.14159, null, null, 2, false)] // Too many decimals
public void Create_InvalidNumber_ThrowsException(...)

[Fact]
public void ToJson_SerializesCorrectly()

[Fact]
public void FromJson_DeserializesCorrectly()

[Fact]
public void IsValidFor_WrongQuestionType_ReturnsFalse()
```

---

#### File 4: `src/SurveyBot.Core/ValueObjects/Answers/DateAnswerValue.cs` (2-3 hours)

**CREATE NEW FILE**

**Implementation Pattern**: Follow LocationAnswerValue pattern (validation logic) + format handling

```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a date answer in DD.MM.YYYY format.
/// Immutable with value semantics.
/// </summary>
public sealed class DateAnswerValue : AnswerValue
{
    /// <summary>
    /// Expected date format: DD.MM.YYYY (e.g., 28.11.2025)
    /// </summary>
    public const string DateFormat = "dd.MM.yyyy";

    [JsonPropertyName("date")]
    public DateTime Date { get; private set; }

    [JsonPropertyName("minDate")]
    public DateTime? MinDate { get; private set; }

    [JsonPropertyName("maxDate")]
    public DateTime? MaxDate { get; private set; }

    [JsonIgnore]
    public override QuestionType QuestionType => QuestionType.Date;

    [JsonIgnore]
    public override string DisplayValue => Date.ToString(DateFormat);

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private DateAnswerValue(DateTime date, DateTime? minDate, DateTime? maxDate)
    {
        Date = date.Date; // Strip time component
        MinDate = minDate?.Date;
        MaxDate = maxDate?.Date;
    }

    /// <summary>
    /// JSON constructor for deserialization.
    /// </summary>
    [JsonConstructor]
    private DateAnswerValue() : this(DateTime.MinValue, null, null) { }

    /// <summary>
    /// Creates a new date answer with validation.
    /// </summary>
    /// <param name="date">The date value</param>
    /// <param name="minDate">Optional minimum allowed date</param>
    /// <param name="maxDate">Optional maximum allowed date</param>
    /// <returns>Validated date answer instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If date is out of range</exception>
    public static DateAnswerValue Create(DateTime date, DateTime? minDate = null, DateTime? maxDate = null)
    {
        // Normalize to date only (strip time)
        date = date.Date;
        minDate = minDate?.Date;
        maxDate = maxDate?.Date;

        // Validate range
        if (minDate.HasValue && date < minDate.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Date,
                $"Date {date:dd.MM.yyyy} is before minimum date {minDate.Value:dd.MM.yyyy}");

        if (maxDate.HasValue && date > maxDate.Value)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Date,
                $"Date {date:dd.MM.yyyy} is after maximum date {maxDate.Value:dd.MM.yyyy}");

        return new DateAnswerValue(date, minDate, maxDate);
    }

    /// <summary>
    /// Parses a string in DD.MM.YYYY format to DateAnswerValue.
    /// </summary>
    /// <param name="input">Date string in DD.MM.YYYY format (e.g., "28.11.2025")</param>
    /// <param name="minDate">Optional minimum allowed date</param>
    /// <param name="maxDate">Optional maximum allowed date</param>
    /// <returns>Validated DateAnswerValue</returns>
    /// <exception cref="InvalidAnswerFormatException">If input is invalid or out of range</exception>
    public static DateAnswerValue Parse(string input, DateTime? minDate = null, DateTime? maxDate = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new InvalidAnswerFormatException(0, QuestionType.Date, "Date cannot be empty");

        // Try exact format first
        if (!DateTime.TryParseExact(
            input.Trim(),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate))
        {
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Date,
                $"'{input}' is not a valid date. Expected format: DD.MM.YYYY (e.g., 28.11.2025)");
        }

        return Create(parsedDate, minDate, maxDate);
    }

    /// <summary>
    /// Tries to parse a date string without throwing exceptions.
    /// </summary>
    public static bool TryParse(
        string input,
        out DateAnswerValue? result,
        DateTime? minDate = null,
        DateTime? maxDate = null)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (!DateTime.TryParseExact(
            input.Trim(),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate))
        {
            return false;
        }

        try
        {
            result = Create(parsedDate, minDate, maxDate);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a date answer from a question's configuration.
    /// Extracts min/max dates from question's OptionsJson.
    /// </summary>
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
    /// Parses date configuration from question's OptionsJson.
    /// </summary>
    private static (DateTime? minDate, DateTime? maxDate) ParseDateConfig(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return (null, null);

        try
        {
            var options = JsonSerializer.Deserialize<DateOptions>(optionsJson);
            return (options?.MinDate, options?.MaxDate);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Parses date answer from JSON storage format.
    /// </summary>
    public static DateAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Date, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<DateData>(json);
            if (data == null)
                throw new InvalidAnswerFormatException(0, QuestionType.Date, "Invalid JSON format");

            return new DateAnswerValue(data.Date, data.MinDate, data.MaxDate);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException($"Invalid JSON for date answer: {ex.Message}");
        }
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new DateData
        {
            Date = Date,
            MinDate = MinDate,
            MaxDate = MaxDate
        });

    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.Date)
            return false;

        var (minDate, maxDate) = ParseDateConfig(question.OptionsJson);

        if (minDate.HasValue && Date < minDate.Value)
            return false;

        if (maxDate.HasValue && Date > maxDate.Value)
            return false;

        return true;
    }

    #region Equality

    public override bool Equals(AnswerValue? other) =>
        other is DateAnswerValue date && Date.Date == date.Date.Date;

    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Date.Date);

    #endregion

    public override string ToString() => $"Date: {DisplayValue}";

    #region Internal DTOs

    private sealed class DateData
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("minDate")]
        public DateTime? MinDate { get; set; }

        [JsonPropertyName("maxDate")]
        public DateTime? MaxDate { get; set; }
    }

    private sealed class DateOptions
    {
        [JsonPropertyName("MinDate")]
        public DateTime? MinDate { get; set; }

        [JsonPropertyName("MaxDate")]
        public DateTime? MaxDate { get; set; }
    }

    #endregion
}
```

**Testing Requirements**:
```csharp
// DateAnswerValueTests.cs
[Theory]
[InlineData("28.11.2025", 2025, 11, 28)]
[InlineData("01.01.2024", 2024, 1, 1)]
[InlineData("29.02.2024", 2024, 2, 29)] // Leap year
public void Parse_ValidDate_Success(...)

[Theory]
[InlineData("2025-11-28")] // ISO format
[InlineData("28/11/2025")] // Slash separator
[InlineData("32.11.2025")] // Invalid day
[InlineData("29.02.2025")] // Not leap year
[InlineData("15.13.2025")] // Invalid month
public void Parse_InvalidDate_ThrowsException(...)

[Fact]
public void TryParse_ValidDate_ReturnsTrue()

[Fact]
public void TryParse_InvalidDate_ReturnsFalse()

[Fact]
public void Create_DateBeforeMin_ThrowsException()

[Fact]
public void Create_DateAfterMax_ThrowsException()
```

---

#### File 5: `src/SurveyBot.Core/Utilities/AnswerValueFactory.cs` (1-2 hours)

**Current State**: Has switch statements for Text, SingleChoice, MultipleChoice, Rating, Location

**Required Changes**:

```csharp
// 1. Update Parse() method
public static AnswerValue Parse(string json, QuestionType questionType)
{
    if (string.IsNullOrWhiteSpace(json))
        throw new InvalidAnswerFormatException(0, questionType, "Answer JSON cannot be empty");

    return questionType switch
    {
        QuestionType.Text => TextAnswerValue.FromJson(json),
        QuestionType.SingleChoice => SingleChoiceAnswerValue.FromJson(json),
        QuestionType.MultipleChoice => MultipleChoiceAnswerValue.FromJson(json),
        QuestionType.Rating => RatingAnswerValue.FromJson(json),
        QuestionType.Location => LocationAnswerValue.FromJson(json),
        QuestionType.Number => NumberAnswerValue.FromJson(json),        // NEW
        QuestionType.Date => DateAnswerValue.FromJson(json),            // NEW
        _ => throw new InvalidQuestionTypeException(questionType)
    };
}

// 2. Update CreateFromInput() method
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
        QuestionType.Location => throw new InvalidOperationException("Use LocationAnswerValue.Create()"),
        QuestionType.Number => CreateNumberAnswer(textAnswer, question),    // NEW
        QuestionType.Date => CreateDateAnswer(textAnswer, question),        // NEW
        _ => throw new InvalidQuestionTypeException(questionType)
    };
}

// 3. Add CreateNumberAnswer() helper
private static NumberAnswerValue CreateNumberAnswer(string? textAnswer, Question? question)
{
    if (string.IsNullOrWhiteSpace(textAnswer))
        throw new InvalidAnswerFormatException(0, QuestionType.Number, "Number answer is required");

    if (!decimal.TryParse(textAnswer.Trim(), out var number))
        throw new InvalidAnswerFormatException(0, QuestionType.Number, $"'{textAnswer}' is not a valid number");

    if (question != null)
        return NumberAnswerValue.CreateForQuestion(number, question);

    return NumberAnswerValue.Create(number);
}

// 4. Add CreateDateAnswer() helper
private static DateAnswerValue CreateDateAnswer(string? textAnswer, Question? question)
{
    if (string.IsNullOrWhiteSpace(textAnswer))
        throw new InvalidAnswerFormatException(0, QuestionType.Date, "Date answer is required");

    if (question != null)
        return DateAnswerValue.CreateForQuestion(
            DateTime.ParseExact(textAnswer.Trim(), DateAnswerValue.DateFormat, CultureInfo.InvariantCulture),
            question);

    return DateAnswerValue.Parse(textAnswer.Trim());
}

// 5. Update ParseWithTypeDetection() method
public static AnswerValue ParseWithTypeDetection(string json, Exception? innerException = null)
{
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;

    // Check for explicit type discriminator
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
            "Number" => NumberAnswerValue.FromJson(json),          // NEW
            "Date" => DateAnswerValue.FromJson(json),              // NEW
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

    if (root.TryGetProperty("number", out _))                  // NEW
        return NumberAnswerValue.FromJson(json);

    if (root.TryGetProperty("date", out _))                    // NEW
        return DateAnswerValue.FromJson(json);

    throw new InvalidAnswerFormatException("Could not determine answer type from JSON content");
}
```

**Testing Requirements**:
- ✅ Test Parse() with Number and Date types
- ✅ Test CreateFromInput() with Number and Date
- ✅ Test ParseWithTypeDetection() with Number/Date JSON
- ✅ Test all exception paths

---

### Phase 2: Bot Layer (5-6.5 hours)

#### File 6: `src/SurveyBot.Bot/Handlers/Questions/NumberQuestionHandler.cs` (2-3 hours)

**CREATE NEW FILE**

**Implementation Pattern**: Follow TextQuestionHandler (text input) + RatingQuestionHandler (validation)

```csharp
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects.Answers;
using System.Globalization;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles number-based questions that accept numeric input via text messages.
/// Supports integers and decimals with configurable validation (min/max, decimal places).
/// </summary>
public class NumberQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly ILogger<NumberQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Number;

    public NumberQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        QuestionMediaHelper mediaHelper,
        ILogger<NumberQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _mediaHelper = mediaHelper ?? throw new ArgumentNullException(nameof(mediaHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default)
    {
        // Send media first if present
        await _mediaHelper.SendQuestionMediaAsync(chatId, question, cancellationToken);

        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired
            ? "(Required)"
            : "(Optional - reply /skip to skip)";

        // Parse configuration
        var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question);

        // Build validation hint
        var validationHint = BuildValidationHint(minValue, maxValue, decimalPlaces);

        // Build navigation help
        var navigationHelp = currentIndex > 0 ? "\n\nType /back to go to previous question" : "";
        if (!question.IsRequired)
        {
            navigationHelp += "\nType /skip to skip this question";
        }

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n\n" +
                      $"{requiredText}\n\n" +
                      $"Please enter a number:{validationHint}{navigationHelp}";

        _logger.LogDebug(
            "Displaying number question {QuestionId} in chat {ChatId}",
            question.Id,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Number questions only accept message input
        if (message == null)
        {
            _logger.LogDebug("Number question requires message input");
            return null;
        }

        var chatId = message.Chat.Id;
        var text = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "Please enter a number.",
                cancellationToken);
            return null;
        }

        // Handle /skip for optional questions
        if (text.Equals("/skip", StringComparison.OrdinalIgnoreCase))
        {
            if (question.IsRequired)
            {
                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    "This question is required and cannot be skipped. Please enter a number.",
                    cancellationToken);
                return null;
            }

            _logger.LogDebug("User {UserId} skipped optional number question {QuestionId}", userId, question.Id);
            return JsonSerializer.Serialize(new { number = (decimal?)null });
        }

        // Handle /back navigation
        if (text.Equals("/back", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Let NavigationHandler handle this
        }

        // Parse number (support both comma and period as decimal separator)
        var normalizedText = text.Replace(',', '.');

        if (!decimal.TryParse(
            normalizedText,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out var number))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"Invalid number format: \"{text}\"\n\n" +
                $"Please enter a valid number.\n" +
                $"Examples: 123, 45.67, -10.5",
                cancellationToken);
            return null;
        }

        // Get validation rules from question configuration
        var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(question);

        // Create NumberAnswerValue (validates range and decimal places)
        try
        {
            var answerValue = NumberAnswerValue.Create(number, minValue, maxValue, decimalPlaces);
            var answerJson = answerValue.ToJson();

            // Final validation using the validator
            var validationResult = _validator.ValidateAnswer(answerJson, question);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Number answer validation failed for question {QuestionId}: {ErrorMessage}",
                    question.Id,
                    validationResult.ErrorMessage);

                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    validationResult.ErrorMessage!,
                    cancellationToken);
                return null;
            }

            _logger.LogDebug(
                "Number answer processed for question {QuestionId} from user {UserId}: {Value}",
                question.Id,
                userId,
                answerValue.DisplayValue);

            return answerJson;
        }
        catch (InvalidAnswerFormatException ex)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                ex.Message,
                cancellationToken);
            return null;
        }
    }

    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired;

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("number", out var numberElement))
                return false;

            if (question.IsRequired && numberElement.ValueKind == JsonValueKind.Null)
                return false;

            if (numberElement.ValueKind != JsonValueKind.Null)
            {
                var number = numberElement.GetDecimal();
                var (minValue, maxValue, _) = ParseNumberConfig(question);

                if (minValue.HasValue && number < minValue.Value)
                    return false;

                if (maxValue.HasValue && number > maxValue.Value)
                    return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for number answer");
            return false;
        }
    }

    #region Private Methods

    private (decimal? minValue, decimal? maxValue, int? decimalPlaces) ParseNumberConfig(QuestionDto question)
    {
        if (question.Options == null || !question.Options.Any())
            return (null, null, null);

        try
        {
            var optionsJson = question.Options.First();
            var options = JsonSerializer.Deserialize<NumberOptions>(optionsJson);
            return (options?.MinValue, options?.MaxValue, options?.DecimalPlaces);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private string BuildValidationHint(decimal? minValue, decimal? maxValue, int? decimalPlaces)
    {
        var hints = new List<string>();

        if (decimalPlaces.HasValue)
        {
            if (decimalPlaces.Value == 0)
                hints.Add("- Enter whole numbers only (no decimals)");
            else
                hints.Add($"- Decimals allowed (up to {decimalPlaces.Value} places)");
        }

        if (minValue.HasValue && maxValue.HasValue)
            hints.Add($"- Value must be between {minValue.Value} and {maxValue.Value}");
        else if (minValue.HasValue)
            hints.Add($"- Value must be at least {minValue.Value}");
        else if (maxValue.HasValue)
            hints.Add($"- Value must not exceed {maxValue.Value}");

        return hints.Any()
            ? "\n\n*Validation:*\n" + string.Join("\n", hints)
            : "";
    }

    private sealed class NumberOptions
    {
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? DecimalPlaces { get; set; }
    }

    #endregion
}
```

**Testing Requirements**:
- ✅ Valid number input accepted
- ✅ Invalid format rejected
- ✅ Range validation works
- ✅ Decimal places validation works
- ✅ /skip works for optional questions
- ✅ /skip rejected for required questions

---

#### File 7: `src/SurveyBot.Bot/Handlers/Questions/DateQuestionHandler.cs` (2-3 hours)

**CREATE NEW FILE**

**Implementation Pattern**: Follow TextQuestionHandler (text input) + format hint appending

```csharp
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects.Answers;
using System.Globalization;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles date-based questions with DD.MM.YYYY format validation.
/// Format hint is automatically appended to question text.
/// </summary>
public class DateQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly QuestionMediaHelper _mediaHelper;
    private readonly ILogger<DateQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.Date;

    public DateQuestionHandler(
        IBotService botService,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        QuestionMediaHelper mediaHelper,
        ILogger<DateQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _mediaHelper = mediaHelper ?? throw new ArgumentNullException(nameof(mediaHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default)
    {
        // Send media first if present
        await _mediaHelper.SendQuestionMediaAsync(chatId, question, cancellationToken);

        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired
            ? "(Required)"
            : "(Optional - reply /skip to skip)";

        // Parse configuration
        var (minDate, maxDate) = ParseDateConfig(question);

        // Build validation hint
        var validationHint = BuildValidationHint(minDate, maxDate);

        // Build navigation help
        var navigationHelp = currentIndex > 0 ? "\n\nType /back to go to previous question" : "";
        if (!question.IsRequired)
        {
            navigationHelp += "\nType /skip to skip this question";
        }

        // IMPORTANT: Automatically append format hint to question text
        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}* (this question must be answered in the format DD.MM.YYYY)\n\n" +
                      $"{requiredText}\n\n" +
                      $"💡 Example: {DateTime.Today:dd.MM.yyyy}{validationHint}{navigationHelp}";

        _logger.LogDebug(
            "Displaying date question {QuestionId} in chat {ChatId}",
            question.Id,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Date questions only accept message input
        if (message == null)
        {
            _logger.LogDebug("Date question requires message input");
            return null;
        }

        var chatId = message.Chat.Id;
        var text = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                "Please enter a date in DD.MM.YYYY format.",
                cancellationToken);
            return null;
        }

        // Handle /skip for optional questions
        if (text.Equals("/skip", StringComparison.OrdinalIgnoreCase))
        {
            if (question.IsRequired)
            {
                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    "This question is required and cannot be skipped. Please enter a date.",
                    cancellationToken);
                return null;
            }

            _logger.LogDebug("User {UserId} skipped optional date question {QuestionId}", userId, question.Id);
            return JsonSerializer.Serialize(new { date = (DateTime?)null });
        }

        // Handle /back navigation
        if (text.Equals("/back", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Let NavigationHandler handle this
        }

        // Parse date with strict format validation
        if (!DateTime.TryParseExact(
            text,
            DateAnswerValue.DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date))
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                $"Invalid date format: \"{text}\"\n\n" +
                $"Please use format DD.MM.YYYY\n" +
                $"Example: {DateTime.Today:dd.MM.yyyy}",
                cancellationToken);
            return null;
        }

        // Get validation rules from question configuration
        var (minDate, maxDate) = ParseDateConfig(question);

        // Create DateAnswerValue (validates range)
        try
        {
            var answerValue = DateAnswerValue.Create(date, minDate, maxDate);
            var answerJson = answerValue.ToJson();

            // Final validation using the validator
            var validationResult = _validator.ValidateAnswer(answerJson, question);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Date answer validation failed for question {QuestionId}: {ErrorMessage}",
                    question.Id,
                    validationResult.ErrorMessage);

                await _errorHandler.ShowValidationErrorAsync(
                    chatId,
                    validationResult.ErrorMessage!,
                    cancellationToken);
                return null;
            }

            _logger.LogDebug(
                "Date answer processed for question {QuestionId} from user {UserId}: {Value}",
                question.Id,
                userId,
                answerValue.DisplayValue);

            return answerJson;
        }
        catch (InvalidAnswerFormatException ex)
        {
            await _errorHandler.ShowValidationErrorAsync(
                chatId,
                ex.Message,
                cancellationToken);
            return null;
        }
    }

    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired;

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("date", out var dateElement))
                return false;

            if (question.IsRequired && dateElement.ValueKind == JsonValueKind.Null)
                return false;

            if (dateElement.ValueKind != JsonValueKind.Null)
            {
                var date = dateElement.GetDateTime();
                var (minDate, maxDate) = ParseDateConfig(question);

                if (minDate.HasValue && date < minDate.Value)
                    return false;

                if (maxDate.HasValue && date > maxDate.Value)
                    return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for date answer");
            return false;
        }
    }

    #region Private Methods

    private (DateTime? minDate, DateTime? maxDate) ParseDateConfig(QuestionDto question)
    {
        if (question.Options == null || !question.Options.Any())
            return (null, null);

        try
        {
            var optionsJson = question.Options.First();
            var options = JsonSerializer.Deserialize<DateOptions>(optionsJson);
            return (options?.MinDate, options?.MaxDate);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private string BuildValidationHint(DateTime? minDate, DateTime? maxDate)
    {
        var hints = new List<string>();

        if (minDate.HasValue && maxDate.HasValue)
            hints.Add($"- Date must be between {minDate.Value:dd.MM.yyyy} and {maxDate.Value:dd.MM.yyyy}");
        else if (minDate.HasValue)
            hints.Add($"- Date must be on or after {minDate.Value:dd.MM.yyyy}");
        else if (maxDate.HasValue)
            hints.Add($"- Date must be on or before {maxDate.Value:dd.MM.yyyy}");

        return hints.Any()
            ? "\n\n*Validation:*\n" + string.Join("\n", hints)
            : "";
    }

    private sealed class DateOptions
    {
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
    }

    #endregion
}
```

**Testing Requirements**:
- ✅ Valid DD.MM.YYYY format accepted
- ✅ Invalid formats rejected (ISO, slash separator, invalid dates)
- ✅ Range validation works
- ✅ Format hint displayed in question
- ✅ /skip works for optional questions
- ✅ Leap year dates handled correctly

---

#### File 8: `src/SurveyBot.Bot/Extensions/BotServiceExtensions.cs` (0.5 hours)

**Current State**: Registers 5 question handlers

**Required Changes**:

```csharp
public static IServiceCollection AddBotServices(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing registrations ...

    // Register question handlers
    services.AddScoped<IQuestionHandler, TextQuestionHandler>();
    services.AddScoped<IQuestionHandler, SingleChoiceQuestionHandler>();
    services.AddScoped<IQuestionHandler, MultipleChoiceQuestionHandler>();
    services.AddScoped<IQuestionHandler, RatingQuestionHandler>();
    services.AddScoped<IQuestionHandler, LocationQuestionHandler>();
    services.AddScoped<IQuestionHandler, NumberQuestionHandler>();      // NEW
    services.AddScoped<IQuestionHandler, DateQuestionHandler>();        // NEW

    return services;
}
```

**Testing Requirements**:
- ✅ Verify all 7 handlers registered in DI
- ✅ Verify handler resolution works for Number and Date
- ✅ Integration test: Create survey → Answer Number question → Retrieve answer
- ✅ Integration test: Create survey → Answer Date question → Retrieve answer

---

### Phase 3: Infrastructure Layer (2-3 hours)

#### File 9: `src/SurveyBot.Infrastructure/Services/ResponseService.cs` (1.5-2 hours)

**Current State**: Has ValidateAnswerAsync() switch for 5 question types

**Required Changes**:

```csharp
// Update ValidateAnswerAsync() method
private async Task<ValidationResult> ValidateAnswerAsync(Question question, SubmitAnswerDto dto)
{
    return question.QuestionType switch
    {
        QuestionType.Text => ValidateTextAnswer(dto.AnswerText, question.IsRequired),
        QuestionType.SingleChoice => ValidateSingleChoiceAnswer(dto.SelectedOptions, question.OptionsJson, question.IsRequired),
        QuestionType.MultipleChoice => ValidateMultipleChoiceAnswer(dto.SelectedOptions, question.OptionsJson, question.IsRequired),
        QuestionType.Rating => ValidateRatingAnswer(dto.RatingValue, question.IsRequired),
        QuestionType.Location => ValidateLocationAnswer(dto.AnswerJson, question.IsRequired),
        QuestionType.Number => ValidateNumberAnswer(dto.AnswerText, question.OptionsJson, question.IsRequired),        // NEW
        QuestionType.Date => ValidateDateAnswer(dto.AnswerText, question.OptionsJson, question.IsRequired),            // NEW
        _ => ValidationResult.Failure($"Unknown question type: {question.QuestionType}")
    };
}

// Add ValidateNumberAnswer() method
private ValidationResult ValidateNumberAnswer(string? answerText, string? optionsJson, bool isRequired)
{
    if (string.IsNullOrWhiteSpace(answerText))
        return isRequired
            ? ValidationResult.Failure("Number answer is required")
            : ValidationResult.Success();

    if (!decimal.TryParse(answerText.Trim(), out var number))
        return ValidationResult.Failure($"'{answerText}' is not a valid number");

    // Parse configuration
    var (minValue, maxValue, decimalPlaces) = ParseNumberConfig(optionsJson);

    if (minValue.HasValue && number < minValue.Value)
        return ValidationResult.Failure($"Number must be at least {minValue.Value}");

    if (maxValue.HasValue && number > maxValue.Value)
        return ValidationResult.Failure($"Number must not exceed {maxValue.Value}");

    if (decimalPlaces.HasValue)
    {
        var actualDecimalPlaces = BitConverter.GetBytes(decimal.GetBits(number)[3])[2];
        if (actualDecimalPlaces > decimalPlaces.Value)
            return ValidationResult.Failure($"Number must have at most {decimalPlaces.Value} decimal places");
    }

    return ValidationResult.Success();
}

// Add ValidateDateAnswer() method
private ValidationResult ValidateDateAnswer(string? answerText, string? optionsJson, bool isRequired)
{
    if (string.IsNullOrWhiteSpace(answerText))
        return isRequired
            ? ValidationResult.Failure("Date answer is required")
            : ValidationResult.Success();

    if (!DateTime.TryParseExact(
        answerText.Trim(),
        DateAnswerValue.DateFormat,
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out var date))
    {
        return ValidationResult.Failure($"'{answerText}' is not a valid date. Use format DD.MM.YYYY");
    }

    // Parse configuration
    var (minDate, maxDate) = ParseDateConfig(optionsJson);

    if (minDate.HasValue && date < minDate.Value)
        return ValidationResult.Failure($"Date must be on or after {minDate.Value:dd.MM.yyyy}");

    if (maxDate.HasValue && date > maxDate.Value)
        return ValidationResult.Failure($"Date must be on or before {maxDate.Value:dd.MM.yyyy}");

    return ValidationResult.Success();
}

// Add helper methods
private (decimal? minValue, decimal? maxValue, int? decimalPlaces) ParseNumberConfig(string? optionsJson)
{
    if (string.IsNullOrWhiteSpace(optionsJson))
        return (null, null, null);

    try
    {
        var options = JsonSerializer.Deserialize<NumberOptions>(optionsJson);
        return (options?.MinValue, options?.MaxValue, options?.DecimalPlaces);
    }
    catch (JsonException)
    {
        return (null, null, null);
    }
}

private (DateTime? minDate, DateTime? maxDate) ParseDateConfig(string? optionsJson)
{
    if (string.IsNullOrWhiteSpace(optionsJson))
        return (null, null);

    try
    {
        var options = JsonSerializer.Deserialize<DateOptions>(optionsJson);
        return (options?.MinDate, options?.MaxDate);
    }
    catch (JsonException)
    {
        return (null, null);
    }
}

private sealed class NumberOptions
{
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? DecimalPlaces { get; set; }
}

private sealed class DateOptions
{
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
}
```

**Testing Requirements**:
- ✅ Valid number passes validation
- ✅ Invalid number fails validation
- ✅ Number out of range fails validation
- ✅ Valid date passes validation
- ✅ Invalid date format fails validation
- ✅ Date out of range fails validation

---

#### File 10: `src/SurveyBot.Infrastructure/Services/SurveyService.cs` (0.5-1 hour)

**Current State**: Has GetQuestionStatistics() switch for 5 question types

**Required Changes**:

```csharp
// Update CalculateQuestionStatistics() method
private QuestionStatisticsDto CalculateQuestionStatistics(Question question, List<Answer> answers)
{
    var questionStat = new QuestionStatisticsDto
    {
        QuestionId = question.Id,
        QuestionText = question.QuestionText,
        QuestionType = question.QuestionType,
        TotalResponses = answers.Count
    };

    switch (question.QuestionType)
    {
        case QuestionType.Text:
            questionStat.TextResponses = answers.Select(a => a.AnswerText).ToList();
            break;

        case QuestionType.SingleChoice:
        case QuestionType.MultipleChoice:
            questionStat.ChoiceDistribution = CalculateChoiceDistribution(answers, question.OptionsJson);
            break;

        case QuestionType.Rating:
            questionStat.RatingStatistics = CalculateRatingStatistics(answers);
            break;

        case QuestionType.Location:
            questionStat.LocationStatistics = CalculateLocationStatistics(answers);
            break;

        case QuestionType.Number:                                          // NEW
            questionStat.NumberStatistics = CalculateNumberStatistics(answers);
            break;

        case QuestionType.Date:                                            // NEW
            questionStat.DateStatistics = CalculateDateStatistics(answers);
            break;

        default:
            _logger.LogWarning("Unknown question type {QuestionType} for statistics", question.QuestionType);
            break;
    }

    return questionStat;
}

// Add CalculateNumberStatistics() method
private NumberStatisticsDto CalculateNumberStatistics(List<Answer> answers)
{
    var numbers = answers
        .Select(a => a.Value as NumberAnswerValue)
        .Where(v => v != null)
        .Select(v => v!.Value)
        .ToList();

    if (!numbers.Any())
        return new NumberStatisticsDto { Count = 0 };

    return new NumberStatisticsDto
    {
        Minimum = numbers.Min(),
        Maximum = numbers.Max(),
        Average = numbers.Average(),
        Median = CalculateMedian(numbers),
        StandardDeviation = CalculateStandardDeviation(numbers),
        Count = numbers.Count
    };
}

// Add CalculateDateStatistics() method
private DateStatisticsDto CalculateDateStatistics(List<Answer> answers)
{
    var dates = answers
        .Select(a => a.Value as DateAnswerValue)
        .Where(v => v != null)
        .Select(v => v!.Date)
        .ToList();

    if (!dates.Any())
        return new DateStatisticsDto { Count = 0 };

    return new DateStatisticsDto
    {
        EarliestDate = dates.Min(),
        LatestDate = dates.Max(),
        DateDistribution = dates
            .GroupBy(d => d.Date)
            .Select(g => new DateFrequency
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(f => f.Date)
            .ToList(),
        Count = dates.Count
    };
}

// Helper methods
private decimal CalculateMedian(List<decimal> numbers)
{
    var sorted = numbers.OrderBy(n => n).ToList();
    int count = sorted.Count;

    if (count % 2 == 0)
        return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
    else
        return sorted[count / 2];
}

private decimal CalculateStandardDeviation(List<decimal> numbers)
{
    var avg = numbers.Average();
    var sumOfSquaresOfDifferences = numbers.Select(val => (val - avg) * (val - avg)).Sum();
    return (decimal)Math.Sqrt((double)(sumOfSquaresOfDifferences / numbers.Count));
}
```

**New DTOs Required** (add to Core/DTOs/Statistics/):

```csharp
// NumberStatisticsDto.cs
public class NumberStatisticsDto
{
    public decimal Minimum { get; set; }
    public decimal Maximum { get; set; }
    public decimal Average { get; set; }
    public decimal Median { get; set; }
    public decimal StandardDeviation { get; set; }
    public int Count { get; set; }
}

// DateStatisticsDto.cs
public class DateStatisticsDto
{
    public DateTime EarliestDate { get; set; }
    public DateTime LatestDate { get; set; }
    public List<DateFrequency> DateDistribution { get; set; } = new();
    public int Count { get; set; }
}

public class DateFrequency
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
```

---

### Phase 4: Testing (6-9 hours)

#### File 11: `tests/SurveyBot.Tests/ValueObjects/NumberAnswerValueTests.cs` (2-3 hours)

**CREATE NEW FILE**

```csharp
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.ValueObjects.Answers;
using Xunit;

namespace SurveyBot.Tests.ValueObjects;

public class NumberAnswerValueTests
{
    [Theory]
    [InlineData("42", 42)]
    [InlineData("3.14", 3.14)]
    [InlineData("0", 0)]
    [InlineData("-5.5", -5.5)]
    [InlineData("1000000", 1000000)]
    public void Create_ValidNumber_Success(string input, decimal expected)
    {
        // Arrange & Act
        var result = NumberAnswerValue.Create(expected);

        // Assert
        Assert.Equal(expected, result.Value);
        Assert.Equal(QuestionType.Number, result.QuestionType);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("12.34.56")]
    [InlineData("1,234,567")] // Commas not supported in Create (only in parsing)
    public void FromJson_InvalidFormat_ThrowsException(string invalidJson)
    {
        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.FromJson($"{{\"number\": \"{invalidJson}\"}}"));
    }

    [Fact]
    public void Create_ValueBelowMin_ThrowsException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Create(5, minValue: 10));

        Assert.Contains("below minimum", ex.Message);
    }

    [Fact]
    public void Create_ValueAboveMax_ThrowsException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Create(15, maxValue: 10));

        Assert.Contains("exceeds maximum", ex.Message);
    }

    [Fact]
    public void Create_TooManyDecimalPlaces_ThrowsException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidAnswerFormatException>(() =>
            NumberAnswerValue.Create(3.14159m, decimalPlaces: 2));

        Assert.Contains("too many decimal places", ex.Message);
    }

    [Fact]
    public void Create_WithinRange_Success()
    {
        // Arrange & Act
        var result = NumberAnswerValue.Create(50, minValue: 0, maxValue: 100);

        // Assert
        Assert.Equal(50, result.Value);
        Assert.Equal(0, result.MinValue);
        Assert.Equal(100, result.MaxValue);
    }

    [Fact]
    public void ToJson_SerializesCorrectly()
    {
        // Arrange
        var value = NumberAnswerValue.Create(42.5m, minValue: 0, maxValue: 100, decimalPlaces: 1);

        // Act
        var json = value.ToJson();

        // Assert
        Assert.Contains("\"number\":42.5", json.Replace(" ", ""));
        Assert.Contains("\"minValue\":0", json.Replace(" ", ""));
        Assert.Contains("\"maxValue\":100", json.Replace(" ", ""));
    }

    [Fact]
    public void FromJson_DeserializesCorrectly()
    {
        // Arrange
        var json = "{\"number\": 42.5, \"minValue\": 0, \"maxValue\": 100, \"decimalPlaces\": 1}";

        // Act
        var result = NumberAnswerValue.FromJson(json);

        // Assert
        Assert.Equal(42.5m, result.Value);
        Assert.Equal(0m, result.MinValue);
        Assert.Equal(100m, result.MaxValue);
        Assert.Equal(1, result.DecimalPlaces);
    }

    [Fact]
    public void DisplayValue_FormatsCorrectly()
    {
        // Arrange
        var value1 = NumberAnswerValue.Create(42.5m, decimalPlaces: 2);
        var value2 = NumberAnswerValue.Create(100m, decimalPlaces: 0);

        // Act & Assert
        Assert.Equal("42.50", value1.DisplayValue);
        Assert.Equal("100", value2.DisplayValue);
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var value1 = NumberAnswerValue.Create(42);
        var value2 = NumberAnswerValue.Create(42);

        // Act & Assert
        Assert.True(value1.Equals(value2));
        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var value1 = NumberAnswerValue.Create(42);
        var value2 = NumberAnswerValue.Create(43);

        // Act & Assert
        Assert.False(value1.Equals(value2));
    }
}
```

---

#### File 12: `tests/SurveyBot.Tests/ValueObjects/DateAnswerValueTests.cs` (2-3 hours)

**CREATE NEW FILE**

```csharp
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.ValueObjects.Answers;
using Xunit;

namespace SurveyBot.Tests.ValueObjects;

public class DateAnswerValueTests
{
    [Theory]
    [InlineData("28.11.2025", 2025, 11, 28)]
    [InlineData("01.01.2024", 2024, 1, 1)]
    [InlineData("29.02.2024", 2024, 2, 29)] // Leap year
    [InlineData("31.12.2023", 2023, 12, 31)]
    public void Parse_ValidDate_Success(string input, int year, int month, int day)
    {
        // Act
        var result = DateAnswerValue.Parse(input);

        // Assert
        Assert.Equal(new DateTime(year, month, day), result.Date);
        Assert.Equal(QuestionType.Date, result.QuestionType);
        Assert.Equal(input, result.DisplayValue);
    }

    [Theory]
    [InlineData("2025-11-28")] // ISO format
    [InlineData("28/11/2025")] // Slash separator
    [InlineData("32.11.2025")] // Invalid day
    [InlineData("29.02.2025")] // Not a leap year
    [InlineData("15.13.2025")] // Invalid month
    [InlineData("abc")]
    [InlineData("")]
    public void Parse_InvalidDate_ThrowsException(string invalidInput)
    {
        // Act & Assert
        Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Parse(invalidInput));
    }

    [Fact]
    public void Create_DateBeforeMin_ThrowsException()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1);
        var testDate = new DateTime(2024, 12, 31);

        // Act & Assert
        var ex = Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Create(testDate, minDate: minDate));

        Assert.Contains("before minimum date", ex.Message);
    }

    [Fact]
    public void Create_DateAfterMax_ThrowsException()
    {
        // Arrange
        var maxDate = new DateTime(2025, 12, 31);
        var testDate = new DateTime(2026, 1, 1);

        // Act & Assert
        var ex = Assert.Throws<InvalidAnswerFormatException>(() =>
            DateAnswerValue.Create(testDate, maxDate: maxDate));

        Assert.Contains("after maximum date", ex.Message);
    }

    [Fact]
    public void Create_WithinRange_Success()
    {
        // Arrange
        var minDate = new DateTime(2020, 1, 1);
        var maxDate = new DateTime(2030, 12, 31);
        var testDate = new DateTime(2025, 11, 28);

        // Act
        var result = DateAnswerValue.Create(testDate, minDate, maxDate);

        // Assert
        Assert.Equal(testDate.Date, result.Date);
        Assert.Equal(minDate.Date, result.MinDate);
        Assert.Equal(maxDate.Date, result.MaxDate);
    }

    [Fact]
    public void ToJson_SerializesCorrectly()
    {
        // Arrange
        var date = new DateTime(2025, 11, 28);
        var value = DateAnswerValue.Create(date);

        // Act
        var json = value.ToJson();

        // Assert
        Assert.Contains("\"date\":", json);
        Assert.Contains("2025-11-28", json); // ISO format in JSON
    }

    [Fact]
    public void FromJson_DeserializesCorrectly()
    {
        // Arrange
        var json = "{\"date\": \"2025-11-28T00:00:00\"}";

        // Act
        var result = DateAnswerValue.FromJson(json);

        // Assert
        Assert.Equal(new DateTime(2025, 11, 28), result.Date);
    }

    [Fact]
    public void TryParse_ValidDate_ReturnsTrue()
    {
        // Act
        var success = DateAnswerValue.TryParse("28.11.2025", out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2025, 11, 28), result!.Date);
    }

    [Fact]
    public void TryParse_InvalidDate_ReturnsFalse()
    {
        // Act
        var success = DateAnswerValue.TryParse("invalid", out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void DisplayValue_ReturnsCorrectFormat()
    {
        // Arrange
        var date = new DateTime(2025, 11, 28);
        var value = DateAnswerValue.Create(date);

        // Act & Assert
        Assert.Equal("28.11.2025", value.DisplayValue);
    }

    [Fact]
    public void Equals_SameDate_ReturnsTrue()
    {
        // Arrange
        var date = new DateTime(2025, 11, 28);
        var value1 = DateAnswerValue.Create(date);
        var value2 = DateAnswerValue.Create(date);

        // Act & Assert
        Assert.True(value1.Equals(value2));
        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentDate_ReturnsFalse()
    {
        // Arrange
        var value1 = DateAnswerValue.Create(new DateTime(2025, 11, 28));
        var value2 = DateAnswerValue.Create(new DateTime(2025, 11, 29));

        // Act & Assert
        Assert.False(value1.Equals(value2));
    }

    [Fact]
    public void Create_StripsTimeComponent()
    {
        // Arrange
        var dateWithTime = new DateTime(2025, 11, 28, 14, 30, 0);

        // Act
        var result = DateAnswerValue.Create(dateWithTime);

        // Assert
        Assert.Equal(new DateTime(2025, 11, 28), result.Date);
        Assert.Equal(TimeSpan.Zero, result.Date.TimeOfDay);
    }
}
```

---

#### File 13: `tests/SurveyBot.Tests/Handlers/NumberDateHandlerTests.cs` (2-3 hours)

**CREATE NEW FILE**

Integration tests for bot handlers (requires mocking Telegram Bot API)

---

### Phase 5: Documentation (3-6 hours)

#### File 14: Update Root CLAUDE.md

Add to question types section, enum values, value object list, etc.

#### File 15: Update Core CLAUDE.md

Document NumberAnswerValue and DateAnswerValue in value objects section

#### File 16: Update Bot CLAUDE.md

Document NumberQuestionHandler and DateQuestionHandler in handlers section

#### File 17: Update API CLAUDE.md (if needed)

Document any API changes

---

## Bug Prevention Checklist

Before marking implementation complete, verify ALL of these:

### Core Layer
- [ ] QuestionType enum has Number = 5, Date = 6
- [ ] AnswerValue has [JsonDerivedType] for Number and Date
- [ ] NumberAnswerValue implements all abstract methods
- [ ] DateAnswerValue implements all abstract methods
- [ ] AnswerValueFactory.Parse() has Number and Date cases
- [ ] AnswerValueFactory.CreateFromInput() has Number and Date cases
- [ ] AnswerValueFactory.ParseWithTypeDetection() has Number and Date cases

### Infrastructure Layer
- [ ] ResponseService.ValidateAnswerAsync() has Number and Date cases
- [ ] SurveyService.CalculateQuestionStatistics() has Number and Date cases
- [ ] ValidateNumberAnswer() method added
- [ ] ValidateDateAnswer() method added
- [ ] CalculateNumberStatistics() method added
- [ ] CalculateDateStatistics() method added

### Bot Layer
- [ ] NumberQuestionHandler created and implements IQuestionHandler
- [ ] DateQuestionHandler created and implements IQuestionHandler
- [ ] Both handlers registered in BotServiceExtensions.cs
- [ ] Date handler appends format hint to question text
- [ ] Number handler shows validation hint (min/max/decimals)

### Testing
- [ ] All unit tests for NumberAnswerValue pass
- [ ] All unit tests for DateAnswerValue pass
- [ ] Integration tests for handlers pass
- [ ] End-to-end test: Create Number question → Answer → Retrieve
- [ ] End-to-end test: Create Date question → Answer → Retrieve

### Compilation
- [ ] No compiler errors
- [ ] No compiler warnings
- [ ] All projects build successfully

### Runtime
- [ ] DI resolution works for all handlers
- [ ] JSON serialization/deserialization works
- [ ] Database saves/retrieves AnswerValue correctly
- [ ] Statistics calculations work

---

## Success Criteria

Implementation is complete when:

✅ Number and Date added to QuestionType enum
✅ NumberAnswerValue and DateAnswerValue value objects created
✅ AnswerValue has JSON discriminators for new types
✅ AnswerValueFactory handles Number and Date
✅ NumberQuestionHandler and DateQuestionHandler created
✅ Handlers registered in DI
✅ ResponseService validates Number and Date answers
✅ SurveyService calculates statistics for Number and Date
✅ All switch statements updated
✅ All unit tests pass
✅ Integration tests pass
✅ Documentation updated
✅ No compilation errors or warnings

---

## Risk Mitigation

### Identified Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Missing switch case | Medium | High | Exhaustive switch with default throw, compile warnings as errors |
| DI registration forgotten | Low | High | Unit test verifies all QuestionType values have handlers |
| JSON serialization error | Low | Medium | Comprehensive unit tests with malformed JSON |
| Validation bypass | Medium | High | Validate in both Bot and Infrastructure layers |
| Date format confusion | Medium | Medium | Clear format hint in UI, strict format validation |
| Decimal places bug | Low | Low | Validation in multiple layers |

---

## Timeline

### Week 1 (Core + Bot)
- **Day 1-2**: Core value objects (Files 1-5) - 6-9.5 hours
- **Day 3-4**: Bot handlers (Files 6-8) - 5-6.5 hours
- **Day 5**: Infrastructure changes (Files 9-10) - 2-3 hours

### Week 2 (Testing + Documentation)
- **Day 1-3**: Unit and integration tests (Files 11-13) - 6-9 hours
- **Day 4-5**: Documentation updates (Files 14-17) - 3-6 hours

**Total Estimated Effort**: 20-33 hours over 2 weeks

---

## Post-Implementation Verification

After implementation, perform these verification steps:

1. **Create Number Question via API**:
   ```bash
   POST /api/surveys/1/questions
   {
     "questionType": "Number",
     "questionText": "How many hours do you work per week?",
     "isRequired": true,
     "options": ["{\"MinValue\": 0, \"MaxValue\": 168, \"DecimalPlaces\": 0}"]
   }
   ```

2. **Answer via Bot**:
   - User types: `40`
   - Verify answer saved correctly

3. **Create Date Question via API**:
   ```bash
   POST /api/surveys/1/questions
   {
     "questionType": "Date",
     "questionText": "What is your date of birth?",
     "isRequired": true,
     "options": ["{\"MinDate\": \"1900-01-01\", \"MaxDate\": \"2010-12-31\"}"]
   }
   ```

4. **Answer via Bot**:
   - User sees format hint: "(this question must be answered in the format DD.MM.YYYY)"
   - User types: `28.11.2000`
   - Verify answer saved correctly

5. **Retrieve Statistics**:
   ```bash
   GET /api/surveys/1/statistics
   ```
   - Verify Number statistics include min, max, average, median
   - Verify Date statistics include earliest, latest dates

6. **Error Scenarios**:
   - Invalid number format → Error message
   - Number out of range → Error message
   - Invalid date format → Error message with example
   - Date out of range → Error message

---

## Conclusion

This implementation plan provides a comprehensive, bug-proof roadmap for adding Number and Date question types to SurveyBot. Following the existing architectural patterns ensures consistency and maintainability.

**Key Success Factors**:
- Follow existing patterns exactly (Rating for Number, Location for Date)
- Update ALL switch statements (compiler will help catch missing cases)
- Register handlers in DI
- Validate in multiple layers (defense in depth)
- Comprehensive testing before deployment

Implementation should take **20-33 hours** over **2 weeks** with proper testing and documentation.