using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationQuestionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_question_type",
                table: "questions");

            migrationBuilder.AddCheckConstraint(
                name: "chk_question_type",
                table: "questions",
                sql: "question_type IN (0, 1, 2, 3, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_question_type",
                table: "questions");

            migrationBuilder.AddCheckConstraint(
                name: "chk_question_type",
                table: "questions",
                sql: "question_type IN (0, 1, 2, 3)");
        }
    }
}
