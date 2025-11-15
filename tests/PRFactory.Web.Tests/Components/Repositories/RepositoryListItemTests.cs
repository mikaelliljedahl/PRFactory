using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Web.Tests.Components.Repositories;

/// <summary>
/// Tests for the RepositoryListItem component.
/// Verifies rendering of repository information, platform icons, status badges, and event callbacks.
/// </summary>
public class RepositoryListItemTests : TestContext
{
    public RepositoryListItemTests()
    {
        // Setup mock NavigationManager
        Services.AddSingleton<NavigationManager>(new MockNavigationManager());
    }

    private RepositoryDto CreateTestRepository(bool isActive = true, string gitPlatform = "GitHub", int ticketCount = 5)
    {
        return new RepositoryDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TenantName = "Test Tenant",
            Name = "Test Repository",
            GitPlatform = gitPlatform,
            CloneUrl = "https://github.com/test/test-repo.git",
            DefaultBranch = "main",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            LastAccessedAt = DateTime.UtcNow.AddHours(-2),
            TicketCount = ticketCount
        };
    }

    [Fact]
    public void Render_WithActiveRepository_DisplaysActiveStatusBadge()
    {
        // Arrange
        var repository = CreateTestRepository(isActive: true);

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var badge = cut.Find(".badge.bg-success");
        Assert.NotNull(badge);
        Assert.Contains("Active", badge.TextContent);
    }

    [Fact]
    public void Render_WithInactiveRepository_DisplaysInactiveStatusBadge()
    {
        // Arrange
        var repository = CreateTestRepository(isActive: false);

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var badge = cut.Find(".badge.bg-secondary");
        Assert.NotNull(badge);
        Assert.Contains("Inactive", badge.TextContent);
    }

    [Fact]
    public void Render_WithGitHubPlatform_DisplaysGitHubIconAndBadge()
    {
        // Arrange
        var repository = CreateTestRepository(gitPlatform: "GitHub");

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var icon = cut.Find("i.bi-github");
        Assert.NotNull(icon);

        var platformBadge = cut.Find(".badge.bg-dark");
        Assert.NotNull(platformBadge);
        Assert.Contains("GitHub", platformBadge.TextContent);
    }

    [Fact]
    public void Render_WithBitbucketPlatform_DisplaysBitbucketIconAndBadge()
    {
        // Arrange
        var repository = CreateTestRepository(gitPlatform: "Bitbucket");

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var icon = cut.Find("i.bi-bucket");
        Assert.NotNull(icon);

        var platformBadge = cut.Find(".badge.bg-primary");
        Assert.NotNull(platformBadge);
        Assert.Contains("Bitbucket", platformBadge.TextContent);
    }

    [Fact]
    public void Render_DisplaysRepositoryNameAsLink()
    {
        // Arrange
        var repository = CreateTestRepository();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var link = cut.Find($"a[href='/repositories/{repository.Id}']");
        Assert.NotNull(link);
        Assert.Contains(repository.Name, link.TextContent);
    }

    [Fact]
    public void Render_DisplaysCloneUrl()
    {
        // Arrange
        var repository = CreateTestRepository();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var code = cut.Find("code");
        Assert.NotNull(code);
        Assert.Contains(repository.CloneUrl, code.TextContent);
    }

    [Fact]
    public void Render_WithTickets_DisplaysTicketCount()
    {
        // Arrange
        var repository = CreateTestRepository(ticketCount: 5);

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var badge = cut.Find(".badge.bg-info");
        Assert.NotNull(badge);
        Assert.Contains("5 tickets", badge.TextContent);
    }

    [Fact]
    public void Render_WithNoTickets_DoesNotDisplayTicketBadge()
    {
        // Arrange
        var repository = CreateTestRepository(ticketCount: 0);

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("tickets", markup);
    }

    [Fact]
    public void Click_EditButton_InvokesOnEditCallback()
    {
        // Arrange
        var repository = CreateTestRepository();
        var editCallbackInvoked = false;
        Guid? editedRepositoryId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<Guid>(this, id =>
            {
                editCallbackInvoked = true;
                editedRepositoryId = id;
            })));

        // Act
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
        editButton.Click();

        // Assert
        Assert.True(editCallbackInvoked);
        Assert.Equal(repository.Id, editedRepositoryId);
    }

    [Fact]
    public void Click_DeleteButton_InvokesOnDeleteCallback()
    {
        // Arrange
        var repository = CreateTestRepository();
        var deleteCallbackInvoked = false;
        Guid? deletedRepositoryId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<Guid>(this, id =>
            {
                deleteCallbackInvoked = true;
                deletedRepositoryId = id;
            })));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        deleteButton.Click();

        // Assert
        Assert.True(deleteCallbackInvoked);
        Assert.Equal(repository.Id, deletedRepositoryId);
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
