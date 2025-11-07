using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /mysurveys command.
/// Displays user's created surveys with status and response counts.
/// </summary>
public class MySurveysCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyRepository _surveyRepository;
    private readonly ILogger<MySurveysCommandHandler> _logger;

    public string Command => "mysurveys";

    public MySurveysCommandHandler(
        IBotService botService,
        IUserRepository userRepository,
        ISurveyRepository surveyRepository,
        ILogger<MySurveysCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _surveyRepository = surveyRepository ?? throw new ArgumentNullException(nameof(surveyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /mysurveys command with null From user");
            return;
        }

        var telegramId = message.From.Id;
        var chatId = message.Chat.Id;

        try
        {
            _logger.LogInformation(
                "Processing /mysurveys command from user {TelegramId}",
                telegramId);

            // Get user from database
            var user = await _userRepository.GetByTelegramIdAsync(telegramId);

            if (user == null)
            {
                _logger.LogWarning(
                    "User with TelegramId {TelegramId} not found in database",
                    telegramId);

                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "You are not registered yet. Please use /start to register.",
                    cancellationToken: cancellationToken);

                return;
            }

            // Get user's surveys
            var surveys = (await _surveyRepository.GetByCreatorIdAsync(user.Id)).ToList();

            _logger.LogInformation(
                "Found {SurveyCount} surveys for user {UserId}",
                surveys.Count,
                user.Id);

            // Build and send message
            if (surveys.Count == 0)
            {
                await SendNoSurveysMessage(chatId, cancellationToken);
            }
            else
            {
                await SendSurveysListMessage(chatId, surveys, cancellationToken);
            }

            _logger.LogInformation(
                "Survey list sent to user {TelegramId} in chat {ChatId}",
                telegramId,
                chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /mysurveys command for user {TelegramId}",
                telegramId);

            // Send error message to user
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while retrieving your surveys. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "View and manage your surveys";
    }

    private async Task SendNoSurveysMessage(long chatId, CancellationToken cancellationToken)
    {
        var message = "*My Surveys*\n\n" +
                     "You haven't created any surveys yet.\n\n" +
                     "Ready to create your first survey? " +
                     "Use the button below or create one through the web interface.";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Create Survey", "action:create_survey")
            }
        });

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task SendSurveysListMessage(
        long chatId,
        List<Core.Entities.Survey> surveys,
        CancellationToken cancellationToken)
    {
        var message = "*My Surveys*\n\n";
        message += $"You have created {surveys.Count} {(surveys.Count == 1 ? "survey" : "surveys")}:\n\n";

        // Get response counts for all surveys
        var surveyDetails = new List<(Core.Entities.Survey survey, int responseCount)>();

        foreach (var survey in surveys)
        {
            var responseCount = await _surveyRepository.GetResponseCountAsync(survey.Id);
            surveyDetails.Add((survey, responseCount));
        }

        // Order by creation date (newest first)
        surveyDetails = surveyDetails
            .OrderByDescending(x => x.survey.CreatedAt)
            .ToList();

        // Build survey list
        for (int i = 0; i < surveyDetails.Count; i++)
        {
            var (survey, responseCount) = surveyDetails[i];
            var statusEmoji = survey.IsActive ? "✅" : "🔴";
            var status = survey.IsActive ? "Active" : "Inactive";

            message += $"{i + 1}. *{EscapeMarkdown(survey.Title)}*\n";
            message += $"   {statusEmoji} Status: {status}\n";
            message += $"   📊 Responses: {responseCount}\n";
            message += $"   📅 Created: {survey.CreatedAt:MMM dd, yyyy}\n\n";
        }

        message += "_Use the buttons below to manage your surveys._";

        // Create inline keyboard for quick actions
        var keyboard = BuildSurveyActionsKeyboard(surveyDetails);

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private static InlineKeyboardMarkup BuildSurveyActionsKeyboard(
        List<(Core.Entities.Survey survey, int responseCount)> surveyDetails)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Add buttons for first 5 surveys (to avoid keyboard size limits)
        var surveysToShow = surveyDetails.Take(5);

        foreach (var (survey, _) in surveysToShow)
        {
            var buttonText = $"📋 {(survey.Title.Length > 25 ? survey.Title.Substring(0, 22) + "..." : survey.Title)}";
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(buttonText, $"survey:view:{survey.Id}"),
                InlineKeyboardButton.WithCallbackData(
                    survey.IsActive ? "⏸️" : "▶️",
                    $"survey:toggle:{survey.Id}")
            });
        }

        // Add "Create New" button
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("➕ Create New Survey", "action:create_survey")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    private static string EscapeMarkdown(string text)
    {
        // Escape special Markdown characters
        var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var c in specialChars)
        {
            text = text.Replace(c.ToString(), $"\\{c}");
        }
        return text;
    }
}
