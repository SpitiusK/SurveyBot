using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Question;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Handles error display and retry mechanism for question handlers.
/// Provides consistent error messaging and question re-display functionality.
/// </summary>
public class QuestionErrorHandler
{
    private readonly IBotService _botService;
    private readonly ILogger<QuestionErrorHandler> _logger;

    public QuestionErrorHandler(
        IBotService botService,
        ILogger<QuestionErrorHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays a validation error to the user with clear instructions on how to fix it.
    /// </summary>
    public async Task ShowValidationErrorAsync(
        long chatId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var message = $"❌ *Validation Error*\n\n{errorMessage}\n\nPlease try again.";

        _logger.LogDebug("Showing validation error to chat {ChatId}: {ErrorMessage}", chatId, errorMessage);

        await _botService.SendMessageAsync(
            chatId,
            message,
            ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Displays a session timeout message to the user.
    /// </summary>
    public async Task ShowSessionTimeoutMessageAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        var message =
            "⏱ *Session Expired*\n\n" +
            "Your session has expired due to inactivity (30 minutes).\n\n" +
            "Your progress has been saved. Use /surveys to browse and continue surveys, " +
            "or /mysurveys to manage your surveys.";

        _logger.LogInformation("Showing session timeout message to chat {ChatId}", chatId);

        await _botService.SendMessageAsync(
            chatId,
            message,
            ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Displays an API error message to the user.
    /// </summary>
    public async Task ShowApiErrorAsync(
        long chatId,
        string operation,
        int? statusCode = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var message = $"❌ *Error*\n\n" +
                      $"An error occurred while {operation}.";

        if (statusCode.HasValue)
        {
            message += $"\n\nError Code: {statusCode}";
        }

        if (!string.IsNullOrWhiteSpace(details))
        {
            message += $"\n\nDetails: {details}";
        }

        message += "\n\nPlease try again later or contact support if the problem persists.";

        _logger.LogError(
            "Showing API error to chat {ChatId}: {Operation}, StatusCode: {StatusCode}, Details: {Details}",
            chatId,
            operation,
            statusCode,
            details);

        await _botService.SendMessageAsync(
            chatId,
            message,
            ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Displays a general error message to the user.
    /// </summary>
    public async Task ShowGeneralErrorAsync(
        long chatId,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var message = customMessage ??
            "❌ *Error*\n\n" +
            "An unexpected error occurred. Please try again.\n\n" +
            "If the problem persists, please contact support.";

        _logger.LogError("Showing general error to chat {ChatId}: {Message}", chatId, customMessage);

        await _botService.SendMessageAsync(
            chatId,
            message,
            ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Displays a data consistency error (e.g., survey deleted, question modified).
    /// </summary>
    public async Task ShowDataConsistencyErrorAsync(
        long chatId,
        string issue,
        CancellationToken cancellationToken = default)
    {
        var message =
            "❌ *Survey Data Changed*\n\n" +
            $"{issue}\n\n" +
            "The survey may have been modified or deleted. " +
            "Please use /surveys to start fresh.";

        _logger.LogWarning("Showing data consistency error to chat {ChatId}: {Issue}", chatId, issue);

        await _botService.SendMessageAsync(
            chatId,
            message,
            ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Displays a processing indicator to show the bot is working.
    /// </summary>
    public async Task<int> ShowProcessingMessageAsync(
        long chatId,
        string action = "Processing your answer",
        CancellationToken cancellationToken = default)
    {
        var message = $"⏳ {action}...";

        var sentMessage = await _botService.SendMessageAsync(
            chatId,
            message,
            cancellationToken: cancellationToken);

        return sentMessage.MessageId;
    }

    /// <summary>
    /// Deletes a processing message.
    /// </summary>
    public async Task DeleteProcessingMessageAsync(
        long chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _botService.DeleteMessageAsync(
                chatId,
                messageId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete processing message {MessageId} in chat {ChatId}",
                messageId, chatId);
        }
    }

    /// <summary>
    /// Checks for concurrent request (double submission) and shows error if detected.
    /// </summary>
    public async Task<bool> CheckAndHandleConcurrentRequestAsync(
        long chatId,
        bool isProcessing,
        CancellationToken cancellationToken = default)
    {
        if (isProcessing)
        {
            await ShowValidationErrorAsync(
                chatId,
                "Please wait while your previous answer is being processed.",
                cancellationToken);

            return true; // Concurrent request detected
        }

        return false; // No concurrent request
    }
}
