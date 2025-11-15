using Bunit;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for TicketHeader component
/// </summary>
public class TicketHeaderTests : TestContext
{
    [Fact]
    public void TicketHeader_WithTicket_RendersTicketKeyAndTitle()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test Ticket Title",
            Description = "Test description",
            State = WorkflowState.Triggered,
            Source = TicketSource.WebUI,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("TEST-123", cut.Markup);
        Assert.Contains("Test Ticket Title", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithWorkflowState_ShowsCorrectStateBadge()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.PlanUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Plan Under Review", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithDescription_DisplaysFormattedDescription()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            Description = "This is a test description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("This is a test description", cut.Markup);
        Assert.Contains("Description", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithoutDescription_ShowsNoDescriptionMessage()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            Description = "",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("No description provided", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithRepositoryName_DisplaysRepositoryName()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            RepositoryName = "MyRepository",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("MyRepository", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithoutRepositoryName_ShowsNA()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            RepositoryName = null,
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("N/A", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithCreatedDate_DisplaysCreatedTimestamp()
    {
        // Arrange
        var createdDate = new DateTime(2024, 1, 15, 10, 30, 0);
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Triggered,
            CreatedAt = createdDate
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Created:", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithUpdatedDate_DisplaysUpdatedTimestamp()
    {
        // Arrange
        var updatedDate = new DateTime(2024, 1, 16, 12, 0, 0);
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = updatedDate
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Updated:", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithCompletedDate_DisplaysCompletedTimestamp()
    {
        // Arrange
        var completedDate = new DateTime(2024, 1, 17, 14, 0, 0);
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = completedDate
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Completed:", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithJiraSource_DisplaysJiraSourceBadge()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Triggered,
            Source = TicketSource.Jira,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Jira", cut.Markup);
    }

    [Fact]
    public void TicketHeader_WithWebUISource_DisplaysWebUISourceBadge()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test",
            State = WorkflowState.Triggered,
            Source = TicketSource.WebUI,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Web UI", cut.Markup);
    }
}
