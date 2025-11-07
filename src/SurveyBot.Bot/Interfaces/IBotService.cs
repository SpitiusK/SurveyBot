using Telegram.Bot;
using Telegram.Bot.Types;

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
}
