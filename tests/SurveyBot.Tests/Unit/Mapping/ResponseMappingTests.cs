using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for Response entity AutoMapper mappings.
/// </summary>
public class ResponseMappingTests
{
    private readonly IMapper _mapper;

    public ResponseMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ResponseMappingProfile>();
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
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_Response_To_ResponseDto_Success()
    {
        // Arrange
        var response = new Response
        {
            Id = 1,
            SurveyId = 1,
            RespondentTelegramId = 123456789,
            IsComplete = false,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            SubmittedAt = null,
            Survey = new Survey
            {
                Id = 1,
                Questions = new List<Question>
                {
                    new Question { Id = 1 },
                    new Question { Id = 2 },
                    new Question { Id = 3 },
                    new Question { Id = 4 },
                    new Question { Id = 5 }
                }
            },
            Answers = new List<Answer>
            {
                new Answer { Id = 1, QuestionId = 1 },
                new Answer { Id = 2, QuestionId = 2 }
            }
        };

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
        var response = new Response
        {
            Id = 2,
            SurveyId = 1,
            RespondentTelegramId = 987654321,
            IsComplete = true,
            StartedAt = startTime,
            SubmittedAt = submitTime,
            Survey = new Survey
            {
                Id = 1,
                Questions = new List<Question>
                {
                    new Question { Id = 1 },
                    new Question { Id = 2 }
                }
            },
            Answers = new List<Answer>
            {
                new Answer { Id = 1, QuestionId = 1 },
                new Answer { Id = 2, QuestionId = 2 }
            }
        };

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
        var response = new Response
        {
            Id = 3,
            SurveyId = 2,
            RespondentTelegramId = 111222333,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            SubmittedAt = null,
            Survey = new Survey
            {
                Id = 2,
                Questions = new List<Question>
                {
                    new Question { Id = 1 },
                    new Question { Id = 2 },
                    new Question { Id = 3 }
                }
            },
            Answers = new List<Answer>
            {
                new Answer { Id = 1 }
            }
        };

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
        var response = new Response
        {
            Id = 4,
            SurveyId = 1,
            RespondentTelegramId = 123,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            Survey = new Survey
            {
                Id = 1,
                Questions = new List<Question>
                {
                    new Question { Id = 1 }
                }
            },
            Answers = new List<Answer>()
        };

        // Act
        var dto = _mapper.Map<ResponseDto>(response);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(0, dto.AnsweredCount);
        Assert.Equal(1, dto.TotalQuestions);
    }
}
