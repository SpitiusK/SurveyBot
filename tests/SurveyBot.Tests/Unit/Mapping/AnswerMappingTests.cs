using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;
using SurveyBot.Tests.Fixtures;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for Answer entity AutoMapper mappings.
/// Updated in v1.5.0 to use AnswerValue value objects.
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
        var question = EntityBuilder.CreateQuestion(questionText: "What do you think?", questionType: QuestionType.Text);
        question.SetId(1);

        var textValue = TextAnswerValue.Create("This is my text answer");
        var answer = Answer.CreateWithValue(responseId: 1, questionId: 1, value: textValue);
        answer.SetId(1);
        answer.SetQuestionInternal(question);

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(answer.Id, dto.Id);
        Assert.Equal(answer.QuestionId, dto.QuestionId);
        Assert.Equal("What do you think?", dto.QuestionText);
        Assert.Equal(QuestionType.Text, dto.QuestionType);
        Assert.Equal("This is my text answer", dto.AnswerText);
        Assert.Null(dto.SelectedOptions); // Text answers have null SelectedOptions
        Assert.Null(dto.RatingValue);
    }

    [Fact]
    public void Map_Answer_SingleChoice_To_AnswerDto_Success()
    {
        // Arrange
        var selectedOption = "Option B";
        var question = EntityBuilder.CreateQuestion(questionText: "Pick one", questionType: QuestionType.SingleChoice);
        question.SetId(2);

        var singleChoiceValue = SingleChoiceAnswerValue.CreateTrusted(selectedOption);
        var answer = Answer.CreateWithValue(responseId: 1, questionId: 2, value: singleChoiceValue);
        answer.SetId(2);
        answer.SetQuestionInternal(question);

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
        var question = EntityBuilder.CreateQuestion(questionText: "Select all that apply", questionType: QuestionType.MultipleChoice);
        question.SetId(3);

        var multipleChoiceValue = MultipleChoiceAnswerValue.CreateTrusted(selectedOptions);
        var answer = Answer.CreateWithValue(responseId: 1, questionId: 3, value: multipleChoiceValue);
        answer.SetId(3);
        answer.SetQuestionInternal(question);

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.AnswerText);
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
        var question = EntityBuilder.CreateQuestion(questionText: "Rate us", questionType: QuestionType.Rating);
        question.SetId(4);

        var ratingAnswerValue = RatingAnswerValue.Create(ratingValue);
        var answer = Answer.CreateWithValue(responseId: 1, questionId: 4, value: ratingAnswerValue);
        answer.SetId(4);
        answer.SetQuestionInternal(question);

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.AnswerText);
        Assert.Null(dto.SelectedOptions); // Rating answers have null SelectedOptions
        Assert.NotNull(dto.RatingValue);
        Assert.Equal(4, dto.RatingValue);
    }

    [Fact]
    public void Map_Answer_Legacy_SingleChoice_To_AnswerDto_Success()
    {
        // Arrange - test legacy AnswerJson format conversion
        var selectedOption = "Option B";
        var question = EntityBuilder.CreateQuestion(questionText: "Pick one", questionType: QuestionType.SingleChoice);
        question.SetId(2);

        // Create answer with legacy JSON format (no Value set)
        var answer = EntityBuilder.CreateTextAnswer(responseId: 1, questionId: 2, answerText: null);
        answer.SetId(2);
        answer.SetValue(null); // Ensure Value is null to trigger legacy fallback
        answer.SetAnswerJson(JsonSerializer.Serialize(new { selectedOption }));
        answer.SetQuestionInternal(question);

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.AnswerText);
        Assert.NotNull(dto.SelectedOptions);
        Assert.Single(dto.SelectedOptions);
        Assert.Equal(selectedOption, dto.SelectedOptions[0]);
    }

    [Fact]
    public void Map_Answer_Legacy_Rating_To_AnswerDto_Success()
    {
        // Arrange - test legacy AnswerJson format conversion
        var ratingValue = 5;
        var question = EntityBuilder.CreateQuestion(questionText: "Rate us", questionType: QuestionType.Rating);
        question.SetId(4);

        // Create answer with legacy JSON format (no Value set)
        var answer = EntityBuilder.CreateTextAnswer(responseId: 1, questionId: 4, answerText: null);
        answer.SetId(4);
        answer.SetValue(null); // Ensure Value is null to trigger legacy fallback
        answer.SetAnswerJson(JsonSerializer.Serialize(new { rating = ratingValue }));
        answer.SetQuestionInternal(question);

        // Act
        var dto = _mapper.Map<AnswerDto>(answer);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.AnswerText);
        Assert.NotNull(dto.RatingValue);
        Assert.Equal(ratingValue, dto.RatingValue);
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
        // Note: In v1.5.0, ResponseService handles Value creation via AnswerValueFactory
        // AutoMapper only sets AnswerText, the service sets Value
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
        // Note: In v1.5.0, AnswerJson is set by ResponseService, not AutoMapper
        // AutoMapper ignores AnswerJson (set by service if needed for backward compatibility)
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
        // Note: In v1.5.0, ResponseService handles the full answer creation
        // AutoMapper just creates the basic Answer entity
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
        // Note: In v1.5.0, ResponseService handles Value creation via AnswerValueFactory
    }
}
