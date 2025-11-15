using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Jira;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Jira;

namespace PRFactory.AgentTools.Tests.Jira;

/// <summary>
/// Basic tests for all Jira tools
/// </summary>
public class JiraToolsBasicTests
{
    private readonly Mock<ILogger<ToolBase>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IJiraService> _mockJiraService;
    private readonly Guid _tenantId;
    private readonly string _workspacePath;

    public JiraToolsBasicTests()
    {
        _mockLogger = new Mock<ILogger<ToolBase>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockJiraService = new Mock<IJiraService>();
        _tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.GetCurrentTenantIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tenantId);
        _workspacePath = Path.GetTempPath();
    }

    [Fact]
    public async Task GetJiraTicketTool_MissingParameter_ReturnsError()
    {
        var tool = new GetJiraTicketTool(_mockLogger.Object, _mockTenantContext.Object, _mockJiraService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AddJiraCommentTool_MissingParameter_ReturnsError()
    {
        var tool = new AddJiraCommentTool(_mockLogger.Object, _mockTenantContext.Object, _mockJiraService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TransitionJiraTicketTool_MissingParameter_ReturnsError()
    {
        var tool = new TransitionJiraTicketTool(_mockLogger.Object, _mockTenantContext.Object, _mockJiraService.Object);
        var context = new ToolExecutionContext { TenantId = _tenantId, WorkspacePath = _workspacePath };

        var result = await tool.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public void GetJiraTicketTool_HasCorrectName()
    {
        var tool = new GetJiraTicketTool(_mockLogger.Object, _mockTenantContext.Object, _mockJiraService.Object);
        Assert.Equal("GetJiraTicket", tool.Name);
    }

    [Fact]
    public void AddJiraCommentTool_HasCorrectName()
    {
        var tool = new AddJiraCommentTool(_mockLogger.Object, _mockTenantContext.Object, _mockJiraService.Object);
        Assert.Equal("AddJiraComment", tool.Name);
    }

    [Fact]
    public void TransitionJiraTicketTool_HasCorrectName()
    {
        var tool = new TransitionJiraTicketTool(_mockLogger.Object, _mockTenantContext.Object, _mockJiraService.Object);
        Assert.Equal("TransitionJiraTicket", tool.Name);
    }
}
