using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Models;

/// <summary>
/// Data transfer object for ticket updates
/// </summary>
public class TicketUpdateDto
{
    /// <summary>
    /// Unique identifier for the ticket update
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ticket this update belongs to
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Refined/updated title for the ticket
    /// </summary>
    public string UpdatedTitle { get; set; } = string.Empty;

    /// <summary>
    /// Refined/updated description for the ticket
    /// </summary>
    public string UpdatedDescription { get; set; } = string.Empty;

    /// <summary>
    /// List of success criteria that define what needs to be achieved
    /// </summary>
    public List<SuccessCriterionDto> SuccessCriteria { get; set; } = new();

    /// <summary>
    /// Acceptance criteria in a structured format (markdown or bullet points)
    /// </summary>
    public string AcceptanceCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Version number for tracking regenerations (starts at 1)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Indicates whether this is a draft (not yet approved)
    /// </summary>
    public bool IsDraft { get; set; } = true;

    /// <summary>
    /// Indicates whether this update has been approved by the user
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Reason for rejection if the update was rejected (null if not rejected)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// When the update was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When the update was approved (null if not yet approved)
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// When the update was posted to the ticket system (null if not yet posted)
    /// </summary>
    public DateTime? PostedAt { get; set; }
}

/// <summary>
/// DTO for success criterion
/// </summary>
public class SuccessCriterionDto
{
    /// <summary>
    /// Category of the success criterion
    /// </summary>
    public SuccessCriterionCategory Category { get; set; }

    /// <summary>
    /// Detailed description of what needs to be achieved
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority level: 0=must-have, 1=should-have, 2=nice-to-have
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Indicates whether this criterion can be objectively tested/verified
    /// </summary>
    public bool IsTestable { get; set; }

    /// <summary>
    /// Gets a human-readable priority label
    /// </summary>
    public string PriorityLabel => Priority switch
    {
        0 => "Must-Have",
        1 => "Should-Have",
        2 => "Nice-to-Have",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the priority badge CSS class
    /// </summary>
    public string PriorityBadgeClass => Priority switch
    {
        0 => "badge bg-danger",
        1 => "badge bg-warning",
        2 => "badge bg-info",
        _ => "badge bg-secondary"
    };

    /// <summary>
    /// Gets the category display name
    /// </summary>
    public string CategoryDisplay => Category.ToString();
}
