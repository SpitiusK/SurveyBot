# Repository Interfaces - Quick Reference Guide

## Overview
6 repository interfaces with 85 total methods across 414 lines of code.

---

## IRepository<T> - Generic Base (7 methods)

### Read Operations
```csharp
Task<T?> GetByIdAsync(int id)
Task<IEnumerable<T>> GetAllAsync()
Task<bool> ExistsAsync(int id)
Task<int> CountAsync()
```

### Write Operations
```csharp
Task<T> CreateAsync(T entity)
Task<T> UpdateAsync(T entity)
Task<bool> DeleteAsync(int id)
```

---

## ISurveyRepository (15 methods = 7 base + 8 specific)

### Retrieval with Relations
```csharp
Task<Survey?> GetByIdWithQuestionsAsync(int id)
Task<Survey?> GetByIdWithDetailsAsync(int id)
```

### Filtering & Search
```csharp
Task<IEnumerable<Survey>> GetByCreatorIdAsync(int creatorId)
Task<IEnumerable<Survey>> GetActiveSurveysAsync()
Task<IEnumerable<Survey>> SearchByTitleAsync(string searchTerm)
```

### Status & Statistics
```csharp
Task<bool> ToggleActiveStatusAsync(int id)
Task<int> GetResponseCountAsync(int surveyId)
Task<bool> HasResponsesAsync(int surveyId)
```

---

## IQuestionRepository (15 methods = 7 base + 8 specific)

### Survey-Scoped Retrieval
```csharp
Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId)
Task<Question?> GetByIdWithAnswersAsync(int id)
Task<IEnumerable<Question>> GetRequiredQuestionsBySurveyIdAsync(int surveyId)
Task<IEnumerable<Question>> GetByTypeAsync(int surveyId, QuestionType questionType)
```

### Ordering
```csharp
Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders)
Task<int> GetNextOrderIndexAsync(int surveyId)
```

### Validation & Bulk Operations
```csharp
Task<bool> BelongsToSurveyAsync(int questionId, int surveyId)
Task<int> DeleteBySurveyIdAsync(int surveyId)
```

---

## IResponseRepository (17 methods = 7 base + 10 specific)

### Retrieval
```csharp
Task<Response?> GetByIdWithAnswersAsync(int id)
Task<IEnumerable<Response>> GetBySurveyIdAsync(int surveyId)
Task<IEnumerable<Response>> GetCompletedBySurveyIdAsync(int surveyId)
```

### User-Specific Queries
```csharp
Task<IEnumerable<Response>> GetByUserAndSurveyAsync(int surveyId, long telegramId)
Task<Response?> GetIncompleteResponseAsync(int surveyId, long telegramId)
Task<bool> HasUserCompletedSurveyAsync(int surveyId, long telegramId)
```

### Filtering & Statistics
```csharp
Task<IEnumerable<Response>> GetByDateRangeAsync(int surveyId, DateTime startDate, DateTime endDate)
Task<int> GetCompletedCountAsync(int surveyId)
```

### State Management
```csharp
Task<bool> MarkAsCompleteAsync(int responseId)
Task<int> DeleteBySurveyIdAsync(int surveyId)
```

---

## IUserRepository (16 methods = 7 base + 9 specific)

### Telegram Integration
```csharp
Task<User?> GetByTelegramIdAsync(long telegramId)
Task<User?> GetByTelegramIdWithSurveysAsync(long telegramId)
Task<bool> ExistsByTelegramIdAsync(long telegramId)
```

### Username Management
```csharp
Task<User?> GetByUsernameAsync(string username)
Task<bool> IsUsernameTakenAsync(string username)
```

### Upsert & Search
```csharp
Task<User> CreateOrUpdateAsync(long telegramId, string? username, string? firstName, string? lastName)
Task<IEnumerable<User>> SearchByNameAsync(string searchTerm)
```

### Creator Management
```csharp
Task<IEnumerable<User>> GetSurveyCreatorsAsync()
Task<int> GetSurveyCountAsync(int userId)
```

---

## IAnswerRepository (15 methods = 7 base + 8 specific)

### Scoped Retrieval
```csharp
Task<IEnumerable<Answer>> GetByResponseIdAsync(int responseId)
Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)
Task<Answer?> GetByResponseAndQuestionAsync(int responseId, int questionId)
```

### Batch Operations
```csharp
Task<IEnumerable<Answer>> CreateBatchAsync(IEnumerable<Answer> answers)
```

### Bulk Deletion
```csharp
Task<int> DeleteByResponseIdAsync(int responseId)
Task<int> DeleteByQuestionIdAsync(int questionId)
```

### Statistics & Validation
```csharp
Task<int> GetCountByQuestionIdAsync(int questionId)
Task<bool> HasAnswerAsync(int responseId, int questionId)
```

---

## Common Patterns

### Eager Loading Pattern
```csharp
GetByIdWithQuestionsAsync()      // Survey with questions
GetByIdWithDetailsAsync()        // Survey with full details
GetByIdWithAnswersAsync()        // Question/Response with answers
GetByTelegramIdWithSurveysAsync() // User with surveys
```

### Existence Checks
```csharp
ExistsAsync(int id)                              // Generic
ExistsByTelegramIdAsync(long telegramId)         // User
HasResponsesAsync(int surveyId)                  // Survey
HasUserCompletedSurveyAsync(int, long)           // Response
HasAnswerAsync(int responseId, int questionId)   // Answer
BelongsToSurveyAsync(int questionId, int surveyId) // Question
```

### Count Operations
```csharp
CountAsync()                                    // Generic
GetResponseCountAsync(int surveyId)             // Survey
GetCompletedCountAsync(int surveyId)            // Response
GetSurveyCountAsync(int userId)                 // User
GetCountByQuestionIdAsync(int questionId)       // Answer
```

### Bulk Operations
```csharp
DeleteBySurveyIdAsync(int surveyId)          // Question, Response
DeleteByResponseIdAsync(int responseId)       // Answer
DeleteByQuestionIdAsync(int questionId)       // Answer
CreateBatchAsync(IEnumerable<Answer>)         // Answer
ReorderQuestionsAsync(Dictionary<int, int>)   // Question
```

### Search & Filter
```csharp
SearchByTitleAsync(string searchTerm)            // Survey
SearchByNameAsync(string searchTerm)             // User
GetActiveSurveysAsync()                          // Survey
GetByTypeAsync(int surveyId, QuestionType)       // Question
GetCompletedBySurveyIdAsync(int surveyId)        // Response
GetByDateRangeAsync(int, DateTime, DateTime)     // Response
```

---

## Usage Tips

### Dependency Injection Setup
```csharp
services.AddScoped<ISurveyRepository, SurveyRepository>();
services.AddScoped<IQuestionRepository, QuestionRepository>();
services.AddScoped<IResponseRepository, ResponseRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IAnswerRepository, AnswerRepository>();
```

### Service Constructor Pattern
```csharp
public class SurveyService
{
    private readonly ISurveyRepository _surveyRepo;
    private readonly IQuestionRepository _questionRepo;

    public SurveyService(
        ISurveyRepository surveyRepo,
        IQuestionRepository questionRepo)
    {
        _surveyRepo = surveyRepo;
        _questionRepo = questionRepo;
    }
}
```

### Testing with Mocks
```csharp
[Test]
public async Task GetSurvey_ReturnsCorrectSurvey()
{
    // Arrange
    var mockRepo = new Mock<ISurveyRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Survey { Id = 1, Title = "Test" });

    var service = new SurveyService(mockRepo.Object);

    // Act
    var result = await service.GetSurveyAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Title);
}
```

---

## Method Count Summary

| Interface | Generic | Specific | Total |
|-----------|---------|----------|-------|
| IRepository<T> | 7 | - | 7 |
| ISurveyRepository | 7 | 8 | 15 |
| IQuestionRepository | 7 | 8 | 15 |
| IResponseRepository | 7 | 10 | 17 |
| IUserRepository | 7 | 9 | 16 |
| IAnswerRepository | 7 | 8 | 15 |
| **TOTAL** | **7** | **43** | **85** |

---

## Files Location
```
C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Interfaces\
├── IRepository.cs              (Base interface)
├── ISurveyRepository.cs        (Survey operations)
├── IQuestionRepository.cs      (Question operations)
├── IResponseRepository.cs      (Response operations)
├── IUserRepository.cs          (User operations)
├── IAnswerRepository.cs        (Answer operations)
└── README.md                   (Detailed documentation)
```

---

## Next Implementation Steps

1. Create concrete implementations in `SurveyBot.Infrastructure/Repositories/`
2. Use Entity Framework Core for data access
3. Configure DbContext with entity configurations
4. Register repositories in DI container
5. Create service layer consuming these interfaces
6. Write unit tests with mocked repositories
