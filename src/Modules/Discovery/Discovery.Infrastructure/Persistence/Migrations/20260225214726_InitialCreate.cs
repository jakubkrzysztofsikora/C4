using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "azure_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_azure_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discovered_resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discovered_resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "drift_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drift_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_azure_subscriptions_ExternalSubscriptionId",
                table: "azure_subscriptions",
                column: "ExternalSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discovered_resources_SubscriptionId_ResourceId",
                table: "discovered_resources",
                columns: new[] { "SubscriptionId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drift_items_SubscriptionId",
                table: "drift_items",
                column: "SubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "azure_subscriptions");

            migrationBuilder.DropTable(
                name: "discovered_resources");

            migrationBuilder.DropTable(
                name: "drift_items");
        }
    }
}
