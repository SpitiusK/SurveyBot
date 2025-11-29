using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionTypeConstraintForNumberAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing CHECK constraint that only allows question types 0-4
            migrationBuilder.Sql(@"
                ALTER TABLE questions
                DROP CONSTRAINT IF EXISTS chk_question_type;
            ");

            // Add new CHECK constraint that allows question types 0-6
            // Including: Text(0), SingleChoice(1), MultipleChoice(2), Rating(3), Location(4), Number(5), Date(6)
            migrationBuilder.Sql(@"
                ALTER TABLE questions
                ADD CONSTRAINT chk_question_type
                CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4, 5, 6]));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the original constraint that only allows question types 0-4
            migrationBuilder.Sql(@"
                ALTER TABLE questions
                DROP CONSTRAINT IF EXISTS chk_question_type;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE questions
                ADD CONSTRAINT chk_question_type
                CHECK (question_type = ANY (ARRAY[0, 1, 2, 3, 4]));
            ");
        }
    }
}
