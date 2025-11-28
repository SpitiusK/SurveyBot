using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Xunit;

namespace SurveyBot.Tests.Integration.Bot;

/// <summary>
/// Integration tests for survey lookup by code functionality.
/// Tests survey code validation and retrieval.
/// </summary>
public class SurveyCodeTests : IClassFixture<BotTestFixture>
{
    private readonly BotTestFixture _fixture;
    private readonly SurveyCommandHandler _surveyCommandHandler;

    private const long TestUserId = 111222333;
    private const long TestChatId = 111222333;

    public SurveyCodeTests(BotTestFixture fixture)
    {
        _fixture = fixture;

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
            new List<IQuestionHandler>(),
            Mock.Of<ILogger<SurveyCommandHandler>>());
    }

    [Fact]
    public async Task StartSurvey_ValidNumericId_StartsSurvey()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var message = _fixture.CreateTestMessage(TestUserId, TestChatId, $"/survey {surveyId}");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId);
        state.Should().NotBeNull();
        state!.CurrentSurveyId.Should().Be(surveyId);

        // Verify intro message was sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId &&
                    req.Text.Contains(_fixture.TestSurvey.Title)),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSurvey_InvalidCode_SendsErrorMessage()
    {
        // Arrange
        var message = _fixture.CreateTestMessage(TestUserId + 1, TestChatId + 1, "/survey INVALID123");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 1);
        state.Should().BeNull(); // State not created

        // Verify error/usage message was sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 1 &&
                    (req.Text.Contains("Usage") || req.Text.Contains("not found"))),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSurvey_MissingIdentifier_SendsUsageMessage()
    {
        // Arrange
        var message = _fixture.CreateTestMessage(TestUserId + 2, TestChatId + 2, "/survey");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 2);
        state.Should().BeNull();

        // Verify usage message was sent
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId + 2 &&
                    req.Text.Contains("Usage") && req.Text.Contains("/survey")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartSurvey_ActiveVsInactive_OnlyStartsActiveOnes()
    {
        // Arrange - Create both active and inactive surveys
        var activeSurvey = EntityBuilder.CreateSurvey("Active Survey", null, _fixture.TestUser.Id, true);
        activeSurvey.SetAllowMultipleResponses(true);

        var inactiveSurvey = EntityBuilder.CreateSurvey("Inactive Survey", null, _fixture.TestUser.Id, false);
        inactiveSurvey.SetAllowMultipleResponses(true);

        await _fixture.DbContext.Surveys.AddRangeAsync(activeSurvey, inactiveSurvey);
        await _fixture.DbContext.SaveChangesAsync();

        // Add at least one question to active survey
        var question = EntityBuilder.CreateQuestion(activeSurvey.Id, "Test?", QuestionType.Text, 0, true);
        await _fixture.DbContext.Questions.AddAsync(question);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Try active survey
        var activeMessage = _fixture.CreateTestMessage(TestUserId + 3, TestChatId + 3, $"/survey {activeSurvey.Id}");
        await _surveyCommandHandler.HandleAsync(activeMessage, CancellationToken.None);

        var activeState = await _fixture.StateManager.GetStateAsync(TestUserId + 3);

        // Act - Try inactive survey
        var inactiveMessage = _fixture.CreateTestMessage(TestUserId + 4, TestChatId + 4, $"/survey {inactiveSurvey.Id}");
        await _surveyCommandHandler.HandleAsync(inactiveMessage, CancellationToken.None);

        var inactiveState = await _fixture.StateManager.GetStateAsync(TestUserId + 4);

        // Assert
        activeState.Should().NotBeNull("Active survey should start successfully");
        activeState!.CurrentSurveyId.Should().Be(activeSurvey.Id);

        inactiveState.Should().BeNull("Inactive survey should not start");
    }
}
