using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /surveys command.
/// Displays all active surveys available for users to take.
/// </summary>
public class SurveysCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly ISurveyRepository _surveyRepository;
    private readonly ILogger<SurveysCommandHandler> _logger;

    // Pagination settings
    private const int SurveysPerPage = 5;

    public string Command => "surveys";

    public SurveysCommandHandler(
        IBotService botService,
        ISurveyRepository surveyRepository,
        ILogger<SurveysCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _surveyRepository = surveyRepository ?? throw new ArgumentNullException(nameof(surveyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /surveys command with null From user");
            return;
        }

        var telegramId = message.From.Id;
        var chatId = message.Chat.Id;

        try
        {
            _logger.LogInformation(
                "Processing /surveys command from user {TelegramId}",
                telegramId);

            // Parse page number from command (default: 0)
            var parts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            var pageNumber = 0;

            if (parts.Length > 1 && int.TryParse(parts[1], out var page) && page >= 0)
            {
                pageNumber = page;
            }

            // Get all active surveys
            var activeSurveys = (await _surveyRepository.GetActiveSurveysAsync()).ToList();

            _logger.LogInformation(
                "Found {SurveyCount} active surveys",
                activeSurveys.Count);

            // Build and send message
            if (activeSurveys.Count == 0)
            {
                await SendNoSurveysMessage(chatId, cancellationToken);
            }
            else
            {
                await SendSurveysListMessage(chatId, activeSurveys, page: pageNumber, cancellationToken);
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
                "Error processing /surveys command for user {TelegramId}",
                telegramId);

            // Send error message to user
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while retrieving surveys. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Find and take available surveys";
    }

    private async Task SendNoSurveysMessage(long chatId, CancellationToken cancellationToken)
    {
        var message = "*Available Surveys*\n\n" +
                     "There are no active surveys available at the moment.\n\n" +
                     "Check back later or create your own survey!";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("My Surveys", "cmd:mysurveys"),
                InlineKeyboardButton.WithCallbackData("Help", "cmd:help")
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
        int page,
        CancellationToken cancellationToken)
    {
        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)surveys.Count / SurveysPerPage);
        var currentPage = Math.Max(0, Math.Min(page, totalPages - 1));
        var skip = currentPage * SurveysPerPage;
        var surveysOnPage = surveys
            .OrderByDescending(s => s.UpdatedAt)
            .Skip(skip)
            .Take(SurveysPerPage)
            .ToList();

        // Build message
        var message = "*Available Surveys*\n\n";
        message += $"Found {surveys.Count} active {(surveys.Count == 1 ? "survey" : "surveys")}";

        if (totalPages > 1)
        {
            message += $" (Page {currentPage + 1} of {totalPages})";
        }

        message += ":\n\n";

        // Get response counts for surveys on current page
        var surveyDetails = new List<(Core.Entities.Survey survey, int responseCount, int questionCount)>();

        foreach (var survey in surveysOnPage)
        {
            var responseCount = await _surveyRepository.GetResponseCountAsync(survey.Id);
            var questionCount = survey.Questions?.Count ?? 0;
            surveyDetails.Add((survey, responseCount, questionCount));
        }

        // Build survey list
        for (int i = 0; i < surveyDetails.Count; i++)
        {
            var (survey, responseCount, questionCount) = surveyDetails[i];
            var globalIndex = skip + i + 1;

            message += $"{globalIndex}. *{EscapeMarkdown(survey.Title)}*\n";

            if (!string.IsNullOrWhiteSpace(survey.Description))
            {
                var description = survey.Description.Length > 100
                    ? survey.Description.Substring(0, 97) + "..."
                    : survey.Description;
                message += $"   {EscapeMarkdown(description)}\n";
            }

            message += $"   Questions: {questionCount}\n";
            message += $"   Responses: {responseCount}\n";
            message += $"   Updated: {survey.UpdatedAt:MMM dd, yyyy}\n\n";
        }

        message += "_Click on a survey below to start taking it._";

        // Create inline keyboard with survey buttons
        var keyboard = BuildSurveyKeyboard(surveyDetails, currentPage, totalPages);

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private static InlineKeyboardMarkup BuildSurveyKeyboard(
        List<(Core.Entities.Survey survey, int responseCount, int questionCount)> surveyDetails,
        int currentPage,
        int totalPages)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Add buttons for each survey
        foreach (var (survey, _, _) in surveyDetails)
        {
            var buttonText = survey.Title.Length > 30
                ? survey.Title.Substring(0, 27) + "..."
                : survey.Title;

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"📋 {buttonText}",
                    $"survey:take:{survey.Code}")
            });
        }

        // Add pagination buttons if needed
        if (totalPages > 1)
        {
            var paginationButtons = new List<InlineKeyboardButton>();

            if (currentPage > 0)
            {
                paginationButtons.Add(
                    InlineKeyboardButton.WithCallbackData("◀️ Previous", $"surveys:page:{currentPage - 1}"));
            }

            // Page indicator
            paginationButtons.Add(
                InlineKeyboardButton.WithCallbackData($"{currentPage + 1}/{totalPages}", "surveys:noop"));

            if (currentPage < totalPages - 1)
            {
                paginationButtons.Add(
                    InlineKeyboardButton.WithCallbackData("Next ▶️", $"surveys:page:{currentPage + 1}"));
            }

            buttons.Add(paginationButtons.ToArray());
        }

        // Add bottom menu buttons
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Refresh", "cmd:surveys"),
            InlineKeyboardButton.WithCallbackData("📊 My Surveys", "cmd:mysurveys")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Escape special Markdown characters
        var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var c in specialChars)
        {
            text = text.Replace(c.ToString(), $"\\{c}");
        }
        return text;
    }
}
