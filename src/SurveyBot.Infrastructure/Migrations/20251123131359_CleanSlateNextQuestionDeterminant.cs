using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <summary>
    /// ⚠️ WARNING: DESTRUCTIVE MIGRATION - This migration performs TRUNCATE CASCADE on all survey-related tables.
    /// ALL existing survey data (users, surveys, questions, question_options, responses, answers) will be PERMANENTLY DELETED.
    /// This is a clean slate refactoring to migrate from primitive int-based NextQuestionId to the NextQuestionDeterminant value object pattern.
    /// DO NOT apply this migration to production environments with existing data.
    /// Ensure database backup before applying in any environment.
    /// </summary>
    /// <inheritdoc />
    public partial class CleanSlateNextQuestionDeterminant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // CLEAN SLATE: Truncate all survey data (required for schema change)
            // ============================================================
            migrationBuilder.Sql(@"
                TRUNCATE TABLE answers RESTART IDENTITY CASCADE;
                TRUNCATE TABLE responses RESTART IDENTITY CASCADE;
                TRUNCATE TABLE question_options RESTART IDENTITY CASCADE;
                TRUNCATE TABLE questions RESTART IDENTITY CASCADE;
                TRUNCATE TABLE surveys RESTART IDENTITY CASCADE;
                TRUNCATE TABLE users RESTART IDENTITY CASCADE;
            ", suppressTransaction: true);

            // ============================================================
            // Drop old indexes
            // ============================================================
            migrationBuilder.DropIndex(
                name: "idx_questions_default_next_question_id",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "idx_question_options_next_question_id",
                table: "question_options");

            // ============================================================
            // Drop old FK constraints (if they exist)
            // ============================================================
            migrationBuilder.Sql(@"
                ALTER TABLE questions DROP CONSTRAINT IF EXISTS fk_questions_default_next_question;
                ALTER TABLE question_options DROP CONSTRAINT IF EXISTS fk_question_options_next_question;
            ");

            // ============================================================
            // Add new columns for NextQuestionDeterminant value object
            // ============================================================
            migrationBuilder.AddColumn<string>(
                name: "default_next_step_type",
                table: "questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "next_step_type",
                table: "question_options",
                type: "text",
                nullable: true);

            // ============================================================
            // Add CHECK constraints for value object invariants
            // ============================================================

            // Question.DefaultNext invariants
            migrationBuilder.Sql(@"
                ALTER TABLE questions ADD CONSTRAINT chk_question_default_next_invariant
                CHECK (
                    (default_next_step_type IS NULL AND default_next_question_id IS NULL) OR
                    (default_next_step_type = 'GoToQuestion' AND default_next_question_id IS NOT NULL AND default_next_question_id > 0) OR
                    (default_next_step_type = 'EndSurvey' AND default_next_question_id IS NULL)
                );
            ");

            // QuestionOption.Next invariants
            migrationBuilder.Sql(@"
                ALTER TABLE question_options ADD CONSTRAINT chk_question_option_next_invariant
                CHECK (
                    (next_step_type IS NULL AND next_question_id IS NULL) OR
                    (next_step_type = 'GoToQuestion' AND next_question_id IS NOT NULL AND next_question_id > 0) OR
                    (next_step_type = 'EndSurvey' AND next_question_id IS NULL)
                );
            ");

            // ============================================================
            // Re-add FK constraints with ON DELETE SET NULL
            // ============================================================

            // Question.DefaultNextQuestionId -> questions.id
            migrationBuilder.AddForeignKey(
                name: "fk_questions_default_next_question",
                table: "questions",
                column: "default_next_question_id",
                principalTable: "questions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // QuestionOption.NextQuestionId -> questions.id
            migrationBuilder.AddForeignKey(
                name: "fk_question_options_next_question",
                table: "question_options",
                column: "next_question_id",
                principalTable: "questions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // ============================================================
            // Create indexes for performance
            // ============================================================
            migrationBuilder.CreateIndex(
                name: "idx_questions_default_next_question_id",
                table: "questions",
                column: "default_next_question_id");

            migrationBuilder.CreateIndex(
                name: "idx_question_options_next_question_id",
                table: "question_options",
                column: "next_question_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Drop indexes
            // ============================================================
            migrationBuilder.DropIndex(
                name: "idx_questions_default_next_question_id",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "idx_question_options_next_question_id",
                table: "question_options");

            // ============================================================
            // Drop FK constraints
            // ============================================================
            migrationBuilder.DropForeignKey(
                name: "fk_questions_default_next_question",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "fk_question_options_next_question",
                table: "question_options");

            // ============================================================
            // Drop CHECK constraints
            // ============================================================
            migrationBuilder.Sql(@"
                ALTER TABLE questions DROP CONSTRAINT IF EXISTS chk_question_default_next_invariant;
                ALTER TABLE question_options DROP CONSTRAINT IF EXISTS chk_question_option_next_invariant;
            ");

            // ============================================================
            // Drop new columns
            // ============================================================
            migrationBuilder.DropColumn(
                name: "default_next_step_type",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "next_step_type",
                table: "question_options");

            // ============================================================
            // WARNING: Cannot restore old schema - data was truncated
            // The old schema used simple int? columns for next question IDs
            // This migration cannot be rolled back without data loss
            // ============================================================
        }
    }
}
