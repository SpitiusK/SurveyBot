using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyVersionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "surveys",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "surveys");
        }
    }
}
