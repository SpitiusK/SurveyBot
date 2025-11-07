using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.Entities;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for Answer entity AutoMapper mappings.
/// </summary>
public class AnswerMappingTests
{
    private readonly IMapper _mapper;

    public AnswerMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AnswerMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        // Arrange & Act & Assert
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AnswerMappingProfile>();
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_Answer_TextQuestion_To_AnswerDto_Success()
    {
        // Arrange
        var answer = new Answer
        {
            Id = 1,
            ResponseId = 1,
            QuestionId = 1,
            AnswerText = "This is my text answer",
            AnswerJson = null,
            CreatedAt = DateTime.UtcNow,
            Question = new Question
            {
                Id = 1,
                QuestionText = "What do you think?",
                QuestionType = QuestionType.Text
            }
        };

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(answer.Id, dto.Id);
        Assert.Equal(answer.QuestionId, dto.QuestionId);
        Assert.Equal("What do you think?", dto.QuestionText);
        Assert.Equal(QuestionType.Text, dto.QuestionType);
        Assert.Equal("This is my text answer", dto.AnswerText);
        Assert.Null(dto.SelectedOptions);
        Assert.Null(dto.RatingValue);
    }

    [Fact]
    public void Map_Answer_SingleChoice_To_AnswerDto_Success()
    {
        // Arrange
        var selectedOption = "Option B";
        var answer = new Answer
        {
            Id = 2,
            ResponseId = 1,
            QuestionId = 2,
            AnswerText = null,
            AnswerJson = JsonSerializer.Serialize(new List<string> { selectedOption }),
            CreatedAt = DateTime.UtcNow,
            Question = new Question
            {
                Id = 2,
                QuestionText = "Pick one",
                QuestionType = QuestionType.SingleChoice
            }
        };

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.AnswerText);
        Assert.NotNull(dto.SelectedOptions);
        Assert.Single(dto.SelectedOptions);
        Assert.Equal(selectedOption, dto.SelectedOptions[0]);
        Assert.Null(dto.RatingValue);
    }

    [Fact]
    public void Map_Answer_MultipleChoice_To_AnswerDto_Success()
    {
        // Arrange
        var selectedOptions = new List<string> { "Option A", "Option C", "Option D" };
        var answer = new Answer
        {
            Id = 3,
            ResponseId = 1,
            QuestionId = 3,
            AnswerText = null,
            AnswerJson = JsonSerializer.Serialize(selectedOptions),
            CreatedAt = DateTime.UtcNow,
            Question = new Question
            {
                Id = 3,
                QuestionText = "Select all that apply",
                QuestionType = QuestionType.MultipleChoice
            }
        };

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.NotNull(dto.SelectedOptions);
        Assert.Equal(3, dto.SelectedOptions.Count);
        Assert.Contains("Option A", dto.SelectedOptions);
        Assert.Contains("Option C", dto.SelectedOptions);
        Assert.Contains("Option D", dto.SelectedOptions);
    }

    [Fact]
    public void Map_Answer_Rating_To_AnswerDto_Success()
    {
        // Arrange
        var ratingValue = 4;
        var answer = new Answer
        {
            Id = 4,
            ResponseId = 1,
            QuestionId = 4,
            AnswerText = null,
            AnswerJson = JsonSerializer.Serialize(ratingValue),
            CreatedAt = DateTime.UtcNow,
            Question = new Question
            {
                Id = 4,
                QuestionText = "Rate us",
                QuestionType = QuestionType.Rating
            }
        };

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.AnswerText);
        Assert.Null(dto.SelectedOptions);
        Assert.NotNull(dto.RatingValue);
        Assert.Equal(4, dto.RatingValue);
    }

    [Fact]
    public void Map_CreateAnswerDto_Text_To_Answer_Success()
    {
        // Arrange
        var createDto = new CreateAnswerDto
        {
            QuestionId = 1,
            AnswerText = "My detailed response",
            SelectedOptions = null,
            RatingValue = null
        };

        // Act
        var answer = _mapper.Map<Answer>(createDto);

        // Assert
        Assert.NotNull(answer);
        Assert.Equal(createDto.QuestionId, answer.QuestionId);
        Assert.Equal("My detailed response", answer.AnswerText);
        Assert.Null(answer.AnswerJson);
    }

    [Fact]
    public void Map_CreateAnswerDto_SingleChoice_To_Answer_Success()
    {
        // Arrange
        var createDto = new CreateAnswerDto
        {
            QuestionId = 2,
            AnswerText = null,
            SelectedOptions = new List<string> { "Choice A" },
            RatingValue = null
        };

        // Act
        var answer = _mapper.Map<Answer>(createDto);

        // Assert
        Assert.NotNull(answer);
        Assert.Null(answer.AnswerText);
        Assert.NotNull(answer.AnswerJson);

        var deserializedOptions = JsonSerializer.Deserialize<List<string>>(answer.AnswerJson);
        Assert.NotNull(deserializedOptions);
        Assert.Single(deserializedOptions);
        Assert.Equal("Choice A", deserializedOptions[0]);
    }

    [Fact]
    public void Map_CreateAnswerDto_MultipleChoice_To_Answer_Success()
    {
        // Arrange
        var createDto = new CreateAnswerDto
        {
            QuestionId = 3,
            AnswerText = null,
            SelectedOptions = new List<string> { "A", "B", "C" },
            RatingValue = null
        };

        // Act
        var answer = _mapper.Map<Answer>(createDto);

        // Assert
        Assert.NotNull(answer);
        Assert.NotNull(answer.AnswerJson);

        var deserializedOptions = JsonSerializer.Deserialize<List<string>>(answer.AnswerJson);
        Assert.NotNull(deserializedOptions);
        Assert.Equal(3, deserializedOptions.Count);
    }

    [Fact]
    public void Map_CreateAnswerDto_Rating_To_Answer_Success()
    {
        // Arrange
        var createDto = new CreateAnswerDto
        {
            QuestionId = 4,
            AnswerText = null,
            SelectedOptions = null,
            RatingValue = 5
        };

        // Act
        var answer = _mapper.Map<Answer>(createDto);

        // Assert
        Assert.NotNull(answer);
        Assert.Null(answer.AnswerText);
        Assert.NotNull(answer.AnswerJson);

        var deserializedRating = JsonSerializer.Deserialize<int>(answer.AnswerJson);
        Assert.Equal(5, deserializedRating);
    }
}
