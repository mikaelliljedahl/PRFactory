using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLlmProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add LlmProviderId column to Tickets table
            migrationBuilder.AddColumn<Guid>(
                name: "LlmProviderId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            // Create TenantLlmProviders table
            migrationBuilder.CreateTable(
                name: "TenantLlmProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    UsesOAuth = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptedApiToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false, defaultValue: 300000),
                    DefaultModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisableNonEssentialTraffic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ModelOverrides = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OAuthTokenRefreshedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLlmProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantLlmProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId",
                table: "TenantLlmProviders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_ProviderType",
                table: "TenantLlmProviders",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId_IsDefault",
                table: "TenantLlmProviders",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId_IsActive",
                table: "TenantLlmProviders",
                columns: new[] { "TenantId", "IsActive" });

            // Add foreign key from Tickets to TenantLlmProviders
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LlmProviderId",
                table: "Tickets",
                column: "LlmProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TenantLlmProviders_LlmProviderId",
                table: "Tickets",
                column: "LlmProviderId",
                principalTable: "TenantLlmProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key and index from Tickets
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TenantLlmProviders_LlmProviderId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_LlmProviderId",
                table: "Tickets");

            // Drop TenantLlmProviders table
            migrationBuilder.DropTable(
                name: "TenantLlmProviders");

            // Drop LlmProviderId column from Tickets
            migrationBuilder.DropColumn(
                name: "LlmProviderId",
                table: "Tickets");
        }
    }
}
