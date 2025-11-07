using Telegram.Bot.Types;

namespace SurveyBot.Bot.Interfaces;

/// <summary>
/// Interface for handling bot commands.
/// Each command handler implements this interface to process specific commands.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Gets the command name this handler processes (e.g., "start", "help", "mysurveys").
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Handles the command execution.
    /// </summary>
    /// <param name="message">The message containing the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the command description for help text.
    /// </summary>
    string GetDescription();
}
