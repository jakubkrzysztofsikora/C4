using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArchitectureContextAndClassificationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classification_ClassificationSource",
                table: "discovered_resources",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Classification_Confidence",
                table: "discovered_resources",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Classification_IsInfrastructure",
                table: "discovered_resources",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "project_architecture_profiles",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectDescription = table.Column<string>(type: "text", nullable: false),
                    SystemBoundaries = table.Column<string>(type: "text", nullable: false),
                    CoreDomains = table.Column<string>(type: "text", nullable: false),
                    ExternalDependencies = table.Column<string>(type: "text", nullable: false),
                    DataSensitivity = table.Column<string>(type: "text", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastQuestionGenerationAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastResourceCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_architecture_profiles", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "project_architecture_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_architecture_questions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_architecture_questions_ProjectId",
                table: "project_architecture_questions",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_architecture_profiles");

            migrationBuilder.DropTable(
                name: "project_architecture_questions");

            migrationBuilder.DropColumn(
                name: "Classification_ClassificationSource",
                table: "discovered_resources");

            migrationBuilder.DropColumn(
                name: "Classification_Confidence",
                table: "discovered_resources");

            migrationBuilder.DropColumn(
                name: "Classification_IsInfrastructure",
                table: "discovered_resources");
        }
    }
}
