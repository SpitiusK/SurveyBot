using SurveyBot.Infrastructure.Data;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Helpers;

/// <summary>
/// Base class for repository tests providing common setup and teardown logic.
/// </summary>
public abstract class RepositoryTestBase : IDisposable
{
    protected readonly SurveyBotDbContext _context;

    protected RepositoryTestBase()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
