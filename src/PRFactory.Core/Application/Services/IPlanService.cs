using PRFactory.Core.Application.DTOs;
using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for retrieving implementation plans.
/// Plans are stored as markdown files in git branches.
/// </summary>
public interface IPlanService
{
    /// <summary>
    /// Gets the implementation plan for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The plan content and metadata, or null if no plan exists</returns>
    Task<PlanInfo?> GetPlanAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all revisions for a ticket
    /// </summary>
    Task<List<PlanRevisionDto>> GetPlanRevisionsAsync(Guid ticketId);

    /// <summary>
    /// Get a specific revision
    /// </summary>
    Task<PlanRevisionDto?> GetPlanRevisionAsync(Guid revisionId);

    /// <summary>
    /// Create a snapshot of the current plan as a revision
    /// </summary>
    Task<PlanRevisionDto> CreateRevisionAsync(
        Guid ticketId,
        PlanRevisionReason reason,
        Guid? createdByUserId = null);

    /// <summary>
    /// Compare two revisions and return diff
    /// </summary>
    Task<PlanRevisionComparisonDto> CompareRevisionsAsync(Guid revision1Id, Guid revision2Id);
}

/// <summary>
/// Information about an implementation plan
/// </summary>
public class PlanInfo
{
    /// <summary>
    /// The branch name where the plan is stored
    /// </summary>
    public required string BranchName { get; init; }

    /// <summary>
    /// Path to the plan markdown file in the repository
    /// </summary>
    public string? MarkdownPath { get; init; }

    /// <summary>
    /// The plan content (markdown)
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// When the plan was created (from ticket)
    /// </summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// Whether the plan has been approved
    /// </summary>
    public bool IsApproved { get; init; }

    /// <summary>
    /// When the plan was approved
    /// </summary>
    public DateTime? ApprovedAt { get; init; }
}
