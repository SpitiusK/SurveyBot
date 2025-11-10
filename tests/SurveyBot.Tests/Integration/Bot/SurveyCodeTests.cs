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
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
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

        var completionHandler = new CompletionHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            _fixture.ResponseRepository,
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
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains(_fixture.TestSurvey.Title)),
                It.IsAny<int?>(),
                It.IsAny<Telegram.Bot.Types.Enums.ParseMode?>(),
                It.IsAny<System.Collections.Generic.IEnumerable<Telegram.Bot.Types.MessageEntity>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
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
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("Usage") || s.Contains("not found")),
                It.IsAny<int?>(),
                It.IsAny<Telegram.Bot.Types.Enums.ParseMode?>(),
                It.IsAny<System.Collections.Generic.IEnumerable<Telegram.Bot.Types.MessageEntity>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
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
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("Usage") && s.Contains("/survey")),
                It.IsAny<int?>(),
                It.IsAny<Telegram.Bot.Types.Enums.ParseMode?>(),
                It.IsAny<System.Collections.Generic.IEnumerable<Telegram.Bot.Types.MessageEntity>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartSurvey_ActiveVsInactive_OnlyStartsActiveOnes()
    {
        // Arrange - Create both active and inactive surveys
        var activeSurvey = new Survey
        {
            Title = "Active Survey",
            CreatorId = _fixture.TestUser.Id,
            IsActive = true,
            AllowMultipleResponses = true,
            CreatedAt = DateTime.UtcNow
        };

        var inactiveSurvey = new Survey
        {
            Title = "Inactive Survey",
            CreatorId = _fixture.TestUser.Id,
            IsActive = false,
            AllowMultipleResponses = true,
            CreatedAt = DateTime.UtcNow
        };

        await _fixture.DbContext.Surveys.AddRangeAsync(activeSurvey, inactiveSurvey);
        await _fixture.DbContext.SaveChangesAsync();

        // Add at least one question to active survey
        await _fixture.DbContext.Questions.AddAsync(new Question
        {
            SurveyId = activeSurvey.Id,
            QuestionText = "Test?",
            QuestionType = QuestionType.Text,
            OrderIndex = 0,
            IsRequired = true,
            CreatedAt = DateTime.UtcNow
        });
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
