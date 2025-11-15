using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Web.Tests.Components.Repositories;

/// <summary>
/// Tests for the RepositoryConnectionTest component.
/// Verifies connection testing, result display, error handling, and callback invocation.
/// </summary>
public class RepositoryConnectionTestTests : TestContext
{
    private readonly Mock<IRepositoryService> _mockRepositoryService;
    private readonly Mock<ILogger<RepositoryConnectionTest>> _mockLogger;

    public RepositoryConnectionTestTests()
    {
        _mockRepositoryService = new Mock<IRepositoryService>();
        _mockLogger = new Mock<ILogger<RepositoryConnectionTest>>();

        Services.AddSingleton(_mockRepositoryService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public void Render_WithValidParameters_DisplaysTestButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Assert
        var button = cut.Find("button");
        Assert.NotNull(button);
        Assert.Contains("Test Connection", button.TextContent);
    }

    [Fact]
    public void Render_WithEmptyCloneUrl_DisablesTestButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "")
            .Add(p => p.AccessToken, "test-token"));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WithEmptyAccessToken_DisablesTestButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, ""));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public async Task HandleTestConnection_WhenSuccessful_DisplaysSuccessMessage()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            AvailableBranches = new List<string> { "main", "develop" },
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Success", markup);
        Assert.Contains("Connection successful", markup);
    }

    [Fact]
    public async Task HandleTestConnection_WhenFailed_DisplaysErrorMessage()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = false,
            Message = "Authentication failed",
            ErrorDetails = "Invalid credentials",
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Connection Failed", markup);
        Assert.Contains("Authentication failed", markup);
    }

    [Fact]
    public async Task HandleTestConnection_WhenSuccessful_DisplaysAvailableBranches()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            AvailableBranches = new List<string> { "main", "develop", "feature/test" },
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Available Branches", markup);
        Assert.Contains("main", markup);
        Assert.Contains("develop", markup);
        Assert.Contains("feature/test", markup);
    }

    [Fact]
    public async Task HandleTestConnection_WithManyBranches_ShowsOnlyFirst10()
    {
        // Arrange
        var branches = Enumerable.Range(1, 15).Select(i => $"branch-{i}").ToList();
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            AvailableBranches = branches,
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("+5 more", markup);
    }

    [Fact]
    public async Task HandleTestConnection_WhenFailed_ShowsErrorDetailsButton()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = false,
            Message = "Connection failed",
            ErrorDetails = "Detailed error information",
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token")
            .Add(p => p.ShowErrorDetails, true));

        // Act
        var testButton = cut.Find("button");
        await cut.InvokeAsync(() => testButton.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Show Error Details", markup);
    }

    [Fact]
    public async Task ToggleErrorDetails_ShowsAndHidesErrorDetails()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = false,
            Message = "Connection failed",
            ErrorDetails = "Detailed error information",
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token")
            .Add(p => p.ShowErrorDetails, true));

        var testButton = cut.Find("button");
        await cut.InvokeAsync(() => testButton.Click());

        // Act - Click show error details
        var toggleButton = cut.FindAll("button").First(b => b.TextContent.Contains("Show Error Details"));
        await cut.InvokeAsync(() => toggleButton.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Detailed error information", markup);
    }

    [Fact]
    public async Task HandleTestConnection_DisplaysTestedAtTimestamp()
    {
        // Arrange
        var testedAt = DateTime.UtcNow;
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            AvailableBranches = new List<string> { "main" },
            TestedAt = testedAt
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tested at", markup);
    }

    [Fact]
    public async Task HandleTestConnection_InvokesOnTestCompletedCallback()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            AvailableBranches = new List<string> { "main" },
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testResult);

        RepositoryConnectionTestResult? callbackResult = null;
        var callbackInvoked = false;

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token")
            .Add(p => p.OnTestCompleted, result =>
            {
                callbackResult = result;
                callbackInvoked = true;
            }));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(callbackResult);
        Assert.True(callbackResult.Success);
        Assert.Equal("Connection successful", callbackResult.Message);
    }

    [Fact]
    public async Task HandleTestConnection_WhenExceptionThrown_DisplaysErrorResult()
    {
        // Arrange
        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network error"));

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Connection Failed", markup);
        Assert.Contains("unexpected error", markup.ToLower());
    }

    [Fact]
    public async Task HandleTestConnection_CallsRepositoryServiceWithCorrectParameters()
    {
        // Arrange
        var cloneUrl = "https://github.com/test/repo.git";
        var accessToken = "test-token-123";

        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Success",
            TestedAt = DateTime.UtcNow
        };

        _mockRepositoryService
            .Setup(s => s.TestConnectionAsync(cloneUrl, accessToken))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, cloneUrl)
            .Add(p => p.AccessToken, accessToken));

        // Act
        var button = cut.Find("button");
        await cut.InvokeAsync(() => button.Click());

        // Assert
        _mockRepositoryService.Verify(s => s.TestConnectionAsync(cloneUrl, accessToken), Times.Once);
    }

    [Fact]
    public void Render_ShowsHelpText()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "test-token"));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Test the connection to your repository", markup);
    }
}
