using Bunit;
using Xunit;
using PRFactory.Web.Components.Workflows;
using PRFactory.Web.Models;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Workflows;

/// <summary>
/// Tests for EventDetail component
/// Verifies rendering of event information including event type, timestamp, payload, and state transitions.
/// </summary>
public class EventDetailTests : TestContext
{
    private WorkflowEventDto CreateTestEvent(
        EventSeverity severity = EventSeverity.Info,
        string eventType = "TestEvent",
        string? ticketKey = "PROJ-123")
    {
        return new WorkflowEventDto
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            TicketKey = ticketKey,
            OccurredAt = DateTime.UtcNow.AddHours(-1),
            EventType = eventType,
            Description = "Test event description",
            Severity = severity,
            Icon = "circle",
            Reason = "Test reason",
            Metadata = new Dictionary<string, object>
            {
                { "userId", "user123" },
                { "action", "updated" }
            }
        };
    }

    [Fact]
    public void Render_WithNullEvent_ShowsNoEventMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, null));

        // Assert
        Assert.Contains("Select an event to view details", cut.Markup);
        Assert.Contains("info-circle", cut.Markup);
    }

    [Fact]
    public void Render_WithEvent_DisplaysEventTypeInBadge()
    {
        // Arrange
        var @event = CreateTestEvent(eventType: "WorkflowStateChanged");

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("WorkflowStateChanged", cut.Markup);
        Assert.Contains("badge bg-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithEvent_DisplaysOccurredTimestamp()
    {
        // Arrange
        var occurredTime = new DateTime(2024, 11, 15, 10, 30, 0, DateTimeKind.Utc);
        var @event = CreateTestEvent();
        @event.OccurredAt = occurredTime;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("November 15, 2024", cut.Markup);
        Assert.Contains("clock", cut.Markup); // Clock icon
    }

    [Fact]
    public void Render_WithTicketKey_DisplaysTicketLink()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var @event = CreateTestEvent(ticketKey: "PROJ-456");
        @event.TicketId = ticketId;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains($"/tickets/{ticketId}", cut.Markup);
        Assert.Contains("PROJ-456", cut.Markup);
        Assert.Contains("ticket-perforated", cut.Markup); // Ticket icon
    }

    [Fact]
    public void Render_WithoutTicketKey_DoesNotDisplayTicketLink()
    {
        // Arrange
        var @event = CreateTestEvent(ticketKey: null);

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.DoesNotContain("ticket-perforated", cut.Markup);
        Assert.DoesNotContain("/tickets/", cut.Markup);
    }

    [Fact]
    public void Render_WithSuccessSeverity_DisplaysSuccessBadgeClass()
    {
        // Arrange
        var @event = CreateTestEvent(severity: EventSeverity.Success);

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("badge bg-success", cut.Markup);
        Assert.Contains("Success", cut.Markup);
    }

    [Fact]
    public void Render_WithErrorSeverity_DisplaysDangerBadgeClass()
    {
        // Arrange
        var @event = CreateTestEvent(severity: EventSeverity.Error);

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("badge bg-danger", cut.Markup);
        Assert.Contains("Error", cut.Markup);
    }

    [Fact]
    public void Render_WithWarningSeverity_DisplaysWarningBadgeClass()
    {
        // Arrange
        var @event = CreateTestEvent(severity: EventSeverity.Warning);

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("badge bg-warning text-dark", cut.Markup);
        Assert.Contains("Warning", cut.Markup);
    }

    [Fact]
    public void Render_WithDescription_DisplaysDescriptionInLightBox()
    {
        // Arrange
        var description = "This is a detailed event description";
        var @event = CreateTestEvent();
        @event.Description = description;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("Description:", cut.Markup);
        Assert.Contains(description, cut.Markup);
        Assert.Contains("bg-light rounded", cut.Markup);
    }

    [Fact]
    public void Render_WithStateTransition_DisplaysFromAndToStates()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.FromState = WorkflowState.Triggered;
        @event.ToState = WorkflowState.Analyzing;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("State Transition:", cut.Markup);
        Assert.Contains("arrow-right", cut.Markup); // Arrow icon
    }

    [Fact]
    public void Render_WithoutStateTransition_DoesNotDisplayStateSection()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.FromState = null;
        @event.ToState = null;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.DoesNotContain("State Transition:", cut.Markup);
    }

    [Fact]
    public void Render_WithReason_DisplaysReasonInLightBox()
    {
        // Arrange
        var reason = "Event was triggered due to user action";
        var @event = CreateTestEvent();
        @event.Reason = reason;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("Reason:", cut.Markup);
        Assert.Contains(reason, cut.Markup);
    }

    [Fact]
    public void Render_WithoutReason_DoesNotDisplayReasonSection()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.Reason = null;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.DoesNotContain("Reason:", cut.Markup);
    }

    [Fact]
    public void Render_WithMetadata_DisplaysFormattedMetadataKeyValues()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.Metadata = new Dictionary<string, object>
        {
            { "userId", "user123" },
            { "actionType", "update" }
        };

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("Additional Information:", cut.Markup);
        // Metadata keys should be formatted from camelCase to Title Case
        Assert.Contains("user123", cut.Markup);
        Assert.Contains("update", cut.Markup);
    }

    [Fact]
    public void Render_WithoutMetadata_DoesNotDisplayAdditionalInformationSection()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.Metadata = new Dictionary<string, object>();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.DoesNotContain("Additional Information:", cut.Markup);
    }

    [Fact]
    public void Render_WithMetadataJson_DisplaysRawJsonTextArea()
    {
        // Arrange
        var metadataJson = """{"userId":"user123","timestamp":"2024-11-15T10:30:00Z"}""";
        var @event = CreateTestEvent();
        @event.MetadataJson = metadataJson;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("Raw Metadata (JSON):", cut.Markup);
        // TextArea should be read-only
        var textarea = cut.FindComponent<Radzen.Blazor.RadzenTextArea>();
        Assert.NotNull(textarea);
    }

    [Fact]
    public void Render_DisplaysEventId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var @event = CreateTestEvent();
        @event.Id = eventId;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("Event ID:", cut.Markup);
        Assert.Contains(eventId.ToString(), cut.Markup);
        Assert.Contains("key", cut.Markup); // Key icon
    }

    [Fact]
    public void Render_WithCardTitle_DisplaysEventDetailsHeader()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, @event));

        // Assert
        Assert.Contains("Event Details", cut.Markup);
        Assert.Contains("info-circle", cut.Markup); // Header icon
    }
}
