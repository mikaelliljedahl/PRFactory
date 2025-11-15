using Bunit;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for TicketDiffViewer component
/// </summary>
public class TicketDiffViewerTests : TestContext
{
    [Fact]
    public void TicketDiffViewer_WithOriginalAndUpdatedTicket_DisplaysBothTitles()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Original Title", cut.Markup);
        Assert.Contains("Updated Title", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithChangedTitle_ShowsChangedBadge()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Changed Title",
            UpdatedDescription = "Description",
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Changed", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithUnchangedTitle_DoesNotShowChangedBadge()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Same Title",
            Description = "Original Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Same Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        // The title section should not have the "changed" class
        var markup = cut.Markup;
        Assert.Contains("Same Title", markup);
    }

    [Fact]
    public void TicketDiffViewer_WithChangedDescription_ShowsChangedBadge()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Original Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Changed", cut.Markup);
        Assert.Contains("Updated Description", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithSuccessCriteria_DisplaysSuccessCriteriaSection()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Description",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Feature should work correctly",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Success Criteria", cut.Markup);
        Assert.Contains("Feature should work correctly", cut.Markup);
        Assert.Contains("Added", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithNoSuccessCriteria_ShowsNoSuccessCriteria()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Description",
            SuccessCriteria = new List<SuccessCriterionDto>(),
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("No success criteria", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithTestableSuccessCriterion_ShowsTestableBadge()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Description",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Testable criterion",
                    Priority = 0,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Testable", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithAcceptanceCriteria_DisplaysAcceptanceCriteria()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Description",
            AcceptanceCriteria = "- User can login\n- User can logout",
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Acceptance Criteria", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_ShowsOriginalAndUpdatedPanels()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Description",
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Original Ticket", cut.Markup);
        Assert.Contains("Updated Ticket", cut.Markup);
    }

    [Fact]
    public void TicketDiffViewer_WithPrioritySuccessCriteria_DisplaysPriorityBadges()
    {
        // Arrange
        var originalTicket = new TicketDto
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            State = WorkflowState.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = originalTicket.Id,
            UpdatedTitle = "Title",
            UpdatedDescription = "Description",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Functional,
                    Description = "Must have feature",
                    Priority = 0,
                    IsTestable = true
                },
                new SuccessCriterionDto
                {
                    Category = SuccessCriterionCategory.Performance,
                    Description = "Should have performance",
                    Priority = 1,
                    IsTestable = true
                }
            },
            GeneratedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TicketDiffViewer>(parameters => parameters
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.TicketUpdate, ticketUpdate));

        // Assert
        Assert.Contains("Must-Have", cut.Markup);
        Assert.Contains("Should-Have", cut.Markup);
    }
}
