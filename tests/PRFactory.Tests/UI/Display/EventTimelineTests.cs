using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Models;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class EventTimelineTests : ComponentTestBase
{
    [Fact]
    public void Render_WithNullEvents_ShowsNoEventsMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, (List<WorkflowEventDto>?)null));

        // Assert
        Assert.Contains("No events to display", cut.Markup);
        var timelineItems = cut.FindAll(".timeline-item");
        Assert.Empty(timelineItems);
    }

    [Fact]
    public void Render_WithEmptyEvents_ShowsNoEventsMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, new List<WorkflowEventDto>()));

        // Assert
        Assert.Contains("No events to display", cut.Markup);
        Assert.Contains("bi-calendar-x", cut.Markup);
    }

    [Fact]
    public void Render_WithEvents_DisplaysTimeline()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                EventType = "TestEvent",
                Description = "Test Description",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("timeline", cut.Markup);
        Assert.Contains("TestEvent", cut.Markup);
        Assert.Contains("Test Description", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEventIcon()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "check-circle",
                Severity = EventSeverity.Success
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEventTime()
    {
        // Arrange
        var occurredAt = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = occurredAt,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Jan 15, 2024", cut.Markup);
        Assert.Contains("14:30:00", cut.Markup);
    }

    [Fact]
    public void Render_WithTicketKey_DisplaysTicketKey()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info,
                TicketKey = "PROJ-123"
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("PROJ-123", cut.Markup);
        Assert.Contains("bi-ticket-perforated", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTicketKey_DoesNotDisplayTicketBadge()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.DoesNotContain("bi-ticket-perforated", cut.Markup);
    }

    [Theory]
    [InlineData(EventSeverity.Success, "success")]
    [InlineData(EventSeverity.Error, "error")]
    [InlineData(EventSeverity.Warning, "warning")]
    [InlineData(EventSeverity.Info, "info")]
    public void Render_WithSeverity_AppliesCorrectMarkerClass(EventSeverity severity, string expectedClass)
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = severity
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains($"timeline-marker {expectedClass}", cut.Markup);
    }

    [Fact]
    public void Render_WithShowDetailsFalse_DoesNotDisplayViewDetailsButton()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.ShowDetails, false));

        // Assert
        Assert.DoesNotContain("View Details", cut.Markup);
    }

    [Fact]
    public void Render_WithShowDetailsTrue_DisplaysViewDetailsButton()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.ShowDetails, true)
            .Add(p => p.OnEventClick, EventCallback.Factory.Create<WorkflowEventDto>(this, _ => { })));

        // Assert
        Assert.Contains("View Details", cut.Markup);
        Assert.Contains("bi-eye", cut.Markup);
    }

    [Fact]
    public void ViewDetailsButton_WhenClicked_InvokesOnEventClickCallback()
    {
        // Arrange
        WorkflowEventDto? clickedEvent = null;
        var testEvent = new WorkflowEventDto
        {
            Id = Guid.NewGuid(),
            EventType = "Test",
            Description = "Test",
            OccurredAt = DateTime.Now,
            Icon = "circle",
            Severity = EventSeverity.Info
        };
        var events = new List<WorkflowEventDto> { testEvent };

        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.ShowDetails, true)
            .Add(p => p.OnEventClick, EventCallback.Factory.Create<WorkflowEventDto>(this, evt => clickedEvent = evt)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.NotNull(clickedEvent);
        Assert.Equal(testEvent.Id, clickedEvent.Id);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesCustomClass()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.AdditionalClass, "custom-timeline"));

        // Assert
        Assert.Contains("custom-timeline", cut.Markup);
    }

    [Fact]
    public void Render_WithMultipleEvents_DisplaysAllEvents()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Event 1",
                Description = "Description 1",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            },
            new WorkflowEventDto
            {
                EventType = "Event 2",
                Description = "Description 2",
                OccurredAt = DateTime.Now,
                Icon = "check",
                Severity = EventSeverity.Success
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Event 1", cut.Markup);
        Assert.Contains("Event 2", cut.Markup);
        Assert.Contains("Description 1", cut.Markup);
        Assert.Contains("Description 2", cut.Markup);
    }

    [Fact]
    public void Render_HasTimelineStructure()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("timeline-item", cut.Markup);
        Assert.Contains("timeline-marker", cut.Markup);
        Assert.Contains("timeline-content", cut.Markup);
        Assert.Contains("timeline-header", cut.Markup);
        Assert.Contains("timeline-body", cut.Markup);
    }

    [Fact]
    public void Render_TimelineHeaderContainsTitle()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Important Event",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("timeline-title", cut.Markup);
        Assert.Contains("Important Event", cut.Markup);
    }

    [Fact]
    public void Render_TimelineHeaderContainsTimeWithClockIcon()
    {
        // Arrange
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                EventType = "Test",
                Description = "Test",
                OccurredAt = DateTime.Now,
                Icon = "circle",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<EventTimeline>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("timeline-time", cut.Markup);
        Assert.Contains("bi-clock", cut.Markup);
    }
}
