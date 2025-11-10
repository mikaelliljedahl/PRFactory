using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PRFactory.Api.Models;

/// <summary>
/// Request to approve an implementation plan
/// </summary>
public class ApprovePlanRequest
{
    /// <summary>
    /// User's approval decision
    /// </summary>
    [Required]
    [JsonPropertyName("approved")]
    public required bool Approved { get; set; }

    /// <summary>
    /// Optional comments from the user
    /// </summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    /// <summary>
    /// User who approved/rejected (for audit trail)
    /// </summary>
    [JsonPropertyName("approvedBy")]
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// Request to reject an implementation plan
/// </summary>
public class RejectPlanRequest
{
    /// <summary>
    /// Reason for rejection
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
    /// Whether to restart the planning process
    /// </summary>
    [JsonPropertyName("restartPlanning")]
    public bool? RestartPlanning { get; set; }
}

/// <summary>
/// Response for approval/rejection operations
/// </summary>
public class ApprovalResponse
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
    /// Updated ticket status
    /// </summary>
    [JsonPropertyName("ticketStatus")]
    public TicketStatusResponse? TicketStatus { get; set; }
}

/// <summary>
/// Request for listing tickets
/// </summary>
public class ListTicketsRequest
{
    /// <summary>
    /// Filter by state
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Filter by repository
    /// </summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response for listing tickets
/// </summary>
public class ListTicketsResponse
{
    /// <summary>
    /// List of tickets
    /// </summary>
    [JsonPropertyName("tickets")]
    public List<TicketStatusResponse> Tickets { get; set; } = new();

    /// <summary>
    /// Total count of tickets matching the filter
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
