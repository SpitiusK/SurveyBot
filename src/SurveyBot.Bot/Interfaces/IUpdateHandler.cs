using Telegram.Bot.Types;

namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Interface for handling Telegram bot updates.
/// Processes incoming messages, callbacks, and other update types.
/// </summary>
public interface IUpdateHandler
{
    /// <summary>
    /// Handles an incoming update from Telegram.
    /// </summary>
    /// <param name="update">The update to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleUpdateAsync(Update update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles errors that occur during update processing.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken = default);
}
