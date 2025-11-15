using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Command;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Tests.Command;

/// <summary>
/// Basic tests for all Command tools
/// </summary>
public class CommandToolsBasicTests : IDisposable
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;

    public CommandToolsBasicTests()
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
    public async Task ExecuteShellTool_MissingParameter_ReturnsError()
    {
        var tool = new ExecuteShellTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RunTestsTool_MissingParameter_ReturnsError()
    {
        var tool = new RunTestsTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BuildProjectTool_MissingParameter_ReturnsError()
    {
        var tool = new BuildProjectTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void ExecuteShellTool_HasCorrectName()
    {
        var tool = new ExecuteShellTool(_mockLogger.Object, _mockTenantContext.Object);
        Assert.Equal("ExecuteShell", tool.Name);
    }

    [Fact]
    public void RunTestsTool_HasCorrectName()
    {
        var tool = new RunTestsTool(_mockLogger.Object, _mockTenantContext.Object);
        Assert.Equal("RunTests", tool.Name);
    }

    [Fact]
    public void BuildProjectTool_HasCorrectName()
    {
        var tool = new BuildProjectTool(_mockLogger.Object, _mockTenantContext.Object);
        Assert.Equal("BuildProject", tool.Name);
    }

    [Fact]
    public async Task ExecuteShellTool_UnauthorizedCommand_ReturnsError()
    {
        var tool = new ExecuteShellTool(_mockLogger.Object, _mockTenantContext.Object);
        var context = new ToolExecutionContext
        {
            TenantId = _tenantId,
            WorkspacePath = _workspacePath,
            Parameters = new Dictionary<string, object>
            {
                { "command", "rm -rf /" }
            }
        };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("not whitelisted", result.ErrorMessage);
    }
}
