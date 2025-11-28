using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.API.Mapping.ValueResolvers;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for Question entity AutoMapper mappings.
/// </summary>
public class QuestionMappingTests
{
    private readonly IMapper _mapper;

    public QuestionMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
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
            cfg.AddProfile<QuestionMappingProfile>();
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_Question_To_QuestionDto_WithOptions_Success()
    {
        // Arrange
        var options = new List<string> { "Option 1", "Option 2", "Option 3" };
        var question = Question.CreateSingleChoiceQuestion(
            surveyId: 1,
            questionText: "What is your favorite color?",
            orderIndex: 0,
            optionsJson: "[\"Option 1\", \"Option 2\", \"Option 3\"]");
        question.SetId(1); // Use internal method instead of reflection
        question.SetIsRequired(true);
        question.SetOptionsJson(JsonSerializer.Serialize(options));

        // Act
        var dto = _mapper.Map<QuestionDto>(question);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(question.Id, dto.Id);
        Assert.Equal(question.QuestionText, dto.QuestionText);
        Assert.Equal(question.QuestionType, dto.QuestionType);
        Assert.Equal(question.IsRequired, dto.IsRequired);
        Assert.NotNull(dto.Options);
        Assert.Equal(3, dto.Options.Count);
        Assert.Equal("Option 1", dto.Options[0]);
        Assert.Equal("Option 2", dto.Options[1]);
        Assert.Equal("Option 3", dto.Options[2]);
    }

    [Fact]
    public void Map_Question_To_QuestionDto_WithoutOptions_Success()
    {
        // Arrange
        var question = Question.CreateTextQuestion(
            surveyId: 1,
            questionText: "Please provide your feedback",
            orderIndex: 1);
        question.SetId(2); // Use internal method instead of reflection
        question.SetIsRequired(false);
        question.SetOptionsJson(null);

        // Act
        var dto = _mapper.Map<QuestionDto>(question);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(question.Id, dto.Id);
        Assert.Equal(question.QuestionText, dto.QuestionText);
        Assert.Equal(question.QuestionType, dto.QuestionType);
        // Resolver returns empty list (not null) when OptionsJson is null
        Assert.NotNull(dto.Options);
        Assert.Empty(dto.Options);
    }

    [Fact]
    public void Map_CreateQuestionDto_To_Question_WithOptions_Success()
    {
        // Arrange
        var createDto = new CreateQuestionDto
        {
            QuestionText = "How satisfied are you?",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            Options = new List<string> { "Very Satisfied", "Satisfied", "Neutral", "Dissatisfied" }
        };

        // Act
        var question = _mapper.Map<Question>(createDto);

        // Assert
        Assert.NotNull(question);
        Assert.Equal(createDto.QuestionText, question.QuestionText);
        Assert.Equal(createDto.QuestionType, question.QuestionType);
        Assert.Equal(createDto.IsRequired, question.IsRequired);
        Assert.NotNull(question.OptionsJson);

        var deserializedOptions = JsonSerializer.Deserialize<List<string>>(question.OptionsJson);
        Assert.NotNull(deserializedOptions);
        Assert.Equal(4, deserializedOptions.Count);
        Assert.Equal("Very Satisfied", deserializedOptions[0]);
    }

    [Fact]
    public void Map_CreateQuestionDto_To_Question_WithoutOptions_Success()
    {
        // Arrange
        var createDto = new CreateQuestionDto
        {
            QuestionText = "Rate our service",
            QuestionType = QuestionType.Rating,
            IsRequired = true,
            Options = null
        };

        // Act
        var question = _mapper.Map<Question>(createDto);

        // Assert
        Assert.NotNull(question);
        Assert.Equal(createDto.QuestionText, question.QuestionText);
        Assert.Equal(createDto.QuestionType, question.QuestionType);
        Assert.Null(question.OptionsJson);
    }

    [Fact]
    public void Map_UpdateQuestionDto_To_Question_Success()
    {
        // Arrange
        var updateDto = new UpdateQuestionDto
        {
            QuestionText = "Updated question text",
            QuestionType = QuestionType.MultipleChoice,
            IsRequired = false,
            Options = new List<string> { "A", "B", "C" }
        };

        // Act
        var question = _mapper.Map<Question>(updateDto);

        // Assert
        Assert.NotNull(question);
        Assert.Equal(updateDto.QuestionText, question.QuestionText);
        Assert.Equal(updateDto.QuestionType, question.QuestionType);
        Assert.NotNull(question.OptionsJson);
    }

    [Fact]
    public void QuestionOptionsResolver_InvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var question = Question.CreateSingleChoiceQuestion(
            surveyId: 1,
            questionText: "Test",
            orderIndex: 0,
            optionsJson: "invalid json {]");
        question.SetId(1); // Use internal method instead of reflection

        // Act
        var dto = _mapper.Map<QuestionDto>(question);

        // Assert - Resolver returns empty list for invalid JSON as a safe fallback
        Assert.NotNull(dto.Options);
        Assert.Empty(dto.Options);
    }
}
