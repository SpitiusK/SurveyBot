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
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Validators;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using Telegram.Bot.Types;
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
        var validator = new AnswerValidator();
        var errorHandler = new QuestionErrorHandler(_fixture.MockBotService.Object, Mock.Of<ILogger<QuestionErrorHandler>>());

        _textHandler = new TextQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            Mock.Of<ILogger<TextQuestionHandler>>());

        _singleChoiceHandler = new SingleChoiceQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            Mock.Of<ILogger<SingleChoiceQuestionHandler>>());

        _multipleChoiceHandler = new MultipleChoiceQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            Mock.Of<ILogger<MultipleChoiceQuestionHandler>>());

        _ratingHandler = new RatingQuestionHandler(
            _fixture.MockBotService.Object,
            validator,
            errorHandler,
            Mock.Of<ILogger<RatingQuestionHandler>>());

        _questionHandlers = new List<IQuestionHandler>
        {
            _textHandler,
            _singleChoiceHandler,
            _multipleChoiceHandler,
            _ratingHandler
        };

        // Create completion handler
        _completionHandler = new CompletionHandler(
            _fixture.MockBotService.Object,
            _fixture.StateManager,
            _fixture.ResponseRepository,
            Mock.Of<ILogger<CompletionHandler>>());

        // Create survey command handler
        _surveyCommandHandler = new SurveyCommandHandler(
            _fixture.MockBotService.Object,
            _fixture.SurveyRepository,
            _fixture.ResponseRepository,
            _fixture.StateManager,
            _completionHandler,
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
        await _fixture.AnswerRepository.CreateAsync(new Answer
        {
            ResponseId = state.CurrentResponseId!.Value,
            QuestionId = _fixture.TestQuestions[0].Id,
            AnswerJson = textResult
        });

        // Answer Question 2: Single Choice
        var singleChoiceCallback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, "option_1_Blue");
        var singleChoiceResult = await _singleChoiceHandler.ProcessAnswerAsync(
            null, singleChoiceCallback, MapToDto(_fixture.TestQuestions[1]), TestUserId, CancellationToken.None);

        singleChoiceResult.Should().NotBeNull();
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 1, singleChoiceResult!);
        await _fixture.StateManager.NextQuestionAsync(TestUserId);

        await _fixture.AnswerRepository.CreateAsync(new Answer
        {
            ResponseId = state.CurrentResponseId!.Value,
            QuestionId = _fixture.TestQuestions[1].Id,
            AnswerJson = singleChoiceResult
        });

        // Answer Question 3: Multiple Choice (optional - skip it)
        await _fixture.StateManager.SkipQuestionAsync(TestUserId, false);
        await _fixture.StateManager.NextQuestionAsync(TestUserId);

        // Answer Question 4: Rating
        var ratingCallback = _fixture.CreateTestCallbackQuery(TestUserId, TestChatId, "rating_5");
        var ratingResult = await _ratingHandler.ProcessAnswerAsync(
            null, ratingCallback, MapToDto(_fixture.TestQuestions[3]), TestUserId, CancellationToken.None);

        ratingResult.Should().NotBeNull();
        await _fixture.StateManager.AnswerQuestionAsync(TestUserId, 3, ratingResult!);

        await _fixture.AnswerRepository.CreateAsync(new Answer
        {
            ResponseId = state.CurrentResponseId!.Value,
            QuestionId = _fixture.TestQuestions[3].Id,
            AnswerJson = ratingResult
        });

        // Complete survey
        var isComplete = await _fixture.StateManager.IsAllAnsweredAsync(TestUserId);
        isComplete.Should().BeTrue();

        await _completionHandler.HandleCompletionAsync(TestChatId, TestUserId, CancellationToken.None);

        // Verify final state
        var finalState = await _fixture.StateManager.GetStateAsync(TestUserId);
        finalState!.CurrentState.Should().Be(Bot.Models.ConversationStateType.ResponseComplete);

        // Verify response in database
        var response = await _fixture.ResponseRepository.GetByIdAsync(state.CurrentResponseId!.Value);
        response.Should().NotBeNull();
        response!.IsComplete.Should().BeTrue();
        response.SubmittedAt.Should().NotBeNull();
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

        // Verify bot sent messages
        _fixture.MockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(),
                It.Is<string>(s => s.Contains("Test Survey")),
                It.IsAny<int?>(),
                It.IsAny<Telegram.Bot.Types.Enums.ParseMode?>(),
                It.IsAny<IEnumerable<Telegram.Bot.Types.MessageEntity>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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

        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 3, TestChatId + 3, "option_1_Blue");
        var question = MapToDto(_fixture.TestQuestions[1]);

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

        // Act - First selection
        var callback1 = _fixture.CreateTestCallbackQuery(TestUserId + 4, TestChatId + 4, "mco_0_C#");
        await _multipleChoiceHandler.ProcessAnswerAsync(null, callback1, question, TestUserId + 4, CancellationToken.None);

        // Second selection
        var callback2 = _fixture.CreateTestCallbackQuery(TestUserId + 4, TestChatId + 4, "mco_2_JavaScript");
        await _multipleChoiceHandler.ProcessAnswerAsync(null, callback2, question, TestUserId + 4, CancellationToken.None);

        // Submit
        var submitCallback = _fixture.CreateTestCallbackQuery(TestUserId + 4, TestChatId + 4, "mco_submit");
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

        var callback = _fixture.CreateTestCallbackQuery(TestUserId + 5, TestChatId + 5, "rating_4");
        var question = MapToDto(_fixture.TestQuestions[3]);

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
