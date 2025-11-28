# Deep Analysis: Architecture Refactoring Plan vs Current Implementation

**Date**: 2025-11-27
**Status**: Comprehensive Analysis Complete
**Priority**: CRITICAL - Foundation for Future Development

---

## Executive Summary

This document provides an in-depth analysis comparing the **!PRIORITY_ARCHITECTURE_IMPROVEMENTS.md** refactoring plan with the current SurveyBot codebase. The analysis reveals:

### Critical Findings

| Category | Current State | Plan Target | Gap Analysis |
|----------|---------------|-------------|--------------|
| **Public Setters** | ALL entities have public setters on ALL properties | Private setters required | **100% gap** - Complete refactoring needed |
| **Factory Methods** | ZERO entities use factory methods | All entities need Create() | **100% gap** - No implementation started |
| **Value Objects** | 1 implemented (NextQuestionDeterminant) | Need 3+ (SurveyCode, MediaContent, AnswerValue) | **67% gap** - 2 of 3 missing |
| **Anemic Model** | Business logic in services | Logic in entities | **80% gap** - Extensive service refactoring needed |
| **Magic Values** | PARTIALLY eliminated (Answer.Next uses VO) | Complete elimination | **20% gap** - Mostly done, some edge cases |

### Architectural Debt Score

**Current DDD Maturity**: 4.5/10 (Poor to Fair)

- ✅ **Completed**: NextQuestionDeterminant value object (v1.4.2)
- ⚠️ **Partial**: Some value object usage (Answer.Next, QuestionOption.Next, Question.DefaultNext)
- ❌ **Missing**: SurveyCode value object, MediaContent value object, AnswerValue hierarchy
- ❌ **Missing**: Private setters on ALL entities
- ❌ **Missing**: Factory methods on ALL entities
- ❌ **Missing**: Rich domain model behavior in Survey/Question/Response

**Technical Debt Interest**: Estimated 40-60 hours to fix all critical issues

---

## Phase 1: Public Setters & Encapsulation Analysis

### ARCH-001: Add Private Setters to Entities (HIGH PRIORITY)

#### Current Implementation: Survey Entity

**File**: `src/SurveyBot.Core/Entities/Survey.cs`

```csharp
// CURRENT (PROBLEM):
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;  // PUBLIC SETTER ❌

    public string? Description { get; set; }            // PUBLIC SETTER ❌

    [MaxLength(10)]
    public string? Code { get; set; }                   // PUBLIC SETTER ❌

    [Required]
    public int CreatorId { get; set; }                  // PUBLIC SETTER ❌

    [Required]
    public bool IsActive { get; set; } = true;          // PUBLIC SETTER ❌

    [Required]
    public bool AllowMultipleResponses { get; set; } = false;  // PUBLIC SETTER ❌

    [Required]
    public bool ShowResults { get; set; } = true;       // PUBLIC SETTER ❌

    // Navigation properties
    public User Creator { get; set; } = null!;          // PUBLIC SETTER ❌

    public ICollection<Question> Questions { get; set; } = new List<Question>();  // PUBLIC SETTER ❌ + MUTABLE COLLECTION ❌

    public ICollection<Response> Responses { get; set; } = new List<Response>();  // PUBLIC SETTER ❌ + MUTABLE COLLECTION ❌
}
```

**Problems Identified**:
1. ✗ **All 11 properties have public setters** - Anyone can modify
2. ✗ **Collections are ICollection** - Allows external Add/Remove/Clear
3. ✗ **No validation on property changes** - Can set `Title = ""`
4. ✗ **Direct state mutation** - Can bypass business rules (`IsActive = true` without validation)

#### Required Refactoring

**File**: `src/SurveyBot.Core/Entities/Survey.cs`

```csharp
// REQUIRED (SOLUTION):
public class Survey : BaseEntity
{
    // Value properties with private setters
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Code { get; private set; }           // Should be SurveyCode value object
    public int CreatorId { get; private set; }
    public bool IsActive { get; private set; }
    public bool AllowMultipleResponses { get; private set; }
    public bool ShowResults { get; private set; }

    // Navigation (EF Core can still set via reflection)
    public User Creator { get; private set; } = null!;

    // Encapsulated collections
    private readonly List<Question> _questions = new();
    private readonly List<Response> _responses = new();

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();
    public IReadOnlyCollection<Response> Responses => _responses.AsReadOnly();

    // Private parameterless constructor for EF Core
    private Survey() { }

    // Factory method (ARCH-002)
    public static Survey Create(string title, string? description, int creatorId, string code)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        if (title.Length > 500)
            throw new SurveyValidationException("Title cannot exceed 500 characters");

        if (creatorId <= 0)
            throw new ArgumentException("Invalid creator ID", nameof(creatorId));

        return new Survey
        {
            Title = title,
            Description = description,
            CreatorId = creatorId,
            Code = code,
            IsActive = false,
            AllowMultipleResponses = false,
            ShowResults = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Rich domain model methods (ARCH-006)
    public void Activate(ISurveyValidationService validationService)
    {
        if (IsActive)
            throw new SurveyOperationException("Survey is already active");

        if (_questions.Count == 0)
            throw new SurveyValidationException("Cannot activate survey without questions");

        // Validate no cycles (delegate to domain service)
        var cycleResult = validationService.DetectCycleAsync(Id).GetAwaiter().GetResult();
        if (cycleResult.HasCycle)
            throw new SurveyCycleException(
                cycleResult.CyclePath!,
                $"Survey contains cycle: {string.Join(" → ", cycleResult.CyclePath)}");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new SurveyOperationException("Survey is already inactive");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string? description)
    {
        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        Title = title;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddQuestion(Question question)
    {
        if (_questions.Count >= SurveyConstants.MaxQuestionsPerSurvey)
            throw new SurveyValidationException(
                $"Cannot exceed {SurveyConstants.MaxQuestionsPerSurvey} questions");

        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        question.SetOrderIndex(_questions.Count);
        _questions.Add(question);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveQuestion(int questionId)
    {
        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            throw new QuestionNotFoundException(questionId);

        _questions.Remove(question);
        ReorderQuestions();
        UpdatedAt = DateTime.UtcNow;
    }

    private void ReorderQuestions()
    {
        for (int i = 0; i < _questions.Count; i++)
            _questions[i].SetOrderIndex(i);
    }
}
```

**Impact Analysis**:

1. **Service Layer Changes Required**:
   - `SurveyService.CreateSurveyAsync` must use `Survey.Create()` factory
   - `SurveyService.UpdateSurveyAsync` must use `survey.UpdateDetails()`
   - `SurveyService.ActivateSurveyAsync` must use `survey.Activate()`
   - `SurveyService.DeactivateSurveyAsync` must use `survey.Deactivate()`

2. **EF Core Compatibility**:
   - ✅ EF Core can set private properties via reflection
   - ✅ Private parameterless constructor works with proxies
   - ✅ Navigation properties still loaded correctly

3. **Testing Impact**:
   - ❌ Test fixtures can't use `new Survey { Title = "..." }` object initializers
   - ✅ Must use `Survey.Create()` factory method
   - ✅ Tests become more realistic (use public API)

4. **Migration Risk**:
   - ⚠️ **HIGH RISK** - Breaking change across 50+ files
   - ⚠️ Must update ALL service methods that create/modify surveys
   - ⚠️ Must update ALL tests that instantiate surveys

---

### ARCH-002: Add Factory Methods to Entities (HIGH PRIORITY)

#### Current Implementation: Zero Factory Methods

**Problem**: ALL entities use public constructors or object initializers

**Files Requiring Factory Methods**:
1. `Survey.cs` - Need `Survey.Create(title, description, creatorId, code)`
2. `Question.cs` - Need `Question.Create(surveyId, text, type, orderIndex)`
3. `QuestionOption.cs` - Need `QuestionOption.Create(questionId, text, orderIndex, next)`
4. `Response.cs` - Need `Response.Start(surveyId, respondentId)`
5. `Answer.cs` - Need `Answer.Create(responseId, questionId, answerValue)`
6. `User.cs` - Need `User.Create(telegramId, username, firstName, lastName)`

#### Example Implementation: Question Entity

```csharp
// CURRENT (PROBLEM):
public class Question : BaseEntity
{
    public int SurveyId { get; set; }           // PUBLIC SETTER ❌
    public string QuestionText { get; set; }    // PUBLIC SETTER ❌
    public QuestionType QuestionType { get; set; }  // PUBLIC SETTER ❌
    public int OrderIndex { get; set; }         // PUBLIC SETTER ❌
    public bool IsRequired { get; set; }        // PUBLIC SETTER ❌

    // ... other properties
}

// Used in services like this:
var question = new Question
{
    SurveyId = surveyId,
    QuestionText = "",              // INVALID but allowed! ❌
    QuestionType = (QuestionType)99,  // INVALID enum value! ❌
    OrderIndex = -5,                // INVALID but allowed! ❌
    IsRequired = true
};
```

```csharp
// REQUIRED (SOLUTION):
public class Question : BaseEntity
{
    public int SurveyId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public QuestionType QuestionType { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsRequired { get; private set; }
    public string? OptionsJson { get; private set; }
    public string? MediaContent { get; private set; }

    // Private parameterless constructor for EF Core
    private Question() { }

    // Factory method with validation
    public static Question Create(
        int surveyId,
        string questionText,
        QuestionType questionType,
        int orderIndex,
        bool isRequired = true,
        string? optionsJson = null,
        string? mediaContent = null)
    {
        // Validation
        if (surveyId <= 0)
            throw new ArgumentException("Invalid survey ID", nameof(surveyId));

        if (string.IsNullOrWhiteSpace(questionText))
            throw new QuestionValidationException("Question text cannot be empty");

        if (questionText.Length > 1000)
            throw new QuestionValidationException("Question text cannot exceed 1000 characters");

        if (orderIndex < 0)
            throw new ArgumentException("Order index must be non-negative", nameof(orderIndex));

        // Type-specific validation
        if (questionType == QuestionType.SingleChoice || questionType == QuestionType.MultipleChoice)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
                throw new QuestionValidationException("Choice questions must have options");
        }

        return new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText,
            QuestionType = questionType,
            OrderIndex = orderIndex,
            IsRequired = isRequired,
            OptionsJson = optionsJson,
            MediaContent = mediaContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Behavior methods
    public void UpdateText(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new QuestionValidationException("Question text cannot be empty");

        QuestionText = newText;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetOrderIndex(int index)
    {
        if (index < 0)
            throw new ArgumentException("Order index must be non-negative", nameof(index));

        OrderIndex = index;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Impact Analysis**:

1. **Service Layer Changes**:
   - `QuestionService.AddQuestionAsync` must use `Question.Create()`
   - All test fixtures must use factory methods

2. **Benefits**:
   - ✅ **Impossible to create invalid questions**
   - ✅ **Validation centralized** - No validation scattered in services
   - ✅ **Clear intent** - `Question.Create()` vs `new Question()`
   - ✅ **Self-documenting** - Factory method signature shows required fields

3. **Migration Effort**:
   - ⚠️ **MEDIUM RISK** - ~20-30 locations creating Questions
   - ⚠️ Must update QuestionService, SurveyService, test fixtures

---

## Phase 2: Value Objects Analysis

### ARCH-003: AnswerValue Value Object Hierarchy (HIGH PRIORITY)

#### Current Implementation: Primitive String Storage

**File**: `src/SurveyBot.Core/Entities/Answer.cs`

```csharp
// CURRENT (PROBLEM):
public class Answer
{
    public string? AnswerText { get; set; }   // For Text questions - PUBLIC SETTER ❌
    public string? AnswerJson { get; set; }   // For everything else - UNTYPED STRING ❌

    // Example values:
    // Text: AnswerText = "My answer"
    // SingleChoice: AnswerJson = "{\"selectedOption\": \"Option 2\"}"
    // MultipleChoice: AnswerJson = "{\"selectedOptions\": [\"Option 1\", \"Option 3\"]}"
    // Rating: AnswerJson = "{\"rating\": 4}"
}
```

**Problems**:
1. ✗ **Type Unsafe** - Can set `AnswerJson = "invalid json"`
2. ✗ **No Validation** - Can set `rating = 99` for 1-5 scale
3. ✗ **Parsing Scattered** - Every service parses JSON differently
4. ✗ **Inconsistent** - Text uses AnswerText, others use AnswerJson

#### Required Refactoring: Polymorphic Value Object Hierarchy

**File**: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValue.cs`

```csharp
// NEW: Abstract base class
public abstract class AnswerValue
{
    public abstract QuestionType QuestionType { get; }
    public abstract string ToJson();
    public abstract bool IsValid(QuestionDto question);
}

// NEW: Text answer
public sealed class TextAnswerValue : AnswerValue
{
    public string Text { get; }
    public override QuestionType QuestionType => QuestionType.Text;

    private TextAnswerValue(string text) => Text = text;

    public static TextAnswerValue Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidAnswerFormatException("Text cannot be empty");

        if (text.Length > 4000)
            throw new InvalidAnswerFormatException("Text too long (max 4000 chars)");

        return new TextAnswerValue(text.Trim());
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new { text = Text });

    public override bool IsValid(QuestionDto q) =>
        q.QuestionType == QuestionType.Text;
}

// NEW: Single choice answer
public sealed class SingleChoiceAnswerValue : AnswerValue
{
    public string SelectedOption { get; }
    public int SelectedOptionIndex { get; }
    public override QuestionType QuestionType => QuestionType.SingleChoice;

    private SingleChoiceAnswerValue(string option, int index)
    {
        SelectedOption = option;
        SelectedOptionIndex = index;
    }

    public static SingleChoiceAnswerValue Create(string option, IList<QuestionOptionDto> options)
    {
        var index = options.ToList().FindIndex(o => o.Text == option);
        if (index < 0)
            throw new InvalidAnswerFormatException($"Option '{option}' not found");

        return new SingleChoiceAnswerValue(option, index);
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new { selectedOption = SelectedOption });

    public override bool IsValid(QuestionDto q) =>
        q.QuestionType == QuestionType.SingleChoice &&
        q.Options.Any(o => o.Text == SelectedOption);
}

// NEW: Multiple choice answer
public sealed class MultipleChoiceAnswerValue : AnswerValue
{
    public IReadOnlyList<string> SelectedOptions { get; }
    public override QuestionType QuestionType => QuestionType.MultipleChoice;

    private MultipleChoiceAnswerValue(IReadOnlyList<string> options) =>
        SelectedOptions = options;

    public static MultipleChoiceAnswerValue Create(
        IEnumerable<string> options,
        IList<QuestionOptionDto> validOptions)
    {
        var selectedList = options.ToList();
        if (selectedList.Count == 0)
            throw new InvalidAnswerFormatException("At least one option required");

        var invalid = selectedList.Except(validOptions.Select(o => o.Text)).ToList();
        if (invalid.Any())
            throw new InvalidAnswerFormatException($"Invalid options: {string.Join(", ", invalid)}");

        return new MultipleChoiceAnswerValue(selectedList);
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new { selectedOptions = SelectedOptions });

    public override bool IsValid(QuestionDto q) =>
        q.QuestionType == QuestionType.MultipleChoice &&
        SelectedOptions.All(s => q.Options.Any(o => o.Text == s));
}

// NEW: Rating answer
public sealed class RatingAnswerValue : AnswerValue
{
    public int Rating { get; }
    public override QuestionType QuestionType => QuestionType.Rating;

    private RatingAnswerValue(int rating) => Rating = rating;

    public static RatingAnswerValue Create(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new InvalidAnswerFormatException($"Rating must be 1-5, got {rating}");

        return new RatingAnswerValue(rating);
    }

    public override string ToJson() =>
        JsonSerializer.Serialize(new { rating = Rating });

    public override bool IsValid(QuestionDto q) =>
        q.QuestionType == QuestionType.Rating && Rating >= 1 && Rating <= 5;
}
```

#### Factory for Parsing

**File**: `src/SurveyBot.Core/ValueObjects/Answers/AnswerValueFactory.cs`

```csharp
public static class AnswerValueFactory
{
    public static AnswerValue Parse(string? answerJson, QuestionType type)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            throw new InvalidAnswerFormatException("Answer JSON is empty");

        return type switch
        {
            QuestionType.Text => ParseText(answerJson),
            QuestionType.SingleChoice => ParseSingleChoice(answerJson),
            QuestionType.MultipleChoice => ParseMultipleChoice(answerJson),
            QuestionType.Rating => ParseRating(answerJson),
            _ => throw new InvalidAnswerFormatException($"Unknown question type: {type}")
        };
    }

    private static TextAnswerValue ParseText(string json)
    {
        var data = JsonSerializer.Deserialize<TextAnswerData>(json);
        return TextAnswerValue.Create(data?.Text ?? "");
    }

    private static SingleChoiceAnswerValue ParseSingleChoice(string json)
    {
        var data = JsonSerializer.Deserialize<SingleChoiceAnswerData>(json);
        return SingleChoiceAnswerValue.Create(data?.SelectedOption ?? "", data?.ValidOptions ?? new List<QuestionOptionDto>());
    }

    // ... other parse methods
}
```

**Impact Analysis**:

1. **Answer Entity Changes**:
   ```csharp
   public class Answer
   {
       // BEFORE:
       public string? AnswerText { get; set; }
       public string? AnswerJson { get; set; }

       // AFTER:
       public AnswerValue Value { get; private set; } = null!;

       // For EF Core storage (owned type or JSON column)
       private string AnswerValueJson
       {
           get => Value.ToJson();
           set => Value = AnswerValueFactory.Parse(value, Question.QuestionType);
       }
   }
   ```

2. **Service Layer Changes**:
   - `ResponseService.SaveAnswerAsync` validates using `AnswerValue.IsValid()`
   - No more manual JSON parsing in services
   - Type-safe answer handling

3. **Benefits**:
   - ✅ **Type Safety** - Can't create invalid rating (99 on 1-5 scale)
   - ✅ **Centralized Validation** - All validation in value object factories
   - ✅ **No JSON Parsing** - Services work with typed objects
   - ✅ **Consistency** - All answer types use same pattern

4. **Migration Effort**:
   - ⚠️ **HIGH RISK** - ~30-40 locations creating/parsing answers
   - ⚠️ Must update ResponseService, AnswerRepository, test fixtures
   - ⚠️ Database migration to add AnswerValueJson column

---

### ARCH-004: SurveyCode Value Object (MEDIUM PRIORITY)

#### Current Implementation: Plain String

**File**: `src/SurveyBot.Core/Entities/Survey.cs`

```csharp
// CURRENT (PROBLEM):
public class Survey : BaseEntity
{
    [MaxLength(10)]
    public string? Code { get; set; }  // Plain string - NO VALIDATION ❌
}

// Used like this:
survey.Code = "INVALID123456";  // Too long, but allowed! ❌
survey.Code = "abc";            // Too short, but allowed! ❌
survey.Code = "A!@#$%";         // Invalid chars, but allowed! ❌
```

**Current Utility**: `SurveyCodeGenerator.cs` exists but not enforced

#### Required Refactoring: SurveyCode Value Object

**File**: `src/SurveyBot.Core/ValueObjects/SurveyCode.cs`

```csharp
public sealed class SurveyCode : IEquatable<SurveyCode>
{
    public string Value { get; }

    private const string ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int Length = 6;

    private SurveyCode(string value) => Value = value;

    public static SurveyCode Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Survey code cannot be empty", nameof(code));

        if (code.Length != Length)
            throw new ArgumentException($"Survey code must be {Length} characters", nameof(code));

        var normalized = code.ToUpperInvariant();
        if (!normalized.All(c => ValidChars.Contains(c)))
            throw new ArgumentException("Survey code contains invalid characters", nameof(code));

        return new SurveyCode(normalized);
    }

    public static SurveyCode Generate()
    {
        var chars = new char[Length];
        for (int i = 0; i < Length; i++)
            chars[i] = ValidChars[RandomNumberGenerator.GetInt32(ValidChars.Length)];

        return new SurveyCode(new string(chars));
    }

    public static async Task<SurveyCode> GenerateUniqueAsync(
        Func<string, Task<bool>> existsAsync,
        int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var code = Generate();
            if (!await existsAsync(code.Value))
                return code;
        }

        throw new InvalidOperationException("Unable to generate unique survey code");
    }

    // Equality
    public bool Equals(SurveyCode? other) => other != null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as SurveyCode);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    // Implicit conversion for EF Core
    public static implicit operator string(SurveyCode code) => code.Value;
}
```

**Impact Analysis**:

1. **Entity Changes**:
   ```csharp
   public class Survey : BaseEntity
   {
       // BEFORE:
       public string? Code { get; set; }

       // AFTER:
       public SurveyCode Code { get; private set; } = null!;
   }
   ```

2. **EF Core Configuration**:
   ```csharp
   // SurveyConfiguration.cs
   builder.Property(s => s.Code)
       .HasConversion(
           code => code.Value,
           value => SurveyCode.Create(value))
       .HasColumnName("code")
       .HasMaxLength(6)
       .IsRequired();
   ```

3. **Service Changes**:
   ```csharp
   // BEFORE:
   survey.Code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
       _surveyRepository.CodeExistsAsync);

   // AFTER:
   survey.Code = await SurveyCode.GenerateUniqueAsync(
       _surveyRepository.CodeExistsAsync);
   ```

4. **Benefits**:
   - ✅ **Type Safety** - Can't assign invalid codes
   - ✅ **Centralized Logic** - Generation + validation in one place
   - ✅ **Encapsulation** - Implementation hidden from services
   - ✅ **Testability** - Easy to mock/stub

5. **Migration Effort**:
   - ⚠️ **LOW RISK** - Only ~5-10 locations use Code
   - ⚠️ Must update SurveyService.CreateSurveyAsync
   - ⚠️ Database migration to add value conversion

---

### ARCH-005: MediaContent Value Object (MEDIUM PRIORITY)

#### Current Implementation: JSON String

**File**: `src/SurveyBot.Core/Entities/Question.cs`

```csharp
// CURRENT (PROBLEM):
public class Question : BaseEntity
{
    public string? MediaContent { get; set; }  // JSON string - UNTYPED ❌

    // Example value:
    // "{\"id\":\"abc123\",\"type\":\"image\",\"filePath\":\"/media/images/abc123.jpg\",\"fileSize\":1048576,\"mimeType\":\"image/jpeg\",\"thumbnailPath\":\"/media/thumbnails/abc123_thumb.jpg\"}"
}

// Used like this:
question.MediaContent = "{invalid json}";  // No validation! ❌
question.MediaContent = "{\"fileSize\": -1000}";  // Invalid but allowed! ❌
```

#### Required Refactoring: MediaContent Value Object

**File**: `src/SurveyBot.Core/ValueObjects/MediaContent.cs`

```csharp
public sealed class MediaContent : IEquatable<MediaContent>
{
    public string Id { get; }
    public MediaType Type { get; }
    public string FilePath { get; }
    public long FileSize { get; }
    public string MimeType { get; }
    public string? ThumbnailPath { get; private set; }

    private MediaContent(string id, MediaType type, string filePath, long fileSize, string mimeType)
    {
        Id = id;
        Type = type;
        FilePath = filePath;
        FileSize = fileSize;
        MimeType = mimeType;
    }

    public static MediaContent Create(
        string id,
        MediaType type,
        string filePath,
        long fileSize,
        string mimeType)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Media ID required", nameof(id));

        if (fileSize <= 0)
            throw new ArgumentException("File size must be positive", nameof(fileSize));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path required", nameof(filePath));

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type required", nameof(mimeType));

        // Type-specific validation
        ValidateFileSize(type, fileSize);

        return new MediaContent(id, type, filePath, fileSize, mimeType);
    }

    public MediaContent WithThumbnail(string thumbnailPath)
    {
        if (string.IsNullOrWhiteSpace(thumbnailPath))
            throw new ArgumentException("Thumbnail path required", nameof(thumbnailPath));

        var copy = new MediaContent(Id, Type, FilePath, FileSize, MimeType)
        {
            ThumbnailPath = thumbnailPath
        };
        return copy;
    }

    private static void ValidateFileSize(MediaType type, long size)
    {
        var maxSize = type switch
        {
            MediaType.Image => 10 * 1024 * 1024,      // 10 MB
            MediaType.Video => 50 * 1024 * 1024,      // 50 MB
            MediaType.Audio => 20 * 1024 * 1024,      // 20 MB
            MediaType.Document => 25 * 1024 * 1024,   // 25 MB
            MediaType.Archive => 100 * 1024 * 1024,   // 100 MB
            _ => throw new ArgumentException($"Unknown media type: {type}")
        };

        if (size > maxSize)
            throw new MediaValidationException($"File size {size} exceeds max {maxSize} for {type}");
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static MediaContent? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<MediaContent>(json);
    }

    // Equality
    public bool Equals(MediaContent? other) =>
        other != null && Id == other.Id && Type == other.Type;

    public override bool Equals(object? obj) => Equals(obj as MediaContent);
    public override int GetHashCode() => HashCode.Combine(Id, Type);
}
```

**Impact Analysis**:

1. **Entity Changes**:
   ```csharp
   public class Question : BaseEntity
   {
       // BEFORE:
       public string? MediaContent { get; set; }

       // AFTER:
       public MediaContent? MediaContent { get; private set; }
   }
   ```

2. **EF Core Configuration**:
   ```csharp
   // QuestionConfiguration.cs
   builder.OwnsOne(q => q.MediaContent, mc =>
   {
       mc.Property(m => m.Id).HasColumnName("media_id");
       mc.Property(m => m.Type).HasColumnName("media_type").HasConversion<string>();
       mc.Property(m => m.FilePath).HasColumnName("media_file_path");
       mc.Property(m => m.FileSize).HasColumnName("media_file_size");
       mc.Property(m => m.MimeType).HasColumnName("media_mime_type");
       mc.Property(m => m.ThumbnailPath).HasColumnName("media_thumbnail_path");
   });
   ```

3. **Service Changes**:
   ```csharp
   // BEFORE (MediaStorageService):
   question.MediaContent = JsonSerializer.Serialize(new
   {
       id = fileId,
       type = mediaType.ToString().ToLower(),
       filePath = savedPath,
       fileSize = file.Length,
       mimeType = file.ContentType,
       thumbnailPath = thumbnailPath
   });

   // AFTER:
   var mediaContent = MediaContent.Create(
       fileId,
       mediaType,
       savedPath,
       file.Length,
       file.ContentType);

   if (thumbnailPath != null)
       mediaContent = mediaContent.WithThumbnail(thumbnailPath);

   question.UpdateMediaContent(mediaContent);
   ```

4. **Benefits**:
   - ✅ **Type Safety** - Can't set negative file size
   - ✅ **Validation** - File size limits enforced
   - ✅ **Immutability** - WithThumbnail returns new instance
   - ✅ **No JSON Parsing** - Services work with typed objects

5. **Migration Effort**:
   - ⚠️ **MEDIUM RISK** - ~10-15 locations use MediaContent
   - ⚠️ Must update MediaStorageService, QuestionService
   - ⚠️ Database migration to split JSON column into owned type columns

---

## Phase 3: Rich Domain Model Analysis

### ARCH-006: Survey Aggregate with Behavior Methods (MEDIUM PRIORITY)

#### Current Implementation: Anemic Model

**File**: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`

```csharp
// CURRENT (PROBLEM): Business logic in service
public class SurveyService : ISurveyService
{
    public async Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId)
    {
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);

        if (survey == null)
            throw new SurveyNotFoundException(surveyId);

        if (survey.CreatorId != userId)
            throw new UnauthorizedAccessException(userId, "Survey", surveyId);

        // BUSINESS LOGIC IN SERVICE ❌
        if (survey.Questions == null || survey.Questions.Count == 0)
            throw new SurveyValidationException(
                "Cannot activate a survey with no questions...");

        var cycleResult = await _validationService.DetectCycleAsync(surveyId);
        if (cycleResult.HasCycle)
            throw new SurveyCycleException(
                cycleResult.CyclePath!,
                $"Cannot activate survey: {cycleResult.ErrorMessage}");

        var endpoints = await _validationService.FindSurveyEndpointsAsync(surveyId);
        if (!endpoints.Any())
            throw new InvalidOperationException(
                "Cannot activate survey: No questions lead to survey completion...");

        // DIRECT STATE MODIFICATION ❌
        survey.IsActive = true;
        survey.UpdatedAt = DateTime.UtcNow;

        await _surveyRepository.UpdateAsync(survey);

        // ... mapping and return
    }
}
```

**Problems**:
1. ✗ **Business Logic in Service** - Activation rules scattered across services
2. ✗ **Direct State Mutation** - `survey.IsActive = true` bypasses encapsulation
3. ✗ **Validation Repeated** - Same checks in multiple service methods
4. ✗ **Hard to Test** - Must mock repositories to test business rules

#### Required Refactoring: Rich Domain Model

**File**: `src/SurveyBot.Core/Entities/Survey.cs`

```csharp
// REQUIRED (SOLUTION): Business logic in entity
public class Survey : BaseEntity
{
    // Private setters (ARCH-001)
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private readonly List<Question> _questions = new();
    private readonly List<Response> _responses = new();

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();
    public IReadOnlyCollection<Response> Responses => _responses.AsReadOnly();

    // Rich behavior: Activate with validation
    public void Activate(ISurveyValidationService validationService)
    {
        if (IsActive)
            throw new SurveyOperationException("Survey is already active");

        if (_questions.Count == 0)
            throw new SurveyValidationException("Cannot activate survey without questions");

        // Validate no cycles (delegate to domain service)
        var cycleResult = validationService.DetectCycleAsync(Id).GetAwaiter().GetResult();
        if (cycleResult.HasCycle)
            throw new SurveyCycleException(
                cycleResult.CyclePath!,
                $"Survey contains cycle: {string.Join(" → ", cycleResult.CyclePath)}");

        // Validate has endpoints
        var endpoints = validationService.FindSurveyEndpointsAsync(Id).GetAwaiter().GetResult();
        if (!endpoints.Any())
            throw new SurveyValidationException(
                "Cannot activate survey: No questions lead to completion");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // Rich behavior: Deactivate
    public void Deactivate()
    {
        if (!IsActive)
            throw new SurveyOperationException("Survey is already inactive");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    // Rich behavior: Update details with validation
    public void UpdateDetails(string title, string? description)
    {
        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        if (title.Length > 500)
            throw new SurveyValidationException("Title cannot exceed 500 characters");

        Title = title;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    // Rich behavior: Add question with validation
    public void AddQuestion(Question question)
    {
        if (_questions.Count >= SurveyConstants.MaxQuestionsPerSurvey)
            throw new SurveyValidationException(
                $"Cannot exceed {SurveyConstants.MaxQuestionsPerSurvey} questions");

        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        question.SetOrderIndex(_questions.Count);
        _questions.Add(question);
        UpdatedAt = DateTime.UtcNow;
    }

    // Rich behavior: Remove question
    public void RemoveQuestion(int questionId)
    {
        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            throw new QuestionNotFoundException(questionId);

        _questions.Remove(question);
        ReorderQuestions();
        UpdatedAt = DateTime.UtcNow;
    }

    // Rich behavior: Reorder questions
    public void ReorderQuestions(IList<int> questionIds)
    {
        if (IsActive && _responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");

        if (questionIds.Count != _questions.Count)
            throw new ArgumentException("Must provide all question IDs");

        var orderedQuestions = questionIds
            .Select(id => _questions.FirstOrDefault(q => q.Id == id))
            .Where(q => q != null)
            .ToList();

        if (orderedQuestions.Count != _questions.Count)
            throw new ArgumentException("Invalid question IDs");

        _questions.Clear();
        _questions.AddRange(orderedQuestions!);

        for (int i = 0; i < _questions.Count; i++)
            _questions[i].SetOrderIndex(i);

        UpdatedAt = DateTime.UtcNow;
    }

    private void ReorderQuestions()
    {
        for (int i = 0; i < _questions.Count; i++)
            _questions[i].SetOrderIndex(i);
    }
}
```

**Service becomes thin orchestrator**:

**File**: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`

```csharp
// AFTER (THIN SERVICE):
public class SurveyService : ISurveyService
{
    public async Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId)
    {
        var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);

        if (survey == null)
            throw new SurveyNotFoundException(surveyId);

        if (survey.CreatorId != userId)
            throw new UnauthorizedAccessException(userId, "Survey", surveyId);

        // BUSINESS LOGIC IN ENTITY ✅
        survey.Activate(_validationService);

        await _surveyRepository.UpdateAsync(survey);

        return _mapper.Map<SurveyDto>(survey);
    }
}
```

**Impact Analysis**:

1. **Benefits**:
   - ✅ **Business Logic Centralized** - All activation logic in one place
   - ✅ **Easier Testing** - Test `survey.Activate()` without mocking DB
   - ✅ **Encapsulation** - Can't bypass validation by setting `IsActive = true`
   - ✅ **Self-Documenting** - Method names express intent

2. **Service Changes Required**:
   - `SurveyService.ActivateSurveyAsync` → call `survey.Activate()`
   - `SurveyService.DeactivateSurveyAsync` → call `survey.Deactivate()`
   - `SurveyService.UpdateSurveyAsync` → call `survey.UpdateDetails()`
   - `QuestionService.AddQuestionAsync` → call `survey.AddQuestion()`
   - `QuestionService.RemoveQuestionAsync` → call `survey.RemoveQuestion()`

3. **Migration Effort**:
   - ⚠️ **MEDIUM RISK** - ~15-20 service methods affected
   - ⚠️ Must refactor SurveyService, QuestionService
   - ⚠️ Must update all tests

---

## Consolidated Refactoring Task List

### Dependency Chain Analysis

```
Phase 1A: Private Setters (Foundation)
├── ARCH-001.1: Survey.cs private setters
├── ARCH-001.2: Question.cs private setters
├── ARCH-001.3: QuestionOption.cs private setters
├── ARCH-001.4: Response.cs private setters
├── ARCH-001.5: Answer.cs private setters
└── ARCH-001.6: User.cs private setters

Phase 1B: Factory Methods (Depends on 1A)
├── ARCH-002.1: Survey.Create() factory
├── ARCH-002.2: Question.Create() factory
├── ARCH-002.3: QuestionOption.Create() factory
├── ARCH-002.4: Response.Start() factory
├── ARCH-002.5: Answer.Create() factory
└── ARCH-002.6: User.Create() factory

Phase 2A: Core Value Objects (Independent)
├── ARCH-004: SurveyCode value object
├── ARCH-005: MediaContent value object
└── ARCH-003: AnswerValue hierarchy

Phase 2B: Service Integration (Depends on 1B + 2A)
├── ARCH-INT-001: Update SurveyService to use Survey.Create() and factories
├── ARCH-INT-002: Update QuestionService to use Question.Create() and factories
├── ARCH-INT-003: Update ResponseService to use AnswerValue hierarchy
└── ARCH-INT-004: Update MediaStorageService to use MediaContent VO

Phase 3: Rich Domain Model (Depends on 1A, 1B)
├── ARCH-006.1: Survey aggregate with behavior
├── ARCH-006.2: Question aggregate with behavior
└── ARCH-006.3: Update services to thin orchestrators
```

### Detailed Task Breakdown

#### Phase 1A: Private Setters (4-6 hours total)

| Task ID | Entity | Effort | Files Affected | Risk |
|---------|--------|--------|----------------|------|
| **ARCH-001.1** | Survey | 1h | Survey.cs, SurveyService.cs, SurveyConfiguration.cs, 10+ tests | HIGH |
| **ARCH-001.2** | Question | 1h | Question.cs, QuestionService.cs, QuestionConfiguration.cs, 8+ tests | HIGH |
| **ARCH-001.3** | QuestionOption | 30min | QuestionOption.cs, QuestionOptionConfiguration.cs, 3+ tests | MEDIUM |
| **ARCH-001.4** | Response | 1h | Response.cs, ResponseService.cs, ResponseConfiguration.cs, 5+ tests | MEDIUM |
| **ARCH-001.5** | Answer | 1h | Answer.cs, ResponseService.cs, AnswerConfiguration.cs, 5+ tests | MEDIUM |
| **ARCH-001.6** | User | 30min | User.cs, UserService.cs, UserConfiguration.cs, 2+ tests | LOW |

**Total**: ~5-6 hours, **50+ files affected**

#### Phase 1B: Factory Methods (4-6 hours total)

| Task ID | Entity | Effort | Dependencies | Risk |
|---------|--------|--------|--------------|------|
| **ARCH-002.1** | Survey.Create() | 1h | ARCH-001.1, SurveyCode VO | HIGH |
| **ARCH-002.2** | Question.Create() | 1.5h | ARCH-001.2, MediaContent VO | HIGH |
| **ARCH-002.3** | QuestionOption.Create() | 30min | ARCH-001.3 | MEDIUM |
| **ARCH-002.4** | Response.Start() | 1h | ARCH-001.4 | MEDIUM |
| **ARCH-002.5** | Answer.Create() | 1h | ARCH-001.5, AnswerValue VO | HIGH |
| **ARCH-002.6** | User.Create() | 30min | ARCH-001.6 | LOW |

**Total**: ~5-6 hours, **30+ files affected**

#### Phase 2A: Value Objects (14-20 hours total)

| Task ID | Value Object | Effort | Files Created | Risk |
|---------|--------------|--------|---------------|------|
| **ARCH-004** | SurveyCode | 2-3h | SurveyCode.cs, tests | LOW |
| **ARCH-005** | MediaContent | 4-6h | MediaContent.cs, MediaType enum, tests, migration | MEDIUM |
| **ARCH-003** | AnswerValue hierarchy | 8-12h | AnswerValue.cs + 4 subclasses, factory, tests | HIGH |

**Total**: ~14-20 hours, **10+ new files**

#### Phase 2B: Service Integration (8-12 hours total)

| Task ID | Service | Effort | Dependencies | Risk |
|---------|---------|--------|--------------|------|
| **ARCH-INT-001** | SurveyService | 3-4h | ARCH-002.1, ARCH-004 | HIGH |
| **ARCH-INT-002** | QuestionService | 3-4h | ARCH-002.2, ARCH-005 | HIGH |
| **ARCH-INT-003** | ResponseService | 4-6h | ARCH-002.5, ARCH-003 | HIGH |
| **ARCH-INT-004** | MediaStorageService | 2-3h | ARCH-005 | MEDIUM |

**Total**: ~12-17 hours, **20+ service methods refactored**

#### Phase 3: Rich Domain Model (8-12 hours total)

| Task ID | Component | Effort | Dependencies | Risk |
|---------|-----------|--------|--------------|------|
| **ARCH-006.1** | Survey aggregate | 4-6h | ARCH-001.1, ARCH-002.1 | HIGH |
| **ARCH-006.2** | Question aggregate | 2-3h | ARCH-001.2, ARCH-002.2 | MEDIUM |
| **ARCH-006.3** | Thin services | 4-6h | ARCH-006.1, ARCH-006.2 | HIGH |

**Total**: ~10-15 hours, **15+ service methods refactored**

---

## Additional Outdated Architecture Patterns Identified

### Issue 1: Direct Collection Modification (All Entities)

**Problem**: All navigation properties are `ICollection<T>`, allowing external modification

**Current Code**:
```csharp
survey.Questions.Add(new Question());  // Bypasses validation! ❌
survey.Questions.Clear();              // Deletes all questions! ❌
survey.Responses.RemoveAt(0);          // No authorization check! ❌
```

**Required Fix**: Use private backing fields with read-only wrappers

```csharp
private readonly List<Question> _questions = new();
public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();
```

**Affected Entities**: Survey (Questions, Responses), Question (Options, Answers), Response (Answers), User (Surveys)

**Effort**: 2-3 hours (included in ARCH-001)

### Issue 2: Scattered Authorization Logic (Services)

**Problem**: Authorization checks repeated in every service method

**Current Pattern**:
```csharp
// In SurveyService.UpdateSurveyAsync:
if (survey.CreatorId != userId)
    throw new UnauthorizedAccessException(userId, "Survey", surveyId);

// In SurveyService.DeleteSurveyAsync:
if (survey.CreatorId != userId)
    throw new UnauthorizedAccessException(userId, "Survey", surveyId);

// Repeated 12+ times across services ❌
```

**Required Fix**: Authorization in entity or domain service

```csharp
// Option 1: Extension method
public static class SurveyExtensions
{
    public static void EnsureOwnership(this Survey survey, int userId)
    {
        if (survey.CreatorId != userId)
            throw new UnauthorizedAccessException(userId, "Survey", survey.Id);
    }
}

// Option 2: Domain service
public class SurveyAuthorizationService
{
    public void EnsureCanModify(Survey survey, int userId)
    {
        if (survey.CreatorId != userId)
            throw new UnauthorizedAccessException(userId, "Survey", survey.Id);

        if (survey.IsActive && survey.Responses.Any())
            throw new SurveyOperationException("Cannot modify active survey with responses");
    }
}
```

**Affected Services**: SurveyService (8 methods), QuestionService (4 methods)

**Effort**: 3-4 hours

**Task ID**: **ARCH-NEW-001** (Authorization Consolidation)

### Issue 3: Magic Strings in OptionsJson (Legacy Pattern)

**Problem**: Question.OptionsJson is still a plain string, not validated

**Current Code**:
```csharp
question.OptionsJson = "[\"Option 1\", \"Option 2\"]";  // No validation ❌
question.OptionsJson = "{invalid json}";                // Accepted! ❌
question.OptionsJson = "[]";                            // Empty array! ❌
```

**Required Fix**: OptionsJson value object or migrate to QuestionOption entities

```csharp
// Option 1: Value object
public sealed class QuestionOptions : IEquatable<QuestionOptions>
{
    public IReadOnlyList<string> Options { get; }

    private QuestionOptions(IReadOnlyList<string> options) => Options = options;

    public static QuestionOptions Create(IEnumerable<string> options)
    {
        var list = options.ToList();

        if (list.Count < 2)
            throw new ArgumentException("At least 2 options required");

        if (list.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Options cannot be empty");

        if (list.Count > SurveyConstants.MaxOptionsPerQuestion)
            throw new ArgumentException($"Cannot exceed {SurveyConstants.MaxOptionsPerQuestion} options");

        return new QuestionOptions(list);
    }

    public string ToJson() => JsonSerializer.Serialize(Options);
    public static QuestionOptions FromJson(string json) =>
        Create(JsonSerializer.Deserialize<List<string>>(json) ?? new());
}

// Option 2: Migrate to QuestionOption entities (already exists!)
// Deprecate OptionsJson, use question.Options collection instead
```

**Affected Code**: QuestionService, ResponseService, Bot question handlers

**Effort**: 4-6 hours

**Task ID**: **ARCH-NEW-002** (OptionsJson Migration or Value Object)

### Issue 4: BaseEntity Public Setters (Infrastructure Concern)

**Problem**: BaseEntity has public setters for CreatedAt/UpdatedAt

**Current Code**:
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }                 // PUBLIC SETTER ❌
    public DateTime CreatedAt { get; set; }     // PUBLIC SETTER ❌
    public DateTime UpdatedAt { get; set; }     // PUBLIC SETTER ❌
}

// Anyone can do this:
survey.CreatedAt = DateTime.MinValue;  // Corrupt timestamp! ❌
```

**Required Fix**: Private setters with initialization logic

```csharp
public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected void MarkAsModified()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Affected Entities**: All entities inheriting BaseEntity (Survey, Question, QuestionOption, User)

**Effort**: 1-2 hours

**Task ID**: **ARCH-NEW-003** (BaseEntity Encapsulation)

### Issue 5: Validation Scattered Across Layers (Anti-Pattern)

**Problem**: Same validation rules repeated in DTOs, Services, and Entities

**Example - Survey Title Validation**:

```csharp
// In CreateSurveyDto:
[Required]
[MinLength(3)]
[MaxLength(500)]
public string Title { get; set; }

// In SurveyService.ValidateCreateSurveyDto:
if (string.IsNullOrWhiteSpace(dto.Title))
    throw new SurveyValidationException("Survey title is required.");

if (dto.Title.Length < 3)
    throw new SurveyValidationException("Survey title must be at least 3 characters...");

if (dto.Title.Length > 500)
    throw new SurveyValidationException("Survey title cannot exceed 500 characters.");

// In Survey.UpdateDetails:
if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
    throw new SurveyValidationException("Title must be at least 3 characters");
```

**Triple Redundancy! ❌**

**Required Fix**: Validation only in entity/value object

```csharp
// DTOs have basic [Required] only (for model binding)
[Required]
public string Title { get; set; }

// Validation in entity factory/method
public static Survey Create(string title, ...)
{
    ValidateTitle(title);
    // ...
}

public void UpdateDetails(string title, ...)
{
    ValidateTitle(title);
    // ...
}

private static void ValidateTitle(string title)
{
    if (string.IsNullOrWhiteSpace(title))
        throw new SurveyValidationException("Title is required");

    if (title.Length < 3)
        throw new SurveyValidationException("Title must be at least 3 characters");

    if (title.Length > 500)
        throw new SurveyValidationException("Title cannot exceed 500 characters");
}
```

**Affected Code**: All service ValidateXxxDto methods, entity Create/Update methods

**Effort**: 3-4 hours

**Task ID**: **ARCH-NEW-004** (Centralize Domain Validation)

---

## Expanded Task List with New Tasks

| Task ID | Priority | Description | Effort | Dependencies |
|---------|----------|-------------|--------|--------------|
| **ARCH-001.1** | 🔴 CRITICAL | Survey: Private setters | 1h | None |
| **ARCH-001.2** | 🔴 CRITICAL | Question: Private setters | 1h | None |
| **ARCH-001.3** | 🔴 CRITICAL | QuestionOption: Private setters | 30min | None |
| **ARCH-001.4** | 🔴 CRITICAL | Response: Private setters | 1h | None |
| **ARCH-001.5** | 🔴 CRITICAL | Answer: Private setters | 1h | None |
| **ARCH-001.6** | 🔴 CRITICAL | User: Private setters | 30min | None |
| **ARCH-NEW-003** | 🔴 CRITICAL | BaseEntity: Private setters | 1-2h | None |
| **ARCH-002.1** | 🟠 HIGH | Survey.Create() factory | 1h | ARCH-001.1 |
| **ARCH-002.2** | 🟠 HIGH | Question.Create() factory | 1.5h | ARCH-001.2 |
| **ARCH-002.3** | 🟠 HIGH | QuestionOption.Create() factory | 30min | ARCH-001.3 |
| **ARCH-002.4** | 🟠 HIGH | Response.Start() factory | 1h | ARCH-001.4 |
| **ARCH-002.5** | 🟠 HIGH | Answer.Create() factory | 1h | ARCH-001.5 |
| **ARCH-002.6** | 🟠 HIGH | User.Create() factory | 30min | ARCH-001.6 |
| **ARCH-003** | 🟠 HIGH | AnswerValue hierarchy | 8-12h | ARCH-001.5 |
| **ARCH-004** | 🟡 MEDIUM | SurveyCode value object | 2-3h | None |
| **ARCH-005** | 🟡 MEDIUM | MediaContent value object | 4-6h | None |
| **ARCH-NEW-002** | 🟡 MEDIUM | OptionsJson value object/migration | 4-6h | ARCH-001.2 |
| **ARCH-006.1** | 🟡 MEDIUM | Survey rich domain model | 4-6h | ARCH-001.1, ARCH-002.1 |
| **ARCH-006.2** | 🟡 MEDIUM | Question rich domain model | 2-3h | ARCH-001.2, ARCH-002.2 |
| **ARCH-NEW-001** | 🟡 MEDIUM | Authorization consolidation | 3-4h | ARCH-006.1 |
| **ARCH-NEW-004** | 🟡 MEDIUM | Centralize domain validation | 3-4h | ARCH-002.1, ARCH-002.2 |
| **ARCH-INT-001** | 🟢 LOW | Update SurveyService | 3-4h | ARCH-002.1, ARCH-004, ARCH-006.1 |
| **ARCH-INT-002** | 🟢 LOW | Update QuestionService | 3-4h | ARCH-002.2, ARCH-005, ARCH-006.2 |
| **ARCH-INT-003** | 🟢 LOW | Update ResponseService | 4-6h | ARCH-002.5, ARCH-003 |
| **ARCH-INT-004** | 🟢 LOW | Update MediaStorageService | 2-3h | ARCH-005 |
| **ARCH-006.3** | 🟢 LOW | Thin service orchestrators | 4-6h | ARCH-006.1, ARCH-006.2 |

**Total Effort Estimate**: **64-92 hours** (8-12 working days for 1 developer)

---

## Implementation Recommendations

### Phase Sequencing

**Week 1: Foundation (Critical Path)**
1. **Day 1-2**: ARCH-001.x (All private setters) + ARCH-NEW-003 (BaseEntity)
2. **Day 3-4**: ARCH-002.x (All factory methods)
3. **Day 5**: ARCH-004 (SurveyCode VO) - Quick win

**Week 2: Value Objects**
1. **Day 6-7**: ARCH-003 (AnswerValue hierarchy) - Most complex
2. **Day 8-9**: ARCH-005 (MediaContent VO)
3. **Day 10**: ARCH-NEW-002 (OptionsJson migration)

**Week 3: Rich Model & Integration**
1. **Day 11-12**: ARCH-006.1, ARCH-006.2 (Rich domain models)
2. **Day 13**: ARCH-NEW-001, ARCH-NEW-004 (Consolidation)
3. **Day 14-15**: ARCH-INT-001, ARCH-INT-002, ARCH-INT-003, ARCH-INT-004 (Service updates)

### Risk Mitigation

**High-Risk Tasks** (Require careful planning):
- ARCH-001.1 (Survey private setters) - Touches 50+ files
- ARCH-003 (AnswerValue hierarchy) - Complex polymorphic design
- ARCH-INT-003 (ResponseService) - Core business logic

**Mitigation Strategies**:
1. **Feature Branches**: One branch per phase
2. **Incremental Commits**: Commit after each task
3. **Test Coverage**: Write/update tests immediately after refactoring
4. **Pair Programming**: Complex tasks (ARCH-003, ARCH-INT-003) done in pairs
5. **Code Reviews**: All pull requests reviewed by 2+ developers

### Testing Strategy

**For Each Refactored Entity**:
1. Unit tests for factory methods
2. Unit tests for behavior methods
3. Integration tests for EF Core mapping
4. Regression tests for existing functionality

**Example - Survey.Create() Tests**:
```csharp
[Fact]
public void Create_WithValidData_CreatesSurvey()
{
    var survey = Survey.Create("Test Survey", "Description", 1, SurveyCode.Generate());

    Assert.Equal("Test Survey", survey.Title);
    Assert.False(survey.IsActive);
    Assert.Equal(0, survey.Questions.Count);
}

[Fact]
public void Create_WithEmptyTitle_ThrowsException()
{
    Assert.Throws<SurveyValidationException>(() =>
        Survey.Create("", null, 1, SurveyCode.Generate()));
}

[Fact]
public void Activate_WithoutQuestions_ThrowsException()
{
    var survey = Survey.Create("Test", null, 1, SurveyCode.Generate());
    var validationService = new Mock<ISurveyValidationService>();

    Assert.Throws<SurveyValidationException>(() =>
        survey.Activate(validationService.Object));
}
```

---

## Success Metrics

### Pre-Refactoring (Current State)

| Metric | Current Value | Target Value |
|--------|---------------|--------------|
| **DDD Score** | 4.5/10 | 8.5/10 |
| **Public Setters** | 100% of properties | 0% (all private) |
| **Factory Methods** | 0 entities | 6 entities (100%) |
| **Value Objects** | 1 (NextQuestionDeterminant) | 5 (SurveyCode, MediaContent, AnswerValue, OptionsJson) |
| **Business Logic in Services** | ~80% | ~20% (thin orchestrators) |
| **Validation Duplication** | 3x (DTO + Service + Entity) | 1x (Entity only) |
| **Authorization Checks Duplicated** | 12+ locations | 1 location (domain service) |
| **Mutable Collections** | 100% (ICollection) | 0% (IReadOnlyCollection) |

### Post-Refactoring (Target State)

**Architectural Purity**:
- ✅ All entities have private setters
- ✅ All entities use factory methods
- ✅ All primitive types replaced with value objects
- ✅ Business logic in entities, not services
- ✅ Single source of truth for validation
- ✅ Collections immutable from outside

**Code Quality**:
- ✅ No magic values (0, null, empty strings)
- ✅ No anemic domain model anti-pattern
- ✅ No primitive obsession
- ✅ No scattered validation
- ✅ No duplicated authorization checks

**Maintainability**:
- ✅ Easier to add new question types
- ✅ Easier to add new answer formats
- ✅ Easier to enforce business rules
- ✅ Easier to test domain logic

---

## Conclusion

This deep analysis reveals that the refactoring plan is **well-aligned with the codebase's actual needs**, but the **scope is larger than initially documented**. The plan correctly identifies the critical issues (public setters, missing factory methods, primitive obsession), but there are additional anti-patterns that should be addressed simultaneously:

1. **Direct Collection Modification** (included in ARCH-001)
2. **Scattered Authorization Logic** (new task ARCH-NEW-001)
3. **Magic Strings in OptionsJson** (new task ARCH-NEW-002)
4. **BaseEntity Public Setters** (new task ARCH-NEW-003)
5. **Validation Duplication** (new task ARCH-NEW-004)

**Recommended Approach**: Implement the refactoring plan **in phases** as documented, starting with the critical foundation (private setters + factory methods) before tackling value objects and rich domain models.

**Estimated Timeline**: **8-12 working days** for complete refactoring by 1 experienced developer, or **5-7 days** with pair programming on complex tasks.

**Go/No-Go Decision**: **GO** - The refactoring is essential for long-term maintainability and should be prioritized before implementing new features (e.g., Location Question Type).

---

**Report Generated**: 2025-11-27
**Analyzed By**: Architecture Analysis Agent
**Status**: Comprehensive Analysis Complete
**Next Steps**: Begin Phase 1A (Private Setters) implementation

