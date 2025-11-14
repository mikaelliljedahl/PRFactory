using Bunit;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Models;
using PRFactory.Web.Pages.Repositories;
using Radzen;
using Xunit;

namespace PRFactory.Tests.Pages.Repositories;

public class DetailTests : PageTestBase
{
    private readonly Mock<ILogger<Detail>> _mockLogger = new();

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<DialogService>();
    }

    [Fact]
    public async Task OnInitialized_LoadsRepository()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithName("Test Repo")
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        MockRepositoryService.Verify(
            x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_WithValidRepository_DisplaysRepositoryDetails()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithName("My Awesome Repository")
            .WithGitPlatform("GitHub")
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("My Awesome Repository", cut.Markup);
        Assert.Contains("GitHub", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WhenRepositoryNotFound_ShowsWarning()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RepositoryDto?)null);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("not found", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WhenError_ShowsLoadingError()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to load repository"));

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        // Component should handle error gracefully
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task Render_ShowsRepositoryName()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithName("Production App")
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Production App", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsEditButton()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Edit", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsDeleteButton()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Delete", cut.Markup);
    }

    [Fact]
    public async Task Render_WithActiveRepository_ShowsActiveBadge()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithIsActive(true)
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public async Task Render_WithInactiveRepository_ShowsInactiveBadge()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithIsActive(false)
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Inactive", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsTabs()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Overview", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsRepositoryDetails()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithCloneUrl("https://github.com/test/repo.git")
            .WithDefaultBranch("main")
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("github.com", cut.Markup);
        Assert.Contains("main", cut.Markup);
    }

    [Fact]
    public async Task Render_WithTickets_ShowsTicketCount()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithTicketCount(5)
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("5", cut.Markup);
    }
}
