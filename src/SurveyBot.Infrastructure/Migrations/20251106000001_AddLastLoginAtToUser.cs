using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add LastLoginAt column to Users table.
    /// Tracks when users last logged in or interacted with the bot.
    /// </summary>
    public partial class AddLastLoginAtToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_login_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "users");
        }
    }
}
