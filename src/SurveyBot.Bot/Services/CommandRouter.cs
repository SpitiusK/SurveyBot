using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Routes incoming commands to appropriate command handlers.
/// Handles command parsing, validation, and error handling.
/// </summary>
public class CommandRouter
{
    private readonly IBotService _botService;
    private readonly IEnumerable<ICommandHandler> _commandHandlers;
    private readonly ILogger<CommandRouter> _logger;
    private readonly Dictionary<string, ICommandHandler> _handlerMap;

    public CommandRouter(
        IBotService botService,
        IEnumerable<ICommandHandler> commandHandlers,
        ILogger<CommandRouter> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _commandHandlers = commandHandlers ?? throw new ArgumentNullException(nameof(commandHandlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Build handler lookup map
        _handlerMap = _commandHandlers.ToDictionary(
            h => h.Command.ToLowerInvariant(),
            h => h);

        _logger.LogInformation(
            "CommandRouter initialized with {HandlerCount} command handlers: {Commands}",
            _handlerMap.Count,
            string.Join(", ", _handlerMap.Keys));
    }

    /// <summary>
    /// Routes a message containing a command to the appropriate handler.
    /// </summary>
    /// <param name="message">The message containing the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if command was handled, false if no handler was found.</returns>
    public async Task<bool> RouteCommandAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message?.Text == null || !message.Text.StartsWith('/'))
        {
            _logger.LogDebug("Message is not a command");
            return false;
        }

        try
        {
            // Parse command from message
            var commandText = ParseCommand(message.Text);

            if (string.IsNullOrWhiteSpace(commandText))
            {
                _logger.LogWarning("Failed to parse command from text: {Text}", message.Text);
                return false;
            }

            _logger.LogInformation(
                "Routing command '{Command}' from user {TelegramId} in chat {ChatId}",
                commandText,
                message.From?.Id,
                message.Chat.Id);

            // Find handler for command
            if (_handlerMap.TryGetValue(commandText, out var handler))
            {
                _logger.LogDebug("Found handler for command '{Command}'", commandText);

                // Execute handler
                await handler.HandleAsync(message, cancellationToken);

                _logger.LogInformation(
                    "Command '{Command}' handled successfully for user {TelegramId}",
                    commandText,
                    message.From?.Id);

                return true;
            }

            // No handler found - handle unknown command
            _logger.LogWarning(
                "No handler found for command '{Command}' from user {TelegramId}",
                commandText,
                message.From?.Id);

            await HandleUnknownCommandAsync(message, commandText, cancellationToken);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error routing command from user {TelegramId} in chat {ChatId}",
                message.From?.Id,
                message.Chat.Id);

            // Send error message to user
            await SendErrorMessageAsync(message.Chat.Id, cancellationToken);

            return false;
        }
    }

    /// <summary>
    /// Gets all registered command handlers.
    /// </summary>
    /// <returns>Collection of command handlers.</returns>
    public IEnumerable<ICommandHandler> GetAllHandlers()
    {
        return _commandHandlers;
    }

    /// <summary>
    /// Checks if a command is registered.
    /// </summary>
    /// <param name="command">The command name (without /).</param>
    /// <returns>True if command is registered.</returns>
    public bool IsCommandRegistered(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        return _handlerMap.ContainsKey(command.ToLowerInvariant());
    }

    private static string ParseCommand(string text)
    {
        // Remove leading /
        var commandText = text.TrimStart('/');

        // Handle commands with bot username (e.g., /start@botname)
        var atIndex = commandText.IndexOf('@');
        if (atIndex > 0)
        {
            commandText = commandText.Substring(0, atIndex);
        }

        // Handle commands with parameters (e.g., /start param1 param2)
        var spaceIndex = commandText.IndexOf(' ');
        if (spaceIndex > 0)
        {
            commandText = commandText.Substring(0, spaceIndex);
        }

        return commandText.ToLowerInvariant().Trim();
    }

    private async Task HandleUnknownCommandAsync(
        Message message,
        string commandText,
        CancellationToken cancellationToken)
    {
        var errorMessage = $"Unknown command: /{commandText}\n\n" +
                          "Use /help to see all available commands.";

        try
        {
            await _botService.Client.SendMessage(
                chatId: message.Chat.Id,
                text: errorMessage,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Unknown command message sent to user {TelegramId} in chat {ChatId}",
                message.From?.Id,
                message.Chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send unknown command message to chat {ChatId}",
                message.Chat.Id);
        }
    }

    private async Task SendErrorMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        var errorMessage = "Sorry, an error occurred while processing your command. " +
                          "Please try again later or use /help for assistance.";

        try
        {
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: errorMessage,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send error message to chat {ChatId}",
                chatId);
        }
    }
}
