using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Analysis;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Analysis;

/// <summary>
/// Basic tests for all Analysis tools
/// </summary>
public class AnalysisToolsBasicTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;

    public AnalysisToolsBasicTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
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
    public async Task CodeSearchTool_MissingParameter_ReturnsError()
    {
        var tool = new CodeSearchTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DependencyMapTool_MissingParameter_ReturnsError()
    {
        var tool = new DependencyMapTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void CodeSearchTool_HasCorrectName()
    {
        var tool = new CodeSearchTool(_mockLogger.Object, _mockTenantContext.Object);
        Assert.Equal("CodeSearch", tool.Name);
    }

    [Fact]
    public void DependencyMapTool_HasCorrectName()
    {
        var tool = new DependencyMapTool(_mockLogger.Object, _mockTenantContext.Object);
        Assert.Equal("DependencyMap", tool.Name);
    }

    [Fact]
    public async Task DependencyMapTool_NoProjectFiles_ReturnsNoFilesMessage()
    {
        var tool = new DependencyMapTool(_mockLogger.Object, _mockTenantContext.Object);
        var repoPath = "test-repo";
        Directory.CreateDirectory(Path.Combine(_workspacePath, repoPath));

        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>
            {
                { "repositoryPath", repoPath }
            }
        };

        var result = await tool.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains("No project files", result.Output);
    }
}
