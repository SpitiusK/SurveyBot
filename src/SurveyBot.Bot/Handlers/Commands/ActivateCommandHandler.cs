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
/// Handles the /activate command.
/// Activates a survey to accept responses (Admin only).
/// </summary>
public class ActivateCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyService _surveyService;
    private readonly ILogger<ActivateCommandHandler> _logger;

    public string Command => "activate";

    public ActivateCommandHandler(
        IBotService botService,
        IAdminAuthService adminAuthService,
        IUserRepository userRepository,
        ISurveyService surveyService,
        ILogger<ActivateCommandHandler> logger)
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
            _logger.LogWarning("Received /activate command with null From user");
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
                    "Unauthorized /activate attempt by user {TelegramUserId}",
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
                    text: "Usage: /activate <survey_id>\n\nExample: /activate 123",
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
                "Processing /activate command from admin user {TelegramUserId} for survey {SurveyId}",
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

            // Activate survey
            var survey = await _surveyService.ActivateSurveyAsync(surveyId, user.Id);

            _logger.LogInformation(
                "Survey {SurveyId} activated by admin user {TelegramUserId}",
                surveyId,
                telegramUserId);

            var successMessage = $"✅ Survey activated successfully!\n\n" +
                                $"*{survey.Title}*\n" +
                                $"ID: {survey.Id}\n" +
                                $"Questions: {survey.Questions?.Count ?? 0}\n" +
                                $"Status: Active\n\n" +
                                $"Users can now respond to this survey.\n" +
                                $"View stats: /stats {survey.Id}";

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
                "Survey {SurveyId} not found for activation by user {TelegramUserId}",
                ex.Message,
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: $"Survey not found. Please check the survey ID and try again.",
                cancellationToken: cancellationToken);
        }
        catch (SurveyValidationException ex)
        {
            _logger.LogWarning(
                ex,
                "Survey validation failed for activation by user {TelegramUserId}: {Message}",
                telegramUserId,
                ex.Message);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: $"Cannot activate survey: {ex.Message}\n\nPlease add questions before activating.",
                cancellationToken: cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized activation attempt by user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "You don't have permission to activate this survey.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /activate command for user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while activating the survey. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Activate a survey to accept responses (Admin only)";
    }
}
