# SurveyBot.Core - Domain Layer Documentation

## Overview

**SurveyBot.Core** is the domain layer and heart of the application. It contains all business entities, domain logic, interfaces (contracts), and has **zero dependencies** on other projects. This is the most stable layer that other projects depend on.

## Architecture Principle

**Dependency Rule**: The Core project should never reference Infrastructure, Bot, or API projects. All dependencies point inward toward Core.

```
API → Infrastructure → Core
API → Bot → Core
```

## Project Structure

```
SurveyBot.Core/
├── Entities/              # Domain entities (business objects)
│   ├── BaseEntity.cs
│   ├── User.cs
│   ├── Survey.cs
│   ├── Question.cs
│   ├── QuestionType.cs (enum)
│   ├── Response.cs
│   └── Answer.cs
├── Interfaces/            # Repository and service contracts
│   ├── IRepository.cs
│   ├── ISurveyRepository.cs
│   ├── IQuestionRepository.cs
│   ├── IResponseRepository.cs
│   ├── IUserRepository.cs
│   ├── IAnswerRepository.cs
│   ├── ISurveyService.cs
│   ├── IQuestionService.cs
│   ├── IResponseService.cs
│   ├── IUserService.cs
│   └── IAuthService.cs
├── DTOs/                  # Data Transfer Objects
│   ├── Survey/
│   │   ├── SurveyDto.cs
│   │   ├── SurveyListDto.cs
│   │   ├── CreateSurveyDto.cs
│   │   ├── UpdateSurveyDto.cs
│   │   └── ToggleSurveyStatusDto.cs
│   ├── Question/
│   │   ├── QuestionDto.cs
│   │   ├── CreateQuestionDto.cs
│   │   ├── UpdateQuestionDto.cs
│   │   └── ReorderQuestionsDto.cs
│   ├── Response/
│   │   ├── ResponseDto.cs
│   │   ├── ResponseListDto.cs
│   │   ├── CreateResponseDto.cs
│   │   ├── SubmitAnswerDto.cs
│   │   └── CompleteResponseDto.cs
│   ├── Answer/
│   │   ├── AnswerDto.cs
│   │   └── CreateAnswerDto.cs
│   ├── User/
│   │   ├── UserDto.cs
│   │   ├── UpdateUserDto.cs
│   │   ├── LoginDto.cs
│   │   ├── RegisterDto.cs
│   │   ├── TokenResponseDto.cs
│   │   ├── RefreshTokenDto.cs
│   │   └── UserWithTokenDto.cs
│   ├── Auth/
│   │   ├── LoginRequestDto.cs
│   │   ├── LoginResponseDto.cs
│   │   └── RefreshTokenRequestDto.cs
│   ├── Statistics/
│   │   ├── SurveyStatisticsDto.cs
│   │   ├── QuestionStatisticsDto.cs
│   │   ├── ChoiceStatisticsDto.cs
│   │   ├── RatingStatisticsDto.cs
│   │   ├── RatingDistributionDto.cs
│   │   └── TextStatisticsDto.cs
│   └── Common/
│       ├── PagedResultDto.cs
│       ├── PaginationQueryDto.cs
│       └── ExportFormatDto.cs
├── Exceptions/            # Domain-specific exceptions
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
├── Configuration/         # Configuration models
│   └── JwtSettings.cs
├── Models/                # Domain models (non-entities)
│   └── ValidationResult.cs
└── Utilities/             # Utility classes
    └── SurveyCodeGenerator.cs
```

---

## Domain Entities

All entities inherit from `BaseEntity` which provides:
- `int Id` (Primary Key)
- `DateTime CreatedAt`
- `DateTime UpdatedAt`

### BaseEntity

**Location**: `Entities/BaseEntity.cs` (lines 6-22)

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Note**: CreatedAt and UpdatedAt are automatically managed by `SurveyBotDbContext.SaveChangesAsync()`.

### User Entity

**Location**: `Entities/User.cs` (lines 8-47)

Represents a Telegram user in the system.

**Properties**:
- `TelegramId` (long, unique, required) - Telegram's unique user ID
- `Username` (string?, max 255) - Telegram username without @
- `FirstName` (string?, max 255)
- `LastName` (string?, max 255)
- `LastLoginAt` (DateTime?) - Last login timestamp, updated during authentication

**Navigation Properties**:
- `Surveys` (ICollection<Survey>) - Surveys created by this user

**Key Points**:
- `TelegramId` is the external identifier from Telegram API (immutable)
- `Id` is internal database primary key (auto-increment)
- `Username` can be null (not all Telegram users have usernames)
- Unique index on `TelegramId` ensures one user per Telegram account

### Survey Entity

**Location**: `Entities/Survey.cs` (lines 8-68)

Represents a survey with metadata and configuration.

**Properties**:
- `Title` (string, required, max 500 chars)
- `Description` (string?, nullable)
- `Code` (string?, max 10 chars) - **NEW** Unique 6-character alphanumeric code for sharing
- `CreatorId` (int, FK to User, required)
- `IsActive` (bool, required, default: true) - Whether survey accepts responses
- `AllowMultipleResponses` (bool, required, default: false) - Allow same user to respond multiple times
- `ShowResults` (bool, required, default: true) - Show results to respondents after submission

**Navigation Properties**:
- `Creator` (User) - User who created the survey
- `Questions` (ICollection<Question>) - Survey questions (ordered by OrderIndex)
- `Responses` (ICollection<Response>) - Survey responses

**Business Rules**:
- Must have at least one question to be activated
- Cannot modify title/description/questions if active with responses (must deactivate first)
- Soft delete if has responses (set IsActive=false), hard delete otherwise
- Survey code is generated automatically on creation and is immutable
- Code format: 6 uppercase alphanumeric characters (e.g., "A3X9K2")

**Survey States**:
1. **Inactive with no questions**: Just created, editing allowed
2. **Inactive with questions**: Ready to activate, editing allowed if no responses
3. **Active**: Accepting responses, limited editing (can deactivate)
4. **Deactivated**: No longer accepting responses, can reactivate if no structural changes needed

### Question Entity

**Location**: `Entities/Question.cs` (lines 8-58)

Represents a question within a survey.

**Properties**:
- `SurveyId` (int, FK, required)
- `QuestionText` (string, required)
- `QuestionType` (QuestionType enum, required)
- `OrderIndex` (int, required, 0-based) - Display order within survey
- `IsRequired` (bool, required, default: true) - Whether answer is mandatory
- `OptionsJson` (string?, JSONB) - JSON array for choice-based questions

**Navigation Properties**:
- `Survey` (Survey)
- `Answers` (ICollection<Answer>) - Answers to this question across all responses

**QuestionType Enum** (`Entities/QuestionType.cs`, lines 6-27):
- `Text` (0) - Free-form text input
- `SingleChoice` (1) - Single selection from options (radio button)
- `MultipleChoice` (2) - Multiple selections from options (checkboxes)
- `Rating` (3) - Numeric rating scale (1-5)

**OptionsJson Format**:

For **SingleChoice** and **MultipleChoice**:
```json
["Option 1", "Option 2", "Option 3", "Option 4"]
```

For **Rating** (stored as metadata, not options):
```json
{"min": 1, "max": 5}
```

For **Text**: `null` (no options needed)

**Validation Rules**:
- Choice questions (Single/Multiple) must have at least 2 options
- Rating questions must have min < max
- Text questions should not have options
- `OrderIndex` must be unique within survey and sequential (0, 1, 2, ...)

### Response Entity

**Location**: `Entities/Response.cs` (lines 8-55)

Represents a user's response to a survey.

**Properties**:
- `SurveyId` (int, FK, required)
- `RespondentTelegramId` (long, required) - **NOT a foreign key** to allow anonymous responses
- `IsComplete` (bool, required, default: false)
- `StartedAt` (DateTime?) - When user started responding
- `SubmittedAt` (DateTime?) - When user completed and submitted

**Navigation Properties**:
- `Survey` (Survey)
- `Answers` (ICollection<Answer>) - Answers within this response

**Important**:
- Response does NOT inherit from `BaseEntity`, so no `CreatedAt`/`UpdatedAt`
- Uses custom timestamp fields: `StartedAt` and `SubmittedAt`
- `RespondentTelegramId` is NOT a FK to allow responses from non-registered users

**Response Lifecycle**:
1. **Started**: `StartedAt` set, `IsComplete = false`, `SubmittedAt = null`
2. **In Progress**: User answering questions, answers being added
3. **Completed**: `SubmittedAt` set, `IsComplete = true`

**Business Rules**:
- If `AllowMultipleResponses = false`, check for existing complete response before creating new one
- Can have incomplete responses (user abandoned survey)
- Once complete, cannot modify answers

### Answer Entity

**Location**: `Entities/Answer.cs` (lines 8-55)

Represents an answer to a specific question within a response.

**Properties**:
- `ResponseId` (int, FK, required)
- `QuestionId` (int, FK, required)
- `AnswerText` (string?, nullable) - For text questions
- `AnswerJson` (string?, JSONB) - For structured answers (choice, rating)
- `CreatedAt` (DateTime, required) - When answer was submitted

**Navigation Properties**:
- `Response` (Response)
- `Question` (Question)

**Important**:
- Answer does NOT inherit from `BaseEntity`
- Has its own `CreatedAt` field
- Unique constraint on (`ResponseId`, `QuestionId`) - one answer per question per response

**AnswerJson Format by Question Type**:

**Text** (uses `AnswerText` field, `AnswerJson` is null):
```
AnswerText: "This is the user's text answer"
```

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

## Interfaces

### Repository Interfaces

All repository interfaces extend the base `IRepository<T>` interface.

#### IRepository<T>

**Location**: `Interfaces/IRepository.cs`

Generic repository base interface providing common CRUD operations:

```csharp
Task<T?> GetByIdAsync(int id);
Task<IEnumerable<T>> GetAllAsync();
Task<T> AddAsync(T entity);
Task<T> UpdateAsync(T entity);
Task DeleteAsync(int id);
Task<bool> ExistsAsync(int id);
Task<T> CreateAsync(T entity); // Alias for AddAsync
```

**Returns**:
- `null` for Get operations when not found
- Entity for Add/Update operations
- `bool` for Exists operation

#### ISurveyRepository

**Location**: `Interfaces/ISurveyRepository.cs` (lines 8-86)

Extends `IRepository<Survey>` with survey-specific query methods:

**Query Methods**:
- `GetByIdWithQuestionsAsync(int id)` - Include questions ordered by OrderIndex
- `GetByIdWithDetailsAsync(int id)` - Include questions, responses, and answers
- `GetByCreatorIdAsync(int creatorId)` - All surveys by a user
- `GetActiveSurveysAsync()` - Only active surveys
- `SearchByTitleAsync(string searchTerm)` - Case-insensitive title search

**Survey Code Methods** (NEW):
- `GetByCodeAsync(string code)` - Find survey by unique code
- `GetByCodeWithQuestionsAsync(string code)` - Find by code with questions included
- `CodeExistsAsync(string code)` - Check if code already exists (for uniqueness)

**Operation Methods**:
- `ToggleActiveStatusAsync(int id)` - Toggle IsActive flag
- `GetResponseCountAsync(int surveyId)` - Count all responses
- `HasResponsesAsync(int surveyId)` - Check if any responses exist

**Notes**:
- All async methods follow TAP (Task-based Asynchronous Pattern)
- Survey codes are case-insensitive (stored uppercase, compared uppercase)

#### IQuestionRepository

**Location**: `Interfaces/IQuestionRepository.cs`

Extends `IRepository<Question>`:

- `GetBySurveyIdAsync(int surveyId)` - All questions for a survey, ordered by OrderIndex
- `GetByIdWithSurveyAsync(int id)` - Question with parent survey
- `GetMaxOrderIndexAsync(int surveyId)` - Highest OrderIndex for survey (for appending)
- `ReorderQuestionsAsync(int surveyId, Dictionary<int, int> newOrders)` - Batch update OrderIndex

#### IResponseRepository

**Location**: `Interfaces/IResponseRepository.cs`

Extends `IRepository<Response>`:

- `GetBySurveyIdAsync(int surveyId)` - All responses for a survey
- `GetByRespondentAsync(long telegramId, int surveyId)` - User's response(s) to a survey
- `GetIncompleteResponseAsync(long telegramId, int surveyId)` - In-progress response
- `GetCompletedResponsesAsync(int surveyId)` - Only completed responses
- `GetCompletedCountAsync(int surveyId)` - Count of completed responses
- `GetResponseWithAnswersAsync(int responseId)` - Response with all answers and questions

#### IUserRepository

**Location**: `Interfaces/IUserRepository.cs`

Extends `IRepository<User>`:

- `GetByTelegramIdAsync(long telegramId)` - Find user by Telegram ID
- `CreateOrUpdateAsync(long telegramId, string? username, string? firstName, string? lastName)` - Upsert user from Telegram data
- `UpdateLastLoginAsync(int userId)` - Update LastLoginAt timestamp

#### IAnswerRepository

**Location**: `Interfaces/IAnswerRepository.cs`

Extends `IRepository<Answer>`:

- `GetByResponseIdAsync(int responseId)` - All answers for a response
- `GetByQuestionIdAsync(int questionId)` - All answers for a question (across responses)
- `GetAnswerAsync(int responseId, int questionId)` - Specific answer for question in response

---

### Service Interfaces

Service interfaces define business logic operations. Services use repositories for data access and enforce business rules.

#### ISurveyService

**Location**: `Interfaces/ISurveyService.cs` (lines 10-108)

Survey business logic operations:

**CRUD Operations**:
- `CreateSurveyAsync(int userId, CreateSurveyDto dto)` - Create new survey (inactive by default, generates unique code)
- `UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)` - Update survey (checks ownership and modification rules)
- `DeleteSurveyAsync(int surveyId, int userId)` - Soft/hard delete based on responses
- `GetSurveyByIdAsync(int surveyId, int userId)` - Get with authorization check
- `GetAllSurveysAsync(int userId, PaginationQueryDto query)` - Paginated list with filters

**Status Operations**:
- `ActivateSurveyAsync(int surveyId, int userId)` - Make survey active (requires at least 1 question)
- `DeactivateSurveyAsync(int surveyId, int userId)` - Stop accepting responses

**Analytics**:
- `GetSurveyStatisticsAsync(int surveyId, int userId)` - Comprehensive statistics with question breakdowns

**Authorization**:
- `UserOwnsSurveyAsync(int surveyId, int userId)` - Check ownership

**Public Access** (NEW):
- `GetSurveyByCodeAsync(string code)` - Get survey by code (no auth required, only returns active surveys)

**Exceptions Thrown**:
- `SurveyNotFoundException` - Survey doesn't exist
- `UnauthorizedAccessException` - User doesn't own survey
- `SurveyValidationException` - Validation failed (e.g., empty title, activating without questions)
- `SurveyOperationException` - Operation not allowed (e.g., modifying active survey with responses)

#### IQuestionService

**Location**: `Interfaces/IQuestionService.cs`

Question business logic operations:

- `AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto)` - Add question (auto-assigns OrderIndex)
- `UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto)` - Update question text/options
- `DeleteQuestionAsync(int questionId, int userId)` - Remove question (reorders remaining)
- `GetQuestionAsync(int questionId, int userId)` - Get with ownership check
- `GetSurveyQuestionsAsync(int surveyId, int userId)` - All questions ordered
- `ReorderQuestionsAsync(int surveyId, int userId, ReorderQuestionsDto dto)` - Change question order

**Validation**:
- Choice questions must have ≥ 2 options
- Options cannot be empty
- Cannot modify questions if survey is active with responses

#### IResponseService

**Location**: `Interfaces/IResponseService.cs`

Response business logic operations:

- `StartResponseAsync(long telegramId, int surveyId)` - Create new response (checks duplicate rules)
- `SubmitAnswerAsync(int responseId, int questionId, SubmitAnswerDto dto)` - Save answer
- `CompleteResponseAsync(int responseId, CompleteResponseDto dto)` - Mark response complete
- `GetResponseAsync(int responseId, int userId)` - Get response details (owner only)
- `GetSurveyResponsesAsync(int surveyId, int userId, PaginationQueryDto query)` - All responses for survey

**Business Rules**:
- Validates answer format matches question type
- Enforces `AllowMultipleResponses` setting
- Cannot submit answers to inactive survey
- Cannot modify completed response

#### IUserService

**Location**: `Interfaces/IUserService.cs`

User management operations:

- `GetOrCreateUserAsync(long telegramId, string? username, string? firstName, string? lastName)` - Upsert user
- `GetUserByIdAsync(int userId)` - Get by internal ID
- `GetUserByTelegramIdAsync(long telegramId)` - Get by Telegram ID
- `UpdateUserAsync(int userId, UpdateUserDto dto)` - Update profile

#### IAuthService

**Location**: `Interfaces/IAuthService.cs`

Authentication and JWT token operations:

- `AuthenticateAsync(LoginRequestDto request)` - Login with Telegram credentials, returns JWT
- `RefreshTokenAsync(RefreshTokenRequestDto request)` - Refresh expired JWT
- `GenerateJwtToken(User user)` - Create JWT token for user

**JWT Claims**:
- `NameIdentifier` - User.Id (internal ID)
- `Name` - User.Username or TelegramId
- `telegram_id` - User.TelegramId

---

## Data Transfer Objects (DTOs)

DTOs are used for API communication. They do NOT contain navigation properties or database-specific fields.

### Naming Conventions

- `CreateXxxDto` - For POST requests (creation)
- `UpdateXxxDto` - For PUT/PATCH requests (updates)
- `XxxDto` - For responses (full details)
- `XxxListDto` - For list responses (summary data)

### Survey DTOs

**CreateSurveyDto** (`DTOs/Survey/CreateSurveyDto.cs`):
```csharp
string Title { get; set; }              // Required, 3-500 chars
string? Description { get; set; }       // Optional, max 2000 chars
bool AllowMultipleResponses { get; set; } = false
bool ShowResults { get; set; } = true
```

**UpdateSurveyDto** (`DTOs/Survey/UpdateSurveyDto.cs`):
```csharp
string Title { get; set; }              // Required
string? Description { get; set; }
bool AllowMultipleResponses { get; set; }
bool ShowResults { get; set; }
```

**SurveyDto** (`DTOs/Survey/SurveyDto.cs`) - Full details:
```csharp
int Id
string Title
string? Description
string? Code                            // NEW: Survey share code
int CreatorId
string? CreatorUsername
bool IsActive
bool AllowMultipleResponses
bool ShowResults
List<QuestionDto> Questions
int TotalResponses
int CompletedResponses
DateTime CreatedAt
DateTime UpdatedAt
```

**SurveyListDto** (`DTOs/Survey/SurveyListDto.cs`) - Summary for lists:
```csharp
int Id
string Title
string? Code                            // NEW
bool IsActive
int QuestionCount
int TotalResponses                      // NEW: renamed from ResponseCount
int CompletedResponses                  // NEW
DateTime CreatedAt
```

### Question DTOs

**CreateQuestionDto** (`DTOs/Question/CreateQuestionDto.cs`):
```csharp
string QuestionText { get; set; }       // Required
QuestionType QuestionType { get; set; }
bool IsRequired { get; set; } = true
List<string>? Options { get; set; }     // For choice questions
```

**UpdateQuestionDto** (`DTOs/Question/UpdateQuestionDto.cs`):
```csharp
string? QuestionText { get; set; }
bool? IsRequired { get; set; }
List<string>? Options { get; set; }
```

**QuestionDto** (`DTOs/Question/QuestionDto.cs`):
```csharp
int Id
int SurveyId
string QuestionText
QuestionType QuestionType
int OrderIndex
bool IsRequired
List<string>? Options
DateTime CreatedAt
```

**ReorderQuestionsDto** (`DTOs/Question/ReorderQuestionsDto.cs`):
```csharp
Dictionary<int, int> QuestionOrders { get; set; }  // QuestionId -> New OrderIndex
```

### Response DTOs

**CreateResponseDto** (`DTOs/Response/CreateResponseDto.cs`):
```csharp
int SurveyId { get; set; }
long RespondentTelegramId { get; set; }
```

**SubmitAnswerDto** (`DTOs/Response/SubmitAnswerDto.cs`):
```csharp
string? TextAnswer { get; set; }                    // For Text questions
string? SelectedOption { get; set; }                // For SingleChoice
List<string>? SelectedOptions { get; set; }         // For MultipleChoice
int? RatingValue { get; set; }                      // For Rating
```

**CompleteResponseDto** (`DTOs/Response/CompleteResponseDto.cs`):
```csharp
// Empty DTO - just signals completion
```

**ResponseDto** (`DTOs/Response/ResponseDto.cs`):
```csharp
int Id
int SurveyId
string SurveyTitle
long RespondentTelegramId
bool IsComplete
int AnsweredCount                       // Calculated
int TotalQuestions                      // Calculated
double ProgressPercentage               // Calculated: AnsweredCount / TotalQuestions * 100
List<AnswerDto> Answers
DateTime? StartedAt
DateTime? SubmittedAt
```

**ResponseListDto** (`DTOs/Response/ResponseListDto.cs`):
```csharp
int Id
int SurveyId
long RespondentTelegramId
bool IsComplete
int AnsweredCount
int TotalQuestions
DateTime? StartedAt
DateTime? SubmittedAt
```

### Answer DTOs

**AnswerDto** (`DTOs/Answer/AnswerDto.cs`):
```csharp
int Id
int ResponseId
int QuestionId
string QuestionText
QuestionType QuestionType
string? TextAnswer                      // For Text
string? SelectedOption                  // For SingleChoice
List<string>? SelectedOptions           // For MultipleChoice
int? RatingValue                        // For Rating
DateTime CreatedAt
```

**CreateAnswerDto** (`DTOs/Answer/CreateAnswerDto.cs`):
```csharp
int QuestionId { get; set; }
string? TextAnswer { get; set; }
string? SelectedOption { get; set; }
List<string>? SelectedOptions { get; set; }
int? RatingValue { get; set; }
```

### Statistics DTOs

**SurveyStatisticsDto** (`DTOs/Statistics/SurveyStatisticsDto.cs`):
```csharp
int SurveyId
string Title
int TotalResponses
int CompletedResponses
int IncompleteResponses
double CompletionRate                   // Percentage
int UniqueRespondents                   // Distinct Telegram IDs
double? AverageCompletionTime           // Seconds
DateTime? FirstResponseAt
DateTime? LastResponseAt
List<QuestionStatisticsDto> QuestionStatistics
```

**QuestionStatisticsDto** (`DTOs/Statistics/QuestionStatisticsDto.cs`):
```csharp
int QuestionId
string QuestionText
QuestionType QuestionType
int TotalAnswers
int SkippedCount
double ResponseRate                     // Percentage

// Type-specific statistics (populated based on QuestionType)
Dictionary<string, ChoiceStatisticsDto>? ChoiceDistribution     // For Single/MultipleChoice
RatingStatisticsDto? RatingStatistics                           // For Rating
TextStatisticsDto? TextStatistics                               // For Text
```

**ChoiceStatisticsDto** (`DTOs/Statistics/ChoiceStatisticsDto.cs`):
```csharp
string Option
int Count
double Percentage
```

**RatingStatisticsDto** (`DTOs/Statistics/RatingStatisticsDto.cs`):
```csharp
double AverageRating
int MinRating
int MaxRating
Dictionary<int, RatingDistributionDto> Distribution  // Rating value -> count/percentage
```

**RatingDistributionDto** (`DTOs/Statistics/RatingDistributionDto.cs`):
```csharp
int Rating
int Count
double Percentage
```

**TextStatisticsDto** (`DTOs/Statistics/TextStatisticsDto.cs`):
```csharp
int TotalAnswers
double AverageLength                    // Character count
int MinLength
int MaxLength
```

### Common DTOs

**PagedResultDto<T>** (`DTOs/Common/PagedResultDto.cs`):
```csharp
List<T> Items { get; set; }
int TotalCount { get; set; }
int PageNumber { get; set; }
int PageSize { get; set; }
int TotalPages { get; set; }            // Calculated: Ceiling(TotalCount / PageSize)
bool HasPreviousPage { get; set; }      // Calculated: PageNumber > 1
bool HasNextPage { get; set; }          // Calculated: PageNumber < TotalPages
```

**PaginationQueryDto** (`DTOs/Common/PaginationQueryDto.cs`):
```csharp
int PageNumber { get; set; } = 1         // Min: 1
int PageSize { get; set; } = 10          // Min: 1, Max: 100
string? SearchTerm { get; set; }
string? SortBy { get; set; }
bool SortDescending { get; set; } = false
```

### User DTOs

**UserDto** (`DTOs/User/UserDto.cs`):
```csharp
int Id
long TelegramId
string? Username
string? FirstName
string? LastName
DateTime? LastLoginAt
DateTime CreatedAt
DateTime UpdatedAt
```

**UpdateUserDto** (`DTOs/User/UpdateUserDto.cs`):
```csharp
string? Username { get; set; }
string? FirstName { get; set; }
string? LastName { get; set; }
```

### Auth DTOs

**LoginRequestDto** (`DTOs/Auth/LoginRequestDto.cs`):
```csharp
long TelegramId { get; set; }
string? Username { get; set; }
string? FirstName { get; set; }
string? LastName { get; set; }
```

**LoginResponseDto** (`DTOs/Auth/LoginResponseDto.cs`):
```csharp
string AccessToken { get; set; }
string RefreshToken { get; set; }
int ExpiresIn { get; set; }             // Seconds
UserDto User { get; set; }
```

---

## Domain Exceptions

All custom exceptions inherit from `Exception` and are in the `SurveyBot.Core.Exceptions` namespace.

### Exception Hierarchy

```
Exception
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

**SurveyNotFoundException** (`Exceptions/SurveyNotFoundException.cs`):
- **When**: Survey with specified ID or code doesn't exist
- **Usage**: Repository returns null, service throws this exception
- **Constructor**: `SurveyNotFoundException(int surveyId)` or `SurveyNotFoundException(string message)`

**QuestionNotFoundException** (`Exceptions/QuestionNotFoundException.cs`):
- **When**: Question with specified ID doesn't exist

**ResponseNotFoundException** (`Exceptions/ResponseNotFoundException.cs`):
- **When**: Response with specified ID doesn't exist

**SurveyValidationException** (`Exceptions/SurveyValidationException.cs`):
- **When**: Survey validation fails
- **Examples**:
  - Empty or too short title
  - Activating survey without questions
  - Title exceeds 500 characters
  - Description exceeds 2000 characters

**QuestionValidationException** (`Exceptions/QuestionValidationException.cs`):
- **When**: Question validation fails
- **Examples**:
  - Choice question with < 2 options
  - Invalid rating range
  - Empty question text

**InvalidQuestionTypeException** (`Exceptions/InvalidQuestionTypeException.cs`):
- **When**: Question type is not supported or invalid

**SurveyOperationException** (`Exceptions/SurveyOperationException.cs`):
- **When**: Operation cannot be performed due to business rules
- **Examples**:
  - Modifying active survey with responses
  - Deleting survey with responses (should soft delete)

**InvalidAnswerFormatException** (`Exceptions/InvalidAnswerFormatException.cs`):
- **When**: Answer JSON doesn't match question type or is malformed

**DuplicateResponseException** (`Exceptions/DuplicateResponseException.cs`):
- **When**: User tries to submit multiple responses when `AllowMultipleResponses = false`

**UnauthorizedAccessException** (`Exceptions/UnauthorizedAccessException.cs`):
- **When**: User tries to access/modify resource they don't own
- **Constructor**: `UnauthorizedAccessException(int userId, string resource, int resourceId)`

### Exception Usage Pattern

```csharp
// In Service
public async Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId)
{
    var survey = await _surveyRepository.GetByIdAsync(surveyId);

    if (survey == null)
        throw new SurveyNotFoundException(surveyId);

    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException(userId, "Survey", surveyId);

    return _mapper.Map<SurveyDto>(survey);
}
```

---

## Configuration Models

### JwtSettings

**Location**: `Configuration/JwtSettings.cs`

Configuration model for JWT authentication loaded from `appsettings.json`:

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;      // Min 32 chars for HS256
    public string Issuer { get; set; } = string.Empty;         // "SurveyBot.API"
    public string Audience { get; set; } = string.Empty;       // "SurveyBot.Client"
    public int ExpirationMinutes { get; set; } = 60;           // Default: 1 hour
}
```

**appsettings.json Configuration**:
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long-for-hmac-sha256",
    "Issuer": "SurveyBot.API",
    "Audience": "SurveyBot.Client",
    "ExpirationMinutes": 60
  }
}
```

**Usage**:
```csharp
// In Startup/Program.cs
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// In AuthService
public AuthService(IOptions<JwtSettings> jwtSettings)
{
    _jwtSettings = jwtSettings.Value;
}
```

---

## Utility Classes

### SurveyCodeGenerator

**Location**: `Utilities/SurveyCodeGenerator.cs` (lines 8-72)

**NEW** Generates unique, URL-safe codes for surveys.

**Purpose**: Create short, memorable codes for sharing surveys (e.g., "A3X9K2").

**Key Features**:
- **6-character alphanumeric codes** (Base36: A-Z, 0-9)
- **Case-insensitive** (stored and compared as uppercase)
- **Cryptographically secure** random generation using `RandomNumberGenerator`
- **Collision detection** via database uniqueness check
- **Validation** method for code format

**Methods**:

1. **GenerateCode()** - Static method:
   ```csharp
   public static string GenerateCode()
   ```
   - Generates a random 6-character code
   - Uses `RandomNumberGenerator.GetInt32()` for secure randomness
   - Returns uppercase string (e.g., "K8F2X9")
   - Does NOT check for uniqueness

2. **GenerateUniqueCodeAsync()** - Static method:
   ```csharp
   public static async Task<string> GenerateUniqueCodeAsync(
       Func<string, Task<bool>> codeExistsAsync,
       int maxAttempts = 10)
   ```
   - Generates code and checks uniqueness via callback
   - Retries up to `maxAttempts` times if collision detected
   - Throws `InvalidOperationException` if unable to generate unique code after max attempts
   - **Usage**:
     ```csharp
     var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
         _surveyRepository.CodeExistsAsync);
     ```

3. **IsValidCode()** - Static method:
   ```csharp
   public static bool IsValidCode(string? code)
   ```
   - Validates code format (6 chars, alphanumeric)
   - Returns `true` if valid, `false` otherwise
   - Used for input validation

**Algorithm**:
- **Character set**: `ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789` (36 characters)
- **Total combinations**: 36^6 = 2,176,782,336 (over 2 billion unique codes)
- **Collision probability**: Extremely low, even with millions of surveys
- **Format**: Fixed 6-character length, uppercase only

**Example Usage**:
```csharp
// In SurveyService.CreateSurveyAsync()
var code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
    _surveyRepository.CodeExistsAsync);

survey.Code = code;
```

**Security Considerations**:
- Uses `RandomNumberGenerator` (cryptographically secure)
- Codes are non-sequential and unpredictable
- No personally identifiable information in codes

---

## Validation Models

### ValidationResult

**Location**: `Models/ValidationResult.cs`

Simple validation result model for business rule validation:

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
private ValidationResult ValidateQuestion(CreateQuestionDto dto)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(dto.QuestionText))
        errors.Add("Question text is required");

    if (dto.QuestionType == QuestionType.SingleChoice && (dto.Options == null || dto.Options.Count < 2))
        errors.Add("Single choice questions must have at least 2 options");

    return errors.Any()
        ? ValidationResult.Failure(errors.ToArray())
        : ValidationResult.Success();
}
```

---

## Best Practices for Core Layer

### 1. Keep Core Independent

**DO**:
- ✅ Define interfaces for all external dependencies
- ✅ Use only .NET standard libraries
- ✅ Keep entities simple and focused on domain logic

**DON'T**:
- ❌ Reference Infrastructure, Bot, or API projects
- ❌ Add external package dependencies (except basic .NET packages)
- ❌ Include framework-specific code (EF, ASP.NET)

### 2. Entity Design

**DO**:
- ✅ Inherit from `BaseEntity` for standard entities
- ✅ Use navigation properties for relationships
- ✅ Add data annotations for validation
- ✅ Override `ToString()` for debugging
- ✅ Use nullable reference types appropriately

**DON'T**:
- ❌ Add repository/service logic to entities
- ❌ Make navigation properties required (use null-forgiving operator `= null!`)
- ❌ Use computed properties that require database access

**Example**:
```csharp
public class Survey : BaseEntity
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    // Navigation property
    public User Creator { get; set; } = null!;  // null-forgiving operator

    public override string ToString() => $"Survey {Id}: {Title}";
}
```

### 3. DTO Design

**DO**:
- ✅ Keep DTOs simple POCOs (Plain Old CLR Objects)
- ✅ Separate DTOs for different purposes (Create, Update, Response)
- ✅ Use nullable types appropriately
- ✅ Add data annotations for validation
- ✅ Use `init` accessors for immutable DTOs

**DON'T**:
- ❌ Include navigation properties (use IDs instead)
- ❌ Add business logic to DTOs
- ❌ Reuse entity classes as DTOs

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

### 4. Interface Design

**DO**:
- ✅ Keep interfaces focused (Interface Segregation Principle)
- ✅ Use specific return types (avoid `object`)
- ✅ Document expected behavior with XML comments
- ✅ Define async methods with `CancellationToken` parameter
- ✅ Return `Task<T?>` for nullable results

**DON'T**:
- ❌ Create "god interfaces" with too many methods
- ❌ Add implementation details to interfaces
- ❌ Use `out` or `ref` parameters (use return types instead)

**Example**:
```csharp
/// <summary>
/// Repository interface for Survey entity with specific query methods.
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

### 5. Exception Design

**DO**:
- ✅ Create specific exceptions for domain errors
- ✅ Include helpful error messages
- ✅ Add constructors for different scenarios
- ✅ Inherit from `Exception` (not `ApplicationException`)

**DON'T**:
- ❌ Catch exceptions in Core layer (let them bubble up)
- ❌ Use exceptions for control flow
- ❌ Create generic exceptions

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

### 6. Naming Conventions

**Entities**: Singular nouns
- ✅ `Survey`, `Question`, `User`
- ❌ `Surveys`, `Questions`, `Users`

**DTOs**: Entity name + purpose suffix
- ✅ `CreateSurveyDto`, `SurveyDto`, `SurveyListDto`
- ❌ `SurveyCreate`, `SurveyModel`, `SurveyViewModel`

**Repositories**: `I{Entity}Repository`
- ✅ `ISurveyRepository`, `IUserRepository`
- ❌ `ISurveyRepo`, `SurveyDataAccess`

**Services**: `I{Entity}Service`
- ✅ `ISurveyService`, `IAuthService`
- ❌ `ISurveyManager`, `SurveyBusinessLogic`

**Async Methods**: End with `Async` suffix
- ✅ `GetByIdAsync`, `CreateSurveyAsync`
- ❌ `GetById`, `CreateSurvey` (for async methods)

---

## Key Relationships

### Entity Relationship Diagram

```
┌─────────┐
│  User   │
└────┬────┘
     │ 1
     │ creates
     │ *
┌────▼────┐
│ Survey  │◄─────────── Code (unique, 6 chars)
└────┬────┘
     │ 1
     │ contains
     │ *
┌────▼──────┐
│ Question  │
└────┬──────┘
     │ 1           1 ┌──────────┐
     │ answered by   │ Survey   │
     │ *         ┌──►│          │
┌────▼────┐     │   └──────────┘
│ Answer  │     │ *
└────┬────┘     │ receives
     │ *        │
     │ in       │ 1
     │ 1        │
┌────▼─────────┐
│  Response    │
└──────────────┘
     RespondentTelegramId (not FK)
```

### Cascade Delete Behavior

**User Deleted**:
- ❌ Surveys **remain** (FK constraint: `DeleteBehavior.Restrict`)
- Consider: Implement soft delete or reassign surveys

**Survey Deleted**:
- ✅ Questions **cascade delete**
- ✅ Responses **cascade delete**
  - ✅ Answers within responses also cascade delete

**Question Deleted**:
- ✅ Answers **cascade delete**

**Response Deleted**:
- ✅ Answers **cascade delete**

---

## Common Patterns

### Repository Method Patterns

**Get with Navigation Properties**:
```csharp
public async Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return await _dbSet
        .AsNoTracking()                              // Read-only
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))  // Ordered collection
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

**Get Collection with Filters**:
```csharp
public async Task<IEnumerable<Survey>> GetActiveSurveysAsync()
{
    return await _dbSet
        .Include(s => s.Creator)
        .Include(s => s.Questions)
        .Where(s => s.IsActive)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

### Service Method Patterns

**Service with Authorization Check**:
```csharp
public async Task<SurveyDto> GetSurveyAsync(int surveyId, int userId)
{
    // 1. Fetch entity
    var survey = await _surveyRepository.GetByIdAsync(surveyId);

    // 2. Check existence
    if (survey == null)
        throw new SurveyNotFoundException(surveyId);

    // 3. Check authorization
    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException(userId, "Survey", surveyId);

    // 4. Map to DTO and return
    return _mapper.Map<SurveyDto>(survey);
}
```

**Service with Validation**:
```csharp
public async Task<SurveyDto> CreateSurveyAsync(int userId, CreateSurveyDto dto)
{
    // 1. Validate input
    ValidateCreateSurveyDto(dto);

    // 2. Create entity
    var survey = _mapper.Map<Survey>(dto);
    survey.CreatorId = userId;
    survey.IsActive = false;

    // 3. Generate unique code
    survey.Code = await SurveyCodeGenerator.GenerateUniqueCodeAsync(
        _surveyRepository.CodeExistsAsync);

    // 4. Save to database
    var created = await _surveyRepository.CreateAsync(survey);

    // 5. Map to DTO and return
    return _mapper.Map<SurveyDto>(created);
}
```

---

## Testing Considerations

### Unit Testing Core

**What to Test**:
- ✅ Entity validation logic
- ✅ Exception throwing conditions
- ✅ DTO validation attributes
- ✅ Utility class logic (e.g., `SurveyCodeGenerator`)

**Mock Dependencies**:
- ✅ Mock repository interfaces
- ✅ Mock service interfaces
- ✅ Use test doubles (fakes, stubs, mocks)

**Example**:
```csharp
[Fact]
public async Task CreateSurvey_WithValidData_ThrowsException()
{
    // Arrange
    var mockRepo = new Mock<ISurveyRepository>();
    var service = new SurveyService(mockRepo.Object, ...);
    var dto = new CreateSurveyDto { Title = "AB" };  // Too short

    // Act & Assert
    await Assert.ThrowsAsync<SurveyValidationException>(
        () => service.CreateSurveyAsync(1, dto));
}
```

### Integration Testing

**What to Test**:
- ✅ Repository implementations with real database
- ✅ Service logic with repositories
- ✅ Complex queries and navigation properties
- ✅ Database constraints (unique indexes, FKs)

**Use**:
- ✅ In-memory database or test database
- ✅ Realistic test data
- ✅ Transaction rollback between tests

---

## Quick Reference

### File Locations

| Component | Location | Lines |
|-----------|----------|-------|
| BaseEntity | `Entities/BaseEntity.cs` | 6-22 |
| User | `Entities/User.cs` | 8-47 |
| Survey | `Entities/Survey.cs` | 8-68 |
| Question | `Entities/Question.cs` | 8-58 |
| QuestionType | `Entities/QuestionType.cs` | 6-27 |
| Response | `Entities/Response.cs` | 8-55 |
| Answer | `Entities/Answer.cs` | 8-55 |
| ISurveyRepository | `Interfaces/ISurveyRepository.cs` | 8-86 |
| ISurveyService | `Interfaces/ISurveyService.cs` | 10-108 |
| SurveyCodeGenerator | `Utilities/SurveyCodeGenerator.cs` | 8-72 |
| JwtSettings | `Configuration/JwtSettings.cs` | - |
| ValidationResult | `Models/ValidationResult.cs` | - |

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
```

---

## Summary

**Remember**: The Core layer defines **WHAT** the application does, not **HOW** it does it. Implementation details belong in Infrastructure and Bot layers.

**Core Responsibilities**:
1. ✅ Define domain entities and business rules
2. ✅ Define interfaces (contracts) for dependencies
3. ✅ Define DTOs for communication
4. ✅ Define domain exceptions
5. ✅ Provide utility classes for common operations

**Core Should NOT**:
1. ❌ Reference other projects (Infrastructure, Bot, API)
2. ❌ Contain database-specific code (EF configurations)
3. ❌ Contain HTTP/API-specific code
4. ❌ Contain Telegram bot-specific code

**Key Principle**: **Dependency Inversion** - High-level policy (Core) should not depend on low-level details (Infrastructure, Bot). Both should depend on abstractions (interfaces).

---

**Last Updated**: 2025-11-10
**Version**: 1.1.0 (Added Survey Code Generation)
**Target Framework**: .NET 8.0
