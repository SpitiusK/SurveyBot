using System;
using System.Collections.Generic;
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
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Utilities;
using SurveyBot.Bot.Validators;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Interfaces;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Xunit;

namespace SurveyBot.Tests.Integration.Bot;

/// <summary>
/// Integration tests for bot navigation functionality (Back/Skip).
/// Tests question flow navigation and state management.
/// </summary>
public class NavigationTests : IClassFixture<BotTestFixture>
{
    private readonly BotTestFixture _fixture;
    private readonly NavigationHandler _navigationHandler;
    private readonly HttpClient _httpClient;

    private const long TestUserId = 987654321;
    private const long TestChatId = 987654321;

    public NavigationTests(BotTestFixture fixture)
    {
        _fixture = fixture;

        // Create mock HTTP client
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                // Mock survey fetch
                if (request.RequestUri!.PathAndQuery.Contains("/api/surveys/"))
                {
                    var survey = new
                    {
                        success = true,
                        data = new
                        {
                            id = _fixture.TestSurvey.Id,
                            title = _fixture.TestSurvey.Title,
                            description = _fixture.TestSurvey.Description,
                            isActive = _fixture.TestSurvey.IsActive,
                            questions = _fixture.TestQuestions.Select(q => new
                            {
                                id = q.Id,
                                surveyId = q.SurveyId,
                                questionText = q.QuestionText,
                                questionType = (int)q.QuestionType,
                                orderIndex = q.OrderIndex,
                                isRequired = q.IsRequired,
                                options = string.IsNullOrWhiteSpace(q.OptionsJson)
                                    ? null
                                    : JsonSerializer.Deserialize<List<string>>(q.OptionsJson)
                            }).ToList()
                        }
                    };

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(survey)
                    };
                }

                // Mock answer submission
                if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery.Contains("/answers"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(new { success = true })
                    };
                }

                // Mock response completion
                if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery.Contains("/complete"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(new { success = true })
                    };
                }

                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
            });

        _httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        // Create question handlers
        var validator = new AnswerValidator(Mock.Of<ILogger<AnswerValidator>>());
        var errorHandler = new QuestionErrorHandler(_fixture.MockBotService.Object, Mock.Of<ILogger<QuestionErrorHandler>>());
        var mockMediaService = new Mock<ITelegramMediaService>();
        var mediaConfig = Options.Create(new BotConfiguration { ApiBaseUrl = "http://localhost:5000" });
        var mediaHelper = new QuestionMediaHelper(mockMediaService.Object, mediaConfig, Mock.Of<ILogger<QuestionMediaHelper>>());

        var questionHandlers = new List<IQuestionHandler>
        {
            new TextQuestionHandler(_fixture.MockBotService.Object, validator, errorHandler, mediaHelper, Mock.Of<ILogger<TextQuestionHandler>>()),
            new SingleChoiceQuestionHandler(_fixture.MockBotService.Object, validator, errorHandler, mediaHelper, Mock.Of<ILogger<SingleChoiceQuestionHandler>>()),
            new MultipleChoiceQuestionHandler(_fixture.MockBotService.Object, _fixture.StateManager, validator, errorHandler, mediaHelper, Mock.Of<ILogger<MultipleChoiceQuestionHandler>>()),
            new RatingQuestionHandler(_fixture.MockBotService.Object, validator, errorHandler, mediaHelper, Mock.Of<ILogger<RatingQuestionHandler>>())
        };

        var botConfig = Options.Create(new BotConfiguration { ApiBaseUrl = "http://localhost:5000" });
        var performanceMonitor = new BotPerformanceMonitor(Mock.Of<ILogger<BotPerformanceMonitor>>());
        var surveyCache = new SurveyCache(Mock.Of<ILogger<SurveyCache>>());

        // Use real survey repository and real mapper from fixture
        // This ensures NavigationHandler can properly fetch surveys and map them to DTOs
        // Previous bug: Mock<IMapper> returned null for all mappings, causing navigation to fail silently

        _navigationHandler = new NavigationHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            questionHandlers,
            _fixture.SurveyRepository,  // Real repository with test data
            _fixture.Mapper,            // Real AutoMapper with production profiles
            _httpClient,
            botConfig,
            performanceMonitor,
            surveyCache,
            Mock.Of<ILogger<NavigationHandler>>());
    }

    [Fact]
    public async Task GoBack_FromSecondQuestion_DisplaysPreviousQuestion()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var responseId = 200;
        await _fixture.StateManager.StartSurveyAsync(TestUserId, surveyId, responseId, 4);

        // Answer first question and move to second
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 0, "{\"text\":\"Test\"}");
        await _fixture.StateManager.NextQuestionAsync(TestUserId);

        var callback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, "nav_back_q2");

        // Act
        var result = await _navigationHandler.HandleBackAsync(callback, 2, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var state = await _fixture.StateManager.GetStateAsync(TestUserId);
        state.Should().NotBeNull();
        state!.CurrentQuestionIndex.Should().Be(0); // Back to first question

        // Verify bot answered callback
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<AnswerCallbackQueryRequest>(req =>
                    req.CallbackQueryId == callback.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GoBack_FromFirstQuestion_ReturnsError()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var responseId = 201;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 1, surveyId, responseId, 4);

        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 1, TestChatId + 1, "nav_back_q1");

        // Act
        var result = await _navigationHandler.HandleBackAsync(callback, 1, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        // Verify error message shown
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<AnswerCallbackQueryRequest>(req =>
                    req.CallbackQueryId == callback.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // State should not change
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 1);
        state!.CurrentQuestionIndex.Should().Be(0);
    }

    [Fact]
    public async Task SkipQuestion_OptionalQuestion_MovesToNextQuestion()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var responseId = 202;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 2, surveyId, responseId, 4);

        // Move to question 3 (multiple choice - optional)
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 2);
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 2);

        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 2, TestChatId + 2, "nav_skip_q3");

        // Act
        var result = await _navigationHandler.HandleSkipAsync(callback, 3, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 2);
        state.Should().NotBeNull();
        state!.CurrentQuestionIndex.Should().Be(3); // Moved to next question

        // Verify callback answered
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<AnswerCallbackQueryRequest>(req =>
                    req.CallbackQueryId == callback.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SkipQuestion_RequiredQuestion_ReturnsError()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var responseId = 203;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 3, surveyId, responseId, 4);

        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 3, TestChatId + 3, "nav_skip_q1");

        // Act
        var result = await _navigationHandler.HandleSkipAsync(callback, 1, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        // Verify error message shown
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<AnswerCallbackQueryRequest>(req =>
                    req.CallbackQueryId == callback.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // State should not change
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 3);
        state!.CurrentQuestionIndex.Should().Be(0);
    }

    [Fact]
    public async Task Navigation_MultipleBackAndForth_MaintainsCorrectState()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var responseId = 204;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 4, surveyId, responseId, 4);

        // Act - Navigate forward then back multiple times
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId + 4, 0, "{\"text\":\"Answer 1\"}");
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 4); // Question 1

        await _fixture.StateManager.AnswerQuestionAsync(TestUserId + 4, 1, "{\"selectedOption\":\"Blue\"}");
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 4); // Question 2

        var state1 = await _fixture.StateManager.GetStateAsync(TestUserId + 4);
        state1!.CurrentQuestionIndex.Should().Be(2);

        // Go back
        await _fixture.StateManager.PreviousQuestionAsync(TestUserId + 4); // Question 1
        var state2 = await _fixture.StateManager.GetStateAsync(TestUserId + 4);
        state2!.CurrentQuestionIndex.Should().Be(1);

        // Go back again
        await _fixture.StateManager.PreviousQuestionAsync(TestUserId + 4); // Question 0
        var state3 = await _fixture.StateManager.GetStateAsync(TestUserId + 4);
        state3!.CurrentQuestionIndex.Should().Be(0);

        // Go forward
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 4); // Question 1
        var state4 = await _fixture.StateManager.GetStateAsync(TestUserId + 4);
        state4!.CurrentQuestionIndex.Should().Be(1);

        // Assert - Verify cached answers are preserved
        var cachedAnswer0 = await _fixture.StateManager.GetCachedAnswerAsync(TestUserId + 4, 0);
        cachedAnswer0.Should().Contain("Answer 1");

        var cachedAnswer1 = await _fixture.StateManager.GetCachedAnswerAsync(TestUserId + 4, 1);
        cachedAnswer1.Should().Contain("Blue");
    }
}
