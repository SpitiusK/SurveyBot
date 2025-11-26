using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Handlers;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Utilities;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for SurveyResponseHandler with focus on conditional question flow.
/// Tests the handler's ability to:
/// - Navigate through branching questions
/// - Prevent revisiting answered questions (cycle prevention)
/// - Handle survey completion
/// - Manage conversation state correctly
/// </summary>
public class SurveyResponseHandlerTests : IDisposable
{
    private readonly Mock<IBotService> _mockBotService;
    private readonly Mock<ITelegramBotClient> _mockBotClient;
    private readonly Mock<IConversationStateManager> _mockStateManager;
    private readonly Mock<IQuestionHandler> _mockTextQuestionHandler;
    private readonly Mock<IQuestionHandler> _mockSingleChoiceHandler;
    private readonly Mock<ISurveyRepository> _mockSurveyRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<BotPerformanceMonitor> _mockPerformanceMonitor;
    private readonly Mock<SurveyCache> _mockSurveyCache;
    private readonly Mock<SurveyNavigationHelper> _mockNavigationHelper;
    private readonly Mock<ILogger<SurveyResponseHandler>> _mockLogger;
    private readonly SurveyResponseHandler _handler;
    private readonly BotConfiguration _botConfiguration;

    public SurveyResponseHandlerTests()
    {
        // Setup bot service and client
        _mockBotService = new Mock<IBotService>();
        _mockBotClient = new Mock<ITelegramBotClient>();
        _mockBotService.Setup(s => s.Client).Returns(_mockBotClient.Object);

        // Setup state manager
        _mockStateManager = new Mock<IConversationStateManager>();

        // Setup question handlers
        _mockTextQuestionHandler = new Mock<IQuestionHandler>();
        _mockTextQuestionHandler.Setup(h => h.QuestionType).Returns(QuestionType.Text);

        _mockSingleChoiceHandler = new Mock<IQuestionHandler>();
        _mockSingleChoiceHandler.Setup(h => h.QuestionType).Returns(QuestionType.SingleChoice);

        var questionHandlers = new List<IQuestionHandler>
        {
            _mockTextQuestionHandler.Object,
            _mockSingleChoiceHandler.Object
        };

        // Setup repository and mapper
        _mockSurveyRepository = new Mock<ISurveyRepository>();
        _mockMapper = new Mock<IMapper>();

        // Setup HTTP client with mocked handler
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        // Setup configuration
        _botConfiguration = new BotConfiguration
        {
            BotToken = "test-token",
            ApiBaseUrl = "http://localhost:5000",
            RequestTimeout = 30
        };
        var options = Options.Create(_botConfiguration);

        // Setup performance monitor and cache
        _mockPerformanceMonitor = new Mock<BotPerformanceMonitor>(
            Mock.Of<ILogger<BotPerformanceMonitor>>());

        _mockSurveyCache = new Mock<SurveyCache>(
            Mock.Of<ILogger<SurveyCache>>());

        // Setup navigation helper
        _mockNavigationHelper = new Mock<SurveyNavigationHelper>(
            _mockHttpClientFactory.Object,
            Mock.Of<ILogger<SurveyNavigationHelper>>());

        _mockLogger = new Mock<ILogger<SurveyResponseHandler>>();

        // Create handler instance
        _handler = new SurveyResponseHandler(
            _mockBotService.Object,
            _mockStateManager.Object,
            questionHandlers,
            _mockSurveyRepository.Object,
            _mockMapper.Object,
            _httpClient,
            options,
            _mockPerformanceMonitor.Object,
            _mockSurveyCache.Object,
            _mockNavigationHelper.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Test 1: HandleMessageResponse_ValidAnswer_UpdatesStateAndSendsNextQuestion

    [Fact]
    public async Task HandleMessageResponse_ValidAnswer_UpdatesStateAndSendsNextQuestion()
    {
        // Arrange
        var message = CreateTestMessage("My answer to question 1");
        var conversationState = CreateTestConversationState(surveyId: 1, responseId: 100, questionIndex: 0);

        var currentQuestion = CreateTestQuestionDto(1, "Question 1?", QuestionType.Text);
        var nextQuestion = CreateTestQuestionDto(2, "Question 2?", QuestionType.Text);
        var survey = CreateTestSurveyDto(1, "Test Survey", new[] { currentQuestion, nextQuestion });

        // Setup state manager
        _mockStateManager
            .Setup(m => m.GetStateAsync(It.IsAny<long>()))
            .ReturnsAsync(conversationState);

        // Setup survey fetch
        SetupSurveyFetch(survey);

        // Setup question handler
        _mockTextQuestionHandler
            .Setup(h => h.ProcessAnswerAsync(
                It.IsAny<Message>(),
                null,
                It.IsAny<QuestionDto>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"text\":\"My answer to question 1\"}");

        // Setup HTTP client for answer submission
        SetupHttpResponse(HttpMethod.Post, "/api/responses/100/answers", HttpStatusCode.OK);

        // Setup navigation helper to return next question
        _mockNavigationHelper
            .Setup(h => h.GetNextQuestionAsync(
                100,
                1,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(QuestionNavigationResult.WithNextQuestion(nextQuestion));

        // Setup question display
        _mockTextQuestionHandler
            .Setup(h => h.DisplayQuestionAsync(
                It.IsAny<long>(),
                It.IsAny<QuestionDto>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.HandleMessageResponseAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify state was updated with visited question
        conversationState.HasVisitedQuestion(1).Should().BeTrue();

        // Verify answer was recorded
        _mockStateManager.Verify(
            m => m.AnswerQuestionAsync(
                message.From.Id,
                0,
                It.IsAny<string>()),
            Times.Once);

        // Verify next question was displayed
        _mockTextQuestionHandler.Verify(
            h => h.DisplayQuestionAsync(
                message.Chat.Id,
                It.Is<QuestionDto>(q => q.Id == 2),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Test 2: HandleMessageResponse_AnswerLeadsToCompletion_SendsCompletionMessage

    [Fact]
    public async Task HandleMessageResponse_AnswerLeadsToCompletion_SendsCompletionMessage()
    {
        // Arrange
        var message = CreateTestMessage("Final answer");
        var conversationState = CreateTestConversationState(surveyId: 1, responseId: 100, questionIndex: 2);

        var lastQuestion = CreateTestQuestionDto(3, "Last question?", QuestionType.Text);
        var survey = CreateTestSurveyDto(1, "Test Survey", new[] { lastQuestion });

        // Setup state manager
        _mockStateManager
            .Setup(m => m.GetStateAsync(It.IsAny<long>()))
            .ReturnsAsync(conversationState);

        // Setup survey fetch
        SetupSurveyFetch(survey);

        // Setup question handler
        _mockTextQuestionHandler
            .Setup(h => h.ProcessAnswerAsync(
                It.IsAny<Message>(),
                null,
                It.IsAny<QuestionDto>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"text\":\"Final answer\"}");

        // Setup HTTP client for answer submission
        SetupHttpResponse(HttpMethod.Post, "/api/responses/100/answers", HttpStatusCode.OK);

        // Setup navigation helper to return completion
        _mockNavigationHelper
            .Setup(h => h.GetNextQuestionAsync(
                100,
                3,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(QuestionNavigationResult.SurveyComplete());

        // Setup HTTP client for completion
        SetupHttpResponse(HttpMethod.Post, "/api/responses/100/complete", HttpStatusCode.OK);

        // Setup bot client to capture completion message
        Message? sentMessage = null;
        _mockBotClient
            .Setup(c => c.SendMessage(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<ParseMode?>(),
                It.IsAny<IEnumerable<MessageEntity>?>(),
                It.IsAny<LinkPreviewOptions?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReplyMarkup?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<ChatId, string, int?, ParseMode?, IEnumerable<MessageEntity>?, LinkPreviewOptions?, bool?, bool?, int?, bool?, IReplyMarkup?, string?, CancellationToken>(
                (chatId, text, messageThreadId, parseMode, entities, linkPreviewOptions, disableNotification, protectContent, replyParameters, allowSendingWithoutReply, replyMarkup, businessConnectionId, cancellationToken) =>
                {
                    sentMessage = new Message { Text = text };
                })
            .ReturnsAsync(new Message());

        // Act
        var result = await _handler.HandleMessageResponseAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify completion message was sent
        sentMessage.Should().NotBeNull();
        sentMessage!.Text.Should().Contain("Survey Completed");

        // Verify state manager was called to complete survey
        _mockStateManager.Verify(
            m => m.CompleteSurveyAsync(message.From.Id),
            Times.Once);

        // Verify visited question was recorded before completion
        conversationState.HasVisitedQuestion(3).Should().BeTrue();
    }

    #endregion

    #region Test 3: HandleMessageResponse_RevisitQuestion_SendsWarning

    [Fact]
    public async Task HandleMessageResponse_RevisitQuestion_SendsWarning()
    {
        // Arrange
        var message = CreateTestMessage("Trying to answer again");
        var conversationState = CreateTestConversationState(surveyId: 1, responseId: 100, questionIndex: 0);

        // Mark question 1 as already visited (simulating cycle)
        conversationState.RecordVisitedQuestion(1);

        var currentQuestion = CreateTestQuestionDto(1, "Already answered question", QuestionType.Text);
        var survey = CreateTestSurveyDto(1, "Test Survey", new[] { currentQuestion });

        // Setup state manager
        _mockStateManager
            .Setup(m => m.GetStateAsync(It.IsAny<long>()))
            .ReturnsAsync(conversationState);

        // Setup survey fetch
        SetupSurveyFetch(survey);

        // Setup bot client to capture warning message
        Message? sentMessage = null;
        _mockBotClient
            .Setup(c => c.SendMessage(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<ParseMode?>(),
                It.IsAny<IEnumerable<MessageEntity>?>(),
                It.IsAny<LinkPreviewOptions?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReplyMarkup?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<ChatId, string, int?, ParseMode?, IEnumerable<MessageEntity>?, LinkPreviewOptions?, bool?, bool?, int?, bool?, IReplyMarkup?, string?, CancellationToken>(
                (chatId, text, messageThreadId, parseMode, entities, linkPreviewOptions, disableNotification, protectContent, replyParameters, allowSendingWithoutReply, replyMarkup, businessConnectionId, cancellationToken) =>
                {
                    sentMessage = new Message { Text = text };
                })
            .ReturnsAsync(new Message());

        // Act
        var result = await _handler.HandleMessageResponseAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify warning message was sent
        sentMessage.Should().NotBeNull();
        sentMessage!.Text.Should().Contain("already answered");
        sentMessage.Text.Should().Contain("⚠️");

        // Verify answer was NOT processed
        _mockTextQuestionHandler.Verify(
            h => h.ProcessAnswerAsync(
                It.IsAny<Message>(),
                It.IsAny<CallbackQuery?>(),
                It.IsAny<QuestionDto>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify state was NOT updated
        _mockStateManager.Verify(
            m => m.AnswerQuestionAsync(
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Test 4: HandleMessageResponse_BranchingQuestion_CorrectPathTaken

    [Fact]
    public async Task HandleMessageResponse_BranchingQuestion_OptionALeadsToQuestion2()
    {
        // Arrange - User answers "Option A" which should lead to Question 2
        var message = CreateTestMessage("Option A");
        var conversationState = CreateTestConversationState(surveyId: 1, responseId: 100, questionIndex: 0);

        var branchingQuestion = CreateTestQuestionDto(1, "Choose option?", QuestionType.SingleChoice);
        var question2 = CreateTestQuestionDto(2, "You chose A - Question 2", QuestionType.Text);
        var survey = CreateTestSurveyDto(1, "Test Survey", new[] { branchingQuestion, question2 });

        // Setup state manager
        _mockStateManager
            .Setup(m => m.GetStateAsync(It.IsAny<long>()))
            .ReturnsAsync(conversationState);

        // Setup survey fetch
        SetupSurveyFetch(survey);

        // Setup question handler
        _mockSingleChoiceHandler
            .Setup(h => h.ProcessAnswerAsync(
                It.IsAny<Message>(),
                null,
                It.IsAny<QuestionDto>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"selectedOption\":\"Option A\"}");

        // Setup HTTP client for answer submission
        SetupHttpResponse(HttpMethod.Post, "/api/responses/100/answers", HttpStatusCode.OK);

        // Setup navigation helper to return Question 2 for Option A
        _mockNavigationHelper
            .Setup(h => h.GetNextQuestionAsync(
                100,
                1,
                It.Is<string>(s => s.Contains("Option A")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(QuestionNavigationResult.WithNextQuestion(question2));

        // Setup question display
        _mockTextQuestionHandler
            .Setup(h => h.DisplayQuestionAsync(
                It.IsAny<long>(),
                It.IsAny<QuestionDto>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.HandleMessageResponseAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify Question 2 was displayed (correct branch)
        _mockTextQuestionHandler.Verify(
            h => h.DisplayQuestionAsync(
                message.Chat.Id,
                It.Is<QuestionDto>(q => q.Id == 2 && q.QuestionText.Contains("chose A")),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageResponse_BranchingQuestion_OptionBLeadsToQuestion3()
    {
        // Arrange - User answers "Option B" which should lead to Question 3
        var message = CreateTestMessage("Option B");
        var conversationState = CreateTestConversationState(surveyId: 1, responseId: 100, questionIndex: 0);

        var branchingQuestion = CreateTestQuestionDto(1, "Choose option?", QuestionType.SingleChoice);
        var question3 = CreateTestQuestionDto(3, "You chose B - Question 3", QuestionType.Text);
        var survey = CreateTestSurveyDto(1, "Test Survey", new[] { branchingQuestion, question3 });

        // Setup state manager
        _mockStateManager
            .Setup(m => m.GetStateAsync(It.IsAny<long>()))
            .ReturnsAsync(conversationState);

        // Setup survey fetch
        SetupSurveyFetch(survey);

        // Setup question handler
        _mockSingleChoiceHandler
            .Setup(h => h.ProcessAnswerAsync(
                It.IsAny<Message>(),
                null,
                It.IsAny<QuestionDto>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"selectedOption\":\"Option B\"}");

        // Setup HTTP client for answer submission
        SetupHttpResponse(HttpMethod.Post, "/api/responses/100/answers", HttpStatusCode.OK);

        // Setup navigation helper to return Question 3 for Option B
        _mockNavigationHelper
            .Setup(h => h.GetNextQuestionAsync(
                100,
                1,
                It.Is<string>(s => s.Contains("Option B")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(QuestionNavigationResult.WithNextQuestion(question3));

        // Setup question display
        _mockTextQuestionHandler
            .Setup(h => h.DisplayQuestionAsync(
                It.IsAny<long>(),
                It.IsAny<QuestionDto>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.HandleMessageResponseAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify Question 3 was displayed (correct branch)
        _mockTextQuestionHandler.Verify(
            h => h.DisplayQuestionAsync(
                message.Chat.Id,
                It.Is<QuestionDto>(q => q.Id == 3 && q.QuestionText.Contains("chose B")),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Test 5: HandleMessageResponse_NoActiveResponse_ReturnsFalse

    [Fact]
    public async Task HandleMessageResponse_NoActiveResponse_ReturnsFalse()
    {
        // Arrange
        var message = CreateTestMessage("Some answer");

        // State with no active response
        var conversationState = new ConversationState
        {
            UserId = 123456,
            CurrentResponseId = null,
            CurrentQuestionIndex = null,
            CurrentSurveyId = null
        };

        // Setup state manager
        _mockStateManager
            .Setup(m => m.GetStateAsync(It.IsAny<long>()))
            .ReturnsAsync(conversationState);

        // Act
        var result = await _handler.HandleMessageResponseAsync(message, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        // Verify no message was sent
        _mockBotClient.Verify(
            c => c.SendMessage(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<ParseMode?>(),
                It.IsAny<IEnumerable<MessageEntity>?>(),
                It.IsAny<LinkPreviewOptions?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReplyMarkup?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify no processing occurred
        _mockTextQuestionHandler.Verify(
            h => h.ProcessAnswerAsync(
                It.IsAny<Message>(),
                It.IsAny<CallbackQuery?>(),
                It.IsAny<QuestionDto>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private Message CreateTestMessage(string text)
    {
        return new Message
        {
            MessageId = 1,
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456 },
            From = new Telegram.Bot.Types.User
            {
                Id = 123456,
                Username = "testuser",
                FirstName = "Test"
            },
            Text = text
        };
    }

    private ConversationState CreateTestConversationState(int surveyId, int responseId, int questionIndex)
    {
        return new ConversationState
        {
            UserId = 123456,
            CurrentSurveyId = surveyId,
            CurrentResponseId = responseId,
            CurrentQuestionIndex = questionIndex,
            TotalQuestions = 5,
            CurrentState = ConversationStateType.AnsweringQuestion
        };
    }

    private QuestionDto CreateTestQuestionDto(int id, string text, QuestionType type)
    {
        return new QuestionDto
        {
            Id = id,
            QuestionText = text,
            QuestionType = type,
            IsRequired = true,
            OrderIndex = id - 1,
            Options = type == QuestionType.SingleChoice || type == QuestionType.MultipleChoice
                ? new List<QuestionOptionDto>
                {
                    new QuestionOptionDto { Id = 1, Text = "Option A", OrderIndex = 0 },
                    new QuestionOptionDto { Id = 2, Text = "Option B", OrderIndex = 1 }
                }
                : new List<QuestionOptionDto>()
        };
    }

    private SurveyDto CreateTestSurveyDto(int id, string title, QuestionDto[] questions)
    {
        return new SurveyDto
        {
            Id = id,
            Title = title,
            Description = "Test survey",
            Code = "TEST01",
            IsActive = true,
            Questions = questions.ToList()
        };
    }

    private void SetupSurveyFetch(SurveyDto survey)
    {
        // Setup performance monitor to pass through
        _mockPerformanceMonitor
            .Setup(m => m.TrackOperationAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SurveyDto?>>>(),
                It.IsAny<string>()))
            .Returns<string, Func<Task<SurveyDto?>>, string>(
                async (operation, func, context) => await func());

        // Setup cache to pass through to factory
        _mockSurveyCache
            .Setup(c => c.GetOrAddSurveyAsync(
                It.IsAny<int>(),
                It.IsAny<Func<Task<SurveyDto?>>>(),
                It.IsAny<TimeSpan>()))
            .Returns<int, Func<Task<SurveyDto?>>, TimeSpan>(
                async (id, factory, ttl) => await factory());

        // Setup repository
        var surveyEntity = new Survey
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Code = survey.Code,
            IsActive = survey.IsActive
        };

        _mockSurveyRepository
            .Setup(r => r.GetByIdWithQuestionsAsync(survey.Id))
            .ReturnsAsync(surveyEntity);

        // Setup mapper
        _mockMapper
            .Setup(m => m.Map<SurveyDto>(It.IsAny<Survey>()))
            .Returns(survey);
    }

    private void SetupHttpResponse(HttpMethod method, string path, HttpStatusCode statusCode, object? content = null)
    {
        var responseMessage = new HttpResponseMessage(statusCode);

        if (content != null)
        {
            responseMessage.Content = JsonContent.Create(content);
        }

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.AbsolutePath.Contains(path)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }

    #endregion
}
