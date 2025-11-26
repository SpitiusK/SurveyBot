using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SurveyBot.Bot.Utilities;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Xunit;

namespace SurveyBot.Tests.Unit.Bot;

/// <summary>
/// Unit tests for SurveyNavigationHelper.
/// Tests the helper's ability to:
/// - Get next question from API
/// - Handle survey completion
/// - Handle errors gracefully
/// - Properly deserialize API responses
/// </summary>
public class SurveyNavigationHelperTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<SurveyNavigationHelper>> _mockLogger;
    private readonly SurveyNavigationHelper _helper;

    public SurveyNavigationHelperTests()
    {
        // Setup HTTP message handler
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Create HttpClient with mocked handler
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        // Setup HttpClientFactory
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory
            .Setup(f => f.CreateClient("SurveyBotApi"))
            .Returns(_httpClient);

        // Setup logger
        _mockLogger = new Mock<ILogger<SurveyNavigationHelper>>();

        // Create helper instance
        _helper = new SurveyNavigationHelper(
            _mockHttpClientFactory.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Test 6: GetNextQuestionAsync_ValidResponse_ReturnsNextQuestion

    [Fact]
    public async Task GetNextQuestionAsync_ValidResponse_ReturnsNextQuestion()
    {
        // Arrange
        var responseId = 100;
        var currentQuestionId = 1;
        var answerText = "{\"text\":\"My answer\"}";

        var nextQuestion = new QuestionDto
        {
            Id = 2,
            QuestionText = "What is your next answer?",
            QuestionType = QuestionType.Text,
            IsRequired = true,
            OrderIndex = 1,
            Options = new List<QuestionOptionDto>()
        };

        var apiResponse = new
        {
            IsComplete = false,
            NextQuestion = nextQuestion
        };

        SetupHttpResponse(
            $"/api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}",
            HttpStatusCode.OK,
            apiResponse);

        // Act
        var result = await _helper.GetNextQuestionAsync(
            responseId,
            currentQuestionId,
            answerText,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeFalse();
        result.IsError.Should().BeFalse();
        result.NextQuestion.Should().NotBeNull();
        result.NextQuestion!.Id.Should().Be(2);
        result.NextQuestion.QuestionText.Should().Be("What is your next answer?");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetNextQuestionAsync_ValidResponse_WithSingleChoiceQuestion_ReturnsWithOptions()
    {
        // Arrange
        var responseId = 100;
        var currentQuestionId = 1;
        var answerText = "{\"text\":\"Answer\"}";

        var nextQuestion = new QuestionDto
        {
            Id = 2,
            QuestionText = "Choose an option:",
            QuestionType = QuestionType.SingleChoice,
            IsRequired = true,
            OrderIndex = 1,
            Options = new List<QuestionOptionDto>
            {
                new QuestionOptionDto { Id = 1, Text = "Option A", OrderIndex = 0 },
                new QuestionOptionDto { Id = 2, Text = "Option B", OrderIndex = 1 },
                new QuestionOptionDto { Id = 3, Text = "Option C", OrderIndex = 2 }
            }
        };

        var apiResponse = new
        {
            IsComplete = false,
            NextQuestion = nextQuestion
        };

        SetupHttpResponse(
            $"/api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}",
            HttpStatusCode.OK,
            apiResponse);

        // Act
        var result = await _helper.GetNextQuestionAsync(
            responseId,
            currentQuestionId,
            answerText,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeFalse();
        result.NextQuestion.Should().NotBeNull();
        result.NextQuestion!.QuestionType.Should().Be(QuestionType.SingleChoice);
        result.NextQuestion.Options.Should().HaveCount(3);
        result.NextQuestion.Options[0].Text.Should().Be("Option A");
    }

    #endregion

    #region Test 7: GetNextQuestionAsync_SurveyComplete_ReturnsSurveyComplete

    [Fact]
    public async Task GetNextQuestionAsync_SurveyComplete_ReturnsSurveyComplete()
    {
        // Arrange
        var responseId = 100;
        var currentQuestionId = 5; // Last question
        var answerText = "{\"text\":\"Final answer\"}";

        var apiResponse = new
        {
            IsComplete = true,
            NextQuestion = (QuestionDto?)null
        };

        SetupHttpResponse(
            $"/api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}",
            HttpStatusCode.OK,
            apiResponse);

        // Act
        var result = await _helper.GetNextQuestionAsync(
            responseId,
            currentQuestionId,
            answerText,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();
        result.IsError.Should().BeFalse();
        result.NextQuestion.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region Additional Tests: Error Handling

    [Fact]
    public async Task GetNextQuestionAsync_NotFound_ReturnsNotFoundError()
    {
        // Arrange
        var responseId = 999;
        var currentQuestionId = 1;
        var answerText = "{\"text\":\"Answer\"}";

        SetupHttpResponse(
            $"/api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}",
            HttpStatusCode.NotFound,
            null);

        // Act
        var result = await _helper.GetNextQuestionAsync(
            responseId,
            currentQuestionId,
            answerText,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.IsComplete.Should().BeFalse();
        result.NextQuestion.Should().BeNull();
        result.ErrorMessage.Should().Contain(currentQuestionId.ToString());
    }

    [Fact]
    public async Task GetNextQuestionAsync_ServerError_ReturnsError()
    {
        // Arrange
        var responseId = 100;
        var currentQuestionId = 1;
        var answerText = "{\"text\":\"Answer\"}";

        SetupHttpResponse(
            $"/api/responses/{responseId}/next-question?currentQuestionId={currentQuestionId}",
            HttpStatusCode.InternalServerError,
            null);

        // Act
        var result = await _helper.GetNextQuestionAsync(
            responseId,
            currentQuestionId,
            answerText,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.IsComplete.Should().BeFalse();
        result.NextQuestion.Should().BeNull();
        result.ErrorMessage.Should().Contain("Unable to determine next question");
    }

    [Fact]
    public async Task GetNextQuestionAsync_NetworkError_ReturnsError()
    {
        // Arrange
        var responseId = 100;
        var currentQuestionId = 1;
        var answerText = "{\"text\":\"Answer\"}";

        // Setup handler to throw network exception
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _helper.GetNextQuestionAsync(
            responseId,
            currentQuestionId,
            answerText,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Network error");
    }

    #endregion

    #region GetFirstQuestionAsync Tests

    [Fact]
    public async Task GetFirstQuestionAsync_ValidSurvey_ReturnsFirstQuestion()
    {
        // Arrange
        var surveyId = 1;

        var questions = new List<QuestionDto>
        {
            new QuestionDto
            {
                Id = 3,
                QuestionText = "Third question",
                QuestionType = QuestionType.Text,
                OrderIndex = 2
            },
            new QuestionDto
            {
                Id = 1,
                QuestionText = "First question",
                QuestionType = QuestionType.Text,
                OrderIndex = 0
            },
            new QuestionDto
            {
                Id = 2,
                QuestionText = "Second question",
                QuestionType = QuestionType.Text,
                OrderIndex = 1
            }
        };

        SetupHttpResponse(
            $"/api/surveys/{surveyId}/questions",
            HttpStatusCode.OK,
            questions);

        // Act
        var result = await _helper.GetFirstQuestionAsync(surveyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.QuestionText.Should().Be("First question");
        result.OrderIndex.Should().Be(0);
    }

    [Fact]
    public async Task GetFirstQuestionAsync_EmptySurvey_ReturnsNull()
    {
        // Arrange
        var surveyId = 1;
        var questions = new List<QuestionDto>();

        SetupHttpResponse(
            $"/api/surveys/{surveyId}/questions",
            HttpStatusCode.OK,
            questions);

        // Act
        var result = await _helper.GetFirstQuestionAsync(surveyId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFirstQuestionAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var surveyId = 999;

        SetupHttpResponse(
            $"/api/surveys/{surveyId}/questions",
            HttpStatusCode.NotFound,
            null);

        // Act
        var result = await _helper.GetFirstQuestionAsync(surveyId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(string path, HttpStatusCode statusCode, object? content)
    {
        var responseMessage = new HttpResponseMessage(statusCode);

        if (content != null)
        {
            var json = JsonSerializer.Serialize(content);
            responseMessage.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.PathAndQuery.Contains(path)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }

    #endregion
}
