using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithTeamReview : Migration
    {
        // Table names
        private const string TableTickets = "Tickets";
        private const string TableWorkflowEvents = "WorkflowEvents";
        private const string TableTenants = "Tenants";
        private const string TableAgentPromptTemplates = "AgentPromptTemplates";
        private const string TableCheckpoints = "Checkpoints";
        private const string TableErrorLogs = "ErrorLogs";
        private const string TableTicketUpdates = "TicketUpdates";
        private const string TableUsers = "Users";
        private const string TablePlanReviews = "PlanReviews";
        private const string TableReviewComments = "ReviewComments";

        // Column types
        private const string TypeText = "TEXT";
        private const string TypeInteger = "INTEGER";

        // Column names
        private const string ColumnId = "Id";
        private const string ColumnTenantId = "TenantId";
        private const string ColumnTicketId = "TicketId";
        private const string ColumnCreatedAt = "CreatedAt";
        private const string ColumnUpdatedAt = "UpdatedAt";
        private const string ColumnQuestionId = "QuestionId";
        private const string ColumnPullRequestUrl = "PullRequestUrl";
        private const string ColumnPullRequestNumber = "PullRequestNumber";
        private const string ColumnBranchName = "BranchName";
        private const string ColumnAnswerText = "AnswerText";
        private const string ColumnDuration = "Duration";
        private const string ColumnError = "Error";
        private const string ColumnGraphId = "GraphId";
        private const string ColumnQuestion = "Question";
        private const string ColumnState = "State";
        private const string ColumnDescription = "Description";
        private const string ColumnExternalTicketId = "ExternalTicketId";
        private const string ColumnLastSyncedAt = "LastSyncedAt";
        private const string ColumnRequiredApprovalCount = "RequiredApprovalCount";
        private const string ColumnSource = "Source";
        private const string ColumnName = "Name";
        private const string ColumnPromptContent = "PromptContent";
        private const string ColumnRecommendedModel = "RecommendedModel";
        private const string ColumnColor = "Color";
        private const string ColumnCategory = "Category";
        private const string ColumnIsSystemTemplate = "IsSystemTemplate";
        private const string ColumnCheckpointId = "CheckpointId";
        private const string ColumnAgentName = "AgentName";
        private const string ColumnNextAgentType = "NextAgentType";
        private const string ColumnStateJson = "StateJson";
        private const string ColumnStatus = "Status";
        private const string ColumnResumedAt = "ResumedAt";
        private const string ColumnSeverity = "Severity";
        private const string ColumnMessage = "Message";
        private const string ColumnStackTrace = "StackTrace";
        private const string ColumnEntityType = "EntityType";
        private const string ColumnEntityId = "EntityId";
        private const string ColumnContextData = "ContextData";
        private const string ColumnIsResolved = "IsResolved";
        private const string ColumnResolvedAt = "ResolvedAt";
        private const string ColumnResolvedBy = "ResolvedBy";
        private const string ColumnResolutionNotes = "ResolutionNotes";
        private const string ColumnUpdatedTitle = "UpdatedTitle";
        private const string ColumnUpdatedDescription = "UpdatedDescription";
        private const string ColumnSuccessCriteria = "SuccessCriteria";
        private const string ColumnAcceptanceCriteria = "AcceptanceCriteria";
        private const string ColumnVersion = "Version";
        private const string ColumnIsDraft = "IsDraft";
        private const string ColumnIsApproved = "IsApproved";
        private const string ColumnRejectionReason = "RejectionReason";
        private const string ColumnGeneratedAt = "GeneratedAt";
        private const string ColumnApprovedAt = "ApprovedAt";
        private const string ColumnPostedAt = "PostedAt";
        private const string ColumnTicketId1 = "TicketId1";
        private const string ColumnEmail = "Email";
        private const string ColumnDisplayName = "DisplayName";
        private const string ColumnAvatarUrl = "AvatarUrl";
        private const string ColumnExternalAuthId = "ExternalAuthId";
        private const string ColumnLastSeenAt = "LastSeenAt";
        private const string ColumnReviewerId = "ReviewerId";
        private const string ColumnIsRequired = "IsRequired";
        private const string ColumnAssignedAt = "AssignedAt";
        private const string ColumnReviewedAt = "ReviewedAt";
        private const string ColumnDecision = "Decision";
        private const string ColumnAuthorId = "AuthorId";
        private const string ColumnContent = "Content";
        private const string ColumnMentionedUserIds = "MentionedUserIds";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: ColumnQuestionId,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: ColumnPullRequestUrl,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: ColumnPullRequestNumber,
                table: TableWorkflowEvents,
                type: TypeInteger,
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: TypeInteger,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: ColumnBranchName,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: ColumnAnswerText,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 5000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: ColumnDuration,
                table: TableWorkflowEvents,
                type: TypeText,
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: ColumnError,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: ColumnGraphId,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: ColumnQuestion,
                table: TableWorkflowEvents,
                type: TypeText,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: ColumnState,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: ColumnDescription,
                table: TableTickets,
                type: TypeText,
                maxLength: 10000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 10000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: ColumnExternalTicketId,
                table: TableTickets,
                type: TypeText,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: ColumnLastSyncedAt,
                table: TableTickets,
                type: TypeText,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: ColumnRequiredApprovalCount,
                table: TableTickets,
                type: TypeInteger,
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: ColumnSource,
                table: TableTickets,
                type: TypeInteger,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: TableAgentPromptTemplates,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    Name = table.Column<string>(type: TypeText, maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: TypeText, maxLength: 1000, nullable: false),
                    PromptContent = table.Column<string>(type: TypeText, nullable: false),
                    RecommendedModel = table.Column<string>(type: TypeText, maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: TypeText, maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: TypeText, maxLength: 100, nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: TypeInteger, nullable: false),
                    TenantId = table.Column<Guid>(type: TypeText, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: TypeText, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: TypeText, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPromptTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentPromptTemplates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: TableTenants,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: TableCheckpoints,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    CheckpointId = table.Column<string>(type: TypeText, maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: TypeText, nullable: false),
                    TicketId = table.Column<Guid>(type: TypeText, nullable: false),
                    GraphId = table.Column<string>(type: TypeText, maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: TypeText, maxLength: 200, nullable: true),
                    NextAgentType = table.Column<string>(type: TypeText, maxLength: 200, nullable: true),
                    StateJson = table.Column<string>(type: TypeText, nullable: false),
                    Status = table.Column<string>(type: TypeText, maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: TypeText, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: TypeText, nullable: true),
                    ResumedAt = table.Column<DateTime>(type: TypeText, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checkpoints_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: TableTenants,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Checkpoints_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: TableTickets,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: TableErrorLogs,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    TenantId = table.Column<Guid>(type: TypeText, nullable: false),
                    Severity = table.Column<string>(type: TypeText, nullable: false),
                    Message = table.Column<string>(type: TypeText, nullable: false),
                    StackTrace = table.Column<string>(type: TypeText, nullable: true),
                    EntityType = table.Column<string>(type: TypeText, nullable: true),
                    EntityId = table.Column<Guid>(type: TypeText, nullable: true),
                    ContextData = table.Column<string>(type: TypeText, nullable: true),
                    IsResolved = table.Column<bool>(type: TypeInteger, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: TypeText, nullable: true),
                    ResolvedBy = table.Column<string>(type: TypeText, nullable: true),
                    ResolutionNotes = table.Column<string>(type: TypeText, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: TypeText, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: TableTenants,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: TableTicketUpdates,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    TicketId = table.Column<Guid>(type: TypeText, nullable: false),
                    UpdatedTitle = table.Column<string>(type: TypeText, maxLength: 500, nullable: false),
                    UpdatedDescription = table.Column<string>(type: TypeText, maxLength: 10000, nullable: false),
                    SuccessCriteria = table.Column<string>(type: TypeText, nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: TypeText, maxLength: 10000, nullable: false),
                    Version = table.Column<int>(type: TypeInteger, nullable: false),
                    IsDraft = table.Column<bool>(type: TypeInteger, nullable: false),
                    IsApproved = table.Column<bool>(type: TypeInteger, nullable: false),
                    RejectionReason = table.Column<string>(type: TypeText, maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: TypeText, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: TypeText, nullable: true),
                    PostedAt = table.Column<DateTime>(type: TypeText, nullable: true),
                    TicketId1 = table.Column<Guid>(type: TypeText, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketUpdates_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: TableTickets,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketUpdates_Tickets_TicketId1",
                        column: x => x.TicketId1,
                        principalTable: TableTickets,
                        principalColumn: ColumnId);
                });

            migrationBuilder.CreateTable(
                name: TableUsers,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    TenantId = table.Column<Guid>(type: TypeText, nullable: false),
                    Email = table.Column<string>(type: TypeText, maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: TypeText, maxLength: 255, nullable: false),
                    AvatarUrl = table.Column<string>(type: TypeText, maxLength: 500, nullable: true),
                    ExternalAuthId = table.Column<string>(type: TypeText, maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: TypeText, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: TypeText, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: TableTenants,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: TablePlanReviews,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    TicketId = table.Column<Guid>(type: TypeText, nullable: false),
                    ReviewerId = table.Column<Guid>(type: TypeText, nullable: false),
                    Status = table.Column<int>(type: TypeInteger, nullable: false),
                    IsRequired = table.Column<bool>(type: TypeInteger, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: TypeText, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: TypeText, nullable: true),
                    Decision = table.Column<string>(type: TypeText, maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanReviews_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: TableTickets,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanReviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: TableUsers,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: TableReviewComments,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: TypeText, nullable: false),
                    TicketId = table.Column<Guid>(type: TypeText, nullable: false),
                    AuthorId = table.Column<Guid>(type: TypeText, nullable: false),
                    Content = table.Column<string>(type: TypeText, maxLength: 10000, nullable: false),
                    MentionedUserIds = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: TypeText, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: TypeText, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewComments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: TableTickets,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewComments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: TableUsers,
                        principalColumn: ColumnId,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPromptTemplates_Category",
                table: TableAgentPromptTemplates,
                column: ColumnCategory);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPromptTemplates_Category_TenantId",
                table: TableAgentPromptTemplates,
                columns: new[] { ColumnCategory, ColumnTenantId });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPromptTemplates_IsSystemTemplate",
                table: TableAgentPromptTemplates,
                column: ColumnIsSystemTemplate);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPromptTemplates_Name",
                table: TableAgentPromptTemplates,
                column: ColumnName);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPromptTemplates_Name_TenantId",
                table: TableAgentPromptTemplates,
                columns: new[] { ColumnName, ColumnTenantId });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPromptTemplates_TenantId",
                table: TableAgentPromptTemplates,
                column: ColumnTenantId);

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_CreatedAt",
                table: TableCheckpoints,
                column: ColumnCreatedAt);

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_Status",
                table: TableCheckpoints,
                column: ColumnStatus);

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_Status_CreatedAt",
                table: TableCheckpoints,
                columns: new[] { ColumnStatus, ColumnCreatedAt });

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TenantId",
                table: TableCheckpoints,
                column: ColumnTenantId);

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TicketId",
                table: TableCheckpoints,
                column: ColumnTicketId);

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TicketId_GraphId",
                table: TableCheckpoints,
                columns: new[] { ColumnTicketId, ColumnGraphId });

            migrationBuilder.CreateIndex(
                name: "IX_Checkpoints_TicketId_GraphId_Status",
                table: TableCheckpoints,
                columns: new[] { ColumnTicketId, ColumnGraphId, ColumnStatus });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_CreatedAt",
                table: TableErrorLogs,
                column: ColumnCreatedAt);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_EntityType_EntityId",
                table: TableErrorLogs,
                columns: new[] { ColumnEntityType, ColumnEntityId });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_IsResolved",
                table: TableErrorLogs,
                column: ColumnIsResolved);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Severity",
                table: TableErrorLogs,
                column: ColumnSeverity);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_TenantId",
                table: TableErrorLogs,
                column: ColumnTenantId);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_TenantId_IsResolved_Severity",
                table: TableErrorLogs,
                columns: new[] { ColumnTenantId, ColumnIsResolved, ColumnSeverity });

            migrationBuilder.CreateIndex(
                name: "IX_PlanReviews_ReviewerId",
                table: TablePlanReviews,
                column: ColumnReviewerId);

            migrationBuilder.CreateIndex(
                name: "IX_PlanReviews_Status",
                table: TablePlanReviews,
                column: ColumnStatus);

            migrationBuilder.CreateIndex(
                name: "IX_PlanReviews_TicketId",
                table: TablePlanReviews,
                column: ColumnTicketId);

            migrationBuilder.CreateIndex(
                name: "IX_PlanReviews_TicketId_ReviewerId",
                table: TablePlanReviews,
                columns: new[] { ColumnTicketId, ColumnReviewerId },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_AuthorId",
                table: TableReviewComments,
                column: ColumnAuthorId);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_CreatedAt",
                table: TableReviewComments,
                column: ColumnCreatedAt,
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewComments_TicketId",
                table: TableReviewComments,
                column: ColumnTicketId);

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_GeneratedAt",
                table: TableTicketUpdates,
                column: ColumnGeneratedAt);

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_IsApproved_PostedAt",
                table: TableTicketUpdates,
                columns: new[] { ColumnIsApproved, ColumnPostedAt },
                filter: "PostedAt IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId",
                table: TableTicketUpdates,
                column: ColumnTicketId);

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId_IsApproved",
                table: TableTicketUpdates,
                columns: new[] { ColumnTicketId, ColumnIsApproved });

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId_IsDraft",
                table: TableTicketUpdates,
                columns: new[] { ColumnTicketId, ColumnIsDraft });

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId_Version",
                table: TableTicketUpdates,
                columns: new[] { ColumnTicketId, ColumnVersion });

            migrationBuilder.CreateIndex(
                name: "IX_TicketUpdates_TicketId1",
                table: TableTicketUpdates,
                column: ColumnTicketId1);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: TableUsers,
                column: ColumnEmail);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: TableUsers,
                column: ColumnTenantId);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: TableUsers,
                columns: new[] { ColumnTenantId, ColumnEmail },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: TableAgentPromptTemplates);

            migrationBuilder.DropTable(
                name: TableCheckpoints);

            migrationBuilder.DropTable(
                name: TableErrorLogs);

            migrationBuilder.DropTable(
                name: TablePlanReviews);

            migrationBuilder.DropTable(
                name: TableReviewComments);

            migrationBuilder.DropTable(
                name: TableTicketUpdates);

            migrationBuilder.DropTable(
                name: TableUsers);

            migrationBuilder.DropColumn(
                name: ColumnDuration,
                table: TableWorkflowEvents);

            migrationBuilder.DropColumn(
                name: ColumnError,
                table: TableWorkflowEvents);

            migrationBuilder.DropColumn(
                name: ColumnGraphId,
                table: TableWorkflowEvents);

            migrationBuilder.DropColumn(
                name: ColumnQuestion,
                table: TableWorkflowEvents);

            migrationBuilder.DropColumn(
                name: ColumnState,
                table: TableWorkflowEvents);

            migrationBuilder.DropColumn(
                name: ColumnExternalTicketId,
                table: TableTickets);

            migrationBuilder.DropColumn(
                name: ColumnLastSyncedAt,
                table: TableTickets);

            migrationBuilder.DropColumn(
                name: ColumnRequiredApprovalCount,
                table: TableTickets);

            migrationBuilder.DropColumn(
                name: ColumnSource,
                table: TableTickets);

            migrationBuilder.AlterColumn<string>(
                name: ColumnQuestionId,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: ColumnPullRequestUrl,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<int>(
                name: ColumnPullRequestNumber,
                table: TableWorkflowEvents,
                type: TypeInteger,
                nullable: true,
                oldClrType: typeof(int),
                oldType: TypeInteger);

            migrationBuilder.AlterColumn<string>(
                name: ColumnBranchName,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: ColumnAnswerText,
                table: TableWorkflowEvents,
                type: TypeText,
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: ColumnDescription,
                table: TableTickets,
                type: TypeText,
                maxLength: 10000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: TypeText,
                oldMaxLength: 10000);
        }
    }
}
