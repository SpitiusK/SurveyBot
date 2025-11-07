using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Core.Exceptions;
using SurveyBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Handlers.Commands;

/// <summary>
/// Handles survey completion flow when user finishes all questions.
/// Marks response as complete, displays thank you message, and offers to take another survey.
/// </summary>
public class CompletionHandler
{
    private readonly IBotService _botService;
    private readonly IResponseService _responseService;
    private readonly ISurveyRepository _surveyRepository;
    private readonly IConversationStateManager _stateManager;
    private readonly ILogger<CompletionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CompletionHandler.
    /// </summary>
    public CompletionHandler(
        IBotService botService,
        IResponseService responseService,
        ISurveyRepository surveyRepository,
        IConversationStateManager stateManager,
        ILogger<CompletionHandler> logger)
    {
        _botService = botService;
        _responseService = responseService;
        _surveyRepository = surveyRepository;
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Handles survey completion when user completes all questions.
    /// Marks response as complete, displays completion message, and offers next actions.
    /// </summary>
    /// <param name="chatId">Telegram chat ID</param>
    /// <param name="userId">Telegram user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HandleCompletionAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling survey completion for user {UserId}", userId);

            // Get current conversation state
            var state = await _stateManager.GetStateAsync(userId);
            if (state == null)
            {
                _logger.LogWarning("No conversation state found for user {UserId}", userId);
                await SendErrorMessageAsync(
                    chatId,
                    "Session expired. Please start a new survey with /surveys",
                    cancellationToken);
                return;
            }

            if (!state.CurrentResponseId.HasValue)
            {
                _logger.LogWarning("No active response for user {UserId}", userId);
                await SendErrorMessageAsync(
                    chatId,
                    "No active survey response found. Please start a new survey.",
                    cancellationToken);
                return;
            }

            // Mark response as complete via API
            var completedResponse = await _responseService.CompleteResponseAsync(
                state.CurrentResponseId.Value,
                userId: null);

            _logger.LogInformation(
                "Response {ResponseId} marked complete for user {UserId}",
                completedResponse.Id,
                userId);

            // Update conversation state to completed
            var transitioned = await _stateManager.CompleteSurveyAsync(userId);
            if (!transitioned)
            {
                _logger.LogWarning("Failed to transition state to completed for user {UserId}", userId);
            }

            // Send completion message with survey title
            var surveyTitle = state.CurrentSurveyId.HasValue
                ? await GetSurveyTitleAsync(state.CurrentSurveyId.Value)
                : "Survey";

            var completionMessage = GetCompletionMessage(
                surveyTitle,
                completedResponse.AnsweredCount,
                completedResponse.TotalQuestions);

            await _botService.Client.SendMessage(
                chatId: chatId,
                text: completionMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: GetCompletionKeyboard(),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Sent completion message for response {ResponseId}",
                state.CurrentResponseId.Value);
        }
        catch (ResponseNotFoundException ex)
        {
            _logger.LogError(ex, "Response not found for user {UserId}", userId);
            await SendErrorMessageAsync(
                chatId,
                "Error: Survey response not found. Please try again.",
                cancellationToken);
        }
        catch (SurveyNotFoundException ex)
        {
            _logger.LogError(ex, "Survey not found for user {UserId}", userId);
            await SendErrorMessageAsync(
                chatId,
                "Error: Survey not found. Please start a new survey.",
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during completion for user {UserId}", userId);
            await SendErrorMessageAsync(
                chatId,
                "Error: Cannot complete survey. Please try again.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling completion for user {UserId}", userId);
            await SendErrorMessageAsync(
                chatId,
                "An unexpected error occurred. Please try again.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Gets the survey title by ID.
    /// </summary>
    private async Task<string> GetSurveyTitleAsync(int surveyId)
    {
        try
        {
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            return survey?.Title ?? "Survey";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve survey {SurveyId} title", surveyId);
            return "Survey";
        }
    }

    /// <summary>
    /// Builds the completion message with survey details.
    /// </summary>
    private string GetCompletionMessage(string surveyTitle, int answeredCount, int totalQuestions)
    {
        return $@"✅ *Thank You!*

Your response to ""{surveyTitle}"" has been submitted successfully!

📊 *Summary:*
• Questions Answered: *{answeredCount} / {totalQuestions}*
• Status: *Complete*

What would you like to do next?";
    }

    /// <summary>
    /// Gets the keyboard with post-completion options.
    /// </summary>
    private InlineKeyboardMarkup GetCompletionKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "📋 Take Another Survey",
                    "surveys_main")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "📊 View My Surveys",
                    "mysurveys_list"),
                InlineKeyboardButton.WithCallbackData(
                    "🏠 Main Menu",
                    "menu_main")
            }
        });
    }

    /// <summary>
    /// Sends an error message to the user.
    /// </summary>
    private async Task SendErrorMessageAsync(
        long chatId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _botService.Client.SendMessage(
                chatId: chatId,
                text: $"❌ {errorMessage}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error message to chat {ChatId}", chatId);
        }
    }
}
