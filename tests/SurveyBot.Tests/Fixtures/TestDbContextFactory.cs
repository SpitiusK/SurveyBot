using Microsoft.EntityFrameworkCore;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Tests.Fixtures;

/// <summary>
/// Factory for creating in-memory test database contexts.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory database context with a unique database name.
    /// </summary>
    public static SurveyBotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SurveyBotDbContext(options);
    }

    /// <summary>
    /// Creates a new in-memory database context with a specific database name.
    /// Useful when you need multiple contexts to share the same database.
    /// </summary>
    public static SurveyBotDbContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<SurveyBotDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new SurveyBotDbContext(options);
    }
}
