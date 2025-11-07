# Repository Pattern Interfaces

This directory contains the repository pattern interfaces for the SurveyBot application. These interfaces define the contract for data access operations and follow SOLID principles.

## Overview

The repository pattern provides an abstraction layer between the business logic and data access layers, making the code more maintainable, testable, and decoupled from specific data access technologies.

## Interface Hierarchy

```
IRepository<T>
├── IAnswerRepository
├── IQuestionRepository
├── IResponseRepository
├── ISurveyRepository
└── IUserRepository
```

## Interfaces

### 1. IRepository<T>
Generic repository interface providing base CRUD operations for all entities.

**Methods:**
- `Task<T?> GetByIdAsync(int id)` - Retrieve entity by ID
- `Task<IEnumerable<T>> GetAllAsync()` - Retrieve all entities
- `Task<T> CreateAsync(T entity)` - Create new entity
- `Task<T> UpdateAsync(T entity)` - Update existing entity
- `Task<bool> DeleteAsync(int id)` - Delete entity by ID
- `Task<bool> ExistsAsync(int id)` - Check if entity exists
- `Task<int> CountAsync()` - Get total count of entities

### 2. ISurveyRepository
Survey-specific repository operations.

**Specific Methods:**
- `Task<Survey?> GetByIdWithQuestionsAsync(int id)` - Get survey with questions
- `Task<Survey?> GetByIdWithDetailsAsync(int id)` - Get survey with full details
- `Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId)` - Get user's surveys
- `Task<IEnumerable<Survey>> GetActiveSurveysAsync()` - Get active surveys
- `Task<bool> ToggleActiveStatusAsync(int id)` - Toggle survey active status
- `Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm)` - Search surveys
- `Task<int> GetResponseCountAsync(int surveyId)` - Get response count
- `Task<bool> HasResponsesAsync(int surveyId)` - Check if survey has responses

### 3. IQuestionRepository
Question-specific repository operations.

**Specific Methods:**
- `Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId)` - Get survey questions
- `Task<Question?> GetByIdWithAnswersAsync(int id)` - Get question with answers
- `Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders)` - Reorder questions
- `Task<int> GetNextOrderIndexAsync(int surveyId)` - Get next order index
- `Task<IEnumerable<Question>> GetRequiredQuestionsBySurveyIdAsync(int surveyId)` - Get required questions
- `Task<IEnumerable<Question>> GetByTypeAsync(int surveyId, QuestionType questionType)` - Filter by type
- `Task<int> DeleteBySurveyIdAsync(int surveyId)` - Delete all survey questions
- `Task<bool> BelongsToSurveyAsync(int questionId, int surveyId)` - Verify question ownership

### 4. IResponseRepository
Response-specific repository operations.

**Specific Methods:**
- `Task<Response?> GetByIdWithAnswersAsync(int id)` - Get response with answers
- `Task<IEnumerable<Response>> GetBySurveyIdAsync(int surveyId)` - Get survey responses
- `Task<IEnumerable<Response>> GetCompletedBySurveyIdAsync(int surveyId)` - Get completed responses
- `Task<IEnumerable<Response>> GetByUserAndSurveyAsync(int surveyId, long telegramId)` - Get user responses
- `Task<Response?> GetIncompleteResponseAsync(int surveyId, long telegramId)` - Get in-progress response
- `Task<bool> HasUserCompletedSurveyAsync(int surveyId, long telegramId)` - Check completion
- `Task<int> GetCompletedCountAsync(int surveyId)` - Count completed responses
- `Task<IEnumerable<Response>> GetByDateRangeAsync(int surveyId, DateTime startDate, DateTime endDate)` - Filter by date
- `Task<bool> MarkAsCompleteAsync(int responseId)` - Mark response as complete
- `Task<int> DeleteBySurveyIdAsync(int surveyId)` - Delete all survey responses

### 5. IUserRepository
User-specific repository operations.

**Specific Methods:**
- `Task<User?> GetByTelegramIdAsync(long telegramId)` - Get user by Telegram ID
- `Task<User?> GetByUsernameAsync(string username)` - Get user by username
- `Task<User?> GetByTelegramIdWithSurveysAsync(long telegramId)` - Get user with surveys
- `Task<bool> ExistsByTelegramIdAsync(long telegramId)` - Check if user exists
- `Task<bool> IsUsernameTakenAsync(string username)` - Check username availability
- `Task<User> CreateOrUpdateAsync(long telegramId, string? username, string? firstName, string? lastName)` - Upsert user
- `Task<IEnumerable<User>> GetSurveyCreatorsAsync()` - Get all survey creators
- `Task<int> GetSurveyCountAsync(int userId)` - Count user's surveys
- `Task<IEnumerable<User>> SearchByNameAsync(string searchTerm)` - Search users

### 6. IAnswerRepository
Answer-specific repository operations.

**Specific Methods:**
- `Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId)` - Get response answers
- `Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)` - Get question answers
- `Task<Answer?> GetByResponseAndQuestionAsync(int responseId, int questionId)` - Get specific answer
- `Task<IEnumerable<Answer>> CreateBatchAsync(IEnumerable<Answer> answers)` - Bulk create
- `Task<int> DeleteByResponseIdAsync(int responseId)` - Delete response answers
- `Task<int> DeleteByQuestionIdAsync(int questionId)` - Delete question answers
- `Task<int> GetCountByQuestionIdAsync(int questionId)` - Count question answers
- `Task<bool> HasAnswerAsync(int responseId, int questionId)` - Check if answer exists

## Usage Examples

### Service Layer Implementation

```csharp
public class SurveyService
{
    private readonly ISurveyRepository _surveyRepository;
    private readonly IQuestionRepository _questionRepository;

    public SurveyService(
        ISurveyRepository surveyRepository,
        IQuestionRepository questionRepository)
    {
        _surveyRepository = surveyRepository;
        _questionRepository = questionRepository;
    }

    // Get survey with all questions
    public async Task<Survey?> GetSurveyWithQuestionsAsync(int surveyId)
    {
        return await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);
    }

    // Create a new survey
    public async Task<Survey> CreateSurveyAsync(Survey survey)
    {
        return await _surveyRepository.CreateAsync(survey);
    }

    // Toggle survey status
    public async Task<bool> ToggleSurveyStatusAsync(int surveyId)
    {
        return await _surveyRepository.ToggleActiveStatusAsync(surveyId);
    }

    // Get active surveys
    public async Task<IEnumerable<Survey>> GetActiveSurveysAsync()
    {
        return await _surveyRepository.GetActiveSurveysAsync();
    }
}
```

### Response Handling

```csharp
public class ResponseService
{
    private readonly IResponseRepository _responseRepository;
    private readonly IAnswerRepository _answerRepository;

    public ResponseService(
        IResponseRepository responseRepository,
        IAnswerRepository answerRepository)
    {
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
    }

    // Start new response
    public async Task<Response> StartResponseAsync(int surveyId, long telegramId)
    {
        // Check for incomplete response
        var incomplete = await _responseRepository
            .GetIncompleteResponseAsync(surveyId, telegramId);

        if (incomplete != null)
            return incomplete;

        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = telegramId,
            StartedAt = DateTime.UtcNow,
            IsComplete = false
        };

        return await _responseRepository.CreateAsync(response);
    }

    // Submit answer
    public async Task<Answer> SubmitAnswerAsync(int responseId, int questionId, string answerText)
    {
        var answer = new Answer
        {
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = answerText
        };

        return await _answerRepository.CreateAsync(answer);
    }

    // Complete response
    public async Task<bool> CompleteResponseAsync(int responseId)
    {
        return await _responseRepository.MarkAsCompleteAsync(responseId);
    }
}
```

### User Management

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Handle Telegram user authentication
    public async Task<User> EnsureUserExistsAsync(
        long telegramId,
        string? username,
        string? firstName,
        string? lastName)
    {
        return await _userRepository.CreateOrUpdateAsync(
            telegramId, username, firstName, lastName);
    }

    // Get user surveys
    public async Task<User?> GetUserWithSurveysAsync(long telegramId)
    {
        return await _userRepository.GetByTelegramIdWithSurveysAsync(telegramId);
    }
}
```

### Question Management

```csharp
public class QuestionService
{
    private readonly IQuestionRepository _questionRepository;

    public QuestionService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    // Add question to survey
    public async Task<Question> AddQuestionAsync(int surveyId, Question question)
    {
        question.SurveyId = surveyId;
        question.OrderIndex = await _questionRepository.GetNextOrderIndexAsync(surveyId);
        return await _questionRepository.CreateAsync(question);
    }

    // Reorder questions
    public async Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders)
    {
        return await _questionRepository.ReorderQuestionsAsync(questionOrders);
    }

    // Get survey questions
    public async Task<IEnumerable<Question>> GetSurveyQuestionsAsync(int surveyId)
    {
        return await _questionRepository.GetBySurveyIdAsync(surveyId);
    }
}
```

## Design Principles

### SOLID Principles Applied

1. **Single Responsibility Principle (SRP)**
   - Each repository interface handles operations for a single entity type
   - Generic repository provides base operations
   - Specific repositories add domain-specific methods

2. **Open/Closed Principle (OCP)**
   - Interfaces are open for extension (inheritance)
   - Closed for modification (stable contracts)

3. **Liskov Substitution Principle (LSP)**
   - All specific repositories can be used as IRepository<T>
   - Implementations can be substituted without breaking code

4. **Interface Segregation Principle (ISP)**
   - Interfaces are focused and not bloated
   - Clients depend only on methods they use

5. **Dependency Inversion Principle (DIP)**
   - High-level modules (services) depend on abstractions (interfaces)
   - Not on concrete implementations

## Benefits

1. **Abstraction**: Business logic doesn't depend on data access details
2. **Testability**: Easy to mock repositories for unit testing
3. **Maintainability**: Changes to data access don't affect business logic
4. **Flexibility**: Can swap implementations (SQL, NoSQL, in-memory, etc.)
5. **Consistency**: Standardized data access patterns across the application

## Next Steps

1. Implement concrete repositories in SurveyBot.Infrastructure project
2. Configure dependency injection in API project
3. Create service layer using these interfaces
4. Write unit tests using mocked repositories

## Implementation Notes

- All methods are asynchronous (Task<T>) for scalability
- Nullable return types (T?) indicate optional results
- Use IEnumerable<T> for collections (deferred execution)
- Follow async/await best practices in implementations
- Implement proper error handling in concrete classes
