using C4.Modules.Graph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Graph.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GraphDbContext))]
[Migration("20260302120000_AddSnapshotPayloadAndSource")]
public sealed class AddSnapshotPayloadAndSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EdgesJson",
            table: "graph_snapshots",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "NodesJson",
            table: "graph_snapshots",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "graph_snapshots",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "discovery");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EdgesJson",
            table: "graph_snapshots");

        migrationBuilder.DropColumn(
            name: "NodesJson",
            table: "graph_snapshots");

        migrationBuilder.DropColumn(
            name: "Source",
            table: "graph_snapshots");
    }
}
