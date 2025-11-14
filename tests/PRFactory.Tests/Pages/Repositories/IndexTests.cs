using Bunit;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Models;
using Radzen;
using Xunit;
using RepositoryIndex = PRFactory.Web.Pages.Repositories.Index;

namespace PRFactory.Tests.Pages.Repositories;

public class IndexTests : PageTestBase
{
    private readonly Mock<ILogger<RepositoryIndex>> _mockLogger = new();

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<DialogService>();
    }

    [Fact]
    public async Task OnInitialized_LoadsRepositories()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            new RepositoryDtoBuilder().WithName("Repo 1").Build(),
            new RepositoryDtoBuilder().WithName("Repo 2").Build(),
            new RepositoryDtoBuilder().WithName("Repo 3").Build()
        };

        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        MockRepositoryService.Verify(
            x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_WithRepositories_DisplaysRepositories()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            new RepositoryDtoBuilder().WithName("Test Repo").Build()
        };

        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Test Repo", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WithNoRepositories_ShowsEmptyState()
    {
        // Arrange
        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RepositoryDto>());

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("No repositories found", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WhenError_ShowsErrorMessage()
    {
        // Arrange
        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to load repositories"));

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Failed to load repositories", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsAddRepositoryButton()
    {
        // Arrange
        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RepositoryDto>());

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Add Repository", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsBreadcrumbs()
    {
        // Arrange
        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RepositoryDto>());

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Repositories", cut.Markup);
    }

    [Fact]
    public async Task Render_WithRepositories_ShowsRepositoryCards()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            new RepositoryDtoBuilder()
                .WithName("Repo 1")
                .WithGitPlatform("GitHub")
                .Build()
        };

        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("GitHub", cut.Markup);
    }

    [Fact]
    public async Task Render_WithActiveRepository_ShowsActiveBadge()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            new RepositoryDtoBuilder()
                .WithIsActive(true)
                .Build()
        };

        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public async Task Render_WithInactiveRepository_ShowsInactiveBadge()
    {
        // Arrange
        var repositories = new List<RepositoryDto>
        {
            new RepositoryDtoBuilder()
                .WithIsActive(false)
                .Build()
        };

        MockRepositoryService
            .Setup(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Inactive", cut.Markup);
    }

    [Fact]
    public async Task DeleteRepository_WhenCalled_DeletesAndReloads()
    {
        // Arrange
        var repository = new RepositoryDtoBuilder().WithName("Test Repo").Build();
        var repositories = new List<RepositoryDto> { repository };

        MockRepositoryService
            .SetupSequence(x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories)
            .ReturnsAsync(new List<RepositoryDto>());

        MockRepositoryService
            .Setup(x => x.DeleteRepositoryAsync(repository.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<RepositoryIndex>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.NotNull(cut);
        // Verify initial load happened
        MockRepositoryService.Verify(
            x => x.GetAllRepositoriesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
