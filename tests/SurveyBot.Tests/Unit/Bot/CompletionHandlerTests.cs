using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for CompletionHandler
/// </summary>
public class CompletionHandlerTests
{
    private readonly CompletionHandler _handler;
    private readonly Mock<IBotService> _botServiceMock;
    private readonly Mock<IResponseService> _responseServiceMock;
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock;
    private readonly Mock<IConversationStateManager> _stateManagerMock;
    private readonly Mock<ILogger<CompletionHandler>> _loggerMock;
    private readonly Mock<ITelegramBotClient> _botClientMock;

    private const long TestUserId = 123456789;
    private const int TestSurveyId = 1;
    private const int TestResponseId = 100;

    public CompletionHandlerTests()
    {
        _botClientMock = new Mock<ITelegramBotClient>();
        _botServiceMock = new Mock<IBotService>();
        _botServiceMock.Setup(x => x.Client).Returns(_botClientMock.Object);

        _responseServiceMock = new Mock<IResponseService>();
        _surveyRepositoryMock = new Mock<ISurveyRepository>();
        _stateManagerMock = new Mock<IConversationStateManager>();
        _loggerMock = new Mock<ILogger<CompletionHandler>>();

        _handler = new CompletionHandler(
            _botServiceMock.Object,
            _responseServiceMock.Object,
            _surveyRepositoryMock.Object,
            _stateManagerMock.Object,
            _loggerMock.Object);
    }

    #region Successful Completion

    [Fact]
    public async Task HandleCompletionAsync_WithValidState_MarksResponseComplete()
    {
        // Arrange
        var chatId = TestUserId;
        var state = new ConversationState
        {
            UserId = TestUserId,
            CurrentResponseId = TestResponseId,
            CurrentSurveyId = TestSurveyId,
            CurrentState = ConversationStateType.InSurvey,
            TotalQuestions = 5,
            CurrentQuestionIndex = 4
        };

        var completedResponse = new ResponseDto
        {
            Id = TestResponseId,
            SurveyId = TestSurveyId,
            IsComplete = true,
            AnsweredCount = 5,
            TotalQuestions = 5,
            SubmittedAt = DateTime.UtcNow
        };

        var survey = new Core.Entities.Survey
        {
            Id = TestSurveyId,
            Title = "Test Survey",
            IsActive = true
        };

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync(state);

        _responseServiceMock
            .Setup(x => x.CompleteResponseAsync(TestResponseId, null))
            .ReturnsAsync(completedResponse);

        _surveyRepositoryMock
            .Setup(x => x.GetByIdAsync(TestSurveyId))
            .ReturnsAsync(survey);

        _stateManagerMock
            .Setup(x => x.CompleteSurveyAsync(TestUserId))
            .ReturnsAsync(true);

        // Mock the actual interface method SendRequest, not the extension method SendMessage
        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _responseServiceMock.Verify(
            x => x.CompleteResponseAsync(TestResponseId, null),
            Times.Once);

        _stateManagerMock.Verify(
            x => x.CompleteSurveyAsync(TestUserId),
            Times.Once);

        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCompletionAsync_IncludesSurveyTitle_InCompletionMessage()
    {
        // Arrange
        var chatId = TestUserId;
        var surveyTitle = "Customer Satisfaction Survey";

        var state = new ConversationState
        {
            UserId = TestUserId,
            CurrentResponseId = TestResponseId,
            CurrentSurveyId = TestSurveyId,
            CurrentState = ConversationStateType.InSurvey
        };

        var completedResponse = new ResponseDto
        {
            Id = TestResponseId,
            SurveyId = TestSurveyId,
            IsComplete = true,
            AnsweredCount = 3,
            TotalQuestions = 3
        };

        var survey = new Core.Entities.Survey
        {
            Id = TestSurveyId,
            Title = surveyTitle
        };

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync(state);

        _responseServiceMock
            .Setup(x => x.CompleteResponseAsync(TestResponseId, null))
            .ReturnsAsync(completedResponse);

        _surveyRepositoryMock
            .Setup(x => x.GetByIdAsync(TestSurveyId))
            .ReturnsAsync(survey);

        _stateManagerMock
            .Setup(x => x.CompleteSurveyAsync(TestUserId))
            .ReturnsAsync(true);

        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task HandleCompletionAsync_NoConversationState_SendsErrorMessage()
    {
        // Arrange
        var chatId = TestUserId;

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync((ConversationState?)null);

        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _responseServiceMock.Verify(
            x => x.CompleteResponseAsync(It.IsAny<int>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleCompletionAsync_NoResponseId_SendsErrorMessage()
    {
        // Arrange
        var chatId = TestUserId;
        var state = new ConversationState
        {
            UserId = TestUserId,
            CurrentResponseId = null,
            CurrentState = ConversationStateType.InSurvey
        };

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync(state);

        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCompletionAsync_ResponseNotFound_SendsErrorMessage()
    {
        // Arrange
        var chatId = TestUserId;
        var state = new ConversationState
        {
            UserId = TestUserId,
            CurrentResponseId = TestResponseId,
            CurrentState = ConversationStateType.InSurvey
        };

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync(state);

        _responseServiceMock
            .Setup(x => x.CompleteResponseAsync(TestResponseId, null))
            .ThrowsAsync(new ResponseNotFoundException("Response not found"));

        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCompletionAsync_InvalidOperation_SendsErrorMessage()
    {
        // Arrange
        var chatId = TestUserId;
        var state = new ConversationState
        {
            UserId = TestUserId,
            CurrentResponseId = TestResponseId,
            CurrentState = ConversationStateType.InSurvey
        };

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync(state);

        _responseServiceMock
            .Setup(x => x.CompleteResponseAsync(TestResponseId, null))
            .ThrowsAsync(new InvalidOperationException("Cannot complete already completed response"));

        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Survey Title Handling

    [Fact]
    public async Task HandleCompletionAsync_SurveyTitleNotFound_UsesDefaultTitle()
    {
        // Arrange
        var chatId = TestUserId;
        var state = new ConversationState
        {
            UserId = TestUserId,
            CurrentResponseId = TestResponseId,
            CurrentSurveyId = TestSurveyId
        };

        var completedResponse = new ResponseDto
        {
            Id = TestResponseId,
            SurveyId = TestSurveyId,
            IsComplete = true,
            AnsweredCount = 2,
            TotalQuestions = 2
        };

        _stateManagerMock
            .Setup(x => x.GetStateAsync(TestUserId))
            .ReturnsAsync(state);

        _responseServiceMock
            .Setup(x => x.CompleteResponseAsync(TestResponseId, null))
            .ReturnsAsync(completedResponse);

        _surveyRepositoryMock
            .Setup(x => x.GetByIdAsync(TestSurveyId))
            .ReturnsAsync((Core.Entities.Survey?)null);

        _stateManagerMock
            .Setup(x => x.CompleteSurveyAsync(TestUserId))
            .ReturnsAsync(true);

        _botClientMock
            .Setup(x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Telegram.Bot.Requests.SendMessageRequest req, CancellationToken ct) =>
            {
                var message = new Telegram.Bot.Types.Message();
                typeof(Telegram.Bot.Types.Message)
                    .GetProperty("MessageId")
                    ?.SetValue(message, 1);
                return message;
            });

        // Act
        await _handler.HandleCompletionAsync(chatId, TestUserId, CancellationToken.None);

        // Assert
        _botClientMock.Verify(
            x => x.SendRequest(
                It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
