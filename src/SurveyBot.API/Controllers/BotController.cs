using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SurveyBot.API.Services;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Interfaces;
using Telegram.Bot.Types;
using ApiResponse = SurveyBot.API.Models.ApiResponse;
using ErrorResponse = SurveyBot.API.Models.ErrorResponse;

namespace SurveyBot.API.Controllers;

/// <summary>
/// Controller for handling Telegram Bot webhook requests.
/// Receives updates from Telegram and routes them for processing.
/// </summary>
[ApiController]
[Route("api/bot")]
[Produces("application/json")]
public class BotController : ControllerBase
{
    private readonly IUpdateHandler _updateHandler;
    private readonly BotConfiguration _botConfiguration;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<BotController> _logger;

    /// <summary>
    /// Initializes a new instance of the BotController.
    /// </summary>
    public BotController(
        IUpdateHandler updateHandler,
        IOptions<BotConfiguration> botConfiguration,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<BotController> logger)
    {
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _botConfiguration = botConfiguration?.Value ?? throw new ArgumentNullException(nameof(botConfiguration));
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Webhook endpoint for receiving Telegram updates.
    /// Validates the request, queues the update for background processing, and returns 200 OK immediately.
    /// </summary>
    /// <param name="update">The update from Telegram.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK response.</returns>
    /// <response code="200">Update received and queued for processing.</response>
    /// <response code="400">Invalid request or update validation failed.</response>
    /// <response code="401">Webhook secret validation failed.</response>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Webhook([FromBody] Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate webhook secret from header
            if (!ValidateWebhookSecret())
            {
                _logger.LogWarning("Webhook request rejected: Invalid or missing secret token");
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Invalid webhook secret"
                });
            }

            // Validate update
            if (update == null)
            {
                _logger.LogWarning("Webhook request rejected: Update is null");
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Update cannot be null"
                });
            }

            _logger.LogInformation(
                "Webhook received update {UpdateId} of type {UpdateType}",
                update.Id,
                update.Type);

            // Queue update for background processing (fire-and-forget)
            _backgroundTaskQueue.QueueBackgroundWorkItem(async (serviceProvider, ct) =>
            {
                try
                {
                    _logger.LogDebug("Processing update {UpdateId} in background", update.Id);
                    
                    // Get a fresh UpdateHandler instance from the scoped service provider
                    var updateHandler = serviceProvider.GetRequiredService<IUpdateHandler>();
                    await updateHandler.HandleUpdateAsync(update, ct);
                    
                    _logger.LogDebug("Update {UpdateId} processed successfully", update.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing update {UpdateId} in background",
                        update.Id);

                    // Handle error through the update handler
                    try
                    {
                        var updateHandler = serviceProvider.GetRequiredService<IUpdateHandler>();
                        await updateHandler.HandleErrorAsync(ex, ct);
                    }
                    catch (Exception errorHandlerEx)
                    {
                        _logger.LogError(
                            errorHandlerEx,
                            "Error handler failed for update {UpdateId}",
                            update.Id);
                    }
                }
            });

            // Return 200 OK immediately (Telegram requires response within 60 seconds)
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Update received and queued for processing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook request");

            // Still return 200 to Telegram to avoid retries
            // Log the error for investigation
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Update received"
            });
        }
    }

    /// <summary>
    /// Gets the current webhook info and bot status.
    /// </summary>
    /// <returns>Webhook and bot status information.</returns>
    /// <response code="200">Returns webhook status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SurveyBot.API.Models.ApiResponse<BotStatusResponse>), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var status = new BotStatusResponse
        {
            WebhookConfigured = _botConfiguration.UseWebhook,
            WebhookUrl = _botConfiguration.UseWebhook ? _botConfiguration.FullWebhookUrl : null,
            BotUsername = _botConfiguration.BotUsername,
            ApiBaseUrl = _botConfiguration.ApiBaseUrl,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Bot status requested");

        return Ok(SurveyBot.API.Models.ApiResponse<BotStatusResponse>.Ok(status));
    }

    /// <summary>
    /// Health check endpoint specific to bot webhook functionality.
    /// </summary>
    /// <returns>Health status.</returns>
    /// <response code="200">Bot webhook is healthy.</response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var response = new
        {
            success = true,
            healthy = true,
            service = "Bot Webhook",
            webhookEnabled = _botConfiguration.UseWebhook,
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Validates the webhook secret from the request header.
    /// Telegram sends a custom header X-Telegram-Bot-Api-Secret-Token with webhook requests.
    /// </summary>
    /// <returns>True if the secret is valid, false otherwise.</returns>
    private bool ValidateWebhookSecret()
    {
        // In development or if webhook is not configured, skip validation
        if (!_botConfiguration.UseWebhook || string.IsNullOrWhiteSpace(_botConfiguration.WebhookSecret))
        {
            _logger.LogWarning("Webhook secret validation skipped (webhook not configured or no secret)");
            return true; // Allow for development/testing
        }

        // Check for Telegram's secret token header
        if (!Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var secretToken))
        {
            _logger.LogWarning("X-Telegram-Bot-Api-Secret-Token header not found");
            return false;
        }

        var isValid = secretToken.ToString() == _botConfiguration.WebhookSecret;

        if (!isValid)
        {
            _logger.LogWarning("Invalid webhook secret token received");
        }

        return isValid;
    }
}

/// <summary>
/// Response model for bot status endpoint.
/// </summary>
public class BotStatusResponse
{
    /// <summary>
    /// Indicates if webhook is configured.
    /// </summary>
    public bool WebhookConfigured { get; set; }

    /// <summary>
    /// The webhook URL if configured.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Bot username.
    /// </summary>
    public string? BotUsername { get; set; }

    /// <summary>
    /// API base URL.
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// Timestamp of status check.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
