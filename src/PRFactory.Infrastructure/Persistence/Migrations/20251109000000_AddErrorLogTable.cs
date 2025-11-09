using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration to add ErrorLogs table for comprehensive error tracking and debugging.
    /// Supports error severity levels, entity association, resolution tracking, and statistics.
    /// </summary>
    public partial class AddErrorLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ErrorLogs table
            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContextData = table.Column<string>(type: "TEXT", nullable: true),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for common queries

            // Index for tenant-based queries
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_TenantId",
                table: "ErrorLogs",
                column: "TenantId");

            // Index for severity-based filtering
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Severity",
                table: "ErrorLogs",
                column: "Severity");

            // Index for resolution status
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_IsResolved",
                table: "ErrorLogs",
                column: "IsResolved");

            // Index for date-based queries
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_CreatedAt",
                table: "ErrorLogs",
                column: "CreatedAt");

            // Index for entity-based queries
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_EntityType_EntityId",
                table: "ErrorLogs",
                columns: new[] { "EntityType", "EntityId" });

            // Composite index for common filter combinations
            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_TenantId_IsResolved_Severity",
                table: "ErrorLogs",
                columns: new[] { "TenantId", "IsResolved", "Severity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes
            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_TenantId_IsResolved_Severity",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_EntityType_EntityId",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_CreatedAt",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_IsResolved",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_Severity",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_TenantId",
                table: "ErrorLogs");

            // Drop the table
            migrationBuilder.DropTable(
                name: "ErrorLogs");
        }
    }
}
