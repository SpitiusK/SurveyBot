using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Questions;

/// <summary>
/// Handles multiple choice questions where users can select multiple options.
/// Uses inline keyboard with checkable buttons and a "Done" button to confirm selections.
/// Includes comprehensive validation and error handling.
/// </summary>
public class MultipleChoiceQuestionHandler : IQuestionHandler
{
    private readonly IBotService _botService;
    private readonly IConversationStateManager _stateManager;
    private readonly IAnswerValidator _validator;
    private readonly QuestionErrorHandler _errorHandler;
    private readonly ILogger<MultipleChoiceQuestionHandler> _logger;

    public QuestionType QuestionType => QuestionType.MultipleChoice;

    public MultipleChoiceQuestionHandler(
        IBotService botService,
        IConversationStateManager stateManager,
        IAnswerValidator validator,
        QuestionErrorHandler errorHandler,
        ILogger<MultipleChoiceQuestionHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays multiple choice question with checkable inline keyboard.
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
                "Multiple choice question {QuestionId} has no options",
                question.Id);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Error: This question has no options configured.",
                cancellationToken: cancellationToken);

            throw new InvalidOperationException($"Multiple choice question {question.Id} has no options");
        }

        var progressText = $"Question {currentIndex + 1} of {totalQuestions}";
        var requiredText = question.IsRequired ? "(Required - select at least one)" : "(Optional)";

        var message = $"{progressText}\n\n" +
                      $"*{question.QuestionText}*\n\n" +
                      $"{requiredText}\n" +
                      $"Select all that apply, then click Done:";

        // Initialize empty selection for this user/question in conversation state
        var selections = new HashSet<int>();
        await SetSelectionsAsync(chatId, question.Id, selections);

        // Build inline keyboard with checkable options
        var keyboard = BuildKeyboard(question, selections);

        _logger.LogDebug(
            "Displaying multiple choice question {QuestionId} with {OptionCount} options in chat {ChatId}",
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
    /// Processes answer from callback query (button clicks).
    /// Handles toggle selections and "Done" confirmation.
    /// </summary>
    public async Task<string?> ProcessAnswerAsync(
        Message? message,
        CallbackQuery? callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken = default)
    {
        // Multiple choice requires callback query (button clicks)
        if (callbackQuery == null || string.IsNullOrWhiteSpace(callbackQuery.Data))
        {
            _logger.LogDebug("Multiple choice question requires callback query");
            return null;
        }

        var callbackData = callbackQuery.Data;
        var chatId = callbackQuery.Message?.Chat.Id ?? userId;

        // Get current selections from conversation state
        var selections = await GetSelectionsAsync(userId, question.Id);

        // Check if user clicked "Done"
        if (callbackData == $"done_q{question.Id}")
        {
            return await HandleDoneAsync(callbackQuery, question, selections, userId, cancellationToken);
        }

        // Check if user clicked "Skip"
        if (callbackData == $"skip_q{question.Id}")
        {
            return await HandleSkipAsync(callbackQuery, question, userId, cancellationToken);
        }

        // Otherwise, it's a toggle selection
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
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Invalid option.",
                cancellationToken: cancellationToken);

            return null;
        }

        // Toggle selection
        bool isNowSelected;
        if (selections.Contains(optionIndex))
        {
            selections.Remove(optionIndex);
            isNowSelected = false;
            _logger.LogDebug("Deselected option {OptionIndex} for question {QuestionId}", optionIndex, question.Id);
        }
        else
        {
            selections.Add(optionIndex);
            isNowSelected = true;
            _logger.LogDebug("Selected option {OptionIndex} for question {QuestionId}", optionIndex, question.Id);
        }

        // Save updated selections to conversation state
        await SetSelectionsAsync(userId, question.Id, selections);

        // Update keyboard to reflect new selections
        var keyboard = BuildKeyboard(question, selections);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: isNowSelected ? "✓ Selected" : "Deselected",
            cancellationToken: cancellationToken);

        // Update the message with new keyboard
        if (callbackQuery.Message != null)
        {
            try
            {
                await _botService.Client.EditMessageReplyMarkup(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update keyboard after selection toggle");
            }
        }

        // Return null because user hasn't clicked "Done" yet
        return null;
    }

    /// <summary>
    /// Validates multiple choice answer format and content.
    /// </summary>
    public bool ValidateAnswer(string? answerJson, QuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(answerJson))
            return !question.IsRequired;

        try
        {
            var answer = JsonSerializer.Deserialize<JsonElement>(answerJson);

            if (!answer.TryGetProperty("selectedOptions", out var selectedOptionsElement))
                return false;

            var selectedOptions = selectedOptionsElement.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => s != null)
                .ToList();

            // Required questions must have at least one selection
            if (question.IsRequired && selectedOptions.Count == 0)
                return false;

            // Validate that all selected options exist in question options
            if (question.Options != null)
            {
                foreach (var option in selectedOptions)
                {
                    if (!question.Options.Contains(option))
                        return false;
                }
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON format for multiple choice answer");
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Builds inline keyboard with checkable options and Done button.
    /// </summary>
    private InlineKeyboardMarkup BuildKeyboard(QuestionDto question, HashSet<int> selections)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Add option buttons with checkmarks for selected options
        for (int i = 0; i < question.Options!.Count; i++)
        {
            var option = question.Options[i];
            var isSelected = selections.Contains(i);
            var buttonText = isSelected ? $"✓ {option}" : $"  {option}";
            var callbackData = $"toggle_q{question.Id}_opt{i}";

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: buttonText,
                    callbackData: callbackData)
            });
        }

        // Add Done button
        var doneButton = InlineKeyboardButton.WithCallbackData(
            text: $"✅ Done ({selections.Count} selected)",
            callbackData: $"done_q{question.Id}");

        buttons.Add(new[] { doneButton });

        // Add navigation row (Back and Skip buttons)
        var navigationRow = new List<InlineKeyboardButton>();

        // Back button (always show)
        navigationRow.Add(InlineKeyboardButton.WithCallbackData(
            text: "⬅️ Back",
            callbackData: $"nav_back_q{question.Id}"));

        // Skip button for optional questions
        if (!question.IsRequired)
        {
            navigationRow.Add(InlineKeyboardButton.WithCallbackData(
                text: "⏭ Skip",
                callbackData: $"nav_skip_q{question.Id}"));
        }

        if (navigationRow.Count > 0)
        {
            buttons.Add(navigationRow.ToArray());
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Handles the "Done" button click and finalizes answer.
    /// </summary>
    private async Task<string?> HandleDoneAsync(
        CallbackQuery callbackQuery,
        QuestionDto question,
        HashSet<int> selections,
        long userId,
        CancellationToken cancellationToken)
    {
        // Validate that at least one option is selected for required questions
        if (question.IsRequired && selections.Count == 0)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Please select at least one option (question is required).",
                showAlert: true,
                cancellationToken: cancellationToken);

            return null;
        }

        // Convert indices to option strings
        var selectedOptions = selections
            .OrderBy(i => i)
            .Select(i => question.Options![i])
            .ToList();

        // Create answer JSON
        var answerJson = JsonSerializer.Serialize(new { selectedOptions });

        // Validate the answer
        var validationResult = _validator.ValidateAnswer(answerJson, question);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Multiple choice answer validation failed for question {QuestionId}: {ErrorMessage}",
                question.Id,
                validationResult.ErrorMessage);

            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: validationResult.ErrorMessage!,
                showAlert: true,
                cancellationToken: cancellationToken);

            return null;
        }

        _logger.LogDebug(
            "Multiple choice answer finalized for question {QuestionId}: {SelectedCount} options",
            question.Id,
            selectedOptions.Count);

        // Answer callback query
        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: $"Confirmed {selectedOptions.Count} selection(s)",
            cancellationToken: cancellationToken);

        // Update message to show final selections
        if (callbackQuery.Message != null)
        {
            try
            {
                var selectionText = string.Join("\n", selectedOptions.Select(o => $"  • {o}"));
                await _botService.Client.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"{callbackQuery.Message.Text}\n\n✓ Your answers:\n{selectionText}",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to edit message after Done");
            }
        }

        // Clean up temporary selections from conversation state
        await ClearSelectionsAsync(userId, question.Id);

        return answerJson;
    }

    /// <summary>
    /// Handles the "Skip" button for optional questions.
    /// </summary>
    private async Task<string?> HandleSkipAsync(
        CallbackQuery callbackQuery,
        QuestionDto question,
        long userId,
        CancellationToken cancellationToken)
    {
        if (question.IsRequired)
        {
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "This question is required and cannot be skipped.",
                showAlert: true,
                cancellationToken: cancellationToken);

            return null;
        }

        // Clean up temporary selections from conversation state
        await ClearSelectionsAsync(userId, question.Id);

        // Return empty array for skipped optional question
        var answerJson = JsonSerializer.Serialize(new { selectedOptions = new List<string>() });

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Question skipped",
            cancellationToken: cancellationToken);

        return answerJson;
    }

    /// <summary>
    /// Parses callback data to extract option index.
    /// Format: "toggle_q{questionId}_opt{optionIndex}"
    /// </summary>
    private bool TryParseCallbackData(string callbackData, int questionId, out int optionIndex)
    {
        optionIndex = -1;

        if (string.IsNullOrWhiteSpace(callbackData))
            return false;

        var expectedPrefix = $"toggle_q{questionId}_opt";
        if (!callbackData.StartsWith(expectedPrefix))
            return false;

        var optionIndexStr = callbackData.Substring(expectedPrefix.Length);
        return int.TryParse(optionIndexStr, out optionIndex);
    }

    /// <summary>
    /// Gets the metadata key for storing selections in conversation state.
    /// </summary>
    private string GetMetadataKey(int questionId)
    {
        return $"mc_selections_q{questionId}";
    }

    /// <summary>
    /// Retrieves current selections from conversation state.
    /// </summary>
    private async Task<HashSet<int>> GetSelectionsAsync(long userId, int questionId)
    {
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null)
            return new HashSet<int>();

        var key = GetMetadataKey(questionId);
        if (state.Metadata.TryGetValue(key, out var value) && value is HashSet<int> selections)
        {
            return selections;
        }

        return new HashSet<int>();
    }

    /// <summary>
    /// Stores selections in conversation state.
    /// </summary>
    private async Task SetSelectionsAsync(long userId, int questionId, HashSet<int> selections)
    {
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null)
        {
            _logger.LogWarning("Cannot set selections - no conversation state for user {UserId}", userId);
            return;
        }

        var key = GetMetadataKey(questionId);
        state.Metadata[key] = selections;

        // State is automatically persisted in ConversationStateManager's ConcurrentDictionary
    }

    /// <summary>
    /// Clears selections from conversation state.
    /// </summary>
    private async Task ClearSelectionsAsync(long userId, int questionId)
    {
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null)
            return;

        var key = GetMetadataKey(questionId);
        state.Metadata.Remove(key);

        // State is automatically persisted in ConversationStateManager's ConcurrentDictionary
    }

    #endregion
}
