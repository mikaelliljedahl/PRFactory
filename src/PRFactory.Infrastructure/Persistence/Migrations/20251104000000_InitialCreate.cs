using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Initial database migration for PRFactory.
    /// Creates tables for Tenants, Repositories, Tickets, and WorkflowEvents.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Tenants table
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JiraUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    JiraApiToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ClaudeApiKey = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Configuration = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            // Create Repositories table
            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GitPlatform = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CloneUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DefaultBranch = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LocalPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repositories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Tickets table
            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TicketSystem = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Questions = table.Column<string>(type: "TEXT", nullable: false),
                    Answers = table.Column<string>(type: "TEXT", nullable: false),
                    PlanBranchName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlanMarkdownPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PlanApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImplementationBranchName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PullRequestUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PullRequestNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create WorkflowEvents table
            migrationBuilder.CreateTable(
                name: "WorkflowEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    From = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    To = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    QuestionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AnswerText = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PullRequestUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PullRequestNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowEvents_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            // Tenant indexes
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants",
                column: "IsActive");

            // Repository indexes
            migrationBuilder.CreateIndex(
                name: "IX_Repositories_TenantId",
                table: "Repositories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_CloneUrl",
                table: "Repositories",
                column: "CloneUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_GitPlatform",
                table: "Repositories",
                column: "GitPlatform");

            // Ticket indexes
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketKey",
                table: "Tickets",
                column: "TicketKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TenantId",
                table: "Tickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RepositoryId",
                table: "Tickets",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_State",
                table: "Tickets",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_State_TenantId",
                table: "Tickets",
                columns: new[] { "State", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedAt",
                table: "Tickets",
                column: "CreatedAt");

            // WorkflowEvent indexes
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_TicketId",
                table: "WorkflowEvents",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_OccurredAt",
                table: "WorkflowEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_EventType",
                table: "WorkflowEvents",
                column: "EventType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkflowEvents");
            migrationBuilder.DropTable(name: "Tickets");
            migrationBuilder.DropTable(name: "Repositories");
            migrationBuilder.DropTable(name: "Tenants");
        }
    }
}
