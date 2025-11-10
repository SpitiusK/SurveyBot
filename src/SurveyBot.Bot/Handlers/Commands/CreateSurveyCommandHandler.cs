using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /createsurvey command.
/// Creates a new survey with basic information (title and description).
/// For MVP, questions are added via web admin panel.
/// </summary>
public class CreateSurveyCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly IUserRepository _userRepository;
    private readonly ISurveyService _surveyService;
    private readonly IConversationStateManager _stateManager;
    private readonly ILogger<CreateSurveyCommandHandler> _logger;

    public string Command => "createsurvey";

    public CreateSurveyCommandHandler(
        IBotService botService,
        IAdminAuthService adminAuthService,
        IUserRepository userRepository,
        ISurveyService surveyService,
        IConversationStateManager stateManager,
        ILogger<CreateSurveyCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _adminAuthService = adminAuthService ?? throw new ArgumentNullException(nameof(adminAuthService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /createsurvey command with null From user");
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
                    "Unauthorized /createsurvey attempt by user {TelegramUserId}",
                    telegramUserId);

                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "This command requires admin privileges. Contact the bot administrator for access.",
                    cancellationToken: cancellationToken);

                return;
            }

            _logger.LogInformation(
                "Processing /createsurvey command from admin user {TelegramUserId}",
                telegramUserId);

            // Parse arguments - check if title and description provided in command
            var commandText = message.Text ?? string.Empty;
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                // No arguments - show interactive flow instructions
                await ShowCreateSurveyInstructions(chatId, cancellationToken);
                return;
            }

            // Extract title and description from command
            // Format: /createsurvey Survey Title | Optional description
            var fullText = commandText.Substring(parts[0].Length).Trim();
            var titleParts = fullText.Split('|', 2, StringSplitOptions.TrimEntries);

            var title = titleParts[0];
            var description = titleParts.Length > 1 ? titleParts[1] : null;

            if (string.IsNullOrWhiteSpace(title))
            {
                await _botService.Client.SendMessage(
                    chatId: chatId,
                    text: "Survey title cannot be empty. Use:\n/createsurvey Survey Title | Optional description",
                    cancellationToken: cancellationToken);
                return;
            }

            // Get or create user
            var user = await _userRepository.GetByTelegramIdAsync(telegramUserId);
            if (user == null)
            {
                user = await _userRepository.CreateOrUpdateAsync(
                    telegramUserId,
                    message.From.Username,
                    message.From.FirstName,
                    message.From.LastName);
            }

            // Create survey
            var createDto = new CreateSurveyDto
            {
                Title = title,
                Description = description,
                AllowMultipleResponses = false,
                ShowResults = true
            };

            var survey = await _surveyService.CreateSurveyAsync(user.Id, createDto);

            _logger.LogInformation(
                "Survey {SurveyId} created by admin user {TelegramUserId}: {Title}",
                survey.Id,
                telegramUserId,
                survey.Title);

            // Send success message with survey code
            var successMessage = $"Survey created successfully!\n\n" +
                                $"Title: {survey.Title}\n" +
                                $"Survey ID: {survey.Id}\n" +
                                $"Status: Inactive (add questions to activate)\n\n" +
                                $"Next steps:\n" +
                                $"1. Add questions using the web admin panel\n" +
                                $"2. Activate survey: /activate {survey.Id}\n" +
                                $"3. Share with respondents: /surveys\n\n" +
                                $"View details: /stats {survey.Id}";

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: successMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /createsurvey command for user {TelegramUserId}",
                telegramUserId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while creating the survey. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Create a new survey (Admin only)";
    }

    private async Task ShowCreateSurveyInstructions(long chatId, CancellationToken cancellationToken)
    {
        var instructions = "Create a new survey using:\n\n" +
                          "/createsurvey Survey Title | Optional description\n\n" +
                          "Examples:\n" +
                          "/createsurvey Customer Feedback\n" +
                          "/createsurvey Product Survey | Help us improve our product\n\n" +
                          "After creation, add questions via the web admin panel, then activate with /activate.";

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: instructions,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
