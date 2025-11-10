using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles the /cancel command.
/// Allows users to stop taking a survey and return to idle state.
/// Shows confirmation dialog before cancelling.
/// </summary>
public class CancelCommandHandler : ICommandHandler
{
    private readonly IBotService _botService;
    private readonly IConversationStateManager _stateManager;
    private readonly ILogger<CancelCommandHandler> _logger;

    public string Command => "cancel";

    public CancelCommandHandler(
        IBotService botService,
        IConversationStateManager stateManager,
        ILogger<CancelCommandHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null)
        {
            _logger.LogWarning("Received /cancel command with null From user");
            return;
        }

        var userId = message.From.Id;
        var chatId = message.Chat.Id;

        try
        {
            _logger.LogInformation(
                "Processing /cancel command from user {TelegramId} (@{Username})",
                userId,
                message.From.Username ?? "no_username");

            // Get current conversation state
            var state = await _stateManager.GetStateAsync(userId);

            if (state == null || state.CurrentSurveyId == null)
            {
                // User is not in a survey
                await SendNotInSurveyMessageAsync(chatId, cancellationToken);
                return;
            }

            // User is in a survey - ask for confirmation
            await SendConfirmationMessageAsync(chatId, state, cancellationToken);

            _logger.LogInformation(
                "Cancel confirmation sent to user {TelegramId} for survey {SurveyId}",
                userId,
                state.CurrentSurveyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing /cancel command for user {TelegramId}",
                userId);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: "Sorry, an error occurred while processing your request. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    public string GetDescription()
    {
        return "Cancel current survey and return to main menu";
    }

    #region Private Methods

    /// <summary>
    /// Sends message when user is not in a survey
    /// </summary>
    private async Task SendNotInSurveyMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        await _botService.Client.SendMessage(
            chatId: chatId,
            text: "You are not currently taking a survey.\n\n" +
                  "Use /surveys to find available surveys.",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends confirmation message with inline keyboard
    /// </summary>
    private async Task SendConfirmationMessageAsync(
        long chatId,
        ConversationState state,
        CancellationToken cancellationToken)
    {
        var progressInfo = state.TotalQuestions.HasValue
            ? $"You have answered {state.AnsweredCount} of {state.TotalQuestions} questions."
            : "You have started this survey.";

        var message = "Are you sure you want to cancel this survey?\n\n" +
                     $"{progressInfo}\n\n" +
                     "Your progress will be lost and the incomplete response will be deleted.";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Yes, Cancel Survey", "cancel_confirm"),
                InlineKeyboardButton.WithCallbackData("No, Continue Survey", "cancel_dismiss")
            }
        });

        await _botService.Client.SendMessage(
            chatId: chatId,
            text: message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    #endregion
}
