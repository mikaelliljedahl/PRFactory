using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating WorkflowEventDto instances for testing
/// </summary>
public class WorkflowEventDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _ticketId = Guid.NewGuid();
    private string? _ticketKey = "TEST-123";
    private DateTime _occurredAt = DateTime.UtcNow;
    private string _eventType = "WorkflowStateChanged";
    private string _description = "Workflow state changed";
    private WorkflowState? _fromState = null;
    private WorkflowState? _toState = null;
    private string? _reason = null;
    private EventSeverity _severity = EventSeverity.Info;
    private string _icon = "circle";
    private readonly string? _metadataJson = null;
    private Dictionary<string, object> _metadata = new();

    public WorkflowEventDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public WorkflowEventDtoBuilder WithTicketId(Guid ticketId)
    {
        _ticketId = ticketId;
        return this;
    }

    public WorkflowEventDtoBuilder WithTicketKey(string ticketKey)
    {
        _ticketKey = ticketKey;
        return this;
    }

    public WorkflowEventDtoBuilder WithOccurredAt(DateTime occurredAt)
    {
        _occurredAt = occurredAt;
        return this;
    }

    public WorkflowEventDtoBuilder WithEventType(string eventType)
    {
        _eventType = eventType;
        return this;
    }

    public WorkflowEventDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public WorkflowEventDtoBuilder WithStateChange(WorkflowState fromState, WorkflowState toState)
    {
        _fromState = fromState;
        _toState = toState;
        _eventType = "WorkflowStateChanged";
        _description = $"State changed from {fromState} to {toState}";
        return this;
    }

    public WorkflowEventDtoBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    public WorkflowEventDtoBuilder WithSeverity(EventSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public WorkflowEventDtoBuilder WithIcon(string icon)
    {
        _icon = icon;
        return this;
    }

    public WorkflowEventDtoBuilder WithMetadata(Dictionary<string, object> metadata)
    {
        _metadata = metadata;
        return this;
    }

    public WorkflowEventDtoBuilder AddMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public WorkflowEventDto Build()
    {
        return new WorkflowEventDto
        {
            Id = _id,
            TicketId = _ticketId,
            TicketKey = _ticketKey,
            OccurredAt = _occurredAt,
            EventType = _eventType,
            Description = _description,
            FromState = _fromState,
            ToState = _toState,
            Reason = _reason,
            Severity = _severity,
            Icon = _icon,
            MetadataJson = _metadataJson,
            Metadata = _metadata
        };
    }

    /// <summary>
    /// Creates a workflow state changed event
    /// </summary>
    public static WorkflowEventDtoBuilder StateChanged(WorkflowState fromState, WorkflowState toState)
    {
        return new WorkflowEventDtoBuilder()
            .WithStateChange(fromState, toState)
            .WithSeverity(EventSeverity.Info)
            .WithIcon("arrow-right");
    }

    /// <summary>
    /// Creates an error event
    /// </summary>
    public static WorkflowEventDtoBuilder Error(string errorMessage)
    {
        return new WorkflowEventDtoBuilder()
            .WithEventType("Error")
            .WithDescription(errorMessage)
            .WithSeverity(EventSeverity.Error)
            .WithIcon("exclamation-circle");
    }

    /// <summary>
    /// Creates a success event
    /// </summary>
    public static WorkflowEventDtoBuilder Success(string message)
    {
        return new WorkflowEventDtoBuilder()
            .WithEventType("Success")
            .WithDescription(message)
            .WithSeverity(EventSeverity.Success)
            .WithIcon("check-circle");
    }

    /// <summary>
    /// Creates a warning event
    /// </summary>
    public static WorkflowEventDtoBuilder Warning(string message)
    {
        return new WorkflowEventDtoBuilder()
            .WithEventType("Warning")
            .WithDescription(message)
            .WithSeverity(EventSeverity.Warning)
            .WithIcon("exclamation-triangle");
    }

    /// <summary>
    /// Creates a question added event
    /// </summary>
    public static WorkflowEventDtoBuilder QuestionAdded(int questionCount)
    {
        return new WorkflowEventDtoBuilder()
            .WithEventType("QuestionAdded")
            .WithDescription($"{questionCount} question(s) added")
            .WithSeverity(EventSeverity.Info)
            .WithIcon("question-circle")
            .AddMetadata("questionCount", questionCount);
    }

    /// <summary>
    /// Creates a plan approved event
    /// </summary>
    public static WorkflowEventDtoBuilder PlanApproved(string approverName)
    {
        return new WorkflowEventDtoBuilder()
            .WithEventType("PlanApproved")
            .WithDescription($"Plan approved by {approverName}")
            .WithSeverity(EventSeverity.Success)
            .WithIcon("check-circle")
            .AddMetadata("approver", approverName);
    }

    /// <summary>
    /// Creates a plan rejected event
    /// </summary>
    public static WorkflowEventDtoBuilder PlanRejected(string reason)
    {
        return new WorkflowEventDtoBuilder()
            .WithEventType("PlanRejected")
            .WithDescription("Plan rejected")
            .WithReason(reason)
            .WithSeverity(EventSeverity.Warning)
            .WithIcon("times-circle");
    }
}
