using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add WorkflowStates table for workflow state persistence.
    /// Implements IWorkflowStateStore for tracking workflow execution across graphs.
    /// </summary>
    public partial class AddWorkflowStateStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create WorkflowStates table
            migrationBuilder.CreateTable(
                name: "WorkflowStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentGraph = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CurrentState = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStates", x => x.Id);
                });

            // Create unique index on WorkflowId for fast lookup
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_WorkflowId",
                table: "WorkflowStates",
                column: "WorkflowId",
                unique: true);

            // Create index on TicketId for finding workflows by ticket
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_TicketId",
                table: "WorkflowStates",
                column: "TicketId");

            // Create index on Status for querying workflows by status
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_Status",
                table: "WorkflowStates",
                column: "Status");

            // Create composite index for common queries
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_TicketId_Status",
                table: "WorkflowStates",
                columns: new[] { "TicketId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the WorkflowStates table
            migrationBuilder.DropTable(
                name: "WorkflowStates");
        }
    }
}
