using C4.Modules.Discovery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discovery.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DiscoveryDbContext))]
[Migration("20260302120010_AddGitBranchAndRootPath")]
public sealed class AddGitBranchAndRootPath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GitBranch",
            table: "azure_subscriptions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "GitRootPath",
            table: "azure_subscriptions",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GitBranch",
            table: "azure_subscriptions");

        migrationBuilder.DropColumn(
            name: "GitRootPath",
            table: "azure_subscriptions");
    }
}
