using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add TicketUpdates table for storing AI-generated ticket refinements.
    /// Supports the refinement workflow where tickets are analyzed and enhanced with
    /// structured success criteria and acceptance criteria.
    /// </summary>
    public partial class AddTicketUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create TicketUpdates table
            migrationBuilder.CreateTable(
                name: "TicketUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UpdatedTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedDescription = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    SuccessCriteria = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketUpdates_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for common queries

            // Index for getting all updates for a ticket
            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId",
                table: "TicketUpdates",
                column: "TicketId");

            // Index for getting specific version of a ticket update
            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId_Version",
                table: "TicketUpdates",
                columns: new[] { "TicketId", "Version" });

            // Index for getting drafts for a ticket
            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId_IsDraft",
                table: "TicketUpdates",
                columns: new[] { "TicketId", "IsDraft" });

            // Index for getting approved updates for a ticket
            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId_IsApproved",
                table: "TicketUpdates",
                columns: new[] { "TicketId", "IsApproved" });

            // Index for date-based queries
            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_GeneratedAt",
                table: "TicketUpdates",
                column: "GeneratedAt");

            // Filtered index for pending posts (approved but not yet posted)
            // Note: SQLite doesn't support filtered indexes in the same way as SQL Server
            // This will create a regular index which is less optimal but functional
            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_IsApproved_PostedAt",
                table: "TicketUpdates",
                columns: new[] { "IsApproved", "PostedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes
            migrationBuilder.DropIndex(
                name: "IX_TicketUpdates_IsApproved_PostedAt",
                table: "TicketUpdates");

            migrationBuilder.DropIndex(
                name: "IX_TicketUpdates_GeneratedAt",
                table: "TicketUpdates");

            migrationBuilder.DropIndex(
                name: "IX_TicketUpdates_TicketId_IsApproved",
                table: "TicketUpdates");

            migrationBuilder.DropIndex(
                name: "IX_TicketUpdates_TicketId_IsDraft",
                table: "TicketUpdates");

            migrationBuilder.DropIndex(
                name: "IX_TicketUpdates_TicketId_Version",
                table: "TicketUpdates");

            migrationBuilder.DropIndex(
                name: "IX_TicketUpdates_TicketId",
                table: "TicketUpdates");

            // Drop the table
            migrationBuilder.DropTable(
                name: "TicketUpdates");
        }
    }
}
