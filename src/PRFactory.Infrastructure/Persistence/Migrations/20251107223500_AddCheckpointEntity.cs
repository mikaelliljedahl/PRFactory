using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add Checkpoints table for graph checkpoint persistence.
    /// Implements checkpoint storage for workflow graph resumption and fault tolerance.
    /// </summary>
    public partial class AddCheckpointEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Checkpoints table
            migrationBuilder.CreateTable(
                name: "Checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CheckpointId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraphId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NextAgentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResumedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checkpoints_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Checkpoints_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index on TenantId for tenant-specific queries
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TenantId",
                table: "Checkpoints",
                column: "TenantId");

            // Create index on TicketId for ticket-specific queries
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TicketId",
                table: "Checkpoints",
                column: "TicketId");

            // Create composite index on TicketId + GraphId for getting latest checkpoint
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TicketId_GraphId",
                table: "Checkpoints",
                columns: new[] { "TicketId", "GraphId" });

            // Create composite index on TicketId + GraphId + Status for active checkpoint queries
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TicketId_GraphId_Status",
                table: "Checkpoints",
                columns: new[] { "TicketId", "GraphId", "Status" });

            // Create index on Status for cleanup operations
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_Status",
                table: "Checkpoints",
                column: "Status");

            // Create index on CreatedAt for expiring old checkpoints
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_CreatedAt",
                table: "Checkpoints",
                column: "CreatedAt");

            // Create composite index on Status + CreatedAt for efficient expiration queries
            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_Status_CreatedAt",
                table: "Checkpoints",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the Checkpoints table
            migrationBuilder.DropTable(
                name: "Checkpoints");
        }
    }
}
