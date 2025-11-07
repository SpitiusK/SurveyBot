using SurveyBot.API.Services;

namespace SurveyBot.API.Extensions;

/// <summary>
/// Extension methods for registering background services.
/// </summary>
public static class BackgroundServiceExtensions
{
    /// <summary>
    /// Adds background task queue and hosted service for processing queued tasks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="queueCapacity">Maximum queue capacity. Default is 100.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackgroundTaskQueue(
        this IServiceCollection services,
        int queueCapacity = 100)
    {
        // Register the background task queue as singleton
        services.AddSingleton<IBackgroundTaskQueue>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<BackgroundTaskQueue>>();
            return new BackgroundTaskQueue(queueCapacity, logger);
        });

        // Register the hosted service that processes queued tasks
        services.AddHostedService<QueuedHostedService>();

        return services;
    }
}
