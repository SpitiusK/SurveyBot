using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Interface for Telegram Bot service operations.
/// Manages bot client lifecycle and provides access to bot functionality.
/// </summary>
public interface IBotService
{
    /// <summary>
    /// Gets the Telegram Bot client instance.
    /// </summary>
    ITelegramBotClient Client { get; }

    /// <summary>
    /// Initializes the bot and validates the bot token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bot information if successful.</returns>
    Task<User> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets up the webhook for the bot.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if webhook was set successfully.</returns>
    Task<bool> SetWebhookAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the webhook and switches to long polling mode.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if webhook was removed successfully.</returns>
    Task<bool> RemoveWebhookAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current webhook information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Webhook info.</returns>
    Task<WebhookInfo> GetWebhookInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bot information (username, id, etc.).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bot user information.</returns>
    Task<User> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the webhook secret token matches the configured value.
    /// </summary>
    /// <param name="secretToken">Secret token from webhook request header.</param>
    /// <returns>True if token is valid.</returns>
    bool ValidateWebhookSecret(string? secretToken);

    // Mockable wrapper methods for Telegram.Bot operations
    // These wrapper methods enable mocking of Telegram.Bot extension methods in tests.
    // Extension methods are static and cannot be mocked on interfaces, so we wrap them
    // in instance methods on IBotService for testability.

    /// <summary>
    /// Sends a text message to the specified chat.
    /// Wrapper for ITelegramBotClient.SendMessage() extension method.
    /// </summary>
    Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits text of a message.
    /// Wrapper for ITelegramBotClient.EditMessageText() extension method.
    /// </summary>
    Task<Message> EditMessageTextAsync(
        ChatId chatId,
        int messageId,
        string text,
        ParseMode? parseMode = null,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a callback query.
    /// Wrapper for ITelegramBotClient.AnswerCallbackQuery() extension method.
    /// </summary>
    Task AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message.
    /// Wrapper for ITelegramBotClient.DeleteMessage() extension method.
    /// </summary>
    Task DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default);
}
