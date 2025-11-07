using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /start command.
/// Welcomes new users, registers them in the system, and displays main menu.
/// </summary>
public class StartCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<StartCommandHandler> _logger;

    public string Command => "start";

    public StartCommandHandler(
        IBotService botService,
        IUserRepository userRepository,
        ILogger<StartCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /start command with null From user");
            return;
        }

        var telegramUser = message.From;
        var chatId = message.Chat.Id;

        try
        {
            _logger.LogInformation(
                "Processing /start command from user {TelegramId} (@{Username})",
                telegramUser.Id,
                telegramUser.Username ?? "no_username");

            // Register or update user in database
            var user = await _userRepository.CreateOrUpdateAsync(
                telegramUser.Id,
                telegramUser.Username,
                telegramUser.FirstName,
                telegramUser.LastName);

            _logger.LogInformation(
                "User {UserId} registered/updated successfully. TelegramId: {TelegramId}",
                user.Id,
                user.TelegramId);

            // Determine if this is a new user
            var isNewUser = user.CreatedAt == user.UpdatedAt;

            // Build welcome message
            var welcomeMessage = BuildWelcomeMessage(telegramUser.FirstName, isNewUser);

            // Create main menu keyboard
            var keyboard = BuildMainMenuKeyboard();

            // Send welcome message with keyboard
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Welcome message sent to user {TelegramId} in chat {ChatId}",
                telegramUser.Id,
                chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /start command for user {TelegramId}",
                telegramUser.Id);

            // Send error message to user
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while processing your request. Please try again later.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Start the bot and display main menu";
    }

    private static string BuildWelcomeMessage(string? firstName, bool isNewUser)
    {
        var greeting = !string.IsNullOrWhiteSpace(firstName) ? $"Hello, {firstName}" : "Hello";

        if (isNewUser)
        {
            return $"{greeting}!\n\n" +
                   "Welcome to SurveyBot! I can help you create and manage surveys, " +
                   "or participate in surveys created by others.\n\n" +
                   "*What would you like to do?*\n" +
                   "- Create and manage your own surveys\n" +
                   "- Take surveys shared by others\n" +
                   "- View results and statistics\n\n" +
                   "Use the buttons below to get started, or type /help to see all available commands.";
        }

        return $"{greeting}!\n\n" +
               "Welcome back to SurveyBot!\n\n" +
               "*What would you like to do today?*\n" +
               "Use the buttons below or type /help for assistance.";
    }

    private static InlineKeyboardMarkup BuildMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Find Surveys", "cmd:surveys")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("My Surveys", "cmd:mysurveys"),
                InlineKeyboardButton.WithCallbackData("Help", "cmd:help")
            }
        });
    }
}
