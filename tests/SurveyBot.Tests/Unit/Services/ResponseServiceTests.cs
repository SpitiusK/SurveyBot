using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.Models;
using SurveyBot.Infrastructure.Services;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.Services;

/// <summary>
/// Unit tests for ResponseService.
/// </summary>
public class ResponseServiceTests
{
    private readonly Mock<IResponseRepository> _responseRepositoryMock;
    private readonly Mock<IAnswerRepository> _answerRepositoryMock;
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock;
    private readonly Mock<IQuestionRepository> _questionRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ResponseService>> _loggerMock;
    private readonly ResponseService _sut;

    public ResponseServiceTests()
    {
        _responseRepositoryMock = new Mock<IResponseRepository>();
        _answerRepositoryMock = new Mock<IAnswerRepository>();
        _surveyRepositoryMock = new Mock<ISurveyRepository>();
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ResponseService>>();

        _sut = new ResponseService(
            _responseRepositoryMock.Object,
            _answerRepositoryMock.Object,
            _surveyRepositoryMock.Object,
            _questionRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    #region StartResponseAsync Tests

    [Fact]
    public async Task StartResponseAsync_WithValidData_CreatesResponseSuccessfully()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;
        var username = "testuser";
        var firstName = "Test";

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            IsActive = true,
            AllowMultipleResponses = false,
            CreatorId = 1
        };

        var response = new Response
        {
            Id = 1,
            SurveyId = surveyId,
            RespondentTelegramId = telegramUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            Answers = new List<Answer>()
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _responseRepositoryMock.Setup(r => r.HasUserCompletedSurveyAsync(surveyId, telegramUserId)).ReturnsAsync(false);
        _responseRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Response>())).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(surveyId)).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.StartResponseAsync(surveyId, telegramUserId, username, firstName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(surveyId, result.SurveyId);
        Assert.Equal(telegramUserId, result.RespondentTelegramId);
        Assert.False(result.IsComplete);
        Assert.NotNull(result.StartedAt);
        Assert.Null(result.SubmittedAt);

        _responseRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Response>()), Times.Once);
    }

    [Fact]
    public async Task StartResponseAsync_WhenSurveyNotFound_ThrowsSurveyNotFoundException()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync((Survey?)null);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyNotFoundException>(() =>
            _sut.StartResponseAsync(surveyId, telegramUserId));
    }

    [Fact]
    public async Task StartResponseAsync_WhenSurveyInactive_ThrowsSurveyOperationException()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            IsActive = false,
            CreatorId = 1
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(() =>
            _sut.StartResponseAsync(surveyId, telegramUserId));
    }

    [Fact]
    public async Task StartResponseAsync_WhenUserAlreadyCompleted_ThrowsDuplicateResponseException()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            IsActive = true,
            AllowMultipleResponses = false,
            CreatorId = 1
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _responseRepositoryMock.Setup(r => r.HasUserCompletedSurveyAsync(surveyId, telegramUserId)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateResponseException>(() =>
            _sut.StartResponseAsync(surveyId, telegramUserId));
    }

    [Fact]
    public async Task StartResponseAsync_WhenAllowMultipleResponses_AllowsDuplicates()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;

        var survey = new Survey
        {
            Id = surveyId,
            Title = "Test Survey",
            IsActive = true,
            AllowMultipleResponses = true,
            CreatorId = 1
        };

        var response = new Response
        {
            Id = 1,
            SurveyId = surveyId,
            RespondentTelegramId = telegramUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            Answers = new List<Answer>()
        };

        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _responseRepositoryMock.Setup(r => r.HasUserCompletedSurveyAsync(surveyId, telegramUserId)).ReturnsAsync(true);
        _responseRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Response>())).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(surveyId)).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.StartResponseAsync(surveyId, telegramUserId);

        // Assert
        Assert.NotNull(result);
        _responseRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Response>()), Times.Once);
    }

    #endregion

    #region SaveAnswerAsync Tests

    [Fact]
    public async Task SaveAnswerAsync_WithValidTextAnswer_SavesSuccessfully()
    {
        // Arrange
        var responseId = 1;
        var questionId = 1;
        var answerText = "This is my answer";

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = false,
            Answers = new List<Answer>()
        };

        var question = new Question
        {
            Id = questionId,
            SurveyId = 1,
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(new Answer { Id = 1 });
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, answerText: answerText);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.AnswerText == answerText
        )), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_WhenResponseNotFound_ThrowsResponseNotFoundException()
    {
        // Arrange
        var responseId = 999;
        var questionId = 1;

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync((Response?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResponseNotFoundException>(() =>
            _sut.SaveAnswerAsync(responseId, questionId, answerText: "test"));
    }

    [Fact]
    public async Task SaveAnswerAsync_WhenResponseCompleted_ThrowsSurveyOperationException()
    {
        // Arrange
        var responseId = 1;
        var questionId = 1;

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = true
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<SurveyOperationException>(() =>
            _sut.SaveAnswerAsync(responseId, questionId, answerText: "test"));
    }

    [Fact]
    public async Task SaveAnswerAsync_WhenQuestionNotFound_ThrowsQuestionNotFoundException()
    {
        // Arrange
        var responseId = 1;
        var questionId = 999;

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = false
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync((Question?)null);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionNotFoundException>(() =>
            _sut.SaveAnswerAsync(responseId, questionId, answerText: "test"));
    }

    [Fact]
    public async Task SaveAnswerAsync_WhenQuestionNotInSurvey_ThrowsQuestionValidationException()
    {
        // Arrange
        var responseId = 1;
        var questionId = 1;

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = false
        };

        var question = new Question
        {
            Id = questionId,
            SurveyId = 999, // Different survey
            QuestionType = QuestionType.Text
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act & Assert
        await Assert.ThrowsAsync<QuestionValidationException>(() =>
            _sut.SaveAnswerAsync(responseId, questionId, answerText: "test"));
    }

    [Fact]
    public async Task SaveAnswerAsync_UpdatesExistingAnswer_WhenAnswerExists()
    {
        // Arrange
        var responseId = 1;
        var questionId = 1;
        var newAnswerText = "Updated answer";

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = false,
            Answers = new List<Answer>()
        };

        var question = new Question
        {
            Id = questionId,
            SurveyId = 1,
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        var existingAnswer = new Answer
        {
            Id = 10,
            ResponseId = responseId,
            QuestionId = questionId,
            AnswerText = "Old answer"
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync(existingAnswer);
        _answerRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Answer>())).ReturnsAsync(existingAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, answerText: newAnswerText);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Answer>(a =>
            a.Id == 10 &&
            a.AnswerText == newAnswerText
        )), Times.Once);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Answer>()), Times.Never);
    }

    #endregion

    #region CompleteResponseAsync Tests

    [Fact]
    public async Task CompleteResponseAsync_WithValidResponse_CompletesSuccessfully()
    {
        // Arrange
        var responseId = 1;

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = false,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            Answers = new List<Answer>()
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _responseRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Response>())).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.CompleteResponseAsync(responseId);

        // Assert
        Assert.NotNull(result);
        _responseRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Response>(resp =>
            resp.IsComplete == true &&
            resp.SubmittedAt != null
        )), Times.Once);
    }

    [Fact]
    public async Task CompleteResponseAsync_WhenResponseNotFound_ThrowsResponseNotFoundException()
    {
        // Arrange
        var responseId = 999;

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync((Response?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResponseNotFoundException>(() =>
            _sut.CompleteResponseAsync(responseId));
    }

    [Fact]
    public async Task CompleteResponseAsync_WhenAlreadyCompleted_ReturnsExistingResponse()
    {
        // Arrange
        var responseId = 1;
        var submittedAt = DateTime.UtcNow.AddMinutes(-5);

        var response = new Response
        {
            Id = responseId,
            SurveyId = 1,
            IsComplete = true,
            SubmittedAt = submittedAt,
            Answers = new List<Answer>()
        };

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.CompleteResponseAsync(responseId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsComplete);
        _responseRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Response>()), Times.Never);
    }

    #endregion

    #region ValidateAnswerFormatAsync Tests

    [Fact]
    public async Task ValidateAnswerFormatAsync_TextQuestion_WithValidAnswer_ReturnsSuccess()
    {
        // Arrange
        var questionId = 1;
        var answerText = "This is a valid answer";

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, answerText: answerText);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_TextQuestion_RequiredWithEmpty_ReturnsFailure()
    {
        // Arrange
        var questionId = 1;

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.Text,
            IsRequired = true
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, answerText: "");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal("Text answer is required", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_TextQuestion_ExceedsMaxLength_ReturnsFailure()
    {
        // Arrange
        var questionId = 1;
        var answerText = new string('a', 5001); // Exceeds 5000 char limit

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.Text,
            IsRequired = false
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, answerText: answerText);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("cannot exceed 5000 characters", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_SingleChoice_WithValidOption_ReturnsSuccess()
    {
        // Arrange
        var questionId = 1;
        var selectedOptions = new List<string> { "Option A" };
        var validOptions = new List<string> { "Option A", "Option B", "Option C" };

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            OptionsJson = JsonSerializer.Serialize(validOptions)
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, selectedOptions: selectedOptions);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_SingleChoice_WithMultipleOptions_ReturnsFailure()
    {
        // Arrange
        var questionId = 1;
        var selectedOptions = new List<string> { "Option A", "Option B" };

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            OptionsJson = JsonSerializer.Serialize(new List<string> { "Option A", "Option B" })
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, selectedOptions: selectedOptions);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("Only one option", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_SingleChoice_WithInvalidOption_ReturnsFailure()
    {
        // Arrange
        var questionId = 1;
        var selectedOptions = new List<string> { "Invalid Option" };
        var validOptions = new List<string> { "Option A", "Option B", "Option C" };

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            OptionsJson = JsonSerializer.Serialize(validOptions)
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, selectedOptions: selectedOptions);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("not valid", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_MultipleChoice_WithValidOptions_ReturnsSuccess()
    {
        // Arrange
        var questionId = 1;
        var selectedOptions = new List<string> { "Option A", "Option C" };
        var validOptions = new List<string> { "Option A", "Option B", "Option C" };

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.MultipleChoice,
            IsRequired = true,
            OptionsJson = JsonSerializer.Serialize(validOptions)
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, selectedOptions: selectedOptions);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_MultipleChoice_WithInvalidOptions_ReturnsFailure()
    {
        // Arrange
        var questionId = 1;
        var selectedOptions = new List<string> { "Option A", "Invalid Option" };
        var validOptions = new List<string> { "Option A", "Option B", "Option C" };

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.MultipleChoice,
            IsRequired = true,
            OptionsJson = JsonSerializer.Serialize(validOptions)
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, selectedOptions: selectedOptions);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("Invalid options", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_Rating_WithValidValue_ReturnsSuccess()
    {
        // Arrange
        var questionId = 1;
        var ratingValue = 4;

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.Rating,
            IsRequired = true
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(10)]
    public async Task ValidateAnswerFormatAsync_Rating_WithInvalidValue_ReturnsFailure(int invalidRating)
    {
        // Arrange
        var questionId = 1;

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.Rating,
            IsRequired = false
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, ratingValue: invalidRating);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("between 1 and 5", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAnswerFormatAsync_Rating_RequiredWithNull_ReturnsFailure()
    {
        // Arrange
        var questionId = 1;

        var question = new Question
        {
            Id = questionId,
            QuestionType = QuestionType.Rating,
            IsRequired = true
        };

        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);

        // Act
        var result = await _sut.ValidateAnswerFormatAsync(questionId, ratingValue: null);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal("Rating is required", result.ErrorMessage);
    }

    #endregion

    #region ResumeResponseAsync Tests

    [Fact]
    public async Task ResumeResponseAsync_WithIncompleteResponse_ReturnsExisting()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;

        var incompleteResponse = new Response
        {
            Id = 10,
            SurveyId = surveyId,
            RespondentTelegramId = telegramUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            Answers = new List<Answer>()
        };

        _responseRepositoryMock.Setup(r => r.GetIncompleteResponseAsync(surveyId, telegramUserId))
            .ReturnsAsync(incompleteResponse);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(surveyId)).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.ResumeResponseAsync(surveyId, telegramUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.False(result.IsComplete);
        _responseRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Response>()), Times.Never);
    }

    [Fact]
    public async Task ResumeResponseAsync_WithNoIncompleteResponse_StartsNew()
    {
        // Arrange
        var surveyId = 1;
        var telegramUserId = 123456789L;

        var survey = new Survey
        {
            Id = surveyId,
            IsActive = true,
            AllowMultipleResponses = false,
            CreatorId = 1
        };

        var newResponse = new Response
        {
            Id = 20,
            SurveyId = surveyId,
            RespondentTelegramId = telegramUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow,
            Answers = new List<Answer>()
        };

        _responseRepositoryMock.Setup(r => r.GetIncompleteResponseAsync(surveyId, telegramUserId))
            .ReturnsAsync((Response?)null);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _responseRepositoryMock.Setup(r => r.HasUserCompletedSurveyAsync(surveyId, telegramUserId)).ReturnsAsync(false);
        _responseRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Response>())).ReturnsAsync(newResponse);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(surveyId)).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.ResumeResponseAsync(surveyId, telegramUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.Id);
        _responseRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Response>()), Times.Once);
    }

    #endregion

    #region DeleteResponseAsync Tests

    [Fact]
    public async Task DeleteResponseAsync_WithValidAuthorization_DeletesSuccessfully()
    {
        // Arrange
        var responseId = 1;
        var userId = 10;
        var surveyId = 5;

        var response = new Response
        {
            Id = responseId,
            SurveyId = surveyId,
            IsComplete = true
        };

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = userId
        };

        _responseRepositoryMock.Setup(r => r.GetByIdAsync(responseId)).ReturnsAsync(response);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);
        _responseRepositoryMock.Setup(r => r.DeleteAsync(responseId)).ReturnsAsync(true);

        // Act
        await _sut.DeleteResponseAsync(responseId, userId);

        // Assert
        _responseRepositoryMock.Verify(r => r.DeleteAsync(responseId), Times.Once);
    }

    [Fact]
    public async Task DeleteResponseAsync_WhenNotAuthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var responseId = 1;
        var userId = 10;
        var surveyId = 5;

        var response = new Response
        {
            Id = responseId,
            SurveyId = surveyId
        };

        var survey = new Survey
        {
            Id = surveyId,
            CreatorId = 999 // Different user
        };

        _responseRepositoryMock.Setup(r => r.GetByIdAsync(responseId)).ReturnsAsync(response);
        _surveyRepositoryMock.Setup(r => r.GetByIdAsync(surveyId)).ReturnsAsync(survey);

        // Act & Assert
        await Assert.ThrowsAsync<Core.Exceptions.UnauthorizedAccessException>(() =>
            _sut.DeleteResponseAsync(responseId, userId));
    }

    #endregion

    #region GetCompletedResponseCountAsync Tests

    [Fact]
    public async Task GetCompletedResponseCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var surveyId = 1;
        var expectedCount = 42;

        _responseRepositoryMock.Setup(r => r.GetCompletedCountAsync(surveyId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _sut.GetCompletedResponseCountAsync(surveyId);

        // Assert
        Assert.Equal(expectedCount, result);
    }

    #endregion
}
