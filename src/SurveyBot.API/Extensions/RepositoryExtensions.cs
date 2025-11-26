using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Repositories;

namespace SurveyBot.API.Extensions;

/// <summary>
/// Extension methods for configuring repository services.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds repository implementations to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register repositories with Scoped lifetime
        // Scoped ensures one instance per HTTP request, which aligns with DbContext lifetime
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        // TEMPORARY: Commented for migration generation (INFRA-002)
        // services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
