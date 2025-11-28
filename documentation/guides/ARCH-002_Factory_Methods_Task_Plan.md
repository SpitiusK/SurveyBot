# ARCH-002: Add Factory Methods to Entities - Implementation Task Plan

**Task ID**: ARCH-002
**Title**: Add Factory Methods to Entities
**Priority**: 🟠 HIGH
**Estimated Effort**: 4-6 hours
**Status**: 📋 Ready
**Dependencies**: ARCH-001 (Private Setters) MUST be completed first
**Phase**: Phase 1 - Value Objects & Encapsulation

---

## Task Overview

### Description
Add static factory methods to all domain entities to centralize object creation and validation logic. Replace direct constructor calls and property assignments with validated factory methods following the Factory Method design pattern. This enforces invariants at creation time and provides clear, intention-revealing APIs.

### Business Value
- **Validation at Creation**: Invalid entities cannot be constructed
- **Clear Intent**: `Survey.Create(...)` is more expressive than `new Survey { ... }`
- **Centralized Logic**: All creation validation in one place
- **Immutability Support**: Private constructors prevent bypassing validation
- **Foundation for DDD**: Enables rich domain model with guaranteed valid state

### Dependencies
- **MUST Complete First**: ARCH-001 (Private Setters)
  - Reason: Factory methods need private setters to enforce encapsulation
  - Without private setters, external code can still modify properties after factory creation
- **Blocks**: ARCH-006 (Rich Domain Model) builds on this pattern
- **Parallel Work**: Can implement factories for independent entities simultaneously

---

## Current State Analysis

### Files Requiring Modification

#### Core Layer - Entity Files (Add Factory Methods)
| File Path | Factory Methods to Add | Complexity |
|-----------|----------------------|------------|
| `src/SurveyBot.Core/Entities/User.cs` | `CreateUser`, `UpdateFromTelegram` | LOW |
| `src/SurveyBot.Core/Entities/Survey.cs` | `Create`, `CreateDraft` | MEDIUM |
| `src/SurveyBot.Core/Entities/Question.cs` | `CreateText`, `CreateSingleChoice`, `CreateMultipleChoice`, `CreateRating` | HIGH (4 factories) |
| `src/SurveyBot.Core/Entities/QuestionOption.cs` | `Create` | LOW |
| `src/SurveyBot.Core/Entities/Response.cs` | `Start`, `Resume` | LOW |
| `src/SurveyBot.Core/Entities/Answer.cs` | `CreateText`, `CreateChoice`, `CreateRating` | MEDIUM (3 factories) |

#### Infrastructure Layer - Service Files (Update to Use Factories)
| File Path | Method Updates | Impact |
|-----------|---------------|--------|
| `src/SurveyBot.Infrastructure/Services/SurveyService.cs` | `CreateSurveyAsync`, `UpdateSurveyAsync` | HIGH |
| `src/SurveyBot.Infrastructure/Services/QuestionService.cs` | `AddQuestionAsync`, `UpdateQuestionAsync` | HIGH |
| `src/SurveyBot.Infrastructure/Services/ResponseService.cs` | `StartResponseAsync`, `SaveAnswerAsync` | MEDIUM |
| `src/SurveyBot.Infrastructure/Services/UserService.cs` | `RegisterAsync`, `GetOrCreateUserAsync` | LOW |

### Current Anti-Pattern (Before)

```csharp
// SurveyService.cs - CURRENT (Scattered validation)
public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
{
    // Validation in service (not reusable)
    if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3)
        throw new SurveyValidationException("Title must be at least 3 characters");

    if (dto.Title.Length > 500)
        throw new SurveyValidationException("Title cannot exceed 500 characters");

    // Object creation scattered across multiple lines
    var survey = new Survey();  // ❌ Can create invalid entity
    survey.SetTitle(dto.Title);
    survey.SetDescription(dto.Description);
    survey.SetCreatorId(userId);
    survey.SetCode(await SurveyCodeGenerator.GenerateUniqueCodeAsync(...));
    survey.SetIsActive(false);
    survey.SetAllowMultipleResponses(dto.AllowMultipleResponses);
    survey.SetShowResults(dto.ShowResults);

    // More scattered logic...
}
```

### Target Pattern (After)

```csharp
// Survey.cs - IMPROVED (Centralized validation and creation)
public class Survey : BaseEntity
{
    // Private constructor - only factory can create
    private Survey() { }

    /// <summary>
    /// Creates a new survey with validation.
    /// </summary>
    public static Survey Create(
        string title,
        string? description,
        int creatorId,
        string surveyCode)
    {
        // ALL validation logic here
        if (string.IsNullOrWhiteSpace(title))
            throw new SurveyValidationException("Title is required");

        if (title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        if (title.Length > 500)
            throw new SurveyValidationException("Title cannot exceed 500 characters");

        if (description?.Length > 2000)
            throw new SurveyValidationException("Description cannot exceed 2000 characters");

        if (creatorId <= 0)
            throw new ArgumentException("Invalid creator ID", nameof(creatorId));

        if (string.IsNullOrWhiteSpace(surveyCode) || surveyCode.Length != 6)
            throw new ArgumentException("Survey code must be 6 characters", nameof(surveyCode));

        // Single atomic creation
        return new Survey
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            CreatorId = creatorId,
            Code = surveyCode.ToUpperInvariant(),
            IsActive = false,
            AllowMultipleResponses = false,
            ShowResults = false
        };
    }
}

// SurveyService.cs - IMPROVED (Thin, delegates to factory)
public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
{
    var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
        _surveyRepository.CodeExistsAsync);

    // ✅ Factory handles ALL validation
    var survey = Survey.Create(
        dto.Title,
        dto.Description,
        userId,
        code);

    var createdSurvey = await _surveyRepository.CreateAsync(survey);
    return _mapper.Map<SurveyDto>(createdSurvey);
}
```

---

## Implementation Steps

### Step 1: Add Factory to User Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/User.cs`

**Changes**:
```csharp
public class User : BaseEntity
{
    // ... existing private properties ...

    /// <summary>
    /// Private constructor for EF Core.
    /// Use Create() factory method for application code.
    /// </summary>
    private User() { }

    /// <summary>
    /// Creates a new user from Telegram data.
    /// </summary>
    /// <param name="telegramId">Telegram user ID (must be positive)</param>
    /// <param name="username">Telegram username (optional)</param>
    /// <param name="firstName">User's first name (optional)</param>
    /// <param name="lastName">User's last name (optional)</param>
    /// <returns>New user instance</returns>
    /// <exception cref="ArgumentException">If telegramId is invalid</exception>
    public static User Create(
        long telegramId,
        string? username = null,
        string? firstName = null,
        string? lastName = null)
    {
        if (telegramId <= 0)
            throw new ArgumentException("Telegram ID must be positive", nameof(telegramId));

        // Validate and normalize username
        var normalizedUsername = string.IsNullOrWhiteSpace(username)
            ? null
            : username.Trim().TrimStart('@');  // Remove @ if present

        return new User
        {
            TelegramId = telegramId,
            Username = normalizedUsername,
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            LastLoginAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates user data from Telegram (upsert pattern).
    /// </summary>
    public void UpdateFromTelegram(
        string? username,
        string? firstName,
        string? lastName)
    {
        Username = string.IsNullOrWhiteSpace(username)
            ? null
            : username.Trim().TrimStart('@');
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        LastLoginAt = DateTime.UtcNow;
        MarkAsModified();
    }

    // Remove old setter methods (replaced by factory)
    // SetTelegramId, SetUsername, etc. - DELETE THESE
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/UserService.cs`
- Method: `RegisterAsync` / `GetOrCreateUserAsync`
- Change from: `new User()` with setters → `User.Create(...)`

**Testing**:
- Verify user creation with factory
- Verify validation (negative telegramId throws)
- Run: `dotnet test --filter "FullyQualifiedName~UserService"`

---

### Step 2: Add Factory to Survey Entity (1 hour)

**File**: `src/SurveyBot.Core/Entities/Survey.cs`

**Changes**:
```csharp
public class Survey : BaseEntity
{
    // ... existing private properties ...

    /// <summary>
    /// Private constructor for EF Core.
    /// Use Create() or CreateDraft() factory methods.
    /// </summary>
    private Survey() { }

    /// <summary>
    /// Creates a new survey with full validation.
    /// Survey is created as inactive (must call Activate() later).
    /// </summary>
    /// <param name="title">Survey title (3-500 characters)</param>
    /// <param name="description">Optional description (max 2000 characters)</param>
    /// <param name="creatorId">ID of the user creating the survey</param>
    /// <param name="surveyCode">Unique 6-character survey code</param>
    /// <returns>New survey instance</returns>
    /// <exception cref="SurveyValidationException">If validation fails</exception>
    /// <exception cref="ArgumentException">If parameters are invalid</exception>
    public static Survey Create(
        string title,
        string? description,
        int creatorId,
        string surveyCode)
    {
        // Title validation
        if (string.IsNullOrWhiteSpace(title))
            throw new SurveyValidationException("Survey title is required");

        if (title.Length < 3)
            throw new SurveyValidationException("Survey title must be at least 3 characters long");

        if (title.Length > 500)
            throw new SurveyValidationException("Survey title cannot exceed 500 characters");

        // Description validation
        if (description != null && description.Length > 2000)
            throw new SurveyValidationException("Survey description cannot exceed 2000 characters");

        // Creator validation
        if (creatorId <= 0)
            throw new ArgumentException("Invalid creator ID", nameof(creatorId));

        // Survey code validation
        if (string.IsNullOrWhiteSpace(surveyCode))
            throw new ArgumentException("Survey code is required", nameof(surveyCode));

        if (surveyCode.Length != 6)
            throw new ArgumentException("Survey code must be exactly 6 characters", nameof(surveyCode));

        if (!surveyCode.All(c => char.IsLetterOrDigit(c)))
            throw new ArgumentException("Survey code must contain only letters and numbers", nameof(surveyCode));

        return new Survey
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatorId = creatorId,
            Code = surveyCode.ToUpperInvariant(),
            IsActive = false,  // Always create inactive
            AllowMultipleResponses = false,
            ShowResults = false
        };
    }

    /// <summary>
    /// Creates a draft survey with minimal validation (for UI workflows).
    /// </summary>
    public static Survey CreateDraft(string title, int creatorId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new SurveyValidationException("Title is required for draft");

        if (creatorId <= 0)
            throw new ArgumentException("Invalid creator ID", nameof(creatorId));

        return new Survey
        {
            Title = title.Trim(),
            CreatorId = creatorId,
            Code = null,  // Will be generated when activated
            IsActive = false
        };
    }

    /// <summary>
    /// Updates survey metadata with validation.
    /// Cannot update if survey is active and has responses.
    /// </summary>
    public void UpdateMetadata(
        string title,
        string? description,
        bool allowMultipleResponses,
        bool showResults)
    {
        if (IsActive && Responses.Any())
            throw new SurveyOperationException(
                "Cannot modify active survey with responses. Deactivate first or create new version.");

        // Title validation
        if (string.IsNullOrWhiteSpace(title))
            throw new SurveyValidationException("Survey title is required");

        if (title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        if (title.Length > 500)
            throw new SurveyValidationException("Title cannot exceed 500 characters");

        // Description validation
        if (description != null && description.Length > 2000)
            throw new SurveyValidationException("Description cannot exceed 2000 characters");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AllowMultipleResponses = allowMultipleResponses;
        ShowResults = showResults;
        MarkAsModified();
    }

    // Remove old setter methods (replaced by factories and UpdateMetadata)
    // SetTitle, SetDescription, etc. - DELETE THESE
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`
- Method: `CreateSurveyAsync`
  ```csharp
  // OLD
  var survey = new Survey();
  survey.SetTitle(dto.Title);
  // ... many more setters ...

  // NEW
  var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(...);
  var survey = Survey.Create(dto.Title, dto.Description, userId, code);
  ```
- Method: `UpdateSurveyAsync`
  ```csharp
  // OLD
  survey.SetTitle(dto.Title);
  survey.SetDescription(dto.Description);
  survey.SetAllowMultipleResponses(dto.AllowMultipleResponses);
  survey.SetShowResults(dto.ShowResults);

  // NEW
  survey.UpdateMetadata(
      dto.Title,
      dto.Description,
      dto.AllowMultipleResponses,
      dto.ShowResults);
  ```

**Testing**:
- Verify survey creation with factory
- Verify validation (short title throws, long title throws)
- Verify survey code normalization (lowercase → uppercase)
- Run: `dotnet test --filter "FullyQualifiedName~SurveyService"`

---

### Step 3: Add Factories to Question Entity (1.5 hours)

**File**: `src/SurveyBot.Core/Entities/Question.cs`

**Reasoning**: Questions have type-specific validation, so we use **separate factories for each type** rather than one generic factory.

**Changes**:
```csharp
public class Question : BaseEntity
{
    // ... existing private properties ...

    /// <summary>
    /// Private constructor for EF Core.
    /// Use CreateText(), CreateSingleChoice(), CreateMultipleChoice(), or CreateRating().
    /// </summary>
    private Question() { }

    /// <summary>
    /// Creates a text question.
    /// </summary>
    /// <param name="surveyId">ID of the survey this question belongs to</param>
    /// <param name="questionText">The question text (required)</param>
    /// <param name="orderIndex">Position in the survey (0-based)</param>
    /// <param name="isRequired">Whether the question must be answered</param>
    /// <param name="mediaContent">Optional multimedia content JSON</param>
    /// <returns>New text question instance</returns>
    public static Question CreateText(
        int surveyId,
        string questionText,
        int orderIndex,
        bool isRequired = true,
        string? mediaContent = null)
    {
        ValidateCommonParameters(surveyId, questionText, orderIndex);

        return new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText.Trim(),
            QuestionType = QuestionType.Text,
            OrderIndex = orderIndex,
            IsRequired = isRequired,
            MediaContent = mediaContent,
            OptionsJson = null,  // Text questions have no options
            DefaultNext = null   // Can be set later via SetDefaultNext()
        };
    }

    /// <summary>
    /// Creates a single-choice question.
    /// </summary>
    /// <param name="surveyId">ID of the survey</param>
    /// <param name="questionText">The question text</param>
    /// <param name="options">List of choice options (must have 2-10 items)</param>
    /// <param name="orderIndex">Position in survey</param>
    /// <param name="isRequired">Whether required</param>
    /// <param name="mediaContent">Optional media</param>
    /// <returns>New single-choice question instance</returns>
    /// <exception cref="QuestionValidationException">If options are invalid</exception>
    public static Question CreateSingleChoice(
        int surveyId,
        string questionText,
        List<string> options,
        int orderIndex,
        bool isRequired = true,
        string? mediaContent = null)
    {
        ValidateCommonParameters(surveyId, questionText, orderIndex);
        ValidateChoiceOptions(options);

        return new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText.Trim(),
            QuestionType = QuestionType.SingleChoice,
            OrderIndex = orderIndex,
            IsRequired = isRequired,
            MediaContent = mediaContent,
            OptionsJson = System.Text.Json.JsonSerializer.Serialize(options),
            DefaultNext = null
        };
    }

    /// <summary>
    /// Creates a multiple-choice question.
    /// </summary>
    public static Question CreateMultipleChoice(
        int surveyId,
        string questionText,
        List<string> options,
        int orderIndex,
        bool isRequired = true,
        string? mediaContent = null)
    {
        ValidateCommonParameters(surveyId, questionText, orderIndex);
        ValidateChoiceOptions(options);

        return new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText.Trim(),
            QuestionType = QuestionType.MultipleChoice,
            OrderIndex = orderIndex,
            IsRequired = isRequired,
            MediaContent = mediaContent,
            OptionsJson = System.Text.Json.JsonSerializer.Serialize(options),
            DefaultNext = null
        };
    }

    /// <summary>
    /// Creates a rating question (1-5 scale).
    /// </summary>
    public static Question CreateRating(
        int surveyId,
        string questionText,
        int orderIndex,
        bool isRequired = true,
        string? mediaContent = null)
    {
        ValidateCommonParameters(surveyId, questionText, orderIndex);

        return new Question
        {
            SurveyId = surveyId,
            QuestionText = questionText.Trim(),
            QuestionType = QuestionType.Rating,
            OrderIndex = orderIndex,
            IsRequired = isRequired,
            MediaContent = mediaContent,
            OptionsJson = null,  // Rating questions don't use options JSON
            DefaultNext = null
        };
    }

    // Validation helpers
    private static void ValidateCommonParameters(
        int surveyId,
        string questionText,
        int orderIndex)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));

        if (string.IsNullOrWhiteSpace(questionText))
            throw new QuestionValidationException("Question text is required");

        if (questionText.Length > 1000)
            throw new QuestionValidationException("Question text cannot exceed 1000 characters");

        if (orderIndex < 0)
            throw new ArgumentException("Order index must be non-negative", nameof(orderIndex));
    }

    private static void ValidateChoiceOptions(List<string> options)
    {
        if (options == null || options.Count == 0)
            throw new QuestionValidationException("Choice questions must have at least 2 options");

        if (options.Count < 2)
            throw new QuestionValidationException("Choice questions must have at least 2 options");

        if (options.Count > 10)
            throw new QuestionValidationException("Questions cannot have more than 10 options");

        if (options.Any(string.IsNullOrWhiteSpace))
            throw new QuestionValidationException("All options must have text");

        var duplicates = options
            .GroupBy(o => o.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
            throw new QuestionValidationException(
                $"Duplicate options not allowed: {string.Join(", ", duplicates)}");
    }

    /// <summary>
    /// Sets the default next question for non-branching flow.
    /// </summary>
    public void SetDefaultNext(NextQuestionDeterminant? defaultNext)
    {
        DefaultNext = defaultNext;
        MarkAsModified();
    }

    // Remove old setter methods
    // SetQuestionText, SetQuestionType, etc. - DELETE THESE
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
- Method: `AddQuestionAsync`
  ```csharp
  // OLD
  var question = new Question();
  question.SetSurveyId(surveyId);
  question.SetQuestionText(dto.QuestionText);
  question.SetQuestionType(dto.QuestionType);
  // ... many more setters ...

  // NEW - Type-specific factory
  Question question = dto.QuestionType switch
  {
      QuestionType.Text => Question.CreateText(
          surveyId,
          dto.QuestionText,
          orderIndex,
          dto.IsRequired,
          dto.MediaContent),

      QuestionType.SingleChoice => Question.CreateSingleChoice(
          surveyId,
          dto.QuestionText,
          dto.Options!,  // Validated earlier
          orderIndex,
          dto.IsRequired,
          dto.MediaContent),

      QuestionType.MultipleChoice => Question.CreateMultipleChoice(
          surveyId,
          dto.QuestionText,
          dto.Options!,
          orderIndex,
          dto.IsRequired,
          dto.MediaContent),

      QuestionType.Rating => Question.CreateRating(
          surveyId,
          dto.QuestionText,
          orderIndex,
          dto.IsRequired,
          dto.MediaContent),

      _ => throw new InvalidQuestionTypeException(dto.QuestionType)
  };

  // Set conditional flow if provided
  if (dto.DefaultNext != null)
      question.SetDefaultNext(dto.DefaultNext.ToValueObject());
  ```

**Testing**:
- Verify each factory creates correct question type
- Verify validation (empty text throws, duplicate options throw)
- Run: `dotnet test --filter "FullyQualifiedName~QuestionService"`

---

### Step 4: Add Factory to QuestionOption Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/QuestionOption.cs`

**Changes**:
```csharp
public class QuestionOption : BaseEntity
{
    // ... existing private properties ...

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private QuestionOption() { }

    /// <summary>
    /// Creates a new question option.
    /// </summary>
    /// <param name="questionId">ID of the parent question</param>
    /// <param name="text">Option text (required)</param>
    /// <param name="orderIndex">Position in option list (0-based)</param>
    /// <param name="next">Optional conditional flow navigation</param>
    /// <returns>New option instance</returns>
    public static QuestionOption Create(
        int questionId,
        string text,
        int orderIndex,
        NextQuestionDeterminant? next = null)
    {
        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Option text is required", nameof(text));

        if (text.Length > 200)
            throw new ArgumentException("Option text cannot exceed 200 characters", nameof(text));

        if (orderIndex < 0)
            throw new ArgumentException("Order index must be non-negative", nameof(orderIndex));

        return new QuestionOption
        {
            QuestionId = questionId,
            Text = text.Trim(),
            OrderIndex = orderIndex,
            Next = next
        };
    }

    /// <summary>
    /// Updates the conditional flow for this option.
    /// </summary>
    public void UpdateNext(NextQuestionDeterminant? next)
    {
        Next = next;
        MarkAsModified();
    }

    // Remove old setter methods
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/QuestionService.cs`
- Update option creation in `AddQuestionAsync`:
  ```csharp
  // OLD
  var option = new QuestionOption();
  option.SetQuestionId(question.Id);
  option.SetText(optionText);
  option.SetOrderIndex(i);

  // NEW
  var option = QuestionOption.Create(
      question.Id,
      optionText,
      i,
      next: null);  // Flow set later if needed
  ```

**Testing**:
- Verify option creation
- Run: `dotnet test --filter "FullyQualifiedName~QuestionOption"`

---

### Step 5: Add Factories to Response Entity (0.5 hours)

**File**: `src/SurveyBot.Core/Entities/Response.cs`

**Changes**:
```csharp
public class Response
{
    // ... existing private properties ...

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Response() { }

    /// <summary>
    /// Starts a new response to a survey.
    /// </summary>
    /// <param name="surveyId">ID of the survey</param>
    /// <param name="respondentTelegramId">Telegram ID of respondent</param>
    /// <returns>New response instance</returns>
    public static Response Start(int surveyId, long respondentTelegramId)
    {
        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));

        if (respondentTelegramId <= 0)
            throw new ArgumentException("Respondent Telegram ID must be positive", nameof(respondentTelegramId));

        return new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = respondentTelegramId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            VisitedQuestionIds = new List<int>()
        };
    }

    /// <summary>
    /// Resumes an incomplete response.
    /// </summary>
    public static Response Resume(
        int responseId,
        int surveyId,
        long respondentTelegramId,
        DateTime startedAt,
        List<int> visitedQuestionIds)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));

        if (surveyId <= 0)
            throw new ArgumentException("Survey ID must be positive", nameof(surveyId));

        if (respondentTelegramId <= 0)
            throw new ArgumentException("Respondent ID must be positive", nameof(respondentTelegramId));

        return new Response
        {
            Id = responseId,
            SurveyId = surveyId,
            RespondentTelegramId = respondentTelegramId,
            IsComplete = false,
            StartedAt = startedAt,
            VisitedQuestionIds = visitedQuestionIds ?? new List<int>()
        };
    }

    /// <summary>
    /// Marks the response as complete.
    /// </summary>
    public void Complete()
    {
        if (IsComplete)
            throw new InvalidOperationException("Response is already complete");

        IsComplete = true;
        SubmittedAt = DateTime.UtcNow;
    }

    // Keep existing helper methods (HasVisitedQuestion, RecordVisitedQuestion)

    // Remove old setter methods
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/ResponseService.cs`
- Method: `StartResponseAsync`
  ```csharp
  // OLD
  var response = new Response();
  response.SetSurveyId(surveyId);
  response.SetRespondentTelegramId(telegramUserId);
  response.SetIsComplete(false);
  response.SetStartedAt(DateTime.UtcNow);

  // NEW
  var response = Response.Start(surveyId, telegramUserId);
  ```
- Method: `CompleteResponseAsync`
  ```csharp
  // OLD
  response.SetIsComplete(true);
  response.SetSubmittedAt(DateTime.UtcNow);

  // NEW
  response.Complete();
  ```

**Testing**:
- Verify response start/complete
- Run: `dotnet test --filter "FullyQualifiedName~ResponseService"`

---

### Step 6: Add Factories to Answer Entity (1 hour)

**File**: `src/SurveyBot.Core/Entities/Answer.cs`

**Changes**:
```csharp
public class Answer
{
    // ... existing private properties ...

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Answer() { }

    /// <summary>
    /// Creates an answer for a text question.
    /// </summary>
    /// <param name="responseId">ID of the response</param>
    /// <param name="questionId">ID of the question</param>
    /// <param name="answerText">The text answer</param>
    /// <param name="next">Next question determinant</param>
    /// <returns>New text answer instance</returns>
    public static Answer CreateText(
        int responseId,
        int questionId,
        string answerText,
        NextQuestionDeterminant next)
    {
        ValidateCommonParameters(responseId, questionId);

        if (string.IsNullOrWhiteSpace(answerText))
            throw new InvalidAnswerFormatException(
                questionId,
                QuestionType.Text,
                "Text answer cannot be empty");

        if (answerText.Length > 5000)
            throw new InvalidAnswerFormatException(
                questionId,
                QuestionType.Text,
                "Text answer cannot exceed 5000 characters");

        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = answerText.Trim(),
            AnswerJson = null,
            Next = next ?? NextQuestionDeterminant.End(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an answer for a choice question (single or multiple).
    /// </summary>
    /// <param name="responseId">ID of the response</param>
    /// <param name="questionId">ID of the question</param>
    /// <param name="selectedOptions">Selected option texts</param>
    /// <param name="next">Next question determinant</param>
    /// <returns>New choice answer instance</returns>
    public static Answer CreateChoice(
        int responseId,
        int questionId,
        List<string> selectedOptions,
        NextQuestionDeterminant next)
    {
        ValidateCommonParameters(responseId, questionId);

        if (selectedOptions == null || selectedOptions.Count == 0)
            throw new InvalidAnswerFormatException(
                questionId,
                QuestionType.SingleChoice,
                "At least one option must be selected");

        var answerJson = System.Text.Json.JsonSerializer.Serialize(
            new { selectedOptions });

        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = null,
            AnswerJson = answerJson,
            Next = next ?? NextQuestionDeterminant.End(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an answer for a rating question.
    /// </summary>
    /// <param name="responseId">ID of the response</param>
    /// <param name="questionId">ID of the question</param>
    /// <param name="ratingValue">Rating value (1-5)</param>
    /// <param name="next">Next question determinant</param>
    /// <returns>New rating answer instance</returns>
    public static Answer CreateRating(
        int responseId,
        int questionId,
        int ratingValue,
        NextQuestionDeterminant next)
    {
        ValidateCommonParameters(responseId, questionId);

        if (ratingValue < 1 || ratingValue > 5)
            throw new InvalidAnswerFormatException(
                questionId,
                QuestionType.Rating,
                $"Rating must be 1-5, got {ratingValue}");

        var answerJson = System.Text.Json.JsonSerializer.Serialize(
            new { rating = ratingValue });

        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = null,
            AnswerJson = answerJson,
            Next = next ?? NextQuestionDeterminant.End(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void ValidateCommonParameters(int responseId, int questionId)
    {
        if (responseId <= 0)
            throw new ArgumentException("Response ID must be positive", nameof(responseId));

        if (questionId <= 0)
            throw new ArgumentException("Question ID must be positive", nameof(questionId));
    }

    // Remove old setter methods
}
```

**Service Updates**:
- File: `src/SurveyBot.Infrastructure/Services/ResponseService.cs`
- Method: `SaveAnswerAsync` - Refactor answer creation:
  ```csharp
  // OLD
  var answer = new Answer();
  answer.SetResponseId(responseId);
  answer.SetQuestionId(questionId);
  answer.SetAnswerText(answerText);
  answer.SetAnswerJson(CreateAnswerJson(...));
  answer.SetNext(nextStep);

  // NEW - Type-specific factory
  Answer answer = question.QuestionType switch
  {
      QuestionType.Text => Answer.CreateText(
          responseId,
          questionId,
          answerText!,
          nextStep),

      QuestionType.SingleChoice or QuestionType.MultipleChoice => Answer.CreateChoice(
          responseId,
          questionId,
          selectedOptions!,
          nextStep),

      QuestionType.Rating => Answer.CreateRating(
          responseId,
          questionId,
          ratingValue!.Value,
          nextStep),

      _ => throw new InvalidQuestionTypeException(question.QuestionType)
  };
  ```

**Testing**:
- Verify answer creation for each type
- Verify validation (empty text, invalid rating)
- Run: `dotnet test --filter "FullyQualifiedName~Answer"`

---

### Step 7: Update EF Core Configurations (0.5 hours)

**Files**: All configuration files

**Required Changes**: Minimal - EF Core can invoke private parameterless constructors via reflection.

**Verification**:
```csharp
// SurveyConfiguration.cs - Example
public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        // No special configuration needed for private constructor
        // EF Core automatically uses reflection to invoke private parameterless constructor

        // ... existing configuration unchanged ...
    }
}
```

**Note**: If any configuration explicitly referenced constructors (unlikely), update those references.

**Testing**:
- Verify EF Core can load all entities
- Run: `dotnet ef database update`
- Run: `dotnet test --filter "FullyQualifiedName~Repository"`

---

### Step 8: Compile and Fix Remaining Usages (1 hour)

**Process**:
1. Compile: `dotnet build`
2. Fix compilation errors:
   - Replace `new Entity()` → `Entity.Create(...)`
   - Replace setter method chains → factory method with parameters
   - Update tests to use factories
3. Search for anti-patterns:
   ```bash
   # Find direct constructor calls
   grep -r "new Survey()" --include="*.cs" src/
   grep -r "new Question()" --include="*.cs" src/
   grep -r "new Answer()" --include="*.cs" src/
   ```

**Common Fixes**:
- Test setup methods
- Data seeding (if any)
- Migration scripts (if any manually create entities)

---

### Step 9: Run Full Test Suite (0.5 hours)

**Commands**:
```bash
dotnet clean
dotnet build
dotnet test --verbosity normal
```

**Expected Results**:
- All tests pass
- No constructor access errors
- Factories work correctly
- Validation throws appropriate exceptions

---

## Testing Strategy

### Unit Tests to Add

**File**: `tests/SurveyBot.Tests/Unit/Entities/EntityFactoryTests.cs` (NEW)

```csharp
public class EntityFactoryTests
{
    [Fact]
    public void Survey_Create_WithValidParameters_ReturnsValidSurvey()
    {
        // Arrange
        var title = "Test Survey";
        var description = "Test Description";
        var creatorId = 1;
        var code = "ABC123";

        // Act
        var survey = Survey.Create(title, description, creatorId, code);

        // Assert
        Assert.Equal(title, survey.Title);
        Assert.Equal(description, survey.Description);
        Assert.Equal(creatorId, survey.CreatorId);
        Assert.Equal("ABC123", survey.Code);  // Normalized to uppercase
        Assert.False(survey.IsActive);
    }

    [Fact]
    public void Survey_Create_WithShortTitle_ThrowsException()
    {
        // Arrange
        var shortTitle = "ab";  // Less than 3 characters

        // Act & Assert
        var exception = Assert.Throws<SurveyValidationException>(
            () => Survey.Create(shortTitle, null, 1, "ABC123"));

        Assert.Contains("3 characters", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Survey_Create_WithInvalidTitle_ThrowsException(string invalidTitle)
    {
        // Act & Assert
        Assert.Throws<SurveyValidationException>(
            () => Survey.Create(invalidTitle, null, 1, "ABC123"));
    }

    [Fact]
    public void Question_CreateText_WithValidParameters_ReturnsTextQuestion()
    {
        // Arrange
        var surveyId = 1;
        var text = "What is your favorite color?";
        var orderIndex = 0;

        // Act
        var question = Question.CreateText(surveyId, text, orderIndex);

        // Assert
        Assert.Equal(QuestionType.Text, question.QuestionType);
        Assert.Equal(text, question.QuestionText);
        Assert.Equal(surveyId, question.SurveyId);
        Assert.Null(question.OptionsJson);
    }

    [Fact]
    public void Question_CreateSingleChoice_WithDuplicateOptions_ThrowsException()
    {
        // Arrange
        var options = new List<string> { "Option A", "Option B", "Option A" };

        // Act & Assert
        var exception = Assert.Throws<QuestionValidationException>(
            () => Question.CreateSingleChoice(1, "Choose one", options, 0));

        Assert.Contains("Duplicate options", exception.Message);
    }

    [Fact]
    public void Answer_CreateText_WithEmptyText_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(
            () => Answer.CreateText(1, 1, "", NextQuestionDeterminant.End()));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Answer_CreateRating_WithInvalidRating_ThrowsException(int invalidRating)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidAnswerFormatException>(
            () => Answer.CreateRating(1, 1, invalidRating, NextQuestionDeterminant.End()));

        Assert.Contains("1-5", exception.Message);
    }

    // Add tests for all factories...
}
```

### Integration Tests to Update

**Pattern**: Update test setup to use factories instead of direct construction.

```csharp
// BEFORE
var survey = new Survey
{
    Title = "Test Survey",
    CreatorId = 1,
    Code = "TEST01",
    IsActive = false
};

// AFTER
var survey = Survey.Create("Test Survey", null, 1, "TEST01");
```

---

## Risk Assessment

### HIGH Risk

| Risk | Impact | Mitigation |
|------|--------|------------|
| **EF Core Cannot Invoke Private Constructor** | Runtime errors | EF Core HAS supported private constructors since v1.0 - verify with test |
| **Many Service Files Need Updates** | Time-consuming | Systematic approach, use compiler errors as guide |

### MEDIUM Risk

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Missed Constructor Calls** | Runtime errors later | Use grep/search to find all `new Entity()` calls |
| **Test Failures** | Delayed completion | Update tests incrementally as entities are refactored |

### Breaking Changes

**NONE** - Internal refactoring only:
- API unchanged
- Database unchanged
- External behavior identical

---

## Acceptance Criteria

### Functionality

- [ ] All entities have private parameterless constructors
- [ ] All entities have public static factory methods
- [ ] Factory methods validate all parameters
- [ ] No direct `new Entity()` calls in service layer (except EF Core)
- [ ] EF Core can still create entities via reflection
- [ ] All CRUD operations work correctly

### Testing

- [ ] All existing tests pass
- [ ] New factory tests added (EntityFactoryTests.cs)
- [ ] Validation tests added for each factory
- [ ] Test coverage maintained or improved

### Code Quality

- [ ] No compilation errors
- [ ] No compiler warnings
- [ ] XML documentation on all factory methods
- [ ] Consistent factory method naming

### Documentation

- [ ] Core CLAUDE.md updated with factory pattern
- [ ] Completion report created
- [ ] Task marked complete in priority plan

---

## Technical Notes

### Factory Method Pattern Benefits

1. **Validation Centralization**: All creation validation in one place
2. **Named Constructors**: `Survey.Create()` vs `new Survey()`
3. **Encapsulation**: Private constructors prevent bypassing validation
4. **Immutability Support**: Can ensure object is fully initialized
5. **Testing**: Easy to mock factories if needed

### EF Core Compatibility

EF Core can:
- Invoke private parameterless constructors via reflection
- Set private properties via reflection
- Use backing fields for collections

No special configuration needed for private constructors.

---

## Completion Checklist

- [ ] Step 1: User factory
- [ ] Step 2: Survey factory
- [ ] Step 3: Question factories
- [ ] Step 4: QuestionOption factory
- [ ] Step 5: Response factories
- [ ] Step 6: Answer factories
- [ ] Step 7: EF Core configurations
- [ ] Step 8: Fix service layer
- [ ] Step 9: Test suite passes
- [ ] Documentation updated
- [ ] Completion report created

---

**Task Plan Created**: 2025-11-27
**Author**: project-manager-agent
**Status**: 📋 Ready (depends on ARCH-001)
**Estimated Time**: 4-6 hours
