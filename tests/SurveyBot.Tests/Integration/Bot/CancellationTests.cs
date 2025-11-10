using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Handlers;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Types;
using Xunit;

namespace SurveyBot.Tests.Integration.Bot;

/// <summary>
/// Integration tests for survey cancellation functionality.
/// Tests cancel flow and response cleanup.
/// </summary>
public class CancellationTests : IClassFixture<BotTestFixture>
{
    private readonly BotTestFixture _fixture;
    private readonly CancelCommandHandler _cancelCommandHandler;
    private readonly CancelCallbackHandler _cancelCallbackHandler;

    private const long TestUserId = 444555666;
    private const long TestChatId = 444555666;

    public CancellationTests(BotTestFixture fixture)
    {
        _fixture = fixture;

        _cancelCommandHandler = new CancelCommandHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            Mock.Of<ILogger<CancelCommandHandler>>());

        _cancelCallbackHandler = new CancelCallbackHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            _fixture.ResponseRepository,
            Mock.Of<ILogger<CancelCallbackHandler>>());
    }

    [Fact]
    public async Task CancelSurvey_MiddleOfSurvey_DeletesResponseAndClearsState()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;

        // Create response
        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = TestUserId,
            IsComplete = false,
            StartedAt = DateTime.UtcNow
        };
        await _fixture.DbContext.Responses.AddAsync(response);
        await _fixture.DbContext.SaveChangesAsync();

        // Start survey
        await _fixture.StateManager.StartSurveyAsync(TestUserId, surveyId, response.Id, 4);

        // Answer first question
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 0, "{\"text\":\"Test\"}");
        await _fixture.StateManager.NextQuestionAsync(TestUserId);

        var message = _fixture.CreateTestMessage(TestUserId, TestChatId, "/cancel");

        // Act
        await _cancelCommandHandler.HandleAsync(message, CancellationToken.None);

        // Verify confirmation dialog shown
        _fixture.MockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("Are you sure")),
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

        // Confirm cancellation
        var confirmCallback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, "cancel_confirm");
        await _cancelCallbackHandler.HandleConfirmAsync(confirmCallback, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId);
        state.Should().NotBeNull();
        state!.CurrentState.Should().Be(Bot.Models.ConversationStateType.Cancelled);
        state.CurrentSurveyId.Should().BeNull();
        state.CurrentResponseId.Should().BeNull();

        // Verify response was deleted
        var deletedResponse = await _fixture.ResponseRepository.GetByIdAsync(response.Id);
        deletedResponse.Should().BeNull();

        // Verify cancellation message sent
        _fixture.MockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("cancelled")),
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
    public async Task CancelSurvey_DismissConfirmation_ContinuesSurvey()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;

        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = TestUserId + 1,
            IsComplete = false,
            StartedAt = DateTime.UtcNow
        };
        await _fixture.DbContext.Responses.AddAsync(response);
        await _fixture.DbContext.SaveChangesAsync();

        await _fixture.StateManager.StartSurveyAsync(TestUserId + 1, surveyId, response.Id, 4);
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId + 1, 0, "{\"text\":\"Test\"}");

        var message = _fixture.CreateTestMessage(TestUserId + 1, TestChatId + 1, "/cancel");

        // Act
        await _cancelCommandHandler.HandleAsync(message, CancellationToken.None);

        // Dismiss cancellation
        var dismissCallback = _fixture.CreateTestCallbackQuery(TestUserId + 1, TestChatId + 1, "cancel_dismiss");
        await _cancelCallbackHandler.HandleDismissAsync(dismissCallback, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 1);
        state.Should().NotBeNull();
        state!.CurrentSurveyId.Should().Be(surveyId);
        state.CurrentResponseId.Should().Be(response.Id);

        // Response should still exist
        var existingResponse = await _fixture.ResponseRepository.GetByIdAsync(response.Id);
        existingResponse.Should().NotBeNull();

        // Verify "continue" message sent
        _fixture.MockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("continue") || s.Contains("resumed")),
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
    public async Task CancelSurvey_NoActiveSurvey_SendsInfoMessage()
    {
        // Arrange
        var message = _fixture.CreateTestMessage(TestUserId + 2, TestChatId + 2, "/cancel");

        // Act
        await _cancelCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 2);
        state.Should().BeNull();

        // Verify info message sent
        _fixture.MockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("no active survey")),
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
    public async Task CancelSurvey_VerifyAnswersDeleted_WhenResponseDeleted()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;

        var response = new Response
        {
            SurveyId = surveyId,
            RespondentTelegramId = TestUserId + 3,
            IsComplete = false,
            StartedAt = DateTime.UtcNow
        };
        await _fixture.DbContext.Responses.AddAsync(response);
        await _fixture.DbContext.SaveChangesAsync();

        // Add some answers
        var answer1 = new Answer
        {
            ResponseId = response.Id,
            QuestionId = _fixture.TestQuestions[0].Id,
            AnswerJson = "{\"text\":\"Answer 1\"}",
            CreatedAt = DateTime.UtcNow
        };

        var answer2 = new Answer
        {
            ResponseId = response.Id,
            QuestionId = _fixture.TestQuestions[1].Id,
            AnswerJson = "{\"selectedOption\":\"Blue\"}",
            CreatedAt = DateTime.UtcNow
        };

        await _fixture.DbContext.Answers.AddRangeAsync(answer1, answer2);
        await _fixture.DbContext.SaveChangesAsync();

        await _fixture.StateManager.StartSurveyAsync(TestUserId + 3, surveyId, response.Id, 4);

        var message = _fixture.CreateTestMessage(TestUserId + 3, TestChatId + 3, "/cancel");

        // Act
        await _cancelCommandHandler.HandleAsync(message, CancellationToken.None);

        var confirmCallback = _fixture.CreateTestCallbackQuery(TestUserId + 3, TestChatId + 3, "cancel_confirm");
        await _cancelCallbackHandler.HandleConfirmAsync(confirmCallback, CancellationToken.None);

        // Assert
        var deletedResponse = await _fixture.ResponseRepository.GetByIdAsync(response.Id);
        deletedResponse.Should().BeNull();

        // Verify answers were also deleted (cascade delete)
        var deletedAnswer1 = await _fixture.AnswerRepository.GetByIdAsync(answer1.Id);
        var deletedAnswer2 = await _fixture.AnswerRepository.GetByIdAsync(answer2.Id);

        deletedAnswer1.Should().BeNull();
        deletedAnswer2.Should().BeNull();
    }
}
