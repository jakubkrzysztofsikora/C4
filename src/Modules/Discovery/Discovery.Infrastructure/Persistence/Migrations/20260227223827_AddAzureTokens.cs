using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAzureTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "azure_tokens",
                columns: table => new
                {
                    external_subscription_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    access_token = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_azure_tokens", x => x.external_subscription_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "azure_tokens");
        }
    }
}
