# DTO Mapping Strategy

## Overview

This document outlines the recommended strategies for mapping between Domain Entities and DTOs in the SurveyBot application.

## Mapping Approaches

### Option 1: AutoMapper (Recommended for MVP)

AutoMapper provides convention-based object-to-object mapping with minimal configuration.

**Pros:**
- Reduces boilerplate code significantly
- Convention-based (automatic mapping for matching property names)
- Supports complex mappings and custom resolvers
- Well-tested and widely used
- Good performance with compiled mappings

**Cons:**
- Additional dependency
- Learning curve for complex scenarios
- Can hide mapping logic

**Installation:**
```bash
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

### Option 2: Manual Mapping via Extension Methods

Create extension methods for explicit mapping control.

**Pros:**
- No external dependencies
- Explicit control over all mappings
- Easy to debug
- Performance (no reflection overhead)

**Cons:**
- More boilerplate code
- Manual maintenance required
- Repetitive code

### Option 3: Mapster (Alternative)

Lightweight, high-performance alternative to AutoMapper.

**Pros:**
- Faster than AutoMapper
- Simpler API
- Source generators for compile-time safety

**Cons:**
- Less mature ecosystem
- Fewer features than AutoMapper

## Recommended: AutoMapper Implementation

### Project Setup

**1. Create Mapping Profile**

File: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Mappings\MappingProfile.cs`

```csharp
using AutoMapper;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.DTOs.User;
using System.Text.Json;

namespace SurveyBot.Core.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        ConfigureSurveyMappings();
        ConfigureQuestionMappings();
        ConfigureResponseMappings();
        ConfigureAnswerMappings();
        ConfigureUserMappings();
    }

    private void ConfigureSurveyMappings()
    {
        // Entity to DTO
        CreateMap<Survey, SurveyDto>()
            .ForMember(dest => dest.TotalResponses,
                opt => opt.MapFrom(src => src.Responses.Count))
            .ForMember(dest => dest.CompletedResponses,
                opt => opt.MapFrom(src => src.Responses.Count(r => r.IsComplete)));

        CreateMap<Survey, SurveyListDto>()
            .ForMember(dest => dest.QuestionCount,
                opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.TotalResponses,
                opt => opt.MapFrom(src => src.Responses.Count))
            .ForMember(dest => dest.CompletedResponses,
                opt => opt.MapFrom(src => src.Responses.Count(r => r.IsComplete)));

        // DTO to Entity
        CreateMap<CreateSurveyDto, Survey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
            .ForMember(dest => dest.Creator, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Responses, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateSurveyDto, Survey>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
            .ForMember(dest => dest.Creator, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Responses, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }

    private void ConfigureQuestionMappings()
    {
        // Entity to DTO
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.Options,
                opt => opt.MapFrom(src => DeserializeOptions(src.OptionsJson)));

        // DTO to Entity
        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.OrderIndex, opt => opt.Ignore())
            .ForMember(dest => dest.OptionsJson,
                opt => opt.MapFrom(src => SerializeOptions(src.Options)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.OrderIndex, opt => opt.Ignore())
            .ForMember(dest => dest.OptionsJson,
                opt => opt.MapFrom(src => SerializeOptions(src.Options)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }

    private void ConfigureResponseMappings()
    {
        // Entity to DTO
        CreateMap<Response, ResponseDto>()
            .ForMember(dest => dest.RespondentUsername, opt => opt.Ignore())
            .ForMember(dest => dest.RespondentFirstName, opt => opt.Ignore())
            .ForMember(dest => dest.AnsweredCount,
                opt => opt.MapFrom(src => src.Answers.Count))
            .ForMember(dest => dest.TotalQuestions,
                opt => opt.MapFrom(src => src.Survey.Questions.Count));

        CreateMap<Response, ResponseListDto>()
            .ForMember(dest => dest.RespondentUsername, opt => opt.Ignore())
            .ForMember(dest => dest.RespondentFirstName, opt => opt.Ignore())
            .ForMember(dest => dest.AnsweredCount,
                opt => opt.MapFrom(src => src.Answers.Count))
            .ForMember(dest => dest.TotalQuestions,
                opt => opt.MapFrom(src => src.Survey.Questions.Count));

        // DTO to Entity
        CreateMap<CreateResponseDto, Response>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.IsComplete, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore());
    }

    private void ConfigureAnswerMappings()
    {
        // Entity to DTO
        CreateMap<Answer, AnswerDto>()
            .ForMember(dest => dest.QuestionText,
                opt => opt.MapFrom(src => src.Question.QuestionText))
            .ForMember(dest => dest.QuestionType,
                opt => opt.MapFrom(src => src.Question.QuestionType))
            .ForMember(dest => dest.SelectedOptions,
                opt => opt.MapFrom(src => DeserializeSelectedOptions(src)))
            .ForMember(dest => dest.RatingValue,
                opt => opt.MapFrom(src => ParseRatingValue(src)));

        // DTO to Entity
        CreateMap<CreateAnswerDto, Answer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ResponseId, opt => opt.Ignore())
            .ForMember(dest => dest.Response, opt => opt.Ignore())
            .ForMember(dest => dest.Question, opt => opt.Ignore())
            .ForMember(dest => dest.AnswerJson,
                opt => opt.MapFrom(src => SerializeAnswer(src)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }

    private void ConfigureUserMappings()
    {
        // Entity to DTO
        CreateMap<User, UserDto>();

        // DTO to Entity
        CreateMap<RegisterDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Surveys, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }

    // Helper methods for JSON serialization
    private static List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        return JsonSerializer.Deserialize<List<string>>(json);
    }

    private static string? SerializeOptions(List<string>? options)
    {
        if (options == null || !options.Any()) return null;
        return JsonSerializer.Serialize(options);
    }

    private static List<string>? DeserializeSelectedOptions(Answer answer)
    {
        if (answer.Question.QuestionType == QuestionType.Text ||
            answer.Question.QuestionType == QuestionType.Rating)
            return null;

        if (string.IsNullOrEmpty(answer.AnswerJson))
            return null;

        return JsonSerializer.Deserialize<List<string>>(answer.AnswerJson);
    }

    private static int? ParseRatingValue(Answer answer)
    {
        if (answer.Question.QuestionType != QuestionType.Rating)
            return null;

        if (string.IsNullOrEmpty(answer.AnswerJson))
            return null;

        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(answer.AnswerJson);
        return data?.GetValueOrDefault("rating");
    }

    private static string? SerializeAnswer(CreateAnswerDto dto)
    {
        if (dto.SelectedOptions != null && dto.SelectedOptions.Any())
        {
            return JsonSerializer.Serialize(dto.SelectedOptions);
        }

        if (dto.RatingValue.HasValue)
        {
            return JsonSerializer.Serialize(new { rating = dto.RatingValue.Value });
        }

        return null;
    }
}
```

**2. Register in DI Container**

File: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Program.cs`

```csharp
// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

**3. Usage in Services**

```csharp
public class SurveyService : ISurveyService
{
    private readonly IMapper _mapper;
    private readonly ISurveyRepository _repository;

    public SurveyService(IMapper mapper, ISurveyRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    public async Task<SurveyDto> GetByIdAsync(int id)
    {
        var survey = await _repository.GetByIdAsync(id);
        return _mapper.Map<SurveyDto>(survey);
    }

    public async Task<SurveyDto> CreateAsync(CreateSurveyDto dto, int creatorId)
    {
        var survey = _mapper.Map<Survey>(dto);
        survey.CreatorId = creatorId;

        await _repository.AddAsync(survey);
        await _repository.SaveChangesAsync();

        return _mapper.Map<SurveyDto>(survey);
    }

    public async Task<SurveyDto> UpdateAsync(int id, UpdateSurveyDto dto)
    {
        var survey = await _repository.GetByIdAsync(id);
        _mapper.Map(dto, survey); // Map to existing entity

        await _repository.SaveChangesAsync();
        return _mapper.Map<SurveyDto>(survey);
    }
}
```

## Alternative: Manual Mapping Extension Methods

If you prefer not to use AutoMapper, create extension methods:

File: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\Extensions\MappingExtensions.cs`

```csharp
using System.Text.Json;
using SurveyBot.Core.Entities;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.DTOs.Question;

namespace SurveyBot.Core.Extensions;

public static class SurveyMappingExtensions
{
    public static SurveyDto ToDto(this Survey survey)
    {
        return new SurveyDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            CreatorId = survey.CreatorId,
            Creator = survey.Creator?.ToDto(),
            IsActive = survey.IsActive,
            AllowMultipleResponses = survey.AllowMultipleResponses,
            ShowResults = survey.ShowResults,
            Questions = survey.Questions.Select(q => q.ToDto()).ToList(),
            TotalResponses = survey.Responses.Count,
            CompletedResponses = survey.Responses.Count(r => r.IsComplete),
            CreatedAt = survey.CreatedAt,
            UpdatedAt = survey.UpdatedAt
        };
    }

    public static SurveyListDto ToListDto(this Survey survey)
    {
        return new SurveyListDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            IsActive = survey.IsActive,
            QuestionCount = survey.Questions.Count,
            TotalResponses = survey.Responses.Count,
            CompletedResponses = survey.Responses.Count(r => r.IsComplete),
            CreatedAt = survey.CreatedAt,
            UpdatedAt = survey.UpdatedAt
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
            AllowMultipleResponses = dto.AllowMultipleResponses,
            ShowResults = dto.ShowResults
        };
    }

    public static void UpdateEntity(this UpdateSurveyDto dto, Survey survey)
    {
        survey.Title = dto.Title;
        survey.Description = dto.Description;
        survey.AllowMultipleResponses = dto.AllowMultipleResponses;
        survey.ShowResults = dto.ShowResults;
    }
}

public static class QuestionMappingExtensions
{
    public static QuestionDto ToDto(this Question question)
    {
        return new QuestionDto
        {
            Id = question.Id,
            SurveyId = question.SurveyId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            OrderIndex = question.OrderIndex,
            IsRequired = question.IsRequired,
            Options = string.IsNullOrEmpty(question.OptionsJson)
                ? null
                : JsonSerializer.Deserialize<List<string>>(question.OptionsJson),
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    public static Question ToEntity(this CreateQuestionDto dto, int surveyId, int orderIndex)
    {
        return new Question
        {
            SurveyId = surveyId,
            QuestionText = dto.QuestionText,
            QuestionType = dto.QuestionType,
            OrderIndex = orderIndex,
            IsRequired = dto.IsRequired,
            OptionsJson = dto.Options != null && dto.Options.Any()
                ? JsonSerializer.Serialize(dto.Options)
                : null
        };
    }

    public static void UpdateEntity(this UpdateQuestionDto dto, Question question)
    {
        question.QuestionText = dto.QuestionText;
        question.QuestionType = dto.QuestionType;
        question.IsRequired = dto.IsRequired;
        question.OptionsJson = dto.Options != null && dto.Options.Any()
            ? JsonSerializer.Serialize(dto.Options)
            : null;
    }
}
```

**Usage:**
```csharp
public async Task<SurveyDto> GetByIdAsync(int id)
{
    var survey = await _repository.GetByIdAsync(id);
    return survey.ToDto(); // Extension method
}
```

## Mapping Special Cases

### 1. JSON Fields (OptionsJson, AnswerJson)

**Challenge:** Database stores JSON strings, DTOs use typed collections

**Solution:** Serialize/deserialize during mapping

```csharp
// Entity -> DTO
Options = string.IsNullOrEmpty(question.OptionsJson)
    ? null
    : JsonSerializer.Deserialize<List<string>>(question.OptionsJson)

// DTO -> Entity
OptionsJson = dto.Options != null && dto.Options.Any()
    ? JsonSerializer.Serialize(dto.Options)
    : null
```

### 2. Computed Properties

**Challenge:** DTOs have properties not in entities (e.g., CompletedResponses)

**Solution:** Map from navigation properties

```csharp
CreateMap<Survey, SurveyDto>()
    .ForMember(dest => dest.CompletedResponses,
        opt => opt.MapFrom(src => src.Responses.Count(r => r.IsComplete)));
```

### 3. Related Entities

**Challenge:** Avoid N+1 queries when mapping collections

**Solution:** Use projection or eager loading

```csharp
// In repository - use projection
public async Task<List<SurveyListDto>> GetAllAsync()
{
    return await _context.Surveys
        .Select(s => new SurveyListDto
        {
            Id = s.Id,
            Title = s.Title,
            // ... other properties
            QuestionCount = s.Questions.Count,
            CompletedResponses = s.Responses.Count(r => r.IsComplete)
        })
        .ToListAsync();
}
```

### 4. Pagination

**Challenge:** Efficiently map large result sets

**Solution:** Map after pagination, not before

```csharp
public async Task<PagedResultDto<SurveyListDto>> GetPagedAsync(PaginationQueryDto query)
{
    var queryable = _context.Surveys.AsQueryable();

    // Apply filters and sorting first
    var totalCount = await queryable.CountAsync();

    // Paginate
    var items = await queryable
        .Skip((query.PageNumber - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToListAsync();

    // Map after pagination
    var dtos = _mapper.Map<List<SurveyListDto>>(items);

    return new PagedResultDto<SurveyListDto>
    {
        Items = dtos,
        PageNumber = query.PageNumber,
        PageSize = query.PageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
    };
}
```

## Performance Optimization

### 1. Projection Instead of Mapping

For read-only scenarios, project directly to DTO:

```csharp
// Better performance - no entity materialization
var surveys = await _context.Surveys
    .Select(s => new SurveyListDto
    {
        Id = s.Id,
        Title = s.Title,
        QuestionCount = s.Questions.Count
    })
    .ToListAsync();

// vs. less efficient
var surveys = await _context.Surveys
    .Include(s => s.Questions)
    .ToListAsync();
var dtos = _mapper.Map<List<SurveyListDto>>(surveys);
```

### 2. Compiled Mappings (AutoMapper)

AutoMapper automatically compiles mappings for performance.

### 3. Avoid Lazy Loading in Mappings

Always use eager loading or explicit loading:

```csharp
var survey = await _context.Surveys
    .Include(s => s.Questions)
    .Include(s => s.Responses)
    .FirstOrDefaultAsync(s => s.Id == id);
```

## Testing Mappings

### AutoMapper Configuration Test

```csharp
public class MappingProfileTests
{
    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        configuration.AssertConfigurationIsValid();
    }
}
```

### Mapping Behavior Tests

```csharp
[Fact]
public void Survey_ToDto_MapsAllProperties()
{
    // Arrange
    var mapper = new MapperConfiguration(cfg =>
        cfg.AddProfile<MappingProfile>()).CreateMapper();

    var survey = new Survey
    {
        Id = 1,
        Title = "Test Survey",
        // ... other properties
    };

    // Act
    var dto = mapper.Map<SurveyDto>(survey);

    // Assert
    Assert.Equal(survey.Id, dto.Id);
    Assert.Equal(survey.Title, dto.Title);
    // ... assert other properties
}
```

## Migration Path

### Phase 1: Setup (Day 1)
- Install AutoMapper
- Create MappingProfile with basic mappings
- Register in DI

### Phase 2: Implementation (Days 2-3)
- Implement all entity-to-DTO mappings
- Implement all DTO-to-entity mappings
- Add custom resolvers for complex mappings

### Phase 3: Testing (Day 4)
- Add configuration tests
- Add mapping behavior tests
- Integration test with actual data

### Phase 4: Optimization (Ongoing)
- Profile mapping performance
- Add projections where beneficial
- Optimize N+1 query scenarios

## Summary

**For MVP: Use AutoMapper**
- Faster development
- Less boilerplate
- Well-tested library
- Good enough performance

**Consider Manual Mapping If:**
- Maximum performance required
- Team unfamiliar with AutoMapper
- Very simple domain model
- Want explicit control

**Next Steps:**
1. Install AutoMapper package
2. Create MappingProfile class
3. Register in Program.cs
4. Use IMapper in services
5. Add mapping tests
