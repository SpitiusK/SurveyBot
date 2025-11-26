using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the QuestionOption entity.
/// </summary>
public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        // Table name
        builder.ToTable("question_options");

        // Primary key
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // QuestionId - foreign key to questions
        builder.Property(o => o.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.HasIndex(o => o.QuestionId)
            .HasDatabaseName("idx_question_options_question_id");

        // Text
        builder.Property(o => o.Text)
            .HasColumnName("text")
            .HasColumnType("text")
            .IsRequired();

        // OrderIndex
        builder.Property(o => o.OrderIndex)
            .HasColumnName("order_index")
            .IsRequired();

        // Composite index for ordered option retrieval
        builder.HasIndex(o => new { o.QuestionId, o.OrderIndex })
            .HasDatabaseName("idx_question_options_question_order");

        // Unique constraint for question_id + order_index
        builder.HasIndex(o => new { o.QuestionId, o.OrderIndex })
            .IsUnique()
            .HasDatabaseName("idx_question_options_question_order_unique");

        // Check constraint for order index
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_question_option_order_index",
            "order_index >= 0"));

        // NEW: Conditional flow configuration using Owned Type (Value Object)

        // Configure Next as owned type (NextQuestionDeterminant value object)
        builder.OwnsOne(o => o.Next, nb =>
        {
            // Type property: Maps NextStepType enum to string column
            nb.Property(n => n.Type)
                .HasColumnName("next_step_type")
                .HasConversion<string>()  // Store enum as string ("GoToQuestion" or "EndSurvey")
                .IsRequired();

            // NextQuestionId property: Nullable int for the target question ID
            nb.Property(n => n.NextQuestionId)
                .HasColumnName("next_question_id")
                .IsRequired(false);  // Nullable (null when Type = EndSurvey)
        });

        // CreatedAt
        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UpdatedAt
        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relationships
        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_question_options_question");
    }
}
