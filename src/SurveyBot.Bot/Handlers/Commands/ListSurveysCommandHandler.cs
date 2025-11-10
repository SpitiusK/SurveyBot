using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /listsurveys command.
/// Shows paginated list of user's surveys with details (Admin only).
/// </summary>
public class ListSurveysCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyService _surveyService;
    private readonly ILogger<ListSurveysCommandHandler> _logger;

    private const int PageSize = 5;

    public string Command => "listsurveys";

    public ListSurveysCommandHandler(
        IBotService botService,
        IAdminAuthService adminAuthService,
        IUserRepository userRepository,
        ISurveyService surveyService,
        ILogger<ListSurveysCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _adminAuthService = adminAuthService ?? throw new ArgumentNullException(nameof(adminAuthService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /listsurveys command with null From user");
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
                    "Unauthorized /listsurveys attempt by user {TelegramUserId}",
                    telegramUserId);

                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "This command requires admin privileges. Contact the bot administrator for access.",
                    cancellationToken: cancellationToken);

                return;
            }

            _logger.LogInformation(
                "Processing /listsurveys command from admin user {TelegramUserId}",
                telegramUserId);

            // Parse page number from command (default: 1)
            var commandText = message.Text ?? string.Empty;
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var pageNumber = 1;

            if (parts.Length > 1 && int.TryParse(parts[1], out var page) && page > 0)
            {
                pageNumber = page;
            }

            // Get user
            var user = await _userRepository.GetByTelegramIdAsync(telegramUserId);
            if (user == null)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "User not found. Use /start to register first.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Fetch surveys with pagination
            var paginationQuery = new PaginationQueryDto
            {
                PageNumber = pageNumber,
                PageSize = PageSize,
                SortBy = "CreatedAt",
                SortDescending = true
            };

            var surveysResult = await _surveyService.GetAllSurveysAsync(user.Id, paginationQuery);

            if (surveysResult.TotalCount == 0)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "You haven't created any surveys yet.\n\nCreate one with: /createsurvey",
                    cancellationToken: cancellationToken);
                return;
            }

            // Build survey list message
            var messageText = BuildSurveyListMessage(surveysResult, pageNumber);

            // Build pagination keyboard
            var keyboard = BuildPaginationKeyboard(pageNumber, surveysResult.TotalPages);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Displayed {Count} surveys (page {Page}/{TotalPages}) to admin user {TelegramUserId}",
                surveysResult.Items.Count,
                pageNumber,
                surveysResult.TotalPages,
                telegramUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /listsurveys command for user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while fetching surveys. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "List your surveys with details (Admin only)";
    }

    private string BuildSurveyListMessage(PagedResultDto<Core.DTOs.Survey.SurveyListDto> result, int currentPage)
    {
        var message = $"*Your Surveys* ({result.TotalCount} total)\n";
        message += $"Page {currentPage} of {result.TotalPages}\n\n";

        var startNumber = (currentPage - 1) * PageSize + 1;

        foreach (var survey in result.Items)
        {
            var statusEmoji = survey.IsActive ? "✅" : "⏸";
            var statusText = survey.IsActive ? "Active" : "Inactive";

            message += $"{statusEmoji} *{survey.Title}*\n";
            message += $"   ID: {survey.Id}\n";
            message += $"   Status: {statusText}\n";
            message += $"   Questions: {survey.QuestionCount}\n";
            message += $"   Responses: {survey.TotalResponses}\n";
            message += $"   Created: {survey.CreatedAt:yyyy-MM-dd}\n";
            message += $"\n";

            startNumber++;
        }

        message += "\nCommands:\n";
        message += "`/activate <id>` - Activate survey\n";
        message += "`/deactivate <id>` - Deactivate survey\n";
        message += "`/stats <id>` - View statistics\n";

        return message;
    }

    private InlineKeyboardMarkup? BuildPaginationKeyboard(int currentPage, int totalPages)
    {
        if (totalPages <= 1)
        {
            return null; // No pagination needed
        }

        var buttons = new List<InlineKeyboardButton>();

        // Previous button
        if (currentPage > 1)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                "⬅ Previous",
                $"listsurveys:page:{currentPage - 1}"));
        }

        // Page indicator
        buttons.Add(InlineKeyboardButton.WithCallbackData(
            $"{currentPage}/{totalPages}",
            "listsurveys:noop"));

        // Next button
        if (currentPage < totalPages)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                "Next ➡",
                $"listsurveys:page:{currentPage + 1}"));
        }

        return new InlineKeyboardMarkup(new[] { buttons });
    }
}
