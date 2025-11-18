# SurveyBot.Core - Domain Layer Documentation

**[← Back to Main Documentation](C:\Users\User\Desktop\SurveyBot\CLAUDE.md)**

**Layer**: Domain | **Dependencies**: None | **Location**: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core`

## Overview

The Core project is the heart of the application containing all business entities, domain logic, and contracts (interfaces). Following Clean Architecture principles, **Core has zero external dependencies** - no references to Infrastructure, Bot, API, or any framework-specific libraries. All other layers depend on Core.

**Purpose**: Define domain models, business rules, repository/service interfaces, DTOs, and domain exceptions. Core is the stable center that other layers adapt to.

Core is the **domain layer** with zero dependencies. Defines business entities, interfaces, DTOs, and domain exceptions. All other layers depend on Core.

**Dependency Rule**: Core never references Infrastructure, Bot, or API. Dependencies point INWARD.

---

## Project Structure

```
SurveyBot.Core/
├── Entities/          # Domain entities (User, Survey, Question, Response, Answer)
├── Interfaces/        # Repository & service contracts
├── DTOs/              # Data transfer objects (API communication)
├── Exceptions/        # Domain-specific exceptions
├── Configuration/     # Configuration models (JwtSettings)
├── Models/            # Domain models (ValidationResult)
└── Utilities/         # Domain utilities (SurveyCodeGenerator)
```

---

## Domain Entities

### Entity Relationships

```
User (1) ─creates─> Surveys (*)
Survey (1) ─contains─> Questions (*) ─answered by─> Answers (*)
Survey (1) ─receives─> Responses (*) ─contains─> Answers (*)
```

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
    public string? OptionsJson { get; set; }        // JSONB: ["Option1", "Option2"]

    public Survey Survey { get; set; }
    public ICollection<Answer> Answers { get; set; }
}

public enum QuestionType { Text = 0, SingleChoice = 1, MultipleChoice = 2, Rating = 3 }
```

**Options Format** (choice questions):
```json
["Option 1", "Option 2", "Option 3"]
```

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

    public Survey Survey { get; set; }
    public ICollection<Answer> Answers { get; set; }
}
```

**Lifecycle**:
1. Started: `StartedAt` set, `IsComplete = false`
2. Completed: `SubmittedAt` set, `IsComplete = true`

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

    public Response Response { get; set; }
    public Question Question { get; set; }
}
```

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

### Other Services

- **IQuestionService**: AddQuestionAsync, UpdateQuestionAsync, ReorderQuestionsAsync
- **IResponseService**: StartResponseAsync, SubmitAnswerAsync, CompleteResponseAsync
- **IUserService**: GetOrCreateUserAsync (upsert pattern)
- **IAuthService**: AuthenticateAsync, GenerateJwtToken, ValidateToken

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
```

---

## Utilities

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

### Clean Architecture

Core is the **innermost layer**. Dependencies point inward.

```
API/Bot (Outer) → Infrastructure (Outer) → Core (Inner)
```

**Benefits**:
- Core is testable without external dependencies
- Infrastructure can be swapped (e.g., switch from PostgreSQL to MongoDB)
- Business logic independent of frameworks

### Repository Pattern

**Core defines interfaces** → **Infrastructure implements**

```csharp
// Core
public interface ISurveyRepository
{
    Task<Survey?> GetByIdAsync(int id);
}

// Infrastructure
public class SurveyRepository : ISurveyRepository
{
    public async Task<Survey?> GetByIdAsync(int id)
    {
        return await _context.Surveys.FindAsync(id);
    }
}
```

### DTO Pattern

**Why?** Decouple API contract from database schema.

```csharp
// Entity (Core)
public class Survey : BaseEntity
{
    public User Creator { get; set; }  // Navigation property
}

// DTO (Core)
public class SurveyDto
{
    public int CreatorId { get; set; }  // ID instead of navigation
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

**Core Responsibilities**: Define domain entities, interfaces, DTOs, exceptions, utilities, configuration models

**Core Should NOT**: Reference other projects, contain database code, include HTTP/API code, have external package dependencies

**Key Principle**: High-level policy (Core) does not depend on low-level details (Infrastructure/Bot/API). Both depend on abstractions (interfaces defined in Core).
