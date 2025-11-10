using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /adminhelp command.
/// Shows all available admin commands (Admin only).
/// </summary>
public class AdminHelpCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly ILogger<AdminHelpCommandHandler> _logger;

    public string Command => "adminhelp";

    public AdminHelpCommandHandler(
        IBotService botService,
        IAdminAuthService adminAuthService,
        ILogger<AdminHelpCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _adminAuthService = adminAuthService ?? throw new ArgumentNullException(nameof(adminAuthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /adminhelp command with null From user");
            return;
        }

        var telegramUserId = message.From.Id;
        var chatId = message.Chat.Id;

        try
        {
            // Check admin authorization
            if (!_adminAuthService.IsAdmin(telegramUserId))
            {
                _logger.LogWarning(
                    "Unauthorized /adminhelp attempt by user {TelegramUserId}",
                    telegramUserId);

                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "This command requires admin privileges. Contact the bot administrator for access.",
                    cancellationToken: cancellationToken);

                return;
            }

            _logger.LogInformation(
                "Processing /adminhelp command from admin user {TelegramUserId}",
                telegramUserId);

            var helpMessage = BuildAdminHelpMessage();

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: helpMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /adminhelp command for user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while displaying help. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Show admin commands help (Admin only)";
    }

    private string BuildAdminHelpMessage()
    {
        return @"*Admin Commands Help*

*Survey Management:*
/createsurvey Title | Description
  Create a new survey
  Example: `/createsurvey Customer Feedback | Help us improve`

/listsurveys [page]
  List all your surveys with pagination
  Example: `/listsurveys` or `/listsurveys 2`

/activate <survey_id>
  Activate a survey to accept responses
  Example: `/activate 123`

/deactivate <survey_id>
  Deactivate a survey to stop accepting responses
  Example: `/deactivate 123`

/stats <survey_id>
  View detailed statistics for a survey
  Example: `/stats 123`

*General Commands:*
/start
  Start the bot and display main menu

/help
  Show general help and available commands

/surveys
  Browse and take available surveys

/mysurveys
  View surveys you've created

/adminhelp
  Show this admin help message

*Workflow:*
1. Create survey: `/createsurvey My Survey | Description`
2. Add questions via web admin panel
3. Activate: `/activate <id>`
4. Monitor: `/stats <id>`
5. Deactivate when done: `/deactivate <id>`

*Tips:*
- Survey must have questions before activation
- Use `/listsurveys` to see all your surveys
- Statistics update in real-time
- Deactivated surveys preserve all responses";
    }
}
