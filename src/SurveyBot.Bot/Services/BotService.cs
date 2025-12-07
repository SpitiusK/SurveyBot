using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Implementation of Telegram Bot service.
/// Manages bot client lifecycle and provides access to bot functionality.
/// </summary>
public class BotService : IBotService
{
    private readonly BotConfiguration _configuration;
    private readonly ILogger<BotService> _logger;
    private readonly ITelegramBotClient _botClient;

    public ITelegramBotClient Client => _botClient;

    public BotService(
        IOptions<BotConfiguration> configuration,
        ILogger<BotService> logger)
    {
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate configuration
        if (!_configuration.IsValid(out var errors))
        {
            var errorMessage = $"Invalid bot configuration: {string.Join(", ", errors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Initialize Telegram Bot Client - use direct constructor to bypass strict validation
        _botClient = new TelegramBotClient(_configuration.BotToken);

        _logger.LogInformation("BotService created successfully");
    }

    /// <inheritdoc/>
    public async Task<User> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing bot...");

            // Validate bot token by getting bot info
            var botInfo = await _botClient.GetMe(cancellationToken);

            _logger.LogInformation(
                "Bot initialized successfully. Bot: @{BotUsername} (ID: {BotId})",
                botInfo.Username,
                botInfo.Id);

            // Update configuration with bot username if not set
            if (string.IsNullOrWhiteSpace(_configuration.BotUsername))
            {
                _configuration.BotUsername = botInfo.Username ?? string.Empty;
            }

            return botInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize bot. Please check your bot token.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetWebhookAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_configuration.UseWebhook)
            {
                _logger.LogWarning("Webhook mode is disabled in configuration");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_configuration.WebhookUrl))
            {
                _logger.LogError("Webhook URL is not configured");
                return false;
            }

            var webhookUrl = _configuration.FullWebhookUrl;

            _logger.LogInformation("Setting webhook to: {WebhookUrl}", webhookUrl);

            await _botClient.SetWebhook(
                url: webhookUrl,
                secretToken: _configuration.WebhookSecret,
                maxConnections: _configuration.MaxConnections,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Webhook set successfully");

            // Verify webhook was set
            var webhookInfo = await GetWebhookInfoAsync(cancellationToken);
            _logger.LogInformation(
                "Webhook info: URL={WebhookUrl}, PendingUpdateCount={PendingCount}, LastError={LastError}",
                webhookInfo.Url,
                webhookInfo.PendingUpdateCount,
                webhookInfo.LastErrorMessage ?? "None");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set webhook");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveWebhookAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing webhook...");

            await _botClient.DeleteWebhook(
                dropPendingUpdates: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Webhook removed successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove webhook");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<WebhookInfo> GetWebhookInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookInfo = await _botClient.GetWebhookInfo(cancellationToken);
            return webhookInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get webhook info");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<User> GetMeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var botInfo = await _botClient.GetMe(cancellationToken);
            return botInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bot info");
            throw;
        }
    }

    /// <inheritdoc/>
    public bool ValidateWebhookSecret(string? secretToken)
    {
        if (!_configuration.UseWebhook)
        {
            _logger.LogWarning("Webhook mode is disabled, skipping secret validation");
            return true;
        }

        if (string.IsNullOrWhiteSpace(_configuration.WebhookSecret))
        {
            _logger.LogWarning("Webhook secret is not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(secretToken))
        {
            _logger.LogWarning("Webhook request missing secret token");
            return false;
        }

        var isValid = secretToken.Equals(_configuration.WebhookSecret, StringComparison.Ordinal);

        if (!isValid)
        {
            _logger.LogWarning("Invalid webhook secret token received");
        }

        return isValid;
    }

    #region Wrapper Methods for Telegram.Bot Operations

    // These wrapper methods enable mocking of Telegram.Bot extension methods in tests.
    // Extension methods are static and cannot be mocked on interfaces, so we wrap them
    // in instance methods on IBotService for testability.

    /// <inheritdoc/>
    public async Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return await _botClient.SendMessage(
            chatId,
            text,
            parseMode: parseMode ?? default,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Message> EditMessageTextAsync(
        ChatId chatId,
        int messageId,
        string text,
        ParseMode? parseMode = null,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return await _botClient.EditMessageText(
            chatId,
            messageId,
            text,
            parseMode: parseMode ?? default,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default)
    {
        await _botClient.AnswerCallbackQuery(
            callbackQueryId,
            text,
            showAlert,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        await _botClient.DeleteMessage(
            chatId,
            messageId,
            cancellationToken: cancellationToken);
    }

    #endregion
}
