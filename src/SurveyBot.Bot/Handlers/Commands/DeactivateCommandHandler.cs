using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UnauthorizedAccessException = SurveyBot.Core.Exceptions.UnauthorizedAccessException;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /deactivate command.
/// Deactivates a survey to stop accepting new responses (Admin only).
/// </summary>
public class DeactivateCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyService _surveyService;
    private readonly ILogger<DeactivateCommandHandler> _logger;

    public string Command => "deactivate";

    public DeactivateCommandHandler(
        IBotService botService,
        IAdminAuthService adminAuthService,
        IUserRepository userRepository,
        ISurveyService surveyService,
        ILogger<DeactivateCommandHandler> logger)
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
            _logger.LogWarning("Received /deactivate command with null From user");
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
                    "Unauthorized /deactivate attempt by user {TelegramUserId}",
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
                    text: "Usage: /deactivate <survey_id>\n\nExample: /deactivate 123",
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
                "Processing /deactivate command from admin user {TelegramUserId} for survey {SurveyId}",
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

            // Deactivate survey
            var survey = await _surveyService.DeactivateSurveyAsync(surveyId, user.Id);

            _logger.LogInformation(
                "Survey {SurveyId} deactivated by admin user {TelegramUserId}",
                surveyId,
                telegramUserId);

            var successMessage = $"⏸ Survey deactivated successfully!\n\n" +
                                $"*{survey.Title}*\n" +
                                $"ID: {survey.Id}\n" +
                                $"Status: Inactive\n\n" +
                                $"No new responses will be accepted. Existing responses are preserved.\n" +
                                $"Reactivate: /activate {survey.Id}";

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: successMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Survey not found for deactivation by user {TelegramUserId}",
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
                "Unauthorized deactivation attempt by user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "You don't have permission to deactivate this survey.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /deactivate command for user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while deactivating the survey. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Deactivate a survey to stop accepting responses (Admin only)";
    }
}
