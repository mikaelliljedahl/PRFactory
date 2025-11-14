namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a snapshot of an implementation plan at a point in time.
/// Git is the source of truth, this database record stores metadata and content snapshot.
/// </summary>
public class PlanRevision
{
    /// <summary>
    /// Unique identifier for this plan revision
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket this revision belongs to
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// Sequential revision number (1, 2, 3...)
    /// </summary>
    public int RevisionNumber { get; private set; }

    /// <summary>
    /// Git branch where plan is stored
    /// </summary>
    public string BranchName { get; private set; } = string.Empty;

    /// <summary>
    /// Path to markdown file in repository
    /// </summary>
    public string MarkdownPath { get; private set; } = string.Empty;

    /// <summary>
    /// Git commit hash (links database revision to git history)
    /// </summary>
    public string CommitHash { get; private set; } = string.Empty;

    /// <summary>
    /// Full markdown content (snapshot)
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// When this revision was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Who triggered this revision (NULL for AI-generated)
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    /// <summary>
    /// Why this revision was created
    /// </summary>
    public PlanRevisionReason Reason { get; private set; }

    // Navigation properties
    public Ticket Ticket { get; private set; } = null!;
    public User? CreatedBy { get; private set; }

    private PlanRevision() { } // EF Core

    /// <summary>
    /// Creates a new plan revision snapshot
    /// </summary>
    public static PlanRevision Create(
        Guid ticketId,
        int revisionNumber,
        string branchName,
        string markdownPath,
        string commitHash,
        string content,
        PlanRevisionReason reason,
        Guid? createdByUserId = null)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID is required", nameof(ticketId));

        if (revisionNumber <= 0)
            throw new ArgumentException("Revision number must be positive", nameof(revisionNumber));

        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name is required", nameof(branchName));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        return new PlanRevision
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            RevisionNumber = revisionNumber,
            BranchName = branchName,
            MarkdownPath = markdownPath ?? string.Empty,
            CommitHash = commitHash ?? string.Empty,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            Reason = reason
        };
    }
}

/// <summary>
/// Reason for creating a plan revision
/// </summary>
public enum PlanRevisionReason
{
    /// <summary>
    /// First plan generated
    /// </summary>
    Initial = 1,

    /// <summary>
    /// Plan refined with instructions (keep structure)
    /// </summary>
    Refined = 2,

    /// <summary>
    /// Plan regenerated from scratch
    /// </summary>
    Regenerated = 3
}
