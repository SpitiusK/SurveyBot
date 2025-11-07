using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for Survey entity AutoMapper mappings.
/// </summary>
public class SurveyMappingTests
{
    private readonly IMapper _mapper;

    public SurveyMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SurveyMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        // Arrange & Act & Assert
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SurveyMappingProfile>();
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_Survey_To_SurveyDto_Success()
    {
        // Arrange
        var survey = new Survey
        {
            Id = 1,
            Title = "Customer Satisfaction Survey",
            Description = "Please rate our service",
            CreatorId = 100,
            IsActive = true,
            AllowMultipleResponses = false,
            ShowResults = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow,
            Questions = new List<Question>
            {
                new Question { Id = 1, QuestionText = "How satisfied are you?" },
                new Question { Id = 2, QuestionText = "Would you recommend us?" }
            },
            Responses = new List<Response>
            {
                new Response { Id = 1, IsComplete = true },
                new Response { Id = 2, IsComplete = false },
                new Response { Id = 3, IsComplete = true }
            }
        };

        // Act
        var dto = _mapper.Map<SurveyDto>(survey);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(survey.Id, dto.Id);
        Assert.Equal(survey.Title, dto.Title);
        Assert.Equal(survey.Description, dto.Description);
        Assert.Equal(survey.IsActive, dto.IsActive);
        Assert.Equal(survey.AllowMultipleResponses, dto.AllowMultipleResponses);
        Assert.Equal(survey.ShowResults, dto.ShowResults);
        Assert.Equal(3, dto.TotalResponses);
        Assert.Equal(2, dto.CompletedResponses);
        Assert.Equal(survey.CreatedAt, dto.CreatedAt);
        Assert.Equal(survey.UpdatedAt, dto.UpdatedAt);
    }

    [Fact]
    public void Map_Survey_To_SurveyListDto_Success()
    {
        // Arrange
        var survey = new Survey
        {
            Id = 1,
            Title = "Product Feedback",
            Description = "Help us improve",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow,
            Questions = new List<Question>
            {
                new Question { Id = 1 },
                new Question { Id = 2 },
                new Question { Id = 3 }
            },
            Responses = new List<Response>
            {
                new Response { Id = 1, IsComplete = true },
                new Response { Id = 2, IsComplete = true }
            }
        };

        // Act
        var dto = _mapper.Map<SurveyListDto>(survey);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(survey.Id, dto.Id);
        Assert.Equal(survey.Title, dto.Title);
        Assert.Equal(survey.IsActive, dto.IsActive);
        Assert.Equal(3, dto.QuestionCount);
        Assert.Equal(2, dto.TotalResponses);
        Assert.Equal(2, dto.CompletedResponses);
    }

    [Fact]
    public void Map_CreateSurveyDto_To_Survey_Success()
    {
        // Arrange
        var createDto = new CreateSurveyDto
        {
            Title = "New Survey",
            Description = "Test survey",
            IsActive = false,
            AllowMultipleResponses = true,
            ShowResults = false
        };

        // Act
        var survey = _mapper.Map<Survey>(createDto);

        // Assert
        Assert.NotNull(survey);
        Assert.Equal(createDto.Title, survey.Title);
        Assert.Equal(createDto.Description, survey.Description);
        Assert.Equal(createDto.IsActive, survey.IsActive);
        Assert.Equal(createDto.AllowMultipleResponses, survey.AllowMultipleResponses);
        Assert.Equal(createDto.ShowResults, survey.ShowResults);
    }

    [Fact]
    public void Map_UpdateSurveyDto_To_Survey_Success()
    {
        // Arrange
        var updateDto = new UpdateSurveyDto
        {
            Title = "Updated Survey",
            Description = "Updated description",
            AllowMultipleResponses = false,
            ShowResults = true
        };

        // Act
        var survey = _mapper.Map<Survey>(updateDto);

        // Assert
        Assert.NotNull(survey);
        Assert.Equal(updateDto.Title, survey.Title);
        Assert.Equal(updateDto.Description, survey.Description);
        Assert.Equal(updateDto.AllowMultipleResponses, survey.AllowMultipleResponses);
        Assert.Equal(updateDto.ShowResults, survey.ShowResults);
    }

    [Fact]
    public void Map_Survey_With_No_Responses_Success()
    {
        // Arrange
        var survey = new Survey
        {
            Id = 1,
            Title = "New Survey",
            IsActive = true,
            Responses = new List<Response>()
        };

        // Act
        var dto = _mapper.Map<SurveyDto>(survey);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(0, dto.TotalResponses);
        Assert.Equal(0, dto.CompletedResponses);
    }
}
