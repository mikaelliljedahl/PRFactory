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
    /// Additional metadata as JSON
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
