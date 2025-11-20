using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the QuestionBranchingRule entity.
/// </summary>
public class QuestionBranchingRuleConfiguration : IEntityTypeConfiguration<QuestionBranchingRule>
{
    public void Configure(EntityTypeBuilder<QuestionBranchingRule> builder)
    {
        // Table name
        builder.ToTable("question_branching_rules");

        // Primary key
        builder.HasKey(qbr => qbr.Id);
        builder.Property(qbr => qbr.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // SourceQuestionId - foreign key to questions
        builder.Property(qbr => qbr.SourceQuestionId)
            .HasColumnName("source_question_id")
            .IsRequired();

        builder.HasIndex(qbr => qbr.SourceQuestionId)
            .HasDatabaseName("idx_branching_rules_source_question");

        // TargetQuestionId - foreign key to questions
        builder.Property(qbr => qbr.TargetQuestionId)
            .HasColumnName("target_question_id")
            .IsRequired();

        builder.HasIndex(qbr => qbr.TargetQuestionId)
            .HasDatabaseName("idx_branching_rules_target_question");

        // Composite index for efficient querying of rules between questions
        builder.HasIndex(qbr => new { qbr.SourceQuestionId, qbr.TargetQuestionId })
            .HasDatabaseName("idx_branching_rules_source_target");

        // Unique constraint - only one rule per source-target pair
        builder.HasIndex(qbr => new { qbr.SourceQuestionId, qbr.TargetQuestionId })
            .IsUnique()
            .HasDatabaseName("idx_branching_rules_source_target_unique");

        // ConditionJson - stored as JSONB in PostgreSQL
        builder.Property(qbr => qbr.ConditionJson)
            .HasColumnName("condition_json")
            .HasColumnType("jsonb")
            .IsRequired();

        // GIN index for JSONB condition searching
        builder.HasIndex(qbr => qbr.ConditionJson)
            .HasDatabaseName("idx_branching_rules_condition_json")
            .HasMethod("gin");

        // Check constraint - condition JSON must not be null
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_condition_json_not_null",
            "condition_json IS NOT NULL"));

        // Check constraint - source and target questions must be different
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_source_target_different",
            "source_question_id != target_question_id"));

        // CreatedAt
        builder.Property(qbr => qbr.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UpdatedAt
        builder.Property(qbr => qbr.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relationships
        builder.HasOne(qbr => qbr.SourceQuestion)
            .WithMany(q => q.OutgoingRules)
            .HasForeignKey(qbr => qbr.SourceQuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_branching_rules_source_question");

        builder.HasOne(qbr => qbr.TargetQuestion)
            .WithMany(q => q.IncomingRules)
            .HasForeignKey(qbr => qbr.TargetQuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_branching_rules_target_question");
    }
}
