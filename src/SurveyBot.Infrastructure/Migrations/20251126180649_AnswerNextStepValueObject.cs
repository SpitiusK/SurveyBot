using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnswerNextStepValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the old index
            migrationBuilder.DropIndex(
                name: "idx_answers_next_question_id",
                table: "answers");

            // Step 2: Add new columns (temporary nullable)
            migrationBuilder.AddColumn<string>(
                name: "next_step_type",
                table: "answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "next_step_question_id",
                table: "answers",
                type: "integer",
                nullable: true);

            // Step 3: Migrate existing data
            // Convert old next_question_id values to new value object format
            // If old value = 0 → next_step_type = 'EndSurvey', next_step_question_id = NULL
            // If old value > 0 → next_step_type = 'GoToQuestion', next_step_question_id = old value
            migrationBuilder.Sql(@"
                UPDATE answers
                SET next_step_type = CASE
                    WHEN next_question_id = 0 THEN 'EndSurvey'
                    ELSE 'GoToQuestion'
                END,
                next_step_question_id = CASE
                    WHEN next_question_id = 0 THEN NULL
                    ELSE next_question_id
                END;
            ");

            // Step 4: Make next_step_type NOT NULL after data migration
            migrationBuilder.AlterColumn<string>(
                name: "next_step_type",
                table: "answers",
                type: "text",
                nullable: false);

            // Step 5: Drop old column
            migrationBuilder.DropColumn(
                name: "next_question_id",
                table: "answers");

            // Step 6: Add CHECK constraint to enforce value object invariants
            migrationBuilder.Sql(@"
                ALTER TABLE answers ADD CONSTRAINT chk_answer_next_invariant
                CHECK (
                    (next_step_type = 'GoToQuestion' AND next_step_question_id IS NOT NULL AND next_step_question_id > 0) OR
                    (next_step_type = 'EndSurvey' AND next_step_question_id IS NULL)
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop CHECK constraint
            migrationBuilder.Sql(@"
                ALTER TABLE answers DROP CONSTRAINT IF EXISTS chk_answer_next_invariant;
            ");

            // Step 2: Add back old column
            migrationBuilder.AddColumn<int>(
                name: "next_question_id",
                table: "answers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Step 3: Migrate data back
            // Convert value object format back to magic value format
            // If next_step_type = 'EndSurvey' → next_question_id = 0
            // If next_step_type = 'GoToQuestion' → next_question_id = next_step_question_id
            migrationBuilder.Sql(@"
                UPDATE answers
                SET next_question_id = CASE
                    WHEN next_step_type = 'EndSurvey' THEN 0
                    ELSE COALESCE(next_step_question_id, 0)
                END;
            ");

            // Step 4: Drop new columns
            migrationBuilder.DropColumn(
                name: "next_step_type",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "next_step_question_id",
                table: "answers");

            // Step 5: Recreate old index
            migrationBuilder.CreateIndex(
                name: "idx_answers_next_question_id",
                table: "answers",
                column: "next_question_id");
        }
    }
}
