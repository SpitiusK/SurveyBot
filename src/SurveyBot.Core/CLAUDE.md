# SurveyBot.Core - Domain Layer Documentation

**Last Updated**: 2025-11-12
**Version**: 1.2.0
**Target Framework**: .NET 8.0
**Project Type**: Class Library (Domain Layer)

---

## Table of Contents

1. [Layer Purpose and Architecture](#layer-purpose-and-architecture)
2. [Project Structure](#project-structure)
3. [Dependencies](#dependencies)
4. [Domain Entities](#domain-entities)
5. [Repository Interfaces](#repository-interfaces)
6. [Service Interfaces](#service-interfaces)
7. [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
8. [Domain Exceptions](#domain-exceptions)
9. [Utilities](#utilities)
10. [Configuration Models](#configuration-models)
11. [Design Patterns and Principles](#design-patterns-and-principles)
12. [Best Practices](#best-practices)
13. [Common Patterns](#common-patterns)
14. [Quick Reference](#quick-reference)

---

## Layer Purpose and Architecture

### What is SurveyBot.Core?

**SurveyBot.Core** is the **domain layer** and the heart of the SurveyBot application. It contains all business entities, domain logic, interfaces (contracts), and Data Transfer Objects (DTOs). This layer has **ZERO dependencies** on external projects or frameworks.

### The Dependency Rule

**Core Principle**: The Core project should never reference Infrastructure, Bot, or API projects. All dependencies point **inward** toward Core.

```
┌─────────────────────────────────────────┐
│         SurveyBot.API                   │
│    (ASP.NET Core Web API)               │
└──────────────┬──────────────────────────┘
               │ depends on
               ▼
┌─────────────────────────────────────────┐
│     SurveyBot.Infrastructure            │
│  (Data Access, Repositories, Services)  │
└──────────────┬──────────────────────────┘
               │ depends on
               ▼
┌─────────────────────────────────────────┐
│         SurveyBot.Core                  │
│    (Domain Layer - ZERO Dependencies)   │
│  Entities, Interfaces, DTOs, Exceptions │
└─────────────────────────────────────────┘
               ▲
               │ depends on
┌──────────────┴──────────────────────────┐
│         SurveyBot.Bot                   │
│      (Telegram Bot Logic)               │
└─────────────────────────────────────────┘
```

**What this means**:
- Core defines **WHAT** the application does (business logic, domain rules)
- Infrastructure/Bot/API define **HOW** it does it (implementation details)
- Core is the most stable layer - changes here affect all other layers
- Core can be tested without any external dependencies

---

## Project Structure

```
SurveyBot.Core/
├── Entities/                           # Domain entities (business objects)
│   ├── BaseEntity.cs                   # Abstract base with Id, CreatedAt, UpdatedAt
│   ├── User.cs                         # Telegram user entity
│   ├── Survey.cs                       # Survey entity with code generation
│   ├── Question.cs                     # Survey question entity
│   ├── QuestionType.cs                 # Question type enumeration
│   ├── Response.cs                     # User's survey response
│   └── Answer.cs                       # Individual question answer
│
├── Interfaces/                         # Repository and service contracts
│   ├── IRepository.cs                  # Generic repository interface
│   ├── ISurveyRepository.cs           # Survey-specific repository
│   ├── IQuestionRepository.cs         # Question repository
│   ├── IResponseRepository.cs         # Response repository
│   ├── IUserRepository.cs             # User repository
│   ├── IAnswerRepository.cs           # Answer repository
│   ├── ISurveyService.cs              # Survey business logic
│   ├── IQuestionService.cs            # Question business logic
│   ├── IResponseService.cs            # Response business logic
│   ├── IUserService.cs                # User business logic
│   └── IAuthService.cs                # Authentication service
│
├── DTOs/                               # Data Transfer Objects
│   ├── Survey/                         # Survey DTOs
│   │   ├── SurveyDto.cs               # Full survey details
│   │   ├── SurveyListDto.cs           # Summary for lists
│   │   ├── CreateSurveyDto.cs         # Create request
│   │   ├── UpdateSurveyDto.cs         # Update request
│   │   └── ToggleSurveyStatusDto.cs   # Status toggle
│   ├── Question/                       # Question DTOs
│   │   ├── QuestionDto.cs
│   │   ├── CreateQuestionDto.cs
│   │   ├── UpdateQuestionDto.cs
│   │   └── ReorderQuestionsDto.cs
│   ├── Response/                       # Response DTOs
│   │   ├── ResponseDto.cs
│   │   ├── ResponseListDto.cs
│   │   ├── CreateResponseDto.cs
│   │   ├── SubmitAnswerDto.cs
│   │   └── CompleteResponseDto.cs
│   ├── Answer/                         # Answer DTOs
│   │   ├── AnswerDto.cs
│   │   └── CreateAnswerDto.cs
│   ├── User/                           # User DTOs
│   │   ├── UserDto.cs
│   │   ├── UpdateUserDto.cs
│   │   ├── LoginDto.cs
│   │   ├── RegisterDto.cs
│   │   ├── TokenResponseDto.cs
│   │   ├── RefreshTokenDto.cs
│   │   └── UserWithTokenDto.cs
│   ├── Auth/                           # Authentication DTOs
│   │   ├── LoginRequestDto.cs
│   │   ├── LoginResponseDto.cs
│   │   └── RefreshTokenRequestDto.cs
│   ├── Statistics/                     # Statistics DTOs
│   │   ├── SurveyStatisticsDto.cs
│   │   ├── QuestionStatisticsDto.cs
│   │   ├── ChoiceStatisticsDto.cs
│   │   ├── RatingStatisticsDto.cs
│   │   ├── RatingDistributionDto.cs
│   │   └── TextStatisticsDto.cs
│   └── Common/                         # Common DTOs
│       ├── PagedResultDto.cs          # Pagination wrapper
│       ├── PaginationQueryDto.cs      # Pagination request
│       └── ExportFormatDto.cs         # Export configuration
│
├── Exceptions/                         # Domain-specific exceptions
│   ├── SurveyNotFoundException.cs
│   ├── QuestionNotFoundException.cs
│   ├── ResponseNotFoundException.cs
│   ├── SurveyValidationException.cs
│   ├── QuestionValidationException.cs
│   ├── InvalidQuestionTypeException.cs
│   ├── SurveyOperationException.cs
│   ├── InvalidAnswerFormatException.cs
│   ├── DuplicateResponseException.cs
│   └── UnauthorizedAccessException.cs
│
├── Configuration/                      # Configuration models
│   └── JwtSettings.cs                 # JWT authentication settings
│
├── Models/                             # Domain models (non-entities)
│   └── ValidationResult.cs            # Validation result wrapper
│
├── Utilities/                          # Utility classes
│   └── SurveyCodeGenerator.cs        # Survey code generation
│
└── SurveyBot.Core.csproj              # Project file (NO dependencies)
```

---

## Dependencies

### External Package Dependencies

**NONE**. The Core project has **zero external package dependencies** except for the .NET 8.0 framework itself.

### Project File

**Location**: `SurveyBot.Core.csproj` (Lines 1-9)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Key Settings**:
- **TargetFramework**: .NET 8.0
- **ImplicitUsings**: Enabled (common namespaces auto-imported)
- **Nullable**: Enabled (null reference type checking)

### Implicit Usings

With `ImplicitUsings` enabled, these namespaces are automatically available:
- `System`
- `System.Collections.Generic`
- `System.Linq`
- `System.Threading`
- `System.Threading.Tasks`

---

## Domain Entities

All domain entities represent core business concepts. Most inherit from `BaseEntity` which provides common properties.

### Entity Relationship Overview

```
┌──────────────┐
│     User     │
│ (1 creator)  │
└──────┬───────┘
       │ 1
       │ creates
       │ *
┌──────▼───────┐
│    Survey    │───── Code (unique 6-char alphanumeric)
│  (has code)  │
└──┬─────────┬─┘
   │ 1       │ 1
   │ contains│ receives
   │ *       │ *
┌──▼───────┐ │
│ Question │ │
└──┬───────┘ │
   │ 1       │
   │answered │
   │by       │
   │ *       │
┌──▼─────┐◄──┘
│ Answer │   ┌──────────┐
└──┬─────┘   │ Response │
   │ *       │          │
   │ in      └──────────┘
   │ 1
   └─────────┘
```

### BaseEntity

**Location**: `Entities/BaseEntity.cs` (Lines 6-22)

Abstract base class providing common properties for all entities.

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Properties**:
- `Id` (int, PK) - Unique identifier, auto-generated by database
- `CreatedAt` (DateTime, UTC) - Automatically set when entity is created
- `UpdatedAt` (DateTime, UTC) - Automatically updated when entity is modified

**Important Notes**:
- Timestamps are managed by `SurveyBotDbContext.SaveChangesAsync()` override (Infrastructure layer)
- All entities inherit from this **except** `Response` and `Answer` (they have custom timestamp management)
- Always stored in UTC to avoid timezone issues

**Which entities inherit from BaseEntity?**
- User
- Survey
- Question

**Which entities do NOT inherit from BaseEntity?**
- Response (has custom `StartedAt`/`SubmittedAt` instead)
- Answer (has custom `CreatedAt` instead)

---

### User

**Location**: `Entities/User.cs` (Lines 8-47)

Represents a Telegram user in the system.

```csharp
public class User : BaseEntity
{
    [Required]
    public long TelegramId { get; set; }

    [MaxLength(255)]
    public string? Username { get; set; }

    [MaxLength(255)]
    public string? FirstName { get; set; }

    [MaxLength(255)]
    public string? LastName { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
```

**Properties**:
- `TelegramId` (long, unique, required) - Telegram's unique user ID from Telegram API
- `Username` (string?, max 255) - Telegram username without @ (nullable)
- `FirstName` (string?, max 255) - User's first name (nullable)
- `LastName` (string?, max 255) - User's last name (nullable)
- `LastLoginAt` (DateTime?) - Last login timestamp (updated during authentication)

**Navigation Properties**:
- `Surveys` - Collection of surveys created by this user

**Important Details**:
- `TelegramId` is the **external identifier** from Telegram API (immutable)
- `Id` is the **internal database primary key** (auto-increment)
- Not all Telegram users have usernames, so `Username` can be null
- Database has a unique index on `TelegramId` to ensure one user per Telegram account
- `LastLoginAt` is updated by `IAuthService.AuthenticateAsync()` during login

**Business Rules**:
- One Telegram account = one User entity (enforced by unique TelegramId)
- Cannot delete User if they have created surveys (FK constraint)
- Username is stored without @ symbol

---

### Survey

**Location**: `Entities/Survey.cs` (Lines 8-68)

Represents a survey with metadata and configuration settings.

```csharp
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(10)]
    public string? Code { get; set; }

    [Required]
    public int CreatorId { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool AllowMultipleResponses { get; set; } = false;

    [Required]
    public bool ShowResults { get; set; } = true;

    // Navigation properties
    public User Creator { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
```

**Properties**:
- `Title` (string, required, max 500) - Survey title
- `Description` (string?, nullable) - Detailed survey description
- `Code` (string?, max 10, unique) - **6-character alphanumeric code** for easy sharing (e.g., "A3X9K2")
- `CreatorId` (int, FK, required) - ID of user who created the survey
- `IsActive` (bool, default: true) - Whether survey is accepting responses
- `AllowMultipleResponses` (bool, default: false) - Allow same user to respond multiple times
- `ShowResults` (bool, default: true) - Show results to respondents after submission

**Navigation Properties**:
- `Creator` - User who created this survey
- `Questions` - Collection of questions (ordered by OrderIndex)
- `Responses` - Collection of all responses to this survey

**Survey Code Feature**:
- Auto-generated on creation using `SurveyCodeGenerator.GenerateUniqueCodeAsync()`
- Format: 6 uppercase alphanumeric characters (Base36: A-Z, 0-9)
- Used for easy survey sharing (e.g., "Take survey: A3X9K2")
- Case-insensitive (stored uppercase, compared uppercase)
- Unique across all surveys (enforced by database unique index)
- Immutable (cannot change after creation)

**Survey States**:

1. **Inactive, No Questions**: Just created, fully editable
2. **Inactive, With Questions**: Ready to activate, editable if no responses
3. **Active, No Responses**: Accepting responses, limited editing allowed
4. **Active, With Responses**: Accepting responses, cannot modify structure
5. **Deactivated**: No longer accepting responses, can reactivate

**Business Rules**:
- Must have **at least one question** to be activated
- Cannot modify `Title`, `Description`, or `Questions` if active with responses (must deactivate first)
- **Soft delete** if has responses (set `IsActive = false`)
- **Hard delete** if no responses (physical removal)
- Code is generated on creation and cannot be changed
- If `AllowMultipleResponses = false`, users can only submit one complete response

**Validation Rules**:
- Title: 3-500 characters
- Description: Max 2000 characters
- Code: Exactly 6 characters, alphanumeric

---

### Question

**Location**: `Entities/Question.cs` (Lines 8-58)

Represents a question within a survey.

```csharp
public class Question : BaseEntity
{
    [Required]
    public int SurveyId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public QuestionType QuestionType { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }

    [Required]
    public bool IsRequired { get; set; } = true;

    public string? OptionsJson { get; set; }

    // Navigation properties
    public Survey Survey { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
```

**Properties**:
- `SurveyId` (int, FK, required) - Parent survey ID
- `QuestionText` (string, required) - The question text displayed to users
- `QuestionType` (QuestionType enum, required) - Type of question (Text, SingleChoice, etc.)
- `OrderIndex` (int, 0-based, required) - Display order within survey
- `IsRequired` (bool, default: true) - Whether answer is mandatory
- `OptionsJson` (string?, JSONB) - JSON array for choice-based questions

**Navigation Properties**:
- `Survey` - Parent survey
- `Answers` - All answers to this question across all responses

**OrderIndex Behavior**:
- **0-based** indexing (first question = 0, second = 1, etc.)
- Must be sequential within a survey (0, 1, 2, 3...)
- Determines display order to respondents
- Automatically managed by `IQuestionService`:
  - New questions appended at end
  - Deleted questions trigger reordering
  - Manual reordering via `ReorderQuestionsAsync()`

**QuestionType Enum** (`Entities/QuestionType.cs`, Lines 6-27):

```csharp
public enum QuestionType
{
    Text = 0,           // Free-form text answer
    SingleChoice = 1,   // Single selection (radio button)
    MultipleChoice = 2, // Multiple selections (checkboxes)
    Rating = 3          // Numeric rating (1-5 scale)
}
```

**OptionsJson Format**:

**For SingleChoice and MultipleChoice**:
```json
["Option 1", "Option 2", "Option 3", "Option 4"]
```

**For Rating** (metadata, not options):
```json
{"min": 1, "max": 5}
```

**For Text**: `null` (no options needed)

**Validation Rules**:
- QuestionText cannot be empty
- Choice questions (Single/Multiple) must have at least 2 options
- Options cannot contain empty strings
- Rating questions must have min < max
- OrderIndex must be unique within survey
- Cannot modify question if survey is active with responses

---

### Response

**Location**: `Entities/Response.cs` (Lines 8-55)

Represents a user's response to a survey. **Does NOT** inherit from BaseEntity.

```csharp
public class Response
{
    public int Id { get; set; }

    [Required]
    public int SurveyId { get; set; }

    [Required]
    public long RespondentTelegramId { get; set; }

    [Required]
    public bool IsComplete { get; set; } = false;

    public DateTime? StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    // Navigation properties
    public Survey Survey { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
```

**Properties**:
- `Id` (int, PK) - Response ID (auto-generated)
- `SurveyId` (int, FK, required) - Survey being responded to
- `RespondentTelegramId` (long, required) - Telegram ID of respondent
- `IsComplete` (bool, default: false) - Whether response is fully submitted
- `StartedAt` (DateTime?) - When user started the response
- `SubmittedAt` (DateTime?) - When response was completed and submitted

**Navigation Properties**:
- `Survey` - Survey this response belongs to
- `Answers` - Collection of answers within this response

**IMPORTANT**: `RespondentTelegramId` is **NOT a foreign key** to the Users table. This design decision allows:
- Anonymous responses (users who haven't registered)
- Responses from users who later delete their accounts
- Decoupling response data from user management

**Response Lifecycle**:

1. **Created/Started** (via `IResponseService.StartResponseAsync()`):
   - `StartedAt` = DateTime.UtcNow
   - `IsComplete` = false
   - `SubmittedAt` = null

2. **In Progress** (user answering questions):
   - Answers being added via `IResponseService.SubmitAnswerAsync()`
   - `IsComplete` remains false

3. **Completed** (via `IResponseService.CompleteResponseAsync()`):
   - `SubmittedAt` = DateTime.UtcNow
   - `IsComplete` = true
   - No further modifications allowed

**Business Rules**:
- If `Survey.AllowMultipleResponses = false`:
  - Check for existing complete response before creating new one
  - Throw `DuplicateResponseException` if found
- Once `IsComplete = true`, answers cannot be modified
- Can have incomplete responses (user abandoned survey)
- Incomplete responses are included in statistics as "incomplete"
- Only complete responses count toward survey completion metrics

---

### Answer

**Location**: `Entities/Answer.cs` (Lines 8-55)

Represents an individual answer to a specific question within a response. **Does NOT** inherit from BaseEntity.

```csharp
public class Answer
{
    public int Id { get; set; }

    [Required]
    public int ResponseId { get; set; }

    [Required]
    public int QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public string? AnswerJson { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Response Response { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
```

**Properties**:
- `Id` (int, PK) - Answer ID (auto-generated)
- `ResponseId` (int, FK, required) - Parent response ID
- `QuestionId` (int, FK, required) - Question being answered
- `AnswerText` (string?, nullable) - For Text questions (plain text)
- `AnswerJson` (string?, JSONB) - For structured answers (JSON format)
- `CreatedAt` (DateTime, required) - When answer was submitted

**Navigation Properties**:
- `Response` - Parent response
- `Question` - Question being answered

**Database Constraints**:
- Unique constraint on `(ResponseId, QuestionId)` - one answer per question per response
- Cannot have multiple answers to same question in same response

**Answer Storage Formats by Question Type**:

**Text Questions**:
- Use `AnswerText` field (plain string)
- `AnswerJson` is null
```csharp
AnswerText = "This is the user's text answer"
AnswerJson = null
```

**SingleChoice Questions**:
- Use `AnswerJson` field
- `AnswerText` is null
```json
{
  "selectedOption": "Option 2"
}
```

**MultipleChoice Questions**:
- Use `AnswerJson` field
- `AnswerText` is null
```json
{
  "selectedOptions": ["Option 1", "Option 3", "Option 4"]
}
```

**Rating Questions**:
- Use `AnswerJson` field
- `AnswerText` is null
```json
{
  "rating": 4
}
```

**Validation Rules**:
- Answer format must match question type
- SingleChoice: selected option must exist in question's OptionsJson
- MultipleChoice: all selected options must exist in question's OptionsJson
- Rating: value must be within min-max range (typically 1-5)
- Text: any non-empty string if question is required
- Cannot submit answer for question not in response's survey

---

## Repository Interfaces

Repository interfaces define contracts for data access operations. They are implemented in the Infrastructure layer.

### IRepository<T>

**Location**: `Interfaces/IRepository.cs` (Lines 9-57)

Generic repository interface providing common CRUD operations for all entities.

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> CountAsync();
}
```

**Methods**:

- **GetByIdAsync(int id)** - Returns entity or null if not found
- **GetAllAsync()** - Returns all entities (use with caution on large tables)
- **CreateAsync(T entity)** - Adds new entity, returns with generated ID
- **UpdateAsync(T entity)** - Updates existing entity, returns updated entity
- **DeleteAsync(int id)** - Deletes entity, returns true if successful
- **ExistsAsync(int id)** - Checks if entity exists, returns boolean
- **CountAsync()** - Returns total count of entities

**Important Notes**:
- All methods are async and follow TAP (Task-based Asynchronous Pattern)
- `GetByIdAsync` returns nullable type (`T?`) - check for null before use
- `CreateAsync` automatically sets timestamps (CreatedAt, UpdatedAt) via DbContext
- `UpdateAsync` automatically updates UpdatedAt via DbContext

---

### ISurveyRepository

**Location**: `Interfaces/ISurveyRepository.cs` (Lines 8-85)

Survey-specific repository extending `IRepository<Survey>` with additional query methods.

```csharp
public interface ISurveyRepository : IRepository<Survey>
{
    // Query methods
    Task<Survey?> GetByIdWithQuestionsAsync(int id);
    Task<Survey?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId);
    Task<IEnumerable<Survey>> GetActiveSurveysAsync();
    Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm);

    // Survey code methods
    Task<Survey?> GetByCodeAsync(string code);
    Task<Survey?> GetByCodeWithQuestionsAsync(string code);
    Task<bool> CodeExistsAsync(string code);

    // Operation methods
    Task<bool> ToggleActiveStatusAsync(int id);
    Task<int> GetResponseCountAsync(int surveyId);
    Task<bool> HasResponsesAsync(int surveyId);
}
```

**Key Methods**:

**GetByIdWithQuestionsAsync(int id)**:
- Returns survey with questions included (eager loading)
- Questions ordered by OrderIndex
- Returns null if survey not found
- Use for displaying survey details

**GetByIdWithDetailsAsync(int id)**:
- Returns survey with questions, responses, and answers
- Complete survey data for statistics/analytics
- Returns null if not found
- Heavy query - use sparingly

**GetByCreatorIdAsync(int creatorId)**:
- Returns all surveys created by specific user
- Includes questions and basic response counts
- Ordered by creation date (newest first)

**GetActiveSurveysAsync()**:
- Returns only surveys where IsActive = true
- Useful for public survey listings
- Excludes deactivated/draft surveys

**SearchByTitleAsync(string searchTerm)**:
- Case-insensitive search in survey titles
- Uses PostgreSQL ILIKE operator
- Returns all matching surveys

**GetByCodeAsync(string code)** / **GetByCodeWithQuestionsAsync(string code)**:
- Find survey by unique code (case-insensitive)
- Used for public survey access via code
- Returns null if code doesn't exist

**CodeExistsAsync(string code)**:
- Check if survey code already exists
- Used by `SurveyCodeGenerator` for uniqueness validation
- Case-insensitive comparison

**ToggleActiveStatusAsync(int id)**:
- Flips IsActive flag (true → false, false → true)
- Returns true if successful

**GetResponseCountAsync(int surveyId)**:
- Returns total number of responses (complete + incomplete)

**HasResponsesAsync(int surveyId)**:
- Returns true if survey has at least one response
- Faster than counting for existence checks

---

### IQuestionRepository

**Location**: `Interfaces/IQuestionRepository.cs`

Question-specific repository operations.

```csharp
public interface IQuestionRepository : IRepository<Question>
{
    Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId);
    Task<Question?> GetByIdWithSurveyAsync(int id);
    Task<int> GetMaxOrderIndexAsync(int surveyId);
    Task<bool> ReorderQuestionsAsync(int surveyId, Dictionary<int, int> newOrders);
}
```

**Key Methods**:

**GetBySurveyIdAsync(int surveyId)**:
- Returns all questions for a survey
- Ordered by OrderIndex (ascending)
- Used for displaying survey questions

**GetByIdWithSurveyAsync(int id)**:
- Returns question with parent survey included
- Useful for authorization checks (verify user owns parent survey)

**GetMaxOrderIndexAsync(int surveyId)**:
- Returns highest OrderIndex for survey
- Used when adding new question (newIndex = maxIndex + 1)

**ReorderQuestionsAsync(int surveyId, Dictionary<int, int> newOrders)**:
- Batch update OrderIndex for multiple questions
- Dictionary: QuestionId → New OrderIndex
- Validates that new order is sequential (0, 1, 2, ...)

---

### IResponseRepository

**Location**: `Interfaces/IResponseRepository.cs`

Response-specific repository operations.

```csharp
public interface IResponseRepository : IRepository<Response>
{
    Task<IEnumerable<Response>> GetBySurveyIdAsync(int surveyId);
    Task<Response?> GetByRespondentAsync(long telegramId, int surveyId);
    Task<Response?> GetIncompleteResponseAsync(long telegramId, int surveyId);
    Task<IEnumerable<Response>> GetCompletedResponsesAsync(int surveyId);
    Task<int> GetCompletedCountAsync(int surveyId);
    Task<Response?> GetResponseWithAnswersAsync(int responseId);
}
```

**Key Methods**:

**GetBySurveyIdAsync(int surveyId)**:
- Returns all responses for a survey (complete + incomplete)
- Includes answers and question details

**GetByRespondentAsync(long telegramId, int surveyId)**:
- Returns completed response(s) by specific user for survey
- Used to check if user already responded (when AllowMultipleResponses = false)

**GetIncompleteResponseAsync(long telegramId, int surveyId)**:
- Returns in-progress response for user
- Used to resume incomplete survey

**GetCompletedResponsesAsync(int surveyId)**:
- Returns only completed responses (IsComplete = true)
- Used for statistics calculations

**GetCompletedCountAsync(int surveyId)**:
- Returns count of completed responses
- Faster than loading all responses for counts

**GetResponseWithAnswersAsync(int responseId)**:
- Returns response with all answers and related questions
- Used for displaying response details

---

### IUserRepository

**Location**: `Interfaces/IUserRepository.cs`

User-specific repository operations.

```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByTelegramIdAsync(long telegramId);
    Task<User> CreateOrUpdateAsync(long telegramId, string? username,
                                     string? firstName, string? lastName);
    Task UpdateLastLoginAsync(int userId);
}
```

**Key Methods**:

**GetByTelegramIdAsync(long telegramId)**:
- Find user by Telegram ID (external identifier)
- Returns null if not found

**CreateOrUpdateAsync(...)**:
- **Upsert** operation: create if doesn't exist, update if exists
- Used during authentication to sync Telegram user data
- Updates Username, FirstName, LastName if changed

**UpdateLastLoginAsync(int userId)**:
- Sets LastLoginAt to current UTC time
- Called during successful authentication

---

### IAnswerRepository

**Location**: `Interfaces/IAnswerRepository.cs`

Answer-specific repository operations.

```csharp
public interface IAnswerRepository : IRepository<Answer>
{
    Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId);
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
    Task<Answer?> GetAnswerAsync(int responseId, int questionId);
}
```

**Key Methods**:

**GetByResponseIdAsync(int responseId)**:
- Returns all answers for a response
- Includes related question details
- Used for displaying response

**GetByQuestionIdAsync(int questionId)**:
- Returns all answers to a specific question (across all responses)
- Used for question-level statistics

**GetAnswerAsync(int responseId, int questionId)**:
- Returns specific answer for question within response
- Used for checking if question already answered

---

## Service Interfaces

Service interfaces define business logic operations. They use repositories for data access and enforce domain rules.

### ISurveyService

**Location**: `Interfaces/ISurveyService.cs` (Lines 10-122)

Survey business logic operations with authorization and validation.

```csharp
public interface ISurveyService
{
    // CRUD operations
    Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto);
    Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto);
    Task<bool> DeleteSurveyAsync(int surveyId, int userId);
    Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId);
    Task<PagedResultDto<SurveyListDto>> GetAllSurveysAsync(int userId, PaginationQueryDto query);

    // Status operations
    Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId);
    Task<SurveyDto> DeactivateSurveyAsync(int surveyId, int userId);

    // Analytics
    Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId);

    // Authorization
    Task<bool> UserOwnsSurveyAsync(int surveyId, int userId);

    // Public access
    Task<SurveyDto> GetSurveyByCodeAsync(string code);

    // Export
    Task<string> ExportSurveyToCSVAsync(int surveyId, int userId,
                                         string filter = "completed",
                                         bool includeMetadata = true,
                                         bool includeTimestamps = true);
}
```

**Key Methods**:

**CreateSurveyAsync(int userId, CreateSurveyDto dto)**:
- Creates new survey for user
- Generates unique 6-character survey code
- Sets CreatorId = userId
- Sets IsActive = false (draft mode)
- Returns SurveyDto with generated code
- **Throws**: SurveyValidationException

**UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)**:
- Updates survey title, description, settings
- Checks user ownership
- Prevents modification if active with responses
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException, SurveyOperationException

**DeleteSurveyAsync(int surveyId, int userId)**:
- **Soft delete** if survey has responses (set IsActive = false)
- **Hard delete** if no responses (physical removal)
- Checks user ownership
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException

**ActivateSurveyAsync(int surveyId, int userId)**:
- Sets IsActive = true
- Validates survey has at least one question
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException, SurveyValidationException

**GetSurveyStatisticsAsync(int surveyId, int userId)**:
- Returns comprehensive statistics:
  - Total/completed/incomplete responses
  - Completion rate, unique respondents
  - Average completion time
  - Question-level statistics (choice distribution, rating averages, etc.)
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException

**GetSurveyByCodeAsync(string code)**:
- **Public method** - no authentication required
- Returns survey by code if IsActive = true
- Used for anonymous survey access
- **Throws**: SurveyNotFoundException

**ExportSurveyToCSVAsync(...)**:
- Exports responses to CSV format
- Filter: "all", "completed", "incomplete"
- Optional metadata and timestamps
- Returns CSV as string
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException

---

### IQuestionService

**Location**: `Interfaces/IQuestionService.cs`

Question business logic operations.

```csharp
public interface IQuestionService
{
    Task<QuestionDto> AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto);
    Task<QuestionDto> UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto);
    Task<bool> DeleteQuestionAsync(int questionId, int userId);
    Task<QuestionDto> GetQuestionAsync(int questionId, int userId);
    Task<IEnumerable<QuestionDto>> GetSurveyQuestionsAsync(int surveyId, int userId);
    Task<bool> ReorderQuestionsAsync(int surveyId, int userId, ReorderQuestionsDto dto);
}
```

**Key Methods**:

**AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto)**:
- Adds question to survey
- Auto-assigns OrderIndex (maxIndex + 1)
- Validates question type and options
- Checks user owns parent survey
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException, QuestionValidationException

**UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto)**:
- Updates question text, options, IsRequired
- Cannot change QuestionType (delete and recreate instead)
- Prevents modification if survey active with responses
- **Throws**: QuestionNotFoundException, UnauthorizedAccessException, SurveyOperationException

**DeleteQuestionAsync(int questionId, int userId)**:
- Removes question
- Reorders remaining questions (fills gap in OrderIndex)
- **Throws**: QuestionNotFoundException, UnauthorizedAccessException

**ReorderQuestionsAsync(int surveyId, int userId, ReorderQuestionsDto dto)**:
- Batch update OrderIndex for multiple questions
- Validates new order is sequential
- **Throws**: SurveyNotFoundException, UnauthorizedAccessException, QuestionValidationException

---

### IResponseService

**Location**: `Interfaces/IResponseService.cs`

Response submission and management operations.

```csharp
public interface IResponseService
{
    Task<ResponseDto> StartResponseAsync(long telegramId, int surveyId);
    Task<AnswerDto> SubmitAnswerAsync(int responseId, int questionId, SubmitAnswerDto dto);
    Task<ResponseDto> CompleteResponseAsync(int responseId, CompleteResponseDto dto);
    Task<ResponseDto> GetResponseAsync(int responseId, int userId);
    Task<PagedResultDto<ResponseListDto>> GetSurveyResponsesAsync(int surveyId, int userId,
                                                                    PaginationQueryDto query);
}
```

**Key Methods**:

**StartResponseAsync(long telegramId, int surveyId)**:
- Creates new response
- Checks survey is active
- Checks AllowMultipleResponses setting
- Sets StartedAt, IsComplete = false
- **Throws**: SurveyNotFoundException, DuplicateResponseException

**SubmitAnswerAsync(int responseId, int questionId, SubmitAnswerDto dto)**:
- Saves answer to question
- Validates answer format matches question type
- Upsert operation (create or update)
- **Throws**: ResponseNotFoundException, QuestionNotFoundException, InvalidAnswerFormatException

**CompleteResponseAsync(int responseId, CompleteResponseDto dto)**:
- Marks response as complete
- Sets SubmittedAt, IsComplete = true
- Validates all required questions answered
- **Throws**: ResponseNotFoundException, SurveyValidationException

**GetResponseAsync(int responseId, int userId)**:
- Returns response details with answers
- Checks user owns parent survey
- **Throws**: ResponseNotFoundException, UnauthorizedAccessException

---

### IUserService

**Location**: `Interfaces/IUserService.cs`

User management operations.

```csharp
public interface IUserService
{
    Task<UserDto> GetOrCreateUserAsync(long telegramId, string? username,
                                         string? firstName, string? lastName);
    Task<UserDto> GetUserByIdAsync(int userId);
    Task<UserDto> GetUserByTelegramIdAsync(long telegramId);
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto dto);
}
```

**Key Methods**:

**GetOrCreateUserAsync(...)**:
- **Upsert** operation for Telegram users
- Creates new user if doesn't exist
- Updates profile if exists
- Returns UserDto

**GetUserByIdAsync(int userId)**:
- Returns user by internal database ID
- **Throws**: UserNotFoundException

**GetUserByTelegramIdAsync(long telegramId)**:
- Returns user by Telegram ID
- **Throws**: UserNotFoundException

---

### IAuthService

**Location**: `Interfaces/IAuthService.cs`

Authentication and JWT token operations.

```csharp
public interface IAuthService
{
    Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    string GenerateJwtToken(User user);
}
```

**Key Methods**:

**AuthenticateAsync(LoginRequestDto request)**:
- Authenticates Telegram user
- Creates/updates user record
- Generates JWT access token and refresh token
- Updates LastLoginAt
- Returns tokens and user info

**RefreshTokenAsync(RefreshTokenRequestDto request)**:
- Refreshes expired access token
- Validates refresh token
- Generates new access token
- Returns new tokens

**GenerateJwtToken(User user)**:
- Creates JWT token with user claims
- Claims: UserId (NameIdentifier), Username, TelegramId
- Token lifetime from JwtSettings configuration

---

## Data Transfer Objects (DTOs)

DTOs are used for API communication and data transfer between layers. They do NOT contain navigation properties or database-specific fields.

### DTO Naming Conventions

- **CreateXxxDto** - For POST requests (creation operations)
- **UpdateXxxDto** - For PUT/PATCH requests (update operations)
- **XxxDto** - For responses (full entity details)
- **XxxListDto** - For list responses (summary/lightweight version)

### Survey DTOs

**SurveyDto** (`DTOs/Survey/SurveyDto.cs`, Lines 9-80):

Full survey details for responses.

```csharp
public class SurveyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public int CreatorId { get; set; }
    public UserDto? Creator { get; set; }
    public bool IsActive { get; set; }
    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**CreateSurveyDto** (`DTOs/Survey/CreateSurveyDto.cs`):

```csharp
public class CreateSurveyDto
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool AllowMultipleResponses { get; set; } = false;
    public bool ShowResults { get; set; } = true;
}
```

**UpdateSurveyDto** (`DTOs/Survey/UpdateSurveyDto.cs`):

```csharp
public class UpdateSurveyDto
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }
}
```

**SurveyListDto** (`DTOs/Survey/SurveyListDto.cs`):

Lightweight version for list displays.

```csharp
public class SurveyListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### Question DTOs

**QuestionDto** (`DTOs/Question/QuestionDto.cs`):

```csharp
public class QuestionDto
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; }
    public List<string>? Options { get; set; }  // Deserialized from OptionsJson
    public DateTime CreatedAt { get; set; }
}
```

**CreateQuestionDto** (`DTOs/Question/CreateQuestionDto.cs`):

```csharp
public class CreateQuestionDto
{
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public QuestionType QuestionType { get; set; }

    public bool IsRequired { get; set; } = true;

    public List<string>? Options { get; set; }  // For choice questions
}
```

**UpdateQuestionDto** (`DTOs/Question/UpdateQuestionDto.cs`):

```csharp
public class UpdateQuestionDto
{
    public string? QuestionText { get; set; }
    public bool? IsRequired { get; set; }
    public List<string>? Options { get; set; }
}
```

**ReorderQuestionsDto** (`DTOs/Question/ReorderQuestionsDto.cs`):

```csharp
public class ReorderQuestionsDto
{
    [Required]
    public Dictionary<int, int> QuestionOrders { get; set; } = new();
    // Key: QuestionId, Value: New OrderIndex
}
```

---

### Response DTOs

**ResponseDto** (`DTOs/Response/ResponseDto.cs`):

```csharp
public class ResponseDto
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public long RespondentTelegramId { get; set; }
    public bool IsComplete { get; set; }
    public int AnsweredCount { get; set; }      // Calculated
    public int TotalQuestions { get; set; }     // Calculated
    public double ProgressPercentage { get; set; }  // Calculated
    public List<AnswerDto> Answers { get; set; } = new();
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
```

**CreateResponseDto** (`DTOs/Response/CreateResponseDto.cs`):

```csharp
public class CreateResponseDto
{
    [Required]
    public int SurveyId { get; set; }

    [Required]
    public long RespondentTelegramId { get; set; }
}
```

**SubmitAnswerDto** (`DTOs/Response/SubmitAnswerDto.cs`):

```csharp
public class SubmitAnswerDto
{
    public string? TextAnswer { get; set; }              // For Text questions
    public string? SelectedOption { get; set; }          // For SingleChoice
    public List<string>? SelectedOptions { get; set; }   // For MultipleChoice
    public int? RatingValue { get; set; }                // For Rating (1-5)
}
```

**CompleteResponseDto** (`DTOs/Response/CompleteResponseDto.cs`):

```csharp
public class CompleteResponseDto
{
    // Empty DTO - just signals completion
    // Could be extended with completion metadata in future
}
```

---

### Answer DTOs

**AnswerDto** (`DTOs/Answer/AnswerDto.cs`):

```csharp
public class AnswerDto
{
    public int Id { get; set; }
    public int ResponseId { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }

    // Type-specific answer fields
    public string? TextAnswer { get; set; }
    public string? SelectedOption { get; set; }
    public List<string>? SelectedOptions { get; set; }
    public int? RatingValue { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

### Statistics DTOs

**SurveyStatisticsDto** (`DTOs/Statistics/SurveyStatisticsDto.cs`):

```csharp
public class SurveyStatisticsDto
{
    public int SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
    public int IncompleteResponses { get; set; }
    public double CompletionRate { get; set; }       // Percentage
    public int UniqueRespondents { get; set; }       // Distinct Telegram IDs
    public double? AverageCompletionTime { get; set; }  // Seconds
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? LastResponseAt { get; set; }
    public List<QuestionStatisticsDto> QuestionStatistics { get; set; } = new();
}
```

**QuestionStatisticsDto** (`DTOs/Statistics/QuestionStatisticsDto.cs`):

```csharp
public class QuestionStatisticsDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int TotalAnswers { get; set; }
    public int SkippedCount { get; set; }
    public double ResponseRate { get; set; }  // Percentage

    // Type-specific statistics (populated based on QuestionType)
    public Dictionary<string, ChoiceStatisticsDto>? ChoiceDistribution { get; set; }
    public RatingStatisticsDto? RatingStatistics { get; set; }
    public TextStatisticsDto? TextStatistics { get; set; }
}
```

**ChoiceStatisticsDto** (`DTOs/Statistics/ChoiceStatisticsDto.cs`):

```csharp
public class ChoiceStatisticsDto
{
    public string Option { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
```

**RatingStatisticsDto** (`DTOs/Statistics/RatingStatisticsDto.cs`):

```csharp
public class RatingStatisticsDto
{
    public double AverageRating { get; set; }
    public int MinRating { get; set; }
    public int MaxRating { get; set; }
    public Dictionary<int, RatingDistributionDto> Distribution { get; set; } = new();
}
```

---

### Common DTOs

**PagedResultDto<T>** (`DTOs/Common/PagedResultDto.cs`, Lines 7-43):

Generic pagination wrapper.

```csharp
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

**PaginationQueryDto** (`DTOs/Common/PaginationQueryDto.cs`):

```csharp
public class PaginationQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
```

---

## Domain Exceptions

All custom exceptions inherit from `System.Exception` and are in the `SurveyBot.Core.Exceptions` namespace.

### Exception Hierarchy

```
System.Exception
├── SurveyNotFoundException
├── QuestionNotFoundException
├── ResponseNotFoundException
├── SurveyValidationException
├── QuestionValidationException
├── InvalidQuestionTypeException
├── SurveyOperationException
├── InvalidAnswerFormatException
├── DuplicateResponseException
└── UnauthorizedAccessException
```

### Exception Details

**SurveyNotFoundException** (`Exceptions/SurveyNotFoundException.cs`, Lines 6-31):

Thrown when a survey with specified ID or code is not found.

```csharp
public class SurveyNotFoundException : Exception
{
    public int? SurveyId { get; }

    public SurveyNotFoundException(int surveyId)
        : base($"Survey with ID {surveyId} was not found.")
    {
        SurveyId = surveyId;
    }

    public SurveyNotFoundException(string message)
        : base(message)
    {
    }
}
```

**When thrown**:
- Repository returns null for survey lookup
- Service layer catches null and throws this exception
- Survey code lookup fails

---

**QuestionNotFoundException** (`Exceptions/QuestionNotFoundException.cs`):

Thrown when a question with specified ID is not found.

---

**ResponseNotFoundException** (`Exceptions/ResponseNotFoundException.cs`):

Thrown when a response with specified ID is not found.

---

**SurveyValidationException** (`Exceptions/SurveyValidationException.cs`):

Thrown when survey data validation fails.

**Examples**:
- Title too short (< 3 characters) or too long (> 500 characters)
- Attempting to activate survey with no questions
- Required field is missing or invalid

---

**QuestionValidationException** (`Exceptions/QuestionValidationException.cs`):

Thrown when question data validation fails.

**Examples**:
- Choice question has fewer than 2 options
- Options array contains empty strings
- Invalid rating range (min >= max)
- Empty question text

---

**InvalidQuestionTypeException** (`Exceptions/InvalidQuestionTypeException.cs`):

Thrown when an invalid question type is specified.

---

**SurveyOperationException** (`Exceptions/SurveyOperationException.cs`):

Thrown when an operation cannot be performed due to business rules.

**Examples**:
- Attempting to modify active survey with responses
- Attempting to delete survey with responses (should use soft delete)
- Attempting operation on deactivated survey

---

**InvalidAnswerFormatException** (`Exceptions/InvalidAnswerFormatException.cs`):

Thrown when answer data doesn't match question type or is malformed.

**Examples**:
- Submitting text answer for rating question
- Selected option not in question's OptionsJson
- Invalid JSON format in AnswerJson
- Rating value outside min-max range

---

**DuplicateResponseException** (`Exceptions/DuplicateResponseException.cs`):

Thrown when user attempts to submit multiple responses when AllowMultipleResponses = false.

---

**UnauthorizedAccessException** (`Exceptions/UnauthorizedAccessException.cs`):

Thrown when user tries to access or modify a resource they don't own.

**Examples**:
- User tries to update another user's survey
- User tries to view private statistics for survey they didn't create

---

## Utilities

### SurveyCodeGenerator

**Location**: `Utilities/SurveyCodeGenerator.cs` (Lines 8-72)

Static utility class for generating unique, URL-safe survey codes.

```csharp
public static class SurveyCodeGenerator
{
    private const string ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    public static string GenerateCode();
    public static Task<string> GenerateUniqueCodeAsync(
        Func<string, Task<bool>> codeExistsAsync, int maxAttempts = 10);
    public static bool IsValidCode(string? code);
}
```

**Purpose**: Generate short, memorable codes for easy survey sharing.

**Code Format**:
- **Length**: 6 characters (fixed)
- **Character Set**: Base36 (A-Z, 0-9) - 36 characters
- **Total Combinations**: 36^6 = 2,176,782,336 (over 2 billion)
- **Case**: Uppercase only (stored and compared as uppercase)
- **Example**: "A3X9K2", "ZBQW8M", "12ABCD"

**Methods**:

**GenerateCode()**:
```csharp
public static string GenerateCode()
```
- Generates random 6-character code
- Uses `RandomNumberGenerator.GetInt32()` (cryptographically secure)
- Returns uppercase string
- Does NOT check for uniqueness

**GenerateUniqueCodeAsync(codeExistsAsync, maxAttempts)**:
```csharp
public static async Task<string> GenerateUniqueCodeAsync(
    Func<string, Task<bool>> codeExistsAsync,
    int maxAttempts = 10)
```
- Generates code and checks uniqueness via callback function
- Retries up to `maxAttempts` times if collision detected
- Throws `InvalidOperationException` if unable to generate unique code
- **Usage**:
  ```csharp
  var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
      _surveyRepository.CodeExistsAsync);
  ```

**IsValidCode(code)**:
```csharp
public static bool IsValidCode(string? code)
```
- Validates code format
- Checks: not null/whitespace, exactly 6 characters, all alphanumeric
- Returns `true` if valid, `false` otherwise
- Used for input validation

**Security Considerations**:
- Uses `RandomNumberGenerator` (cryptographically secure PRNG)
- Codes are non-sequential and unpredictable
- No personally identifiable information in codes
- Collision probability is extremely low (< 0.0001% with millions of surveys)

**Example Usage in Service**:
```csharp
// In SurveyService.CreateSurveyAsync()
var survey = _mapper.Map<Survey>(dto);
survey.CreatorId = userId;
survey.Code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
    _surveyRepository.CodeExistsAsync);
await _surveyRepository.CreateAsync(survey);
```

---

## Configuration Models

### JwtSettings

**Location**: `Configuration/JwtSettings.cs` (Lines 6-35)

Configuration model for JWT authentication settings.

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int TokenLifetimeHours { get; set; } = 24;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
```

**Properties**:
- **SecretKey** - Secret key for signing JWT tokens (min 32 characters for HMAC-SHA256)
- **Issuer** - Token issuer (API that creates the token, e.g., "SurveyBot.API")
- **Audience** - Token audience (who can use the token, e.g., "SurveyBot.Client")
- **TokenLifetimeHours** - Access token lifetime in hours (default: 24)
- **RefreshTokenLifetimeDays** - Refresh token lifetime in days (default: 7)

**appsettings.json Configuration**:
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "TokenLifetimeHours": 24,
    "RefreshTokenLifetimeDays": 7
  }
}
```

**Usage in Dependency Injection**:
```csharp
// In Program.cs or Startup.cs
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// In AuthService constructor
public AuthService(IOptions<JwtSettings> jwtSettings)
{
    _jwtSettings = jwtSettings.Value;
}
```

**Security Notes**:
- SecretKey must be at least 32 characters for HS256 algorithm
- Store SecretKey in environment variables or Azure Key Vault (not in source control)
- Rotate keys periodically for enhanced security

---

## Design Patterns and Principles

### 1. Clean Architecture (Onion Architecture)

The Core layer is the **innermost layer** with zero dependencies on external concerns.

**Dependency Rule**: Dependencies point **inward**. Core defines interfaces, outer layers implement them.

```
┌────────────────────────────────────┐
│    Infrastructure (Outer Layer)   │  ← Implements IRepository
│  Repositories, DbContext, Services │
└────────────────┬───────────────────┘
                 │ depends on
                 ▼
┌────────────────────────────────────┐
│      Core (Inner Layer)            │  ← Defines IRepository
│  Entities, Interfaces, DTOs        │
└────────────────────────────────────┘
```

**Benefits**:
- Core is testable without external dependencies
- Infrastructure can be swapped (e.g., switch from PostgreSQL to MongoDB)
- Business logic is independent of frameworks

---

### 2. Repository Pattern

Abstracts data access logic behind interfaces.

**Core Layer**: Defines `IRepository<T>`, `ISurveyRepository`, etc.
**Infrastructure Layer**: Implements repositories using EF Core.

**Benefits**:
- Decouples business logic from data access
- Enables easy mocking for unit tests
- Provides consistent data access API

**Example**:
```csharp
// In Core
public interface ISurveyRepository : IRepository<Survey>
{
    Task<Survey?> GetByIdWithQuestionsAsync(int id);
}

// In Infrastructure
public class SurveyRepository : ISurveyRepository
{
    private readonly SurveyBotDbContext _context;

    public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
    {
        return await _context.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
```

---

### 3. Dependency Inversion Principle (DIP)

High-level modules (business logic) depend on abstractions (interfaces), not concrete implementations.

**Core defines interfaces**:
```csharp
public interface ISurveyService
{
    Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto);
}
```

**Infrastructure implements interfaces**:
```csharp
public class SurveyService : ISurveyService
{
    public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
    {
        // Implementation
    }
}
```

**API depends on abstraction**:
```csharp
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;  // Depends on interface

    public SurveysController(ISurveyService surveyService)
    {
        _surveyService = surveyService;
    }
}
```

---

### 4. Data Transfer Object (DTO) Pattern

Separate DTOs from domain entities for API communication.

**Why?**
- Decouple API contract from database schema
- Control what data is exposed to clients
- Enable different representations for different use cases (Create vs Update vs List)

**Example**:
```csharp
// Entity (Core)
public class Survey : BaseEntity
{
    public string Title { get; set; }
    public User Creator { get; set; }  // Navigation property
    public ICollection<Question> Questions { get; set; }
}

// DTO (Core)
public class SurveyDto
{
    public string Title { get; set; }
    public int CreatorId { get; set; }  // ID instead of navigation property
    public List<QuestionDto> Questions { get; set; }  // DTO instead of entity
}
```

---

### 5. Domain-Driven Design (DDD) Principles

**Entities**: Core business objects with identity (User, Survey, Question)
**Value Objects**: Objects defined by attributes (QuestionType enum, SurveyCode)
**Aggregates**: Survey is root aggregate containing Questions
**Domain Services**: Business logic that doesn't belong to single entity (ISurveyService)
**Repositories**: Data access abstraction for aggregates

---

## Best Practices

### 1. Keep Core Independent

**DO**:
- Define interfaces for all external dependencies
- Use only .NET standard libraries
- Keep entities simple and focused on domain logic
- Define business rules and validation in Core

**DON'T**:
- Reference Infrastructure, Bot, or API projects
- Add external NuGet package dependencies
- Include database-specific code (EF configurations)
- Include HTTP/API-specific code (controllers, middleware)

---

### 2. Entity Design Best Practices

**DO**:
- Inherit from `BaseEntity` for standard entities
- Use navigation properties for relationships
- Add data annotations for validation
- Use nullable reference types appropriately (`string?`)
- Override `ToString()` for debugging

**DON'T**:
- Add repository/service logic to entities
- Make navigation properties required (use null-forgiving: `= null!`)
- Use computed properties that require database access

**Example**:
```csharp
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    // Navigation property with null-forgiving operator
    public User Creator { get; set; } = null!;

    public override string ToString() => $"Survey {Id}: {Title}";
}
```

---

### 3. DTO Design Best Practices

**DO**:
- Keep DTOs simple POCOs (Plain Old CLR Objects)
- Separate DTOs for different purposes (Create, Update, Response)
- Use nullable types appropriately
- Add data annotations for validation
- Use `init` accessors for immutable DTOs (optional)

**DON'T**:
- Include navigation properties (use IDs or nested DTOs)
- Add business logic to DTOs
- Reuse entity classes as DTOs

**Example**:
```csharp
public class CreateSurveyDto
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}
```

---

### 4. Interface Design Best Practices

**DO**:
- Keep interfaces focused (Interface Segregation Principle)
- Use specific return types (avoid `object`)
- Document expected behavior with XML comments
- Return `Task<T?>` for nullable results
- Use `CancellationToken` for long-running operations

**DON'T**:
- Create "god interfaces" with too many methods
- Add implementation details to interfaces
- Use `out` or `ref` parameters (use return types)

**Example**:
```csharp
/// <summary>
/// Repository interface for Survey entity.
/// </summary>
public interface ISurveyRepository : IRepository<Survey>
{
    /// <summary>
    /// Gets a survey by ID with all related questions included.
    /// </summary>
    /// <param name="id">The survey ID.</param>
    /// <returns>The survey with questions if found, otherwise null.</returns>
    Task<Survey?> GetByIdWithQuestionsAsync(int id);
}
```

---

### 5. Exception Design Best Practices

**DO**:
- Create specific exceptions for domain errors
- Include helpful error messages
- Add properties with error context (e.g., `SurveyId`)
- Inherit from `Exception` (not `ApplicationException`)

**DON'T**:
- Catch exceptions in Core layer (let them bubble up)
- Use exceptions for control flow
- Create generic exceptions

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

### 6. Naming Conventions

**Entities**: Singular nouns
- User, Survey, Question ✅
- Users, Surveys, Questions ❌

**DTOs**: Entity name + purpose suffix
- CreateSurveyDto, SurveyDto, SurveyListDto ✅
- SurveyCreate, SurveyModel ❌

**Repositories**: I{Entity}Repository
- ISurveyRepository, IUserRepository ✅
- ISurveyRepo, SurveyDataAccess ❌

**Services**: I{Entity}Service
- ISurveyService, IAuthService ✅
- ISurveyManager, SurveyBusinessLogic ❌

**Async Methods**: End with `Async` suffix
- GetByIdAsync, CreateSurveyAsync ✅
- GetById, CreateSurvey (for async methods) ❌

---

## Common Patterns

### 1. Service Method with Authorization

```csharp
public async Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId)
{
    // 1. Fetch entity
    var survey = await _surveyRepository.GetByIdAsync(surveyId);

    // 2. Check existence
    if (survey == null)
        throw new SurveyNotFoundException(surveyId);

    // 3. Check authorization
    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException(
            $"User {userId} is not authorized to access survey {surveyId}.");

    // 4. Map to DTO and return
    return _mapper.Map<SurveyDto>(survey);
}
```

---

### 2. Service Method with Validation

```csharp
public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
{
    // 1. Validate input
    if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3)
        throw new SurveyValidationException("Survey title must be at least 3 characters.");

    // 2. Map DTO to entity
    var survey = _mapper.Map<Survey>(dto);
    survey.CreatorId = userId;
    survey.IsActive = false;

    // 3. Generate unique code
    survey.Code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
        _surveyRepository.CodeExistsAsync);

    // 4. Save to database
    var created = await _surveyRepository.CreateAsync(survey);

    // 5. Map back to DTO and return
    return _mapper.Map<SurveyDto>(created);
}
```

---

### 3. Repository Method with Eager Loading

```csharp
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()  // Read-only query (performance)
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))  // Eager load, ordered
        .Include(s => s.Creator)  // Include creator
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

---

### 4. Handling Optional Navigation Properties

```csharp
// In entity
public class Survey : BaseEntity
{
    public User Creator { get; set; } = null!;  // Null-forgiving operator
}

// In service
public async Task<SurveyDto> GetSurveyAsync(int id)
{
    var survey = await _repository.GetByIdAsync(id);
    if (survey == null)
        throw new SurveyNotFoundException(id);

    // Creator might not be loaded, handle gracefully
    var dto = _mapper.Map<SurveyDto>(survey);
    return dto;
}
```

---

## Quick Reference

### File Locations

| Component | File Path | Key Lines |
|-----------|-----------|-----------|
| BaseEntity | `Entities/BaseEntity.cs` | 6-22 |
| User | `Entities/User.cs` | 8-47 |
| Survey | `Entities/Survey.cs` | 8-68 |
| Question | `Entities/Question.cs` | 8-58 |
| QuestionType | `Entities/QuestionType.cs` | 6-27 |
| Response | `Entities/Response.cs` | 8-55 |
| Answer | `Entities/Answer.cs` | 8-55 |
| IRepository<T> | `Interfaces/IRepository.cs` | 9-57 |
| ISurveyRepository | `Interfaces/ISurveyRepository.cs` | 8-85 |
| ISurveyService | `Interfaces/ISurveyService.cs` | 10-122 |
| SurveyDto | `DTOs/Survey/SurveyDto.cs` | 9-80 |
| PagedResultDto<T> | `DTOs/Common/PagedResultDto.cs` | 7-43 |
| SurveyCodeGenerator | `Utilities/SurveyCodeGenerator.cs` | 8-72 |
| JwtSettings | `Configuration/JwtSettings.cs` | 6-35 |
| SurveyNotFoundException | `Exceptions/SurveyNotFoundException.cs` | 6-31 |

---

### Key Constants

```csharp
// Survey Code
const int SURVEY_CODE_LENGTH = 6;
const string VALID_CODE_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
const long TOTAL_CODE_COMBINATIONS = 2_176_782_336;

// Validation Limits
const int SURVEY_TITLE_MIN_LENGTH = 3;
const int SURVEY_TITLE_MAX_LENGTH = 500;
const int SURVEY_DESCRIPTION_MAX_LENGTH = 2000;
const int QUESTION_MIN_OPTIONS = 2;
const int RATING_MIN = 1;
const int RATING_MAX = 5;
const int PAGINATION_MAX_PAGE_SIZE = 100;
```

---

### Entity Cascade Delete Behavior

| Parent Entity | Child Entity | Behavior |
|---------------|--------------|----------|
| User | Surveys | **Restrict** (cannot delete user with surveys) |
| Survey | Questions | **Cascade Delete** |
| Survey | Responses | **Cascade Delete** |
| Question | Answers | **Cascade Delete** |
| Response | Answers | **Cascade Delete** |

---

### Common Validation Rules

**Survey**:
- Title: 3-500 characters, required
- Description: max 2000 characters, optional
- Code: exactly 6 alphanumeric characters, auto-generated
- Must have ≥1 question to activate

**Question**:
- QuestionText: required, non-empty
- Choice questions: ≥2 options
- Options: no empty strings
- OrderIndex: 0-based, sequential

**Response**:
- Cannot create duplicate complete response if AllowMultipleResponses = false
- Cannot modify after IsComplete = true

**Answer**:
- Format must match question type
- Selected options must exist in question's OptionsJson
- Rating must be within range (1-5)

---

## Summary

### Core Responsibilities

1. **Define domain entities** - User, Survey, Question, Response, Answer
2. **Define interfaces** - Contracts for repositories and services
3. **Define DTOs** - Communication objects for API
4. **Define exceptions** - Domain-specific error types
5. **Provide utilities** - SurveyCodeGenerator
6. **Define configuration** - JwtSettings

### Core Should NOT

1. Reference other projects (Infrastructure, Bot, API)
2. Contain database-specific code (EF configurations, migrations)
3. Contain HTTP/API-specific code (controllers, middleware)
4. Contain Telegram bot-specific code (Telegram.Bot library)
5. Have external package dependencies (except .NET 8.0 SDK)

### Key Architectural Principle

**Dependency Inversion**: High-level policy (Core) should not depend on low-level details (Infrastructure, Bot, API). Both should depend on abstractions (interfaces defined in Core).

```
Core defines WHAT (business logic)
Infrastructure/Bot/API define HOW (implementation)
```

---

**Last Updated**: 2025-11-12
**Version**: 1.2.0
**Target Framework**: .NET 8.0
**Maintainer**: SurveyBot Development Team
