using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBranchingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_questions_survey_order_unique",
                table: "questions");

            migrationBuilder.CreateTable(
                name: "question_branching_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_question_id = table.Column<int>(type: "integer", nullable: false),
                    target_question_id = table.Column<int>(type: "integer", nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_branching_rules", x => x.id);
                    table.CheckConstraint("chk_condition_json_not_null", "condition_json IS NOT NULL");
                    table.CheckConstraint("chk_source_target_different", "source_question_id != target_question_id");
                    table.ForeignKey(
                        name: "fk_branching_rules_source_question",
                        column: x => x.source_question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_branching_rules_target_question",
                        column: x => x.target_question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_questions_survey_order",
                table: "questions",
                columns: new[] { "survey_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_branching_rules_condition_json",
                table: "question_branching_rules",
                column: "condition_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_branching_rules_source_question",
                table: "question_branching_rules",
                column: "source_question_id");

            migrationBuilder.CreateIndex(
                name: "idx_branching_rules_source_target_unique",
                table: "question_branching_rules",
                columns: new[] { "source_question_id", "target_question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_branching_rules_target_question",
                table: "question_branching_rules",
                column: "target_question_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "question_branching_rules");

            migrationBuilder.DropIndex(
                name: "idx_questions_survey_order",
                table: "questions");

            migrationBuilder.CreateIndex(
                name: "idx_questions_survey_order_unique",
                table: "questions",
                columns: new[] { "survey_id", "order_index" },
                unique: true);
        }
    }
}
