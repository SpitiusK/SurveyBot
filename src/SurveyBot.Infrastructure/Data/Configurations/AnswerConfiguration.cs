using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Answer entity.
/// </summary>
public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        // Table name
        builder.ToTable("answers");

        // Primary key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // ResponseId - foreign key to responses
        builder.Property(a => a.ResponseId)
            .HasColumnName("response_id")
            .IsRequired();

        builder.HasIndex(a => a.ResponseId)
            .HasDatabaseName("idx_answers_response_id");

        // QuestionId - foreign key to questions
        builder.Property(a => a.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.HasIndex(a => a.QuestionId)
            .HasDatabaseName("idx_answers_question_id");

        // Composite index for response + question
        builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
            .HasDatabaseName("idx_answers_response_question");

        // Unique constraint: one answer per question per response
        builder.HasIndex(a => new { a.ResponseId, a.QuestionId })
            .IsUnique()
            .HasDatabaseName("idx_answers_response_question_unique");

        // AnswerText
        builder.Property(a => a.AnswerText)
            .HasColumnName("answer_text")
            .HasColumnType("text");

        // AnswerJson - stored as JSONB in PostgreSQL
        builder.Property(a => a.AnswerJson)
            .HasColumnName("answer_json")
            .HasColumnType("jsonb");

        // GIN index for JSONB answer searching
        builder.HasIndex(a => a.AnswerJson)
            .HasDatabaseName("idx_answers_answer_json")
            .HasMethod("gin");

        // Check constraint: at least one of answer_text or answer_json must be present
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_answer_not_null",
            "answer_text IS NOT NULL OR answer_json IS NOT NULL"));

        // CreatedAt
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // NEW: Conditional flow configuration

        // NextQuestionId - required, default 0 (end of survey marker)
        // Note: 0 is a special value meaning "end of survey", not a FK to questions
        builder.Property(a => a.NextQuestionId)
            .HasColumnName("next_question_id")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(a => a.NextQuestionId)
            .HasDatabaseName("idx_answers_next_question_id");

        // Relationships
        builder.HasOne(a => a.Response)
            .WithMany(r => r.Answers)
            .HasForeignKey(a => a.ResponseId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_answers_response");

        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_answers_question");

        // NextQuestion relationship - NO FK constraint (0 is valid end-of-survey marker)
        // Navigation property for convenience, but no database constraint
        builder.Ignore(a => a.NextQuestion);
    }
}
