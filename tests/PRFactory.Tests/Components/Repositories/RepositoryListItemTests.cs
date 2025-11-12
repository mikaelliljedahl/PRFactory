using Bunit;
using Microsoft.AspNetCore.Components;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Tests.Components.Repositories;

public class RepositoryListItemTests : ComponentTestBase
{

    [Fact]
    public void Render_WithActiveRepository_ShowsActiveBadge()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithIsActive(true)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Active", cut.Markup);
        Assert.Contains("badge bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WithInactiveRepository_ShowsInactiveBadge()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithIsActive(false)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Inactive", cut.Markup);
        Assert.Contains("badge bg-secondary", cut.Markup);
    }

    [Fact]
    public void Render_WithGitHubRepository_ShowsGitHubBadgeAndIcon()
    {
        // Arrange
        var repository = RepositoryDtoBuilder.GitHub().Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("GitHub", cut.Markup);
        Assert.Contains("badge bg-dark", cut.Markup);
        Assert.Contains("bi-github", cut.Markup);
    }

    [Fact]
    public void Render_WithBitbucketRepository_ShowsBitbucketBadgeAndIcon()
    {
        // Arrange
        var repository = RepositoryDtoBuilder.Bitbucket().Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Bitbucket", cut.Markup);
        Assert.Contains("badge bg-primary", cut.Markup);
        Assert.Contains("bi-bucket", cut.Markup);
    }

    [Fact]
    public void Render_WithAzureDevOpsRepository_ShowsAzureDevOpsBadgeAndIcon()
    {
        // Arrange
        var repository = RepositoryDtoBuilder.AzureDevOps().Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("AzureDevOps", cut.Markup);
        Assert.Contains("badge bg-info", cut.Markup);
        Assert.Contains("bi-microsoft", cut.Markup);
    }

    [Fact]
    public void Render_WithRepositoryName_ShowsNameAsLink()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithName("my-awesome-repo")
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("my-awesome-repo", cut.Markup);
        Assert.Contains($"/repositories/{repository.Id}", cut.Markup);
    }

    [Fact]
    public void Render_WithLongCloneUrl_TruncatesUrl()
    {
        // Arrange
        var longUrl = "https://github.com/very-long-organization-name/very-long-repository-name-that-exceeds-fifty-characters.git";
        var repository = new RepositoryDtoBuilder()
            .WithCloneUrl(longUrl)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("...", cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultBranch_ShowsBranchName()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithDefaultBranch("develop")
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("develop", cut.Markup);
        Assert.Contains("Branch:", cut.Markup);
    }

    [Fact]
    public void Render_WithTenantName_ShowsTenantName()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithTenantName("ACME Corporation")
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("ACME Corporation", cut.Markup);
        Assert.Contains("Tenant:", cut.Markup);
    }

    [Fact]
    public void Render_WithTickets_ShowsTicketCount()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithTicketCount(15)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("15 tickets", cut.Markup);
    }

    [Fact]
    public void Render_WithNoTickets_HidesTicketCount()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithTicketCount(0)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.DoesNotContain("tickets", cut.Markup);
    }

    [Fact]
    public void Render_WithTimestamps_ShowsCreatedAt()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-5);
        var repository = new RepositoryDtoBuilder()
            .WithCreatedAt(createdAt)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Created", cut.Markup);
    }

    [Fact]
    public void Render_WithUpdatedAt_ShowsUpdatedTimestamp()
    {
        // Arrange
        var updatedAt = DateTime.UtcNow.AddHours(-2);
        var repository = new RepositoryDtoBuilder()
            .WithUpdatedAt(updatedAt)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Updated", cut.Markup);
    }

    [Fact]
    public void Render_WithLastAccessedAt_ShowsAccessedTimestamp()
    {
        // Arrange
        var lastAccessedAt = DateTime.UtcNow.AddMinutes(-30);
        var repository = new RepositoryDtoBuilder()
            .WithLastAccessedAt(lastAccessedAt)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Accessed", cut.Markup);
    }

    [Fact]
    public async Task EditButton_WhenClicked_InvokesOnEditCallback()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder().Build();
        Guid? editedId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnEdit, (id) => { editedId = id; }));

        // Act
        var editButton = cut.Find("button:contains('Edit')");
        await editButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(repository.Id, editedId);
    }

    [Fact]
    public async Task DeleteButton_WhenClicked_InvokesOnDeleteCallback()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder().Build();
        Guid? deletedId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnDelete, (id) => { deletedId = id; }));

        // Act
        var deleteButton = cut.Find("button:contains('Delete')");
        await deleteButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(repository.Id, deletedId);
    }

    [Fact]
    public async Task TestConnectionButton_WhenClicked_InvokesOnTestConnectionCallback()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder().Build();
        Guid? testedId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnTestConnection, (id) => { testedId = id; }));

        // Act
        var testButton = cut.Find("button:contains('Test')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(repository.Id, testedId);
    }

    [Fact]
    public async Task DeactivateButton_WhenActiveRepository_InvokesOnDeactivateCallback()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithIsActive(true)
            .Build();
        Guid? deactivatedId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnDeactivate, (id) => { deactivatedId = id; }));

        // Act
        var deactivateButton = cut.Find("button:contains('Deactivate')");
        await deactivateButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(repository.Id, deactivatedId);
    }

    [Fact]
    public async Task ActivateButton_WhenInactiveRepository_InvokesOnActivateCallback()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithIsActive(false)
            .Build();
        Guid? activatedId = null;

        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository)
            .Add(p => p.OnActivate, (id) => { activatedId = id; }));

        // Act
        var activateButton = cut.Find("button:contains('Activate')");
        await activateButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(repository.Id, activatedId);
    }

    [Fact]
    public void Render_ShowsAllActionButtons()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder()
            .WithIsActive(true)
            .Build();

        // Act
        var cut = RenderComponent<RepositoryListItem>(parameters => parameters
            .Add(p => p.Repository, repository));

        // Assert
        Assert.Contains("Details", cut.Markup);
        Assert.Contains("Edit", cut.Markup);
        Assert.Contains("Test", cut.Markup);
        Assert.Contains("Deactivate", cut.Markup);
        Assert.Contains("Delete", cut.Markup);
    }
}
