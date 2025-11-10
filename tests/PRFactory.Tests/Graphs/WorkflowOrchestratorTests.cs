using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using Xunit;
using ICheckpointStore = PRFactory.Infrastructure.Agents.ICheckpointStore;

namespace PRFactory.Tests.Graphs;

/// <summary>
/// Comprehensive tests for WorkflowOrchestrator - the core workflow coordination component
/// </summary>
public class WorkflowOrchestratorTests
{
    private readonly Mock<ILogger<WorkflowOrchestrator>> _mockLogger;
    private readonly Mock<RefinementGraph> _mockRefinementGraph;
    private readonly Mock<PlanningGraph> _mockPlanningGraph;
    private readonly Mock<ImplementationGraph> _mockImplementationGraph;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IEventPublisher> _mockEventPublisher;

    public WorkflowOrchestratorTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowOrchestrator>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        // Mock graphs - they need logger, checkpoint store, and agent executor
        var mockGraphLogger = new Mock<ILogger<RefinementGraph>>();
        var mockCheckpointStore = new Mock<ICheckpointStore>();
        var mockAgentExecutor = new Mock<IAgentExecutor>();

        _mockRefinementGraph = new Mock<RefinementGraph>(
            mockGraphLogger.Object,
            mockCheckpointStore.Object,
            mockAgentExecutor.Object);

        var mockPlanningLogger = new Mock<ILogger<PlanningGraph>>();
        _mockPlanningGraph = new Mock<PlanningGraph>(
            mockPlanningLogger.Object,
            mockCheckpointStore.Object,
            mockAgentExecutor.Object);

        var mockImplementationLogger = new Mock<ILogger<ImplementationGraph>>();
        _mockImplementationGraph = new Mock<ImplementationGraph>(
            mockImplementationLogger.Object,
            mockCheckpointStore.Object,
            mockAgentExecutor.Object);
    }

    #region StartWorkflowAsync Tests

    [Fact]
    public async Task StartWorkflowAsync_WithValidTriggerMessage_StartsRefinementGraph()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var suspendedResult = GraphExecutionResult.Suspended(
            "awaiting_answers",
            new MessagePostedMessage(ticketId, "questions", DateTime.UtcNow));

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedResult);

        // Act
        var workflowId = await orchestrator.StartWorkflowAsync(message);

        // Assert
        Assert.NotEqual(Guid.Empty, workflowId);
        _mockRefinementGraph.Verify(
            x => x.ExecuteAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartWorkflowAsync_WithValidTriggerMessage_SavesWorkflowState()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var suspendedResult = GraphExecutionResult.Suspended(
            "awaiting_answers",
            new MessagePostedMessage(ticketId, "questions", DateTime.UtcNow));

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedResult);

        WorkflowState? savedState = null;
        _mockStateStore
            .Setup(x => x.SaveStateAsync(It.IsAny<WorkflowState>()))
            .Callback<WorkflowState>(state => savedState = state)
            .Returns(Task.CompletedTask);

        // Act
        await orchestrator.StartWorkflowAsync(message);

        // Assert
        Assert.NotNull(savedState);
        Assert.Equal(ticketId, savedState!.TicketId);
        Assert.Equal("RefinementGraph", savedState.CurrentGraph);
        _mockStateStore.Verify(
            x => x.SaveStateAsync(It.IsAny<WorkflowState>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartWorkflowAsync_WhenRefinementGraphSuspends_UpdatesStatusToSuspended()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var suspendedResult = GraphExecutionResult.Suspended(
            "awaiting_answers",
            new MessagePostedMessage(ticketId, "questions", DateTime.UtcNow));

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedResult);

        // Act
        await orchestrator.StartWorkflowAsync(message);

        // Assert
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowSuspendedEvent>(e =>
                e.TicketId == ticketId &&
                e.GraphId == "RefinementGraph" &&
                e.State == "awaiting_answers")),
            Times.Once);
    }

    [Fact]
    public async Task StartWorkflowAsync_WhenRefinementGraphFails_UpdatesStatusToFailed()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var exception = new InvalidOperationException("Test error");
        var failedResult = GraphExecutionResult.Failure("failed", exception);

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // Act
        await orchestrator.StartWorkflowAsync(message);

        // Assert
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowFailedEvent>(e =>
                e.TicketId == ticketId &&
                e.GraphId == "RefinementGraph" &&
                e.Error == "Test error")),
            Times.Once);
    }

    [Fact]
    public async Task StartWorkflowAsync_WhenRefinementGraphThrowsException_UpdatesStatusToFailed()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.StartWorkflowAsync(message));

        _mockStateStore.Verify(
            x => x.UpdateStatusAsync(
                It.IsAny<Guid>(),
                WorkflowStatus.Failed,
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Graph Transition Tests

    [Fact]
    public async Task StartWorkflowAsync_WhenRefinementCompletes_TransitionsToPlanningGraph()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var refinementCompleteEvent = new RefinementCompleteEvent(ticketId, DateTime.UtcNow);
        var refinementResult = GraphExecutionResult.Success(
            "refinement_complete",
            refinementCompleteEvent,
            TimeSpan.FromMinutes(5));

        var planningResult = GraphExecutionResult.Suspended(
            "awaiting_plan_approval",
            new PlanCommittedMessage(ticketId, "plan-branch", "abc123", "http://example.com"));

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);

        _mockPlanningGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planningResult);

        // Act
        await orchestrator.StartWorkflowAsync(message);

        // Assert
        _mockPlanningGraph.Verify(
            x => x.ExecuteAsync(refinementCompleteEvent, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockStateStore.Verify(
            x => x.SaveStateAsync(It.Is<WorkflowState>(s =>
                s.CurrentGraph == "PlanningGraph" &&
                s.Status == WorkflowStatus.Running)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WhenPlanApproved_TransitionsToImplementationGraph()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "PlanningGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var planApprovedMessage = new PlanApprovedMessage(ticketId, DateTime.UtcNow, "user@example.com");
        var planApprovedEvent = new PlanApprovedEvent(ticketId, DateTime.UtcNow);
        var planningResult = GraphExecutionResult.Success(
            "plan_approved",
            planApprovedEvent,
            TimeSpan.FromMinutes(1));

        var implementationResult = GraphExecutionResult.Success(
            "implementation_complete",
            new PRCreatedMessage(ticketId, 42, "http://example.com/pr/42", DateTime.UtcNow),
            TimeSpan.FromMinutes(10));

        _mockPlanningGraph
            .Setup(x => x.ResumeAsync(ticketId, planApprovedMessage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planningResult);

        _mockImplementationGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationResult);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, planApprovedMessage);

        // Assert
        _mockImplementationGraph.Verify(
            x => x.ExecuteAsync(It.Is<PlanApprovedMessage>(m => m.TicketId == ticketId), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockStateStore.Verify(
            x => x.SaveStateAsync(It.Is<WorkflowState>(s =>
                s.CurrentGraph == "ImplementationGraph" &&
                s.Status == WorkflowStatus.Running)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WhenImplementationCompletes_MarksWorkflowCompleted()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "ImplementationGraph",
            Status = WorkflowStatus.Suspended,
            StartedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var implementationResult = GraphExecutionResult.Success(
            "implementation_complete",
            new PRCreatedMessage(ticketId, 42, "http://example.com/pr/42", DateTime.UtcNow),
            TimeSpan.FromMinutes(10));

        _mockImplementationGraph
            .Setup(x => x.ResumeAsync(ticketId, It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationResult);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, new PlanApprovedMessage(ticketId, DateTime.UtcNow, "user"));

        // Assert
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowCompletedEvent>(e =>
                e.TicketId == ticketId &&
                e.WorkflowId == workflowId)),
            Times.Once);

        _mockStateStore.Verify(
            x => x.SaveStateAsync(It.Is<WorkflowState>(s =>
                s.Status == WorkflowStatus.Completed &&
                s.CompletedAt != null)),
            Times.AtLeastOnce);
    }

    #endregion

    #region ResumeWorkflowAsync Tests

    [Fact]
    public async Task ResumeWorkflowAsync_WithNoExistingWorkflow_ThrowsInvalidOperationException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync((WorkflowState?)null);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ResumeWorkflowAsync(ticketId, message));

        Assert.Contains("No workflow found", exception.Message);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WithNonSuspendedWorkflow_ThrowsInvalidOperationException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "RefinementGraph",
            Status = WorkflowStatus.Running  // Not suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ResumeWorkflowAsync(ticketId, message));

        Assert.Contains("not in suspended state", exception.Message);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WithRefinementGraph_ResumesRefinementGraph()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "RefinementGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());
        var result = GraphExecutionResult.Suspended(
            "awaiting_approval",
            new TicketUpdateGeneratedMessage(ticketId, Guid.NewGuid(), 1, "Updated Title"));

        _mockRefinementGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, message);

        // Assert
        _mockRefinementGraph.Verify(
            x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WithPlanningGraph_ResumesPlanningGraph()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "PlanningGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new PlanApprovedMessage(ticketId, DateTime.UtcNow, "user@example.com");
        var result = GraphExecutionResult.Success(
            "plan_approved",
            new PlanApprovedEvent(ticketId, DateTime.UtcNow),
            TimeSpan.FromMinutes(1));

        _mockPlanningGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        _mockImplementationGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GraphExecutionResult.Success(
                "complete",
                new PRCreatedMessage(ticketId, 1, "url", DateTime.UtcNow),
                TimeSpan.FromMinutes(1)));

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, message);

        // Assert
        _mockPlanningGraph.Verify(
            x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WithImplementationGraph_ResumesImplementationGraph()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "ImplementationGraph",
            Status = WorkflowStatus.Suspended,
            StartedAt = DateTime.UtcNow
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new CodeImplementedMessage(
            ticketId,
            new Dictionary<string, string>(),
            new List<string>(),
            "Summary");
        var result = GraphExecutionResult.Success(
            "implementation_complete",
            new PRCreatedMessage(ticketId, 42, "http://example.com/pr/42", DateTime.UtcNow),
            TimeSpan.FromMinutes(10));

        _mockImplementationGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, message);

        // Assert
        _mockImplementationGraph.Verify(
            x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WithUnknownGraph_ThrowsInvalidOperationException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "UnknownGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ResumeWorkflowAsync(ticketId, message));

        Assert.Contains("Unknown graph", exception.Message);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WhenGraphFails_UpdatesStatusToFailed()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "RefinementGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());
        var exception = new InvalidOperationException("Test error");
        var failedResult = GraphExecutionResult.Failure("failed", exception);

        _mockRefinementGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, message);

        // Assert
        _mockStateStore.Verify(
            x => x.UpdateStatusAsync(workflowId, WorkflowStatus.Failed, It.IsAny<string>()),
            Times.Never); // Should be done through SaveStateAsync

        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowFailedEvent>(e =>
                e.TicketId == ticketId &&
                e.Error == "Test error")),
            Times.Once);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WhenGraphThrowsException_UpdatesStatusToFailedAndRethrows()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "RefinementGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());

        _mockRefinementGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ResumeWorkflowAsync(ticketId, message));

        _mockStateStore.Verify(
            x => x.UpdateStatusAsync(workflowId, WorkflowStatus.Failed, It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region GetWorkflowStatusAsync Tests

    [Fact]
    public async Task GetWorkflowStatusAsync_WithExistingWorkflow_ReturnsStatus()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowState = new WorkflowState
        {
            TicketId = ticketId,
            Status = WorkflowStatus.Running
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        // Act
        var status = await orchestrator.GetWorkflowStatusAsync(ticketId);

        // Assert
        Assert.Equal(WorkflowStatus.Running, status);
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithNoWorkflow_ReturnsNotFound()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync((WorkflowState?)null);

        // Act
        var status = await orchestrator.GetWorkflowStatusAsync(ticketId);

        // Assert
        Assert.Equal(WorkflowStatus.NotFound, status);
    }

    #endregion

    #region CancelWorkflowAsync Tests

    [Fact]
    public async Task CancelWorkflowAsync_WithExistingWorkflow_UpdatesStatusToCancelled()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            Status = WorkflowStatus.Running
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        // Act
        await orchestrator.CancelWorkflowAsync(ticketId);

        // Assert
        _mockStateStore.Verify(
            x => x.UpdateStatusAsync(workflowId, WorkflowStatus.Cancelled),
            Times.Once);
    }

    [Fact]
    public async Task CancelWorkflowAsync_WithExistingWorkflow_PublishesCancelledEvent()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            Status = WorkflowStatus.Running
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        // Act
        await orchestrator.CancelWorkflowAsync(ticketId);

        // Assert
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowCancelledEvent>(e =>
                e.TicketId == ticketId &&
                e.WorkflowId == workflowId)),
            Times.Once);
    }

    [Fact]
    public async Task CancelWorkflowAsync_WithNoWorkflow_ThrowsInvalidOperationException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync((WorkflowState?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.CancelWorkflowAsync(ticketId));

        Assert.Contains("No workflow found", exception.Message);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task StartWorkflowAsync_SavesInitialState_WithCorrectProperties()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var suspendedResult = GraphExecutionResult.Suspended(
            "awaiting_answers",
            new MessagePostedMessage(ticketId, "questions", DateTime.UtcNow));

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedResult);

        WorkflowState? savedState = null;
        _mockStateStore
            .Setup(x => x.SaveStateAsync(It.IsAny<WorkflowState>()))
            .Callback<WorkflowState>(state => savedState = state)
            .Returns(Task.CompletedTask);

        // Act
        await orchestrator.StartWorkflowAsync(message);

        // Assert
        Assert.NotNull(savedState);
        Assert.NotEqual(Guid.Empty, savedState!.WorkflowId);
        Assert.Equal(ticketId, savedState.TicketId);
        Assert.Equal("RefinementGraph", savedState.CurrentGraph);
        Assert.True(savedState.StartedAt <= DateTime.UtcNow);
        Assert.True(savedState.StartedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ResumeWorkflowAsync_UpdatesWorkflowStateToRunning()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "RefinementGraph",
            Status = WorkflowStatus.Suspended
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());
        var result = GraphExecutionResult.Suspended(
            "awaiting_approval",
            new TicketUpdateGeneratedMessage(ticketId, Guid.NewGuid(), 1, "Updated"));

        _mockRefinementGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, message);

        // Assert
        _mockStateStore.Verify(
            x => x.SaveStateAsync(It.Is<WorkflowState>(s =>
                s.Status == WorkflowStatus.Running)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Event Publishing Tests

    [Fact]
    public async Task StartWorkflowAsync_WhenGraphSuspends_PublishesSuspendedEvent()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var message = new TriggerTicketMessage(
            "TEST-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jira")
        {
            TicketId = ticketId
        };

        var suspendedResult = GraphExecutionResult.Suspended(
            "awaiting_answers",
            new MessagePostedMessage(ticketId, "questions", DateTime.UtcNow));

        _mockRefinementGraph
            .Setup(x => x.ExecuteAsync(It.IsAny<IAgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedResult);

        // Act
        await orchestrator.StartWorkflowAsync(message);

        // Assert
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowSuspendedEvent>(e =>
                e.TicketId == ticketId &&
                e.State == "awaiting_answers")),
            Times.Once);
    }

    [Fact]
    public async Task ResumeWorkflowAsync_WhenWorkflowCompletes_PublishesCompletedEvent()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var ticketId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var workflowState = new WorkflowState
        {
            WorkflowId = workflowId,
            TicketId = ticketId,
            CurrentGraph = "ImplementationGraph",
            Status = WorkflowStatus.Suspended,
            StartedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockStateStore
            .Setup(x => x.GetByTicketIdAsync(ticketId))
            .ReturnsAsync(workflowState);

        var message = new PlanApprovedMessage(ticketId, DateTime.UtcNow, "user");
        var result = GraphExecutionResult.Success(
            "implementation_complete",
            new PRCreatedMessage(ticketId, 42, "http://example.com/pr/42", DateTime.UtcNow),
            TimeSpan.FromMinutes(10));

        _mockImplementationGraph
            .Setup(x => x.ResumeAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await orchestrator.ResumeWorkflowAsync(ticketId, message);

        // Assert
        _mockEventPublisher.Verify(
            x => x.PublishAsync(It.Is<WorkflowCompletedEvent>(e =>
                e.TicketId == ticketId &&
                e.WorkflowId == workflowId &&
                e.Duration > TimeSpan.Zero)),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private WorkflowOrchestrator CreateOrchestrator()
    {
        return new WorkflowOrchestrator(
            _mockLogger.Object,
            _mockRefinementGraph.Object,
            _mockPlanningGraph.Object,
            _mockImplementationGraph.Object,
            _mockStateStore.Object,
            _mockEventPublisher.Object);
    }

    #endregion
}
