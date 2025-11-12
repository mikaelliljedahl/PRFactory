using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRFactory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds code review configuration properties to TenantConfiguration.
    ///
    /// Properties added:
    /// - EnableAutoCodeReview (bool)
    /// - CodeReviewLlmProviderId (Guid?)
    /// - ImplementationLlmProviderId (Guid?)
    /// - PlanningLlmProviderId (Guid?)
    /// - AnalysisLlmProviderId (Guid?)
    /// - MaxCodeReviewIterations (int)
    /// - AutoApproveIfNoIssues (bool)
    /// - RequireHumanApprovalAfterReview (bool)
    ///
    /// Note: TenantConfiguration is stored as JSON, so no schema migration needed.
    /// EF Core automatically handles JSON structure evolution.
    /// </summary>
    public partial class AddCodeReviewConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No database schema changes needed.
            // TenantConfiguration is stored as JSONB column and EF Core handles structure evolution.
            // The new properties will be automatically serialized/deserialized.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No database schema changes to revert.
            // JSON column structure remains flexible.
        }
    }
}
