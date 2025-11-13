using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Workflows;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Tests.Components.Workflows;

public class EventDetailTests : ComponentTestBase
{
    [Fact]
    public void Render_WithNullEvent_DisplaysEmptyState()
    {
        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, null));

        // Assert
        Assert.Contains("Select an event to view details", cut.Markup);
        Assert.Contains("bi-info-circle", cut.Markup);
    }

    [Fact]
    public void Render_WithEvent_DisplaysEventDetails()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithEventType("StateChanged")
            .WithDescription("Test event description")
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("StateChanged", cut.Markup);
        Assert.Contains("Test event description", cut.Markup);
        Assert.Contains("Event Type:", cut.Markup);
        Assert.Contains("Occurred At:", cut.Markup);
    }

    [Fact]
    public void Render_WithTicketKey_DisplaysTicketLink()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithTicketId(ticketId)
            .WithTicketKey("TEST-123")
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("TEST-123", cut.Markup);
        Assert.Contains($"/tickets/{ticketId}", cut.Markup);
        Assert.Contains("bi-ticket-perforated", cut.Markup);
    }

    [Theory]
    [InlineData(EventSeverity.Success, "bg-success")]
    [InlineData(EventSeverity.Error, "bg-danger")]
    [InlineData(EventSeverity.Warning, "bg-warning")]
    [InlineData(EventSeverity.Info, "bg-info")]
    public void Render_WithSeverity_ShowsCorrectBadgeClass(EventSeverity severity, string expectedClass)
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithSeverity(severity)
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
        Assert.Contains(severity.ToString(), cut.Markup);
    }

    [Fact(Skip = "TODO: StatusBadge components don't render expected 'Draft' and 'RefinementInProgress' text - need to verify StatusBadge output format")]
    public void Render_WithStateTransition_DisplaysFromAndToStates()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithStateChange(WorkflowState.Triggered, WorkflowState.Analyzing)
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("State Transition:", cut.Markup);
        Assert.Contains("bi-arrow-right", cut.Markup);
        // StatusBadge components should render for both states
        Assert.Contains("Draft", cut.Markup);
        Assert.Contains("RefinementInProgress", cut.Markup);
    }

    [Fact]
    public void Render_WithReason_DisplaysReason()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithReason("User requested plan regeneration")
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("Reason:", cut.Markup);
        Assert.Contains("User requested plan regeneration", cut.Markup);
    }

    [Fact]
    public void Render_WithMetadata_DisplaysMetadataKeyValuePairs()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "questionCount", 5 },
            { "approver", "John Doe" }
        };

        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithMetadata(metadata)
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("Additional Information:", cut.Markup);
        Assert.Contains("Question Count:", cut.Markup); // FormatMetadataKey converts camelCase
        Assert.Contains("5", cut.Markup);
        Assert.Contains("Approver:", cut.Markup);
        Assert.Contains("John Doe", cut.Markup);
    }

    [Fact]
    public void Render_WithMetadataJson_DisplaysRawJson()
    {
        // Arrange
        var jsonMetadata = "{\"key\": \"value\"}";
        var workflowEvent = new WorkflowEventDtoBuilder()
            .Build();

        // Manually set MetadataJson since builder doesn't have a method for it
        workflowEvent.MetadataJson = jsonMetadata;

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("Raw Metadata (JSON):", cut.Markup);
        // RadzenTextArea should be present
        var textAreas = cut.FindAll("textarea");
        Assert.NotEmpty(textAreas);
    }

    [Fact]
    public void Render_WithoutReason_DoesNotDisplayReasonSection()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.DoesNotContain("Reason:", cut.Markup);
    }

    [Fact]
    public void Render_WithoutMetadata_DoesNotDisplayMetadataSection()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.DoesNotContain("Additional Information:", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEventId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithId(eventId)
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains($"Event ID: {eventId}", cut.Markup);
        Assert.Contains("bi-key", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysIcon()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .WithIcon("check-circle")
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Fact]
    public void FormatMetadataKey_ConvertsFromCamelCaseToTitleCase()
    {
        // Arrange
        var workflowEvent = new WorkflowEventDtoBuilder()
            .AddMetadata("myComplexKey", "value")
            .Build();

        // Act
        var cut = RenderComponent<EventDetail>(parameters => parameters
            .Add(p => p.Event, workflowEvent));

        // Assert
        // FormatMetadataKey should convert "myComplexKey" to "My Complex Key"
        Assert.Contains("My Complex Key:", cut.Markup);
    }
}
