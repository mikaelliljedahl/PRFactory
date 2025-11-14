using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Configuration;
using Xunit;
using Checkpoint = PRFactory.Infrastructure.Agents.Base.Checkpoint;

namespace PRFactory.Tests.Graphs;

/// <summary>
/// Comprehensive tests for ImplementationGraph covering configuration checking,
/// implementation execution, parallel PR/Jira posting, checkpointing, and error handling
/// </summary>
public class ImplementationGraphTests
{
    private readonly Mock<ILogger<ImplementationGraph>> _mockLogger;
    private readonly Mock<ICheckpointStore> _mockCheckpointStore;
    private readonly Mock<IAgentExecutor> _mockAgentExecutor;
    private readonly Mock<ITenantConfigurationService> _mockTenantConfigService;
    private readonly ImplementationGraph _implementationGraph;
    private readonly Guid _testTicketId;

    public ImplementationGraphTests()
    {
        _mockLogger = new Mock<ILogger<ImplementationGraph>>();
        _mockCheckpointStore = new Mock<ICheckpointStore>();
        _mockAgentExecutor = new Mock<IAgentExecutor>();
        _mockTenantConfigService = new Mock<ITenantConfigurationService>();
        _implementationGraph = new ImplementationGraph(
            _mockLogger.Object,
            _mockCheckpointStore.Object,
            _mockAgentExecutor.Object,
            _mockTenantConfigService.Object
        );
        _testTicketId = Guid.NewGuid();
    }

    #region Configuration Check Tests

    [Fact]
    public async Task ExecuteAsync_ChecksTenantConfigurationForAutoImplement()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(
            _testTicketId,
            DateTime.UtcNow,
            "approver@test.com"
        );

        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        _mockTenantConfigService.Verify(
            x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAutoImplementDisabled_SkipsImplementation()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = false };

        Dictionary<string, object>? savedState = null;

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "skipped",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedState = state)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("skipped", result.State);
        Assert.NotNull(savedState);
        Assert.True((bool)savedState["is_completed"]);
        Assert.True((bool)savedState["skipped"]);
        Assert.Equal("auto_implementation_disabled", savedState["skip_reason"]);

        // Should NOT execute any agents
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigurationNotFound_SkipsImplementation()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantConfiguration?)null);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("skipped", result.State);

        // Should NOT execute implementation
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigurationThrowsException_DefaultsToDisabled()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Config service error"));

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("skipped", result.State);

        // Should NOT execute implementation (defaults to disabled on error)
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Implementation Execution Tests

    [Fact]
    public async Task ExecuteAsync_WhenAutoImplementEnabled_RunsImplementationAgent()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ImplementationAgent>(
                It.Is<PlanApprovedMessage>(m => m.TicketId == _testTicketId),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AfterImplementation_CommitsCodeChanges()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        var codeImplementedMessage = new CodeImplementedMessage(
            _testTicketId,
            new Dictionary<string, string> { ["src/file.cs"] = "updated content" },
            new List<string> { "src/new.cs" },
            "Implemented feature X"
        );

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeImplementedMessage);

        SetupGitCommitAndPRFlow();

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<GitCommitAgent>(
                It.Is<CodeImplementedMessage>(m => m.TicketId == _testTicketId),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SavesCheckpointAfterImplementation()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "code_implemented",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task ExecuteAsync_RunsPullRequestAndJiraPostInParallel()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        var prExecutionOrder = 0;
        var jiraExecutionOrder = 0;
        var executionCounter = 0;

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupImplementationAndGitCommitAgents();

        var prMessage = new PRCreatedMessage(_testTicketId, 123, "https://github.com/pr/123", DateTime.UtcNow);
        var jiraMessage = new MessagePostedMessage(_testTicketId, "implementation_complete", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                prExecutionOrder = Interlocked.Increment(ref executionCounter);
                return prMessage;
            });

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                jiraExecutionOrder = Interlocked.Increment(ref executionCounter);
                return jiraMessage;
            });

        SetupCompletionAgent();

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);

        // Both should have executed
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Both should have executed (order may vary due to parallelism)
        Assert.True(prExecutionOrder > 0);
        Assert.True(jiraExecutionOrder > 0);
    }

    [Fact]
    public async Task ExecuteAsync_SavesPRDetailsToContext()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        var prMessage = new PRCreatedMessage(
            _testTicketId,
            456,
            "https://github.com/repo/pull/456",
            DateTime.UtcNow
        );
        var jiraMessage = new MessagePostedMessage(_testTicketId, "implementation_complete", DateTime.UtcNow);

        Dictionary<string, object>? savedState = null;

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupImplementationAndGitCommitAgents();

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "pr_created",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "pr_created"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        SetupCompletionAgent();

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.NotNull(savedState);
        Assert.Equal(456, savedState["pr_number"]);
        Assert.Equal("https://github.com/repo/pull/456", savedState["pr_url"]);
        Assert.True((bool)savedState["jira_posted"]);
    }

    #endregion

    #region Workflow Completion Tests

    [Fact]
    public async Task ExecuteAsync_AfterPRCreation_RunsCompletionAgent()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<CompletionAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesWorkflowSuccessfully()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        Dictionary<string, object>? completedState = null;

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "completed",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => completedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "completed"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("completed", result.State);
        Assert.NotNull(completedState);
        Assert.True((bool)completedState["is_completed"]);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCompletionMessageAsOutput()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        var completionMessage = new WorkflowCompletedMessage(
            _testTicketId,
            "completed",
            TimeSpan.FromMinutes(5),
            new Dictionary<string, object>()
        );

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupImplementationAndGitCommitAgents();

        var prMessage = new PRCreatedMessage(_testTicketId, 123, "https://github.com/pr/123", DateTime.UtcNow);
        var jiraMessage = new MessagePostedMessage(_testTicketId, "implementation_complete", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<CompletionAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(completionMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.OutputMessage);
        Assert.IsType<WorkflowCompletedMessage>(result.OutputMessage);

        var outputMessage = (WorkflowCompletedMessage)result.OutputMessage;
        Assert.Equal(_testTicketId, outputMessage.TicketId);
        Assert.Equal("completed", outputMessage.FinalState);
    }

    #endregion

    #region Checkpoint Tests

    [Fact]
    public async Task ExecuteAsync_SavesCheckpointsAfterEachStage()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert - Verify all checkpoint saves
        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "code_implemented",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once,
            "Should save checkpoint after implementation");

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "code_committed",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once,
            "Should save checkpoint after git commit");

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "pr_created",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once,
            "Should save checkpoint after PR creation");

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "completed",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once,
            "Should save checkpoint after completion");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenImplementationAgentFails_ReturnsFailure()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Implementation failed"));

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("Implementation failed", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenImplementationAgentFails_SavesFailureCheckpoint()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        Dictionary<string, object>? failedState = null;

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Implementation error"));

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "ImplementationGraph",
                "failed",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => failedState = state)
            .Returns(Task.CompletedTask);

        // Act
        await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.NotNull(failedState);
        Assert.True((bool)failedState["is_failed"]);
        Assert.Equal("Implementation error", failedState["error"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPRCreationFails_ReturnsFailure()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupImplementationAndGitCommitAgents();

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("PR creation failed"));

        var jiraMessage = new MessagePostedMessage(_testTicketId, "implementation_complete", DateTime.UtcNow);
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("PR creation failed", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenImplementationReturnsWrongMessageType_ReturnsFailure()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        var wrongMessage = new MessagePostedMessage(_testTicketId, "wrong", DateTime.UtcNow);

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wrongMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(planApprovedMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Contains("Expected CodeImplementedMessage", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongInputMessageType_ThrowsInvalidOperationException()
    {
        // Arrange
        var wrongMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ExecuteAsync(wrongMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Contains("Expected PlanApprovedMessage", result.Error.Message);
    }

    #endregion

    #region Resume Tests

    [Fact]
    public async Task ResumeAsync_WithPlanApprovedMessage_RestartsFromBeginning()
    {
        // Arrange
        var planApprovedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var config = new TenantConfiguration { AutoImplementAfterPlanApproval = true };

        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "ImplementationGraph",
            CheckpointId = "code_committed",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "code_committed"
            },
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "ImplementationGraph"))
            .ReturnsAsync(checkpoint);

        _mockTenantConfigService
            .Setup(x => x.GetConfigurationForTicketAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupSuccessfulImplementationFlow();

        // Act
        var result = await _implementationGraph.ResumeAsync(_testTicketId, planApprovedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("completed", result.State);

        // Should re-execute the entire flow
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_WithWrongMessageType_ThrowsException()
    {
        // Arrange
        var wrongMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());

        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "ImplementationGraph",
            CheckpointId = "code_committed",
            State = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "ImplementationGraph"))
            .ReturnsAsync(checkpoint);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _implementationGraph.ResumeAsync(_testTicketId, wrongMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Contains("Cannot resume ImplementationGraph", result.Error.Message);
    }

    #endregion

    #region GraphId Test

    [Fact]
    public void GraphId_ReturnsImplementationGraph()
    {
        // Act
        var graphId = _implementationGraph.GraphId;

        // Assert
        Assert.Equal("ImplementationGraph", graphId);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulImplementationFlow()
    {
        SetupImplementationAndGitCommitAgents();

        var prMessage = new PRCreatedMessage(_testTicketId, 123, "https://github.com/pr/123", DateTime.UtcNow);
        var jiraMessage = new MessagePostedMessage(_testTicketId, "implementation_complete", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        SetupCompletionAgent();

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupImplementationAndGitCommitAgents()
    {
        var codeImplementedMessage = new CodeImplementedMessage(
            _testTicketId,
            new Dictionary<string, string> { ["src/file.cs"] = "content" },
            new List<string>(),
            "Implemented feature"
        );

        var committedMessage = new MessagePostedMessage(_testTicketId, "committed", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeImplementedMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitCommitAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(committedMessage);
    }

    private void SetupGitCommitAndPRFlow()
    {
        var committedMessage = new MessagePostedMessage(_testTicketId, "committed", DateTime.UtcNow);
        var prMessage = new PRCreatedMessage(_testTicketId, 123, "https://github.com/pr/123", DateTime.UtcNow);
        var jiraMessage = new MessagePostedMessage(_testTicketId, "implementation_complete", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitCommitAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(committedMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PullRequestAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        SetupCompletionAgent();

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupCompletionAgent()
    {
        var completionMessage = new WorkflowCompletedMessage(
            _testTicketId,
            "completed",
            TimeSpan.FromMinutes(5),
            new Dictionary<string, object>()
        );

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<CompletionAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(completionMessage);
    }

    #endregion
}
