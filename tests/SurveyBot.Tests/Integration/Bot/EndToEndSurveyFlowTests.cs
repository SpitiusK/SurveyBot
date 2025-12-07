using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Validators;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace SurveyBot.Tests.Integration.Bot;

/// <summary>
/// Integration tests for complete end-to-end survey flow through bot.
/// Tests the full user experience from starting a survey to completion.
/// </summary>
public class EndToEndSurveyFlowTests : IClassFixture<BotTestFixture>
{
    private readonly BotTestFixture _fixture;
    private readonly SurveyCommandHandler _surveyCommandHandler;
    private readonly CompletionHandler _completionHandler;
    private readonly TextQuestionHandler _textHandler;
    private readonly SingleChoiceQuestionHandler _singleChoiceHandler;
    private readonly MultipleChoiceQuestionHandler _multipleChoiceHandler;
    private readonly RatingQuestionHandler _ratingHandler;
    private readonly List<IQuestionHandler> _questionHandlers;

    private const long TestUserId = 123456789;
    private const long TestChatId = 123456789;

    public EndToEndSurveyFlowTests(BotTestFixture fixture)
    {
        _fixture = fixture;

        // Create question handlers
        var validator = new AnswerValidator(Mock.Of<ILogger<AnswerValidator>>());
        var errorHandler = new QuestionErrorHandler(_fixture.MockBotService.Object, Mock.Of<ILogger<QuestionErrorHandler>>());
        var mockMediaService = new Mock<SurveyBot.Core.Interfaces.ITelegramMediaService>();
        var mockBotConfig = Microsoft.Extensions.Options.Options.Create(new SurveyBot.Bot.Configuration.BotConfiguration());
        var mediaHelper = new QuestionMediaHelper(mockMediaService.Object, mockBotConfig, Mock.Of<ILogger<QuestionMediaHelper>>());

        _textHandler = new TextQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            mediaHelper,
            Mock.Of<ILogger<TextQuestionHandler>>());

        _singleChoiceHandler = new SingleChoiceQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            mediaHelper,
            Mock.Of<ILogger<SingleChoiceQuestionHandler>>());

        _multipleChoiceHandler = new MultipleChoiceQuestionHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            validator,
            errorHandler,
            mediaHelper,
            Mock.Of<ILogger<MultipleChoiceQuestionHandler>>());

        _ratingHandler = new RatingQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            mediaHelper,
            Mock.Of<ILogger<RatingQuestionHandler>>());

        _questionHandlers = new List<IQuestionHandler>
        {
            _textHandler,
            _singleChoiceHandler,
            _multipleChoiceHandler,
            _ratingHandler
        };

        // Create completion handler with mock IResponseService
        var mockResponseService = new Mock<IResponseService>();
        mockResponseService
            .Setup(x => x.CompleteResponseAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync((int responseId, int? userId) => new Core.DTOs.Response.ResponseDto
            {
                Id = responseId,
                IsComplete = true,
                SubmittedAt = DateTime.UtcNow,
                AnsweredCount = 3,
                TotalQuestions = 4
            });

        _completionHandler = new CompletionHandler(
            _fixture.MockBotService.Object,
            mockResponseService.Object,
            _fixture.SurveyRepository,
            _fixture.StateManager,
            Mock.Of<ILogger<CompletionHandler>>());

        // Create survey command handler
        _surveyCommandHandler = new SurveyCommandHandler(
            _fixture.MockBotService.Object,
            _fixture.SurveyRepository,
            _fixture.ResponseRepository,
            _fixture.StateManager,
            _completionHandler,
            new SurveyCache(Mock.Of<ILogger<SurveyCache>>()),
            _questionHandlers,
            Mock.Of<ILogger<SurveyCommandHandler>>());
    }

    [Fact]
    public async Task CompleteSurveyFlow_AllQuestionTypes_Success()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var startMessage = _fixture.CreateTestMessage(TestUserId, TestChatId, $"/survey {surveyId}");

        // Act & Assert - Start survey
        await _surveyCommandHandler.HandleAsync(startMessage, CancellationToken.None);

        // Verify state initialized
        var state = await _fixture.StateManager.GetStateAsync(TestUserId);
        state.Should().NotBeNull();
        state!.CurrentSurveyId.Should().Be(surveyId);
        state.CurrentQuestionIndex.Should().Be(0);
        state.TotalQuestions.Should().Be(4);

        // Answer Question 1: Text
        var textAnswer = _fixture.CreateTestMessage(TestUserId, TestChatId, "John Doe");
        var textResult = await _textHandler.ProcessAnswerAsync(
            textAnswer, null, MapToDto(_fixture.TestQuestions[0]), TestUserId, CancellationToken.None);

        textResult.Should().NotBeNull();
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 0, textResult!);
        await _fixture.StateManager.NextQuestionAsync(TestUserId);

        // Submit answer to repository
        var answer1 = EntityBuilder.CreateAnswer(
            responseId: state.CurrentResponseId!.Value,
            questionId: _fixture.TestQuestions[0].Id);
        answer1.SetAnswerJson(textResult);
        await _fixture.AnswerRepository.CreateAsync(answer1);

        // Answer Question 2: Single Choice
        // Callback data format: answer_q{questionId}_opt{optionIndex}
        var question2Dto = MapToDto(_fixture.TestQuestions[1]);
        var singleChoiceCallback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, $"answer_q{question2Dto.Id}_opt1");
        var singleChoiceResult = await _singleChoiceHandler.ProcessAnswerAsync(
            null, singleChoiceCallback, question2Dto, TestUserId, CancellationToken.None);

        singleChoiceResult.Should().NotBeNull();
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 1, singleChoiceResult!);
        await _fixture.StateManager.NextQuestionAsync(TestUserId);

        var answer2 = EntityBuilder.CreateAnswer(
            responseId: state.CurrentResponseId!.Value,
            questionId: _fixture.TestQuestions[1].Id);
        answer2.SetAnswerJson(singleChoiceResult);
        await _fixture.AnswerRepository.CreateAsync(answer2);

        // Answer Question 3: Multiple Choice (optional - skip it)
        // NOTE: SkipQuestionAsync calls NextQuestionAsync internally, so we don't need to call it again
        await _fixture.StateManager.SkipQuestionAsync(TestUserId, false);

        // Verify skip was recorded (TEST-002 regression test)
        var stateAfterSkip = await _fixture.StateManager.GetStateAsync(TestUserId);
        stateAfterSkip.Should().NotBeNull();
        stateAfterSkip!.SkippedCount.Should().Be(1);
        stateAfterSkip.SkippedQuestionIndices.Should().Contain(2); // Question 3 at index 2
        stateAfterSkip.IsQuestionSkipped(2).Should().BeTrue();
        stateAfterSkip.IsQuestionSkipped(0).Should().BeFalse(); // Question 1 was answered, not skipped
        stateAfterSkip.CurrentQuestionIndex.Should().Be(3); // Should have moved to question 4

        // Answer Question 4: Rating
        // Callback data format: rating_q{questionId}_r{ratingValue}
        var question4Dto = MapToDto(_fixture.TestQuestions[3]);
        var ratingCallback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, $"rating_q{question4Dto.Id}_r5");
        var ratingResult = await _ratingHandler.ProcessAnswerAsync(
            null, ratingCallback, question4Dto, TestUserId, CancellationToken.None);

        ratingResult.Should().NotBeNull();
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 3, ratingResult!);

        var answer3 = EntityBuilder.CreateAnswer(
            responseId: state.CurrentResponseId!.Value,
            questionId: _fixture.TestQuestions[3].Id);
        answer3.SetAnswerJson(ratingResult);
        await _fixture.AnswerRepository.CreateAsync(answer3);

        // Complete survey - verify skip-aware completion works (TEST-002 fix)
        var isComplete = await _fixture.StateManager.IsAllAnsweredAsync(TestUserId);
        isComplete.Should().BeTrue("3 answered + 1 skipped should equal 4 total questions");

        await _completionHandler.HandleCompletionAsync(TestChatId, TestUserId, CancellationToken.None);

        // Verify final state
        var finalState = await _fixture.StateManager.GetStateAsync(TestUserId);
        finalState!.CurrentState.Should().Be(ConversationStateType.ResponseComplete);

        // Note: Since we use a mock IResponseService, we need to manually complete the Response
        // in the database to verify the full flow. In production, ResponseService handles this.
        var response = await _fixture.ResponseRepository.GetByIdAsync(state.CurrentResponseId!.Value);
        response.Should().NotBeNull();

        // Complete the response manually since mock doesn't update database
        response!.MarkAsComplete();
        await _fixture.ResponseRepository.UpdateAsync(response);

        // Re-fetch and verify completion
        var completedResponse = await _fixture.ResponseRepository.GetByIdAsync(state.CurrentResponseId!.Value);
        completedResponse.Should().NotBeNull();
        completedResponse!.IsComplete.Should().BeTrue();
        completedResponse.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartSurvey_ValidSurveyId_DisplaysFirstQuestion()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        var message = _fixture.CreateTestMessage(TestUserId + 1, TestChatId + 1, $"/survey {surveyId}");

        // Act
        await _surveyCommandHandler.HandleAsync(message, CancellationToken.None);

        // Assert
        var state = await _fixture.StateManager.GetStateAsync(TestUserId + 1);
        state.Should().NotBeNull();
        state!.CurrentSurveyId.Should().Be(surveyId);
        state.CurrentQuestionIndex.Should().Be(0);
        state.TotalQuestions.Should().Be(4);

        // Verify bot sent survey intro message via IBotService wrapper
        _fixture.MockBotService.Verify(
            x => x.SendMessageAsync(
                It.Is<ChatId>(c => c.Identifier == TestChatId + 1),
                It.Is<string>(msg => msg.Contains("Test Survey")),
                It.IsAny<ParseMode?>(),
                It.IsAny<InlineKeyboardMarkup?>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "SurveyCommandHandler should send intro message containing survey title");
    }

    [Fact]
    public async Task AnswerTextQuestion_ValidInput_RecordsAnswer()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 2, surveyId, 100, 4);

        var message = _fixture.CreateTestMessage(TestUserId + 2, TestChatId + 2, "My test answer");
        var question = MapToDto(_fixture.TestQuestions[0]);

        // Act
        var result = await _textHandler.ProcessAnswerAsync(message, null, question, TestUserId + 2, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var answerData = JsonSerializer.Deserialize<JsonElement>(result!);
        answerData.GetProperty("text").GetString().Should().Be("My test answer");
    }

    [Fact]
    public async Task AnswerSingleChoiceQuestion_ValidOption_RecordsAnswer()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 3, surveyId, 101, 4);
        await _fixture.StateManager.NextQuestionAsync(TestUserId + 3);

        var question = MapToDto(_fixture.TestQuestions[1]);
        // Callback data format: answer_q{questionId}_opt{optionIndex} (index 1 = "Blue")
        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 3, TestChatId + 3, $"answer_q{question.Id}_opt1");

        // Act
        var result = await _singleChoiceHandler.ProcessAnswerAsync(null, callback, question, TestUserId + 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var answerData = JsonSerializer.Deserialize<JsonElement>(result!);
        answerData.GetProperty("selectedOption").GetString().Should().Be("Blue");
    }

    [Fact]
    public async Task AnswerMultipleChoiceQuestion_MultipleOptions_RecordsAllSelections()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 4, surveyId, 102, 4);

        var question = MapToDto(_fixture.TestQuestions[2]);

        // Act - First selection (C# is at index 0)
        // Callback data format: toggle_q{questionId}_opt{optionIndex}
        var callback1 = _fixture.CreateTestCallbackQuery(TestUserId + 4, TestChatId + 4, $"toggle_q{question.Id}_opt0");
        await _multipleChoiceHandler.ProcessAnswerAsync(null, callback1, question, TestUserId + 4, CancellationToken.None);

        // Second selection (JavaScript is at index 2)
        var callback2 = _fixture.CreateTestCallbackQuery(TestUserId + 4, TestChatId + 4, $"toggle_q{question.Id}_opt2");
        await _multipleChoiceHandler.ProcessAnswerAsync(null, callback2, question, TestUserId + 4, CancellationToken.None);

        // Submit - Callback data format: done_q{questionId}
        var submitCallback = _fixture.CreateTestCallbackQuery(TestUserId + 4, TestChatId + 4, $"done_q{question.Id}");
        var result = await _multipleChoiceHandler.ProcessAnswerAsync(null, submitCallback, question, TestUserId + 4, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var answerData = JsonSerializer.Deserialize<JsonElement>(result!);
        var selectedOptions = answerData.GetProperty("selectedOptions").EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        selectedOptions.Should().HaveCount(2);
        selectedOptions.Should().Contain("C#");
        selectedOptions.Should().Contain("JavaScript");
    }

    [Fact]
    public async Task AnswerRatingQuestion_ValidRating_RecordsAnswer()
    {
        // Arrange
        var surveyId = _fixture.TestSurvey.Id;
        await _fixture.StateManager.StartSurveyAsync(TestUserId + 5, surveyId, 103, 4);

        var question = MapToDto(_fixture.TestQuestions[3]);
        // Callback data format: rating_q{questionId}_r{ratingValue}
        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 5, TestChatId + 5, $"rating_q{question.Id}_r4");

        // Act
        var result = await _ratingHandler.ProcessAnswerAsync(null, callback, question, TestUserId + 5, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var answerData = JsonSerializer.Deserialize<JsonElement>(result!);
        answerData.GetProperty("rating").GetInt32().Should().Be(4);
    }

    private Core.DTOs.Question.QuestionDto MapToDto(Question question)
    {
        List<string>? options = null;
        if (!string.IsNullOrWhiteSpace(question.OptionsJson))
        {
            options = JsonSerializer.Deserialize<List<string>>(question.OptionsJson);
        }

        return new Core.DTOs.Question.QuestionDto
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
