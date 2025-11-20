using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaContentToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "media_content",
                table: "questions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_questions_media_content",
                table: "questions",
                column: "media_content")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_questions_media_content",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "media_content",
                table: "questions");
        }
    }
}
