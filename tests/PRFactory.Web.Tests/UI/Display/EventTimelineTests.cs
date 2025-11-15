using Bunit;
using Xunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for EventTimeline component
/// </summary>
public class EventTimelineTests : TestContext
{
    private List<WorkflowEventDto> CreateTestEvents()
    {
        return new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = Guid.NewGuid(),
                TicketKey = "PROJ-123",
                EventType = "WorkflowStateChanged",
                Description = "Workflow started",
                OccurredAt = DateTime.UtcNow.AddHours(-2),
                Severity = EventSeverity.Info,
                Icon = "play-circle"
            },
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = Guid.NewGuid(),
                TicketKey = "PROJ-123",
                EventType = "TicketUpdateGenerated",
                Description = "Ticket update generated",
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                Severity = EventSeverity.Success,
                Icon = "check-circle"
            }
        };
    }

    [Fact]
    public void Render_WithEvents_DisplaysTimeline()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("timeline", cut.Markup);
        Assert.Contains("Workflow started", cut.Markup);
        Assert.Contains("Ticket update generated", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyEvents_ShowsEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, new List<WorkflowEventDto>()));

        // Assert
        Assert.Contains("No events to display", cut.Markup);
    }

    [Fact]
    public void Render_WithNullEvents_ShowsEmptyMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, null!));

        // Assert
        Assert.Contains("No events to display", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEventIcons()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("bi-play-circle", cut.Markup);
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEventTimestamps()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        foreach (var evt in events)
        {
            var formattedTime = evt.OccurredAt.ToString("MMM dd, yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            Assert.Contains(formattedTime, cut.Markup);
        }
    }

    [Fact]
    public void Render_DisplaysTicketKeys()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("PROJ-123", cut.Markup);
    }

    [Theory]
    [InlineData(EventSeverity.Success, "success")]
    [InlineData(EventSeverity.Error, "error")]
    [InlineData(EventSeverity.Warning, "warning")]
    [InlineData(EventSeverity.Info, "info")]
    public void Render_WithDifferentSeverities_AppliesCorrectMarkerClass(EventSeverity severity, string expectedClass)
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = Guid.NewGuid(),
                EventType = "Test",
                Description = "Test event",
                OccurredAt = DateTime.UtcNow,
                Severity = severity,
                Icon = "circle"
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WhenShowDetailsTrue_ShowsViewDetailsButton()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.ShowDetails, true)
            .Add(p => p.OnEventClick, _ => { }));

        // Assert
        Assert.Contains("View Details", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowDetailsFalse_DoesNotShowViewDetailsButton()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.ShowDetails, false));

        // Assert
        Assert.DoesNotContain("View Details", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesAdditionalClass()
    {
        // Arrange
        var events = CreateTestEvents();
        var additionalClass = "my-custom-timeline";

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.AdditionalClass, additionalClass));

        // Assert
        var timeline = cut.Find(".timeline");
        Assert.Contains(additionalClass, timeline.ClassName);
    }

    [Fact]
    public void Render_DisplaysEventTypes()
    {
        // Arrange
        var events = CreateTestEvents();

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("WorkflowStateChanged", cut.Markup);
        Assert.Contains("TicketUpdateGenerated", cut.Markup);
    }
}
