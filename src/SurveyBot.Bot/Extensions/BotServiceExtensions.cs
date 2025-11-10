using Microsoft.Extensions.DependencyInjection;
using SurveyBot.Bot.Handlers;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Validators;

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
        // Register command handlers - User commands
        services.AddTransient<ICommandHandler, StartCommandHandler>();
        services.AddTransient<ICommandHandler, HelpCommandHandler>();
        services.AddTransient<ICommandHandler, MySurveysCommandHandler>();
        services.AddTransient<ICommandHandler, SurveysCommandHandler>();
        services.AddTransient<ICommandHandler, SurveyCommandHandler>();
        services.AddTransient<ICommandHandler, CancelCommandHandler>();

        // Register admin command handlers
        services.AddTransient<ICommandHandler, CreateSurveyCommandHandler>();
        services.AddTransient<ICommandHandler, ListSurveysCommandHandler>();
        services.AddTransient<ICommandHandler, ActivateCommandHandler>();
        services.AddTransient<ICommandHandler, DeactivateCommandHandler>();
        services.AddTransient<ICommandHandler, StatsCommandHandler>();
        services.AddTransient<ICommandHandler, AdminHelpCommandHandler>();

        // Register question handlers
        services.AddTransient<IQuestionHandler, TextQuestionHandler>();
        services.AddTransient<IQuestionHandler, SingleChoiceQuestionHandler>();
        services.AddTransient<IQuestionHandler, MultipleChoiceQuestionHandler>();
        services.AddTransient<IQuestionHandler, RatingQuestionHandler>();

        // Register completion handler
        services.AddTransient<CompletionHandler>();

        // Register navigation handler (for back/skip navigation)
        services.AddTransient<NavigationHandler>();

        // Register cancel callback handler
        services.AddTransient<CancelCallbackHandler>();

        // Register command router
        services.AddSingleton<CommandRouter>();

        // Register update handler
        services.AddSingleton<IUpdateHandler, UpdateHandler>();

        // Register conversation state manager (singleton for in-memory storage)
        services.AddSingleton<IConversationStateManager, ConversationStateManager>();

        // Register validation and error handling services
        services.AddScoped<IAnswerValidator, AnswerValidator>();
        services.AddScoped<QuestionErrorHandler>();
        services.AddScoped<ApiErrorHandler>();

        // Register performance monitoring services (singleton for shared metrics)
        services.AddSingleton<BotPerformanceMonitor>();
        services.AddSingleton<SurveyCache>();

        // Register admin authorization service (singleton - no state)
        services.AddSingleton<IAdminAuthService, AdminAuthService>();

        return services;
    }
}
