using Microsoft.Extensions.DependencyInjection;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;

namespace SurveyBot.Bot.Extensions;

/// <summary>
/// Extension methods for registering bot services in the DI container.
/// </summary>
public static class BotServiceExtensions
{
    /// <summary>
    /// Adds bot command handlers and routing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBotHandlers(this IServiceCollection services)
    {
        // Register command handlers
        services.AddTransient<ICommandHandler, StartCommandHandler>();
        services.AddTransient<ICommandHandler, HelpCommandHandler>();
        services.AddTransient<ICommandHandler, MySurveysCommandHandler>();
        services.AddTransient<ICommandHandler, SurveysCommandHandler>();

        // Register command router
        services.AddSingleton<CommandRouter>();

        // Register update handler
        services.AddSingleton<IUpdateHandler, UpdateHandler>();

        return services;
    }
}
