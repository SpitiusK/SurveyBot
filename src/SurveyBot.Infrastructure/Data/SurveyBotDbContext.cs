using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Data.Configurations;

namespace SurveyBot.Infrastructure.Data;

/// <summary>
/// Database context for the Survey Bot application.
/// </summary>
public class SurveyBotDbContext : DbContext
{
    public SurveyBotDbContext(DbContextOptions<SurveyBotDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Surveys DbSet.
    /// </summary>
    public DbSet<Survey> Surveys { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Questions DbSet.
    /// </summary>
    public DbSet<Question> Questions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Responses DbSet.
    /// </summary>
    public DbSet<Response> Responses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Answers DbSet.
    /// </summary>
    public DbSet<Answer> Answers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new SurveyConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionConfiguration());
        modelBuilder.ApplyConfiguration(new ResponseConfiguration());
        modelBuilder.ApplyConfiguration(new AnswerConfiguration());

        // Configure automatic timestamp updates for entities with UpdatedAt
        // This will be handled by SaveChangesAsync override
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable detailed logging in development
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps for entities that have UpdatedAt property
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Update CreatedAt for entities that have it but don't inherit from BaseEntity
        var nonBaseEntities = ChangeTracker.Entries()
            .Where(e => !(e.Entity is BaseEntity) && e.State == EntityState.Added);

        foreach (var entry in nonBaseEntities)
        {
            var entity = entry.Entity;
            var createdAtProperty = entity.GetType().GetProperty("CreatedAt");

            if (createdAtProperty != null && createdAtProperty.PropertyType == typeof(DateTime))
            {
                createdAtProperty.SetValue(entity, DateTime.UtcNow);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
