using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncludeInStatisticsToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "include_in_statistics",
                table: "questions",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Whether to include this question in statistics calculations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "include_in_statistics",
                table: "questions");
        }
    }
}
