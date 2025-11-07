# ResponseService Quick Reference

## Service Interface
`IResponseService` - Interface for managing survey responses and answers

## File Locations

### Interface
```
C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Interfaces\IResponseService.cs
```

### Implementation
```
C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\Services\ResponseService.cs
```

### Tests
```
C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests\Unit\Services\ResponseServiceTests.cs
```

## Method Summary

### StartResponseAsync
```csharp
Task<ResponseDto> StartResponseAsync(
    int surveyId,
    long telegramUserId,
    string? username = null,
    string? firstName = null)
```
- Creates new response
- Validates survey is active
- Checks for duplicate completed responses
- Sets StartedAt timestamp

**Throws:**
- `SurveyNotFoundException` - Survey not found
- `SurveyOperationException` - Survey not active
- `DuplicateResponseException` - User already completed (when not allowed)

### SaveAnswerAsync
```csharp
Task<ResponseDto> SaveAnswerAsync(
    int responseId,
    int questionId,
    string? answerText = null,
    List<string>? selectedOptions = null,
    int? ratingValue = null,
    int? userId = null)
```
- Saves or updates answer
- Validates answer format
- Prevents modification of completed responses

**Throws:**
- `ResponseNotFoundException` - Response not found
- `QuestionNotFoundException` - Question not found
- `InvalidAnswerFormatException` - Invalid answer format
- `SurveyOperationException` - Response already completed
- `QuestionValidationException` - Question not in survey
- `UnauthorizedAccessException` - User not authorized

### CompleteResponseAsync
```csharp
Task<ResponseDto> CompleteResponseAsync(
    int responseId,
    int? userId = null)
```
- Marks response as complete
- Sets SubmittedAt timestamp
- Prevents further modifications

**Throws:**
- `ResponseNotFoundException` - Response not found
- `UnauthorizedAccessException` - User not authorized

### GetResponseAsync
```csharp
Task<ResponseDto> GetResponseAsync(
    int responseId,
    int? userId = null)
```
- Retrieves response with all answers

**Throws:**
- `ResponseNotFoundException` - Response not found
- `UnauthorizedAccessException` - User not authorized

### GetSurveyResponsesAsync
```csharp
Task<PagedResultDto<ResponseDto>> GetSurveyResponsesAsync(
    int surveyId,
    int userId,
    int pageNumber = 1,
    int pageSize = 20,
    bool? isCompleteFilter = null,
    DateTime? startDate = null,
    DateTime? endDate = null)
```
- Gets paginated list of responses
- Supports filtering by completion status and date range
- Only accessible by survey creator

**Throws:**
- `SurveyNotFoundException` - Survey not found
- `UnauthorizedAccessException` - User is not survey creator

### ValidateAnswerFormatAsync
```csharp
Task<ValidationResult> ValidateAnswerFormatAsync(
    int questionId,
    string? answerText = null,
    List<string>? selectedOptions = null,
    int? ratingValue = null)
```
- Validates answer format for question type
- Returns validation result with error details

**Throws:**
- `QuestionNotFoundException` - Question not found

### ResumeResponseAsync
```csharp
Task<ResponseDto> ResumeResponseAsync(
    int surveyId,
    long telegramUserId,
    string? username = null,
    string? firstName = null)
```
- Returns existing incomplete response
- Starts new response if none exists

**Throws:**
- `SurveyNotFoundException` - Survey not found
- `DuplicateResponseException` - User already completed

### DeleteResponseAsync
```csharp
Task DeleteResponseAsync(int responseId, int userId)
```
- Deletes response and all answers
- Only accessible by survey creator

**Throws:**
- `ResponseNotFoundException` - Response not found
- `UnauthorizedAccessException` - User not survey creator

### GetCompletedResponseCountAsync
```csharp
Task<int> GetCompletedResponseCountAsync(int surveyId)
```
- Returns count of completed responses for survey

## Validation Rules

### Text Questions
- Max 5000 characters
- Required: Non-empty text
- Optional: Can be null/empty

### SingleChoice Questions
- Exactly one option
- Must be from question's option list
- Required: Must select one

### MultipleChoice Questions
- One or more options
- All must be from question's option list
- Required: At least one selection

### Rating Questions
- Integer value 1-5 (inclusive)
- Required: Must provide value

## Answer Storage Format

### Text
- Stored in `Answer.AnswerText`
- `Answer.AnswerJson` is null

### SingleChoice / MultipleChoice
- Stored in `Answer.AnswerJson` as:
```json
{
  "selectedOptions": ["Option A", "Option B"]
}
```

### Rating
- Stored in `Answer.AnswerJson` as:
```json
{
  "ratingValue": 4
}
```

## Common Usage Patterns

### Bot Response Flow
```csharp
// 1. User starts survey
var response = await _responseService.StartResponseAsync(surveyId, telegramUserId);

// 2. Save answers as user provides them
foreach (var question in questions)
{
    var answer = await GetAnswerFromUser(question);
    await _responseService.SaveAnswerAsync(
        response.Id,
        question.Id,
        answerText: answer
    );
}

// 3. Complete response
await _responseService.CompleteResponseAsync(response.Id);
```

### Resume Incomplete
```csharp
// Returns existing or creates new
var response = await _responseService.ResumeResponseAsync(
    surveyId,
    telegramUserId
);

// Check progress
var progress = $"{response.AnsweredCount}/{response.TotalQuestions}";
```

### Admin View Responses
```csharp
// Get all completed responses
var responses = await _responseService.GetSurveyResponsesAsync(
    surveyId: 1,
    userId: adminId,
    isCompleteFilter: true,
    pageNumber: 1,
    pageSize: 20
);

// Filter by date range
var recentResponses = await _responseService.GetSurveyResponsesAsync(
    surveyId: 1,
    userId: adminId,
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow
);
```

## Test Coverage (30 tests)

- StartResponseAsync: 5 tests
- SaveAnswerAsync: 6 tests
- CompleteResponseAsync: 3 tests
- ValidateAnswerFormatAsync: 13 tests
  - Text: 3 tests
  - SingleChoice: 3 tests
  - MultipleChoice: 2 tests
  - Rating: 3 tests (including Theory test)
- ResumeResponseAsync: 2 tests
- DeleteResponseAsync: 2 tests
- GetCompletedResponseCountAsync: 1 test

## Dependencies

```
ResponseService requires:
- IResponseRepository
- IAnswerRepository
- ISurveyRepository
- IQuestionRepository
- IMapper (AutoMapper)
- ILogger<ResponseService>
```

## DI Registration

```csharp
// In Program.cs or ServiceExtensions
services.AddScoped<IResponseService, ResponseService>();
```
