using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "architecture_graphs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_architecture_graphs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "graph_edges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    direction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ArchitectureGraphId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_edges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_graph_edges_architecture_graphs_ArchitectureGraphId",
                        column: x => x.ArchitectureGraphId,
                        principalTable: "architecture_graphs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "graph_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    technology = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tags = table.Column<string>(type: "text", nullable: false),
                    cost = table.Column<decimal>(type: "numeric", nullable: false),
                    ArchitectureGraphId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_graph_nodes_architecture_graphs_ArchitectureGraphId",
                        column: x => x.ArchitectureGraphId,
                        principalTable: "architecture_graphs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "graph_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchitectureGraphId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_graph_snapshots_architecture_graphs_ArchitectureGraphId",
                        column: x => x.ArchitectureGraphId,
                        principalTable: "architecture_graphs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_architecture_graphs_ProjectId",
                table: "architecture_graphs",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_graph_edges_ArchitectureGraphId",
                table: "graph_edges",
                column: "ArchitectureGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_graph_nodes_ArchitectureGraphId",
                table: "graph_nodes",
                column: "ArchitectureGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_graph_nodes_ExternalResourceId_ArchitectureGraphId",
                table: "graph_nodes",
                columns: new[] { "ExternalResourceId", "ArchitectureGraphId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_graph_snapshots_ArchitectureGraphId",
                table: "graph_snapshots",
                column: "ArchitectureGraphId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "graph_edges");

            migrationBuilder.DropTable(
                name: "graph_nodes");

            migrationBuilder.DropTable(
                name: "graph_snapshots");

            migrationBuilder.DropTable(
                name: "architecture_graphs");
        }
    }
}
