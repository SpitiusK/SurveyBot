using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add Code column to Surveys table.
    /// Adds a unique, URL-safe code for easy survey sharing.
    /// </summary>
    public partial class AddSurveyCodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the code column
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "surveys",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            // Create unique index on code (with filter for non-null values)
            migrationBuilder.CreateIndex(
                name: "idx_surveys_code",
                table: "surveys",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            // Generate codes for existing surveys
            migrationBuilder.Sql(@"
                UPDATE surveys
                SET code = UPPER(SUBSTRING(MD5(RANDOM()::text || id::text), 1, 6))
                WHERE code IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the index
            migrationBuilder.DropIndex(
                name: "idx_surveys_code",
                table: "surveys");

            // Drop the column
            migrationBuilder.DropColumn(
                name: "code",
                table: "surveys");
        }
    }
}
