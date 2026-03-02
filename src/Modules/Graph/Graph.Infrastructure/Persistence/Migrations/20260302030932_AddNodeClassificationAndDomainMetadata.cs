using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeClassificationAndDomainMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "classification_confidence",
                table: "graph_nodes",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                table: "graph_nodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "domain",
                table: "graph_nodes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_infrastructure",
                table: "graph_nodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "classification_confidence",
                table: "graph_nodes");

            migrationBuilder.DropColumn(
                name: "classification_source",
                table: "graph_nodes");

            migrationBuilder.DropColumn(
                name: "domain",
                table: "graph_nodes");

            migrationBuilder.DropColumn(
                name: "is_infrastructure",
                table: "graph_nodes");
        }
    }
}
