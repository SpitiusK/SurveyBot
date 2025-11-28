using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for Response entity AutoMapper mappings.
/// Updated in v1.5.0 to use internal methods instead of reflection.
/// </summary>
public class ResponseMappingTests
{
    private readonly IMapper _mapper;

    public ResponseMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ResponseMappingProfile>();
            cfg.AddProfile<AnswerMappingProfile>();
            cfg.AddProfile<QuestionMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        // Arrange & Act & Assert
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ResponseMappingProfile>();
            cfg.AddProfile<AnswerMappingProfile>();
            cfg.AddProfile<QuestionMappingProfile>();
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_Response_To_ResponseDto_Success()
    {
        // Arrange
        var survey = EntityBuilder.CreateSurvey();
        survey.SetId(1);

        var question1 = EntityBuilder.CreateQuestion(surveyId: 1);
        question1.SetId(1);
        var question2 = EntityBuilder.CreateQuestion(surveyId: 1);
        question2.SetId(2);
        var question3 = EntityBuilder.CreateQuestion(surveyId: 1);
        question3.SetId(3);
        var question4 = EntityBuilder.CreateQuestion(surveyId: 1);
        question4.SetId(4);
        var question5 = EntityBuilder.CreateQuestion(surveyId: 1);
        question5.SetId(5);

        // Add questions using internal method instead of reflection
        survey.AddQuestionInternal(question1);
        survey.AddQuestionInternal(question2);
        survey.AddQuestionInternal(question3);
        survey.AddQuestionInternal(question4);
        survey.AddQuestionInternal(question5);

        var response = EntityBuilder.CreateResponse(surveyId: 1, respondentTelegramId: 123456789);
        response.SetId(1);
        response.SetSurveyInternal(survey);

        var answer1 = EntityBuilder.CreateAnswer(responseId: 1, questionId: 1);
        answer1.SetId(1);
        var answer2 = EntityBuilder.CreateAnswer(responseId: 1, questionId: 2);
        answer2.SetId(2);

        response.AddAnswerInternal(answer1);
        response.AddAnswerInternal(answer2);

        // Act
        var dto = _mapper.Map<ResponseDto>(response);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(response.Id, dto.Id);
        Assert.Equal(response.SurveyId, dto.SurveyId);
        Assert.Equal(response.RespondentTelegramId, dto.RespondentTelegramId);
        Assert.Equal(response.IsComplete, dto.IsComplete);
        Assert.Equal(response.StartedAt, dto.StartedAt);
        Assert.Null(dto.SubmittedAt);
        Assert.Equal(2, dto.AnsweredCount); // 2 answers
        Assert.Equal(5, dto.TotalQuestions); // 5 questions in survey
    }

    [Fact]
    public void Map_Response_Complete_To_ResponseDto_Success()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var submitTime = DateTime.UtcNow;

        var survey = EntityBuilder.CreateSurvey();
        survey.SetId(1);

        var question1 = EntityBuilder.CreateQuestion(surveyId: 1);
        question1.SetId(1);
        var question2 = EntityBuilder.CreateQuestion(surveyId: 1);
        question2.SetId(2);

        // Add questions using internal method instead of reflection
        survey.AddQuestionInternal(question1);
        survey.AddQuestionInternal(question2);

        var response = EntityBuilder.CreateResponse(surveyId: 1, respondentTelegramId: 987654321, isComplete: true);
        response.SetId(2);
        response.SetStartedAt(startTime);
        response.SetSubmittedAt(submitTime);
        response.SetSurveyInternal(survey);

        var answer1 = EntityBuilder.CreateAnswer(responseId: 2, questionId: 1);
        answer1.SetId(1);
        var answer2 = EntityBuilder.CreateAnswer(responseId: 2, questionId: 2);
        answer2.SetId(2);

        response.AddAnswerInternal(answer1);
        response.AddAnswerInternal(answer2);

        // Act
        var dto = _mapper.Map<ResponseDto>(response);

        // Assert
        Assert.NotNull(dto);
        Assert.True(dto.IsComplete);
        Assert.Equal(startTime, dto.StartedAt);
        Assert.Equal(submitTime, dto.SubmittedAt);
        Assert.Equal(2, dto.AnsweredCount);
        Assert.Equal(2, dto.TotalQuestions);
    }

    [Fact]
    public void Map_Response_To_ResponseListDto_Success()
    {
        // Arrange
        var survey = EntityBuilder.CreateSurvey();
        survey.SetId(2);

        var question1 = EntityBuilder.CreateQuestion(surveyId: 2);
        question1.SetId(1);
        var question2 = EntityBuilder.CreateQuestion(surveyId: 2);
        question2.SetId(2);
        var question3 = EntityBuilder.CreateQuestion(surveyId: 2);
        question3.SetId(3);

        // Add questions using internal method instead of reflection
        survey.AddQuestionInternal(question1);
        survey.AddQuestionInternal(question2);
        survey.AddQuestionInternal(question3);

        var response = EntityBuilder.CreateResponse(surveyId: 2, respondentTelegramId: 111222333);
        response.SetId(3);
        response.SetSurveyInternal(survey);

        var answer1 = EntityBuilder.CreateAnswer(responseId: 3, questionId: 1);
        answer1.SetId(1);

        response.AddAnswerInternal(answer1);

        // Act
        var dto = _mapper.Map<ResponseListDto>(response);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(response.Id, dto.Id);
        Assert.Equal(response.SurveyId, dto.SurveyId);
        Assert.Equal(response.RespondentTelegramId, dto.RespondentTelegramId);
        Assert.False(dto.IsComplete);
        Assert.Equal(1, dto.AnsweredCount);
        Assert.Equal(3, dto.TotalQuestions);
    }

    [Fact]
    public void Map_CreateResponseDto_To_Response_Success()
    {
        // Arrange
        var createDto = new CreateResponseDto
        {
            RespondentTelegramId = 555666777,
            RespondentUsername = "testuser",
            RespondentFirstName = "Test"
        };

        // Act
        var response = _mapper.Map<Response>(createDto);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(createDto.RespondentTelegramId, response.RespondentTelegramId);
        Assert.False(response.IsComplete);
        Assert.NotNull(response.StartedAt);
        Assert.Null(response.SubmittedAt);
    }

    [Fact]
    public void Map_Response_With_No_Answers_Success()
    {
        // Arrange
        var survey = EntityBuilder.CreateSurvey();
        survey.SetId(1);

        var question1 = EntityBuilder.CreateQuestion(surveyId: 1);
        question1.SetId(1);

        // Add question using internal method instead of reflection
        survey.AddQuestionInternal(question1);

        var response = EntityBuilder.CreateResponse(surveyId: 1, respondentTelegramId: 123);
        response.SetId(4);
        response.SetSurveyInternal(survey);
        // No answers added - response starts with empty answers collection

        // Act
        var dto = _mapper.Map<ResponseDto>(response);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(0, dto.AnsweredCount);
        Assert.Equal(1, dto.TotalQuestions);
    }
}
