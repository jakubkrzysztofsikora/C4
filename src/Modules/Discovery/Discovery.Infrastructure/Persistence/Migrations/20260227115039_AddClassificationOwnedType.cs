using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationOwnedType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classification_C4Level",
                table: "discovered_resources",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Classification_FriendlyName",
                table: "discovered_resources",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Classification_IncludeInDiagram",
                table: "discovered_resources",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Classification_ServiceType",
                table: "discovered_resources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classification_C4Level",
                table: "discovered_resources");

            migrationBuilder.DropColumn(
                name: "Classification_FriendlyName",
                table: "discovered_resources");

            migrationBuilder.DropColumn(
                name: "Classification_IncludeInDiagram",
                table: "discovered_resources");

            migrationBuilder.DropColumn(
                name: "Classification_ServiceType",
                table: "discovered_resources");
        }
    }
}
