using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Tests.Components;

/// <summary>
/// Comprehensive bUnit tests for TicketListItem component
/// Tests ticket information display, status badges, links, and metadata
/// </summary>
public class TicketListItemTests : ComponentTestBase
{
    [Fact]
    public void Render_WithTicket_DisplaysTicketKey()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithTicketKey("PROJ-456")
            .WithTitle("Test Ticket")
            .WithState(WorkflowState.Planning)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("PROJ-456", cut.Markup);
    }

    [Fact]
    public void Render_WithTicket_DisplaysTitle()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithTitle("Implement new authentication system")
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Implement new authentication system", cut.Markup);
    }

    [Fact]
    public void Render_WithDescription_DisplaysDescription()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithDescription("This ticket involves implementing OAuth 2.0")
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("This ticket involves implementing OAuth 2.0", cut.Markup);
    }

    [Fact]
    public void Render_WithoutDescription_DisplaysEmptyMessage()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithDescription("")
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("No description", cut.Markup);
    }

    [Fact]
    public void Render_WithState_DisplaysStatusBadge()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.Completed)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Completed", cut.Markup);
        Assert.Contains("badge", cut.Markup);
    }

    [Fact]
    public void Render_WithDifferentStates_DisplaysCorrectBadgeClass()
    {
        // Arrange
        var statesAndClasses = new[]
        {
            (WorkflowState.Completed, "bg-success"),
            (WorkflowState.Failed, "bg-danger"),
            (WorkflowState.Planning, "bg-info"),
        };

        foreach (var (state, expectedClass) in statesAndClasses)
        {
            // Arrange
            var ticket = new TicketDtoBuilder()
                .WithState(state)
                .Build();

            // Act
            var cut = RenderComponent<TicketListItem>(parameters => parameters
                .Add(p => p.Ticket, ticket));

            // Assert
            Assert.Contains(expectedClass, cut.Markup);
        }
    }

    [Fact]
    public void Render_WithTicket_DisplaysSourceIcon()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithSource(TicketSource.Jira)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("bi-box-seam", cut.Markup);
        Assert.Contains("Jira", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysCreatedTime()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var ticket = new TicketDtoBuilder()
            .WithCreatedAt(createdAt)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("bi-clock", cut.Markup);
        // Relative time should be displayed
        Assert.Contains("ago", cut.Markup);
    }

    [Fact]
    public void Render_WithRepositoryName_DisplaysRepository()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithRepositoryName("myapp-api")
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("myapp-api", cut.Markup);
        Assert.Contains("bi-git", cut.Markup);
    }

    [Fact]
    public void Render_WithoutRepositoryName_DoesNotDisplayRepositorySection()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithRepositoryName(string.Empty)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        // Repository section should not be displayed when name is empty/null
        var gitIconCount = markup.Split("bi-git").Length - 1;
        // Should have at most 1 git icon (for PR link if present)
        Assert.True(gitIconCount <= 1);
    }

    [Fact]
    public void Render_WithPullRequest_DisplaysPRLink()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithPullRequest("https://github.com/myapp/api/pull/42", 42)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("PR #42", cut.Markup);
        Assert.Contains("https://github.com/myapp/api/pull/42", cut.Markup);
        Assert.Contains("btn-outline-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithoutPullRequest_DoesNotDisplayPRSection()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.DoesNotContain("PR #", cut.Markup);
    }

    [Fact]
    public void Render_WithError_DisplaysErrorAlert()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithLastError("Failed to create pull request: authentication failed")
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Failed to create pull request: authentication failed", cut.Markup);
        Assert.Contains("alert-danger", cut.Markup);
        Assert.Contains("bi-exclamation-triangle", cut.Markup);
    }

    [Fact]
    public void Render_WithoutError_DoesNotDisplayErrorAlert()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithLastError(null)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.DoesNotContain("alert-danger", cut.Markup);
    }

    [Fact]
    public void Render_WithTicket_ContainsViewDetailsLink()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new TicketDtoBuilder()
            .WithId(ticketId)
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("View Details", cut.Markup);
        Assert.Contains($"/tickets/{ticketId}", cut.Markup);
    }

    [Fact]
    public void Render_WithTicket_LinkNavigateToCorrectUrl()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new TicketDtoBuilder()
            .WithId(ticketId)
            .WithTicketKey("TICKET-1")
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var links = cut.FindAll("a");
        var detailLinks = links.Where(a => a.TextContent.Contains("View Details")).ToList();
        Assert.NotEmpty(detailLinks);
        Assert.Contains(ticketId.ToString(), cut.Markup);
    }

    [Fact]
    public void Render_HasCardStructure()
    {
        // Arrange
        var ticket = new TicketDtoBuilder().Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("card", cut.Markup);
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("card-footer", cut.Markup);
    }

    [Fact]
    public void Render_CompleteTicketWithAllFields()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithTicketKey("FEATURE-789")
            .WithTitle("Complex feature implementation")
            .WithDescription("Implement a complex feature with multiple components")
            .WithState(WorkflowState.PRCreated)
            .WithSource(TicketSource.AzureDevOps)
            .WithRepositoryName("microservices-core")
            .WithPullRequest("https://github.com/example/repo/pull/123", 123)
            .WithCreatedAt(DateTime.UtcNow.AddDays(-1))
            .Build();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("FEATURE-789", cut.Markup);
        Assert.Contains("Complex feature implementation", cut.Markup);
        Assert.Contains("Implement a complex feature with multiple components", cut.Markup);
        Assert.Contains("PR Created", cut.Markup);
        Assert.Contains("Azure DevOps", cut.Markup);
        Assert.Contains("microservices-core", cut.Markup);
        Assert.Contains("PR #123", cut.Markup);
        Assert.Contains("View Details", cut.Markup);
    }
}
