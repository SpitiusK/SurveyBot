using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionalQuestionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "visited_question_ids",
                table: "responses",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "default_next_question_id",
                table: "questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "next_question_id",
                table: "answers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    next_question_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.id);
                    table.CheckConstraint("chk_question_option_order_index", "order_index >= 0");
                    table.ForeignKey(
                        name: "fk_question_options_next_question",
                        column: x => x.next_question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_question_options_question",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_responses_visited_question_ids",
                table: "responses",
                column: "visited_question_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_questions_default_next_question_id",
                table: "questions",
                column: "default_next_question_id");

            migrationBuilder.CreateIndex(
                name: "idx_answers_next_question_id",
                table: "answers",
                column: "next_question_id");

            migrationBuilder.CreateIndex(
                name: "idx_question_options_next_question_id",
                table: "question_options",
                column: "next_question_id");

            migrationBuilder.CreateIndex(
                name: "idx_question_options_question_id",
                table: "question_options",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "idx_question_options_question_order_unique",
                table: "question_options",
                columns: new[] { "question_id", "order_index" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_questions_default_next_question",
                table: "questions",
                column: "default_next_question_id",
                principalTable: "questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_questions_default_next_question",
                table: "questions");

            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropIndex(
                name: "idx_responses_visited_question_ids",
                table: "responses");

            migrationBuilder.DropIndex(
                name: "idx_questions_default_next_question_id",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "idx_answers_next_question_id",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "visited_question_ids",
                table: "responses");

            migrationBuilder.DropColumn(
                name: "default_next_question_id",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "next_question_id",
                table: "answers");
        }
    }
}
