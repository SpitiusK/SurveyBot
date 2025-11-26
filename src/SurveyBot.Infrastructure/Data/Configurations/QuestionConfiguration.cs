using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Question entity.
/// </summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        // Table name
        builder.ToTable("questions");

        // Primary key
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // SurveyId - foreign key to surveys
        builder.Property(q => q.SurveyId)
            .HasColumnName("survey_id")
            .IsRequired();

        builder.HasIndex(q => q.SurveyId)
            .HasDatabaseName("idx_questions_survey_id");

        // QuestionText
        builder.Property(q => q.QuestionText)
            .HasColumnName("question_text")
            .HasColumnType("text")
            .IsRequired();

        // QuestionType with check constraint
        builder.Property(q => q.QuestionType)
            .HasColumnName("question_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(q => q.QuestionType)
            .HasDatabaseName("idx_questions_type");

        // Check constraint for question type (0=Text, 1=SingleChoice, 2=MultipleChoice, 3=Rating)
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_question_type",
            "question_type IN (0, 1, 2, 3)"));

        // OrderIndex
        builder.Property(q => q.OrderIndex)
            .HasColumnName("order_index")
            .IsRequired();

        // Composite index for ordered question retrieval
        builder.HasIndex(q => new { q.SurveyId, q.OrderIndex })
            .HasDatabaseName("idx_questions_survey_order");

        // Unique constraint for survey_id + order_index
        builder.HasIndex(q => new { q.SurveyId, q.OrderIndex })
            .IsUnique()
            .HasDatabaseName("idx_questions_survey_order_unique");

        // Check constraint for order index
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_order_index",
            "order_index >= 0"));

        // IsRequired
        builder.Property(q => q.IsRequired)
            .HasColumnName("is_required")
            .IsRequired()
            .HasDefaultValue(true);

        // OptionsJson - stored as JSONB in PostgreSQL
        builder.Property(q => q.OptionsJson)
            .HasColumnName("options_json")
            .HasColumnType("jsonb");

        // GIN index for JSONB options searching
        builder.HasIndex(q => q.OptionsJson)
            .HasDatabaseName("idx_questions_options_json")
            .HasMethod("gin");

        // MediaContent - stored as JSONB in PostgreSQL
        builder.Property(q => q.MediaContent)
            .HasColumnName("media_content")
            .HasColumnType("jsonb");

        // GIN index for JSONB media content searching
        builder.HasIndex(q => q.MediaContent)
            .HasDatabaseName("idx_questions_media_content")
            .HasMethod("gin");

        // CreatedAt
        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UpdatedAt
        builder.Property(q => q.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // NEW: Conditional flow configuration using Owned Type (Value Object)

        // Configure DefaultNext as owned type (NextQuestionDeterminant value object)
        builder.OwnsOne(q => q.DefaultNext, nb =>
        {
            // Type property: Maps NextStepType enum to string column
            nb.Property(n => n.Type)
                .HasColumnName("default_next_step_type")
                .HasConversion<string>()  // Store enum as string ("GoToQuestion" or "EndSurvey")
                .IsRequired();

            // NextQuestionId property: Nullable int for the target question ID
            nb.Property(n => n.NextQuestionId)
                .HasColumnName("default_next_question_id")
                .IsRequired(false);  // Nullable (null when Type = EndSurvey)
        });

        // SupportsBranching - computed property, not mapped to database
        builder.Ignore(q => q.SupportsBranching);

        // Relationships
        builder.HasOne(q => q.Survey)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_questions_survey");

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_answers_question");

        // Options relationship
        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_question_options_question");
    }
}
