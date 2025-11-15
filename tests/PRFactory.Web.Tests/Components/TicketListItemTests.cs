using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Web.Tests.Components;

/// <summary>
/// Tests for the TicketListItem component.
/// Verifies rendering of ticket information, status badges, links, and relative time formatting.
/// </summary>
public class TicketListItemTests : TestContext
{
    public TicketListItemTests()
    {
        // Setup mock NavigationManager
        Services.AddSingleton<NavigationManager>(new MockNavigationManager());
    }

    private TicketDto CreateTestTicket(
        WorkflowState state = WorkflowState.Triggered,
        TicketSource source = TicketSource.WebUI,
        string? description = "Test description",
        DateTime? createdAt = null,
        string? repositoryName = "TestRepo",
        string? pullRequestUrl = null,
        string? lastError = null)
    {
        return new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TICKET-001",
            Title = "Test Ticket Title",
            Description = description ?? string.Empty,
            State = state,
            Source = source,
            RepositoryId = Guid.NewGuid(),
            RepositoryName = repositoryName,
            CreatedAt = createdAt ?? DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = null,
            PullRequestUrl = pullRequestUrl,
            PullRequestNumber = pullRequestUrl != null ? 123 : null,
            LastError = lastError
        };
    }

    [Fact]
    public void Render_DisplaysTicketCard()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var card = cut.Find(".card");
        Assert.NotNull(card);
        Assert.Contains("ticket-card", card.GetAttribute("class") ?? "");
    }

    [Fact]
    public void Render_DisplaysTicketKey()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(ticket.TicketKey, markup);
    }

    [Fact]
    public void Render_DisplaysTicketTitle()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(ticket.Title, markup);
    }

    [Fact]
    public void Render_DisplaysTicketDescription()
    {
        // Arrange
        var ticket = CreateTestTicket(description: "This is a detailed ticket description");

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("This is a detailed ticket description", markup);
    }

    [Fact]
    public void Render_WithoutDescription_DisplaysEmptyMessagel()
    {
        // Arrange
        var ticket = CreateTestTicket(description: null);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("No description", markup);
    }

    [Fact]
    public void Render_DisplaysStateBadge()
    {
        // Arrange
        var ticket = CreateTestTicket(state: WorkflowState.Completed);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var badge = cut.Find("span[class*='badge']");
        Assert.NotNull(badge);
        Assert.Contains("Completed", badge.TextContent);
    }

    [Fact]
    public void Render_DisplaysSourceIcon()
    {
        // Arrange
        var ticket = CreateTestTicket(source: TicketSource.Jira);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("box-seam", markup);
        Assert.Contains("Jira", markup);
    }

    [Fact]
    public void Render_DisplaysSourceDisplay()
    {
        // Arrange
        var ticket = CreateTestTicket(source: TicketSource.AzureDevOps);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Azure DevOps", markup);
    }

    [Fact]
    public void Render_DisplaysRelativeTime()
    {
        // Arrange
        var ticket = CreateTestTicket(createdAt: DateTime.UtcNow.AddMinutes(-30));

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        // Should contain relative time display (e.g., "30m ago")
        Assert.Contains("ago", markup);
    }

    [Fact]
    public void Render_DisplaysClockIcon()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("bi-clock", markup);
    }

    [Fact]
    public void Render_WithRepositoryName_DisplaysRepository()
    {
        // Arrange
        var ticket = CreateTestTicket(repositoryName: "my-awesome-repo");

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("my-awesome-repo", markup);
        Assert.Contains("bi-git", markup);
    }

    [Fact]
    public void Render_WithoutRepositoryName_DoesNotDisplayRepository()
    {
        // Arrange
        var ticket = CreateTestTicket(repositoryName: null);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("bi-git", markup);
    }

    [Fact]
    public void Render_WithPullRequest_DisplaysPRLink()
    {
        // Arrange
        var ticket = CreateTestTicket(
            pullRequestUrl: "https://github.com/test/repo/pull/123");

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var prLink = cut.Find("a[href='https://github.com/test/repo/pull/123']");
        Assert.NotNull(prLink);
        Assert.Contains("PR #123", prLink.TextContent);
    }

    [Fact]
    public void Render_WithoutPullRequest_DoesNotDisplayPRLink()
    {
        // Arrange
        var ticket = CreateTestTicket(pullRequestUrl: null);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("PR #", markup);
    }

    [Fact]
    public void Render_WithError_DisplaysErrorMessage()
    {
        // Arrange
        var ticket = CreateTestTicket(lastError: "Connection timeout while executing workflow");

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var errorAlert = cut.Find(".alert.alert-danger");
        Assert.NotNull(errorAlert);
        Assert.Contains("Connection timeout while executing workflow", errorAlert.TextContent);
    }

    [Fact]
    public void Render_WithoutError_DoesNotDisplayErrorMessage()
    {
        // Arrange
        var ticket = CreateTestTicket(lastError: null);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var errorAlert = cut.FindAll(".alert.alert-danger");
        Assert.Empty(errorAlert);
    }

    [Fact]
    public void Render_DisplaysViewDetailsButton()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var viewButton = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("View Details") &&
            a.GetAttribute("href") == $"/tickets/{ticket.Id}");
        Assert.NotNull(viewButton);
    }

    [Fact]
    public void Render_TicketTitleIsLink()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var titleLink = cut.Find($"a[href='/tickets/{ticket.Id}']");
        Assert.NotNull(titleLink);
        Assert.Contains(ticket.TicketKey, titleLink.TextContent);
    }

    [Fact]
    public void Render_DisplaysCorrectStateBadgeColor()
    {
        // Arrange
        var ticket = CreateTestTicket(state: WorkflowState.PRCreated);

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var badge = cut.Find("span[class*='badge']");
        Assert.NotNull(badge);
        // PRCreated should have bg-success class
        Assert.Contains("bg-success", badge.GetAttribute("class") ?? "");
    }

    [Fact]
    public void Render_MultipleStates_DisplaysCorrectBadges()
    {
        // Arrange
        var states = new[]
        {
            WorkflowState.Analyzing,
            WorkflowState.Planning,
            WorkflowState.Implementing,
            WorkflowState.Completed,
            WorkflowState.Failed
        };

        // Act & Assert
        foreach (var state in states)
        {
            var ticket = CreateTestTicket(state: state);
            var cut = RenderComponent<TicketListItem>(parameters => parameters
                .Add(p => p.Ticket, ticket));

            var badge = cut.Find("span[class*='badge']");
            Assert.NotNull(badge);
        }
    }

    [Fact]
    public void Render_CardHasHeightFull()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var card = cut.Find(".card");
        Assert.Contains("h-100", card.GetAttribute("class") ?? "");
    }

    [Fact]
    public void Render_CardHasShadow()
    {
        // Arrange
        var ticket = CreateTestTicket();

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var card = cut.Find(".card");
        Assert.Contains("shadow-sm", card.GetAttribute("class") ?? "");
    }

    [Fact]
    public void Render_WithJustNowTimestamp_DisplaysJustNow()
    {
        // Arrange
        var ticket = CreateTestTicket(createdAt: DateTime.UtcNow.AddSeconds(-15));

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("just now", markup);
    }

    [Fact]
    public void Render_WithOldTimestamp_DisplaysYearsAgo()
    {
        // Arrange
        var ticket = CreateTestTicket(createdAt: DateTime.UtcNow.AddDays(-500));

        // Act
        var cut = RenderComponent<TicketListItem>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("ago", markup);
    }
}

/// <summary>
/// Mock NavigationManager for testing navigation behavior.
/// </summary>
internal class MockNavigationManager : NavigationManager
{
    public MockNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Uri = ToAbsoluteUri(uri).ToString();
    }
}
