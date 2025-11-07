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
├── Interfaces/            # Repository and service contracts
├── DTOs/                  # Data Transfer Objects
│   ├── Survey/
│   ├── Question/
│   ├── Response/
│   ├── Answer/
│   ├── User/
│   ├── Auth/
│   ├── Statistics/
│   └── Common/
├── Exceptions/            # Domain-specific exceptions
├── Configuration/         # Configuration models
└── Models/                # Domain models (non-entities)
```

---

## Domain Entities

All entities inherit from `BaseEntity` which provides:
- `int Id` (Primary Key)
- `DateTime CreatedAt`
- `DateTime UpdatedAt`

### User Entity
Represents a Telegram user in the system.

**Properties**:
- `TelegramId` (long, unique) - Telegram's unique user ID
- `Username` (string?) - Telegram username without @
- `FirstName` (string?)
- `LastName` (string?)
- `LastLoginAt` (DateTime?) - Last login timestamp

**Navigation Properties**:
- `Surveys` (ICollection<Survey>) - Surveys created by this user

**Key Points**:
- TelegramId is the external identifier from Telegram API
- Id is internal database primary key
- Username can be null (not all Telegram users have one)
- Navigation to surveys enables querying user's created surveys

### Survey Entity
Represents a survey with metadata and configuration.

**Properties**:
- `Title` (string, required, max 500 chars)
- `Description` (string?)
- `CreatorId` (int, FK to User)
- `IsActive` (bool, default: true) - Whether survey accepts responses
- `AllowMultipleResponses` (bool, default: false)
- `ShowResults` (bool, default: true)

**Navigation Properties**:
- `Creator` (User) - User who created the survey
- `Questions` (ICollection<Question>) - Survey questions
- `Responses` (ICollection<Response>) - Survey responses

**Business Rules**:
- Must have at least one question to be activated
- Cannot modify if active with responses
- Soft delete if has responses, hard delete otherwise

### Question Entity
Represents a question within a survey.

**Properties**:
- `SurveyId` (int, FK)
- `QuestionText` (string, required)
- `QuestionType` (QuestionType enum)
- `OrderIndex` (int, 0-based) - Display order
- `IsRequired` (bool, default: true)
- `OptionsJson` (string?, JSONB) - JSON array for choice-based questions

**Navigation Properties**:
- `Survey` (Survey)
- `Answers` (ICollection<Answer>)

**QuestionType Enum**:
- `Text` (0) - Free text input
- `MultipleChoice` (1) - Multiple selections allowed
- `SingleChoice` (2) - Single selection only
- `YesNo` (3) - Binary choice
- `Rating` (4) - Numeric rating scale

**OptionsJson Format**:
For choice-based questions (MultipleChoice, SingleChoice):
```json
["Option 1", "Option 2", "Option 3"]
```

For Rating questions (stored in format):
```json
{"min": 1, "max": 5}
```

### Response Entity
Represents a user's response to a survey.

**Properties**:
- `SurveyId` (int, FK)
- `RespondentTelegramId` (long) - NOT a foreign key to allow anonymous responses
- `IsComplete` (bool, default: false)
- `StartedAt` (DateTime?)
- `SubmittedAt` (DateTime?)

**Navigation Properties**:
- `Survey` (Survey)
- `Answers` (ICollection<Answer>)

**Important**: Response does NOT have a created/updated timestamp from BaseEntity. It has custom `StartedAt` and `SubmittedAt` fields.

**States**:
- Started: `StartedAt` set, `IsComplete = false`
- Completed: `SubmittedAt` set, `IsComplete = true`

### Answer Entity
Represents an answer to a specific question within a response.

**Properties**:
- `ResponseId` (int, FK)
- `QuestionId` (int, FK)
- `AnswerJson` (string, JSONB) - Stores answer data
- `CreatedAt` (DateTime) - When answer was submitted

**Navigation Properties**:
- `Response` (Response)
- `Question` (Question)

**AnswerJson Format by Question Type**:

**Text**:
```json
{"text": "User's answer"}
```

**SingleChoice**:
```json
{"selectedOption": "Option 2"}
```

**MultipleChoice**:
```json
{"selectedOptions": ["Option 1", "Option 3"]}
```

**YesNo**:
```json
{"value": true}
```

**Rating**:
```json
{"rating": 4}
```

---

## Interfaces

### Repository Interfaces

**IRepository<T>** - Generic repository base:
```csharp
Task<T?> GetByIdAsync(int id);
Task<IEnumerable<T>> GetAllAsync();
Task<T> AddAsync(T entity);
Task<T> UpdateAsync(T entity);
Task DeleteAsync(int id);
Task<bool> ExistsAsync(int id);
```

**ISurveyRepository** extends `IRepository<Survey>`:
- `GetByIdWithQuestionsAsync(int id)` - Include questions
- `GetByIdWithDetailsAsync(int id)` - Include questions and responses
- `GetByCreatorIdAsync(int creatorId)` - User's surveys
- `GetActiveSurveysAsync()` - Only active surveys
- `ToggleActiveStatusAsync(int id)` - Toggle IsActive
- `SearchByTitleAsync(string searchTerm)` - Search by title
- `GetResponseCountAsync(int surveyId)` - Count responses
- `HasResponsesAsync(int surveyId)` - Check if has responses

**IQuestionRepository** extends `IRepository<Question>`:
- `GetBySurveyIdAsync(int surveyId)` - All questions for survey
- `GetByIdWithSurveyAsync(int id)` - Include parent survey
- `GetMaxOrderIndexAsync(int surveyId)` - Highest order index
- `ReorderQuestionsAsync(int surveyId, Dictionary<int, int> newOrders)` - Update order

**IResponseRepository** extends `IRepository<Response>`:
- `GetBySurveyIdAsync(int surveyId)` - All responses for survey
- `GetByRespondentAsync(long telegramId, int surveyId)` - User's response
- `GetIncompleteResponseAsync(long telegramId, int surveyId)` - In-progress response
- `GetCompletedResponsesAsync(int surveyId)` - Only completed
- `GetResponseWithAnswersAsync(int responseId)` - Include answers

**IUserRepository** extends `IRepository<User>`:
- `GetByTelegramIdAsync(long telegramId)` - Find by Telegram ID
- `CreateOrUpdateFromTelegramAsync(TelegramUser)` - Upsert from Telegram data
- `UpdateLastLoginAsync(int userId)` - Update last login timestamp

**IAnswerRepository** extends `IRepository<Answer>`:
- `GetByResponseIdAsync(int responseId)` - All answers for response
- `GetByQuestionIdAsync(int questionId)` - All answers for question
- `GetAnswerAsync(int responseId, int questionId)` - Specific answer

### Service Interfaces

**ISurveyService** - Survey business logic:
- `CreateSurveyAsync(int userId, CreateSurveyDto dto)`
- `UpdateSurveyAsync(int surveyId, int userId, UpdateSurveyDto dto)`
- `DeleteSurveyAsync(int surveyId, int userId)`
- `GetSurveyByIdAsync(int surveyId, int userId)`
- `GetAllSurveysAsync(int userId, PaginationQueryDto query)`
- `ActivateSurveyAsync(int surveyId, int userId)`
- `DeactivateSurveyAsync(int surveyId, int userId)`
- `GetSurveyStatisticsAsync(int surveyId, int userId)`
- `UserOwnsSurveyAsync(int surveyId, int userId)`

**IQuestionService** - Question business logic:
- `AddQuestionAsync(int surveyId, int userId, CreateQuestionDto dto)`
- `UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto)`
- `DeleteQuestionAsync(int questionId, int userId)`
- `GetQuestionAsync(int questionId, int userId)`
- `GetSurveyQuestionsAsync(int surveyId, int userId)`
- `ReorderQuestionsAsync(int surveyId, int userId, ReorderQuestionsDto dto)`

**IResponseService** - Response business logic:
- `StartResponseAsync(long telegramId, int surveyId)`
- `SubmitAnswerAsync(int responseId, int questionId, SubmitAnswerDto dto)`
- `CompleteResponseAsync(int responseId, CompleteResponseDto dto)`
- `GetResponseAsync(int responseId, int userId)`
- `GetSurveyResponsesAsync(int surveyId, int userId, PaginationQueryDto query)`

**IUserService** - User business logic:
- `GetOrCreateUserAsync(long telegramId, string? username, string? firstName, string? lastName)`
- `GetUserByIdAsync(int userId)`
- `GetUserByTelegramIdAsync(long telegramId)`
- `UpdateUserAsync(int userId, UpdateUserDto dto)`

**IAuthService** - Authentication:
- `AuthenticateAsync(LoginRequestDto request)`
- `RefreshTokenAsync(RefreshTokenRequestDto request)`
- `GenerateJwtToken(User user)`

---

## Data Transfer Objects (DTOs)

### DTO Naming Conventions
- `CreateXxxDto` - For POST requests (creation)
- `UpdateXxxDto` - For PUT requests (updates)
- `XxxDto` - For responses (full details)
- `XxxListDto` - For list responses (summary data)

### Survey DTOs

**CreateSurveyDto**:
```csharp
string Title { get; set; }
string? Description { get; set; }
bool AllowMultipleResponses { get; set; }
bool ShowResults { get; set; }
```

**UpdateSurveyDto**:
```csharp
string? Title { get; set; }
string? Description { get; set; }
bool? AllowMultipleResponses { get; set; }
bool? ShowResults { get; set; }
```

**SurveyDto** (full details):
```csharp
int Id { get; set; }
string Title { get; set; }
string? Description { get; set; }
int CreatorId { get; set; }
string CreatorUsername { get; set; }
bool IsActive { get; set; }
bool AllowMultipleResponses { get; set; }
bool ShowResults { get; set; }
List<QuestionDto> Questions { get; set; }
int TotalResponses { get; set; }
int CompletedResponses { get; set; }
DateTime CreatedAt { get; set; }
DateTime UpdatedAt { get; set; }
```

**SurveyListDto** (summary for lists):
```csharp
int Id { get; set; }
string Title { get; set; }
bool IsActive { get; set; }
int QuestionCount { get; set; }
int ResponseCount { get; set; }
DateTime CreatedAt { get; set; }
```

### Question DTOs

**CreateQuestionDto**:
```csharp
string QuestionText { get; set; }
QuestionType QuestionType { get; set; }
bool IsRequired { get; set; }
List<string>? Options { get; set; }
```

**QuestionDto**:
```csharp
int Id { get; set; }
int SurveyId { get; set; }
string QuestionText { get; set; }
QuestionType QuestionType { get; set; }
int OrderIndex { get; set; }
bool IsRequired { get; set; }
List<string>? Options { get; set; }
DateTime CreatedAt { get; set; }
```

### Response DTOs

**CreateResponseDto**:
```csharp
int SurveyId { get; set; }
long RespondentTelegramId { get; set; }
```

**ResponseDto**:
```csharp
int Id { get; set; }
int SurveyId { get; set; }
string SurveyTitle { get; set; }
long RespondentTelegramId { get; set; }
bool IsComplete { get; set; }
int AnsweredCount { get; set; }
int TotalQuestions { get; set; }
List<AnswerDto> Answers { get; set; }
DateTime? StartedAt { get; set; }
DateTime? SubmittedAt { get; set; }
```

### Common DTOs

**PagedResultDto<T>**:
```csharp
List<T> Items { get; set; }
int TotalCount { get; set; }
int PageNumber { get; set; }
int PageSize { get; set; }
int TotalPages { get; set; }
bool HasPreviousPage { get; set; }
bool HasNextPage { get; set; }
```

**PaginationQueryDto**:
```csharp
int PageNumber { get; set; } = 1
int PageSize { get; set; } = 10
string? SearchTerm { get; set; }
string? SortBy { get; set; }
bool SortDescending { get; set; }
```

---

## Domain Exceptions

All custom exceptions inherit from `Exception` and are in the `SurveyBot.Core.Exceptions` namespace.

### Exception Types

**SurveyNotFoundException**:
- Thrown when a survey with specified ID doesn't exist
- Usage: Repository or service layer when GetById returns null

**QuestionNotFoundException**:
- Thrown when a question with specified ID doesn't exist

**ResponseNotFoundException**:
- Thrown when a response with specified ID doesn't exist

**SurveyValidationException**:
- Thrown when survey validation fails
- Examples: Empty title, activating survey without questions

**QuestionValidationException**:
- Thrown when question validation fails
- Examples: Invalid options for choice questions, invalid rating range

**SurveyOperationException**:
- Thrown when an operation cannot be performed
- Examples: Modifying active survey with responses

**InvalidAnswerFormatException**:
- Thrown when answer JSON doesn't match question type

**DuplicateResponseException**:
- Thrown when user tries to submit multiple responses (if not allowed)

**UnauthorizedAccessException**:
- Thrown when user tries to access/modify resource they don't own

### Exception Usage Pattern

```csharp
// In Service
public async Task<SurveyDto> GetSurveyByIdAsync(int surveyId, int userId)
{
    var survey = await _repository.GetByIdAsync(surveyId);

    if (survey == null)
        throw new SurveyNotFoundException($"Survey {surveyId} not found");

    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException("You don't own this survey");

    return MapToDto(survey);
}
```

---

## Configuration Models

### JwtSettings

Located in `Configuration/JwtSettings.cs`:

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;  // Min 32 chars
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
```

Loaded from `appsettings.json`:
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

---

## Validation Models

### ValidationResult

Located in `Models/ValidationResult.cs`:

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

---

## Best Practices for Core Layer

### 1. Keep Core Independent
- Never reference Infrastructure, Bot, or API projects
- Only use .NET standard libraries
- Define interfaces, let other projects implement them

### 2. Entity Design
- Keep entities focused on domain logic
- Use navigation properties for relationships
- Add validation attributes where appropriate
- Override ToString() for debugging

### 3. DTO Design
- DTOs should be simple POCOs
- Separate DTOs for different purposes (Create, Update, Response)
- Use nullable types appropriately
- Add data annotations for validation

### 4. Interface Design
- Keep interfaces focused (Interface Segregation Principle)
- Use specific return types (avoid object)
- Document expected behavior with XML comments
- Define async methods with CancellationToken parameter

### 5. Exception Design
- Create specific exceptions for domain errors
- Include helpful error messages
- Don't catch exceptions in Core layer (let them bubble up)
- Use exceptions for exceptional cases, not control flow

### 6. Naming Conventions
- Entities: Singular nouns (Survey, Question, User)
- DTOs: Entity name + purpose (CreateSurveyDto, SurveyListDto)
- Repositories: IEntityRepository
- Services: IEntityService

---

## Key Relationships

```
User (1) ──creates──> Surveys (*)
Survey (1) ──contains──> Questions (*)
Survey (1) ──receives──> Responses (*)
Question (1) ──answered by──> Answers (*)
Response (1) ──contains──> Answers (*)
```

### Cascade Delete Behavior
- Delete User → Surveys remain (CreatorId becomes orphaned - consider soft delete)
- Delete Survey → Questions cascade delete
- Delete Survey → Responses cascade delete
- Delete Question → Answers cascade delete
- Delete Response → Answers cascade delete

---

## Common Patterns

### Repository Method Patterns

```csharp
// Get single with navigation properties
Task<Survey?> GetByIdWithQuestionsAsync(int id)
{
    return _dbSet
        .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
        .Include(s => s.Creator)
        .FirstOrDefaultAsync(s => s.Id == id);
}

// Get collection with filters
Task<IEnumerable<Survey>> GetActiveSurveysAsync()
{
    return _dbSet
        .Include(s => s.Creator)
        .Where(s => s.IsActive)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();
}
```

### Service Method Patterns

```csharp
// Service with authorization check
public async Task<SurveyDto> GetSurveyAsync(int surveyId, int userId)
{
    var survey = await _repository.GetByIdAsync(surveyId);

    if (survey == null)
        throw new SurveyNotFoundException(surveyId);

    if (survey.CreatorId != userId)
        throw new UnauthorizedAccessException();

    return _mapper.Map<SurveyDto>(survey);
}
```

---

## Testing Considerations

### Unit Testing Core
- Test entity validation logic
- Test exception throwing conditions
- Mock repository interfaces
- Test DTO validation attributes

### Integration Testing
- Test repository implementations with real database
- Test service logic with repositories
- Test complex queries and navigation properties

---

**Remember**: The Core layer defines WHAT the application does, not HOW it does it. Implementation details belong in Infrastructure and Bot layers.
