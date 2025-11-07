# AutoMapper Usage Examples

This document provides examples of how to use AutoMapper with the Survey Bot API DTOs and entities.

## Table of Contents
- [Setup](#setup)
- [Basic Mapping](#basic-mapping)
- [Survey Mappings](#survey-mappings)
- [Question Mappings](#question-mappings)
- [Response Mappings](#response-mappings)
- [Answer Mappings](#answer-mappings)
- [User Mappings](#user-mappings)
- [Testing Mappings](#testing-mappings)

## Setup

AutoMapper is already configured in `Program.cs` via dependency injection:

```csharp
// In Program.cs
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

All mapping profiles in the `SurveyBot.API.Mapping` namespace are automatically discovered and registered.

## Basic Mapping

### Injecting IMapper

In controllers or services, inject `IMapper`:

```csharp
public class SurveyController : ControllerBase
{
    private readonly IMapper _mapper;

    public SurveyController(IMapper mapper)
    {
        _mapper = mapper;
    }
}
```

### Simple Mapping

```csharp
// Entity to DTO
var surveyDto = _mapper.Map<SurveyDto>(surveyEntity);

// DTO to Entity
var surveyEntity = _mapper.Map<Survey>(createSurveyDto);
```

### Collection Mapping

```csharp
// Map a list of entities to DTOs
var surveyDtos = _mapper.Map<List<SurveyListDto>>(surveyEntities);

// Map a list of DTOs to entities
var questionEntities = _mapper.Map<List<Question>>(createQuestionDtos);
```

## Survey Mappings

### Creating a Survey

```csharp
var createDto = new CreateSurveyDto
{
    Title = "Customer Satisfaction Survey",
    Description = "Help us improve our service",
    IsActive = true
};

// Map to entity
var survey = _mapper.Map<Survey>(createDto);

// Set additional properties not in DTO
survey.CreatorId = currentUserId;
survey.CreatedAt = DateTime.UtcNow;

await _surveyRepository.AddAsync(survey);
```

### Updating a Survey

```csharp
var updateDto = new UpdateSurveyDto
{
    Title = "Updated Survey Title",
    Description = "Updated description"
};

// Get existing entity
var survey = await _surveyRepository.GetByIdAsync(surveyId);

// Map updates to existing entity (only non-null properties)
_mapper.Map(updateDto, survey);

survey.UpdatedAt = DateTime.UtcNow;

await _surveyRepository.UpdateAsync(survey);
```

### Getting Survey Details

```csharp
// Get entity with includes
var survey = await _surveyRepository.GetByIdWithQuestionsAsync(surveyId);

// Map to detailed DTO
var surveyDetailDto = _mapper.Map<SurveyDetailDto>(survey);

// surveyDetailDto will include:
// - QuestionCount (calculated)
// - ResponseCount (calculated)
// - Questions (mapped from navigation property)
```

### Getting Survey List

```csharp
var surveys = await _surveyRepository.GetAllAsync();

// Map to list DTOs
var surveyListDtos = _mapper.Map<List<SurveyListDto>>(surveys);

// Each item includes:
// - QuestionCount
// - ResponseCount
// - Basic survey information
```

## Question Mappings

### Creating Questions

```csharp
var createDto = new CreateQuestionDto
{
    QuestionText = "How satisfied are you with our service?",
    QuestionType = QuestionType.Rating,
    IsRequired = true,
    Options = null // Not needed for rating questions
};

// Map to entity
var question = _mapper.Map<Question>(createDto);

// QuestionOptionsJsonResolver automatically serializes Options to OptionsJson
// Set additional properties
question.SurveyId = surveyId;
question.OrderIndex = nextOrderIndex;

await _questionRepository.AddAsync(question);
```

### Creating Multiple Choice Question

```csharp
var createDto = new CreateQuestionDto
{
    QuestionText = "Which features do you use most?",
    QuestionType = QuestionType.MultiChoice,
    IsRequired = true,
    Options = new List<string>
    {
        "Feature A",
        "Feature B",
        "Feature C",
        "Feature D"
    }
};

var question = _mapper.Map<Question>(createDto);
// Options list is automatically serialized to JSON in OptionsJson field

question.SurveyId = surveyId;
question.OrderIndex = nextOrderIndex;

await _questionRepository.AddAsync(question);
```

### Reading Questions

```csharp
var question = await _questionRepository.GetByIdAsync(questionId);

// Map to DTO
var questionDto = _mapper.Map<QuestionDto>(question);

// QuestionOptionsResolver automatically deserializes OptionsJson to Options list
// questionDto.Options will be a List<string> or null
```

## Response Mappings

### Creating a Response

```csharp
var createDto = new CreateResponseDto
{
    TelegramUserId = 123456789
};

// Map to entity
var response = _mapper.Map<Response>(createDto);

// Automatically sets:
// - IsComplete = false
// - StartedAt = DateTime.UtcNow

response.SurveyId = surveyId;

await _responseRepository.AddAsync(response);
```

### Getting Response Details

```csharp
var response = await _responseRepository.GetByIdWithAnswersAsync(responseId);

// Map to DTO
var responseDto = _mapper.Map<ResponseDto>(response);

// Includes:
// - AnsweredCount (calculated from Answers collection)
// - TotalQuestions (calculated from Survey.Questions)
// - Answers (mapped from navigation property)
```

### Getting Response List

```csharp
var responses = await _responseRepository.GetBySurveyIdAsync(surveyId);

// Map to list DTOs
var responseDtos = _mapper.Map<List<ResponseListDto>>(responses);

// Each item includes calculated fields:
// - AnsweredCount
// - TotalQuestions
// - Progress percentage can be calculated client-side
```

## Answer Mappings

### Creating Text Answer

```csharp
var createDto = new CreateAnswerDto
{
    QuestionId = questionId,
    AnswerText = "This is my text answer"
};

// Map to entity
var answer = _mapper.Map<Answer>(createDto);

// AnswerJsonResolver handles serialization:
// - For text answers, AnswerJson remains null
// - AnswerText is stored directly

answer.ResponseId = responseId;

await _answerRepository.AddAsync(answer);
```

### Creating Choice Answer

```csharp
var createDto = new CreateAnswerDto
{
    QuestionId = questionId,
    SelectedOptions = new List<string> { "Option A", "Option C" }
};

// Map to entity
var answer = _mapper.Map<Answer>(createDto);

// AnswerJsonResolver serializes SelectedOptions to AnswerJson
// answer.AnswerJson = ["Option A", "Option C"]

answer.ResponseId = responseId;

await _answerRepository.AddAsync(answer);
```

### Creating Rating Answer

```csharp
var createDto = new CreateAnswerDto
{
    QuestionId = questionId,
    RatingValue = 5
};

// Map to entity
var answer = _mapper.Map<Answer>(createDto);

// AnswerJsonResolver serializes RatingValue to AnswerJson
// answer.AnswerJson = "5"

answer.ResponseId = responseId;

await _answerRepository.AddAsync(answer);
```

### Reading Answers

```csharp
var answer = await _answerRepository.GetByIdWithQuestionAsync(answerId);

// Map to DTO
var answerDto = _mapper.Map<AnswerDto>(answer);

// Automatically includes:
// - QuestionText (from Question navigation property)
// - QuestionType (from Question navigation property)
// - SelectedOptions (deserialized from AnswerJson if applicable)
// - RatingValue (deserialized from AnswerJson if applicable)
```

## User Mappings

### Mapping User to DTO

```csharp
var user = await _userRepository.GetByTelegramIdAsync(telegramUserId);

// Map to DTO
var userDto = _mapper.Map<UserDto>(user);
```

### Creating User from Login

```csharp
var loginDto = new LoginDto
{
    TelegramUserId = 123456789,
    Username = "john_doe",
    FirstName = "John",
    LastName = "Doe"
};

// Map to entity (for user creation during Telegram auth)
var user = _mapper.Map<User>(loginDto);

await _userRepository.AddAsync(user);
```

## Testing Mappings

### Unit Testing Mapping Configuration

```csharp
[Fact]
public void AutoMapper_Configuration_IsValid()
{
    // Arrange
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddMaps(typeof(SurveyMappingProfile).Assembly);
    });

    // Act & Assert - throws if invalid
    config.AssertConfigurationIsValid();
}
```

### Testing Specific Mapping

```csharp
[Fact]
public void Map_Survey_To_SurveyDto()
{
    // Arrange
    var mapper = AutoMapperConfigurationTest.CreateMapper();

    var survey = new Survey
    {
        Id = 1,
        Title = "Test Survey",
        Description = "Test Description",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        Questions = new List<Question>
        {
            new Question { Id = 1, QuestionText = "Q1" },
            new Question { Id = 2, QuestionText = "Q2" }
        },
        Responses = new List<Response>
        {
            new Response { Id = 1 }
        }
    };

    // Act
    var dto = mapper.Map<SurveyDto>(survey);

    // Assert
    Assert.Equal(survey.Id, dto.Id);
    Assert.Equal(survey.Title, dto.Title);
    Assert.Equal(2, dto.QuestionCount);
    Assert.Equal(1, dto.ResponseCount);
}
```

### Testing Custom Value Resolver

```csharp
[Fact]
public void QuestionOptionsResolver_DeserializesJson()
{
    // Arrange
    var mapper = AutoMapperConfigurationTest.CreateMapper();

    var question = new Question
    {
        Id = 1,
        QuestionText = "Test Question",
        QuestionType = QuestionType.SingleChoice,
        OptionsJson = "[\"Option A\",\"Option B\",\"Option C\"]"
    };

    // Act
    var dto = mapper.Map<QuestionDto>(question);

    // Assert
    Assert.NotNull(dto.Options);
    Assert.Equal(3, dto.Options.Count);
    Assert.Contains("Option A", dto.Options);
}
```

## Best Practices

1. **Always inject IMapper** - Don't create mapper instances manually in production code
2. **Use specific DTOs** - Use `SurveyListDto` for lists and `SurveyDetailDto` for details
3. **Include navigation properties** - When mapping to DTOs that need counts, ensure navigation properties are loaded
4. **Test your mappings** - Use `AssertConfigurationIsValid()` in tests
5. **Custom resolvers for complex logic** - Use value resolvers for JSON serialization and calculated fields
6. **Partial updates** - For update operations, map DTO to existing entity to preserve unmapped properties

## Common Patterns

### Map with additional operations

```csharp
// Map and modify
var survey = _mapper.Map<Survey>(createDto);
survey.CreatorId = userId;
survey.CreatedAt = DateTime.UtcNow;
await _repository.AddAsync(survey);
```

### Projection with LINQ

```csharp
// Map query results
var surveys = await _context.Surveys
    .Where(s => s.IsActive)
    .ToListAsync();

var dtos = _mapper.Map<List<SurveyListDto>>(surveys);
```

### Updating existing entities

```csharp
// Get existing
var survey = await _repository.GetByIdAsync(id);

// Map updates (only non-null properties)
_mapper.Map(updateDto, survey);

// Set audit fields
survey.UpdatedAt = DateTime.UtcNow;

await _repository.UpdateAsync(survey);
```
