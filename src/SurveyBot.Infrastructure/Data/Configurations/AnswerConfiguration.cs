using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Answer entity.
/// </summary>
public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    /// <summary>
    /// JSON serializer options for AnswerValue polymorphic serialization.
    /// IMPORTANT: We serialize WITH type discriminator for storage,
    /// but use AnswerValueFactory.ParseWithTypeDetection() for deserialization
    /// because System.Text.Json requires $type to be the FIRST property,
    /// which is not guaranteed in PostgreSQL JSONB storage.
    /// </summary>
    private static readonly JsonSerializerOptions AnswerValueJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

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

        // Value - polymorphic answer value object stored as JSONB
        // Uses System.Text.Json with polymorphic type discriminator
        // CRITICAL FIX: PostgreSQL JSONB may reorder properties, so $type may not be first.
        // Standard JsonSerializer.Deserialize<AnswerValue> requires $type to be first.
        // Solution: Use AnswerValueFactory.ParseWithTypeDetection() which handles any property order.
        builder.Property(a => a.Value)
            .HasColumnName("answer_value_json")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(
                // To database: Serialize value object to JSON with type discriminator
                v => v != null ? JsonSerializer.Serialize(v, AnswerValueJsonOptions) : null,
                // From database: Use factory method that handles $type in any position
                json => !string.IsNullOrWhiteSpace(json)
                    ? AnswerValueFactory.ParseWithTypeDetection(json, null)
                    : null);

        // GIN index for answer_value_json searching
        builder.HasIndex(a => a.Value)
            .HasDatabaseName("idx_answers_value_json")
            .HasMethod("gin");

        // Check constraint: at least one of answer_text, answer_json, or answer_value_json must be present
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_answer_not_null",
            "answer_text IS NOT NULL OR answer_json IS NOT NULL OR answer_value_json IS NOT NULL"));

        // CreatedAt
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // NEW: Conditional flow configuration using Owned Type (Value Object)

        // Configure Next as owned type (NextQuestionDeterminant value object)
        builder.OwnsOne(a => a.Next, nb =>
        {
            // Type property: Maps NextStepType enum to string column
            nb.Property(n => n.Type)
                .HasColumnName("next_step_type")
                .HasConversion<string>()  // Store enum as string ("GoToQuestion" or "EndSurvey")
                .IsRequired();

            // NextQuestionId property: Nullable int for the target question ID
            nb.Property(n => n.NextQuestionId)
                .HasColumnName("next_step_question_id")
                .IsRequired(false);  // Nullable (null when Type = EndSurvey)
        });

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
    }
}
