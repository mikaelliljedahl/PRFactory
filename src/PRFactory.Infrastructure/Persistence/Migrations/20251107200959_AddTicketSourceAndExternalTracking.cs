using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add ticket source and external tracking fields.
    /// Adds support for Web UI ticket creation and external ticket synchronization.
    /// </summary>
    public partial class AddTicketSourceAndExternalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Source column to track ticket origin (0 = WebUI, 1 = Jira, etc.)
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Add ExternalTicketId to track mapping to external systems
            migrationBuilder.AddColumn<string>(
                name: "ExternalTicketId",
                table: "Tickets",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            // Add LastSyncedAt to track synchronization with external systems
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            // Add PlanSummary to store plan summaries for Web UI display
            migrationBuilder.AddColumn<string>(
                name: "PlanSummary",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            // Create index on ExternalTicketId for faster lookups
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ExternalTicketId",
                table: "Tickets",
                column: "ExternalTicketId",
                unique: true);

            // Create index on Source for filtering by ticket source
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Source",
                table: "Tickets",
                column: "Source");

            // Create index on LastSyncedAt for sync tracking queries
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LastSyncedAt",
                table: "Tickets",
                column: "LastSyncedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Tickets_LastSyncedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Source",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ExternalTicketId",
                table: "Tickets");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "PlanSummary",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExternalTicketId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Tickets");
        }
    }
}
