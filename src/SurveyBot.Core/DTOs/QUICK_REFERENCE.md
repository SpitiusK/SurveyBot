# DTO Quick Reference Guide

## Common DTO Patterns

### Creating a New Survey
```csharp
var createDto = new CreateSurveyDto
{
    Title = "Customer Satisfaction Survey",
    Description = "Help us improve our service",
    IsActive = false,  // Create as draft
    AllowMultipleResponses = false,
    ShowResults = true
};
```

### Updating a Survey
```csharp
var updateDto = new UpdateSurveyDto
{
    Title = "Updated Survey Title",
    Description = "Updated description",
    AllowMultipleResponses = true,
    ShowResults = true
};
```

### Creating Questions

#### Text Question
```csharp
var questionDto = new CreateQuestionDto
{
    QuestionText = "What do you think about our service?",
    QuestionType = QuestionType.Text,
    IsRequired = true,
    Options = null  // No options for text questions
};
```

#### Single Choice Question
```csharp
var questionDto = new CreateQuestionDto
{
    QuestionText = "How satisfied are you?",
    QuestionType = QuestionType.SingleChoice,
    IsRequired = true,
    Options = new List<string>
    {
        "Very Satisfied",
        "Satisfied",
        "Neutral",
        "Dissatisfied",
        "Very Dissatisfied"
    }
};
```

#### Multiple Choice Question
```csharp
var questionDto = new CreateQuestionDto
{
    QuestionText = "Which features do you use? (Select all that apply)",
    QuestionType = QuestionType.MultipleChoice,
    IsRequired = false,
    Options = new List<string>
    {
        "Feature A",
        "Feature B",
        "Feature C",
        "Feature D"
    }
};
```

#### Rating Question
```csharp
var questionDto = new CreateQuestionDto
{
    QuestionText = "Rate our customer service (1-5)",
    QuestionType = QuestionType.Rating,
    IsRequired = true,
    Options = null  // No options for rating questions
};
```

### Submitting Responses

#### Start Response
```csharp
var responseDto = new CreateResponseDto
{
    RespondentTelegramId = 123456789,
    RespondentUsername = "john_doe",
    RespondentFirstName = "John"
};
```

#### Submit Text Answer
```csharp
var answerDto = new CreateAnswerDto
{
    QuestionId = 1,
    AnswerText = "Your service is excellent!",
    SelectedOptions = null,
    RatingValue = null
};
```

#### Submit Single Choice Answer
```csharp
var answerDto = new CreateAnswerDto
{
    QuestionId = 2,
    AnswerText = null,
    SelectedOptions = new List<string> { "Very Satisfied" },
    RatingValue = null
};
```

#### Submit Multiple Choice Answer
```csharp
var answerDto = new CreateAnswerDto
{
    QuestionId = 3,
    AnswerText = null,
    SelectedOptions = new List<string> { "Feature A", "Feature C" },
    RatingValue = null
};
```

#### Submit Rating Answer
```csharp
var answerDto = new CreateAnswerDto
{
    QuestionId = 4,
    AnswerText = null,
    SelectedOptions = null,
    RatingValue = 5
};
```

#### Complete Response (All at Once)
```csharp
var completeDto = new CompleteResponseDto
{
    RespondentTelegramId = 123456789,
    RespondentUsername = "john_doe",
    RespondentFirstName = "John",
    Answers = new List<CreateAnswerDto>
    {
        new() { QuestionId = 1, AnswerText = "Great service!" },
        new() { QuestionId = 2, SelectedOptions = new List<string> { "Very Satisfied" } },
        new() { QuestionId = 3, RatingValue = 5 }
    }
};
```

### Authentication

#### Login with Telegram
```csharp
var loginDto = new LoginDto
{
    InitData = "query_id=xxx&user=...",  // From Telegram Web App
    TelegramId = null,
    Username = null
};
```

#### Login (Simple Mode for Testing)
```csharp
var loginDto = new LoginDto
{
    InitData = "",
    TelegramId = 123456789,
    Username = "john_doe"
};
```

#### Register New User
```csharp
var registerDto = new RegisterDto
{
    TelegramId = 123456789,
    Username = "john_doe",
    FirstName = "John",
    LastName = "Doe"
};
```

#### Refresh Token
```csharp
var refreshDto = new RefreshTokenDto
{
    RefreshToken = "eyJhbGciOiJIUzI1..."
};
```

### Pagination

#### Query with Pagination
```csharp
var query = new PaginationQueryDto
{
    PageNumber = 1,
    PageSize = 10,
    SearchTerm = "customer",
    SortBy = "CreatedAt",
    SortDescending = true
};
```

### Response Patterns

#### Success Response
```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "Survey Title"
  },
  "message": null
}
```

#### Paginated Response
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 45,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Validation Error Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required"],
    "Options": ["Choice-based questions must have at least 2 options"]
  }
}
```

## Validation Rules Quick Reference

### Survey
- Title: 3-500 chars (required)
- Description: max 2000 chars (optional)

### Question
- QuestionText: 3-1000 chars (required)
- Options: 2-10 items for choice questions, each max 200 chars
- QuestionType: valid enum (required)

### Answer
- Text: max 5000 chars
- Rating: 1-5 (required for rating questions)
- Options: required for choice questions

### User
- TelegramId: positive number (required)
- Username/Names: max 255 chars (optional)

### Pagination
- PageNumber: min 1
- PageSize: 1-100

## API Endpoint Patterns

### Surveys
```
GET    /api/surveys                     -> PagedResultDto<SurveyListDto>
GET    /api/surveys/{id}                -> SurveyDto
POST   /api/surveys                     -> SurveyDto
PUT    /api/surveys/{id}                -> SurveyDto
DELETE /api/surveys/{id}                -> 204 No Content
POST   /api/surveys/{id}/toggle-status  -> SurveyDto
```

### Questions
```
POST   /api/surveys/{id}/questions      -> QuestionDto
PUT    /api/questions/{id}              -> QuestionDto
DELETE /api/questions/{id}              -> 204 No Content
PUT    /api/questions/reorder           -> 204 No Content
```

### Responses
```
GET    /api/surveys/{id}/responses      -> PagedResultDto<ResponseListDto>
GET    /api/responses/{id}              -> ResponseDto
POST   /api/surveys/{id}/responses      -> ResponseDto
POST   /api/responses/{id}/answers      -> AnswerDto
POST   /api/surveys/{id}/complete       -> ResponseDto
```

### Statistics
```
GET    /api/surveys/{id}/statistics     -> SurveyStatisticsDto
GET    /api/surveys/{id}/export         -> File (CSV/Excel/JSON)
```

### Authentication
```
POST   /api/auth/login                  -> TokenResponseDto
POST   /api/auth/refresh                -> TokenResponseDto
POST   /api/auth/register               -> UserDto
```

## Common Validation Errors

### "Title is required"
- Missing Title field in CreateSurveyDto or UpdateSurveyDto

### "Title must be at least 3 characters"
- Title is too short (min 3, max 500)

### "Choice-based questions must have at least 2 options"
- SingleChoice/MultipleChoice question with < 2 options

### "Questions cannot have more than 10 options"
- Too many options (max 10)

### "Text and Rating questions should not have options"
- Options provided for Text or Rating question type

### "Question ID is required"
- Missing QuestionId in CreateAnswerDto

### "Rating must be between 1 and 5"
- RatingValue outside 1-5 range

### "Respondent Telegram ID is required"
- Missing TelegramId in CreateResponseDto

### "Page number must be at least 1"
- PageNumber < 1 in PaginationQueryDto

### "Page size must be between 1 and 100"
- PageSize outside valid range

## Controller Usage Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    [HttpPost]
    public async Task<ActionResult<SurveyDto>> Create([FromBody] CreateSurveyDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _surveyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SurveyDto>> GetById(int id)
    {
        var result = await _surveyService.GetByIdAsync(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<SurveyListDto>>> GetAll(
        [FromQuery] PaginationQueryDto query)
    {
        var result = await _surveyService.GetAllAsync(query);
        return Ok(result);
    }
}
```

## Service Layer Example

```csharp
public class SurveyService : ISurveyService
{
    private readonly IMapper _mapper;
    private readonly ISurveyRepository _repository;

    public async Task<SurveyDto> CreateAsync(CreateSurveyDto dto)
    {
        // Map DTO to entity
        var survey = _mapper.Map<Survey>(dto);

        // Business logic
        survey.CreatorId = GetCurrentUserId();

        // Save
        await _repository.AddAsync(survey);
        await _repository.SaveChangesAsync();

        // Map back to DTO
        return _mapper.Map<SurveyDto>(survey);
    }

    public async Task<PagedResultDto<SurveyListDto>> GetAllAsync(
        PaginationQueryDto query)
    {
        var queryable = _repository.GetQueryable();

        // Apply search
        if (!string.IsNullOrEmpty(query.SearchTerm))
            queryable = queryable.Where(s => s.Title.Contains(query.SearchTerm));

        // Get total
        var total = await queryable.CountAsync();

        // Apply pagination
        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // Map to DTOs
        var dtos = _mapper.Map<List<SurveyListDto>>(items);

        return new PagedResultDto<SurveyListDto>
        {
            Items = dtos,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
        };
    }
}
```

## Testing Examples

### Unit Test - Validation
```csharp
[Fact]
public void CreateSurveyDto_InvalidTitle_FailsValidation()
{
    var dto = new CreateSurveyDto { Title = "ab" }; // Too short

    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(dto, context, results, true);

    Assert.False(isValid);
}
```

### Integration Test - API
```csharp
[Fact]
public async Task CreateSurvey_ValidDto_ReturnsCreated()
{
    var dto = new CreateSurveyDto
    {
        Title = "Test Survey",
        Description = "Test"
    };

    var response = await _client.PostAsJsonAsync("/api/surveys", dto);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var result = await response.Content.ReadFromJsonAsync<SurveyDto>();
    Assert.NotNull(result);
    Assert.Equal(dto.Title, result.Title);
}
```

## Additional Resources

- Full Documentation: `README.md`
- Validation Reference: `VALIDATION_SUMMARY.md`
- Mapping Strategy: `MAPPING_STRATEGY.md`
- ASP.NET Validation: https://docs.microsoft.com/aspnet/core/mvc/models/validation
- AutoMapper: https://docs.automapper.org/
