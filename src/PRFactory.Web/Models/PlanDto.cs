namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing an implementation plan
/// </summary>
public class PlanDto
{
    /// <summary>
    /// The branch name where the plan is stored
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Path to the plan markdown file in the repository
    /// </summary>
    public string? MarkdownPath { get; set; }

    /// <summary>
    /// The plan content (markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the plan was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Whether the plan has been approved
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// When the plan was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
}
