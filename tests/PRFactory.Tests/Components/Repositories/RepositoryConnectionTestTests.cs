using Bunit;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Repositories;

public class RepositoryConnectionTestTests : ComponentTestBase
{
    private readonly Mock<ILogger<RepositoryConnectionTest>> _mockLogger = new();

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_mockLogger.Object);

        // Note: IRepositoryService is already registered by TestContextBase
    }

    [Fact]
    public void Render_WithValidParameters_ShowsTestButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Assert
        Assert.Contains("Test Connection", cut.Markup);
        var button = cut.Find("button:contains('Test Connection')");
        Assert.NotNull(button);
    }

    [Fact]
    public void Render_WithEmptyCloneUrl_DisablesTestButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "")
            .Add(p => p.AccessToken, "token123"));

        // Assert
        var button = cut.Find("button:contains('Test Connection')");
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
        var button = cut.Find("button:contains('Test Connection')");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public async Task TestConnection_WhenSuccessful_ShowsSuccessMessage()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connection successful!",
            AvailableBranches = new List<string> { "main", "develop" },
            TestedAt = DateTime.UtcNow
        };

        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Connection successful!"));
        Assert.Contains("Success", cut.Markup);
        Assert.Contains("main", cut.Markup);
        Assert.Contains("develop", cut.Markup);
    }

    [Fact]
    public async Task TestConnection_WhenFailed_ShowsErrorMessage()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = false,
            Message = "Authentication failed",
            ErrorDetails = "Invalid credentials",
            TestedAt = DateTime.UtcNow
        };

        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Authentication failed"));
        Assert.Contains("Connection Failed", cut.Markup);
        Assert.Contains("Authentication failed", cut.Markup);
    }

    [Fact]
    public async Task TestConnection_WhenSuccessfulWithManyBranches_ShowsFirst10Branches()
    {
        // Arrange
        var branches = Enumerable.Range(1, 15).Select(i => $"branch-{i}").ToList();
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connected",
            AvailableBranches = branches,
            TestedAt = DateTime.UtcNow
        };

        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("branch-1"));
        Assert.Contains("branch-1", cut.Markup);
        Assert.Contains("branch-10", cut.Markup);
        Assert.Contains("+5 more", cut.Markup);
    }

    [Fact]
    public async Task TestConnection_WhenException_ShowsGenericErrorMessage()
    {
        // Arrange
        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("unexpected error"));
        Assert.Contains("An unexpected error occurred while testing the connection", cut.Markup);
    }

    [Fact]
    public async Task TestConnection_InvokesCallback_WhenTestCompleted()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connected",
            AvailableBranches = new List<string> { "main" },
            TestedAt = DateTime.UtcNow
        };

        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        RepositoryConnectionTestResult? callbackResult = null;
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123")
            .Add(p => p.OnTestCompleted, (result) => { callbackResult = result; }));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => callbackResult != null);
        Assert.NotNull(callbackResult);
        Assert.True(callbackResult.Success);
        Assert.Equal("Connected", callbackResult.Message);
    }

    [Fact]
    public async Task TestConnection_WithErrorDetails_ShowsErrorDetailsToggle()
    {
        // Arrange
        var testResult = new RepositoryConnectionTestResult
        {
            Success = false,
            Message = "Connection failed",
            ErrorDetails = "Detailed error information",
            TestedAt = DateTime.UtcNow
        };

        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123")
            .Add(p => p.ShowErrorDetails, true));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Show Error Details"));
        Assert.Contains("Show Error Details", cut.Markup);
    }

    [Fact]
    public async Task TestConnection_ShowsTestedAtTimestamp()
    {
        // Arrange
        var testedAt = DateTime.UtcNow;
        var testResult = new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connected",
            AvailableBranches = new List<string>(),
            TestedAt = testedAt
        };

        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Tested at"));
        Assert.Contains("Tested at", cut.Markup);
    }

    [Fact]
    public async Task TestConnection_DisablesButton_WhileTesting()
    {
        // Arrange
        var tcs = new TaskCompletionSource<RepositoryConnectionTestResult>();
        MockRepositoryService
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Act
        var testButton = cut.Find("button:contains('Test Connection')");
        await testButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("spinner"));
        Assert.Contains("spinner", cut.Markup.ToLower());

        // Complete the test
        tcs.SetResult(new RepositoryConnectionTestResult
        {
            Success = true,
            Message = "Connected",
            TestedAt = DateTime.UtcNow
        });
    }

    [Fact]
    public void Render_ShowsConnectionInstructions()
    {
        // Arrange & Act
        var cut = RenderComponent<RepositoryConnectionTest>(parameters => parameters
            .Add(p => p.CloneUrl, "https://github.com/test/repo.git")
            .Add(p => p.AccessToken, "token123"));

        // Assert
        Assert.Contains("Test the connection to your repository", cut.Markup);
    }
}
