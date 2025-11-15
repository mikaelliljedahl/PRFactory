using Bunit;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for WorkflowTimeline component
/// </summary>
public class WorkflowTimelineTests : TestContext
{
    [Fact]
    public void WorkflowTimeline_WithNoEvents_ShowsCurrentState()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test Ticket",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, null));

        // Assert
        Assert.Contains("Triggered", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithEvents_DisplaysAllEvents()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Planning,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow.AddHours(-2),
                EventType = "WorkflowStateChanged",
                Description = "Workflow triggered",
                FromState = null,
                ToState = WorkflowState.Triggered,
                Severity = EventSeverity.Info
            },
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                EventType = "WorkflowStateChanged",
                Description = "Started planning",
                FromState = WorkflowState.Triggered,
                ToState = WorkflowState.Planning,
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Workflow triggered", cut.Markup);
        Assert.Contains("Started planning", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithWorkflowStateChanged_ShowsStateTransition()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Planning,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                EventType = "WorkflowStateChanged",
                Description = "State changed",
                FromState = WorkflowState.Triggered,
                ToState = WorkflowState.Analyzing,
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Triggered", cut.Markup);
        Assert.Contains("Analyzing", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithReason_DisplaysReason()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Failed,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow,
                EventType = "WorkflowStateChanged",
                Description = "Workflow failed",
                ToState = WorkflowState.Failed,
                Reason = "Error occurred during implementation",
                Severity = EventSeverity.Error
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("Error occurred during implementation", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_ShowsTimelineHeader()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, null));

        // Assert
        Assert.Contains("Workflow Timeline", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithQuestionAddedEvent_ShowsCorrectIcon()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.QuestionsPosted,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow,
                EventType = "QuestionAdded",
                Description = "Questions posted for clarification",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("bi-question-circle", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithAnswerAddedEvent_ShowsCorrectIcon()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.AnswersReceived,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow,
                EventType = "AnswerAdded",
                Description = "Answers received",
                Severity = EventSeverity.Success
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("bi-chat-left-text", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithPlanCreatedEvent_ShowsCorrectIcon()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.PlanPosted,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow,
                EventType = "PlanCreated",
                Description = "Implementation plan created",
                Severity = EventSeverity.Success
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("bi-file-earmark-text", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_WithPullRequestCreatedEvent_ShowsCorrectIcon()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.PRCreated,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow,
                EventType = "PullRequestCreated",
                Description = "Pull request created",
                Severity = EventSeverity.Success
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert
        Assert.Contains("bi-git", cut.Markup);
    }

    [Fact]
    public void WorkflowTimeline_OrdersEventsChronologically()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Planning,
            CreatedAt = DateTime.UtcNow
        };

        var events = new List<WorkflowEventDto>
        {
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                EventType = "WorkflowStateChanged",
                Description = "Second event",
                Severity = EventSeverity.Info
            },
            new WorkflowEventDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                OccurredAt = DateTime.UtcNow.AddHours(-2),
                EventType = "WorkflowStateChanged",
                Description = "First event",
                Severity = EventSeverity.Info
            }
        };

        // Act
        var cut = RenderComponent<WorkflowTimeline>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.Events, events));

        // Assert - Should be in descending order (most recent first)
        var markup = cut.Markup;
        var secondIndex = markup.IndexOf("Second event");
        var firstIndex = markup.IndexOf("First event");
        Assert.True(secondIndex < firstIndex, "Events should be ordered with most recent first");
    }
}
