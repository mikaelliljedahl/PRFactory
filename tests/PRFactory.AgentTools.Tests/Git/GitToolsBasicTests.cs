using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Git;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Git;

namespace PRFactory.AgentTools.Tests.Git;

/// <summary>
/// Basic tests for all Git tools to ensure they compile and basic functionality works
/// </summary>
public class GitToolsBasicTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILocalGitService> _mockGitService;
    private readonly Mock<IGitPlatformService> _mockGitPlatformService;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;

    public GitToolsBasicTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockGitService = new Mock<ILocalGitService>();
        _mockGitPlatformService = new Mock<IGitPlatformService>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        _workspacePath = Path.Combine(Path.GetTempPath(), $"prfactory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspacePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    [Fact]
    public async Task GitCommitTool_MissingParameter_ReturnsError()
    {
        var tool = new GitCommitTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GitBranchTool_MissingParameter_ReturnsError()
    {
        var tool = new GitBranchTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GitPullRequestTool_MissingParameter_ReturnsError()
    {
        var tool = new GitPullRequestTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitPlatformService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GitDiffTool_MissingParameter_ReturnsError()
    {
        var tool = new GitDiffTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void GitCommitTool_HasCorrectName()
    {
        var tool = new GitCommitTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
        Assert.Equal("GitCommit", tool.Name);
    }

    [Fact]
    public void GitBranchTool_HasCorrectName()
    {
        var tool = new GitBranchTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
        Assert.Equal("GitBranch", tool.Name);
    }

    [Fact]
    public void GitPullRequestTool_HasCorrectName()
    {
        var tool = new GitPullRequestTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitPlatformService.Object);
        Assert.Equal("GitPullRequest", tool.Name);
    }

    [Fact]
    public void GitDiffTool_HasCorrectName()
    {
        var tool = new GitDiffTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
        Assert.Equal("GitDiff", tool.Name);
    }
}
