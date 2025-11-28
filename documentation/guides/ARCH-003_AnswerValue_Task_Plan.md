# ARCH-003: Create AnswerValue Value Object (Polymorphic) - Implementation Task Plan

**Task ID**: ARCH-003
**Title**: Create AnswerValue Value Object (Polymorphic Hierarchy)
**Priority**: 🟠 HIGH
**Estimated Effort**: 6-8 hours
**Status**: 📋 Ready
**Dependencies**: ARCH-001 (Private Setters), ARCH-002 (Factory Methods)
**Phase**: Phase 1 - Value Objects & Encapsulation

---

## Task Overview

### Description
Replace primitive string-based answer storage (`AnswerText`, `AnswerJson`) with a **polymorphic value object hierarchy** that provides type-safe answer handling. This eliminates string parsing throughout the codebase and centralizes answer validation logic in dedicated value object types.

### Business Value
- **Type Safety**: Compiler prevents invalid answer types for questions
- **Validation**: Answer format validation at creation time, not at parse time
- **Maintainability**: All answer logic centralized in value objects
- **Extensibility**: Adding new question types doesn't require changing Answer entity
- **Eliminate Magic Strings**: No more JSON parsing scattered across services

### Dependencies
- **MUST Complete First**:
  - ARCH-001 (Private Setters) - Enables encapsulation of value object
  - ARCH-002 (Factory Methods) - Provides pattern for value object factories
- **Blocks**: Location Question Type implementation (will add LocationAnswerValue)
- **Enables**: ARCH-007 (JSON-based config) becomes easier with value objects

---

## Current State Analysis

### Current Anti-Pattern (Before)

**Problem 1: Untyped Storage**
```csharp
// Answer.cs - CURRENT
public class Answer
{
    public string? AnswerText { get; set; }   // For text questions only
    public string? AnswerJson { get; set; }   // For everything else (untyped!)

    // Questions:
    // - Which property to use for each question type?
    // - How to validate format?
    // - How to parse JSON safely?
}
```

**Problem 2: Scattered Parsing Logic**
```csharp
// ResponseService.cs - CURRENT (parsing scattered everywhere)
public async Task<AnswerDto> MapToAnswerDtoAsync(Answer answer)
{
    var dto = new AnswerDto { ... };

    // ❌ JSON parsing logic duplicated across multiple methods
    if (!string.IsNullOrEmpty(answer.AnswerJson))
    {
        try
        {
            var answerData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answer.AnswerJson);

            if (answerData.ContainsKey("selectedOptions"))
                dto.SelectedOptions = JsonSerializer.Deserialize<List<string>>(...);

            if (answerData.ContainsKey("ratingValue"))
                dto.RatingValue = answerData["ratingValue"].GetInt32();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse answer JSON");  // Silent failure!
        }
    }
}

// SurveyService.cs - DUPLICATE parsing logic
private string FormatAnswerForCSV(Answer? answer, Question question)
{
    if (answer == null) return "";

    try
    {
        switch (question.QuestionType)
        {
            case QuestionType.Text:
                return answer.AnswerText ?? "";

            case QuestionType.SingleChoice:
                if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                {
                    var singleChoice = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                    // ❌ DUPLICATE parsing logic!
                }
                // ...more parsing...
        }
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Failed to parse answer JSON");
        return "";  // Silent failure!
    }
}
```

**Problem 3: No Compile-Time Safety**
```csharp
// CURRENT - No type checking
var answer = new Answer();
answer.AnswerText = "Some text";
answer.AnswerJson = "{\"rating\": 4}";  // ❌ Can set both! Which one is valid?

// Saving rating as text answer - compiles but logically wrong
var textAnswer = new Answer
{
    QuestionId = ratingQuestionId,  // Rating question!
    AnswerText = "4"  // ❌ Should be JSON, not text!
};
```

### Target Pattern (After)

**Solution: Polymorphic Value Object Hierarchy**

```
                  AnswerValue (abstract)
                        |
        +---------------+---------------+---------------+
        |               |               |               |
  TextAnswerValue  SingleChoice   MultipleChoice   RatingAnswerValue
                   AnswerValue     AnswerValue
```

**Improved Answer Entity**:
```csharp
// Answer.cs - IMPROVED
public class Answer
{
    public int Id { get; private set; }
    public int ResponseId { get; private set; }
    public int QuestionId { get; private set; }

    // ✅ Single typed property (polymorphic!)
    public AnswerValue Value { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public NextQuestionDeterminant Next { get; private set; }

    // Navigation properties
    public Response Response { get; private set; } = null!;
    public Question Question { get; private set; } = null!;

    private Answer() { }

    public static Answer Create(
        int responseId,
        int questionId,
        AnswerValue value,  // ✅ Type-safe!
        NextQuestionDeterminant next)
    {
        // Value object is already validated in its factory
        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            Value = value,
            Next = next,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**Improved Service Usage**:
```csharp
// ResponseService.cs - IMPROVED (no parsing!)
public async Task<ResponseDto> SaveAnswerAsync(...)
{
    // ✅ Type-safe creation (validated in factory)
    AnswerValue answerValue = question.QuestionType switch
    {
        QuestionType.Text => TextAnswerValue.Create(answerText!),
        QuestionType.SingleChoice => SingleChoiceAnswerValue.Create(selectedOptions![0], question.Options),
        QuestionType.MultipleChoice => MultipleChoiceAnswerValue.Create(selectedOptions!, question.Options),
        QuestionType.Rating => RatingAnswerValue.Create(ratingValue!.Value),
        _ => throw new InvalidQuestionTypeException(question.QuestionType)
    };

    var answer = Answer.Create(responseId, questionId, answerValue, nextStep);
    await _answerRepository.CreateAsync(answer);
}

// NO parsing needed - value object has typed properties!
public async Task<AnswerDto> MapToAnswerDtoAsync(Answer answer)
{
    var dto = new AnswerDto { ... };

    // ✅ Type-safe access (no try-catch!)
    switch (answer.Value)
    {
        case TextAnswerValue text:
            dto.AnswerText = text.Text;
            break;

        case SingleChoiceAnswerValue single:
            dto.SelectedOptions = new List<string> { single.SelectedOption };
            break;

        case MultipleChoiceAnswerValue multiple:
            dto.SelectedOptions = multiple.SelectedOptions.ToList();
            break;

        case RatingAnswerValue rating:
            dto.RatingValue = rating.Rating;
            break;
    }

    return dto;
}
```

---

## Implementation Steps

### Step 1: Create Base AnswerValue Class (0.5 hours)

**File**: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValue.cs` (NEW)

**Directory Structure**:
```
src/SurveyBot.Core/ValueObjects/
├── NextQuestionDeterminant.cs (existing)
└── Answers/ (NEW folder)
    ├── AnswerValue.cs (abstract base)
    ├── TextAnswerValue.cs
    ├── SingleChoiceAnswerValue.cs
    ├── MultipleChoiceAnswerValue.cs
    ├── RatingAnswerValue.cs
    └── AnswerValueFactory.cs (parser)
```

**Implementation**:
```csharp
using SurveyBot.Core.Enums;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Abstract base class for all answer value objects.
/// Implements polymorphic answer storage with type-safe access.
/// </summary>
public abstract class AnswerValue : IEquatable<AnswerValue>
{
    /// <summary>
    /// Gets the question type this answer is valid for.
    /// </summary>
    public abstract QuestionType QuestionType { get; }

    /// <summary>
    /// Converts the answer value to JSON for database storage.
    /// </summary>
    /// <returns>JSON representation of the answer</returns>
    public abstract string ToJson();

    /// <summary>
    /// Validates that this answer is appropriate for the given question.
    /// </summary>
    /// <param name="question">The question being answered</param>
    /// <returns>True if answer is valid for the question</returns>
    public abstract bool IsValidFor(Question question);

    /// <summary>
    /// Gets a human-readable display value for the answer.
    /// </summary>
    public abstract string DisplayValue { get; }

    #region Equality (Value Semantics)

    public abstract bool Equals(AnswerValue? other);

    public override bool Equals(object? obj) =>
        Equals(obj as AnswerValue);

    public abstract override int GetHashCode();

    #endregion

    /// <summary>
    /// Factory method to parse JSON into appropriate AnswerValue subtype.
    /// </summary>
    public static AnswerValue FromJson(string json, QuestionType questionType) =>
        AnswerValueFactory.Parse(json, questionType);
}
```

---

### Step 2: Create TextAnswerValue (0.5 hours)

**File**: `src/SurveyBot.Core/ValueObjects/Answers/TextAnswerValue.cs` (NEW)

**Implementation**:
```csharp
using System.Text.Json;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a text answer.
/// Immutable with value semantics.
/// </summary>
public sealed class TextAnswerValue : AnswerValue
{
    private const int MaxLength = 5000;

    /// <summary>
    /// Gets the text answer content.
    /// </summary>
    public string Text { get; }

    public override QuestionType QuestionType => QuestionType.Text;

    public override string DisplayValue => Text;

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private TextAnswerValue(string text)
    {
        Text = text;
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
                0,  // Question ID unknown at this point
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
    /// Parses text answer from JSON storage format.
    /// </summary>
    /// <param name="json">JSON string from database</param>
    /// <returns>Parsed text answer</returns>
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
            throw new InvalidAnswerFormatException(0, QuestionType.Text, $"Invalid JSON: {ex.Message}", ex);
        }
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new TextAnswerData { Text = Text });

    public override bool IsValidFor(Question question) =>
        question.QuestionType == QuestionType.Text;

    #region Equality

    public override bool Equals(AnswerValue? other) =>
        other is TextAnswerValue text && Text == text.Text;

    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Text);

    #endregion

    public override string ToString() => $"Text: {Text}";

    // Internal DTO for JSON serialization
    private class TextAnswerData
    {
        public string Text { get; set; } = string.Empty;
    }
}
```

**Unit Tests** (to add):
```csharp
// tests/SurveyBot.Tests/Unit/ValueObjects/TextAnswerValueTests.cs
public class TextAnswerValueTests
{
    [Fact]
    public void Create_WithValidText_ReturnsTextAnswerValue()
    {
        var text = "This is my answer";
        var answer = TextAnswerValue.Create(text);

        Assert.Equal(text, answer.Text);
        Assert.Equal(QuestionType.Text, answer.QuestionType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidText_ThrowsException(string invalidText)
    {
        Assert.Throws<InvalidAnswerFormatException>(() =>
            TextAnswerValue.Create(invalidText));
    }

    [Fact]
    public void Create_WithTooLongText_ThrowsException()
    {
        var longText = new string('x', 5001);

        var ex = Assert.Throws<InvalidAnswerFormatException>(() =>
            TextAnswerValue.Create(longText));

        Assert.Contains("5000 characters", ex.Message);
    }

    [Fact]
    public void ToJson_ThenFromJson_ReturnsEqualValue()
    {
        var original = TextAnswerValue.Create("Test answer");
        var json = original.ToJson();
        var parsed = TextAnswerValue.FromJson(json);

        Assert.Equal(original, parsed);
    }
}
```

---

### Step 3: Create SingleChoiceAnswerValue (1 hour)

**File**: `src/SurveyBot.Core/ValueObjects/Answers/SingleChoiceAnswerValue.cs` (NEW)

**Implementation**:
```csharp
using System.Text.Json;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Value object representing a single-choice answer.
/// Immutable with value semantics.
/// </summary>
public sealed class SingleChoiceAnswerValue : AnswerValue
{
    /// <summary>
    /// Gets the selected option text.
    /// </summary>
    public string SelectedOption { get; }

    /// <summary>
    /// Gets the zero-based index of the selected option.
    /// </summary>
    public int SelectedOptionIndex { get; }

    public override QuestionType QuestionType => QuestionType.SingleChoice;

    public override string DisplayValue => SelectedOption;

    /// <summary>
    /// Private constructor - use Create() factory method.
    /// </summary>
    private SingleChoiceAnswerValue(string selectedOption, int selectedOptionIndex)
    {
        SelectedOption = selectedOption;
        SelectedOptionIndex = selectedOptionIndex;
    }

    /// <summary>
    /// Creates a new single-choice answer with validation.
    /// </summary>
    /// <param name="selectedOption">The selected option text</param>
    /// <param name="validOptions">All valid options from the question</param>
    /// <returns>Validated single-choice answer</returns>
    /// <exception cref="InvalidAnswerFormatException">If option is invalid</exception>
    public static SingleChoiceAnswerValue Create(
        string selectedOption,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (string.IsNullOrWhiteSpace(selectedOption))
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                "Selected option cannot be empty");

        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                "Question has no valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();
        var index = optionsList.FindIndex(o =>
            string.Equals(o.Text, selectedOption, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                $"Option '{selectedOption}' not found. Valid options: {string.Join(", ", optionsList.Select(o => o.Text))}");

        return new SingleChoiceAnswerValue(optionsList[index].Text, index);
    }

    /// <summary>
    /// Creates from option index (when you already know the index).
    /// </summary>
    public static SingleChoiceAnswerValue CreateByIndex(
        int optionIndex,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "No valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();

        if (optionIndex < 0 || optionIndex >= optionsList.Count)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.SingleChoice,
                $"Option index {optionIndex} out of range (0-{optionsList.Count - 1})");

        return new SingleChoiceAnswerValue(optionsList[optionIndex].Text, optionIndex);
    }

    /// <summary>
    /// Parses single-choice answer from JSON storage format.
    /// </summary>
    public static SingleChoiceAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<SingleChoiceData>(json);

            if (data == null || string.IsNullOrWhiteSpace(data.SelectedOption))
                throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, "selectedOption missing");

            // Note: When parsing from DB, we don't have access to validOptions
            // So we trust the stored data is valid (it was validated on creation)
            return new SingleChoiceAnswerValue(data.SelectedOption, data.SelectedOptionIndex ?? -1);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(0, QuestionType.SingleChoice, $"Invalid JSON: {ex.Message}", ex);
        }
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new SingleChoiceData
        {
            SelectedOption = SelectedOption,
            SelectedOptionIndex = SelectedOptionIndex
        });

    public override bool IsValidFor(Question question)
    {
        if (question.QuestionType != QuestionType.SingleChoice)
            return false;

        // Check option exists in question's options
        return question.Options.Any(o =>
            string.Equals(o.Text, SelectedOption, StringComparison.OrdinalIgnoreCase));
    }

    #region Equality

    public override bool Equals(AnswerValue? other) =>
        other is SingleChoiceAnswerValue single &&
        SelectedOption == single.SelectedOption &&
        SelectedOptionIndex == single.SelectedOptionIndex;

    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, SelectedOption, SelectedOptionIndex);

    #endregion

    public override string ToString() => $"SingleChoice: {SelectedOption} (index {SelectedOptionIndex})";

    private class SingleChoiceData
    {
        public string SelectedOption { get; set; } = string.Empty;
        public int? SelectedOptionIndex { get; set; }
    }
}
```

---

### Step 4: Create MultipleChoiceAnswerValue (1 hour)

**File**: `src/SurveyBot.Core/ValueObjects/Answers/MultipleChoiceAnswerValue.cs` (NEW)

**Implementation**: Similar pattern to SingleChoiceAnswerValue, but stores `IReadOnlyList<string>` instead of single string.

```csharp
public sealed class MultipleChoiceAnswerValue : AnswerValue
{
    /// <summary>
    /// Gets the selected options (in the order they were selected).
    /// </summary>
    public IReadOnlyList<string> SelectedOptions { get; }

    /// <summary>
    /// Gets the zero-based indices of selected options.
    /// </summary>
    public IReadOnlyList<int> SelectedOptionIndices { get; }

    public override QuestionType QuestionType => QuestionType.MultipleChoice;

    public override string DisplayValue => string.Join(", ", SelectedOptions);

    private MultipleChoiceAnswerValue(
        IReadOnlyList<string> selectedOptions,
        IReadOnlyList<int> selectedOptionIndices)
    {
        SelectedOptions = selectedOptions;
        SelectedOptionIndices = selectedOptionIndices;
    }

    /// <summary>
    /// Creates a multiple-choice answer with validation.
    /// </summary>
    /// <param name="selectedOptions">List of selected option texts</param>
    /// <param name="validOptions">All valid options from the question</param>
    /// <returns>Validated multiple-choice answer</returns>
    public static MultipleChoiceAnswerValue Create(
        IEnumerable<string> selectedOptions,
        IReadOnlyCollection<QuestionOption> validOptions)
    {
        if (selectedOptions == null)
            throw new ArgumentNullException(nameof(selectedOptions));

        var selectedList = selectedOptions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        if (selectedList.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "At least one option must be selected");

        if (validOptions == null || validOptions.Count == 0)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.MultipleChoice,
                "Question has no valid options");

        var optionsList = validOptions.OrderBy(o => o.OrderIndex).ToList();
        var indices = new List<int>();
        var validatedOptions = new List<string>();

        foreach (var selected in selectedList)
        {
            var index = optionsList.FindIndex(o =>
                string.Equals(o.Text, selected, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
                throw new InvalidAnswerFormatException(
                    0,
                    QuestionType.MultipleChoice,
                    $"Option '{selected}' not found. Valid: {string.Join(", ", optionsList.Select(o => o.Text))}");

            indices.Add(index);
            validatedOptions.Add(optionsList[index].Text);
        }

        return new MultipleChoiceAnswerValue(validatedOptions, indices);
    }

    // ToJson, FromJson, IsValidFor, Equals methods...
    // (Similar pattern to SingleChoiceAnswerValue)
}
```

---

### Step 5: Create RatingAnswerValue (0.5 hours)

**File**: `src/SurveyBot.Core/ValueObjects/Answers/RatingAnswerValue.cs` (NEW)

**Implementation**:
```csharp
public sealed class RatingAnswerValue : AnswerValue
{
    private const int MinRating = 1;
    private const int MaxRating = 5;

    /// <summary>
    /// Gets the rating value (1-5).
    /// </summary>
    public int Rating { get; }

    public override QuestionType QuestionType => QuestionType.Rating;

    public override string DisplayValue => $"{Rating}/5";

    private RatingAnswerValue(int rating)
    {
        Rating = rating;
    }

    /// <summary>
    /// Creates a new rating answer with validation.
    /// </summary>
    /// <param name="rating">Rating value (must be 1-5)</param>
    /// <returns>Validated rating answer</returns>
    public static RatingAnswerValue Create(int rating)
    {
        if (rating < MinRating || rating > MaxRating)
            throw new InvalidAnswerFormatException(
                0,
                QuestionType.Rating,
                $"Rating must be between {MinRating} and {MaxRating}, got {rating}");

        return new RatingAnswerValue(rating);
    }

    public static RatingAnswerValue FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAnswerFormatException(0, QuestionType.Rating, "JSON cannot be empty");

        try
        {
            var data = JsonSerializer.Deserialize<RatingData>(json);

            if (data == null)
                throw new InvalidAnswerFormatException(0, QuestionType.Rating, "Invalid JSON");

            return Create(data.Rating);
        }
        catch (JsonException ex)
        {
            throw new InvalidAnswerFormatException(0, QuestionType.Rating, $"Invalid JSON: {ex.Message}", ex);
        }
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new RatingData { Rating = Rating });

    public override bool IsValidFor(Question question) =>
        question.QuestionType == QuestionType.Rating;

    public override bool Equals(AnswerValue? other) =>
        other is RatingAnswerValue rating && Rating == rating.Rating;

    public override int GetHashCode() =>
        HashCode.Combine(QuestionType, Rating);

    public override string ToString() => $"Rating: {Rating}/5";

    private class RatingData
    {
        public int Rating { get; set; }
    }
}
```

---

### Step 6: Create AnswerValueFactory (0.5 hours)

**File**: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValueFactory.cs` (NEW)

**Purpose**: Parse JSON from database back into appropriate AnswerValue subtype.

**Implementation**:
```csharp
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;

namespace SurveyBot.Core.ValueObjects.Answers;

/// <summary>
/// Factory for parsing stored JSON into appropriate AnswerValue subtype.
/// </summary>
public static class AnswerValueFactory
{
    /// <summary>
    /// Parses JSON from database into the correct AnswerValue subtype.
    /// </summary>
    /// <param name="json">JSON string from Answer.AnswerJson column</param>
    /// <param name="questionType">Type of question this answer is for</param>
    /// <returns>Parsed AnswerValue instance</returns>
    /// <exception cref="InvalidAnswerFormatException">If JSON cannot be parsed</exception>
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
            _ => throw new InvalidQuestionTypeException(questionType)
        };
    }

    /// <summary>
    /// Attempts to parse JSON, returning null on failure instead of throwing.
    /// </summary>
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
    }
}
```

---

### Step 7: Update Answer Entity (1 hour)

**File**: `src/SurveyBot.Core/Entities/Answer.cs`

**Changes**:
```csharp
using SurveyBot.Core.ValueObjects;
using SurveyBot.Core.ValueObjects.Answers;

public class Answer
{
    public int Id { get; private set; }
    public int ResponseId { get; private set; }
    public int QuestionId { get; private set; }

    // ✅ NEW: Single typed property (replaces AnswerText + AnswerJson)
    public AnswerValue Value { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public NextQuestionDeterminant Next { get; private set; }

    // Navigation properties
    public Response Response { get; private set; } = null!;
    public Question Question { get; private set; } = null!;

    private Answer() { }

    /// <summary>
    /// Creates a new answer with type-safe value object.
    /// </summary>
    public static Answer Create(
        int responseId,
        int questionId,
        AnswerValue value,
        NextQuestionDeterminant next)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));

        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            Value = value,
            Next = next ?? NextQuestionDeterminant.End(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ❌ REMOVE old factory methods:
    // - CreateText
    // - CreateChoice
    // - CreateRating
}
```

---

### Step 8: Update EF Core Configuration (1.5 hours)

**File**: `src/SurveyBot.Infrastructure/Data/Configurations/AnswerConfiguration.cs`

**Challenge**: EF Core doesn't natively support polymorphic owned types, so we need to:
1. Store `AnswerValue` as JSON string in database
2. Use value converter to serialize/deserialize
3. Keep existing columns for backward compatibility (migration strategy)

**Implementation**:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;
using System.Text.Json;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers");

        builder.HasKey(a => a.Id);

        // ... existing FK configurations ...

        // NEW: Store AnswerValue as JSON
        builder.Property(a => a.Value)
            .HasColumnName("answer_value_json")
            .HasConversion(
                // To database: Serialize value object to JSON
                v => SerializeAnswerValue(v),
                // From database: Deserialize JSON to value object
                // Problem: We need QuestionType to know which subtype to create!
                // Solution: Store type discriminator in JSON OR query question separately
                json => DeserializeAnswerValue(json))
            .HasColumnType("jsonb")
            .IsRequired();

        // MIGRATION STRATEGY: Keep old columns for now (mark as deprecated)
        builder.Property<string>("_answerText")
            .HasColumnName("answer_text")
            .IsRequired(false);

        builder.Property<string>("_answerJson")
            .HasColumnName("answer_json")
            .HasColumnType("jsonb")
            .IsRequired(false);

        // Add computed column for easier querying
        builder.Property(a => a.Value)
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
    }

    private static string SerializeAnswerValue(AnswerValue value)
    {
        // Store as JSON with type discriminator
        var wrapper = new AnswerValueWrapper
        {
            Type = value.QuestionType.ToString(),
            Json = value.ToJson()
        };
        return JsonSerializer.Serialize(wrapper);
    }

    private static AnswerValue DeserializeAnswerValue(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Answer value JSON cannot be empty");

        var wrapper = JsonSerializer.Deserialize<AnswerValueWrapper>(json);
        if (wrapper == null)
            throw new InvalidOperationException("Failed to deserialize answer value wrapper");

        var questionType = Enum.Parse<QuestionType>(wrapper.Type);
        return AnswerValueFactory.Parse(wrapper.Json, questionType);
    }

    private class AnswerValueWrapper
    {
        public string Type { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
    }
}
```

**Database Migration** (Step 9):
- Add `answer_value_json` column (JSONB)
- Migrate data from `answer_text` + `answer_json` → `answer_value_json`
- Keep old columns for rollback safety (can drop later)

---

### Step 9: Create Database Migration (1 hour)

**Migration Name**: `20251127_AnswerValuePolymorphic`

**Up Migration**:
```sql
-- Add new column for polymorphic value object
ALTER TABLE answers ADD COLUMN answer_value_json JSONB;

-- Migrate existing data (Text answers)
UPDATE answers
SET answer_value_json = jsonb_build_object(
    'Type', 'Text',
    'Json', jsonb_build_object('Text', answer_text)
)
WHERE answer_text IS NOT NULL;

-- Migrate existing data (Choice/Rating answers)
UPDATE answers a
SET answer_value_json = jsonb_build_object(
    'Type', q.question_type::text,
    'Json', a.answer_json::jsonb
)
FROM questions q
WHERE a.question_id = q.id
AND a.answer_json IS NOT NULL;

-- Make new column required
ALTER TABLE answers ALTER COLUMN answer_value_json SET NOT NULL;

-- OPTIONAL: Drop old columns (or keep for rollback)
-- ALTER TABLE answers DROP COLUMN answer_text;
-- ALTER TABLE answers DROP COLUMN answer_json;

-- Add GIN index for JSON queries
CREATE INDEX idx_answers_value_json ON answers USING GIN (answer_value_json);
```

**Down Migration** (Rollback):
```sql
-- Restore old columns from new column
UPDATE answers
SET answer_text = (answer_value_json->>'Json')::jsonb->>'Text'
WHERE (answer_value_json->>'Type') = 'Text';

UPDATE answers
SET answer_json = (answer_value_json->>'Json')::jsonb
WHERE (answer_value_json->>'Type') != 'Text';

-- Drop new column
ALTER TABLE answers DROP COLUMN answer_value_json;
```

---

### Step 10: Update ResponseService (1.5 hours)

**File**: `src/SurveyBot.Infrastructure/Services/ResponseService.cs`

**Key Changes**:

1. **SaveAnswerAsync** - Use value object factories:
```csharp
public async Task<ResponseDto> SaveAnswerAsync(...)
{
    // ... validation ...

    // ✅ NEW: Create type-safe value object
    AnswerValue answerValue = question.QuestionType switch
    {
        QuestionType.Text => TextAnswerValue.Create(answerText!),

        QuestionType.SingleChoice => SingleChoiceAnswerValue.Create(
            selectedOptions![0],
            question.Options),

        QuestionType.MultipleChoice => MultipleChoiceAnswerValue.Create(
            selectedOptions!,
            question.Options),

        QuestionType.Rating => RatingAnswerValue.Create(ratingValue!.Value),

        _ => throw new InvalidQuestionTypeException(question.QuestionType)
    };

    // Determine next step
    var nextStep = await DetermineNextStepAsync(...);

    // ✅ NEW: Single factory method
    var answer = Answer.Create(responseId, questionId, answerValue, nextStep);

    await _answerRepository.CreateAsync(answer);
}
```

2. **MapToAnswerDtoAsync** - No more JSON parsing:
```csharp
private async Task<AnswerDto> MapToAnswerDtoAsync(Answer answer)
{
    var question = await _questionRepository.GetByIdAsync(answer.QuestionId);

    var dto = new AnswerDto
    {
        Id = answer.Id,
        ResponseId = answer.ResponseId,
        QuestionId = answer.QuestionId,
        QuestionText = question?.QuestionText ?? "",
        QuestionType = question?.QuestionType ?? QuestionType.Text,
        CreatedAt = answer.CreatedAt
    };

    // ✅ NEW: Type-safe pattern matching (no try-catch!)
    switch (answer.Value)
    {
        case TextAnswerValue text:
            dto.AnswerText = text.Text;
            break;

        case SingleChoiceAnswerValue single:
            dto.SelectedOptions = new List<string> { single.SelectedOption };
            break;

        case MultipleChoiceAnswerValue multiple:
            dto.SelectedOptions = multiple.SelectedOptions.ToList();
            break;

        case RatingAnswerValue rating:
            dto.RatingValue = rating.Rating;
            break;
    }

    return dto;
}
```

3. **Remove CreateAnswerJson helper** - No longer needed!

---

### Step 11: Update SurveyService CSV Export (0.5 hours)

**File**: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`

**Method**: `FormatAnswerForCSV`

**Changes**:
```csharp
private string FormatAnswerForCSV(Answer? answer, Question question)
{
    if (answer == null)
        return "";

    // ✅ NEW: Type-safe access (no try-catch!)
    return answer.Value switch
    {
        TextAnswerValue text => text.Text,

        SingleChoiceAnswerValue single => single.SelectedOption,

        MultipleChoiceAnswerValue multiple => string.Join(", ", multiple.SelectedOptions),

        RatingAnswerValue rating => rating.Rating.ToString(),

        _ => ""
    };
}
```

**Lines Removed**: ~50 lines of JSON parsing code deleted!

---

### Step 12: Run Full Test Suite (0.5 hours)

**Commands**:
```bash
dotnet clean
dotnet build
dotnet test --verbosity normal
```

**Expected Results**:
- All tests pass
- Value object tests pass
- No JSON parsing errors
- Answer creation works for all types

---

## Testing Strategy

### Unit Tests to Add

**Files to Create**:
1. `tests/SurveyBot.Tests/Unit/ValueObjects/TextAnswerValueTests.cs`
2. `tests/SurveyBot.Tests/Unit/ValueObjects/SingleChoiceAnswerValueTests.cs`
3. `tests/SurveyBot.Tests/Unit/ValueObjects/MultipleChoiceAnswerValueTests.cs`
4. `tests/SurveyBot.Tests/Unit/ValueObjects/RatingAnswerValueTests.cs`
5. `tests/SurveyBot.Tests/Unit/ValueObjects/AnswerValueFactoryTests.cs`

**Test Coverage** (per value object):
- Factory method validation
- JSON serialization/deserialization round-trip
- Equality semantics
- IsValidFor() logic
- Edge cases and error handling

---

## Risk Assessment

### HIGH Risk

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Data Migration Failure** | Data loss | Test migration on copy of production data first |
| **EF Core Value Converter Issues** | Runtime errors | Comprehensive integration tests |

### MEDIUM Risk

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Missing Answer Types** | Compilation errors | Systematic search and replace |
| **Performance Degradation** | Slower queries | Benchmark before/after, add indexes |

### Breaking Changes

**Database Schema**:
- New column: `answer_value_json` (JSONB)
- Old columns deprecated but kept for rollback
- GIN index added for performance

**Code**:
- `Answer.Create()` signature changed
- Services must use value object factories
- AutoMapper profiles need updates

---

## Acceptance Criteria

### Functionality
- [ ] All AnswerValue subtypes created
- [ ] AnswerValueFactory parses correctly
- [ ] Answer entity uses Value property
- [ ] EF Core can store/load value objects
- [ ] Data migration preserves all data
- [ ] Services use value objects (no JSON parsing)

### Testing
- [ ] Unit tests for all value objects (50+ tests)
- [ ] Integration tests for Answer CRUD
- [ ] Migration tested on sample data
- [ ] All existing tests pass

### Code Quality
- [ ] No JSON parsing in service layer
- [ ] No magic strings for answer types
- [ ] XML documentation complete
- [ ] Code coverage >80%

### Documentation
- [ ] Core CLAUDE.md updated
- [ ] Infrastructure CLAUDE.md updated
- [ ] Migration guide created
- [ ] Completion report generated

---

## Technical Notes

### Why Polymorphic Hierarchy?

**Alternatives Considered**:
1. **Single class with union properties** - Messy, no type safety
2. **Generic AnswerValue<T>** - Doesn't work well with EF Core
3. **Strategy pattern** - More complex, less intuitive
4. **Polymorphic value objects** - ✅ Clean, type-safe, extensible

### EF Core Polymorphism Challenges

EF Core doesn't natively support polymorphic owned types, so we:
1. Store as JSON with type discriminator
2. Use value converter for serialization
3. Parse back to correct subtype using factory

**Alternative**: Use TPH (Table Per Hierarchy) with discriminator column - but this makes Answer an aggregate root, which violates DDD principles.

### Performance Considerations

**Serialization Overhead**:
- JSON serialization on write: Minimal (~1ms per answer)
- JSON deserialization on read: Minimal (~1ms per answer)
- GIN index: Fast JSON queries in PostgreSQL

**Mitigation**:
- GIN index on `answer_value_json` column
- Caching in application layer if needed
- Benchmark before/after to measure impact

---

## Completion Checklist

- [ ] Step 1: Base AnswerValue class
- [ ] Step 2: TextAnswerValue
- [ ] Step 3: SingleChoiceAnswerValue
- [ ] Step 4: MultipleChoiceAnswerValue
- [ ] Step 5: RatingAnswerValue
- [ ] Step 6: AnswerValueFactory
- [ ] Step 7: Update Answer entity
- [ ] Step 8: Update EF Core config
- [ ] Step 9: Create migration
- [ ] Step 10: Update ResponseService
- [ ] Step 11: Update SurveyService CSV
- [ ] Step 12: Test suite passes
- [ ] Documentation updated
- [ ] Completion report created

---

**Task Plan Created**: 2025-11-27
**Author**: project-manager-agent
**Status**: 📋 Ready (depends on ARCH-001, ARCH-002)
**Estimated Time**: 6-8 hours
**Complexity**: HIGH (polymorphism + EF Core + migration)
