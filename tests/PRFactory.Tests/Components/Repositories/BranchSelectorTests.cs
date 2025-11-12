using Bunit;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Repositories;

public class BranchSelectorTests : ComponentTestBase
{
    private readonly Mock<ILogger<BranchSelector>> _mockLogger = new();

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_mockLogger.Object);

        // Note: IRepositoryService is already registered by TestContextBase
    }

    [Fact]
    public void Render_WithLabel_ShowsDefaultBranchLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, Guid.NewGuid()));

        // Assert
        Assert.Contains("Default Branch", cut.Markup);
    }

    [Fact]
    public void Render_WithRequired_ShowsRequiredAsterisk()
    {
        // Arrange & Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, Guid.NewGuid())
            .Add(p => p.Required, true));

        // Assert
        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("*", cut.Markup);
    }

    [Fact]
    public void Render_WithHelpText_ShowsHelpText()
    {
        // Arrange & Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, Guid.NewGuid())
            .Add(p => p.HelpText, "Select the main branch for this repository"));

        // Assert
        Assert.Contains("Select the main branch for this repository", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyRepositoryId_DisablesRefreshButton()
    {
        // Arrange & Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, Guid.Empty));

        // Assert
        var refreshButton = cut.Find("button:contains('Refresh')");
        Assert.True(refreshButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task OnParametersSet_WithValidRepositoryId_LoadsBranches()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branches = new List<string> { "main", "develop", "feature/test" };

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async load
        cut.WaitForState(() => cut.Markup.Contains("main"));

        MockRepositoryService.Verify(
            x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadBranches_SelectsMainByDefault_WhenMainExists()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branches = new List<string> { "develop", "main", "feature/test" };

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        string? selectedBranch = null;

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId)
            .Add(p => p.SelectedBranchChanged, (branch) => { selectedBranch = branch; }));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => selectedBranch == "main");
        Assert.Equal("main", selectedBranch);
    }

    [Fact]
    public async Task LoadBranches_SelectsMasterByDefault_WhenMainDoesNotExist()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branches = new List<string> { "develop", "master", "feature/test" };

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        string? selectedBranch = null;

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId)
            .Add(p => p.SelectedBranchChanged, (branch) => { selectedBranch = branch; }));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => selectedBranch == "master");
        Assert.Equal("master", selectedBranch);
    }

    [Fact]
    public async Task LoadBranches_SelectsFirstBranch_WhenNeitherMainNorMasterExists()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branches = new List<string> { "develop", "feature/test" };

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        string? selectedBranch = null;

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId)
            .Add(p => p.SelectedBranchChanged, (branch) => { selectedBranch = branch; }));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => selectedBranch == "develop");
        Assert.Equal("develop", selectedBranch);
    }

    [Fact]
    public async Task LoadBranches_WhenNoBranchesFound_ShowsErrorMessage()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("No branches found"));
        Assert.Contains("No branches found in repository", cut.Markup);
    }

    [Fact]
    public async Task LoadBranches_WhenException_ShowsErrorMessage()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Failed to load branches"));
        Assert.Contains("Failed to load branches: Network error", cut.Markup);
    }

    [Fact]
    public async Task RefreshButton_WhenClicked_ReloadsBranches()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branches = new List<string> { "main", "develop" };

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        await cut.InvokeAsync(() => Task.Delay(100));

        // Act
        var refreshButton = cut.Find("button:contains('Refresh')");
        await refreshButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        MockRepositoryService.Verify(
            x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Once on init, once on refresh
    }

    [Fact]
    public async Task LoadBranches_DisablesDropdownWhileLoading()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<List<string>>();

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert - check that dropdown is disabled while loading
        await cut.InvokeAsync(() => Task.Delay(100));
        // Radzen dropdown should be disabled
        Assert.True(cut.Markup.Contains("Disabled=\"True\"") ||
               cut.Markup.ToLower().Contains("disabled=\"disabled\""));

        // Complete loading
        tcs.SetResult(new List<string> { "main" });
    }

    [Fact]
    public async Task LoadBranches_DisablesRefreshButtonWhileLoading()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<List<string>>();

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        var refreshButton = cut.Find("button:contains('Refresh')");
        Assert.True(refreshButton.HasAttribute("disabled") || cut.Markup.Contains("spinner"));

        // Complete loading
        tcs.SetResult(new List<string> { "main" });
    }

    [Fact]
    public async Task LoadBranches_PreservesSelectedBranch_WhenBranchStillExists()
    {
        // Arrange
        var repositoryId = Guid.NewGuid();
        var branches = new List<string> { "main", "develop", "feature/test" };

        MockRepositoryService
            .Setup(x => x.GetBranchesAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        string? selectedBranch = null;

        // Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, repositoryId)
            .Add(p => p.SelectedBranch, "develop")
            .Add(p => p.SelectedBranchChanged, (branch) => { selectedBranch = branch; }));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        // Branch should remain "develop" since it exists in the list
        Assert.Null(selectedBranch); // No change event should be triggered
    }

    [Fact]
    public void Render_WithEmptyRepositoryId_ShowsSelectPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<BranchSelector>(parameters => parameters
            .Add(p => p.RepositoryId, Guid.Empty));

        // Assert
        Assert.Contains("Select a branch", cut.Markup);
    }
}
