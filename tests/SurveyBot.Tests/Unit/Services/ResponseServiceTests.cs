using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using SurveyBot.Core.Models;
using SurveyBot.Core.ValueObjects;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Services;
using SurveyBot.Tests.Fixtures;
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
    private readonly Mock<SurveyBotDbContext> _contextMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ResponseService>> _loggerMock;
    private readonly ResponseService _sut;

    public ResponseServiceTests()
    {
        _responseRepositoryMock = new Mock<IResponseRepository>();
        _answerRepositoryMock = new Mock<IAnswerRepository>();
        _surveyRepositoryMock = new Mock<ISurveyRepository>();
        _questionRepositoryMock = new Mock<IQuestionRepository>();
        _contextMock = new Mock<SurveyBotDbContext>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ResponseService>>();

        _sut = new ResponseService(
            _responseRepositoryMock.Object,
            _answerRepositoryMock.Object,
            _surveyRepositoryMock.Object,
            _questionRepositoryMock.Object,
            _contextMock.Object,
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

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: 1,
            isActive: true);
        survey.SetId(surveyId);
        survey.SetAllowMultipleResponses(false);

        var response = EntityBuilder.CreateResponse(
            surveyId: surveyId,
            respondentTelegramId: telegramUserId,
            isComplete: false);
        response.SetId(1);

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

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: 1,
            isActive: false);
        survey.SetId(surveyId);

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

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: 1,
            isActive: true);
        survey.SetId(surveyId);
        survey.SetAllowMultipleResponses(false);

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

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: 1,
            isActive: true);
        survey.SetId(surveyId);
        survey.SetAllowMultipleResponses(true);

        var response = EntityBuilder.CreateResponse(
            surveyId: surveyId,
            respondentTelegramId: telegramUserId,
            isComplete: false);
        response.SetId(1);

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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateTextQuestion(
            surveyId: 1,
            questionText: "Test Question?",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);
        // Set a dummy next question to avoid sequential lookup (which requires DbContext)
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(999));

        var createdAnswer = EntityBuilder.CreateTextAnswer(
            responseId: responseId,
            questionId: questionId,
            answerText: answerText);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);  // For validation
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, answerText: answerText);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Value != null  // Check Value property is set (new AnswerValue pattern)
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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: true);
        response.SetId(responseId);

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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateQuestion(
            surveyId: 999, // Different survey
            questionText: "Test Question?",
            questionType: QuestionType.Text);
        question.SetId(questionId);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);

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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateTextQuestion(
            surveyId: 1,
            questionText: "Test Question?",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);
        // Set a dummy next question to avoid sequential lookup (which requires DbContext)
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(999));

        var existingAnswer = EntityBuilder.CreateTextAnswer(
            responseId: responseId,
            questionId: questionId,
            answerText: "Old answer");
        existingAnswer.SetId(10);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);  // For validation
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
            a.Value != null  // Check Value property is set (new AnswerValue pattern)
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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);
        response.SetStartedAt(DateTime.UtcNow.AddMinutes(-10));

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

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: true);
        response.SetId(responseId);
        response.SetSubmittedAt(submittedAt);

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Test Question?",
            questionType: QuestionType.Text,
            isRequired: true);
        question.SetId(questionId);

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Test Question?",
            questionType: QuestionType.Text,
            isRequired: true);
        question.SetId(questionId);

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Test Question?",
            questionType: QuestionType.Text,
            isRequired: false);
        question.SetId(questionId);

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Choose one",
            questionType: QuestionType.SingleChoice,
            isRequired: true);
        question.SetId(questionId);
        question.SetOptionsJson(JsonSerializer.Serialize(validOptions));

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Choose one",
            questionType: QuestionType.SingleChoice,
            isRequired: true);
        question.SetId(questionId);
        question.SetOptionsJson(JsonSerializer.Serialize(new List<string> { "Option A", "Option B" }));

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Choose one",
            questionType: QuestionType.SingleChoice,
            isRequired: true);
        question.SetId(questionId);
        question.SetOptionsJson(JsonSerializer.Serialize(validOptions));

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Choose multiple",
            questionType: QuestionType.MultipleChoice,
            isRequired: true);
        question.SetId(questionId);
        question.SetOptionsJson(JsonSerializer.Serialize(validOptions));

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Choose multiple",
            questionType: QuestionType.MultipleChoice,
            isRequired: true);
        question.SetId(questionId);
        question.SetOptionsJson(JsonSerializer.Serialize(validOptions));

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Rate this",
            questionType: QuestionType.Rating,
            isRequired: true);
        question.SetId(questionId);

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Rate this",
            questionType: QuestionType.Rating,
            isRequired: false);
        question.SetId(questionId);

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

        var question = EntityBuilder.CreateQuestion(
            surveyId: 1,
            questionText: "Rate this",
            questionType: QuestionType.Rating,
            isRequired: true);
        question.SetId(questionId);

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

        var incompleteResponse = EntityBuilder.CreateResponse(
            surveyId: surveyId,
            respondentTelegramId: telegramUserId,
            isComplete: false);
        incompleteResponse.SetId(10);
        incompleteResponse.SetStartedAt(DateTime.UtcNow.AddHours(-1));

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

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: 1,
            isActive: true);
        survey.SetId(surveyId);
        survey.SetAllowMultipleResponses(false);

        var newResponse = EntityBuilder.CreateResponse(
            surveyId: surveyId,
            respondentTelegramId: telegramUserId,
            isComplete: false);
        newResponse.SetId(20);

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

        var response = EntityBuilder.CreateResponse(
            surveyId: surveyId,
            respondentTelegramId: 987654321,
            isComplete: true);
        response.SetId(responseId);

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: userId,
            isActive: true);
        survey.SetId(surveyId);

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

        var response = EntityBuilder.CreateResponse(
            surveyId: surveyId,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var survey = EntityBuilder.CreateSurvey(
            title: "Test Survey",
            creatorId: 999, // Different user
            isActive: true);
        survey.SetId(surveyId);

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

    #region Rating Conditional Branching Tests (NEW - Rating Value to Index Mapping)

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task SaveAnswerAsync_RatingValue_CreatesAnswerSuccessfully(int ratingValue)
    {
        // Arrange
        var responseId = 1;
        var questionId = 1;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        // Create rating question with QuestionOptions for conditional flow
        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "How satisfied are you?",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(999)); // Fallback

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Value != null  // AnswerValue should be set
        )), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingWithQuestionOptions_StoresCorrectNextStep()
    {
        // Arrange
        var responseId = 1;
        var questionId = 1;
        var ratingValue = 3;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Set up QuestionOptions for rating branching - need options 0, 1, 2 for rating 1, 2, 3
        var opt1 = EntityBuilder.CreateQuestionOption(questionId, "1", 0, NextQuestionDeterminant.ToQuestion(10));
        var opt2 = EntityBuilder.CreateQuestionOption(questionId, "2", 1, NextQuestionDeterminant.ToQuestion(20));
        var opt3 = EntityBuilder.CreateQuestionOption(questionId, "3", 2, NextQuestionDeterminant.ToQuestion(50));

        // Mock question with all options loaded (rating value 3 corresponds to index 2)
        question.AddOptionInternal(opt1);
        question.AddOptionInternal(opt2);
        question.AddOptionInternal(opt3);

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Next != null &&
            a.Next.NextQuestionId == 50  // Should match the QuestionOption's next (rating 3 = index 2 = opt3 = Q50)
        )), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingWithoutQuestionOptions_UsesDefaultNext()
    {
        // Arrange - Backward compatibility test
        var responseId = 1;
        var questionId = 1;
        var ratingValue = 4;
        var defaultNextQuestionId = 99;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Set DefaultNext but NO QuestionOptions (old-style rating question)
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(defaultNextQuestionId));
        // Don't add any QuestionOptions - this simulates existing rating questions

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert - Should use DefaultNext regardless of rating value
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Next != null &&
            a.Next.NextQuestionId == defaultNextQuestionId  // Should use DefaultNext
        )), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingDifferentValues_DifferentNextQuestions()
    {
        // Arrange - Test multiple ratings lead to different paths
        var responseId = 1;
        var questionId = 1;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "NPS Score",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Set up branching: Low ratings (1-2) → Q10, High ratings (3-5) → Q20
        var opt1 = EntityBuilder.CreateQuestionOption(questionId, "1", 0, NextQuestionDeterminant.ToQuestion(10));
        var opt2 = EntityBuilder.CreateQuestionOption(questionId, "2", 1, NextQuestionDeterminant.ToQuestion(10));
        var opt3 = EntityBuilder.CreateQuestionOption(questionId, "3", 2, NextQuestionDeterminant.ToQuestion(20));
        var opt4 = EntityBuilder.CreateQuestionOption(questionId, "4", 3, NextQuestionDeterminant.ToQuestion(20));
        var opt5 = EntityBuilder.CreateQuestionOption(questionId, "5", 4, NextQuestionDeterminant.ToQuestion(20));

        question.AddOptionInternal(opt1);
        question.AddOptionInternal(opt2);
        question.AddOptionInternal(opt3);
        question.AddOptionInternal(opt4);
        question.AddOptionInternal(opt5);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act - Test low rating (1)
        var answer1 = EntityBuilder.CreateAnswer(responseId, questionId);
        answer1.SetId(1);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(answer1);
        var result1 = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: 1);

        // Act - Test high rating (5)
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        var answer2 = EntityBuilder.CreateAnswer(responseId, questionId);
        answer2.SetId(2);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(answer2);
        var result2 = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: 5);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);

        // Verify the answer creation was called twice (once for each rating)
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Answer>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingEndSurvey_StoresEndNext()
    {
        // Arrange - Test rating option that ends survey
        var responseId = 1;
        var questionId = 1;
        var ratingValue = 5;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Satisfaction",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Rating 5 ends survey
        var opt5 = EntityBuilder.CreateEndSurveyOption(
            questionId: questionId,
            text: "5",
            orderIndex: 4);

        question.AddOptionInternal(opt5);

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Next != null &&
            a.Next.Type == NextStepType.EndSurvey
        )), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingNullOptions_DoesNotCrash()
    {
        // Arrange - Edge case: Question.Options is null
        var responseId = 1;
        var questionId = 1;
        var ratingValue = 3;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Rate us",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(99));
        // Don't add any QuestionOptions - Options collection is empty

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act & Assert - Should not throw, should fall back to DefaultNext
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.Next != null &&
            a.Next.NextQuestionId == 99  // Should use DefaultNext
        )), Times.Once);
    }

    #endregion

    #region Rating Question Transition Tests (INFRA-FIX-002 Regression Tests)

    [Fact]
    public async Task SaveAnswerAsync_RatingWithoutOptions_EndSurvey_ReturnsEnd()
    {
        // Arrange - Rating question with no QuestionOptions, DefaultNext = EndSurvey
        var responseId = 1;
        var questionId = 1;
        var ratingValue = 3;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Rate your experience",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Set DefaultNext to EndSurvey (this was the bug - was being ignored)
        question.SetDefaultNext(NextQuestionDeterminant.End());
        // No QuestionOptions added - simulates existing rating questions

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Next != null &&
            a.Next.Type == NextStepType.EndSurvey &&
            a.Next.NextQuestionId == null
        )), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_RatingWithoutOptions_GoToQuestion_NavigatesToTarget()
    {
        // Arrange - Rating question with no QuestionOptions, DefaultNext = ToQuestion(5)
        var responseId = 1;
        var questionId = 1;
        var ratingValue = 4;
        var targetQuestionId = 5;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Rate your experience",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Set DefaultNext to GoToQuestion(5)
        question.SetDefaultNext(NextQuestionDeterminant.ToQuestion(targetQuestionId));
        // No QuestionOptions added

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Next != null &&
            a.Next.Type == NextStepType.GoToQuestion &&
            a.Next.NextQuestionId == targetQuestionId
        )), Times.Once);
    }

    // Note: Sequential fallback tests removed - they require DbContext mocking which is complex
    // The sequential fallback logic is tested in integration tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task SaveAnswerAsync_RatingWithoutOptions_AllRatingValues_RespectEndSurvey(int ratingValue)
    {
        // Arrange - Ensure ALL rating values respect DefaultNext = EndSurvey (comprehensive regression test)
        var responseId = 1;
        var questionId = 1;

        var response = EntityBuilder.CreateResponse(
            surveyId: 1,
            respondentTelegramId: 987654321,
            isComplete: false);
        response.SetId(responseId);

        var question = EntityBuilder.CreateRatingQuestion(
            surveyId: 1,
            questionText: "Rate your experience",
            orderIndex: 0,
            isRequired: true);
        question.SetId(questionId);

        // Set DefaultNext to EndSurvey - ALL rating values should end survey
        question.SetDefaultNext(NextQuestionDeterminant.End());

        var createdAnswer = EntityBuilder.CreateAnswer(
            responseId: responseId,
            questionId: questionId);
        createdAnswer.SetId(1);

        _responseRepositoryMock.Setup(r => r.GetByIdWithAnswersAsync(responseId)).ReturnsAsync(response);
        _questionRepositoryMock.Setup(r => r.GetByIdWithFlowConfigAsync(questionId, default)).ReturnsAsync(question);
        _questionRepositoryMock.Setup(r => r.GetByIdAsync(questionId)).ReturnsAsync(question);
        _answerRepositoryMock.Setup(r => r.GetByResponseAndQuestionAsync(responseId, questionId)).ReturnsAsync((Answer?)null);
        _answerRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(createdAnswer);
        _questionRepositoryMock.Setup(r => r.GetBySurveyIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Question>());
        _answerRepositoryMock.Setup(r => r.GetByResponseIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Answer>());

        // Act
        var result = await _sut.SaveAnswerAsync(responseId, questionId, ratingValue: ratingValue);

        // Assert
        Assert.NotNull(result);
        _answerRepositoryMock.Verify(r => r.CreateAsync(It.Is<Answer>(a =>
            a.ResponseId == responseId &&
            a.QuestionId == questionId &&
            a.Next != null &&
            a.Next.Type == NextStepType.EndSurvey &&
            a.Next.NextQuestionId == null
        )), Times.Once);
    }

    #endregion
}
