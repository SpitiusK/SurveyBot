# SurveyBot DTOs Documentation

## Overview

This directory contains all Data Transfer Objects (DTOs) used for API request/response handling in the SurveyBot application. DTOs provide a clean separation between domain entities and API contracts, enabling flexible API evolution without affecting the database schema.

## Directory Structure

```
DTOs/
├── Common/              # Shared DTOs for pagination, responses, etc.
├── Survey/              # Survey-related DTOs
├── Question/            # Question-related DTOs
├── Response/            # Survey response DTOs
├── Answer/              # Answer DTOs
├── User/                # User and authentication DTOs
└── Statistics/          # Statistics and analytics DTOs
```

## Design Principles

### 1. Separation of Concerns
- **Read DTOs**: For GET operations (e.g., `SurveyDto`)
- **Create DTOs**: For POST operations (e.g., `CreateSurveyDto`)
- **Update DTOs**: For PUT/PATCH operations (e.g., `UpdateSurveyDto`)
- **List DTOs**: For list views with summary data (e.g., `SurveyListDto`)

### 2. Validation Strategy
All DTOs use Data Annotations for validation:
- `[Required]` - Mandatory fields
- `[MaxLength]` - String length constraints
- `[MinLength]` - Minimum length requirements
- `[Range]` - Numeric value constraints
- `[EnumDataType]` - Enum validation
- Custom validation methods via `IValidatableObject`

### 3. JSON Serialization
- DTOs are designed for JSON serialization
- Property names use PascalCase (converted to camelCase by API)
- Nullable properties use `?` syntax
- Collections initialized to prevent null references

## DTO Categories

### Survey DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `SurveyDto` | Full survey details with questions | None (read-only) |
| `CreateSurveyDto` | Create new survey | Title (3-500 chars, required) |
| `UpdateSurveyDto` | Update existing survey | Title (3-500 chars, required) |
| `SurveyListDto` | Survey list item | None (read-only) |
| `ToggleSurveyStatusDto` | Activate/deactivate | IsActive (required) |

### Question DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `QuestionDto` | Full question details | None (read-only) |
| `CreateQuestionDto` | Create new question | QuestionText (3-1000 chars), Options validation |
| `UpdateQuestionDto` | Update existing question | QuestionText (3-1000 chars), Options validation |
| `ReorderQuestionsDto` | Reorder questions | QuestionIds (min 1 item) |

**Question Validation Rules:**
- SingleChoice/MultipleChoice: Must have 2-10 options, each max 200 chars
- Text/Rating: Should not have options
- All question types: QuestionText required (3-1000 chars)

### Response DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `ResponseDto` | Full response with all answers | None (read-only) |
| `CreateResponseDto` | Start new response | TelegramId (required, positive) |
| `SubmitAnswerDto` | Submit individual answer | Answer (required) |
| `CompleteResponseDto` | Submit full response at once | TelegramId, Answers (min 1) |
| `ResponseListDto` | Response list item | None (read-only) |

### Answer DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `AnswerDto` | Full answer details | None (read-only) |
| `CreateAnswerDto` | Create/update answer | QuestionId (required), content validation |

**Answer Validation Rules:**
- Text questions: AnswerText (max 5000 chars)
- SingleChoice/MultipleChoice: SelectedOptions (required)
- Rating questions: RatingValue (1-5, required)

### User & Authentication DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `UserDto` | User profile information | None (read-only) |
| `LoginDto` | Login request | InitData or TelegramId (required) |
| `RegisterDto` | User registration | TelegramId (required, positive) |
| `TokenResponseDto` | Authentication token response | None (read-only) |
| `RefreshTokenDto` | Refresh access token | RefreshToken (required) |

### Statistics DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `SurveyStatisticsDto` | Survey-level analytics | None (read-only) |
| `QuestionStatisticsDto` | Question-level analytics | None (read-only) |
| `ChoiceStatisticsDto` | Choice option statistics | None (read-only) |
| `RatingStatisticsDto` | Rating statistics | None (read-only) |
| `RatingDistributionDto` | Rating value distribution | None (read-only) |
| `TextStatisticsDto` | Text answer statistics | None (read-only) |

### Common DTOs

| DTO Name | Purpose | Key Validations |
|----------|---------|-----------------|
| `PagedResultDto<T>` | Paginated results wrapper | None (read-only) |
| `PaginationQueryDto` | Pagination query parameters | PageNumber (min 1), PageSize (1-100) |
| `ExportFormat` | Export format enum | None |

## DTO Mapping Strategy

### Recommended Approach: AutoMapper

**Setup:**
```csharp
// In SurveyBot.Core/Mappings/MappingProfile.cs
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Survey mappings
        CreateMap<Survey, SurveyDto>()
            .ForMember(dest => dest.QuestionCount,
                opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.TotalResponses,
                opt => opt.MapFrom(src => src.Responses.Count));

        CreateMap<CreateSurveyDto, Survey>();
        CreateMap<UpdateSurveyDto, Survey>();

        // Question mappings
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.Options,
                opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.OptionsJson)
                        ? null
                        : JsonSerializer.Deserialize<List<string>>(src.OptionsJson)));

        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.OptionsJson,
                opt => opt.MapFrom(src =>
                    src.Options == null
                        ? null
                        : JsonSerializer.Serialize(src.Options)));

        // Response mappings
        CreateMap<Response, ResponseDto>();
        CreateMap<CreateResponseDto, Response>();

        // Answer mappings
        CreateMap<Answer, AnswerDto>()
            .ForMember(dest => dest.SelectedOptions,
                opt => opt.MapFrom(src => ParseAnswerJson(src)))
            .ForMember(dest => dest.RatingValue,
                opt => opt.MapFrom(src => ParseRatingValue(src)));

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<RegisterDto, User>();
    }
}
```

### Manual Mapping Alternative

For simple cases or when AutoMapper is not used:

```csharp
// Extension methods in SurveyBot.Core/Extensions/DtoExtensions.cs
public static class SurveyExtensions
{
    public static SurveyDto ToDto(this Survey survey)
    {
        return new SurveyDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            // ... map other properties
            Questions = survey.Questions.Select(q => q.ToDto()).ToList()
        };
    }

    public static Survey ToEntity(this CreateSurveyDto dto, int creatorId)
    {
        return new Survey
        {
            Title = dto.Title,
            Description = dto.Description,
            CreatorId = creatorId,
            IsActive = dto.IsActive,
            // ... map other properties
        };
    }
}
```

## Validation Examples

### Controller-Level Validation

```csharp
[HttpPost]
public async Task<ActionResult<SurveyDto>> CreateSurvey([FromBody] CreateSurveyDto dto)
{
    // ModelState.IsValid automatically checks data annotations
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    // Business logic validation
    var survey = await _surveyService.CreateAsync(dto);
    return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
}
```

### Custom Validation Example

```csharp
public class CreateQuestionDto : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (QuestionType == QuestionType.SingleChoice ||
            QuestionType == QuestionType.MultipleChoice)
        {
            if (Options == null || Options.Count < 2)
            {
                yield return new ValidationResult(
                    "Choice-based questions must have at least 2 options",
                    new[] { nameof(Options) });
            }
        }
    }
}
```

## Best Practices

### 1. Never Expose Entities Directly
- Always use DTOs for API endpoints
- Prevents over-posting vulnerabilities
- Allows API evolution without database changes

### 2. Validation Placement
- Simple validation: Data Annotations on DTOs
- Complex validation: Service layer or FluentValidation
- Business rules: Domain layer or service layer

### 3. DTO Reusability
- Create specialized DTOs for different use cases
- Avoid "one DTO fits all" approach
- Consider inheritance for shared properties

### 4. Null Handling
- Use nullable types (`?`) for optional fields
- Initialize collections to empty lists
- Document null semantics in XML comments

### 5. Performance Considerations
- Use projection (Select) when mapping collections
- Avoid lazy loading in DTO mappings
- Consider pagination for large result sets

## Error Response Format

All API errors should use a consistent format:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "Title": ["Title is required", "Title must be at least 3 characters"],
    "Options": ["Choice-based questions must have at least 2 options"]
  }
}
```

## Versioning Strategy

When breaking changes are needed:
1. Create new DTOs with version suffix (e.g., `SurveyDtoV2`)
2. Maintain old DTOs for backward compatibility
3. Use API versioning middleware
4. Deprecate old versions gradually

## Testing DTOs

### Unit Tests
```csharp
[Fact]
public void CreateSurveyDto_ValidData_PassesValidation()
{
    var dto = new CreateSurveyDto
    {
        Title = "Test Survey",
        Description = "Test Description"
    };

    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(dto, context, results, true);

    Assert.True(isValid);
}
```

## Additional Resources

- ASP.NET Core Model Validation: https://docs.microsoft.com/aspnet/core/mvc/models/validation
- AutoMapper Documentation: https://docs.automapper.org/
- Data Annotations Reference: https://docs.microsoft.com/dotnet/api/system.componentmodel.dataannotations
