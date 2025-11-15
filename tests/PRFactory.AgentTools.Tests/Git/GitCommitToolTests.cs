using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Git;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Git;

namespace PRFactory.AgentTools.Tests.Git;

public class GitCommitToolTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILocalGitService> _mockGitService;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;
    private readonly GitCommitTool _tool;

    public GitCommitToolTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockGitService = new Mock<ILocalGitService>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);

        _workspacePath = Path.Combine(Path.GetTempPath(), $"prfactory_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspacePath);

        _tool = new GitCommitTool(_mockLogger.Object, _mockTenantContext.Object, _mockGitService.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    [Fact]
    public async Task CommitFiles_ValidInput_CommitsSuccessfully()
    {
        // Arrange
        var repoPath = "test-repo";
        var fullRepoPath = Path.Combine(_workspacePath, repoPath);
        Directory.CreateDirectory(fullRepoPath);

        var files = new Dictionary<string, string>
        {
            { "file1.txt", "content1" },
            { "file2.txt", "content2" }
        };

        foreach (var (path, content) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(fullRepoPath, path), content);
        }

        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>
            {
                { "repositoryPath", repoPath },
                { "files", files },
                { "commitMessage", "Test commit" }
            }
        };

        _mockGitService.Setup(g => g.CommitAsync(fullRepoPath, files, "Test commit", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully committed 2 file(s)", result.Output);
        _mockGitService.Verify(g => g.CommitAsync(fullRepoPath, files, "Test commit", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CommitFiles_EmptyMessage_ReturnsError()
    {
        // Arrange
        var repoPath = "test-repo";
        var fullRepoPath = Path.Combine(_workspacePath, repoPath);
        Directory.CreateDirectory(fullRepoPath);

        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>
            {
                { "repositoryPath", repoPath },
                { "files", new Dictionary<string, string> { { "test.txt", "content" } } },
                { "commitMessage", "" }
            }
        };

        await File.WriteAllTextAsync(Path.Combine(fullRepoPath, "test.txt"), "content");

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Commit message cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task CommitFiles_TooManyFiles_ReturnsError()
    {
        // Arrange
        var repoPath = "test-repo";
        var fullRepoPath = Path.Combine(_workspacePath, repoPath);
        Directory.CreateDirectory(fullRepoPath);

        var files = new Dictionary<string, string>();
        for (int i = 0; i < 101; i++)
        {
            files[$"file{i}.txt"] = "content";
            await File.WriteAllTextAsync(Path.Combine(fullRepoPath, $"file{i}.txt"), "content");
        }

        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>
            {
                { "repositoryPath", repoPath },
                { "files", files },
                { "commitMessage", "Test" }
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot commit more than 100 files", result.ErrorMessage);
    }

    [Fact]
    public async Task CommitFiles_NonexistentRepository_ReturnsError()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>
            {
                { "repositoryPath", "nonexistent" },
                { "files", new Dictionary<string, string> { { "test.txt", "content" } } },
                { "commitMessage", "Test" }
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task CommitFiles_MissingParameters_ReturnsError()
    {
        // Arrange
        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("repositoryPath", result.ErrorMessage);
    }

    private ToolExecutionContext CreateContext()
    {
        return new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>()
        };
    }
}
