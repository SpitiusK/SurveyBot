using Microsoft.EntityFrameworkCore;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.API.Extensions;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adds database context and related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The web host environment.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Configure Entity Framework Core with PostgreSQL
        services.AddDbContext<SurveyBotDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string 'DefaultConnection' is not configured.");
            }

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Configure connection resilience
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Set command timeout
                npgsqlOptions.CommandTimeout(30);
            });

            // Enable detailed logging in development
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Add query logging
            options.LogTo(Console.WriteLine, LogLevel.Information);
        }, ServiceLifetime.Scoped);

        return services;
    }
}
