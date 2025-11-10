using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UnauthorizedAccessException = System.UnauthorizedAccessException;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /stats command.
/// Displays quick statistics for a survey (Admin only).
/// </summary>
public class StatsCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyService _surveyService;
    private readonly ILogger<StatsCommandHandler> _logger;

    public string Command => "stats";

    public StatsCommandHandler(
        IBotService botService,
        IAdminAuthService adminAuthService,
        IUserRepository userRepository,
        ISurveyService surveyService,
        ILogger<StatsCommandHandler> logger)
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
            _logger.LogWarning("Received /stats command with null From user");
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
                    "Unauthorized /stats attempt by user {TelegramUserId}",
                    telegramUserId);

                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "This command requires admin privileges. Contact the bot administrator for access.",
                    cancellationToken: cancellationToken);

                return;
            }

            // Parse survey ID from command
            var commandText = message.Text ?? string.Empty;
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Usage: /stats <survey_id>\n\nExample: /stats 123",
                    cancellationToken: cancellationToken);
                return;
            }

            if (!int.TryParse(parts[1], out var surveyId) || surveyId <= 0)
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Invalid survey ID. Please provide a valid number.",
                    cancellationToken: cancellationToken);
                return;
            }

            _logger.LogInformation(
                "Processing /stats command from admin user {TelegramUserId} for survey {SurveyId}",
                telegramUserId,
                surveyId);

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

            // Get survey statistics
            var stats = await _surveyService.GetSurveyStatisticsAsync(surveyId, user.Id);

            _logger.LogInformation(
                "Retrieved statistics for survey {SurveyId} for admin user {TelegramUserId}",
                surveyId,
                telegramUserId);

            // Build statistics message
            var statusEmoji = stats.IsActive ? "✅" : "⏸";
            var statusText = stats.IsActive ? "Active" : "Inactive";

            var completionRate = stats.TotalResponses > 0
                ? (stats.CompletedResponses * 100.0 / stats.TotalResponses)
                : 0;

            var message = $"📊 *Survey Statistics*\n\n" +
                         $"*{stats.Title}*\n" +
                         $"ID: {stats.Id}\n" +
                         $"Status: {statusEmoji} {statusText}\n\n" +
                         $"📈 *Response Stats:*\n" +
                         $"Total Responses: {stats.TotalResponses}\n" +
                         $"Completed: {stats.CompletedResponses}\n" +
                         $"In Progress: {stats.TotalResponses - stats.CompletedResponses}\n" +
                         $"Completion Rate: {completionRate:F1}%\n\n" +
                         $"❓ *Questions:*\n" +
                         $"Total Questions: {stats.TotalQuestions}\n\n";

            // Add top 3 questions by response count
            if (stats.QuestionStats != null && stats.QuestionStats.Any())
            {
                message += "📋 *Top Questions by Responses:*\n";

                var topQuestions = stats.QuestionStats
                    .OrderByDescending(q => q.TotalAnswers)
                    .Take(3);

                var index = 1;
                foreach (var question in topQuestions)
                {
                    var questionPreview = question.QuestionText.Length > 50
                        ? question.QuestionText.Substring(0, 47) + "..."
                        : question.QuestionText;

                    message += $"{index}. {questionPreview}\n";
                    message += $"   Answers: {question.TotalAnswers}\n";
                    index++;
                }

                message += "\n";
            }

            message += $"📅 Created: {stats.CreatedAt:yyyy-MM-dd HH:mm}\n";

            if (stats.TotalResponses == 0)
            {
                message += "\n💡 *Tip:* Share this survey to start collecting responses!";
            }

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Survey not found for stats by user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: $"Survey not found. Please check the survey ID and try again.",
                cancellationToken: cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized stats access attempt by user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "You don't have permission to view statistics for this survey.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /stats command for user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while fetching statistics. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "View survey statistics (Admin only)";
    }
}
