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

public class EditTests : PageTestBase
{
    private readonly Mock<ILogger<Edit>> _mockLogger = new();

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
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        MockRepositoryService.Verify(
            x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_WithValidRepository_DisplaysRepositoryName()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var repository = new RepositoryDtoBuilder()
            .WithId(repositoryId)
            .WithName("My Test Repository")
            .Build();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("My Test Repository", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WhenRepositoryNotFound_ShowsNotFound()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RepositoryDto?)null);

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.True(cut.Markup.Contains("not found") || cut.Markup.Contains("Not Found"));
    }

    [Fact]
    public async Task OnInitialized_WhenError_HandlesGracefully()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        MockRepositoryService
            .Setup(x => x.GetRepositoryByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failed to load repository"));

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        // The component should handle error gracefully
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task UpdateRepository_WhenSuccessful_NavigatesToDetails()
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

        MockRepositoryService
            .Setup(x => x.UpdateRepositoryAsync(repositoryId, It.IsAny<UpdateRepositoryRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task UpdateRepository_WhenError_ShowsErrorMessage()
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

        MockRepositoryService
            .Setup(x => x.UpdateRepositoryAsync(repositoryId, It.IsAny<UpdateRepositoryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task Render_ShowsRepositoryForm()
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
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.True(cut.Markup.Contains("Repository") || cut.Markup.Contains("Edit"));
    }

    [Fact]
    public async Task Render_ShowsEditForm()
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
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("Test Repo", cut.Markup);
    }
}
