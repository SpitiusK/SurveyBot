using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Handles incoming updates from Telegram.
/// Routes updates to appropriate handlers based on update type.
/// </summary>
public class UpdateHandler : IUpdateHandler
{
    private readonly IBotService _botService;
    private readonly CommandRouter _commandRouter;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        IBotService botService,
        CommandRouter commandRouter,
        ILogger<UpdateHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received update {UpdateId} of type {UpdateType}",
                update.Id,
                update.Type);

            var handled = update.Type switch
            {
                UpdateType.Message => await HandleMessageAsync(update.Message!, cancellationToken),
                UpdateType.CallbackQuery => await HandleCallbackQueryAsync(update.CallbackQuery!, cancellationToken),
                UpdateType.EditedMessage => await HandleEditedMessageAsync(update.EditedMessage!, cancellationToken),
                _ => await HandleUnsupportedUpdateAsync(update, cancellationToken)
            };

            if (handled)
            {
                _logger.LogInformation("Update {UpdateId} handled successfully", update.Id);
            }
            else
            {
                _logger.LogDebug("Update {UpdateId} was not handled", update.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
            await HandleErrorAsync(ex, cancellationToken);
        }
    }

    public async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, "Error in update handler: {ErrorMessage}", exception.Message);

        // Log specific error details
        if (exception is Telegram.Bot.Exceptions.ApiRequestException apiException)
        {
            _logger.LogError(
                "Telegram API Error: {ErrorCode} - {ErrorMessage}",
                apiException.ErrorCode,
                apiException.Message);
        }

        // In production, you might want to:
        // - Send notification to administrators
        // - Log to external error tracking service
        // - Store error details in database

        await Task.CompletedTask;
    }

    private async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text == null)
        {
            _logger.LogDebug("Message has no text content, ignoring");
            return false;
        }

        _logger.LogInformation(
            "Processing message from user {TelegramId} in chat {ChatId}: {MessageText}",
            message.From?.Id,
            message.Chat.Id,
            message.Text.Length > 50 ? message.Text.Substring(0, 47) + "..." : message.Text);

        // Check if message is a command
        if (message.Text.StartsWith('/'))
        {
            return await _commandRouter.RouteCommandAsync(message, cancellationToken);
        }

        // Handle regular text messages
        return await HandleTextMessageAsync(message, cancellationToken);
    }

    private async Task<bool> HandleTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        // For now, we'll just acknowledge the message
        // In the future, this will handle survey responses and other interactions

        _logger.LogDebug(
            "Received text message from user {TelegramId}: {Text}",
            message.From?.Id,
            message.Text);

        // We don't handle regular messages yet - they'll be used for survey responses
        // Send a helpful message
        var helpText = "I understand commands like /start and /help. " +
                      "To interact with surveys, use the inline keyboard buttons.";

        try
        {
            await _botService.Client.SendMessage(
                chatId: message.Chat.Id,
                text: helpText,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send help text to chat {ChatId}", message.Chat.Id);
            return false;
        }
    }

    private async Task<bool> HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Data == null || callbackQuery.Message == null)
        {
            _logger.LogWarning("Callback query has no data or message");
            return false;
        }

        _logger.LogInformation(
            "Processing callback query from user {TelegramId}: {CallbackData}",
            callbackQuery.From.Id,
            callbackQuery.Data);

        try
        {
            // Parse callback data
            var parts = callbackQuery.Data.Split(':');
            var action = parts.Length > 0 ? parts[0] : string.Empty;

            // Route callback to appropriate handler
            var handled = action switch
            {
                "cmd" => await HandleCallbackCommandAsync(callbackQuery, parts, cancellationToken),
                "survey" => await HandleSurveyCallbackAsync(callbackQuery, parts, cancellationToken),
                "action" => await HandleActionCallbackAsync(callbackQuery, parts, cancellationToken),
                _ => await HandleUnknownCallbackAsync(callbackQuery, cancellationToken)
            };

            // Answer callback query to remove loading state
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);

            return handled;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling callback query from user {TelegramId}",
                callbackQuery.From.Id);

            // Try to answer callback query even if there was an error
            try
            {
                await _botService.Client.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "An error occurred. Please try again.",
                    showAlert: true,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // Ignore errors when answering callback query
            }

            return false;
        }
    }

    private async Task<bool> HandleCallbackCommandAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (parts.Length < 2)
        {
            _logger.LogWarning("Invalid callback command format");
            return false;
        }

        var command = parts[1];

        _logger.LogInformation(
            "Processing callback command '{Command}' from user {TelegramId}",
            command,
            callbackQuery.From.Id);

        // Check if command is registered
        if (!_commandRouter.IsCommandRegistered(command))
        {
            _logger.LogWarning(
                "Callback command '{Command}' not registered",
                command);
            return false;
        }

        // Get the handler directly
        var handler = _commandRouter.GetAllHandlers()
            .FirstOrDefault(h => h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            return false;
        }

        // Execute handler with the callback's message
        await handler.HandleAsync(callbackQuery.Message!, cancellationToken);

        return true;
    }

    private async Task<bool> HandleSurveyCallbackAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        // Survey actions: view, toggle, etc.
        // This will be implemented in future tasks

        _logger.LogInformation(
            "Survey callback not yet implemented: {CallbackData}",
            callbackQuery.Data);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "This feature is coming soon!",
            showAlert: true,
            cancellationToken: cancellationToken);

        return true;
    }

    private async Task<bool> HandleActionCallbackAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        // Generic actions: create_survey, etc.
        // This will be implemented in future tasks

        _logger.LogInformation(
            "Action callback not yet implemented: {CallbackData}",
            callbackQuery.Data);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "This feature is coming soon!",
            showAlert: true,
            cancellationToken: cancellationToken);

        return true;
    }

    private async Task<bool> HandleUnknownCallbackAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Unknown callback query type: {CallbackData}",
            callbackQuery.Data);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Unknown action",
            showAlert: true,
            cancellationToken: cancellationToken);

        return false;
    }

    private async Task<bool> HandleEditedMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Edited message from user {TelegramId} in chat {ChatId}",
            message.From?.Id,
            message.Chat.Id);

        // We don't handle edited messages differently for now
        // Could implement specific logic if needed

        return await Task.FromResult(false);
    }

    private async Task<bool> HandleUnsupportedUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Received unsupported update type {UpdateType} with ID {UpdateId}",
            update.Type,
            update.Id);

        return await Task.FromResult(false);
    }
}
