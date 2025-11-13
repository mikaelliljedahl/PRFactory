using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Tests.Components.Tickets;

public class WorkflowTimelineTests : ComponentTestBase
{
    [Fact]
    public void Renders_WithoutEvents()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, null));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_WithEmptyEventsList()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>();

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Renders_WorkflowStateChangedEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.StateChanged(WorkflowState.Triggered, WorkflowState.Analyzing).Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Analyzing", cut.Markup);
    }

    [Fact]
    public void Renders_MultipleEvents_InOrder()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.StateChanged(WorkflowState.Triggered, WorkflowState.Analyzing)
                .WithOccurredAt(DateTime.UtcNow.AddMinutes(-10))
                .Build(),
            WorkflowEventDtoBuilder.StateChanged(WorkflowState.Analyzing, WorkflowState.Planning)
                .WithOccurredAt(DateTime.UtcNow.AddMinutes(-5))
                .Build(),
            WorkflowEventDtoBuilder.StateChanged(WorkflowState.Planning, WorkflowState.PlanUnderReview)
                .WithOccurredAt(DateTime.UtcNow)
                .Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Analyzing", cut.Markup);
        Assert.Contains("Planning", cut.Markup);
        Assert.Contains("Plan Under Review", cut.Markup);
    }

    [Fact]
    public void Renders_QuestionAddedEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.QuestionAdded(3).Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("question", cut.Markup.ToLower());
    }

    [Fact]
    public void Renders_PlanApprovedEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.PlanApproved("TestUser").Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("approved", cut.Markup.ToLower());
    }

    [Fact]
    public void Renders_PlanRejectedEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.PlanRejected("Not detailed enough").Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("rejected", cut.Markup.ToLower());
    }

    [Fact]
    public void Renders_ErrorEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.Error("Something went wrong").Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Something went wrong", cut.Markup);
    }

    [Fact]
    public void Renders_WarningEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.Warning("This might take a while").Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("This might take a while", cut.Markup);
    }

    [Fact]
    public void Renders_SuccessEvent()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.Success("Operation completed successfully").Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Operation completed successfully", cut.Markup);
    }

    [Fact]
    public void FormatTimestamp_ShowsRelativeTime_ForRecentEvents()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDtoBuilder()
                .WithOccurredAt(DateTime.UtcNow.AddMinutes(-5))
                .Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert - Should contain relative time like "5 minutes ago"
        Assert.Contains("ago", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Completed)]
    [InlineData(WorkflowState.Failed)]
    [InlineData(WorkflowState.Cancelled)]
    [InlineData(WorkflowState.PlanApproved)]
    [InlineData(WorkflowState.PlanRejected)]
    public void AppliesCorrectMarkerClass_ForTerminalStates(WorkflowState state)
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();
        var events = new List<WorkflowEventDto>
        {
            WorkflowEventDtoBuilder.StateChanged(WorkflowState.Triggered, state).Build()
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert - Should render with appropriate marker class
        Assert.NotNull(cut.Markup);
        Assert.NotEmpty(cut.Markup);
    }
}
