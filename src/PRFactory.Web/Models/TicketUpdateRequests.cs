using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PRFactory.Web.Models;

/// <summary>
/// Request to approve a ticket update
/// </summary>
public class ApproveTicketUpdateRequest
{
    /// <summary>
    /// Optional comments from the approver
    /// </summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    /// <summary>
    /// User who approved (for audit trail)
    /// </summary>
    [JsonPropertyName("approvedBy")]
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// Request to reject a ticket update
/// </summary>
public class RejectTicketUpdateRequest
{
    /// <summary>
    /// Reason for rejection (required)
    /// </summary>
    [Required]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// User who rejected (for audit trail)
    /// </summary>
    [JsonPropertyName("rejectedBy")]
    public string? RejectedBy { get; set; }

    /// <summary>
    /// Whether to regenerate the ticket update
    /// </summary>
    [JsonPropertyName("regenerate")]
    public bool Regenerate { get; set; } = true;
}

/// <summary>
/// Request to update a ticket update (manual edits)
/// </summary>
public class UpdateTicketUpdateRequest
{
    /// <summary>
    /// Updated title
    /// </summary>
    [JsonPropertyName("updatedTitle")]
    public string? UpdatedTitle { get; set; }

    /// <summary>
    /// Updated description
    /// </summary>
    [JsonPropertyName("updatedDescription")]
    public string? UpdatedDescription { get; set; }

    /// <summary>
    /// Updated acceptance criteria
    /// </summary>
    [JsonPropertyName("acceptanceCriteria")]
    public string? AcceptanceCriteria { get; set; }
}

/// <summary>
/// Response for ticket update operations
/// </summary>
public class TicketUpdateResponse
{
    /// <summary>
    /// Ticket update ID
    /// </summary>
    [JsonPropertyName("ticketUpdateId")]
    public Guid TicketUpdateId { get; set; }

    /// <summary>
    /// Ticket ID
    /// </summary>
    [JsonPropertyName("ticketId")]
    public Guid TicketId { get; set; }

    /// <summary>
    /// Ticket key (e.g., "PROJ-123")
    /// </summary>
    [JsonPropertyName("ticketKey")]
    public string? TicketKey { get; set; }

    /// <summary>
    /// Updated title
    /// </summary>
    [JsonPropertyName("updatedTitle")]
    public string UpdatedTitle { get; set; } = string.Empty;

    /// <summary>
    /// Updated description
    /// </summary>
    [JsonPropertyName("updatedDescription")]
    public string UpdatedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Acceptance criteria
    /// </summary>
    [JsonPropertyName("acceptanceCriteria")]
    public string AcceptanceCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Version number
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// Whether this is a draft
    /// </summary>
    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; }

    /// <summary>
    /// Whether this has been approved
    /// </summary>
    [JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Rejection reason (if rejected)
    /// </summary>
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// When generated
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When approved
    /// </summary>
    [JsonPropertyName("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// When posted to ticket system
    /// </summary>
    [JsonPropertyName("postedAt")]
    public DateTime? PostedAt { get; set; }

    /// <summary>
    /// Success criteria
    /// </summary>
    [JsonPropertyName("successCriteria")]
    public List<SuccessCriterionDto> SuccessCriteria { get; set; } = new();
}

/// <summary>
/// Response for ticket update approval/rejection operations
/// </summary>
public class TicketUpdateOperationResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Updated ticket update information
    /// </summary>
    [JsonPropertyName("ticketUpdate")]
    public TicketUpdateResponse? TicketUpdate { get; set; }

    /// <summary>
    /// Current ticket workflow state
    /// </summary>
    [JsonPropertyName("ticketState")]
    public string? TicketState { get; set; }
}
