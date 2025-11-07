using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace SurveyBot.API.Extensions;

/// <summary>
/// Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds health check services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString!,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "postgresql" },
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }

    /// <summary>
    /// Maps health check endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Basic health check endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        })
        .WithName("HealthCheck")
        .WithOpenApi()
        .AllowAnonymous();

        // Database-specific health check
        app.MapHealthChecks("/health/db", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("db"),
            ResponseWriter = WriteHealthCheckResponse
        })
        .WithName("DatabaseHealthCheck")
        .WithOpenApi()
        .AllowAnonymous();

        // Liveness probe (for container orchestration)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // No checks, just returns healthy if app is running
        })
        .WithName("LivenessCheck")
        .WithOpenApi()
        .AllowAnonymous();

        // Readiness probe (for container orchestration)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = (check) => check.Tags.Contains("db"),
            ResponseWriter = WriteHealthCheckResponse
        })
        .WithName("ReadinessCheck")
        .WithOpenApi()
        .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Writes a detailed health check response in JSON format.
    /// </summary>
    private static async Task WriteHealthCheckResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(result);
    }
}
