using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing a workflow event for display in the UI
/// </summary>
public class WorkflowEventDto
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ticket this event is associated with
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Ticket key (e.g., "PROJ-123") for display
    /// </summary>
    public string? TicketKey { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// The type of event (e.g., "WorkflowStateChanged", "QuestionAdded")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the event
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Previous state (for WorkflowStateChanged events)
    /// </summary>
    public WorkflowState? FromState { get; set; }

    /// <summary>
    /// New state (for WorkflowStateChanged events)
    /// </summary>
    public WorkflowState? ToState { get; set; }

    /// <summary>
    /// Reason for the event (if applicable)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Severity level for visual indicators (success, error, warning, info)
    /// </summary>
    public EventSeverity Severity { get; set; }

    /// <summary>
    /// Icon name for display
    /// </summary>
    public string Icon { get; set; } = "circle";

    /// <summary>
    /// Additional metadata as JSON string
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Additional metadata as dictionary (for UI binding)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Event severity levels for visual indicators
/// </summary>
public enum EventSeverity
{
    Info,
    Success,
    Warning,
    Error
}
