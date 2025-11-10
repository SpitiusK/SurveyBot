using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers;

/// <summary>
/// Handles cancel-related callback queries:
/// - cancel_confirm: User confirmed cancellation
/// - cancel_dismiss: User dismissed cancellation (continue survey)
/// </summary>
public class CancelCallbackHandler
{
    private readonly IBotService _botService;
    private readonly IConversationStateManager _stateManager;
    private readonly IResponseRepository _responseRepository;
    private readonly ILogger<CancelCallbackHandler> _logger;

    public CancelCallbackHandler(
        IBotService botService,
        IConversationStateManager stateManager,
        IResponseRepository responseRepository,
        ILogger<CancelCallbackHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _responseRepository = responseRepository ?? throw new ArgumentNullException(nameof(responseRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the cancel confirmation callback
    /// </summary>
    public async Task<bool> HandleConfirmAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken = default)
    {
        if (callbackQuery.Message == null || callbackQuery.From == null)
        {
            _logger.LogWarning("Cancel confirm callback with null message or user");
            return false;
        }

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        try
        {
            _logger.LogInformation(
                "Processing cancel confirmation from user {TelegramId}",
                userId);

            // Get current state
            var state = await _stateManager.GetStateAsync(userId);

            if (state == null || state.CurrentResponseId == null)
            {
                _logger.LogWarning(
                    "User {TelegramId} confirmed cancel but has no active survey",
                    userId);

                await SendAlreadyCancelledMessageAsync(chatId, messageId, cancellationToken);
                return true;
            }

            var responseId = state.CurrentResponseId.Value;

            // Delete incomplete response from database
            try
            {
                await _responseRepository.DeleteAsync(responseId);
                _logger.LogInformation(
                    "Deleted incomplete response {ResponseId} for user {TelegramId}",
                    responseId,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete response {ResponseId} for user {TelegramId}",
                    responseId,
                    userId);
                // Continue anyway - state will be cleared
            }

            // Clear conversation state
            await _stateManager.CancelSurveyAsync(userId);
            await _stateManager.ClearStateAsync(userId);

            // Edit the confirmation message to show cancellation success
            await _botService.Client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: "Survey cancelled successfully.\n\n" +
                      "Your incomplete response has been deleted.\n\n" +
                      "Use /surveys to find available surveys.",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Survey cancelled successfully for user {TelegramId}",
                userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling cancel confirmation for user {TelegramId}",
                userId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "An error occurred while cancelling the survey. Please try again.",
                cancellationToken: cancellationToken);

            return false;
        }
    }

    /// <summary>
    /// Handles the cancel dismiss callback (user wants to continue)
    /// </summary>
    public async Task<bool> HandleDismissAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken = default)
    {
        if (callbackQuery.Message == null || callbackQuery.From == null)
        {
            _logger.LogWarning("Cancel dismiss callback with null message or user");
            return false;
        }

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        try
        {
            _logger.LogInformation(
                "Processing cancel dismiss from user {TelegramId}",
                userId);

            // Get current state to verify user is still in survey
            var state = await _stateManager.GetStateAsync(userId);

            if (state == null || state.CurrentSurveyId == null)
            {
                _logger.LogWarning(
                    "User {TelegramId} dismissed cancel but has no active survey",
                    userId);

                await SendNoActiveSurveyMessageAsync(chatId, messageId, cancellationToken);
                return true;
            }

            // Edit the confirmation message to show continuation
            await _botService.Client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: "Continuing survey...\n\n" +
                      "Please answer the current question or use /cancel if you change your mind.",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "User {TelegramId} chose to continue survey {SurveyId}",
                userId,
                state.CurrentSurveyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling cancel dismiss for user {TelegramId}",
                userId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "An error occurred. Please try again.",
                cancellationToken: cancellationToken);

            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Sends message when survey was already cancelled
    /// </summary>
    private async Task SendAlreadyCancelledMessageAsync(
        long chatId,
        int messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _botService.Client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: "This survey has already been cancelled.\n\n" +
                      "Use /surveys to find available surveys.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit message for already cancelled survey");
        }
    }

    /// <summary>
    /// Sends message when no active survey found
    /// </summary>
    private async Task SendNoActiveSurveyMessageAsync(
        long chatId,
        int messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _botService.Client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: "You are not currently taking a survey.\n\n" +
                      "Use /surveys to find available surveys.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit message for no active survey");
        }
    }

    #endregion
}
