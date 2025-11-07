using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles single choice questions with inline keyboard buttons.
/// User can select exactly one option from the provided choices.
/// </summary>
public class SingleChoiceQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly ILogger<SingleChoiceQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.SingleChoice;

    public SingleChoiceQuestionHandler(
        IBotService botService,
        ILogger<SingleChoiceQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays single choice question with inline keyboard.
    /// </summary>
    public async Task<int> DisplayQuestionAsync(
        long chatId,
        QuestionDto question,
        int currentIndex,
        int totalQuestions,
        CancellationToken cancellationToken = default)
    {
        if (question.Options == null || question.Options.Count == 0)
        {
            _logger.LogError(
                "Single choice question {QuestionId} has no options",
                question.Id);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Error: This question has no options configured.",
                cancellationToken: cancellationToken);

            throw new InvalidOperationException($"Single choice question {question.Id} has no options");
        }

        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired ? "(Required)" : "(Optional)";

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n\n" +
                      $"{requiredText}\n" +
                      $"Select one option:";

        // Build inline keyboard with one button per option
        var keyboard = BuildKeyboard(question);

        _logger.LogDebug(
            "Displaying single choice question {QuestionId} with {OptionCount} options in chat {ChatId}",
            question.Id,
            question.Options.Count,
            chatId);

        var sentMessage = await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Processes answer from callback query (button click).
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Single choice requires callback query (button click)
        if (callbackQuery == null || string.IsNullOrWhiteSpace(callbackQuery.Data))
        {
            _logger.LogDebug("Single choice question requires callback query");
            return null;
        }

        // Parse callback data: "answer_q{questionId}_opt{optionIndex}"
        var callbackData = callbackQuery.Data;
        if (!TryParseCallbackData(callbackData, question.Id, out var optionIndex))
        {
            _logger.LogWarning(
                "Invalid callback data format: {CallbackData}",
                callbackData);

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Invalid selection. Please try again.",
                cancellationToken: cancellationToken);

            return null;
        }

        // Validate option index
        if (question.Options == null ||
            optionIndex < 0 ||
            optionIndex >= question.Options.Count)
        {
            _logger.LogWarning(
                "Option index {OptionIndex} out of range for question {QuestionId}",
                optionIndex,
                question.Id);

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Invalid option selected.",
                cancellationToken: cancellationToken);

            return null;
        }

        var selectedOption = question.Options[optionIndex];

        // Create answer JSON
        var answerJson = JsonSerializer.Serialize(new { selectedOption });

        _logger.LogDebug(
            "Single choice answer processed for question {QuestionId} from user {UserId}: {SelectedOption}",
            question.Id,
            userId,
            selectedOption);

        // Answer the callback query to remove loading state
        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Selected: {selectedOption}",
            cancellationToken: cancellationToken);

        // Update the message to show selection
        if (callbackQuery.Message != null)
        {
            try
            {
                await _botService.Client.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"{callbackQuery.Message.Text}\n\n✓ Your answer: *{selectedOption}*",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to edit message after selection");
            }
        }

        return answerJson;
    }

    /// <summary>
    /// Validates single choice answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired;

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("selectedOption", out var selectedOptionElement))
                return false;

            var selectedOption = selectedOptionElement.GetString();

            if (question.IsRequired && string.IsNullOrWhiteSpace(selectedOption))
                return false;

            // Validate that selected option exists in question options
            if (question.Options != null && !string.IsNullOrWhiteSpace(selectedOption))
            {
                if (!question.Options.Contains(selectedOption))
                    return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for single choice answer");
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Builds inline keyboard with option buttons.
    /// </summary>
    private InlineKeyboardMarkup BuildKeyboard(QuestionDto question)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < question.Options!.Count; i++)
        {
            var option = question.Options[i];
            var callbackData = $"answer_q{question.Id}_opt{i}";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: option,
                    callbackData: callbackData)
            });
        }

        // Add skip button for optional questions
        if (!question.IsRequired)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "⏭ Skip this question",
                    callbackData: $"skip_q{question.Id}")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Parses callback data to extract option index.
    /// Format: "answer_q{questionId}_opt{optionIndex}"
    /// </summary>
    private bool TryParseCallbackData(string callbackData, int questionId, out int optionIndex)
    {
        optionIndex = -1;

        if (string.IsNullOrWhiteSpace(callbackData))
            return false;

        // Handle skip command
        if (callbackData == $"skip_q{questionId}")
        {
            optionIndex = -1; // Special value for skip
            return true;
        }

        var expectedPrefix = $"answer_q{questionId}_opt";
        if (!callbackData.StartsWith(expectedPrefix))
            return false;

        var optionIndexStr = callbackData.Substring(expectedPrefix.Length);
        return int.TryParse(optionIndexStr, out optionIndex);
    }

    #endregion
}
