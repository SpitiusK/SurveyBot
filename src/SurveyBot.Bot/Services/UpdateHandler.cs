using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SurveyBot.Bot.Handlers;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Handles incoming updates from Telegram.
/// Routes updates to appropriate handlers based on update type.
/// Includes performance monitoring to ensure < 2 second response times.
/// </summary>
public class UpdateHandler : IUpdateHandler
{
    private readonly IBotService _botService;
    private readonly CommandRouter _commandRouter;
    private readonly NavigationHandler _navigationHandler;
    private readonly CancelCallbackHandler _cancelCallbackHandler;
    private readonly SurveyResponseHandler _surveyResponseHandler;
    private readonly IConversationStateManager _stateManager;
    private readonly BotPerformanceMonitor _performanceMonitor;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        IBotService botService,
        CommandRouter commandRouter,
        NavigationHandler navigationHandler,
        CancelCallbackHandler cancelCallbackHandler,
        SurveyResponseHandler surveyResponseHandler,
        IConversationStateManager stateManager,
        BotPerformanceMonitor performanceMonitor,
        ILogger<UpdateHandler> logger)
    {
        _botService = botService ?? throw new ArgumentNullException(nameof(botService));
        _commandRouter = commandRouter ?? throw new ArgumentNullException(nameof(commandRouter));
        _navigationHandler = navigationHandler ?? throw new ArgumentNullException(nameof(navigationHandler));
        _cancelCallbackHandler = cancelCallbackHandler ?? throw new ArgumentNullException(nameof(cancelCallbackHandler));
        _surveyResponseHandler = surveyResponseHandler ?? throw new ArgumentNullException(nameof(surveyResponseHandler));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        await _performanceMonitor.TrackOperationAsync(
            "HandleUpdate",
            async () =>
            {
                try
                {
                    _logger.LogInformation(
                        "Received update {UpdateId} of type {UpdateType}",
                        update.Id,
                        update.Type);

                    var handled = update.Type switch
                    {
                        UpdateType.Message => await HandleMessageAsync(update.Message!, cancellationToken),
                        UpdateType.CallbackQuery => await HandleCallbackQueryAsync(update.CallbackQuery!, cancellationToken),
                        UpdateType.EditedMessage => await HandleEditedMessageAsync(update.EditedMessage!, cancellationToken),
                        _ => await HandleUnsupportedUpdateAsync(update, cancellationToken)
                    };

                    if (handled)
                    {
                        _logger.LogInformation("Update {UpdateId} handled successfully", update.Id);
                    }
                    else
                    {
                        _logger.LogDebug("Update {UpdateId} was not handled", update.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
                    await HandleErrorAsync(ex, cancellationToken);
                }
            },
            context: $"UpdateId={update.Id}, Type={update.Type}");
    }

    public async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, "Error in update handler: {ErrorMessage}", exception.Message);

        // Log specific error details
        if (exception is Telegram.Bot.Exceptions.ApiRequestException apiException)
        {
            _logger.LogError(
                "Telegram API Error: {ErrorCode} - {ErrorMessage}",
                apiException.ErrorCode,
                apiException.Message);
        }

        // In production, you might want to:
        // - Send notification to administrators
        // - Log to external error tracking service
        // - Store error details in database

        await Task.CompletedTask;
    }

    private async Task<bool> HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text == null)
        {
            _logger.LogDebug("Message has no text content, ignoring");
            return false;
        }

        _logger.LogInformation(
            "Processing message from user {TelegramId} in chat {ChatId}: {MessageText}",
            message.From?.Id,
            message.Chat.Id,
            message.Text.Length > 50 ? message.Text.Substring(0, 47) + "..." : message.Text);

        // Check if message is a command
        if (message.Text.StartsWith('/'))
        {
            return await _commandRouter.RouteCommandAsync(message, cancellationToken);
        }

        // Handle regular text messages
        return await HandleTextMessageAsync(message, cancellationToken);
    }

    private async Task<bool> HandleTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.From == null)
            return false;

        var userId = message.From.Id;

        _logger.LogDebug(
            "Received text message from user {TelegramId}: {Text}",
            userId,
            message.Text);

        // Check if user is in an active survey
        var state = await _stateManager.GetStateAsync(userId);

        // Enhanced logging to debug state issues
        if (state == null)
        {
            _logger.LogDebug("User {UserId} has no state", userId);
        }
        else
        {
            _logger.LogDebug(
                "User {UserId} state - SurveyId: {SurveyId}, QuestionIndex: {QuestionIndex}, State: {State}",
                userId,
                state.CurrentSurveyId,
                state.CurrentQuestionIndex,
                state.CurrentState);
        }

        if (state != null && state.CurrentSurveyId.HasValue && state.CurrentQuestionIndex.HasValue)
        {
            // User is in active survey - route to survey response handler
            _logger.LogInformation(
                "User {UserId} is in active survey {SurveyId}, routing to response handler",
                userId,
                state.CurrentSurveyId);
            return await _surveyResponseHandler.HandleMessageResponseAsync(message, cancellationToken);
        }

        // Not in a survey - send helpful message
        var helpText = "I understand commands like /start and /help. " +
                      "To interact with surveys, use the inline keyboard buttons.";

        try
        {
            await _botService.Client.SendMessage(
                chatId: message.Chat.Id,
                text: helpText,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send help text to chat {ChatId}", message.Chat.Id);
            return false;
        }
    }

    private async Task<bool> HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        return await _performanceMonitor.TrackOperationAsync(
            "HandleCallbackQuery",
            async () =>
            {
                if (callbackQuery.Data == null || callbackQuery.Message == null)
                {
                    _logger.LogWarning("Callback query has no data or message");
                    return false;
                }

                _logger.LogInformation(
                    "Processing callback query from user {TelegramId}: {CallbackData}",
                    callbackQuery.From.Id,
                    callbackQuery.Data);

                try
                {
                    // Check if callback is a navigation action (nav_back_q{id} or nav_skip_q{id})
                    if (callbackQuery.Data.StartsWith("nav_"))
                    {
                        return await HandleNavigationCallbackAsync(callbackQuery, cancellationToken);
                    }

                    // Check if callback is a cancel action (cancel_confirm or cancel_dismiss)
                    if (callbackQuery.Data.StartsWith("cancel_"))
                    {
                        return await HandleCancelCallbackAsync(callbackQuery, cancellationToken);
                    }

                    // Parse callback data
                    var parts = callbackQuery.Data.Split(':');
                    var action = parts.Length > 0 ? parts[0] : string.Empty;

                    // Route callback to appropriate handler
                    var handled = action switch
                    {
                        "cmd" => await HandleCallbackCommandAsync(callbackQuery, parts, cancellationToken),
                        "survey" => await HandleSurveyCallbackAsync(callbackQuery, parts, cancellationToken),
                        "surveys" => await HandleSurveysCallbackAsync(callbackQuery, parts, cancellationToken), // Pagination and noop
                        "action" => await HandleActionCallbackAsync(callbackQuery, parts, cancellationToken),
                        "listsurveys" => await HandleActionCallbackAsync(callbackQuery, parts, cancellationToken), // Legacy pagination
                        _ => await HandleUnknownCallbackAsync(callbackQuery, cancellationToken)
                    };

                    // Answer callback query to remove loading state (fast response < 100ms target)
                    await _botService.Client.AnswerCallbackQuery(
                        callbackQueryId: callbackQuery.Id,
                        cancellationToken: cancellationToken);

                    return handled;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error handling callback query from user {TelegramId}",
                        callbackQuery.From.Id);

                    // Try to answer callback query even if there was an error
                    try
                    {
                        await _botService.Client.AnswerCallbackQuery(
                            callbackQueryId: callbackQuery.Id,
                            text: "An error occurred. Please try again.",
                            showAlert: true,
                            cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        // Ignore errors when answering callback query
                    }

                    return false;
                }
            },
            context: $"UserId={callbackQuery.From.Id}, Data={callbackQuery.Data}");
    }

    private async Task<bool> HandleCallbackCommandAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (parts.Length < 2)
        {
            _logger.LogWarning("Invalid callback command format");
            return false;
        }

        var command = parts[1];

        _logger.LogInformation(
            "Processing callback command '{Command}' from user {TelegramId}",
            command,
            callbackQuery.From.Id);

        // Check if command is registered
        if (!_commandRouter.IsCommandRegistered(command))
        {
            _logger.LogWarning(
                "Callback command '{Command}' not registered",
                command);
            return false;
        }

        // Get the handler directly
        var handler = _commandRouter.GetAllHandlers()
            .FirstOrDefault(h => h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            return false;
        }

        // Execute handler with the callback's message
        await handler.HandleAsync(callbackQuery.Message!, cancellationToken);

        return true;
    }

    private async Task<bool> HandleSurveyCallbackAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        // Handle survey:take:{surveyCode} callback
        if (parts.Length >= 3 && parts[1] == "take")
        {
            var surveyCode = parts[2];

            if (string.IsNullOrWhiteSpace(surveyCode))
            {
                _logger.LogWarning("Invalid survey code in callback: {SurveyCode}", parts[2]);
                await _botService.Client.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid survey code",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return false;
            }

            _logger.LogInformation(
                "Starting survey {SurveyCode} for user {TelegramId} via callback",
                surveyCode,
                callbackQuery.From.Id);

            // Get the survey command handler
            var surveyHandler = _commandRouter.GetAllHandlers()
                .FirstOrDefault(h => h.Command.Equals("survey", StringComparison.OrdinalIgnoreCase));

            if (surveyHandler == null)
            {
                _logger.LogError("Survey command handler not found");
                await _botService.Client.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Survey handler not available",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return false;
            }

            // Use JSON serialization to create a modified message with the survey command
            var originalMessage = callbackQuery.Message!;
            try
            {
                var messageJson = JsonConvert.SerializeObject(originalMessage);
                var messageObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(messageJson);

                if (messageObject != null)
                {
                    // Modify the text property
                    messageObject["text"] = $"/survey {surveyCode}";

                    // CRITICAL FIX: Update the 'from' field to use the callback query sender (real user)
                    // instead of the original message sender (bot), otherwise state gets set for wrong user
                    var fromJson = JsonConvert.SerializeObject(callbackQuery.From);
                    messageObject["from"] = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(fromJson);

                    // Deserialize back to Message
                    var modifiedMessage = messageObject.ToObject<Message>();

                    if (modifiedMessage != null)
                    {
                        // Execute the handler with the modified message
                        await surveyHandler.HandleAsync(modifiedMessage, cancellationToken);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create modified message for survey callback");
                await _botService.Client.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Failed to start survey",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return false;
            }

            return false;
        }

        // Other survey actions not yet implemented
        _logger.LogInformation(
            "Survey callback not yet implemented: {CallbackData}",
            callbackQuery.Data);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "This feature is coming soon!",
            showAlert: true,
            cancellationToken: cancellationToken);

        return true;
    }

    private async Task<bool> HandleSurveysCallbackAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        // Handle surveys:page:{pageNumber} callback
        if (parts.Length >= 3 && parts[1] == "page")
        {
            if (!int.TryParse(parts[2], out var pageNumber))
            {
                _logger.LogWarning("Invalid page number in callback: {PageNumber}", parts[2]);
                await _botService.Client.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid page number",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return false;
            }

            _logger.LogInformation(
                "Processing surveys pagination callback: page {Page}",
                pageNumber);

            // Get the surveys command handler
            var surveysHandler = _commandRouter.GetAllHandlers()
                .FirstOrDefault(h => h.Command.Equals("surveys", StringComparison.OrdinalIgnoreCase));

            if (surveysHandler == null)
            {
                _logger.LogError("Surveys command handler not found");
                return false;
            }

            // Use JSON serialization to create a modified message with page parameter
            var originalMessage = callbackQuery.Message!;
            try
            {
                var messageJson = JsonConvert.SerializeObject(originalMessage);
                var messageObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(messageJson);

                if (messageObject != null)
                {
                    // Modify the text property to include page number
                    messageObject["text"] = $"/surveys {pageNumber}";

                    // Deserialize back to Message
                    var modifiedMessage = messageObject.ToObject<Message>();

                    if (modifiedMessage != null)
                    {
                        // Execute the handler with the modified message
                        await surveysHandler.HandleAsync(modifiedMessage, cancellationToken);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create modified message for pagination");
            }

            return false;
        }

        // Handle surveys:noop callback (page indicator button - do nothing)
        if (parts.Length >= 2 && parts[1] == "noop")
        {
            // Just answer the callback, don't do anything
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
            return true;
        }

        _logger.LogWarning("Unknown surveys callback: {CallbackData}", callbackQuery.Data);
        return false;
    }

    private async Task<bool> HandleActionCallbackAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        // Handle listsurveys pagination: listsurveys:page:2
        if (parts.Length >= 3 && parts[0] == "listsurveys" && parts[1] == "page")
        {
            return await HandleListSurveysPaginationAsync(callbackQuery, parts, cancellationToken);
        }

        // Handle noop callbacks (page indicator button)
        if (parts.Length >= 2 && parts[0] == "listsurveys" && parts[1] == "noop")
        {
            // Just answer the callback, don't do anything
            await _botService.Client.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
            return true;
        }

        // Generic actions: create_survey, etc.
        // This will be implemented in future tasks

        _logger.LogInformation(
            "Action callback not yet implemented: {CallbackData}",
            callbackQuery.Data);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "This feature is coming soon!",
            showAlert: true,
            cancellationToken: cancellationToken);

        return true;
    }

    private async Task<bool> HandleListSurveysPaginationAsync(
        CallbackQuery callbackQuery,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (parts.Length < 3 || !int.TryParse(parts[2], out var pageNumber))
        {
            _logger.LogWarning("Invalid pagination callback: {CallbackData}", callbackQuery.Data);
            return false;
        }

        _logger.LogInformation(
            "Processing listsurveys pagination callback: page {Page}",
            pageNumber);

        // We cannot create a new Message object or modify the existing one because Message properties
        // in Telegram.Bot library are read-only. Instead of using reflection hacks, we'll create a
        // wrapper message that can be used by the handler.

        // The simplest approach: create a Message-like object with the properties we need
        // Since we can't instantiate Message directly, we'll pass the existing message but handle
        // the text parsing ourselves by creating a synthetic command text approach

        // Alternative: directly invoke the handler with a synthesized message
        // Since Message class has init-only properties, we need to use System.Text.Json or
        // similar to create an instance

        var handler = _commandRouter.GetAllHandlers()
            .FirstOrDefault(h => h.Command.Equals("listsurveys", StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            _logger.LogWarning("ListSurveysCommandHandler not found");
            return false;
        }

        // Use JSON serialization/deserialization as a workaround to create a modified Message
        // This is the cleanest way to work with init-only properties without using reflection
        // Telegram.Bot uses Newtonsoft.Json internally, so this approach is compatible
        var originalMessage = callbackQuery.Message!;

        try
        {
            var messageJson = JsonConvert.SerializeObject(originalMessage);
            var messageObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(messageJson);

            if (messageObject != null)
            {
                // Modify the text property
                messageObject["text"] = $"/listsurveys {pageNumber}";

                // Deserialize back to Message
                var modifiedMessage = messageObject.ToObject<Message>();

                if (modifiedMessage != null)
                {
                    // Execute the handler with the modified message
                    await handler.HandleAsync(modifiedMessage, cancellationToken);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create modified message for pagination");
        }

        return false;
    }

    private async Task<bool> HandleUnknownCallbackAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Unknown callback query type: {CallbackData}",
            callbackQuery.Data);

        await _botService.Client.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Unknown action",
            showAlert: true,
            cancellationToken: cancellationToken);

        return false;
    }

    private async Task<bool> HandleEditedMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Edited message from user {TelegramId} in chat {ChatId}",
            message.From?.Id,
            message.Chat.Id);

        // We don't handle edited messages differently for now
        // Could implement specific logic if needed

        return await Task.FromResult(false);
    }

    private async Task<bool> HandleUnsupportedUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Received unsupported update type {UpdateType} with ID {UpdateId}",
            update.Type,
            update.Id);

        return await Task.FromResult(false);
    }

    private async Task<bool> HandleCancelCallbackAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // Parse cancel callback: "cancel_confirm" or "cancel_dismiss"
        var data = callbackQuery.Data;

        if (data == "cancel_confirm")
        {
            return await _cancelCallbackHandler.HandleConfirmAsync(callbackQuery, cancellationToken);
        }
        else if (data == "cancel_dismiss")
        {
            return await _cancelCallbackHandler.HandleDismissAsync(callbackQuery, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown cancel callback format: {CallbackData}", data);
            return false;
        }
    }

    private async Task<bool> HandleNavigationCallbackAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // Parse navigation callback: "nav_back_q{questionId}" or "nav_skip_q{questionId}"
        var data = callbackQuery.Data;
        var isBack = data.StartsWith("nav_back_");
        var isSkip = data.StartsWith("nav_skip_");

        if (!isBack && !isSkip)
        {
            _logger.LogWarning("Invalid navigation callback format: {CallbackData}", data);
            return false;
        }

        // Extract question ID
        var prefix = isBack ? "nav_back_q" : "nav_skip_q";
        var questionIdStr = data.Substring(prefix.Length);

        if (!int.TryParse(questionIdStr, out var questionId))
        {
            _logger.LogWarning(
                "Failed to parse question ID from callback: {CallbackData}",
                data);
            return false;
        }

        // Route to navigation handler
        if (isBack)
        {
            return await _navigationHandler.HandleBackAsync(callbackQuery, questionId, cancellationToken);
        }
        else
        {
            return await _navigationHandler.HandleSkipAsync(callbackQuery, questionId, cancellationToken);
        }
    }
}
