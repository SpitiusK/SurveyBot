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
            cfg.AddProfile<QuestionMappingProfile>();
            cfg.AddProfile<ResponseMappingProfile>();
            cfg.AddProfile<AnswerMappingProfile>();
            cfg.AddProfile<UserMappingProfile>();
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
            cfg.AddProfile<QuestionMappingProfile>();
            cfg.AddProfile<ResponseMappingProfile>();
            cfg.AddProfile<AnswerMappingProfile>();
            cfg.AddProfile<UserMappingProfile>();
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_Survey_To_SurveyDto_Success()
    {
        // Arrange
        var survey = Survey.Create(
            title: "Customer Satisfaction Survey",
            creatorId: 100,
            description: "Please rate our service",
            code: null,
            isActive: true,
            allowMultipleResponses: false,
            showResults: true);
        survey.SetId(1);

        var question1 = Question.CreateTextQuestion(1, "How satisfied are you?", 0, true);
        question1.SetId(1);

        var question2 = Question.CreateTextQuestion(1, "Would you recommend us?", 1, true);
        question2.SetId(2);

        survey.AddQuestionInternal(question1);
        survey.AddQuestionInternal(question2);

        var response1 = Response.Create(1, 123456, DateTime.UtcNow, true, DateTime.UtcNow);
        response1.SetId(1);

        var response2 = Response.Create(1, 234567, DateTime.UtcNow, false, null);
        response2.SetId(2);

        var response3 = Response.Create(1, 345678, DateTime.UtcNow, true, DateTime.UtcNow);
        response3.SetId(3);

        survey.AddResponseInternal(response1);
        survey.AddResponseInternal(response2);
        survey.AddResponseInternal(response3);

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
        var survey = Survey.Create(
            title: "Product Feedback",
            creatorId: 1,
            description: "Help us improve",
            code: null,
            isActive: true,
            allowMultipleResponses: false,
            showResults: true);
        survey.SetId(1);

        var question1 = Question.CreateTextQuestion(1, "Question 1", 0, true);
        question1.SetId(1);

        var question2 = Question.CreateTextQuestion(1, "Question 2", 1, true);
        question2.SetId(2);

        var question3 = Question.CreateTextQuestion(1, "Question 3", 2, true);
        question3.SetId(3);

        survey.AddQuestionInternal(question1);
        survey.AddQuestionInternal(question2);
        survey.AddQuestionInternal(question3);

        var response1 = Response.Create(1, 123456, DateTime.UtcNow, true, DateTime.UtcNow);
        response1.SetId(1);

        var response2 = Response.Create(1, 234567, DateTime.UtcNow, true, DateTime.UtcNow);
        response2.SetId(2);

        survey.AddResponseInternal(response1);
        survey.AddResponseInternal(response2);

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
        var survey = Survey.Create(
            title: "New Survey",
            creatorId: 1,
            description: null,
            code: null,
            isActive: true,
            allowMultipleResponses: false,
            showResults: true);
        survey.SetId(1);

        // Act
        var dto = _mapper.Map<SurveyDto>(survey);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(0, dto.TotalResponses);
        Assert.Equal(0, dto.CompletedResponses);
    }
}
