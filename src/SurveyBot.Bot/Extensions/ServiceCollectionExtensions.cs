using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurveyBot.Bot.Configuration;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;

namespace SurveyBot.Bot.Extensions;

/// <summary>
/// Extension methods for configuring Telegram Bot services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Telegram Bot services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTelegramBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register BotConfiguration
        services.Configure<BotConfiguration>(
            configuration.GetSection(BotConfiguration.SectionName));

        // Register BotService as Singleton
        // Singleton because we want only one bot client instance throughout the application
        services.AddSingleton<IBotService, BotService>();

        // Register ITelegramBotClient as Singleton via factory method from BotService
        // This allows other services to depend on ITelegramBotClient directly
        services.AddSingleton(sp =>
        {
            var botService = sp.GetRequiredService<IBotService>();
            return botService.Client;
        });

        return services;
    }

    /// <summary>
    /// Initializes the Telegram Bot by validating the token and optionally setting up webhook.
    /// Call this method during application startup.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when bot is initialized.</returns>
    public static async Task InitializeTelegramBotAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILogger<IBotService>>();

        try
        {
            logger.LogInformation("Starting Telegram Bot initialization...");

            var botService = services.GetRequiredService<IBotService>();

            // Initialize and validate bot token
            var botInfo = await botService.InitializeAsync(cancellationToken);

            logger.LogInformation(
                "Bot initialized: @{Username} (ID: {BotId})",
                botInfo.Username,
                botInfo.Id);

            // Get current configuration
            var configuration = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<BotConfiguration>>();
            var botConfig = configuration.Value;

            // Setup webhook if enabled in configuration
            if (botConfig.UseWebhook)
            {
                logger.LogInformation("Webhook mode enabled, setting up webhook...");
                var webhookSet = await botService.SetWebhookAsync(cancellationToken);

                if (webhookSet)
                {
                    logger.LogInformation("Webhook configured successfully");
                }
                else
                {
                    logger.LogWarning("Failed to configure webhook");
                }
            }
            else
            {
                logger.LogInformation("Long polling mode enabled (webhook disabled)");

                // Ensure webhook is removed if it exists
                var webhookInfo = await botService.GetWebhookInfoAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(webhookInfo.Url))
                {
                    logger.LogInformation("Removing existing webhook...");
                    await botService.RemoveWebhookAsync(cancellationToken);
                }
            }

            logger.LogInformation("Telegram Bot initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Telegram Bot");
            throw;
        }
    }
}
