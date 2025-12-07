using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Handlers;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Requests;
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
        var response = EntityBuilder.CreateResponse(surveyId, TestUserId, false);
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
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == TestChatId &&
                    req.Text.Contains("Are you sure")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Confirm cancellation
        var confirmCallback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, "cancel_confirm");
        await _cancelCallbackHandler.HandleConfirmAsync(confirmCallback, CancellationToken.None);

        // Assert
        // State should be completely cleared (CancelSurveyAsync + ClearStateAsync)
        var state = await _fixture.StateManager.GetStateAsync(TestUserId);
        state.Should().BeNull();

        // Verify response was deleted
        var deletedResponse = await _fixture.ResponseRepository.GetByIdAsync(response.Id);
        deletedResponse.Should().BeNull();

        // Verify confirmation message was edited to show cancellation success
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<EditMessageTextRequest>(req =>
                    req.ChatId.Identifier == TestChatId &&
                    req.Text.Contains("cancelled successfully")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelSurvey_DismissConfirmation_ContinuesSurvey()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;

        var response = EntityBuilder.CreateResponse(surveyId, TestUserId + 1, false);
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

        // Verify confirmation message was edited to show continuation
        _fixture.MockBotClient.Verify(
            x => x.SendRequest(
                It.Is<EditMessageTextRequest>(req =>
                    req.ChatId.Identifier == (TestChatId + 1) &&
                    req.Text.Contains("Continuing survey")),
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
            x => x.SendRequest(
                It.Is<SendMessageRequest>(req =>
                    req.ChatId.Identifier == (TestChatId + 2) &&
                    req.Text.Contains("not currently taking a survey")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelSurvey_VerifyAnswersDeleted_WhenResponseDeleted()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;

        var response = EntityBuilder.CreateResponse(surveyId, TestUserId + 3, false);
        await _fixture.DbContext.Responses.AddAsync(response);
        await _fixture.DbContext.SaveChangesAsync();

        // Add some answers
        var answer1 = EntityBuilder.CreateAnswer(response.Id, _fixture.TestQuestions[0].Id, "Answer 1");
        answer1.SetAnswerJson("{\"text\":\"Answer 1\"}");

        var answer2 = EntityBuilder.CreateAnswer(response.Id, _fixture.TestQuestions[1].Id, "Blue");
        answer2.SetAnswerJson("{\"selectedOption\":\"Blue\"}");

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
