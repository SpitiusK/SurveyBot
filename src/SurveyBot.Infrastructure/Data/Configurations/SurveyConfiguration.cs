using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Survey entity.
/// </summary>
public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        // Table name
        builder.ToTable("surveys");

        // Primary key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Title
        builder.Property(s => s.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        // Description
        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // Code - unique survey code for sharing
        builder.Property(s => s.Code)
            .HasColumnName("code")
            .HasMaxLength(10);

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("idx_surveys_code")
            .HasFilter("code IS NOT NULL");

        // CreatorId - foreign key to users
        builder.Property(s => s.CreatorId)
            .HasColumnName("creator_id")
            .IsRequired();

        builder.HasIndex(s => s.CreatorId)
            .HasDatabaseName("idx_surveys_creator_id");

        // IsActive
        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("idx_surveys_is_active")
            .HasFilter("is_active = true");

        // AllowMultipleResponses
        builder.Property(s => s.AllowMultipleResponses)
            .HasColumnName("allow_multiple_responses")
            .IsRequired()
            .HasDefaultValue(false);

        // ShowResults
        builder.Property(s => s.ShowResults)
            .HasColumnName("show_results")
            .IsRequired()
            .HasDefaultValue(true);

        // CreatedAt
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UpdatedAt
        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Composite index for common query pattern (creator + active status)
        builder.HasIndex(s => new { s.CreatorId, s.IsActive })
            .HasDatabaseName("idx_surveys_creator_active");

        // Index for sorting by creation date
        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("idx_surveys_created_at")
            .IsDescending();

        // Configure backing fields for collections
        builder.Navigation(s => s.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(s => s.Responses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Relationships
        builder.HasOne(s => s.Creator)
            .WithMany(u => u.Surveys)
            .HasForeignKey(s => s.CreatorId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_surveys_creator");

        builder.HasMany(s => s.Questions)
            .WithOne(q => q.Survey)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_questions_survey");

        builder.HasMany(s => s.Responses)
            .WithOne(r => r.Survey)
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_responses_survey");
    }
}
