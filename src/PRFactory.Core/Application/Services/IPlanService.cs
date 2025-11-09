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
