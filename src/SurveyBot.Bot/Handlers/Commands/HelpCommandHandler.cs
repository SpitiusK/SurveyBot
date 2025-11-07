using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /help command.
/// Displays list of available commands and usage instructions.
/// </summary>
public class HelpCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IEnumerable<ICommandHandler> _commandHandlers;
    private readonly ILogger<HelpCommandHandler> _logger;

    public string Command => "help";

    public HelpCommandHandler(
        IBotService botService,
        IEnumerable<ICommandHandler> commandHandlers,
        ILogger<HelpCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _commandHandlers = commandHandlers ?? throw new ArgumentNullException(nameof(commandHandlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /help command with null From user");
            return;
        }

        var chatId = message.Chat.Id;
        var telegramId = message.From.Id;

        try
        {
            _logger.LogInformation(
                "Processing /help command from user {TelegramId}",
                telegramId);

            // Build help message with all available commands
            var helpMessage = BuildHelpMessage();

            // Send help message
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: helpMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Help message sent to user {TelegramId} in chat {ChatId}",
                telegramId,
                chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /help command for user {TelegramId}",
                telegramId);

            // Send error message to user
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while processing your request. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Show available commands and usage instructions";
    }

    private string BuildHelpMessage()
    {
        var helpText = "*SurveyBot - Available Commands*\n\n";
        helpText += "Here are all the commands you can use:\n\n";

        // Get all command handlers except this one
        var handlers = _commandHandlers
            .Where(h => h.Command != Command)
            .OrderBy(h => h.Command)
            .ToList();

        foreach (var handler in handlers)
        {
            helpText += $"/{handler.Command} - {handler.GetDescription()}\n";
        }

        // Add this command last
        helpText += $"/{Command} - {GetDescription()}\n\n";

        // Add usage instructions
        helpText += "*How to use SurveyBot:*\n\n";
        helpText += "1. *Create Surveys*\n";
        helpText += "   Use /mysurveys to manage your surveys. You can create new surveys, edit existing ones, and view results.\n\n";

        helpText += "2. *Take Surveys*\n";
        helpText += "   When someone shares a survey link with you, simply click it to start taking the survey.\n\n";

        helpText += "3. *View Results*\n";
        helpText += "   Survey creators can view detailed statistics and responses for their surveys.\n\n";

        helpText += "*Need more help?*\n";
        helpText += "Contact support or visit our documentation for detailed guides.\n\n";

        helpText += "_SurveyBot - Making surveys simple and fun!_";

        return helpText;
    }
}
