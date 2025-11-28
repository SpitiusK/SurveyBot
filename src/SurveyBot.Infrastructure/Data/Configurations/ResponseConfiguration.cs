using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Response entity.
/// </summary>
public class ResponseConfiguration : IEntityTypeConfiguration<Response>
{
    public void Configure(EntityTypeBuilder<Response> builder)
    {
        // Table name
        builder.ToTable("responses");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // SurveyId - foreign key to surveys
        builder.Property(r => r.SurveyId)
            .HasColumnName("survey_id")
            .IsRequired();

        builder.HasIndex(r => r.SurveyId)
            .HasDatabaseName("idx_responses_survey_id");

        // RespondentTelegramId - NOT a foreign key to allow anonymous responses
        builder.Property(r => r.RespondentTelegramId)
            .HasColumnName("respondent_telegram_id")
            .IsRequired();

        // Composite index for survey + respondent (for duplicate checking)
        builder.HasIndex(r => new { r.SurveyId, r.RespondentTelegramId })
            .HasDatabaseName("idx_responses_survey_respondent");

        // IsComplete
        builder.Property(r => r.IsComplete)
            .HasColumnName("is_complete")
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(r => r.IsComplete)
            .HasDatabaseName("idx_responses_complete")
            .HasFilter("is_complete = true");

        // StartedAt
        builder.Property(r => r.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamp with time zone");

        // SubmittedAt
        builder.Property(r => r.SubmittedAt)
            .HasColumnName("submitted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(r => r.SubmittedAt)
            .HasDatabaseName("idx_responses_submitted_at")
            .HasFilter("submitted_at IS NOT NULL");

        // NEW: Conditional flow tracking

        // VisitedQuestionIds - JSON array of visited question IDs for cycle prevention
        builder.Property(r => r.VisitedQuestionIds)
            .HasColumnName("visited_question_ids")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'::jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<int>());

        // GIN index for JSON querying
        builder.HasIndex(r => r.VisitedQuestionIds)
            .HasDatabaseName("idx_responses_visited_question_ids")
            .HasMethod("gin");

        // Configure backing field for Answers collection
        builder.Navigation(r => r.Answers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Relationships
        builder.HasOne(r => r.Survey)
            .WithMany(s => s.Responses)
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_responses_survey");

        builder.HasMany(r => r.Answers)
            .WithOne(a => a.Response)
            .HasForeignKey(a => a.ResponseId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_answers_response");
    }
}
