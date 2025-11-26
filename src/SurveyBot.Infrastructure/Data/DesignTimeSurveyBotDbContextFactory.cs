using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SurveyBot.Infrastructure.Data;

/// <summary>
/// Design-time factory for SurveyBotDbContext.
/// Used by EF Core tools (migrations, etc.) to create DbContext instances at design time.
/// TEMPORARY: Created for INFRA-002 migration generation during clean slate refactoring.
/// </summary>
public class DesignTimeSurveyBotDbContextFactory : IDesignTimeDbContextFactory<SurveyBotDbContext>
{
    public SurveyBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SurveyBotDbContext>();

        // Use a hardcoded connection string for design-time operations
        // This is only used by EF Core tools, not at runtime
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("SurveyBot.Infrastructure"));

        return new SurveyBotDbContext(optionsBuilder.Options);
    }
}
