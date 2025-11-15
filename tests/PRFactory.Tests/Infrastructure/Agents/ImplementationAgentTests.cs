using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Claude;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Tests.Infrastructure.Agents;

public class ImplementationAgentTests
{
    private readonly Mock<ILogger<ImplementationAgent>> _loggerMock;
    private readonly Mock<ICliAgent> _cliAgentMock;
    private readonly Mock<IContextBuilder> _contextBuilderMock;
    private readonly Mock<ITicketRepository> _ticketRepositoryMock;
    private readonly Mock<ILocalGitService> _localGitServiceMock;
    private readonly Mock<IWorkspaceService> _workspaceServiceMock;

    public ImplementationAgentTests()
    {
        _loggerMock = new Mock<ILogger<ImplementationAgent>>();
        _cliAgentMock = new Mock<ICliAgent>();
        _contextBuilderMock = new Mock<IContextBuilder>();
        _ticketRepositoryMock = new Mock<ITicketRepository>();
        _localGitServiceMock = new Mock<ILocalGitService>();
        _workspaceServiceMock = new Mock<IWorkspaceService>();
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesDiff_AfterCodeImplementation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create("TEST-123", tenantId, repositoryId, "Jira", TicketSource.WebUI);

        // Use reflection to set private ID since we can't set it directly
        var idProperty = typeof(Ticket).GetProperty("Id");
        idProperty!.SetValue(ticket, ticketId);

        var tenant = Tenant.Create("Test Tenant", "test", "test@example.com", "test-domain", "google", "pk_test", "sk_test");

        // Enable auto-implementation for test via reflection
        var configProperty = typeof(Tenant).GetProperty("Configuration");
        var config = configProperty!.GetValue(tenant) as TenantConfiguration;
        config!.AutoImplementAfterPlanApproval = true;

        var context = new AgentContext
        {
            Ticket = ticket,
            Tenant = tenant,
            RepositoryPath = "/test/repo/path",
            ImplementationPlan = "Test implementation plan"
        };

        var cliResponse = new CliAgentResponse
        {
            Success = true,
            Content = @"{""files"": [{""path"": ""test.cs"", ""content"": ""// test"", ""action"": ""create""}]}"
        };

        var diffContent = @"diff --git a/test.cs b/test.cs
new file mode 100644
index 0000000..abc123
--- /dev/null
+++ b/test.cs
@@ -0,0 +1,5 @@
+using System;
+
+public class Test {
+    // test
+}";

        _contextBuilderMock
            .Setup(x => x.BuildImplementationContextAsync(It.IsAny<Ticket>(), "/test/repo/path"))
            .ReturnsAsync("Test codebase context");

        _cliAgentMock
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                "/test/repo/path",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliResponse);

        _cliAgentMock
            .Setup(x => x.AgentName)
            .Returns("TestAgent");

        _localGitServiceMock
            .Setup(x => x.GetDiffAsync(
                "/test/repo/path",
                null,
                "main",
                null))
            .ReturnsAsync(diffContent);

        _workspaceServiceMock
            .Setup(x => x.WriteDiffAsync(ticketId, diffContent))
            .Returns(Task.CompletedTask);

        var agent = new ImplementationAgent(
            _loggerMock.Object,
            _cliAgentMock.Object,
            _contextBuilderMock.Object,
            _ticketRepositoryMock.Object,
            _localGitServiceMock.Object,
            _workspaceServiceMock.Object);

        // Act
        var result = await agent.ExecuteWithMiddlewareAsync(context);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);

        // Verify diff was generated
        _localGitServiceMock.Verify(
            x => x.GetDiffAsync("/test/repo/path", null, "main", null),
            Times.Once);

        // Verify diff was written to workspace
        _workspaceServiceMock.Verify(
            x => x.WriteDiffAsync(ticketId, diffContent),
            Times.Once);
    }

}
