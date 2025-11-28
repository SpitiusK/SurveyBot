using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerValueJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_answer_not_null",
                table: "answers");

            migrationBuilder.AddColumn<string>(
                name: "answer_value_json",
                table: "answers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_answers_value_json",
                table: "answers",
                column: "answer_value_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.AddCheckConstraint(
                name: "chk_answer_not_null",
                table: "answers",
                sql: "answer_text IS NOT NULL OR answer_json IS NOT NULL OR answer_value_json IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_answers_value_json",
                table: "answers");

            migrationBuilder.DropCheckConstraint(
                name: "chk_answer_not_null",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "answer_value_json",
                table: "answers");

            migrationBuilder.AddCheckConstraint(
                name: "chk_answer_not_null",
                table: "answers",
                sql: "answer_text IS NOT NULL OR answer_json IS NOT NULL");
        }
    }
}
