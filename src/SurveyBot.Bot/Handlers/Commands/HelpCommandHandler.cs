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
    private readonly ILogger<HelpCommandHandler> _logger;

    public string Command => "help";

    public HelpCommandHandler(
        IBotService botService,
        ILogger<HelpCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
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

        // Hardcoded list of commands to avoid circular dependency
        helpText += "/start - Start the bot and get welcome message\n";
        helpText += "/mysurveys - View and manage your surveys\n";
        helpText += "/preview - Preview a survey with media info\n";
        helpText += "/surveys - Browse available surveys\n";
        helpText += "/survey - Take a specific survey\n";
        helpText += "/create - Create a new survey\n";
        helpText += "/list - List all your surveys\n";
        helpText += "/activate - Activate a survey\n";
        helpText += "/deactivate - Deactivate a survey\n";
        helpText += "/stats - View survey statistics\n";
        helpText += "/help - Show this help message\n\n";

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
