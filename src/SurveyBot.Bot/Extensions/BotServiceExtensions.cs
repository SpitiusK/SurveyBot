using Microsoft.Extensions.DependencyInjection;
using SurveyBot.Bot.Handlers;
using SurveyBot.Bot.Handlers.Commands;
using SurveyBot.Bot.Handlers.Questions;
using SurveyBot.Bot.Interfaces;
using SurveyBot.Bot.Services;
using SurveyBot.Bot.Utilities;
using SurveyBot.Bot.Validators;
using System.Net.Http;

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
        // Register HttpClient for handlers that need it
        services.AddScoped<HttpClient>();

        // Register named HttpClient for SurveyNavigationHelper with bot configuration
        services.AddHttpClient("SurveyBotApi", (sp, client) =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Configuration.BotConfiguration>>().Value;
            client.BaseAddress = new Uri(config.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.RequestTimeout);
        });

        // Register SurveyNavigationHelper (for conditional question flows)
        services.AddScoped<SurveyNavigationHelper>();

        // Register QuestionMediaHelper (needed by all question handlers)
        services.AddScoped<QuestionMediaHelper>();

        // Register command handlers - User commands (Scoped to work with Singleton CommandRouter)
        services.AddScoped<ICommandHandler, StartCommandHandler>();
        services.AddScoped<ICommandHandler, HelpCommandHandler>();
        services.AddScoped<ICommandHandler, MySurveysCommandHandler>();
        services.AddScoped<ICommandHandler, SurveysCommandHandler>();
        services.AddScoped<ICommandHandler, SurveyCommandHandler>();
        services.AddScoped<ICommandHandler, CancelCommandHandler>();
        services.AddScoped<ICommandHandler, PreviewCommand>();

        // Register admin command handlers
        services.AddScoped<ICommandHandler, CreateSurveyCommandHandler>();
        services.AddScoped<ICommandHandler, ListSurveysCommandHandler>();
        services.AddScoped<ICommandHandler, ActivateCommandHandler>();
        services.AddScoped<ICommandHandler, DeactivateCommandHandler>();
        services.AddScoped<ICommandHandler, StatsCommandHandler>();
        services.AddScoped<ICommandHandler, AdminHelpCommandHandler>();

        // Register question handlers (7 total - one per QuestionType)
        services.AddScoped<IQuestionHandler, TextQuestionHandler>();
        services.AddScoped<IQuestionHandler, SingleChoiceQuestionHandler>();
        services.AddScoped<IQuestionHandler, MultipleChoiceQuestionHandler>();
        services.AddScoped<IQuestionHandler, RatingQuestionHandler>();
        services.AddScoped<IQuestionHandler, LocationQuestionHandler>();
        services.AddScoped<IQuestionHandler, NumberQuestionHandler>();
        services.AddScoped<IQuestionHandler, DateQuestionHandler>();

        // Register completion handler
        services.AddScoped<CompletionHandler>();

        // Register navigation handler (for back/skip navigation)
        services.AddScoped<NavigationHandler>();

        // Register survey response handler (for processing survey answers)
        services.AddScoped<SurveyResponseHandler>();

        // Register cancel callback handler
        services.AddScoped<CancelCallbackHandler>();

        // Register command router as Scoped to allow it to resolve scoped services
        services.AddScoped<CommandRouter>();

        // Register update handler as Scoped (needs access to scoped services like CommandRouter)
        services.AddScoped<IUpdateHandler, UpdateHandler>();

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

        // Register media services
        services.AddScoped<SurveyBot.Core.Interfaces.ITelegramMediaService, TelegramMediaService>();

        return services;
    }
}
