# AutoMapper Quick Reference

## Injection

```csharp
public class MyService
{
    private readonly IMapper _mapper;

    public MyService(IMapper mapper)
    {
        _mapper = mapper;
    }
}
```

## Common Mappings

### Survey Operations

```csharp
// Create
var survey = _mapper.Map<Survey>(createSurveyDto);

// Read
var surveyDto = _mapper.Map<SurveyDto>(survey);
var surveyList = _mapper.Map<List<SurveyListDto>>(surveys);

// Update
_mapper.Map(updateSurveyDto, existingSurvey);
```

### Question Operations

```csharp
// Create with options
var question = _mapper.Map<Question>(createQuestionDto);
// Options list automatically serialized to OptionsJson

// Read with options
var questionDto = _mapper.Map<QuestionDto>(question);
// OptionsJson automatically deserialized to Options list
```

### Response Operations

```csharp
// Create
var response = _mapper.Map<Response>(createResponseDto);
// IsComplete = false, StartedAt = UtcNow

// Read with stats
var responseDto = _mapper.Map<ResponseDto>(response);
// Includes AnsweredCount, TotalQuestions
```

### Answer Operations

```csharp
// Text answer
var answer = _mapper.Map<Answer>(new CreateAnswerDto
{
    QuestionId = id,
    AnswerText = "text"
});

// Choice answer
var answer = _mapper.Map<Answer>(new CreateAnswerDto
{
    QuestionId = id,
    SelectedOptions = new List<string> { "A", "B" }
});
// Automatically serialized to AnswerJson

// Rating answer
var answer = _mapper.Map<Answer>(new CreateAnswerDto
{
    QuestionId = id,
    RatingValue = 5
});
// Automatically serialized to AnswerJson
```

## Mapping Profiles

| Profile | Mappings |
|---------|----------|
| SurveyMappingProfile | Survey ↔ SurveyDto, SurveyListDto, CreateSurveyDto, UpdateSurveyDto |
| QuestionMappingProfile | Question ↔ QuestionDto, CreateQuestionDto, UpdateQuestionDto |
| ResponseMappingProfile | Response ↔ ResponseDto, ResponseListDto, CreateResponseDto |
| AnswerMappingProfile | Answer ↔ AnswerDto, CreateAnswerDto |
| UserMappingProfile | User ↔ UserDto, LoginDto |
| StatisticsMappingProfile | Documentation only (computed in service layer) |

## Value Resolvers

### JSON Serialization
- **QuestionOptionsResolver**: OptionsJson → Options (List<string>)
- **QuestionOptionsJsonResolver**: Options → OptionsJson
- **AnswerJsonResolver**: Answer fields → AnswerJson
- **AnswerSelectedOptionsResolver**: AnswerJson → SelectedOptions
- **AnswerRatingValueResolver**: AnswerJson → RatingValue

### Calculated Fields
- **SurveyTotalResponsesResolver**: Counts total responses
- **SurveyCompletedResponsesResolver**: Counts completed responses
- **ResponseAnsweredCountResolver**: Counts answered questions
- **ResponseTotalQuestionsResolver**: Counts total survey questions
- **AnswersCountResolver**: Counts answers in response
- **ResponsePercentageResolver**: Calculates completion percentage

## Testing

```csharp
// Validate configuration
[Fact]
public void AutoMapper_Configuration_IsValid()
{
    AutoMapperConfigurationTest.ValidateConfiguration();
}

// Test specific mapping
[Fact]
public void Map_Survey_To_Dto()
{
    var mapper = AutoMapperConfigurationTest.CreateMapper();
    var survey = new Survey { /* ... */ };

    var dto = mapper.Map<SurveyDto>(survey);

    Assert.NotNull(dto);
}
```

## Tips

1. Always include navigation properties when mapping to DTOs with counts
2. Use `GetByIdWithQuestionsAsync()` before mapping to SurveyDetailDto
3. Use `GetByIdWithAnswersAsync()` before mapping to ResponseDto
4. For updates, map DTO to existing entity, not new instance
5. Set audit fields (CreatedAt, UpdatedAt) after mapping
6. Validate configuration in startup tests
