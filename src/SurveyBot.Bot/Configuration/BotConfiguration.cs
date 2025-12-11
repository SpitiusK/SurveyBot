namespace SurveyBot.Bot.Configuration;

/// <summary>
/// Configuration settings for the Telegram Bot.
/// </summary>
public class BotConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "BotConfiguration";

    /// <summary>
    /// Telegram Bot API token from BotFather.
    /// Should be stored in user secrets for development and secure configuration for production.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for webhook endpoint (e.g., "https://yourdomain.com").
    /// Required for webhook mode.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Webhook endpoint path (e.g., "/api/bot/webhook").
    /// Will be appended to WebhookUrl.
    /// </summary>
    public string WebhookPath { get; set; } = "/api/bot/webhook";

    /// <summary>
    /// Secret token for webhook validation.
    /// Used to verify that requests are coming from Telegram.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of connections for webhook.
    /// Default: 40 (Telegram's default).
    /// </summary>
    public int MaxConnections { get; set; } = 40;

    /// <summary>
    /// The base URL of the SurveyBot API (e.g., "https://localhost:5001").
    /// Used by the bot to make API calls.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Enable or disable webhook mode.
    /// If false, bot will use long polling (for development).
    /// </summary>
    public bool UseWebhook { get; set; } = false;

    /// <summary>
    /// Bot username (without @).
    /// Used for logging and display purposes.
    /// </summary>
    public string BotUsername { get; set; } = string.Empty;

    /// <summary>
    /// Timeout for API requests in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int RequestTimeout { get; set; } = 30;

    /// <summary>
    /// List of Telegram user IDs that have admin privileges.
    /// These users can execute admin commands like /createsurvey, /listsurveys, etc.
    /// </summary>
    public long[] AdminUserIds { get; set; } = Array.Empty<long>();

    /// <summary>
    /// Gets the full webhook URL by combining WebhookUrl and WebhookPath.
    /// </summary>
    public string FullWebhookUrl => $"{WebhookUrl.TrimEnd('/')}{WebhookPath}";

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    /// <remarks>
    /// When UseWebhook=true but WebhookUrl is empty, the bot expects webhook
    /// registration to be handled externally (e.g., by a webhook-registrar container).
    /// In this case, the bot will listen for incoming webhooks but won't register them.
    /// </remarks>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BotToken))
        {
            errors.Add("BotToken is required");
        }

        if (UseWebhook)
        {
            // WebhookUrl is optional - if empty, webhook registration is handled externally
            // (e.g., by webhook-registrar container in Docker setup)
            if (!string.IsNullOrWhiteSpace(WebhookUrl) && !Uri.TryCreate(WebhookUrl, UriKind.Absolute, out _))
            {
                errors.Add("WebhookUrl must be a valid absolute URL");
            }

            // WebhookSecret is required for security when receiving webhooks
            if (string.IsNullOrWhiteSpace(WebhookSecret))
            {
                errors.Add("WebhookSecret is required when UseWebhook is enabled");
            }
        }

        // ApiBaseUrl is optional - only needed for internal API calls
        // When empty, bot can still function for receiving webhooks
        if (!string.IsNullOrWhiteSpace(ApiBaseUrl) && !Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("ApiBaseUrl must be a valid absolute URL");
        }

        return errors.Count == 0;
    }
}
