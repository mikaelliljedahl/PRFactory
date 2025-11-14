using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for plan revision information
/// </summary>
public class PlanRevisionDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public int RevisionNumber { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string MarkdownPath { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = "AI Generated";
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Maps a PlanRevision entity to a DTO
    /// </summary>
    public static PlanRevisionDto FromEntity(PlanRevision revision)
    {
        if (revision == null)
            throw new ArgumentNullException(nameof(revision));

        return new PlanRevisionDto
        {
            Id = revision.Id,
            TicketId = revision.TicketId,
            RevisionNumber = revision.RevisionNumber,
            BranchName = revision.BranchName,
            MarkdownPath = revision.MarkdownPath,
            CommitHash = revision.CommitHash,
            Content = revision.Content,
            CreatedAt = revision.CreatedAt,
            CreatedByUserId = revision.CreatedByUserId,
            CreatedByName = revision.CreatedBy?.DisplayName ?? "AI Generated",
            Reason = revision.Reason.ToString()
        };
    }
}

/// <summary>
/// DTO for comparing two plan revisions
/// </summary>
public class PlanRevisionComparisonDto
{
    public PlanRevisionDto Revision1 { get; set; } = null!;
    public PlanRevisionDto Revision2 { get; set; } = null!;
    public List<DiffLine> DiffLines { get; set; } = new();
}

/// <summary>
/// Represents a single line in a diff comparison
/// </summary>
public class DiffLine
{
    public int LineNumber { get; set; }
    public DiffLineType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? OldContent { get; set; } // For Modified lines
}

/// <summary>
/// Type of difference in a diff line
/// </summary>
public enum DiffLineType
{
    Unchanged,
    Added,
    Removed,
    Modified
}
