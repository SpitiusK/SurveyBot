using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Options;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Validators;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Xunit;

namespace SurveyBot.Tests.Integration.Bot;

/// <summary>
/// Integration tests for bot error handling scenarios.
/// Tests validation, edge cases, and error recovery.
/// </summary>
public class ErrorHandlingTests : IClassFixture<BotTestFixture>
{
    private readonly BotTestFixture _fixture;
    private readonly TextQuestionHandler _textHandler;
    private readonly SingleChoiceQuestionHandler _singleChoiceHandler;
    private readonly SurveyCommandHandler _surveyCommandHandler;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;

    private const long TestUserId = 555666777;
    private const long TestChatId = 555666777;

    public ErrorHandlingTests(BotTestFixture fixture)
    {
        _fixture = fixture;

        _validator = new AnswerValidator(Mock.Of<ILogger<AnswerValidator>>());
        _errorHandler = new QuestionErrorHandler(_fixture.MockBotService.Object, Mock.Of<ILogger<QuestionErrorHandler>>());

        // Create QuestionMediaHelper with mocked dependencies
        var mediaHelper = new QuestionMediaHelper(
            Mock.Of<ITelegramMediaService>(),
            Options.Create(new BotConfiguration { ApiBaseUrl = "http://localhost:5000" }),
            Mock.Of<ILogger<QuestionMediaHelper>>());

        _textHandler = new TextQuestionHandler(
            _fixture.MockBotService.Object,
            _validator,
            _errorHandler,
            mediaHelper,
            Mock.Of<ILogger<TextQuestionHandler>>());

        _singleChoiceHandler = new SingleChoiceQuestionHandler(
            _fixture.MockBotService.Object,
            _validator,
            _errorHandler,
            mediaHelper,
            Mock.Of<ILogger<SingleChoiceQuestionHandler>>());

        // Create mock for IResponseService
        var mockResponseService = new Mock<IResponseService>();
        mockResponseService
            .Setup(x => x.CompleteResponseAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync((int responseId, int? userId) => new ResponseDto
            {
                Id = responseId,
                IsComplete = true,
                SubmittedAt = DateTime.UtcNow
            });

        var completionHandler = new CompletionHandler(
            _fixture.MockBotService.Object,
            mockResponseService.Object,
            _fixture.SurveyRepository,
            _fixture.StateManager,
            Mock.Of<ILogger<CompletionHandler>>());

        _surveyCommandHandler = new SurveyCommandHandler(
            _fixture.MockBotService.Object,
            _fixture.SurveyRepository,
            _fixture.ResponseRepository,
            _fixture.StateManager,
            completionHandler,
            new List<IQuestionHandler> { _textHandler, _singleChoiceHandler },
            Mock.Of<ILogger<SurveyCommandHandler>>());
    }

    [Fact]
    public async Task TextQuestion_TooLongInput_ReturnsValidationError()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId, surveyId, 300, 4);

        var longText = new string('A', 4001); // Exceeds 4000 character limit
        var message = _fixture.CreateTestMessage(TestUserId, TestChatId, longText);
        var question = CreateQuestionDto(1, "Text question", QuestionType.Text, true);

        // Act
        var result = await _textHandler.ProcessAnswerAsync(message, null, question, TestUserId, CancellationToken.None);

        // Assert
        result.Should().BeNull(); // Validation failed

        // Verify error message was sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId &&
                    req.Text.Contains("too long")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TextQuestion_EmptyRequiredAnswer_ReturnsValidationError()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 1, surveyId, 301, 4);

        var message = _fixture.CreateTestMessage(TestUserId + 1, TestChatId + 1, "   "); // Empty/whitespace
        var question = CreateQuestionDto(1, "Required text question", QuestionType.Text, true);

        // Act
        var result = await _textHandler.ProcessAnswerAsync(message, null, question, TestUserId + 1, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        // Verify error message
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 1 &&
                    req.Text.Contains("required")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SingleChoiceQuestion_InvalidOption_ReturnsValidationError()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 2, surveyId, 302, 4);

        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 2, TestChatId + 2, "option_99_InvalidOption");
        var question = CreateQuestionDto(2, "Single choice", QuestionType.SingleChoice, true, new[] { "Red", "Blue", "Green" });

        // Act
        var result = await _singleChoiceHandler.ProcessAnswerAsync(null, callback, question, TestUserId + 2, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        // Verify error was shown
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 2 &&
                    (req.Text.Contains("Invalid") || req.Text.Contains("not valid"))),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SkipRequiredQuestion_ReturnsError()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 3, surveyId, 303, 4);

        var message = _fixture.CreateTestMessage(TestUserId + 3, TestChatId + 3, "/skip");
        var question = CreateQuestionDto(1, "Required question", QuestionType.Text, true);

        // Act
        var result = await _textHandler.ProcessAnswerAsync(message, null, question, TestUserId + 3, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        // Verify error shown
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 3 &&
                    req.Text.Contains("required") && req.Text.Contains("cannot be skipped")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SessionTimeout_HandlesExpiredState()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 4, surveyId, 304, 4);

        // Get state and manually expire it
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 4);
        state.Should().NotBeNull();

        // Set last activity to 31 minutes ago (past expiration)
        var stateType = state!.GetType();
        var lastActivityField = stateType.GetProperty("LastActivityAt");
        lastActivityField!.SetValue(state, DateTime.UtcNow.AddMinutes(-31));

        await _fixture.StateManager.SetStateAsync(TestUserId + 4, state);

        // Act - Try to get expired state
        // Note: GetStateAsync should return null for expired states (30+ min inactive)
        // or the state manager should update CurrentState to SessionExpired
        var expiredState = await _fixture.StateManager.GetStateAsync(TestUserId + 4);

        // Assert - State should either be null or marked as expired
        if (expiredState != null)
        {
            expiredState.CurrentState.Should().Be(ConversationStateType.SessionExpired);
        }
        else
        {
            // If null is returned, the session timeout is working (state cleaned up)
            expiredState.Should().BeNull();
        }
    }

    [Fact]
    public async Task StartSurvey_InvalidSurveyId_SendsErrorMessage()
    {
        // Arrange
        var invalidSurveyId = 99999;
        var message = _fixture.CreateTestMessage(TestUserId + 5, TestChatId + 5, $"/survey {invalidSurveyId}");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 5);
        state.Should().BeNull(); // State not created for invalid survey

        // Verify error message sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 5 &&
                    req.Text.Contains("not found")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSurvey_InactiveSurvey_SendsErrorMessage()
    {
        // Arrange - Create inactive survey
        var inactiveSurvey = EntityBuilder.CreateSurvey("Inactive Survey", "This is inactive", _fixture.TestUser.Id, false);
        inactiveSurvey.SetAllowMultipleResponses(false);
        await _fixture.DbContext.Surveys.AddAsync(inactiveSurvey);
        await _fixture.DbContext.SaveChangesAsync();

        var message = _fixture.CreateTestMessage(TestUserId + 6, TestChatId + 6, $"/survey {inactiveSurvey.Id}");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 6);
        state.Should().BeNull(); // State not created for inactive survey

        // Verify error message sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 6 &&
                    req.Text.Contains("not currently accepting")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSurvey_DuplicateResponse_SendsErrorWhenNotAllowed()
    {
        // Arrange - Create survey that doesn't allow multiple responses
        var singleResponseSurvey = EntityBuilder.CreateSurvey("Single Response Survey", "Only one response allowed", _fixture.TestUser.Id, true);
        singleResponseSurvey.SetAllowMultipleResponses(false);
        await _fixture.DbContext.Surveys.AddAsync(singleResponseSurvey);
        await _fixture.DbContext.SaveChangesAsync();

        // Create completed response
        var existingResponse = EntityBuilder.CreateResponse(singleResponseSurvey.Id, TestUserId + 7, true);
        existingResponse.MarkAsComplete();
        await _fixture.DbContext.Responses.AddAsync(existingResponse);
        await _fixture.DbContext.SaveChangesAsync();

        var message = _fixture.CreateTestMessage(TestUserId + 7, TestChatId + 7, $"/survey {singleResponseSurvey.Id}");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 7);
        state.Should().BeNull(); // State not created due to duplicate

        // Verify error message sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 7 &&
                    req.Text.Contains("already completed")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private QuestionDto CreateQuestionDto(int id, string text, QuestionType type, bool required, string[]? options = null)
    {
        return new QuestionDto
        {
            Id = id,
            SurveyId = _fixture.TestSurvey.Id,
            QuestionText = text,
            QuestionType = type,
            OrderIndex = 0,
            IsRequired = required,
            Options = options?.ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
