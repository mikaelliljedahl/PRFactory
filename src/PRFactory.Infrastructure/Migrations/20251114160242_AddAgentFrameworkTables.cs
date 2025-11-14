using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentFrameworkTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ErrorLogs_Tenants_TenantId",
                table: "ErrorLogs");

            migrationBuilder.RenameColumn(
                name: "JiraUrl",
                table: "Tenants",
                newName: "TicketPlatformUrl");

            migrationBuilder.RenameColumn(
                name: "JiraApiToken",
                table: "Tenants",
                newName: "TicketPlatformApiToken");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionId",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PullRequestUrl",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<int>(
                name: "PullRequestNumber",
                table: "WorkflowEvents",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "BranchName",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5000);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowSuspended_GraphId",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityProvider",
                table: "Users",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthAccessToken",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthRefreshToken",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthScopes",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "OAuthTokenExpiresAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LlmProviderId",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTenantId",
                table: "Tenants",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentityProvider",
                table: "Tenants",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketPlatform",
                table: "Tenants",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Jira");

            migrationBuilder.AddColumn<string>(
                name: "AgentState",
                table: "Checkpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentThreadId",
                table: "Checkpoints",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversationHistory",
                table: "Checkpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: false),
                    EnabledTools = table.Column<string>(type: "TEXT", nullable: false),
                    MaxTokens = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 8000),
                    Temperature = table.Column<float>(type: "REAL", nullable: false, defaultValue: 0.3f),
                    StreamingEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ToolName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Input = table.Column<string>(type: "TEXT", nullable: false),
                    Output = table.Column<string>(type: "TEXT", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    TokensUsed = table.Column<int>(type: "INTEGER", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentExecutionLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentExecutionLogs_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeReviewResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PullRequestNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PullRequestUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LlmProviderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CriticalIssues = table.Column<string>(type: "jsonb", nullable: false),
                    Suggestions = table.Column<string>(type: "jsonb", nullable: false),
                    Praise = table.Column<string>(type: "jsonb", nullable: false),
                    FullReviewContent = table.Column<string>(type: "text", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetryAttempt = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeReviewResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeReviewResults_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantLlmProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    UsesOAuth = table.Column<bool>(type: "INTEGER", nullable: false),
                    EncryptedApiToken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TimeoutMs = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 300000),
                    DefaultModel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisableNonEssentialTraffic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ModelOverrides = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OAuthTokenRefreshedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId1 = table.Column<Guid>(type: "TEXT", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_TenantLlmProviders_Tenants_TenantId1",
                        column: x => x.TenantId1,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LlmProviderId",
                table: "Tickets",
                column: "LlmProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IdentityProvider_ExternalTenantId",
                table: "Tenants",
                columns: new[] { "IdentityProvider", "ExternalTenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TicketPlatform",
                table: "Tenants",
                column: "TicketPlatform");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConfigurations_TenantId",
                table: "AgentConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConfigurations_TenantId_AgentName",
                table: "AgentConfigurations",
                columns: new[] { "TenantId", "AgentName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionLogs_ExecutedAt",
                table: "AgentExecutionLogs",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionLogs_TenantId",
                table: "AgentExecutionLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionLogs_TenantId_AgentName",
                table: "AgentExecutionLogs",
                columns: new[] { "TenantId", "AgentName" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionLogs_TicketId",
                table: "AgentExecutionLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionLogs_TicketId_ExecutedAt",
                table: "AgentExecutionLogs",
                columns: new[] { "TicketId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeReviewResults_LlmProviderName",
                table: "CodeReviewResults",
                column: "LlmProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_CodeReviewResults_PullRequestNumber",
                table: "CodeReviewResults",
                column: "PullRequestNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CodeReviewResults_ReviewedAt",
                table: "CodeReviewResults",
                column: "ReviewedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CodeReviewResults_TicketId",
                table: "CodeReviewResults",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeReviewResults_TicketId_RetryAttempt",
                table: "CodeReviewResults",
                columns: new[] { "TicketId", "RetryAttempt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_ProviderType",
                table: "TenantLlmProviders",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId",
                table: "TenantLlmProviders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId_IsActive",
                table: "TenantLlmProviders",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId_IsDefault",
                table: "TenantLlmProviders",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantLlmProviders_TenantId1",
                table: "TenantLlmProviders",
                column: "TenantId1");

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
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TenantLlmProviders_LlmProviderId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "AgentConfigurations");

            migrationBuilder.DropTable(
                name: "AgentExecutionLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CodeReviewResults");

            migrationBuilder.DropTable(
                name: "TenantLlmProviders");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_LlmProviderId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_IdentityProvider_ExternalTenantId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_TicketPlatform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WorkflowSuspended_GraphId",
                table: "WorkflowEvents");

            migrationBuilder.DropColumn(
                name: "IdentityProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OAuthAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OAuthRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OAuthScopes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OAuthTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LlmProviderId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExternalTenantId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IdentityProvider",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TicketPlatform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AgentState",
                table: "Checkpoints");

            migrationBuilder.DropColumn(
                name: "AgentThreadId",
                table: "Checkpoints");

            migrationBuilder.DropColumn(
                name: "ConversationHistory",
                table: "Checkpoints");

            migrationBuilder.RenameColumn(
                name: "TicketPlatformUrl",
                table: "Tenants",
                newName: "JiraUrl");

            migrationBuilder.RenameColumn(
                name: "TicketPlatformApiToken",
                table: "Tenants",
                newName: "JiraApiToken");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "WorkflowEvents",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionId",
                table: "WorkflowEvents",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PullRequestUrl",
                table: "WorkflowEvents",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PullRequestNumber",
                table: "WorkflowEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GraphId",
                table: "WorkflowEvents",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "WorkflowEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BranchName",
                table: "WorkflowEvents",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "WorkflowEvents",
                type: "TEXT",
                maxLength: 5000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ErrorLogs_Tenants_TenantId",
                table: "ErrorLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
