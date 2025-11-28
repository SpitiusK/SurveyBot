# SurveyBot.Core - Domain Layer Documentation

**[← Back to Main Documentation](C:\Users\User\Desktop\SurveyBot\CLAUDE.md)**

**Layer**: Domain | **Dependencies**: None | **Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core`

**Version**: 1.5.0 (Location Question Type) | **Last Updated**: 2025-11-27

## Overview

The Core project is the heart of the application containing all business entities, domain logic, and contracts (interfaces). Following Clean Architecture principles, **Core has zero external dependencies** - no references to Infrastructure, Bot, API, or any framework-specific libraries. All other layers depend on Core.

**Purpose**: Define domain models, business rules, repository/service interfaces, DTOs, and domain exceptions. Core is the stable center that other layers adapt to.

**Dependency Rule**: Core never references Infrastructure, Bot, or API. Dependencies point INWARD.

### Clean Architecture Compliance Verification

**Zero Dependencies Confirmed**:
- ✅ No references to Infrastructure, Bot, or API projects
- ✅ No external NuGet packages (only .NET 8.0 runtime libraries)
- ✅ No framework-specific dependencies (EF Core, ASP.NET, Telegram.Bot)
- ✅ Pure domain logic and contracts only

**Dependency Inversion Principle**:
- Core defines **interfaces** (contracts)
- Infrastructure/Bot/API **implement** these interfaces
- Outer layers depend on Core, never the reverse

**Verification**: Check `SurveyBot.Core.csproj` - should have ZERO `<PackageReference>` or `<ProjectReference>` items.

---

## Project Structure

```
SurveyBot.Core/
├── Entities/                      # Domain entities (7 total)
│   ├── BaseEntity.cs              # Abstract base class (Id, timestamps)
│   ├── User.cs                    # User entity (Telegram integration)
│   ├── Survey.cs                  # Survey entity (with activation logic)
│   ├── Question.cs                # Question entity (with conditional flow)
│   ├── QuestionOption.cs          # Option entity (NEW v1.4.0 - structured options)
│   ├── Response.cs                # Response entity (with cycle tracking)
│   └── Answer.cs                  # Answer entity (with next step logic)
├── Interfaces/                    # Repository & service contracts (16 total)
│   ├── IRepository.cs             # Generic repository base
│   ├── ISurveyRepository.cs       # Survey-specific methods
│   ├── IQuestionRepository.cs     # Question operations
│   ├── IResponseRepository.cs     # Response tracking
│   ├── IAnswerRepository.cs       # Answer operations
│   ├── IUserRepository.cs         # User management
│   ├── ISurveyService.cs          # Survey business logic
│   ├── IQuestionService.cs        # Question management
│   ├── IResponseService.cs        # Response processing
│   ├── IUserService.cs            # User operations
│   ├── IAuthService.cs            # Authentication
│   ├── ISurveyValidationService.cs # Cycle detection (NEW v1.4.0)
│   ├── IMediaStorageService.cs    # Media storage (NEW v1.3.0)
│   └── IMediaValidationService.cs # Media validation (NEW v1.3.0)
├── DTOs/                          # Data transfer objects (42+ DTOs in 8 categories)
│   ├── Answer/                    # Answer DTOs
│   ├── Auth/                      # Authentication DTOs
│   ├── Common/                    # Shared DTOs (PagedResult, ApiResponse)
│   ├── Media/                     # Media DTOs (NEW v1.3.0)
│   ├── Question/                  # Question DTOs (with flow DTOs v1.4.0)
│   ├── Response/                  # Response DTOs
│   ├── Statistics/                # Statistics DTOs
│   ├── Survey/                    # Survey DTOs
│   ├── ConditionalFlowDto.cs      # Flow configuration (NEW v1.4.0)
│   ├── NextQuestionDeterminantDto.cs # Next step DTO (NEW v1.4.0)
│   └── UpdateQuestionFlowDto.cs   # Flow update DTO (NEW v1.4.0)
├── ValueObjects/                  # Domain value objects (v1.4.0+)
│   ├── NextQuestionDeterminant.cs # Next step value object (DDD pattern - v1.4.0)
│   └── Answers/                   # Polymorphic answer value objects (NEW v1.5.0)
│       ├── AnswerValue.cs         # Abstract base with [JsonDerivedType]
│       ├── TextAnswerValue.cs     # Text answer (max 5000 chars)
│       ├── SingleChoiceAnswerValue.cs  # Single selection with validation
│       ├── MultipleChoiceAnswerValue.cs # Multiple selections
│       ├── RatingAnswerValue.cs   # Rating with min/max validation
│       └── AnswerValueFactory.cs  # Factory for parsing/creating values
├── Enums/                         # Domain enumerations (NEW v1.4.0)
│   ├── QuestionType.cs            # Text, SingleChoice, MultipleChoice, Rating
│   └── NextStepType.cs            # GoToQuestion, EndSurvey
├── Constants/                     # Domain constants (NEW v1.4.0)
│   └── SurveyConstants.cs         # EndOfSurveyMarker, limits, validation rules
├── Exceptions/                    # Domain-specific exceptions (10 total)
│   ├── SurveyNotFoundException.cs
│   ├── QuestionNotFoundException.cs
│   ├── SurveyValidationException.cs
│   ├── SurveyOperationException.cs
│   ├── InvalidAnswerFormatException.cs
│   ├── DuplicateResponseException.cs
│   ├── UnauthorizedAccessException.cs
│   ├── MediaValidationException.cs    # NEW v1.3.0
│   ├── MediaStorageException.cs       # NEW v1.3.0
│   └── SurveyCycleException.cs        # NEW v1.4.0
├── Configuration/                 # Configuration models
│   └── JwtSettings.cs             # JWT authentication settings
├── Models/                        # Domain models
│   └── ValidationResult.cs        # Validation result wrapper
├── Extensions/                    # Extension methods (NEW v1.4.0)
│   └── QuestionExtensions.cs      # Question helper methods
└── Utilities/                     # Domain utilities
    └── SurveyCodeGenerator.cs     # 6-char code generator (Base36)
```

**File Count Summary** (v1.5.0):
- **Entities**: 7 (BaseEntity + 6 domain entities, all with private setters + factory methods)
- **Interfaces**: 16 (1 generic + 15 specific)
- **DTOs**: 42+ (organized in 8 categories)
- **Value Objects**: 7 total
  - NextQuestionDeterminant (v1.4.0)
  - AnswerValue hierarchy: AnswerValue (abstract), TextAnswerValue, SingleChoiceAnswerValue, MultipleChoiceAnswerValue, RatingAnswerValue (v1.5.0)
  - AnswerValueFactory (v1.5.0)
- **Enums**: 2 (QuestionType, NextStepType)
- **Exceptions**: 10 (domain-specific)
- **Constants**: 1 (SurveyConstants)
- **Utilities**: 1 (SurveyCodeGenerator)

---

## DDD Patterns (v1.5.0)

### Encapsulation with Private Setters (ARCH-001)

All entities now enforce proper encapsulation using private setters. Direct property modification is no longer allowed - entities control their own state through dedicated methods.

**Pattern**:
```csharp
public class Survey : BaseEntity
{
    // Private setter - cannot be set directly from outside
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // Private backing field for collections
    private readonly List<Question> _questions = new();

    // Exposed as read-only collection
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    // Modification through controlled methods
    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (_questions.Count == 0)
            throw new SurveyValidationException("Cannot activate survey without questions");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Benefits**:
- **Invariant enforcement**: Business rules validated on every modification
- **Audit trail**: Can add logging/events in setter methods
- **Immutability of collections**: Cannot replace entire collection, only add/remove items
- **EF Core compatible**: Internal methods like `AddQuestionInternal()` for framework use

**All entities with private setters**: User, Survey, Question, QuestionOption, Response, Answer

### Factory Methods (ARCH-002)

All entities use static factory methods for creation instead of public constructors. This centralizes validation and ensures entities are always created in a valid state.

**Pattern**:
```csharp
public class Survey : BaseEntity
{
    // Private constructor - cannot use 'new Survey()' from outside
    private Survey() { }

    // Factory method - the ONLY way to create a survey
    public static Survey Create(
        string title,
        int creatorId,
        string? description = null,
        string? code = null,
        bool isActive = false,
        bool allowMultipleResponses = false,
        bool showResults = true)
    {
        // Validation at construction
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (creatorId <= 0)
            throw new ArgumentException("Invalid creator ID", nameof(creatorId));

        // Auto-generation of defaults
        return new Survey
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            CreatorId = creatorId,
            Code = code ?? SurveyCodeGenerator.GenerateCode(),  // Auto-generate code
            IsActive = isActive,
            AllowMultipleResponses = allowMultipleResponses,
            ShowResults = showResults,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

// Usage
var survey = Survey.Create("My Survey", userId: 123, description: "Survey description");
// NOT: var survey = new Survey { Title = "My Survey" };  // Won't compile
```

**Factory Methods Available**:

| Entity | Factory Method | Validation |
|--------|---------------|------------|
| Survey | `Survey.Create(title, creatorId, ...)` | Title length, CreatorId > 0 |
| Question | `Question.Create(surveyId, text, type, ...)` | QuestionText not empty |
| QuestionOption | `QuestionOption.Create(questionId, text, orderIndex)` | Text not empty |
| Response | `Response.Create(surveyId, respondentId)` | SurveyId, RespondentId > 0 |
| Answer | `Answer.CreateWithValue(responseId, questionId, value, ...)` | All IDs > 0, value not null |
| Answer | `Answer.CreateTextAnswer(responseId, questionId, text, ...)` | For text questions |
| Answer | `Answer.CreateJsonAnswer(responseId, questionId, json, ...)` | For choice/rating questions |
| User | `User.Create(telegramId, ...)` | TelegramId > 0 |

**Benefits**:
- **Centralized validation**: All validation logic in one place
- **Clear intent**: `Survey.Create()` more explicit than `new Survey()`
- **Impossible invalid states**: Cannot create entity without required data
- **Auto-generation**: Handles timestamps, codes automatically

### Polymorphic Value Objects (ARCH-003)

The AnswerValue hierarchy provides type-safe answer handling with polymorphic serialization. Each question type has its own value object class.

**Hierarchy**:
```
AnswerValue (abstract base)
├── TextAnswerValue
├── SingleChoiceAnswerValue
├── MultipleChoiceAnswerValue
├── RatingAnswerValue
└── LocationAnswerValue (NEW v1.5.0)
```

**Pattern**:
```csharp
// Base class with polymorphic JSON support
[JsonDerivedType(typeof(TextAnswerValue), typeDiscriminator: "Text")]
[JsonDerivedType(typeof(SingleChoiceAnswerValue), typeDiscriminator: "SingleChoice")]
[JsonDerivedType(typeof(MultipleChoiceAnswerValue), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(RatingAnswerValue), typeDiscriminator: "Rating")]
[JsonDerivedType(typeof(LocationAnswerValue), typeDiscriminator: "Location")]
public abstract class AnswerValue : IEquatable<AnswerValue>
{
    public abstract QuestionType QuestionType { get; }
    public abstract string DisplayValue { get; }
    public abstract string ToJson();
    public abstract bool IsValidFor(Question question);
}

// Concrete implementation
public sealed class TextAnswerValue : AnswerValue
{
    public const int MaxLength = 5000;

    public string Text { get; private set; }  // Immutable

    private TextAnswerValue(string text) => Text = text;

    // Factory method with validation
    public static TextAnswerValue Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidAnswerFormatException(0, QuestionType.Text, "Text answer cannot be empty");

        if (text.Length > MaxLength)
            throw new InvalidAnswerFormatException(0, QuestionType.Text,
                $"Text answer cannot exceed {MaxLength} characters");

        return new TextAnswerValue(text.Trim());
    }

    public override QuestionType QuestionType => QuestionType.Text;
    public override string DisplayValue => Text;
    public override string ToJson() => JsonSerializer.Serialize(new { text = Text });
    public override bool IsValidFor(Question question) =>
        question.QuestionType == QuestionType.Text;
}
```

**Usage in Answer Entity**:
```csharp
public class Answer
{
    public AnswerValue? Value { get; private set; }  // New value object property
    public string? AnswerText { get; private set; }  // Legacy for backward compatibility
    public string? AnswerJson { get; private set; }  // Legacy for backward compatibility

    // Create answer with type-safe value object
    public static Answer CreateWithValue(
        int responseId,
        int questionId,
        AnswerValue value,
        NextQuestionDeterminant? next = null)
    {
        // ...validation...
        return new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            Value = value,
            // Also set legacy properties for backward compatibility
            AnswerText = value is TextAnswerValue textValue ? textValue.Text : null,
            AnswerJson = value is not TextAnswerValue ? value.ToJson() : null,
            Next = next ?? NextQuestionDeterminant.End()
        };
    }
}
```

**AnswerValueFactory for Parsing**:
```csharp
public static class AnswerValueFactory
{
    // Strict parsing (throws on error)
    public static AnswerValue Parse(string json, QuestionType questionType);

    // Lenient parsing (returns null on error)
    public static AnswerValue? TryParse(string? json, QuestionType questionType);

    // Create from user input
    public static AnswerValue CreateFromInput(
        QuestionType questionType,
        string? textAnswer = null,
        IEnumerable<string>? selectedOptions = null,
        int? ratingValue = null,
        Question? question = null);

    // Parse with type detection from JSON discriminator
    public static AnswerValue ParseWithTypeDiscriminator(string json);

    // Convert from legacy storage format
    public static AnswerValue? ConvertFromLegacy(
        string? answerText,
        string? answerJson,
        QuestionType questionType);
}

// Usage
var answer = AnswerValueFactory.CreateFromInput(
    QuestionType.SingleChoice,
    selectedOptions: new[] { "Option A" },
    question: question);

var parsed = AnswerValueFactory.Parse(jsonFromDb, QuestionType.Rating);
```

**Benefits**:
- **Type safety**: Compiler prevents wrong answer types for questions
- **Validation**: Each value object validates its own data
- **Polymorphism**: Serialize/deserialize with automatic type detection
- **No string parsing**: Strongly-typed access to answer content
- **Value semantics**: Equality based on content, not reference
- **Backward compatible**: Legacy AnswerText/AnswerJson still supported

---

## Domain Entities

### Entity Relationship Diagram

```
┌─────────────────┐
│      User       │
│  (BaseEntity)   │
├─────────────────┤
│ TelegramId (UQ) │ 1
│ Username        │ │
│ FirstName       │ │ creates
│ LastName        │ │
│ LastLoginAt     │ │
└─────────────────┘ │
                    │
                    ↓ *
           ┌─────────────────┐
           │     Survey      │
           │  (BaseEntity)   │
           ├─────────────────┤
           │ Title           │ 1                1
           │ Description     │ │                │
           │ Code (UQ)       │ │ contains       │ receives
           │ CreatorId (FK)  │ │                │
           │ IsActive        │ │                │
           │ AllowMultiple   │ │                │
           │ ShowResults     │ │                │
           └─────────────────┘ │                │
                               ↓ *              ↓ *
                    ┌─────────────────┐  ┌─────────────────┐
                    │    Question     │  │    Response     │
                    │  (BaseEntity)   │  │  (Custom PK)    │
                    ├─────────────────┤  ├─────────────────┤
                    │ SurveyId (FK)   │  │ SurveyId (FK)   │ 1
                    │ QuestionText    │  │ RespondentId    │ │
                    │ QuestionType    │  │ IsComplete      │ │
                    │ OrderIndex      │  │ StartedAt       │ │ contains
                    │ IsRequired      │  │ SubmittedAt     │ │
                    │ OptionsJson     │  │ VisitedQIds[]   │ │
                    │ MediaContent    │  └─────────────────┘ │
                    │ DefaultNextId   │                      │
                    │ SupportsBranch  │                      ↓ *
                    └─────────────────┘           ┌─────────────────┐
                               │ 1                │     Answer      │
                               │ has options      │  (Custom PK)    │
                               ↓ *                ├─────────────────┤
                    ┌─────────────────┐           │ ResponseId (FK) │
                    │ QuestionOption  │           │ QuestionId (FK) │
                    │  (BaseEntity)   │ *         │ AnswerText      │
                    ├─────────────────┤ │         │ AnswerJson      │
                    │ QuestionId (FK) │ │         │ NextStep (VO)   │
                    │ Text            │ │ answers │ CreatedAt       │
                    │ OrderIndex      │ │         └─────────────────┘
                    │ NextStepDet(VO) │ │
                    └─────────────────┘ ↓

Legend:
- (BaseEntity): Inherits from BaseEntity (Id, CreatedAt, UpdatedAt)
- (Custom PK): Does NOT inherit BaseEntity, has custom Id
- (UQ): Unique constraint
- (FK): Foreign key
- (VO): Value object
- 1 : * = One-to-many relationship
```

**Cascade Delete Behavior**:

| Parent Entity | Child Entity | On Delete Behavior | Reason |
|---------------|--------------|-------------------|---------|
| User | Survey | **Restrict** | Prevent accidental deletion of users with surveys |
| Survey | Question | **Cascade** | Questions belong to survey, no orphans |
| Survey | Response | **Cascade** | Responses belong to survey, delete with survey |
| Survey | QuestionOption | **Cascade** (via Question) | Options belong to questions |
| Question | Answer | **Cascade** | Answers reference questions, delete with question |
| Question | QuestionOption | **Cascade** | Options belong to question only |
| Response | Answer | **Cascade** | Answers belong to response, delete together |

**Navigation Property Patterns**:
- **One-to-Many**: `ICollection<TChild>` on parent, `TParent` on child
- **Foreign Keys**: Explicit `int ForeignKeyId` properties for clarity
- **Required vs Optional**: Use `?` for nullable navigations (e.g., `Question? DefaultNextQuestion`)

### Entity Inheritance Hierarchy

```
BaseEntity (abstract)
├── User
├── Survey
├── Question
└── QuestionOption

Custom Primary Keys (NO inheritance)
├── Response
└── Answer
```

**Why Response and Answer don't inherit BaseEntity?**
- Custom timestamp requirements (StartedAt/SubmittedAt vs CreatedAt/UpdatedAt)
- More flexibility in primary key naming
- Distinct lifecycle management

### BaseEntity

**Purpose**: Common properties for User, Survey, Question

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }                    // Auto-increment PK
    public DateTime CreatedAt { get; set; }        // Auto-set on create
    public DateTime UpdatedAt { get; set; }        // Auto-set on modify
}
```

**Note**: Timestamps managed by DbContext.SaveChangesAsync override (Infrastructure layer)

### User

```csharp
public class User : BaseEntity
{
    public long TelegramId { get; set; }           // Unique, indexed
    public string? Username { get; set; }          // Nullable (not all Telegram users have one)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Survey> Surveys { get; set; }  // Created surveys
}
```

**Key Point**: `TelegramId` is external identifier from Telegram API, `Id` is internal DB PK.

### Survey

```csharp
public class Survey : BaseEntity
{
    public string Title { get; set; }               // Required, 3-500 chars
    public string? Description { get; set; }        // Optional
    public string? Code { get; set; }               // 6-char alphanumeric (A3X9K2), unique
    public int CreatorId { get; set; }              // FK to User
    public bool IsActive { get; set; }              // Accepting responses?
    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }

    public User Creator { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<Response> Responses { get; set; }
}
```

**Business Rules**:
- Must have ≥1 question to activate
- Cannot modify if active with responses (must deactivate first)
- Soft delete if has responses, hard delete otherwise
- Code auto-generated using `SurveyCodeGenerator`

### Question

```csharp
public class Question : BaseEntity
{
    public int SurveyId { get; set; }
    public string QuestionText { get; set; }
    public QuestionType QuestionType { get; set; }  // Text, SingleChoice, MultipleChoice, Rating
    public int OrderIndex { get; set; }             // 0-based, sequential
    public bool IsRequired { get; set; }
    public string? OptionsJson { get; set; }        // JSONB: ["Option1", "Option2"] (legacy)
    public string? MediaContent { get; set; }       // JSONB: Multimedia metadata (NEW in v1.3.0)

    // NEW: Conditional Flow (v1.4.0)
    public int? DefaultNextQuestionId { get; set; } // For non-branching: maps all answers to this question
    [NotMapped]
    public bool SupportsBranching =>
        Type == QuestionType.SingleChoice || Type == QuestionType.Rating;

    public Survey Survey { get; set; }
    public ICollection<Answer> Answers { get; set; }
    public ICollection<QuestionOption> Options { get; set; }  // For choice questions (NEW v1.4.0) - use NextQuestionDeterminant
    public Question? DefaultNextQuestion { get; set; }  // Navigation to next question
}

public enum QuestionType { Text = 0, SingleChoice = 1, MultipleChoice = 2, Rating = 3, Location = 4 }
```

**Business Rules - Conditional Flow** (NEW in v1.4.0):
- **SupportsBranching = true** (SingleChoice, Rating): Each option can point to different next question via NextQuestionDeterminant
- **SupportsBranching = false** (Text, MultipleChoice): All answers map to DefaultNextQuestionId
- **DefaultNextQuestionId = null**: Survey ends after this question
- Survey must be **acyclic** (no infinite loops) to activate
- **Use NextQuestionDeterminant**: Option.NextQuestionDeterminant encapsulates next step decision (replaces magic 0)

**Options Format** (choice questions - legacy):
```json
["Option 1", "Option 2", "Option 3"]
```

**QuestionOption Format** (NEW - structured options with flow):
```csharp
public class QuestionOption : BaseEntity
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public int OrderIndex { get; set; }
    public NextQuestionDeterminant NextQuestionDeterminant { get; set; }  // Where this option leads (NEW v1.4.0, replaces magic values)

    public Question Question { get; set; }
}
```

**NextQuestionDeterminant Usage**:
- **Type**: Value object (not nullable integer)
- **Factory**: Use `NextQuestionDeterminant.ToQuestion(id)` or `NextQuestionDeterminant.End()`
- **Enforced Invariants**: No invalid states like ID=0
- **JSON Mapping**: Serializes to `{ "type": "GoToQuestion", "nextQuestionId": 5 }` or `{ "type": "EndSurvey", "nextQuestionId": null }`

**MediaContent Format** (NEW - multimedia questions):
```json
{
  "id": "unique-media-id",
  "type": "image",
  "filePath": "/media/images/abc123.jpg",
  "fileSize": 1048576,
  "mimeType": "image/jpeg",
  "thumbnailPath": "/media/thumbnails/abc123_thumb.jpg"
}
```

**Supported Media Types**:
- **Images**: jpg, png, gif, webp, bmp, tiff, svg (max 10 MB)
- **Videos**: mp4, webm, mov, avi, mkv, flv, wmv (max 50 MB)
- **Audio**: mp3, wav, ogg, m4a, flac, aac (max 20 MB)
- **Documents**: pdf, doc, docx, xls, xlsx, ppt, pptx, txt, rtf, csv (max 25 MB)
- **Archives**: zip, rar, 7z, tar, gz, bz2 (max 100 MB)

### Response

**Note**: Does NOT inherit from BaseEntity (custom timestamps)

```csharp
public class Response
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public long RespondentTelegramId { get; set; }  // NOT a FK (allows anonymous responses)
    public bool IsComplete { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // NEW: Conditional Flow (v1.4.0) - Runtime cycle prevention
    public List<int> VisitedQuestionIds { get; set; } = new();  // Track answered questions

    public Survey Survey { get; set; }
    public ICollection<Answer> Answers { get; set; }
}

public partial class Response
{
    // NEW: Helper methods for visited question tracking (v1.4.0)
    public bool HasVisitedQuestion(int questionId) =>
        VisitedQuestionIds.Contains(questionId);

    public void RecordVisitedQuestion(int questionId)
    {
        if (!VisitedQuestionIds.Contains(questionId))
            VisitedQuestionIds.Add(questionId);
    }
}
```

**Lifecycle**:
1. Started: `StartedAt` set, `IsComplete = false`, `VisitedQuestionIds` empty
2. During: `VisitedQuestionIds` updated as questions answered (prevents re-answering)
3. Completed: `SubmittedAt` set, `IsComplete = true`

**VisitedQuestionIds** (NEW in v1.4.0):
- **Purpose**: Prevent re-answering same question in one response (runtime cycle prevention)
- **Storage**: PostgreSQL JSONB array (efficient querying via GIN index)
- **Use Case**: Protects against infinite loops when survey has cycles in design (should not happen with validation, but this provides safety net)

### Answer

**Note**: Does NOT inherit from BaseEntity

```csharp
public class Answer
{
    public int Id { get; set; }
    public int ResponseId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }        // For Text questions
    public string? AnswerJson { get; set; }        // JSONB for structured data
    public DateTime CreatedAt { get; set; }

    // UPDATED in v1.4.2: Complete migration to value object (no more magic values!)
    public NextQuestionDeterminant Next { get; set; } = NextQuestionDeterminant.End();

    public Response Response { get; set; }
    public Question Question { get; set; }
}
```

**Next** (UPDATED in v1.4.2 - Complete Migration):
- **Type**: NextQuestionDeterminant (value object, not a magic integer)
- **Purpose**: Encapsulates the decision of where to navigate after answering
- **Migration**: v1.4.2 completed migration from `int NextQuestionId` to value object
- **Determined by**: Question type and answer
  - **Branching questions** (SingleChoice, Rating): Selected option determines next step
  - **Non-branching** (Text, MultipleChoice): Question's DefaultNextQuestionId used
- **Set by**: ResponseService.DetermineNextStepAsync during answer recording
- **Used by**: Frontend/Bot to determine next question display
- **No Magic Values**: Compiler enforces type safety, no 0 checks needed

**Answer Formats by Question Type**:

**Text**: `AnswerText = "User's answer"`, `AnswerJson = null`

**SingleChoice**:
```json
{"selectedOption": "Option 2"}
```

**MultipleChoice**:
```json
{"selectedOptions": ["Option 1", "Option 3"]}
```

**Rating**:
```json
{"rating": 4}
```

**Location** (NEW v1.5.0):
```json
{"latitude": 40.7128, "longitude": -74.0060}
```

---

## Repository Interfaces

### IRepository<T> (Generic Base)

```csharp
Task<T?> GetByIdAsync(int id);
Task<IEnumerable<T>> GetAllAsync();
Task<T> CreateAsync(T entity);
Task<T> UpdateAsync(T entity);
Task<bool> DeleteAsync(int id);
Task<bool> ExistsAsync(int id);
Task<int> CountAsync();
```

### ISurveyRepository

**Key Methods**:
- `GetByIdWithQuestionsAsync(int id)` - Include questions ordered by OrderIndex
- `GetByIdWithDetailsAsync(int id)` - Include questions, responses, answers
- `GetByCreatorIdAsync(int creatorId)` - User's surveys
- `GetActiveSurveysAsync()` - Only active surveys
- `GetByCodeAsync(string code)` - Find by unique code
- `CodeExistsAsync(string code)` - Check code uniqueness

### Other Repositories

- **IQuestionRepository**: GetBySurveyIdAsync, ReorderQuestionsAsync
- **IResponseRepository**: GetIncompleteResponseAsync, GetCompletedCountAsync
- **IUserRepository**: GetByTelegramIdAsync, CreateOrUpdateAsync (upsert)
- **IAnswerRepository**: GetByResponseIdAsync, GetByQuestionIdAsync
- **IMediaStorageService**: SaveMediaAsync, DeleteMediaAsync, GetMediaAsync (NEW in v1.3.0)
- **IMediaValidationService**: ValidateMediaAsync, ValidateMediaWithAutoDetectionAsync (NEW in v1.3.0)

---

## Service Interfaces

### ISurveyService

**CRUD**:
- `CreateSurveyAsync(userId, dto)` - Create with auto-generated code
- `UpdateSurveyAsync(surveyId, userId, dto)` - With authorization check
- `DeleteSurveyAsync(surveyId, userId)` - Smart delete (soft/hard)
- `GetSurveyByIdAsync(surveyId, userId)` - With ownership check

**Status**:
- `ActivateSurveyAsync(surveyId, userId)` - Validates ≥1 question
- `DeactivateSurveyAsync(surveyId, userId)`

**Analytics**:
- `GetSurveyStatisticsAsync(surveyId, userId)` - Comprehensive stats

**Public Access**:
- `GetSurveyByCodeAsync(code)` - No auth, only active surveys

### ISurveyValidationService (NEW in v1.4.0)

**Purpose**: Validate survey structure for conditional flow feature (cycle detection)

```csharp
public interface ISurveyValidationService
{
    Task<CycleDetectionResult> DetectCycleAsync(int surveyId);
    Task<bool> ValidateSurveyStructureAsync(int surveyId);
    Task<List<int>> FindSurveyEndpointsAsync(int surveyId);
}

public class CycleDetectionResult
{
    public bool HasCycle { get; set; }
    public List<int>? CyclePath { get; set; }  // Path showing the cycle
    public string? ErrorMessage { get; set; }
}
```

**Key Methods**:
- `DetectCycleAsync`: DFS-based cycle detection, O(V+E) complexity
- `ValidateSurveyStructureAsync`: Checks no cycles + has endpoints
- `FindSurveyEndpointsAsync`: Questions that lead to survey completion

### Other Services

- **IQuestionService**: AddQuestionAsync, UpdateQuestionAsync, ReorderQuestionsAsync
- **IResponseService**: StartResponseAsync, SubmitAnswerAsync, CompleteResponseAsync, RecordVisitedQuestionAsync, GetNextQuestionAsync
- **IUserService**: GetOrCreateUserAsync (upsert pattern)
- **IAuthService**: AuthenticateAsync, GenerateJwtToken, ValidateToken
- **IMediaStorageService**: SaveMediaAsync, DeleteMediaAsync, GetMediaAsync (NEW in v1.3.0)
- **IMediaValidationService**: ValidateMediaAsync, ValidateMediaWithAutoDetectionAsync (NEW in v1.3.0)
- **ISurveyValidationService**: DetectCycleAsync, ValidateSurveyStructureAsync, FindSurveyEndpointsAsync (NEW in v1.4.0)

---

## Data Transfer Objects (DTOs)

### Naming Conventions

- `CreateXxxDto` - POST requests (creation)
- `UpdateXxxDto` - PUT requests (updates)
- `XxxDto` - Responses (full details)
- `XxxListDto` - List responses (summary)

### Key DTOs

**SurveyDto**:
```csharp
public class SurveyDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public List<QuestionDto> Questions { get; set; }
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**PagedResultDto<T>** (Pagination wrapper):
```csharp
public class PagedResultDto<T>
{
    public List<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; }
}
```

**Statistics DTOs**:
- `SurveyStatisticsDto` - Total/completed responses, completion rate, avg time
- `QuestionStatisticsDto` - Response rate, choice distribution, rating averages

**Media DTOs** (NEW in v1.3.0):
- `MediaItemDto` - Media file metadata (id, type, path, size, mimeType, thumbnail)
- `MediaContentDto` - Simplified media content for embedding in questions
- `MediaValidationResult` - Validation result with errors and detected type

**Conditional Flow DTOs** (NEW in v1.4.0):

**ConditionalFlowDto** - Get current flow configuration:
```csharp
public class ConditionalFlowDto
{
    public int QuestionId { get; set; }
    public bool SupportsBranching { get; set; }
    public int? DefaultNextQuestionId { get; set; }
    public List<OptionFlowDto> OptionFlows { get; set; } = new();
}
```

**OptionFlowDto** - Individual option with its next question:
```csharp
public class OptionFlowDto
{
    public int OptionId { get; set; }
    public string OptionText { get; set; }
    public int NextQuestionId { get; set; }  // 0 = end of survey
    [JsonIgnore]
    public bool IsEndOfSurvey => NextQuestionId == SurveyConstants.EndOfSurveyMarker;
}
```

**UpdateQuestionFlowDto** - Update flow configuration:
```csharp
public class UpdateQuestionFlowDto
{
    public int? DefaultNextQuestionId { get; set; }  // For non-branching
    public Dictionary<int, int> OptionNextQuestions { get; set; } = new();  // For branching
}
```

---

## Domain Exceptions

All inherit from `System.Exception`:

- `SurveyNotFoundException` - Survey doesn't exist
- `QuestionNotFoundException` - Question doesn't exist
- `SurveyValidationException` - Validation fails (e.g., title too short, no questions)
- `SurveyOperationException` - Operation not allowed (e.g., modify active survey with responses)
- `InvalidAnswerFormatException` - Answer JSON doesn't match question type
- `DuplicateResponseException` - Multiple responses when not allowed
- `UnauthorizedAccessException` - User doesn't own resource
- `MediaValidationException` - Media file validation fails (NEW in v1.3.0)
- `MediaStorageException` - Media storage operation fails (NEW in v1.3.0)
- `SurveyCycleException` - Survey has cycle in question flow (NEW in v1.4.0)

**Example**:
```csharp
public class SurveyNotFoundException : Exception
{
    public int SurveyId { get; }

    public SurveyNotFoundException(int surveyId)
        : base($"Survey with ID {surveyId} was not found.")
    {
        SurveyId = surveyId;
    }
}

// NEW: SurveyCycleException (v1.4.0)
public class SurveyCycleException : Exception
{
    public List<int> CyclePath { get; }

    public SurveyCycleException(List<int> cyclePath, string message)
        : base(message)
    {
        CyclePath = cyclePath;
    }
}
```

**SurveyCycleException** (NEW in v1.4.0):
- **Purpose**: Thrown when survey flow contains cycle (invalid state)
- **CyclePath**: Sequence of question IDs forming the cycle (useful for debugging)
- **Example**: CyclePath = [1, 2, 3, 1] indicates Q1→Q2→Q3→Q1 cycle
- **When Thrown**: During survey activation validation, before user can distribute survey

---

## Recent Architectural Changes

### Version 1.4.0 - Conditional Question Flow (2025-11-21 to 2025-11-23)

**Feature**: Conditional branching logic allowing different survey paths based on user answers.

**Entity Changes**:

1. **Question Entity** (`C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\Question.cs`):
   - **Added**: `DefaultNextQuestionId` (nullable int) - Default next question for non-branching types
   - **Added**: `SupportsBranching` (computed property) - True for SingleChoice and Rating questions
   - **Added**: `DefaultNextQuestion` (navigation property) - Self-referential navigation
   - **Impact**: Non-branching questions (Text, MultipleChoice) use single default next question

2. **QuestionOption Entity** (NEW):
   - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\QuestionOption.cs`
   - **Purpose**: Structured options with individual flow logic (replaces OptionsJson array)
   - **Properties**: QuestionId, Text, OrderIndex, NextQuestionDeterminant
   - **Relationship**: Question (1) → QuestionOption (*)
   - **Migration**: Existing surveys with OptionsJson still supported (legacy format)

3. **Response Entity** (`C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\Response.cs`):
   - **Added**: `VisitedQuestionIds` (List<int>) - Track answered questions in this response
   - **Purpose**: Runtime cycle prevention (don't re-show answered questions)
   - **Storage**: PostgreSQL JSONB array with GIN index for efficient querying
   - **Helper Methods**: `HasVisitedQuestion(id)`, `RecordVisitedQuestion(id)`

4. **Answer Entity** (`C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\Answer.cs`):
   - **COMPLETED v1.4.2**: Full migration from `NextQuestionId` (int with magic value 0) to `Next` (NextQuestionDeterminant)
   - **Before**: `NextQuestionId = 0` meant "end survey" (magic value, inconsistent with Question/QuestionOption)
   - **After**: `Next = NextQuestionDeterminant.End()` (explicit, type-safe, consistent)
   - **Database**: Owned type with columns `next_step_type` (TEXT) and `next_step_question_id` (INT)
   - **Migration**: 20251126180649_AnswerNextStepValueObject with data transformation
   - **Invariants**: CHECK constraint enforces value object rules at database level

**New Value Objects**:

5. **NextQuestionDeterminant** (NEW):
   - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\ValueObjects\NextQuestionDeterminant.cs`
   - **Pattern**: Domain-Driven Design value object with factory methods
   - **Purpose**: Encapsulate next step decision (go to question X or end survey)
   - **Factory Methods**: `ToQuestion(id)`, `End()`
   - **Invariants**: GoToQuestion requires ID > 0, EndSurvey has null ID
   - **Replaces**: Magic value 0 for "end of survey"

**New Enums**:

6. **NextStepType** (NEW):
   - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Enums\NextStepType.cs`
   - **Values**: `GoToQuestion = 0`, `EndSurvey = 1`
   - **Usage**: Property in NextQuestionDeterminant value object

**New Constants**:

7. **SurveyConstants** (NEW):
   - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Constants\SurveyConstants.cs`
   - **Purpose**: Central repository for domain constants
   - **Key Constants**: `EndOfSurveyMarker = 0`, `MaxQuestionsPerSurvey = 100`, `MaxOptionsPerQuestion = 50`
   - **Helper Methods**: `IsEndOfSurvey(int nextQuestionId)`

**New Services**:

8. **ISurveyValidationService** (NEW):
   - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Interfaces\ISurveyValidationService.cs`
   - **Purpose**: Validate survey structure (cycle detection, endpoint verification)
   - **Methods**:
     - `DetectCycleAsync(surveyId)` - DFS-based cycle detection
     - `ValidateSurveyStructureAsync(surveyId)` - Comprehensive validation (no cycles + has endpoints)
     - `FindSurveyEndpointsAsync(surveyId)` - Find questions that end the survey
   - **Used By**: Survey activation logic (prevents activating invalid surveys)

**New Exceptions**:

9. **SurveyCycleException** (NEW):
   - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Exceptions\SurveyCycleException.cs`
   - **Purpose**: Thrown when survey contains cycle (Question A → B → C → A)
   - **Properties**: `CyclePath` (List<int>) - Sequence of question IDs forming the cycle
   - **Example**: CyclePath = [1, 2, 3, 1] indicates Q1→Q2→Q3→Q1 cycle
   - **Thrown By**: SurveyValidationService during activation validation

**New DTOs**:

10. **ConditionalFlowDto** (NEW):
    - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\DTOs\ConditionalFlowDto.cs`
    - **Purpose**: Retrieve current flow configuration for a question
    - **Properties**: QuestionId, SupportsBranching, DefaultNextQuestionId, OptionFlows

11. **NextQuestionDeterminantDto** (NEW):
    - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\DTOs\NextQuestionDeterminantDto.cs`
    - **Purpose**: DTO representation of NextQuestionDeterminant value object
    - **Used In**: API responses for answer submission

12. **UpdateQuestionFlowDto** (NEW):
    - **File**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\DTOs\UpdateQuestionFlowDto.cs`
    - **Purpose**: Update flow configuration for a question
    - **Properties**: DefaultNextQuestionId (for non-branching), OptionNextQuestions (for branching)

**Database Migrations**:

- **Migration 1**: `20251121172521_AddConditionalQuestionFlow` - Initial conditional flow schema
- **Migration 2**: `20251123010631_RemoveNextQuestionFKConstraints` - Remove FK constraints (allow forward references)
- **Migration 3**: `20251123131359_CleanSlateNextQuestionDeterminant` - Rebuild with value object mapping

**Design Decisions**:

1. **No FK Constraint on NextQuestionId**: Allows referencing questions created later in survey builder
2. **Value Object Pattern**: Replaces magic values with explicit types (DDD best practice)
3. **Backwards Compatibility**: Legacy OptionsJson still supported for existing surveys
4. **Runtime Cycle Prevention**: VisitedQuestionIds provides safety net even if validation misses cycle
5. **Validation at Activation**: Survey cannot be activated (distributed) if it has cycles

**Breaking Changes**:
- Answer entity: `NextQuestionId` property removed, replaced with `NextStep` value object
- Infrastructure layer must use `OwnedType` mapping for NextQuestionDeterminant

---

### Version 1.3.0 - Multimedia Support (2025-11-18 to 2025-11-20)

**Feature**: Support for images, videos, audio, documents in survey questions.

**Entity Changes**:

1. **Question Entity**:
   - **Added**: `MediaContent` (string, nullable) - JSONB metadata for attached media
   - **Format**: `{ "id": "...", "type": "image", "filePath": "...", "fileSize": 1048576, ... }`
   - **Supported Types**: Image, Video, Audio, Document, Archive

**New Services**:

2. **IMediaStorageService** (NEW):
   - **Purpose**: Save, retrieve, delete media files from filesystem
   - **Methods**: `SaveMediaAsync`, `GetMediaAsync`, `DeleteMediaAsync`

3. **IMediaValidationService** (NEW):
   - **Purpose**: Validate media files (type, size, format)
   - **Methods**: `ValidateMediaAsync`, `ValidateMediaWithAutoDetectionAsync`
   - **Validation**: File size limits, MIME type detection, extension validation

**New DTOs**:

4. **MediaItemDto**: Media file metadata with thumbnail support
5. **MediaContentDto**: Simplified media content for embedding in questions
6. **MediaValidationResult**: Validation result with errors and detected type

**New Exceptions**:

7. **MediaValidationException**: Media file validation fails (invalid type, too large, corrupt)
8. **MediaStorageException**: Media storage operation fails (disk full, permission denied)

**Storage Strategy**:
- Files stored in `wwwroot/media/{type}/{filename}`
- Metadata stored in Question.MediaContent (JSONB)
- Thumbnails auto-generated for images/videos

---

## Value Objects

### NextQuestionDeterminant (NEW in v1.4.0)

**Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\ValueObjects\NextQuestionDeterminant.cs`

**Purpose**: Encapsulates the decision of where to navigate after answering a question. Replaces magic value 0 with explicit type system.

**Type**: Immutable value object with value semantics

```csharp
public sealed class NextQuestionDeterminant : IEquatable<NextQuestionDeterminant>
{
    public NextStepType Type { get; private set; }      // GoToQuestion or EndSurvey
    public int? NextQuestionId { get; private set; }    // Only when Type = GoToQuestion
}
```

**Key Features**:
- **Immutable**: No public setters, values assigned via factory methods
- **Value Semantics**: Equality based on Type and NextQuestionId, not reference
- **Invariants Enforced**: GoToQuestion requires valid ID > 0, EndSurvey has null ID
- **No Magic Values**: Replaces `int nextQuestionId` where 0 meant "end survey"

**Factory Methods** (Recommended):

```csharp
// Navigate to question with ID 5
var next = NextQuestionDeterminant.ToQuestion(5);
// Returns: NextQuestionDeterminant { Type: GoToQuestion, NextQuestionId: 5 }

// End the survey
var end = NextQuestionDeterminant.End();
// Returns: NextQuestionDeterminant { Type: EndSurvey, NextQuestionId: null }
```

**Usage Example**:

```csharp
// In ResponseService - determining next step after answer
if (question.SupportsBranching)
{
    // Get option based on selected answer
    var option = question.Options.FirstOrDefault(o => o.Text == selectedOption);
    answer.NextStep = NextQuestionDeterminant.ToQuestion(option.NextQuestionId);
}
else
{
    // Non-branching: use question's default
    if (question.DefaultNextQuestionId.HasValue)
    {
        answer.NextStep = NextQuestionDeterminant.ToQuestion(question.DefaultNextQuestionId.Value);
    }
    else
    {
        answer.NextStep = NextQuestionDeterminant.End();  // Survey ends
    }
}
```

**When to Use NextQuestionDeterminant**:
- Storing next step decisions in Answer entity
- Passing next step info in API responses
- Determining flow in Frontend/Bot question loop

**Advantages over Magic Values**:
- **Type Safety**: Compiler prevents invalid states
- **Explicit Intent**: Code clearly shows intent (end survey vs. go to question)
- **No Invalid States**: Can't accidentally set ID = 0 for GoToQuestion
- **Self-Documenting**: No need for comments explaining magic 0

**JSON Serialization**:

```json
{
  "type": "GoToQuestion",
  "nextQuestionId": 5
}
```

or

```json
{
  "type": "EndSurvey",
  "nextQuestionId": null
}
```

### NextStepType Enum

**Purpose**: Defines the type of action after answering a question

```csharp
public enum NextStepType
{
    GoToQuestion = 0,   // Navigate to question with NextQuestionId
    EndSurvey = 1       // End the survey (no more questions)
}
```

**Values**:
- **GoToQuestion (0)**: Requires valid NextQuestionId > 0
- **EndSurvey (1)**: NextQuestionId must be null

---

## Utilities

### SurveyConstants (NEW in v1.4.0)

**Purpose**: Central repository for domain constants, especially conditional flow markers

```csharp
public static class SurveyConstants
{
    // Conditional flow markers
    public const int EndOfSurveyMarker = 0;  // NextQuestionId = 0 means survey ends

    public static bool IsEndOfSurvey(int nextQuestionId) =>
        nextQuestionId == EndOfSurveyMarker;

    // Limits
    public const int MaxQuestionsPerSurvey = 100;
    public const int MaxOptionsPerQuestion = 50;

    // Validation rules
    public const int SurveyCodeLength = 6;
    public const int SurveyTitleMin = 3;
    public const int SurveyTitleMax = 500;
    public const int RatingMin = 1;
    public const int RatingMax = 5;
}
```

**Key Constants**:
- **EndOfSurveyMarker (0)**: Special value indicating survey completion (not a valid question ID)
- **IsEndOfSurvey()**: Helper to check if answer leads to survey end
- **MaxQuestionsPerSurvey**: Prevents excessively large surveys
- **MaxOptionsPerQuestion**: Limits options per choice question

### SurveyCodeGenerator

**Purpose**: Generate unique 6-character alphanumeric codes for easy survey sharing

**Format**: Base36 (A-Z, 0-9), uppercase, 2.17 billion combinations

**Key Methods**:

```csharp
// Generate random code
public static string GenerateCode()
// Returns: "A3X9K2"

// Generate unique code with collision detection
public static async Task<string> GenerateUniqueCodeAsync(
    Func<string, Task<bool>> codeExistsAsync,
    int maxAttempts = 10)

// Validate code format
public static bool IsValidCode(string? code)
```

**Usage**:
```csharp
var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
    _surveyRepository.CodeExistsAsync);
```

**Security**: Uses `RandomNumberGenerator` (cryptographically secure)

---

## Configuration Models

### JwtSettings

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; }          // Min 32 chars for HMAC-SHA256
    public string Issuer { get; set; }             // "SurveyBot.API"
    public string Audience { get; set; }           // "SurveyBot.Client"
    public int TokenLifetimeHours { get; set; }    // Default: 24
    public int RefreshTokenLifetimeDays { get; set; }  // Default: 7
}
```

**appsettings.json**:
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "TokenLifetimeHours": 24
  }
}
```

---

## Design Patterns

### 1. Clean Architecture (Onion Architecture)

**Core Principle**: Core is the **innermost layer**. All dependencies point inward.

```
┌──────────────────────────────────────┐
│  API Layer (Outer)                   │
│  - Controllers, Middleware           │
│  - Depends on: Core, Infrastructure  │
└────────────────┬─────────────────────┘
                 │ depends on
┌────────────────▼─────────────────────┐
│  Bot Layer (Outer)                   │
│  - Telegram handlers                 │
│  - Depends on: Core, Infrastructure  │
└────────────────┬─────────────────────┘
                 │ depends on
┌────────────────▼─────────────────────┐
│  Infrastructure Layer (Middle)       │
│  - DbContext, Repositories           │
│  - Depends on: Core only             │
└────────────────┬─────────────────────┘
                 │ depends on
┌────────────────▼─────────────────────┐
│  Core Layer (Inner) ← YOU ARE HERE   │
│  - Entities, Interfaces, DTOs        │
│  - Depends on: NOTHING               │
└──────────────────────────────────────┘
```

**Benefits**:
- **Testability**: Core testable without external dependencies (no mocking needed)
- **Flexibility**: Infrastructure can be swapped (PostgreSQL → MongoDB, Telegram → WhatsApp)
- **Stability**: Business logic independent of frameworks (EF Core, ASP.NET)
- **Domain Focus**: Core reflects business rules, not technical constraints

**Rules Enforced**:
1. Core defines **contracts** (interfaces), outer layers implement
2. Core has **zero** external dependencies (no NuGet packages)
3. Outer layers reference Core, never the reverse
4. Business logic lives in Core (entities) or Infrastructure (services implementing Core interfaces)

---

### 2. Repository Pattern

**Purpose**: Abstract data access logic from business logic. Core defines interfaces, Infrastructure implements.

**Pattern Structure**:

```csharp
// CORE: Define the contract
// File: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Interfaces\ISurveyRepository.cs
public interface ISurveyRepository : IRepository<Survey>
{
    Task<Survey?> GetByIdWithQuestionsAsync(int id);
    Task<Survey?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code);
}

// INFRASTRUCTURE: Implement with EF Core
// File: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Repositories\SurveyRepository.cs
public class SurveyRepository : Repository<Survey>, ISurveyRepository
{
    public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
    {
        return await _context.Surveys
            .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
```

**Benefits**:
- Business logic doesn't know about EF Core, SQL, or database specifics
- Can swap data source (PostgreSQL → MongoDB) without changing Core
- Easy to mock repositories in unit tests
- Multiple implementations possible (in-memory for testing, SQL for production)

**Generic Base Pattern**:

```csharp
// Core: Generic repository for common operations
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}

// Specific repositories extend generic with entity-specific methods
public interface ISurveyRepository : IRepository<Survey>
{
    Task<Survey?> GetByCodeAsync(string code);  // Survey-specific
}
```

---

### 3. DTO Pattern (Data Transfer Object)

**Purpose**: Decouple API contract from database schema. Prevent over-posting, control serialization, add computed properties.

**Pattern Structure**:

```csharp
// ENTITY (Core): Database model with navigation properties
// File: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Entities\Survey.cs
public class Survey : BaseEntity
{
    public string Title { get; set; }
    public int CreatorId { get; set; }
    public User Creator { get; set; }                      // Navigation (not JSON-serializable)
    public ICollection<Question> Questions { get; set; }   // Navigation (circular reference risk)
    public ICollection<Response> Responses { get; set; }   // Navigation (too much data)
}

// DTO (Core): API contract with flat structure
// File: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\DTOs\Survey\SurveyDto.cs
public class SurveyDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int CreatorId { get; set; }                     // ID instead of navigation
    public List<QuestionDto> Questions { get; set; }       // DTO instead of entity
    public int TotalResponses { get; set; }                // Computed property
    public DateTime CreatedAt { get; set; }
}
```

**DTO Naming Conventions**:
- `CreateXxxDto` - POST requests (creation, no ID)
- `UpdateXxxDto` - PUT requests (updates, partial properties)
- `XxxDto` - GET responses (full details, includes computed properties)
- `XxxListDto` - List responses (summary, fewer fields for performance)

**Benefits**:
- **Security**: Prevent over-posting attacks (user can't set `IsAdmin = true`)
- **Performance**: Control what data is returned (don't send all Responses in Survey list)
- **Versioning**: Change DTO without changing entity (API v1 vs v2)
- **Computed Properties**: Add `TotalResponses` without modifying entity
- **Serialization Control**: No circular reference issues

**AutoMapper Usage** (in Infrastructure):

```csharp
// Infrastructure layer maps entities to DTOs
var dto = _mapper.Map<SurveyDto>(survey);
```

---

### 4. Value Object Pattern (DDD)

**Purpose**: Model domain concepts that don't have identity, only value equality. Enforce invariants, replace magic values.

**Pattern Structure**:

```csharp
// VALUE OBJECT (Core)
// File: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\ValueObjects\NextQuestionDeterminant.cs
public sealed class NextQuestionDeterminant : IEquatable<NextQuestionDeterminant>
{
    // Immutable properties
    public NextStepType Type { get; private set; }
    public int? NextQuestionId { get; private set; }

    // Private constructor: Force factory method usage
    private NextQuestionDeterminant(NextStepType type, int? nextQuestionId)
    {
        // Enforce invariants
        if (type == NextStepType.GoToQuestion && (!nextQuestionId.HasValue || nextQuestionId <= 0))
            throw new ArgumentException("GoToQuestion requires valid NextQuestionId > 0");

        if (type == NextStepType.EndSurvey && nextQuestionId.HasValue)
            throw new ArgumentException("EndSurvey must have null NextQuestionId");

        Type = type;
        NextQuestionId = nextQuestionId;
    }

    // Factory methods (static): Named constructors with clear intent
    public static NextQuestionDeterminant ToQuestion(int questionId)
        => new(NextStepType.GoToQuestion, questionId);

    public static NextQuestionDeterminant End()
        => new(NextStepType.EndSurvey, null);

    // Value equality (not reference equality)
    public bool Equals(NextQuestionDeterminant? other)
        => other != null && Type == other.Type && NextQuestionId == other.NextQuestionId;

    public override bool Equals(object? obj)
        => Equals(obj as NextQuestionDeterminant);

    public override int GetHashCode()
        => HashCode.Combine(Type, NextQuestionId);
}
```

**Before (Magic Values)**:

```csharp
// Problem: What does 0 mean? End survey? Not set? Error?
answer.NextQuestionId = 0;  // Magic value

if (answer.NextQuestionId == 0)  // Is this intentional or a bug?
    EndSurvey();
```

**After (Value Object)**:

```csharp
// Clear intent: End the survey explicitly
answer.NextStep = NextQuestionDeterminant.End();

if (answer.NextStep.Type == NextStepType.EndSurvey)
    EndSurvey();
```

**Benefits**:
- **No Magic Values**: `NextQuestionDeterminant.End()` vs `0`
- **Type Safety**: Compiler prevents invalid states (`GoToQuestion` with null ID)
- **Self-Documenting**: Code clearly expresses intent
- **Invariants Enforced**: Can't accidentally create invalid combinations
- **Value Semantics**: Two instances with same values are equal

**When to Use Value Objects**:
- Modeling domain concepts without identity (Money, Address, DateRange)
- Replacing primitive types with business meaning (UserId vs int)
- Enforcing complex validation rules (Email, PhoneNumber)
- Eliminating magic values and flags

---

### 5. Factory Method Pattern

**Purpose**: Provide named constructors for value objects and entities with complex initialization logic.

**Pattern Structure**:

```csharp
// ENTITY (Core)
public class Survey : BaseEntity
{
    // Private constructor: Force factory method usage
    private Survey() { }

    // Factory method: Named, validates parameters, generates code
    public static Survey Create(string title, string? description, int creatorId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            throw new SurveyValidationException("Title must be at least 3 characters");

        return new Survey
        {
            Title = title,
            Description = description,
            CreatorId = creatorId,
            Code = SurveyCodeGenerator.GenerateCode(),  // Auto-generate
            IsActive = false,  // Default state
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**Benefits**:
- **Clear Intent**: `Survey.Create()` vs `new Survey()`
- **Validation**: Enforce business rules at construction
- **Encapsulation**: Hide implementation details (code generation)
- **Immutability**: Can make constructors private, force factory usage

---

### 6. Strategy Pattern (Implicit)

**Purpose**: Different behavior based on QuestionType without switch statements.

**Current Implementation** (Conditional Logic):

```csharp
// In ResponseService
if (question.Type == QuestionType.SingleChoice)
{
    // Branching: Get option's NextQuestionDeterminant
    var option = question.Options.FirstOrDefault(o => o.Text == selectedOption);
    nextStep = option.NextQuestionDeterminant;
}
else if (question.Type == QuestionType.Text || question.Type == QuestionType.MultipleChoice)
{
    // Non-branching: Use DefaultNextQuestionId
    nextStep = question.DefaultNextQuestionId.HasValue
        ? NextQuestionDeterminant.ToQuestion(question.DefaultNextQuestionId.Value)
        : NextQuestionDeterminant.End();
}
```

**Potential Refactoring** (Strategy Pattern):

```csharp
// Define strategy interface
public interface IQuestionFlowStrategy
{
    NextQuestionDeterminant DetermineNextStep(Question question, Answer answer);
}

// Branching strategy
public class BranchingFlowStrategy : IQuestionFlowStrategy
{
    public NextQuestionDeterminant DetermineNextStep(Question question, Answer answer)
    {
        var option = question.Options.FirstOrDefault(o => o.Text == answer.AnswerText);
        return option?.NextQuestionDeterminant ?? NextQuestionDeterminant.End();
    }
}

// Non-branching strategy
public class SequentialFlowStrategy : IQuestionFlowStrategy
{
    public NextQuestionDeterminant DetermineNextStep(Question question, Answer answer)
    {
        return question.DefaultNextQuestionId.HasValue
            ? NextQuestionDeterminant.ToQuestion(question.DefaultNextQuestionId.Value)
            : NextQuestionDeterminant.End();
    }
}

// Usage
var strategy = question.SupportsBranching
    ? new BranchingFlowStrategy()
    : new SequentialFlowStrategy();

answer.NextStep = strategy.DetermineNextStep(question, answer);
```

**Benefits**:
- **Open/Closed Principle**: Add new question types without modifying existing code
- **Testability**: Test each strategy independently
- **Clarity**: Each strategy is a separate class with single responsibility

---

### 7. Specification Pattern (Potential)

**Purpose**: Encapsulate business rules for querying and validation.

**Current**: Business rules in repositories and services

**Potential Refactoring**:

```csharp
// Define specification interface
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
    Expression<Func<T, bool>> ToExpression();
}

// Active survey specification
public class ActiveSurveySpecification : ISpecification<Survey>
{
    public bool IsSatisfiedBy(Survey survey) => survey.IsActive;

    public Expression<Func<Survey, bool>> ToExpression()
        => survey => survey.IsActive;
}

// Usage in repository
var spec = new ActiveSurveySpecification();
var activeSurveys = await _context.Surveys.Where(spec.ToExpression()).ToListAsync();
```

**Benefits**:
- Reusable business rules
- Combinable specifications (AND, OR, NOT)
- Testable in isolation

---

### 8. Unit of Work Pattern (Infrastructure)

**Purpose**: Group multiple repository operations into a single transaction.

**Current Implementation**: DbContext acts as Unit of Work

```csharp
// Infrastructure layer
public class UnitOfWork : IUnitOfWork
{
    private readonly SurveyBotDbContext _context;

    public ISurveyRepository Surveys { get; }
    public IQuestionRepository Questions { get; }

    public async Task<int> CommitAsync()
        => await _context.SaveChangesAsync();
}

// Usage
await _unitOfWork.Surveys.CreateAsync(survey);
await _unitOfWork.Questions.CreateAsync(question);
await _unitOfWork.CommitAsync();  // Single transaction
```

---

## Architectural Recommendations

### 1. Migrate Answer.NextQuestionId to Value Object (In Progress)

**Current State**: Answer entity still uses `NextStep` (NextQuestionDeterminant value object) - ✅ **COMPLETED**

**Status**: Successfully implemented in v1.4.0

---

### 2. Consider MediaContent Value Object

**Current**: MediaContent is a string (JSONB serialized)

**Recommendation**: Create MediaContent value object

```csharp
public class MediaContent : IEquatable<MediaContent>
{
    public string Id { get; private set; }
    public MediaType Type { get; private set; }  // Enum: Image, Video, Audio, Document
    public string FilePath { get; private set; }
    public long FileSize { get; private set; }
    public string MimeType { get; private set; }
    public string? ThumbnailPath { get; private set; }

    private MediaContent() { }

    public static MediaContent Create(string id, MediaType type, string filePath, long fileSize, string mimeType)
    {
        // Validation
        if (fileSize <= 0) throw new ArgumentException("File size must be positive");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required");

        return new MediaContent
        {
            Id = id,
            Type = type,
            FilePath = filePath,
            FileSize = fileSize,
            MimeType = mimeType
        };
    }

    public MediaContent WithThumbnail(string thumbnailPath)
    {
        ThumbnailPath = thumbnailPath;
        return this;
    }
}
```

**Benefits**:
- Type safety (can't set invalid file size)
- Encapsulation (validation logic in one place)
- Easier testing (mock MediaContent vs string parsing)

---

### 3. Add Rich Domain Models

**Current**: Entities are anemic (mostly getters/setters)

**Recommendation**: Add behavior to entities

```csharp
public class Survey : BaseEntity
{
    // Current: IsActive property
    public bool IsActive { get; set; }

    // Proposed: Behavior methods
    public void Activate()
    {
        if (Questions.Count == 0)
            throw new SurveyValidationException("Cannot activate survey without questions");

        if (HasCycle())
            throw new SurveyCycleException("Cannot activate survey with cycles");

        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private bool HasCycle()
    {
        // Cycle detection logic (or delegate to validation service)
    }
}
```

**Benefits**:
- Business logic closer to data
- Encapsulation (can't bypass validation)
- Testability (test entity behavior directly)

---

### 4. Consider Aggregate Roots

**Current**: All entities are directly accessible

**Recommendation**: Make Survey an aggregate root

```csharp
public class Survey : BaseEntity, IAggregateRoot
{
    private readonly List<Question> _questions = new();

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    public void AddQuestion(string text, QuestionType type)
    {
        var orderIndex = _questions.Count;
        var question = Question.Create(Id, text, type, orderIndex);
        _questions.Add(question);
    }

    public void RemoveQuestion(int questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question != null)
        {
            _questions.Remove(question);
            ReorderQuestions();  // Maintain sequential OrderIndex
        }
    }
}
```

**Benefits**:
- Enforces consistency boundaries
- All changes to Questions go through Survey
- Easier to maintain invariants

---

### 5. Add Unit Tests for Value Objects

**Recommendation**: Comprehensive tests for NextQuestionDeterminant

```csharp
[Fact]
public void ToQuestion_WithValidId_CreatesGoToQuestionDeterminant()
{
    var determinant = NextQuestionDeterminant.ToQuestion(5);

    Assert.Equal(NextStepType.GoToQuestion, determinant.Type);
    Assert.Equal(5, determinant.NextQuestionId);
}

[Fact]
public void ToQuestion_WithZeroId_ThrowsException()
{
    Assert.Throws<ArgumentException>(() => NextQuestionDeterminant.ToQuestion(0));
}

[Fact]
public void End_CreatesEndSurveyDeterminant()
{
    var determinant = NextQuestionDeterminant.End();

    Assert.Equal(NextStepType.EndSurvey, determinant.Type);
    Assert.Null(determinant.NextQuestionId);
}

[Fact]
public void Equals_WithSameValues_ReturnsTrue()
{
    var d1 = NextQuestionDeterminant.ToQuestion(5);
    var d2 = NextQuestionDeterminant.ToQuestion(5);

    Assert.Equal(d1, d2);  // Value equality
    Assert.True(d1.Equals(d2));
}
```

---

## Validation Rules

### Survey
- Title: 3-500 characters, required
- Description: Max 2000 characters, optional
- Code: Exactly 6 alphanumeric characters, auto-generated
- Must have ≥1 question to activate

### Question
- QuestionText: Required, non-empty
- Choice questions: ≥2 options, no empty strings
- OrderIndex: 0-based, sequential, unique within survey

### Response
- Cannot create duplicate complete response if `AllowMultipleResponses = false`
- Cannot modify after `IsComplete = true`

### Answer
- Format must match question type
- Selected options must exist in question's OptionsJson
- Rating must be 1-5
- Location latitude must be -90 to 90, longitude must be -180 to 180

---

## Best Practices

### Keep Core Independent

**DO**:
- Define interfaces for all external dependencies
- Use only .NET standard libraries
- Keep entities focused on domain logic

**DON'T**:
- Reference Infrastructure, Bot, or API
- Add external NuGet packages
- Include database-specific code (EF configurations)

### Entity Design

**DO**:
```csharp
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public User Creator { get; set; } = null!;  // Null-forgiving
}
```

**DON'T**:
- Add repository/service logic to entities
- Use computed properties requiring database access

### Exception Design

**DO**:
```csharp
public class SurveyNotFoundException : Exception
{
    public int SurveyId { get; }  // Include context

    public SurveyNotFoundException(int surveyId)
        : base($"Survey with ID {surveyId} was not found.")
    {
        SurveyId = surveyId;
    }
}
```

**DON'T**:
- Catch exceptions in Core (let them bubble up)
- Use exceptions for control flow

---

## Quick Reference

### Cascade Delete Behavior

| Parent | Child | Behavior |
|--------|-------|----------|
| User | Surveys | Restrict (cannot delete user with surveys) |
| Survey | Questions | Cascade Delete |
| Survey | Responses | Cascade Delete |
| Question | Answers | Cascade Delete |
| Response | Answers | Cascade Delete |

### Common Constants

```csharp
const int SURVEY_CODE_LENGTH = 6;
const int SURVEY_TITLE_MIN = 3;
const int SURVEY_TITLE_MAX = 500;
const int RATING_MIN = 1;
const int RATING_MAX = 5;
const int PAGINATION_MAX_PAGE_SIZE = 100;
```

---

## Related Documentation

### Documentation Hub

For comprehensive project documentation, see the **centralized documentation folder**:

**Main Documentation**:
- [Project Root CLAUDE.md](../../CLAUDE.md) - Overall project overview and quick start
- [Documentation Index](../../documentation/INDEX.md) - Complete documentation catalog
- [Navigation Guide](../../documentation/NAVIGATION.md) - Role-based navigation

**Architecture Documentation**:
- [Architecture Overview](../../documentation/architecture/ARCHITECTURE.md) - Detailed architecture patterns
- [ER Diagram](../../documentation/database/ER_DIAGRAM.md) - Entity relationships
- [Entity Relationships](../../documentation/database/RELATIONSHIPS.md) - Database relationships

**Related Layer Documentation**:
- [Infrastructure Layer](../SurveyBot.Infrastructure/CLAUDE.md) - Repository implementations, DbContext
- [API Layer](../SurveyBot.API/CLAUDE.md) - REST API endpoints using Core DTOs
- [Bot Layer](../SurveyBot.Bot/CLAUDE.md) - Telegram bot using Core services

**Development Resources**:
- [DI Structure](../../documentation/development/DI-STRUCTURE.md) - Dependency injection setup
- [Developer Onboarding](../../documentation/DEVELOPER_ONBOARDING.md) - Getting started guide

### Documentation Maintenance

**When updating Core layer**:
1. Update this CLAUDE.md file with entity/interface changes
2. Update [ER Diagram](../../documentation/database/ER_DIAGRAM.md) if relationships change
3. Update [Infrastructure CLAUDE.md](../SurveyBot.Infrastructure/CLAUDE.md) if repository contracts change
4. Update [Documentation Index](../../documentation/INDEX.md) if adding significant documentation

**Where to save Core-related documentation**:
- Entity model changes → This file
- Business logic patterns → This file
- DTO specifications → This file
- Domain validation rules → This file
- Architecture decisions → `documentation/architecture/`
- Database relationships → `documentation/database/`

---

**Core Responsibilities**: Define domain entities, interfaces, DTOs, exceptions, utilities, configuration models

**Core Should NOT**: Reference other projects, contain database code, include HTTP/API code, have external package dependencies

**Key Principle**: High-level policy (Core) does not depend on low-level details (Infrastructure/Bot/API). Both depend on abstractions (interfaces defined in Core).

---

**Last Updated**: 2025-11-26 | **Version**: 1.4.2 | **Status**: Production-Ready

---

## Summary

**SurveyBot.Core** is the domain layer implementing Clean Architecture principles with zero external dependencies. It defines the business domain through entities, value objects, interfaces, and DTOs.

**Key Architectural Features**:
- ✅ **Zero Dependencies**: No external packages or project references
- ✅ **Value Objects**: NextQuestionDeterminant (DDD pattern) replaces magic values
- ✅ **Rich Enums**: QuestionType, NextStepType with business meaning
- ✅ **Constants**: SurveyConstants for domain limits and markers
- ✅ **Conditional Flow**: Branching logic with cycle detection (v1.4.0)
- ✅ **Multimedia Support**: Images, videos, audio, documents (v1.3.0)
- ✅ **Repository Pattern**: Interface-based data access contracts
- ✅ **DTO Pattern**: API contract decoupling from entities
- ✅ **Domain Exceptions**: Type-safe error handling (10 custom exceptions)

**Entity Count**: 7 entities (BaseEntity + User, Survey, Question, QuestionOption, Response, Answer)

**Interface Count**: 16 interfaces (1 generic + 15 specific)

**DTO Count**: 42+ DTOs organized in 8 categories

**Design Patterns Used**:
1. Clean Architecture (Onion Architecture)
2. Repository Pattern
3. DTO Pattern
4. Value Object Pattern (DDD)
5. Factory Method Pattern
6. Strategy Pattern (implicit)

**Recent Major Changes**:
- **v1.5.0 (2025-11-27)**: Complete DDD architecture enhancements
  - ✅ **ARCH-001**: Private setters with encapsulation (all entities)
  - ✅ **ARCH-002**: Factory methods with validation (all entities)
  - ✅ **ARCH-003**: AnswerValue polymorphic value object hierarchy
- **v1.4.2 (2025-11-26)**: Completed Answer.Next value object migration, eliminated all magic values in conditional flow
- **v1.4.0 (2025-11-21 to 2025-11-25)**: Conditional question flow with NextQuestionDeterminant value object, cycle detection, QuestionOption entity
- **v1.3.0 (2025-11-18 to 2025-11-20)**: Multimedia support with MediaContent, file storage services

**Next Recommended Enhancements** (ARCH-004 to ARCH-007):
1. **ARCH-004**: SurveyCode value object (replace string primitive)
2. **ARCH-005**: MediaContent value object (replace string JSONB)
3. **ARCH-006**: Rich domain models (add behavior to entities, Survey as aggregate root)
4. **ARCH-007**: JSON-based question configuration (for scalable question types)

**See**: [Architecture Improvements Plan](../../documentation/features/!PRIORITY_ARCHITECTURE_IMPROVEMENTS.md) for detailed implementation guide
3. Aggregate roots (Survey as aggregate)
4. Unit tests for value objects
5. Specification pattern for reusable business rules

---

**Core Responsibilities**: Define domain entities, interfaces, DTOs, exceptions, utilities, configuration models

**Core Should NOT**: Reference other projects, contain database code, include HTTP/API code, have external package dependencies

**Key Principle**: High-level policy (Core) does not depend on low-level details (Infrastructure/Bot/API). Both depend on abstractions (interfaces defined in Core).

---

**Version History**:
- **v1.5.0** (2025-11-27): DDD architecture enhancements - private setters, factory methods, AnswerValue hierarchy
- **v1.4.2** (2025-11-26): Complete value object migration for Answer.Next
- **v1.4.0** (2025-11-25): Conditional flow with value objects, cycle detection
- **v1.3.0** (2025-11-20): Multimedia support
- **v1.2.0** (2025-11-15): JWT authentication
- **v1.1.0** (2025-11-10): Survey statistics and analytics
- **v1.0.0** (2025-11-01): Initial release with basic survey functionality
