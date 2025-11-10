# SurveyBot.Core - Domain Layer Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture Principles](#architecture-principles)
3. [Project Structure](#project-structure)
4. [Entities](#entities)
5. [Interfaces](#interfaces)
6. [Data Transfer Objects](#data-transfer-objects)
7. [Exceptions](#exceptions)
8. [Utilities](#utilities)
9. [Configuration](#configuration)
10. [Validation Models](#validation-models)
11. [Best Practices](#best-practices)

---

## Overview

**SurveyBot.Core** is the pure domain layer and heart of the application containing all business entities, domain logic, and contracts (interfaces). This is the **innermost layer** with **ZERO external dependencies**.

### Key Characteristics
- No dependencies on other projects
- No dependencies on Entity Framework
- No dependencies on Telegram.Bot
- Only .NET 8.0 base class libraries
- Defines contracts that other layers implement
- Contains pure domain logic

### Purpose
- Define business entities and their relationships
- Establish contracts (interfaces) for repositories and services
- Define Data Transfer Objects for API communication
- Specify domain exceptions for business rule violations
- Provide utilities for domain operations

### Target Framework
- **.NET 8.0** (`net8.0`)
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled

---

## Architecture Principles

### Dependency Rule

The Core project follows the **Dependency Inversion Principle**. All dependencies point **inward** toward Core:

```
┌─────────────────────────────────────┐
│         SurveyBot.API               │
│    (Presentation Layer)             │
└────────────┬────────────────────────┘
             │
             │ depends on
             │
┌────────────▼─────┐  ┌────────────────┐
│  Infrastructure  │  │   Bot Layer    │
│  (Data Access)   │  │  (Telegram)    │
└────────────┬─────┘  └────────┬───────┘
             │                  │
             │ depends on       │ depends on
             │                  │
             └──────────┬───────┘
                        │
                  ┌─────▼──────┐
                  │    CORE    │
                  │  (Domain)  │
                  │ NO DEPS!   │
                  └────────────┘
```

**Core NEVER depends on**:
- Infrastructure
- Bot
- API
- Entity Framework
- Any external libraries (except Microsoft.Extensions.*)

---

## Project Structure

### Complete File Listing

```
SurveyBot.Core/
├── Entities/                        # Domain entities (business objects)
│   ├── BaseEntity.cs                # Base class for all entities
│   ├── User.cs                      # Telegram user entity
│   ├── Survey.cs                    # Survey entity with code
│   ├── Question.cs                  # Survey question entity
│   ├── QuestionType.cs              # Question type enumeration
│   ├── Response.cs                  # Survey response entity
│   └── Answer.cs                    # Individual answer entity
├── Interfaces/                      # Repository and service contracts
│   ├── IRepository.cs               # Generic repository interface
│   ├── ISurveyRepository.cs         # Survey-specific queries
│   ├── IQuestionRepository.cs       # Question-specific queries
│   ├── IResponseRepository.cs       # Response-specific queries
│   ├── IUserRepository.cs           # User-specific queries
│   ├── IAnswerRepository.cs         # Answer-specific queries
│   ├── ISurveyService.cs            # Survey business logic
│   ├── IQuestionService.cs          # Question business logic
│   ├── IResponseService.cs          # Response business logic
│   ├── IUserService.cs              # User business logic
│   └── IAuthService.cs              # Authentication logic
├── DTOs/                            # Data Transfer Objects
│   ├── Survey/
│   │   ├── CreateSurveyDto.cs       # Survey creation request
│   │   ├── UpdateSurveyDto.cs       # Survey update request
│   │   ├── SurveyDto.cs             # Full survey response
│   │   ├── SurveyListDto.cs         # Survey list item
│   │   └── ToggleSurveyStatusDto.cs # Toggle status request
│   ├── Question/
│   │   ├── QuestionDto.cs           # Full question response
│   │   ├── CreateQuestionDto.cs     # Question creation request
│   │   ├── UpdateQuestionDto.cs     # Question update request
│   │   └── ReorderQuestionsDto.cs   # Reordering request
│   ├── Response/
│   │   ├── ResponseDto.cs           # Full response details
│   │   ├── CreateResponseDto.cs     # Start response request
│   │   ├── ResponseListDto.cs       # Response list item
│   │   ├── SubmitAnswerDto.cs       # Submit single answer
│   │   └── CompleteResponseDto.cs   # Complete response request
│   ├── Answer/
│   │   ├── AnswerDto.cs             # Answer response
│   │   └── CreateAnswerDto.cs       # Answer creation request
│   ├── User/
│   │   ├── UserDto.cs               # User profile
│   │   ├── UpdateUserDto.cs         # Update profile request
│   │   ├── UserWithTokenDto.cs      # User + JWT token
│   │   ├── LoginDto.cs              # Login request (legacy)
│   │   ├── RegisterDto.cs           # Register request (legacy)
│   │   ├── TokenResponseDto.cs      # Token response (legacy)
│   │   └── RefreshTokenDto.cs       # Refresh token request
│   ├── Auth/
│   │   ├── LoginRequestDto.cs       # Current login request
│   │   ├── LoginResponseDto.cs      # Current login response
│   │   └── RefreshTokenRequestDto.cs # Current refresh request
│   ├── Statistics/
│   │   ├── SurveyStatisticsDto.cs   # Overall survey stats
│   │   ├── QuestionStatisticsDto.cs # Per-question stats
│   │   ├── ChoiceStatisticsDto.cs   # Choice distribution
│   │   ├── RatingStatisticsDto.cs   # Rating statistics
│   │   ├── RatingDistributionDto.cs # Rating breakdown
│   │   └── TextStatisticsDto.cs     # Text answer stats
│   └── Common/
│       ├── PagedResultDto.cs        # Paginated results wrapper
│       ├── PaginationQueryDto.cs    # Pagination parameters
│       └── ExportFormatDto.cs       # Export format specification
├── Exceptions/                      # Domain-specific exceptions
│   ├── SurveyNotFoundException.cs   # Survey not found
│   ├── QuestionNotFoundException.cs # Question not found
│   ├── ResponseNotFoundException.cs # Response not found
│   ├── SurveyValidationException.cs # Survey validation error
│   ├── QuestionValidationException.cs # Question validation error
│   ├── InvalidQuestionTypeException.cs # Invalid question type
│   ├── SurveyOperationException.cs  # Operation not allowed
│   ├── InvalidAnswerFormatException.cs # Answer format error
│   ├── DuplicateResponseException.cs # Duplicate response attempt
│   └── UnauthorizedAccessException.cs # Authorization failure
├── Utilities/                       # Domain utilities
│   └── SurveyCodeGenerator.cs       # Generate unique survey codes
├── Configuration/                   # Configuration models
│   └── JwtSettings.cs               # JWT authentication settings
├── Models/                          # Domain models (non-entities)
│   └── ValidationResult.cs          # Validation result container
└── SurveyBot.Core.csproj            # Project file
```

### Lines of Code Estimate
- **Entities**: ~400 lines
- **Interfaces**: ~800 lines
- **DTOs**: ~1200 lines
- **Exceptions**: ~300 lines
- **Utilities**: ~100 lines
- **Total**: ~2800 lines of domain logic

---

## Entities

All entities inherit from `BaseEntity` (line 1-20 of BaseEntity.cs).

### BaseEntity
**File**: `Entities/BaseEntity.cs`

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Properties**:
- `Id` (int, PK) - Auto-increment primary key
- `CreatedAt` (DateTime) - UTC timestamp of creation
- `UpdatedAt` (DateTime) - UTC timestamp of last update

**Important**: Timestamps are automatically managed by `SurveyBotDbContext.SaveChangesAsync()`.

---

### User Entity
**File**: `Entities/User.cs` (lines 1-45)

Represents a Telegram user registered in the system.

**Properties**:
- `TelegramId` (long, unique, required) - Telegram's user ID
- `Username` (string?, max 255) - Telegram username without @
- `FirstName` (string?, max 255) - User's first name
- `LastName` (string?, max 255) - User's last name
- `LastLoginAt` (DateTime?) - Last login timestamp

**Navigation Properties**:
- `Surveys` (ICollection<Survey>) - Surveys created by this user

**Key Points**:
- `TelegramId` is the external identifier (Telegram's ID)
- `Id` is the internal database primary key
- Username can be null (not all Telegram users have one)
- `LastLoginAt` is updated on each login via `IUserRepository.UpdateLastLoginAsync()`

**Business Rules**:
- `TelegramId` must be unique across all users
- Cannot delete user if they have active surveys with responses

---

### Survey Entity
**File**: `Entities/Survey.cs` (lines 1-69)

Represents a survey with metadata and configuration.

**Properties**:
- `Title` (string, required, max 500) - Survey title
- `Description` (string?) - Optional description
- `Code` (string?, max 10) - **NEW**: Unique 6-character code for sharing
- `CreatorId` (int, FK to User, required) - Survey creator
- `IsActive` (bool, default: true) - Accept responses?
- `AllowMultipleResponses` (bool, default: false) - Allow duplicate responses?
- `ShowResults` (bool, default: true) - Show results to respondents?

**Navigation Properties**:
- `Creator` (User, required) - User who created the survey
- `Questions` (ICollection<Question>) - Survey questions
- `Responses` (ICollection<Response>) - Survey responses

**Survey Code Feature** (Added line 23-26):
- Auto-generated 6-character alphanumeric code (e.g., "A3X9K2")
- Used for easy survey sharing: `/survey A3X9K2`
- Generated by `SurveyCodeGenerator.GenerateUniqueCodeAsync()`
- Validated with `SurveyCodeGenerator.IsValidCode(code)`

**Business Rules**:
- Must have at least one question to activate
- Cannot modify title/description if active with responses
- Soft delete if has responses, hard delete otherwise
- Code must be unique across all surveys

**State Diagram**:
```
[Created] --AddQuestions--> [Has Questions] --Activate--> [Active]
                                                             │
                                                             ▼
[Inactive] <--Deactivate-- [Active] <--Responses--> [Active+Responses]
```

---

### Question Entity
**File**: `Entities/Question.cs` (lines 1-58)

Represents a question within a survey.

**Properties**:
- `SurveyId` (int, FK, required) - Parent survey
- `QuestionText` (string, required, max 1000) - Question content
- `QuestionType` (QuestionType enum, required) - Type of question
- `OrderIndex` (int, required, 0-based) - Display order
- `IsRequired` (bool, default: true) - Must be answered?
- `OptionsJson` (string?, JSONB) - JSON array for choice-based questions

**Navigation Properties**:
- `Survey` (Survey, required) - Parent survey
- `Answers` (ICollection<Answer>) - Answers to this question

**QuestionType Enum** (File: `Entities/QuestionType.cs`, lines 1-26):
```csharp
public enum QuestionType
{
    Text = 0,           // Free text input
    MultipleChoice = 1, // Multiple selections allowed
    SingleChoice = 2,   // Single selection only
    YesNo = 3,          // Binary choice
    Rating = 4          // Numeric rating scale
}
```

**OptionsJson Format**:

**For MultipleChoice/SingleChoice** (JSON array):
```json
["Option 1", "Option 2", "Option 3", "Option 4"]
```

**For Rating** (JSON object):
```json
{"min": 1, "max": 5}
```

**For Text/YesNo**: `OptionsJson` is null.

**Business Rules**:
- Choice questions (MultipleChoice/SingleChoice) must have at least 2 options
- Rating questions must specify min/max range
- OrderIndex determines display sequence (0-based)
- Cannot delete if has answers and survey is active

---

### Response Entity
**File**: `Entities/Response.cs` (lines 1-50)

Represents a user's response to a survey (submission session).

**Properties**:
- `SurveyId` (int, FK, required) - Target survey
- `RespondentTelegramId` (long, required) - Respondent's Telegram ID
- `IsComplete` (bool, default: false) - Response completed?
- `StartedAt` (DateTime?) - When response was started
- `SubmittedAt` (DateTime?) - When response was completed

**Navigation Properties**:
- `Survey` (Survey, required) - Target survey
- `Answers` (ICollection<Answer>) - Individual answers

**Important**:
- `RespondentTelegramId` is **NOT** a foreign key (allows anonymous responses)
- Does **NOT** inherit timestamps from BaseEntity (has custom fields)
- One Response per survey per user (unless `AllowMultipleResponses` is true)

**Response States**:
```
Started: StartedAt set, IsComplete = false, SubmittedAt = null
Completed: StartedAt set, IsComplete = true, SubmittedAt set
```

**Business Rules**:
- Cannot submit duplicate responses unless survey allows it
- Can only answer questions from the parent survey
- Must answer all required questions before completing

---

### Answer Entity
**File**: `Entities/Answer.cs` (lines 1-40)

Represents an answer to a specific question within a response.

**Properties**:
- `ResponseId` (int, FK, required) - Parent response
- `QuestionId` (int, FK, required) - Question being answered
- `AnswerJson` (string, JSONB, required) - Answer data as JSON
- `CreatedAt` (DateTime, required) - When answer was submitted

**Navigation Properties**:
- `Response` (Response, required) - Parent response
- `Question` (Question, required) - Question being answered

**AnswerJson Format by QuestionType**:

**Text**:
```json
{"text": "User's free text answer"}
```

**SingleChoice**:
```json
{"selectedOption": "Option 2"}
```

**MultipleChoice**:
```json
{"selectedOptions": ["Option 1", "Option 3", "Option 5"]}
```

**YesNo**:
```json
{"value": true}
```

**Rating**:
```json
{"rating": 4}
```

**Business Rules**:
- Answer format must match question type
- Unique constraint: One answer per (ResponseId, QuestionId)
- Validated by `AnswerValidator` in Bot layer

---

## Interfaces

### Repository Interfaces

#### IRepository<T>
**File**: `Interfaces/IRepository.cs` (lines 1-48)

**Generic base repository interface** for all entities.

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
```

**Method Signatures**:
- `GetByIdAsync(int id)` - Returns entity or null if not found
- `GetAllAsync()` - Returns all entities (use with caution!)
- `AddAsync(T entity)` - Adds entity, returns created entity with ID
- `UpdateAsync(T entity)` - Updates entity, returns updated entity
- `DeleteAsync(int id)` - Deletes entity by ID (throws if not found)
- `ExistsAsync(int id)` - Checks if entity exists

**Usage Pattern**:
```csharp
var survey = await _repository.GetByIdAsync(surveyId);
if (survey == null)
    throw new SurveyNotFoundException(surveyId);
```

---

#### ISurveyRepository
**File**: `Interfaces/ISurveyRepository.cs` (lines 1-86)

**Extends**: `IRepository<Survey>`

**Additional Methods**:

```csharp
// With navigation properties
Task<Survey?> GetByIdWithQuestionsAsync(int id);
Task<Survey?> GetByIdWithDetailsAsync(int id);

// Query methods
Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId);
Task<IEnumerable<Survey>> GetActiveSurveysAsync();
Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm);

// Operations
Task<bool> ToggleActiveStatusAsync(int id);

// Statistics
Task<int> GetResponseCountAsync(int surveyId);
Task<bool> HasResponsesAsync(int surveyId);

// Survey Code methods (NEW)
Task<Survey?> GetByCodeAsync(string code);
Task<Survey?> GetByCodeWithQuestionsAsync(string code);
Task<bool> CodeExistsAsync(string code);
```

**Key Methods**:

1. **GetByIdWithQuestionsAsync** - Includes questions ordered by OrderIndex
2. **GetByIdWithDetailsAsync** - Includes questions AND responses with answers
3. **GetByCodeAsync** - Retrieves survey by 6-character code
4. **CodeExistsAsync** - Checks if code is already taken (for uniqueness)

---

#### IQuestionRepository
**File**: `Interfaces/IQuestionRepository.cs` (lines 1-65)

**Extends**: `IRepository<Question>`

**Additional Methods**:

```csharp
Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId);
Task<Question?> GetByIdWithSurveyAsync(int id);
Task<int> GetMaxOrderIndexAsync(int surveyId);
Task ReorderQuestionsAsync(int surveyId, Dictionary<int, int> newOrders);
```

**Usage - Reorder Questions**:
```csharp
var newOrders = new Dictionary<int, int>
{
    { questionId1, 0 },  // Move to first position
    { questionId2, 1 },  // Move to second position
    { questionId3, 2 }   // Move to third position
};
await _questionRepository.ReorderQuestionsAsync(surveyId, newOrders);
```

---

#### IResponseRepository
**File**: `Interfaces/IResponseRepository.cs` (lines 1-60)

**Extends**: `IRepository<Response>`

**Additional Methods**:

```csharp
Task<IEnumerable<Response>> GetBySurveyIdAsync(int surveyId);
Task<Response?> GetByRespondentAsync(long telegramId, int surveyId);
Task<Response?> GetIncompleteResponseAsync(long telegramId, int surveyId);
Task<IEnumerable<Response>> GetCompletedResponsesAsync(int surveyId);
Task<Response?> GetResponseWithAnswersAsync(int responseId);
Task<int> GetCompletedCountAsync(int surveyId);
```

**Key Methods**:

1. **GetIncompleteResponseAsync** - Find in-progress response for resuming
2. **GetResponseWithAnswersAsync** - Includes all answers for display/analysis

---

#### IUserRepository
**File**: `Interfaces/IUserRepository.cs` (lines 1-45)

**Extends**: `IRepository<User>`

**Additional Methods**:

```csharp
Task<User?> GetByTelegramIdAsync(long telegramId);
Task<User> CreateOrUpdateFromTelegramAsync(Telegram.Bot.Types.User telegramUser);
Task UpdateLastLoginAsync(int userId);
```

**CreateOrUpdateFromTelegramAsync** - Upsert pattern:
- If user with TelegramId exists → Update name fields
- If user doesn't exist → Create new user
- Returns the user entity

---

#### IAnswerRepository
**File**: `Interfaces/IAnswerRepository.cs` (lines 1-40)

**Extends**: `IRepository<Answer>`

**Additional Methods**:

```csharp
Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId);
Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
Task<Answer?> GetAnswerAsync(int responseId, int questionId);
```

---

### Service Interfaces

#### ISurveyService
**File**: `Interfaces/ISurveyService.cs` (lines 1-109)

**Survey management business logic**

**Methods**:

```csharp
// CRUD Operations
Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto);
Task<SurveyDto> UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto);
Task<bool> DeleteSurveyAsync(int surveyId, int userId);
Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId);
Task<PagedResultDto<SurveyListDto>> GetAllSurveysAsync(int userId, PaginationQueryDto query);

// Status Management
Task<SurveyDto> ActivateSurveyAsync(int surveyId, int userId);
Task<SurveyDto> DeactivateSurveyAsync(int surveyId, int userId);

// Statistics
Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int surveyId, int userId);

// Authorization
Task<bool> UserOwnsSurveyAsync(int surveyId, int userId);

// Public Access (NEW)
Task<SurveyDto> GetSurveyByCodeAsync(string code);
```

**Key Business Rules**:
- All methods except `GetSurveyByCodeAsync()` require authorization
- Cannot activate survey without questions
- Cannot modify active survey with responses
- Soft delete if survey has responses

---

#### IQuestionService
**File**: `Interfaces/IQuestionService.cs` (lines 1-75)

**Question management business logic**

**Methods**:

```csharp
Task<QuestionDto> AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto);
Task<QuestionDto> UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto);
Task DeleteQuestionAsync(int questionId, int userId);
Task<QuestionDto> GetQuestionAsync(int questionId, int userId);
Task<IEnumerable<QuestionDto>> GetSurveyQuestionsAsync(int surveyId, int userId);
Task ReorderQuestionsAsync(int surveyId, int userId, ReorderQuestionsDto dto);
```

**Key Business Rules**:
- OrderIndex automatically assigned on creation (max + 1)
- Choice questions must have at least 2 options
- Cannot delete question if survey has responses
- Reordering validates all question IDs belong to survey

---

#### IResponseService
**File**: `Interfaces/IResponseService.cs` (lines 1-60)

**Response management business logic**

**Methods**:

```csharp
Task<ResponseDto> StartResponseAsync(long telegramId, int surveyId);
Task<AnswerDto> SubmitAnswerAsync(int responseId, int questionId, SubmitAnswerDto dto);
Task<ResponseDto> CompleteResponseAsync(int responseId, CompleteResponseDto dto);
Task<ResponseDto> GetResponseAsync(int responseId, int userId);
Task<PagedResultDto<ResponseListDto>> GetSurveyResponsesAsync(int surveyId, int userId, PaginationQueryDto query);
```

**Response Flow**:
1. **Start** → Creates Response record (IsComplete=false)
2. **Submit Answers** → Creates Answer records linked to Response
3. **Complete** → Marks Response as complete (IsComplete=true)

**Key Business Rules**:
- Prevents duplicate responses (unless survey allows)
- Validates answer format matches question type
- Requires all required questions answered before completing

---

#### IUserService
**File**: `Interfaces/IUserService.cs` (lines 1-50)

**User management business logic**

**Methods**:

```csharp
Task<UserDto> GetOrCreateUserAsync(long telegramId, string? username, string? firstName, string? lastName);
Task<UserDto> GetUserByIdAsync(int userId);
Task<UserDto> GetUserByTelegramIdAsync(long telegramId);
Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto dto);
```

**GetOrCreateUserAsync** - Upsert pattern for Telegram integration.

---

#### IAuthService
**File**: `Interfaces/IAuthService.cs` (lines 1-40)

**Authentication business logic**

**Methods**:

```csharp
Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request);
Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
string GenerateJwtToken(User user);
```

**Authentication Flow**:
1. User sends Telegram credentials
2. Service validates and creates/updates User record
3. Generates JWT token with claims
4. Returns token + user info

---

## Data Transfer Objects

### DTO Naming Conventions

- **CreateXxxDto** - For POST requests (creation)
- **UpdateXxxDto** - For PUT/PATCH requests (updates)
- **XxxDto** - For responses (full details)
- **XxxListDto** - For list responses (summary data)

---

### Survey DTOs

#### CreateSurveyDto
**File**: `DTOs/Survey/CreateSurveyDto.cs` (lines 1-25)

```csharp
public class CreateSurveyDto
{
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool AllowMultipleResponses { get; set; } = false;
    public bool ShowResults { get; set; } = true;
}
```

**Note**: `Code` is auto-generated, `IsActive` defaults to false.

---

#### UpdateSurveyDto
**File**: `DTOs/Survey/UpdateSurveyDto.cs` (lines 1-20)

```csharp
public class UpdateSurveyDto
{
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }
}
```

---

#### SurveyDto
**File**: `DTOs/Survey/SurveyDto.cs` (lines 1-60)

**Full survey details with questions**

```csharp
public class SurveyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }  // NEW: Survey code

    public int CreatorId { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;

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

---

#### SurveyListDto
**File**: `DTOs/Survey/SurveyListDto.cs` (lines 1-35)

**Survey summary for lists**

```csharp
public class SurveyListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Code { get; set; }  // NEW: Survey code

    public bool IsActive { get; set; }

    public int QuestionCount { get; set; }
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

### Question DTOs

#### CreateQuestionDto
**File**: `DTOs/Question/CreateQuestionDto.cs` (lines 1-30)

```csharp
public class CreateQuestionDto
{
    [Required, MaxLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public QuestionType QuestionType { get; set; }

    public bool IsRequired { get; set; } = true;

    public List<string>? Options { get; set; }  // For choice-based questions
}
```

**Validation**:
- If QuestionType is MultipleChoice/SingleChoice, Options must have ≥2 items
- Options ignored for Text/YesNo/Rating

---

#### QuestionDto
**File**: `DTOs/Question/QuestionDto.cs` (lines 1-40)

```csharp
public class QuestionDto
{
    public int Id { get; set; }
    public int SurveyId { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }

    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; }

    public List<string>? Options { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

### Response DTOs

#### ResponseDto
**File**: `DTOs/Response/ResponseDto.cs` (lines 1-50)

```csharp
public class ResponseDto
{
    public int Id { get; set; }

    public int SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;

    public long RespondentTelegramId { get; set; }
    public bool IsComplete { get; set; }

    public int AnsweredCount { get; set; }
    public int TotalQuestions { get; set; }

    public List<AnswerDto> Answers { get; set; } = new();

    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
```

---

#### SubmitAnswerDto
**File**: `DTOs/Response/SubmitAnswerDto.cs` (lines 1-30)

```csharp
public class SubmitAnswerDto
{
    // For Text questions
    public string? TextAnswer { get; set; }

    // For SingleChoice/YesNo questions
    public string? SelectedOption { get; set; }

    // For MultipleChoice questions
    public List<string>? SelectedOptions { get; set; }

    // For Rating questions
    public int? RatingValue { get; set; }
}
```

**Important**: Service layer validates that exactly ONE field is populated based on question type.

---

### Answer DTOs

#### AnswerDto
**File**: `DTOs/Answer/AnswerDto.cs` (lines 1-40)

```csharp
public class AnswerDto
{
    public int Id { get; set; }

    public int ResponseId { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    // Parsed answer (varies by question type)
    public string? TextAnswer { get; set; }
    public string? SelectedOption { get; set; }
    public List<string>? SelectedOptions { get; set; }
    public int? RatingValue { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

### Statistics DTOs

#### SurveyStatisticsDto
**File**: `DTOs/Statistics/SurveyStatisticsDto.cs` (lines 1-60)

```csharp
public class SurveyStatisticsDto
{
    public int SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;

    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
    public int IncompleteResponses { get; set; }

    public double CompletionRate { get; set; }  // Percentage
    public int UniqueRespondents { get; set; }

    public double? AverageCompletionTime { get; set; }  // Seconds

    public DateTime? FirstResponseAt { get; set; }
    public DateTime? LastResponseAt { get; set; }

    public List<QuestionStatisticsDto>? QuestionStatistics { get; set; }
}
```

---

#### QuestionStatisticsDto
**File**: `DTOs/Statistics/QuestionStatisticsDto.cs` (lines 1-50)

```csharp
public class QuestionStatisticsDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }

    public int TotalAnswers { get; set; }
    public int SkippedCount { get; set; }
    public double ResponseRate { get; set; }  // Percentage

    // Type-specific statistics (only one will be populated)
    public Dictionary<string, ChoiceStatisticsDto>? ChoiceDistribution { get; set; }
    public RatingStatisticsDto? RatingStatistics { get; set; }
    public TextStatisticsDto? TextStatistics { get; set; }
}
```

---

#### ChoiceStatisticsDto
**File**: `DTOs/Statistics/ChoiceStatisticsDto.cs` (lines 1-20)

```csharp
public class ChoiceStatisticsDto
{
    public string Option { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
```

---

#### RatingStatisticsDto
**File**: `DTOs/Statistics/RatingStatisticsDto.cs` (lines 1-30)

```csharp
public class RatingStatisticsDto
{
    public double AverageRating { get; set; }
    public int MinRating { get; set; }
    public int MaxRating { get; set; }

    public Dictionary<int, RatingDistributionDto>? Distribution { get; set; }
}
```

---

#### TextStatisticsDto
**File**: `DTOs/Statistics/TextStatisticsDto.cs` (lines 1-25)

```csharp
public class TextStatisticsDto
{
    public int TotalAnswers { get; set; }
    public double AverageLength { get; set; }
    public int MinLength { get; set; }
    public int MaxLength { get; set; }
}
```

---

### Common DTOs

#### PagedResultDto<T>
**File**: `DTOs/Common/PagedResultDto.cs` (lines 1-40)

```csharp
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

---

#### PaginationQueryDto
**File**: `DTOs/Common/PaginationQueryDto.cs` (lines 1-30)

```csharp
public class PaginationQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
```

---

## Exceptions

All custom exceptions are in `SurveyBot.Core.Exceptions` namespace.

### Exception Hierarchy

```
Exception
└── SurveyBotException (base)
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

### Exception Definitions

#### SurveyNotFoundException
**File**: `Exceptions/SurveyNotFoundException.cs` (lines 1-20)

```csharp
public class SurveyNotFoundException : Exception
{
    public SurveyNotFoundException(int surveyId)
        : base($"Survey with ID {surveyId} was not found.")
    {
        SurveyId = surveyId;
    }

    public int SurveyId { get; }
}
```

**When to throw**: Survey not found by ID or code

---

#### QuestionNotFoundException
**File**: `Exceptions/QuestionNotFoundException.cs` (lines 1-20)

**When to throw**: Question not found by ID

---

#### ResponseNotFoundException
**File**: `Exceptions/ResponseNotFoundException.cs` (lines 1-20)

**When to throw**: Response not found by ID

---

#### SurveyValidationException
**File**: `Exceptions/SurveyValidationException.cs` (lines 1-15)

```csharp
public class SurveyValidationException : Exception
{
    public SurveyValidationException(string message) : base(message) { }
}
```

**When to throw**:
- Title too short/long
- Activating survey without questions
- Invalid survey configuration

---

#### QuestionValidationException
**File**: `Exceptions/QuestionValidationException.cs` (lines 1-15)

**When to throw**:
- Choice question with < 2 options
- Invalid rating range
- Question text too long

---

#### SurveyOperationException
**File**: `Exceptions/SurveyOperationException.cs` (lines 1-15)

**When to throw**:
- Modifying active survey with responses
- Deleting survey with active responses
- Invalid state transition

---

#### InvalidAnswerFormatException
**File**: `Exceptions/InvalidAnswerFormatException.cs` (lines 1-15)

**When to throw**:
- Answer JSON doesn't match question type
- Missing required answer fields
- Invalid rating value

---

#### DuplicateResponseException
**File**: `Exceptions/DuplicateResponseException.cs` (lines 1-15)

**When to throw**:
- User tries to submit multiple responses when not allowed

---

#### UnauthorizedAccessException
**File**: `Exceptions/UnauthorizedAccessException.cs` (lines 1-25)

```csharp
public class UnauthorizedAccessException : Exception
{
    public UnauthorizedAccessException(int userId, string resource, int resourceId)
        : base($"User {userId} is not authorized to access {resource} {resourceId}.")
    {
        UserId = userId;
        Resource = resource;
        ResourceId = resourceId;
    }

    public int UserId { get; }
    public string Resource { get; }
    public int ResourceId { get; }
}
```

**When to throw**:
- User tries to access/modify survey they don't own
- User tries to view responses for someone else's survey

---

## Utilities

### SurveyCodeGenerator
**File**: `Utilities/SurveyCodeGenerator.cs` (lines 1-73)

**Purpose**: Generate unique, URL-safe survey codes for easy sharing.

**Features**:
- Generates 6-character alphanumeric codes (Base36: A-Z, 0-9)
- Cryptographically secure random generation
- Uniqueness validation via callback
- Format validation

**Public Methods**:

```csharp
// Generate random code (may not be unique)
public static string GenerateCode()

// Generate unique code with existence check
public static async Task<string> GenerateUniqueCodeAsync(
    Func<string, Task<bool>> codeExistsAsync,
    int maxAttempts = 10)

// Validate code format
public static bool IsValidCode(string? code)
```

**Usage Example**:
```csharp
// In SurveyService
var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
    _surveyRepository.CodeExistsAsync);
// Returns: "A3X9K2" (example)
```

**Algorithm**:
1. Use `RandomNumberGenerator` for crypto-secure randomness
2. Select 6 characters from "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
3. Check uniqueness via provided callback
4. Retry up to `maxAttempts` if collision occurs
5. Throw exception if unable to generate unique code

**Collision Probability**: With 36^6 = 2.2 billion possible codes, collision is extremely rare.

---

## Configuration

### JwtSettings
**File**: `Configuration/JwtSettings.cs` (lines 1-30)

**Purpose**: JWT authentication configuration model.

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;  // Min 32 chars
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
```

**Configuration** (in `appsettings.json`):
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpirationMinutes": 60
  }
}
```

**Security Requirements**:
- `SecretKey` must be at least 32 characters for HS256
- Store securely (environment variables in production)
- Rotate regularly

---

## Validation Models

### ValidationResult
**File**: `Models/ValidationResult.cs` (lines 1-30)

**Purpose**: Container for validation results.

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
```

**Usage**:
```csharp
var result = ValidationResult.Success();

if (string.IsNullOrWhiteSpace(dto.Title))
{
    result = ValidationResult.Failure("Title is required");
}

if (!result.IsValid)
{
    throw new ValidationException(string.Join(", ", result.Errors));
}
```

---

## Best Practices

### For Core Layer Development

#### 1. Keep Core Pure
✅ **DO**:
- Define business entities
- Create interfaces for contracts
- Define DTOs for data transfer
- Create domain exceptions
- Add domain utilities (like SurveyCodeGenerator)

❌ **DON'T**:
- Reference Infrastructure, Bot, or API
- Add Entity Framework attributes (use Fluent API in Infrastructure)
- Add Telegram.Bot types
- Add HTTP-specific logic
- Add database-specific code

---

#### 2. Entity Design

✅ **DO**:
```csharp
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public User Creator { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
```

❌ **DON'T**:
```csharp
// Don't add EF-specific attributes beyond basic data annotations
[Index(nameof(Code), IsUnique = true)]  // This belongs in Infrastructure
```

---

#### 3. DTO Design

✅ **DO**:
- Separate DTOs for Create, Update, and Response
- Use data annotations for validation
- Include all necessary fields
- Use nullable types appropriately

```csharp
public class CreateSurveyDto
{
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}
```

❌ **DON'T**:
- Expose entities directly through API
- Use same DTO for Create and Update
- Include navigation properties in DTOs

---

#### 4. Interface Design

✅ **DO**:
- Use specific return types (not object)
- Document expected behavior with XML comments
- Define async methods with CancellationToken
- Keep interfaces focused (SRP)

```csharp
/// <summary>
/// Gets a survey by its unique code.
/// </summary>
/// <param name="code">The 6-character survey code.</param>
/// <returns>The survey if found and active, otherwise null.</returns>
Task<Survey?> GetByCodeAsync(string code);
```

❌ **DON'T**:
- Create "God" interfaces with too many methods
- Use synchronous methods
- Return untyped collections

---

#### 5. Exception Design

✅ **DO**:
- Create specific exceptions for domain errors
- Include helpful error messages
- Add relevant properties (e.g., SurveyId)
- Document when exceptions are thrown

```csharp
public class SurveyNotFoundException : Exception
{
    public SurveyNotFoundException(int surveyId)
        : base($"Survey with ID {surveyId} was not found.")
    {
        SurveyId = surveyId;
    }

    public int SurveyId { get; }
}
```

❌ **DON'T**:
- Catch exceptions in Core (let them bubble)
- Use exceptions for control flow
- Create generic exceptions

---

#### 6. Naming Conventions

**Entities**: Singular nouns
- `Survey`, `Question`, `User`

**DTOs**: Entity name + purpose
- `CreateSurveyDto`, `SurveyListDto`, `SurveyStatisticsDto`

**Repositories**: `IEntityRepository`
- `ISurveyRepository`, `IQuestionRepository`

**Services**: `IEntityService`
- `ISurveyService`, `IAuthService`

**Exceptions**: `EntityProblemException`
- `SurveyNotFoundException`, `SurveyValidationException`

---

## Entity Relationships

### Relationship Diagram

```
┌─────────┐         ┌─────────┐
│  User   │1 ────< │ Survey  │
└─────────┘ creates └────┬────┘
                         │
                         │1
                         │
                         │
                    ┌────▼────┐
                    │Question │
                    └────┬────┘
                         │*
                         │
                         │
                    ┌────▼────┐
                  * │ Answer  │
         ┌──────────┤         │
         │          └─────────┘
         │
    ┌────▼────┐
    │Response │
    └─────────┘
```

### Cascade Delete Behavior

**Survey → Questions**: CASCADE
- Delete survey → Delete all questions

**Survey → Responses**: CASCADE
- Delete survey → Delete all responses

**Question → Answers**: CASCADE
- Delete question → Delete all answers

**Response → Answers**: CASCADE
- Delete response → Delete all answers

**User → Surveys**: RESTRICT
- Cannot delete user with surveys (prevent orphaned surveys)

---

## Key Files Reference

### Most Important Files

1. **Survey.cs** (lines 1-69)
   - Core entity with survey code feature

2. **ISurveyRepository.cs** (lines 1-86)
   - Survey repository contract with code methods

3. **ISurveyService.cs** (lines 1-109)
   - Survey service contract with public code access

4. **SurveyCodeGenerator.cs** (lines 1-73)
   - Unique code generation utility

5. **SurveyDto.cs** (lines 1-60)
   - Full survey response with code

6. **QuestionType.cs** (lines 1-26)
   - Question type enumeration

7. **IRepository.cs** (lines 1-48)
   - Generic repository base interface

8. **PagedResultDto.cs** (lines 1-40)
   - Pagination wrapper

---

## Testing Considerations

### Unit Testing Core

**Test Entity Validation**:
```csharp
[Fact]
public void Survey_Title_CannotBeEmpty()
{
    var survey = new Survey { Title = "" };
    // Validation should fail
}
```

**Test DTO Validation**:
```csharp
[Fact]
public void CreateSurveyDto_RequiresTitle()
{
    var dto = new CreateSurveyDto();
    var validationResults = Validate(dto);
    Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(dto.Title)));
}
```

**Test Exception Throwing**:
```csharp
[Fact]
public void SurveyNotFoundException_IncludesSurveyId()
{
    var exception = new SurveyNotFoundException(123);
    Assert.Equal(123, exception.SurveyId);
    Assert.Contains("123", exception.Message);
}
```

**Test Survey Code Generation**:
```csharp
[Fact]
public void SurveyCodeGenerator_GeneratesValidCode()
{
    var code = SurveyCodeGenerator.GenerateCode();
    Assert.Equal(6, code.Length);
    Assert.True(SurveyCodeGenerator.IsValidCode(code));
}

[Fact]
public async Task SurveyCodeGenerator_GeneratesUniqueCode()
{
    var existingCodes = new HashSet<string>();
    var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
        async c => existingCodes.Contains(c));
    Assert.False(existingCodes.Contains(code));
}
```

---

## Summary

**SurveyBot.Core** is the stable, dependency-free heart of the application. It:

✅ Defines **8 domain entities** with clear relationships
✅ Declares **11 interface contracts** for repositories and services
✅ Provides **40+ DTOs** for data transfer
✅ Specifies **10 domain exceptions** for business rules
✅ Includes **SurveyCodeGenerator** utility for unique code generation
✅ Contains **ZERO external dependencies** (pure .NET 8.0)

**Remember**: Core defines **WHAT** the application does, not **HOW** it does it. Implementation details belong in Infrastructure and Bot layers.

---

**Version**: 1.1.0 (with Survey Code feature)
**Last Updated**: 2025-11-10
**Total Files**: 60+
**Total Lines**: ~2800
