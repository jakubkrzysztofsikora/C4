using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feedback.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackParamlessCtor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedback_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    classification_correction = table.Column<string>(type: "jsonb", nullable: true),
                    edge_correction = table.Column<string>(type: "jsonb", nullable: true),
                    node_correction = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_insights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    InsightType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    FeedbackCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_insights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_Category_SubmittedAtUtc",
                table: "feedback_entries",
                columns: new[] { "Category", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_ProjectId",
                table: "feedback_entries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_insights_ExpiresAtUtc",
                table: "learning_insights",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_learning_insights_ProjectId_Category",
                table: "learning_insights",
                columns: new[] { "ProjectId", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback_entries");

            migrationBuilder.DropTable(
                name: "learning_insights");
        }
    }
}
