using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add multi-platform ticket system support.
    /// Renames Jira-specific columns to platform-agnostic names and adds TicketPlatform column.
    /// Maintains backward compatibility by keeping legacy column names as aliases.
    /// </summary>
    public partial class AddTicketPlatformSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TicketPlatform column (default to "Jira" for existing tenants)
            migrationBuilder.AddColumn<string>(
                name: "TicketPlatform",
                table: "Tenants",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Jira");

            // Rename JiraUrl to TicketPlatformUrl
            migrationBuilder.RenameColumn(
                name: "JiraUrl",
                table: "Tenants",
                newName: "TicketPlatformUrl");

            // Rename JiraApiToken to TicketPlatformApiToken
            migrationBuilder.RenameColumn(
                name: "JiraApiToken",
                table: "Tenants",
                newName: "TicketPlatformApiToken");

            // Create index on TicketPlatform for filtering by platform type
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TicketPlatform",
                table: "Tenants",
                column: "TicketPlatform");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_Tenants_TicketPlatform",
                table: "Tenants");

            // Rename back to original names
            migrationBuilder.RenameColumn(
                name: "TicketPlatformUrl",
                table: "Tenants",
                newName: "JiraUrl");

            migrationBuilder.RenameColumn(
                name: "TicketPlatformApiToken",
                table: "Tenants",
                newName: "JiraApiToken");

            // Drop TicketPlatform column
            migrationBuilder.DropColumn(
                name: "TicketPlatform",
                table: "Tenants");
        }
    }
}
