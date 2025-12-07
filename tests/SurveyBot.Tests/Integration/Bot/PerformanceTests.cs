using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Utilities;
using SurveyBot.Bot.Validators;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Types;
using Xunit;

namespace SurveyBot.Tests.Integration.Bot;

/// <summary>
/// Performance tests for bot operations.
/// Ensures response times meet requirements (< 2s for operations, < 500ms for question display).
/// </summary>
public class PerformanceTests : IClassFixture<BotTestFixture>
{
    private readonly BotTestFixture _fixture;
    private readonly SurveyCommandHandler _surveyCommandHandler;
    private readonly List<IQuestionHandler> _questionHandlers;
    private readonly NavigationHandler _navigationHandler;
    private readonly BotPerformanceMonitor _performanceMonitor;

    private const long TestUserId = 777888999;
    private const long TestChatId = 777888999;
    private const int MaxResponseTimeMs = 500; // Question display target
    private const int MaxAnswerSubmissionMs = 1000; // Answer submission target
    private const int MaxOverallOperationMs = 2000; // Overall operation requirement

    public PerformanceTests(BotTestFixture fixture)
    {
        _fixture = fixture;

        // Create performance monitor
        _performanceMonitor = new BotPerformanceMonitor(Mock.Of<ILogger<BotPerformanceMonitor>>());

        // Create question handlers
        var validator = new AnswerValidator(Mock.Of<ILogger<AnswerValidator>>());
        var errorHandler = new QuestionErrorHandler(_fixture.MockBotService.Object, Mock.Of<ILogger<QuestionErrorHandler>>());
        var mockMediaService = new Mock<ITelegramMediaService>();
        var mediaConfig = Options.Create(new BotConfiguration { ApiBaseUrl = "http://localhost:5000" });
        var mediaHelper = new QuestionMediaHelper(mockMediaService.Object, mediaConfig, Mock.Of<ILogger<QuestionMediaHelper>>());

        _questionHandlers = new List<IQuestionHandler>
        {
            new TextQuestionHandler(_fixture.MockBotService.Object, validator, errorHandler, mediaHelper, Mock.Of<ILogger<TextQuestionHandler>>()),
            new SingleChoiceQuestionHandler(_fixture.MockBotService.Object, validator, errorHandler, mediaHelper, Mock.Of<ILogger<SingleChoiceQuestionHandler>>()),
            new MultipleChoiceQuestionHandler(_fixture.MockBotService.Object, _fixture.StateManager, validator, errorHandler, mediaHelper, Mock.Of<ILogger<MultipleChoiceQuestionHandler>>()),
            new RatingQuestionHandler(_fixture.MockBotService.Object, validator, errorHandler, mediaHelper, Mock.Of<ILogger<RatingQuestionHandler>>())
        };

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
                // Simulate fast API responses
                if (request.RequestUri!.PathAndQuery.Contains("/api/surveys/"))
                {
                    var survey = new
                    {
                        success = true,
                        data = new
                        {
                            id = _fixture.TestSurvey.Id,
                            title = _fixture.TestSurvey.Title,
                            isActive = true,
                            questions = _fixture.TestQuestions.Select(q => new
                            {
                                id = q.Id,
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

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new { success = true })
                };
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        var botConfig = Options.Create(new BotConfiguration { ApiBaseUrl = "http://localhost:5000" });
        var surveyCache = new SurveyCache(Mock.Of<ILogger<SurveyCache>>());
        var mockSurveyRepo = new Mock<ISurveyRepository>();
        var mockMapper = new Mock<IMapper>();

        _navigationHandler = new NavigationHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            _questionHandlers,
            mockSurveyRepo.Object,
            mockMapper.Object,
            httpClient,
            botConfig,
            _performanceMonitor,
            surveyCache,
            Mock.Of<ILogger<NavigationHandler>>());

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
            new SurveyCache(Mock.Of<ILogger<SurveyCache>>()),
            _questionHandlers,
            Mock.Of<ILogger<SurveyCommandHandler>>());
    }

    [Fact]
    public async Task QuestionDisplay_ResponseTime_UnderHalfSecond()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId, surveyId, 500, 4);

        var stopwatch = Stopwatch.StartNew();

        // Act - Display all question types
        var times = new List<long>();

        foreach (var questionHandler in _questionHandlers)
        {
            stopwatch.Restart();

            var question = _fixture.TestQuestions.First(q => q.QuestionType == questionHandler.QuestionType);
            var questionDto = MapToDto(question);

            await questionHandler.DisplayQuestionAsync(TestChatId, questionDto, 0, 4, CancellationToken.None);

            stopwatch.Stop();
            times.Add(stopwatch.ElapsedMilliseconds);

            // Output for debugging
            Console.WriteLine($"{questionHandler.QuestionType} display time: {stopwatch.ElapsedMilliseconds}ms");
        }

        // Assert
        var maxTime = times.Max();
        var avgTime = times.Average();

        Console.WriteLine($"Max display time: {maxTime}ms");
        Console.WriteLine($"Avg display time: {avgTime}ms");

        maxTime.Should().BeLessThan(MaxResponseTimeMs,
            $"Question display should be under {MaxResponseTimeMs}ms (was {maxTime}ms)");

        avgTime.Should().BeLessThan(MaxResponseTimeMs / 2,
            $"Average display time should be well under limit (was {avgTime}ms)");
    }

    [Fact]
    public async Task AnswerSubmission_ResponseTime_UnderOneSecond()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 1, surveyId, 501, 4);

        var stopwatch = Stopwatch.StartNew();
        var times = new List<long>();

        // Act - Test answer submission for different question types

        // 1. Text answer
        stopwatch.Restart();
        var textMessage = _fixture.CreateTestMessage(TestUserId + 1, TestChatId + 1, "My answer text");
        var textQuestion = MapToDto(_fixture.TestQuestions[0]);
        var textResult = await _questionHandlers[0].ProcessAnswerAsync(
            textMessage, null, textQuestion, TestUserId + 1, CancellationToken.None);
        stopwatch.Stop();
        times.Add(stopwatch.ElapsedMilliseconds);
        Console.WriteLine($"Text answer processing: {stopwatch.ElapsedMilliseconds}ms");

        // 2. Single choice answer
        stopwatch.Restart();
        var singleChoiceQuestion = MapToDto(_fixture.TestQuestions[1]);
        // Callback data format: answer_q{questionId}_opt{optionIndex} (index 0 = "Red")
        var singleChoiceCallback = _fixture.CreateTestCallbackQuery(TestUserId + 1, TestChatId + 1, $"answer_q{singleChoiceQuestion.Id}_opt0");
        var singleChoiceResult = await _questionHandlers[1].ProcessAnswerAsync(
            null, singleChoiceCallback, singleChoiceQuestion, TestUserId + 1, CancellationToken.None);
        stopwatch.Stop();
        times.Add(stopwatch.ElapsedMilliseconds);
        Console.WriteLine($"Single choice answer processing: {stopwatch.ElapsedMilliseconds}ms");

        // 3. Rating answer
        stopwatch.Restart();
        var ratingQuestion = MapToDto(_fixture.TestQuestions[3]);
        // Callback data format: rating_q{questionId}_r{ratingValue}
        var ratingCallback = _fixture.CreateTestCallbackQuery(TestUserId + 1, TestChatId + 1, $"rating_q{ratingQuestion.Id}_r5");
        var ratingResult = await _questionHandlers[3].ProcessAnswerAsync(
            null, ratingCallback, ratingQuestion, TestUserId + 1, CancellationToken.None);
        stopwatch.Stop();
        times.Add(stopwatch.ElapsedMilliseconds);
        Console.WriteLine($"Rating answer processing: {stopwatch.ElapsedMilliseconds}ms");

        // Assert
        var maxTime = times.Max();
        var avgTime = times.Average();

        Console.WriteLine($"Max answer submission time: {maxTime}ms");
        Console.WriteLine($"Avg answer submission time: {avgTime}ms");

        maxTime.Should().BeLessThan(MaxAnswerSubmissionMs,
            $"Answer submission should be under {MaxAnswerSubmissionMs}ms (was {maxTime}ms)");

        avgTime.Should().BeLessThan(MaxAnswerSubmissionMs / 2,
            $"Average answer submission time should be well under limit (was {avgTime}ms)");

        // All results should be successful
        textResult.Should().NotBeNull();
        singleChoiceResult.Should().NotBeNull();
        ratingResult.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteOperations_EndToEnd_UnderTwoSeconds()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var stopwatch = Stopwatch.StartNew();

        // Act - Complete survey start operation
        var startMessage = _fixture.CreateTestMessage(TestUserId + 2, TestChatId + 2, $"/survey {surveyId}");
        await _surveyCommandHandler.HandleAsync(startMessage, CancellationToken.None);
        stopwatch.Stop();

        var surveyStartTime = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Survey start (full operation): {surveyStartTime}ms");

        // Act - Navigation back operation
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 2);
        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 2, TestChatId + 2, "nav_back_q2");

        stopwatch.Restart();
        await _navigationHandler.HandleBackAsync(callback, 2, CancellationToken.None);
        stopwatch.Stop();

        var navigationTime = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Navigation back (full operation): {navigationTime}ms");

        // Assert
        surveyStartTime.Should().BeLessThan(MaxOverallOperationMs,
            $"Survey start should be under {MaxOverallOperationMs}ms (was {surveyStartTime}ms)");

        navigationTime.Should().BeLessThan(MaxOverallOperationMs,
            $"Navigation should be under {MaxOverallOperationMs}ms (was {navigationTime}ms)");
    }

    [Fact]
    public async Task ConcurrentOperations_MultipleUsers_MaintainPerformance()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var userCount = 10;
        var tasks = new List<Task<long>>();

        // Act - Simulate multiple users starting surveys simultaneously
        for (int i = 0; i < userCount; i++)
        {
            var userId = TestUserId + 1000 + i;
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();

                await _fixture.StateManager.StartSurveyAsync(userId, surveyId, 600 + i, 4);

                var message = _fixture.CreateTestMessage(userId, TestChatId + 1000 + i, "Test answer");
                var question = MapToDto(_fixture.TestQuestions[0]);

                await _questionHandlers[0].ProcessAnswerAsync(
                    message, null, question, userId, CancellationToken.None);

                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }));
        }

        var times = await Task.WhenAll(tasks);

        // Assert
        var maxTime = times.Max();
        var avgTime = times.Average();

        Console.WriteLine($"Concurrent operations - Max time: {maxTime}ms");
        Console.WriteLine($"Concurrent operations - Avg time: {avgTime}ms");

        // With concurrent operations, we allow slightly more time but still reasonable
        maxTime.Should().BeLessThan(MaxOverallOperationMs,
            $"Even under concurrent load, operations should be under {MaxOverallOperationMs}ms (was {maxTime}ms)");

        avgTime.Should().BeLessThan(MaxAnswerSubmissionMs,
            $"Average time under concurrent load should be reasonable (was {avgTime}ms)");
    }

    [Fact]
    public async Task StateManager_OperationPerformance_FastAccess()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var stopwatch = Stopwatch.StartNew();

        // Act - Test state manager operations
        var times = new Dictionary<string, long>();

        // Start survey
        stopwatch.Restart();
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 3, surveyId, 700, 4);
        stopwatch.Stop();
        times["StartSurvey"] = stopwatch.ElapsedMilliseconds;

        // Get state
        stopwatch.Restart();
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 3);
        stopwatch.Stop();
        times["GetState"] = stopwatch.ElapsedMilliseconds;

        // Answer question
        stopwatch.Restart();
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId + 3, 0, "{\"text\":\"Test\"}");
        stopwatch.Stop();
        times["AnswerQuestion"] = stopwatch.ElapsedMilliseconds;

        // Next question
        stopwatch.Restart();
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 3);
        stopwatch.Stop();
        times["NextQuestion"] = stopwatch.ElapsedMilliseconds;

        // Previous question
        stopwatch.Restart();
        await _fixture.StateManager.PreviousQuestionAsync(TestUserId + 3);
        stopwatch.Stop();
        times["PreviousQuestion"] = stopwatch.ElapsedMilliseconds;

        // Get progress
        stopwatch.Restart();
        var progress = await _fixture.StateManager.GetProgressPercentAsync(TestUserId + 3);
        stopwatch.Stop();
        times["GetProgress"] = stopwatch.ElapsedMilliseconds;

        // Assert - All state operations should be very fast (< 50ms)
        foreach (var kvp in times)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}ms");
            kvp.Value.Should().BeLessThan(50, $"{kvp.Key} should be fast (was {kvp.Value}ms)");
        }

        var avgTime = times.Values.Average();
        avgTime.Should().BeLessThan(20, $"Average state operation time should be very fast (was {avgTime}ms)");
    }

    private QuestionDto MapToDto(Question question)
    {
        List<string>? options = null;
        if (!string.IsNullOrWhiteSpace(question.OptionsJson))
        {
            options = JsonSerializer.Deserialize<List<string>>(question.OptionsJson);
        }

        return new QuestionDto
        {
            Id = question.Id,
            SurveyId = question.SurveyId,
            QuestionText = question.QuestionText,
            QuestionType = question.QuestionType,
            OrderIndex = question.OrderIndex,
            IsRequired = question.IsRequired,
            Options = options,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }
}
