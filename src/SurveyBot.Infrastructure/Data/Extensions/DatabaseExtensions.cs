using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SurveyBot.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for database operations.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Seeds the database with development data.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        var seeder = new DataSeeder(context, logger);
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Applies any pending migrations to the database.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();

        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Drops and recreates the database, then seeds with development data.
    /// WARNING: This will delete all existing data!
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ResetAndSeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SurveyBotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        // Drop and recreate
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        // Seed
        var seeder = new DataSeeder(context, logger);
        await seeder.SeedAsync();
    }
}
