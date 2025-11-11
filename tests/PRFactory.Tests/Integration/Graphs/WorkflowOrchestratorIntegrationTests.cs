using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Tests.Builders;
using Xunit;

namespace PRFactory.Tests.Integration.Graphs;

/// <summary>
/// Integration tests for WorkflowOrchestrator with CodeReviewGraph.
/// Tests the complete workflow including graph transitions and state management.
/// </summary>
public class WorkflowOrchestratorIntegrationTests
{
    private readonly Mock<ILogger<WorkflowOrchestrator>> _mockLogger;
    private readonly Mock<RefinementGraph> _mockRefinementGraph;
    private readonly Mock<PlanningGraph> _mockPlanningGraph;
    private readonly Mock<ImplementationGraph> _mockImplementationGraph;
    private readonly Mock<CodeReviewGraph> _mockCodeReviewGraph;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ITenantRepository> _mockTenantRepo;
    private readonly Mock<ITenantConfigurationService> _mockTenantConfigService;

    public WorkflowOrchestratorIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowOrchestrator>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockTenantRepo = new Mock<ITenantRepository>();
        _mockTenantConfigService = new Mock<ITenantConfigurationService>();

        // Setup graphs
        var mockCheckpointStore = new Mock<ICheckpointStore>();
        var mockAgentExecutor = new Mock<IAgentExecutor>();

        _mockRefinementGraph = new Mock<RefinementGraph>(
            Mock.Of<ILogger<RefinementGraph>>(),
            mockCheckpointStore.Object,
            mockAgentExecutor.Object);

        _mockPlanningGraph = new Mock<PlanningGraph>(
            Mock.Of<ILogger<PlanningGraph>>(),
            mockCheckpointStore.Object,
            mockAgentExecutor.Object);

        _mockImplementationGraph = new Mock<ImplementationGraph>(
            Mock.Of<ILogger<ImplementationGraph>>(),
            mockCheckpointStore.Object,
            mockAgentExecutor.Object,
            _mockTenantConfigService.Object);

        _mockCodeReviewGraph = new Mock<CodeReviewGraph>(
            Mock.Of<ILogger<CodeReviewGraph>>(),
            mockCheckpointStore.Object,
            mockAgentExecutor.Object);
    }

    private WorkflowOrchestrator CreateOrchestrator()
    {
        return new WorkflowOrchestrator(
            _mockLogger.Object,
            _mockRefinementGraph.Object,
            _mockPlanningGraph.Object,
            _mockImplementationGraph.Object,
            _mockCodeReviewGraph.Object,
            _mockStateStore.Object,
            _mockEventPublisher.Object,
            _mockTenantRepo.Object,
            _mockTenantConfigService.Object);
    }

    #region HandleImplementationCompletion Tests

    [Fact]
    public async Task HandleImplementationCompletion_WithAutoReviewEnabled_TransitionsToCodeReview()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        tenant.UpdateConfiguration(new TenantConfiguration
        {
            EnableAutoCodeReview = true,
            MaxCodeReviewIterations = 3
        });

        _mockTenantRepo
            .Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var prCreatedMessage = new PRCreatedMessage(
            ticketId,
            "https://github.com/test/repo/pull/123",
            123,
            "feature/test",
            DateTime.UtcNow);

        var codeReviewResult = GraphExecutionResult.Success(
            "review_approved",
            new CodeReviewCompleteMessage(
                ticketId,
                HasCriticalIssues: false,
                Issues: new List<CodeIssue>(),
                ReviewContent: "## No issues found",
                CompletedAt: DateTime.UtcNow),
            TimeSpan.FromSeconds(30));

        _mockCodeReviewGraph
            .Setup(g => g.ExecuteAsync(
                It.IsAny<ReviewCodeMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeReviewResult);

        _mockStateStore
            .Setup(s => s.SaveAsync(It.IsAny<WorkflowState>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await orchestrator.HandleImplementationCompletionAsync(
            ticketId, tenantId, prCreatedMessage);

        // Assert
        Assert.True(result);

        // Verify code review graph was executed
        _mockCodeReviewGraph.Verify(
            g => g.ExecuteAsync(
                It.Is<ReviewCodeMessage>(m =>
                    m.TicketId == ticketId &&
                    m.PullRequestNumber == 123),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleImplementationCompletion_WithAutoReviewDisabled_CompletesWorkflow()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        tenant.UpdateConfiguration(new TenantConfiguration
        {
            EnableAutoCodeReview = false // Disabled
        });

        _mockTenantRepo
            .Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var prCreatedMessage = new PRCreatedMessage(
            ticketId,
            "https://github.com/test/repo/pull/123",
            123,
            "feature/test",
            DateTime.UtcNow);

        _mockStateStore
            .Setup(s => s.SaveAsync(It.IsAny<WorkflowState>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await orchestrator.HandleImplementationCompletionAsync(
            ticketId, tenantId, prCreatedMessage);

        // Assert
        Assert.True(result);

        // Verify code review graph was NOT executed
        _mockCodeReviewGraph.Verify(
            g => g.ExecuteAsync(
                It.IsAny<ReviewCodeMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region HandleCodeReviewCompletion Tests

    [Fact]
    public async Task HandleCodeReviewCompletion_WithNoIssues_CompletesWorkflow()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var reviewCompleteMessage = new CodeReviewCompleteMessage(
            ticketId,
            HasCriticalIssues: false,
            Issues: new List<CodeIssue>(),
            ReviewContent: "## Review passed",
            CompletedAt: DateTime.UtcNow);

        var workflowState = new WorkflowState
        {
            TicketId = ticketId,
            TenantId = tenantId,
            CurrentState = "code_review_complete",
            GraphData = new Dictionary<string, object>
            {
                ["review_retry_count"] = 0
            }
        };

        _mockStateStore
            .Setup(s => s.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        _mockStateStore
            .Setup(s => s.SaveAsync(It.IsAny<WorkflowState>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await orchestrator.HandleCodeReviewCompletionAsync(
            ticketId, reviewCompleteMessage);

        // Assert
        Assert.True(result);

        // Verify workflow was marked as complete
        _mockStateStore.Verify(
            s => s.SaveAsync(
                It.Is<WorkflowState>(ws =>
                    ws.TicketId == ticketId &&
                    ws.CurrentState == "workflow_complete")),
            Times.Once);
    }

    [Fact]
    public async Task HandleCodeReviewCompletion_WithIssues_LoopsToImplementation()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var issues = new List<CodeIssue>
        {
            new CodeIssue(
                Severity: "critical",
                FilePath: "Service.cs",
                LineNumber: 42,
                Description: "SQL injection vulnerability")
        };

        var reviewCompleteMessage = new CodeReviewCompleteMessage(
            ticketId,
            HasCriticalIssues: true,
            Issues: issues,
            ReviewContent: "## Critical issues found",
            CompletedAt: DateTime.UtcNow);

        var workflowState = new WorkflowState
        {
            TicketId = ticketId,
            TenantId = tenantId,
            CurrentState = "code_review_complete",
            GraphData = new Dictionary<string, object>
            {
                ["review_retry_count"] = 0 // Within limit
            }
        };

        _mockStateStore
            .Setup(s => s.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        _mockStateStore
            .Setup(s => s.SaveAsync(It.IsAny<WorkflowState>()))
            .Returns(Task.CompletedTask);

        var implementationResult = GraphExecutionResult.Success(
            "implementation_complete",
            new PRCreatedMessage(ticketId, "https://github.com/test/repo/pull/124", 124, "feature/fix", DateTime.UtcNow),
            TimeSpan.FromMinutes(5));

        _mockImplementationGraph
            .Setup(g => g.ExecuteAsync(
                It.IsAny<FixCodeIssuesMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationResult);

        // Act
        var result = await orchestrator.HandleCodeReviewCompletionAsync(
            ticketId, reviewCompleteMessage);

        // Assert
        Assert.True(result);

        // Verify implementation graph was executed with fix message
        _mockImplementationGraph.Verify(
            g => g.ExecuteAsync(
                It.Is<FixCodeIssuesMessage>(m =>
                    m.TicketId == ticketId &&
                    m.Issues.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCodeReviewCompletion_AtMaxRetries_CompletesWithWarnings()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new TenantBuilder()
            .WithId(tenantId)
            .Build();

        tenant.UpdateConfiguration(new TenantConfiguration
        {
            EnableAutoCodeReview = true,
            MaxCodeReviewIterations = 3
        });

        _mockTenantRepo
            .Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var issues = new List<CodeIssue>
        {
            new CodeIssue("critical", "Service.cs", 42, "Issue still present")
        };

        var reviewCompleteMessage = new CodeReviewCompleteMessage(
            ticketId,
            HasCriticalIssues: true,
            Issues: issues,
            ReviewContent: "## Issues still present",
            CompletedAt: DateTime.UtcNow);

        var workflowState = new WorkflowState
        {
            TicketId = ticketId,
            TenantId = tenantId,
            CurrentState = "code_review_complete",
            GraphData = new Dictionary<string, object>
            {
                ["review_retry_count"] = 3 // At max limit
            }
        };

        _mockStateStore
            .Setup(s => s.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        _mockStateStore
            .Setup(s => s.SaveAsync(It.IsAny<WorkflowState>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await orchestrator.HandleCodeReviewCompletionAsync(
            ticketId, reviewCompleteMessage);

        // Assert
        Assert.True(result);

        // Verify implementation graph was NOT executed (max retries reached)
        _mockImplementationGraph.Verify(
            g => g.ExecuteAsync(
                It.IsAny<FixCodeIssuesMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify workflow was marked complete with warnings
        _mockStateStore.Verify(
            s => s.SaveAsync(
                It.Is<WorkflowState>(ws =>
                    ws.TicketId == ticketId &&
                    ws.CurrentState.Contains("complete"))),
            Times.Once);
    }

    #endregion
}
