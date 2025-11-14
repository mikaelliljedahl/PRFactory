using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using Xunit;

namespace PRFactory.Tests.Graphs;

/// <summary>
/// Comprehensive tests for PlanningGraph covering planning, parallel execution, suspension, approval, and rejection flows
/// </summary>
public class PlanningGraphTests
{
    private readonly Mock<ILogger<PlanningGraph>> _mockLogger;
    private readonly Mock<ICheckpointStore> _mockCheckpointStore;
    private readonly Mock<IAgentExecutor> _mockAgentExecutor;
    private readonly PlanningGraph _planningGraph;
    private readonly Guid _testTicketId;

    public PlanningGraphTests()
    {
        _mockLogger = new Mock<ILogger<PlanningGraph>>();
        _mockCheckpointStore = new Mock<ICheckpointStore>();
        _mockAgentExecutor = new Mock<IAgentExecutor>();
        _planningGraph = new PlanningGraph(_mockLogger.Object, _mockCheckpointStore.Object, _mockAgentExecutor.Object);
        _testTicketId = Guid.NewGuid();
    }

    #region Planning Agent Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithAnswersReceivedMessage_ExecutesPlanningAgent()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>
        {
            ["q1"] = "answer1"
        });

        var planMessage = new PlanGeneratedMessage(
            _testTicketId,
            "# Implementation Plan",
            "src/file.cs",
            "Unit tests",
            3
        );

        var gitMessage = new PlanCommittedMessage(_testTicketId, "plan/branch", "abc123", "http://git.com/branch");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

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
        var result = await _planningGraph.ExecuteAsync(inputMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_approval", result.State);
        Assert.IsType<MessagePostedMessage>(result.OutputMessage);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PlanningAgentReturnsInvalidMessage_ThrowsInvalidOperationException()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var invalidMessage = new MessagePostedMessage(_testTicketId, "invalid", DateTime.UtcNow);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var result = await _planningGraph.ExecuteAsync(inputMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Contains("Expected PlanGeneratedMessage", result.Error.Message);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task ExecuteAsync_GitPlanAndJiraPost_ExecuteInParallel()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        var gitExecutionOrder = 0;
        var jiraExecutionOrder = 0;
        var executionCounter = 0;

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                gitExecutionOrder = Interlocked.Increment(ref executionCounter);
                return gitMessage;
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

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ExecuteAsync(inputMessage);

        // Assert
        Assert.True(result.IsSuccess);

        // Both agents should have been executed
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<GitPlanAgent>(
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
        Assert.True(gitExecutionOrder > 0);
        Assert.True(jiraExecutionOrder > 0);
    }

    [Fact]
    public async Task ExecuteAsync_SavesGitCommitShaAndJiraPostedToContext()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "abc123def", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? savedState = null;

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "plan_posted",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "plan_posted"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _planningGraph.ExecuteAsync(inputMessage);

        // Assert
        Assert.NotNull(savedState);
        Assert.Equal("abc123def", savedState["git_commit_sha"]);
        Assert.True((bool)savedState["jira_posted"]);
    }

    #endregion

    #region Suspension Logic Tests

    [Fact]
    public async Task ExecuteAsync_AfterPostingPlan_SuspendsAwaitingApproval()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? suspendedState = null;

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "awaiting_approval",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => suspendedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "awaiting_approval"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ExecuteAsync(inputMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_approval", result.State);
        Assert.NotNull(suspendedState);
        Assert.True((bool)suspendedState["is_suspended"]);
        Assert.Equal("plan_approval", suspendedState["waiting_for"]);
    }

    [Fact]
    public async Task ExecuteAsync_SavesRetryCountZeroOnFirstExecution()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? suspendedState = null;

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "awaiting_approval",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => suspendedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "awaiting_approval"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _planningGraph.ExecuteAsync(inputMessage);

        // Assert
        Assert.NotNull(suspendedState);
        Assert.Equal(0, suspendedState["plan_retry_count"]);
    }

    #endregion

    #region Approval Flow Tests

    [Fact]
    public async Task ResumeAsync_WithPlanApprovedMessage_CompletesSuccessfully()
    {
        // Arrange
        var approvedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "user@example.com");
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["is_suspended"] = true
            },
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, approvedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("plan_approved", result.State);
        Assert.NotNull(result.OutputMessage);
        Assert.IsType<PlanApprovedEvent>(result.OutputMessage);

        var approvedEvent = (PlanApprovedEvent)result.OutputMessage;
        Assert.Equal(_testTicketId, approvedEvent.TicketId);
    }

    [Fact]
    public async Task ResumeAsync_WithPlanApproved_SavesApprovalDetails()
    {
        // Arrange
        var approvedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "approver@test.com");
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object> { ["current_state"] = "awaiting_approval" },
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        Dictionary<string, object>? savedState = null;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "plan_approved",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedState = state)
            .Returns(Task.CompletedTask);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, approvedMessage);

        // Assert
        Assert.NotNull(savedState);
        Assert.True((bool)savedState["is_completed"]);
        Assert.False((bool)savedState["is_suspended"]);
        Assert.Equal("approver@test.com", savedState["approved_by"]);
        Assert.NotNull(savedState["approved_at"]);
    }

    #endregion

    #region Rejection Flow Tests

    [Fact]
    public async Task ResumeAsync_WithPlanRejected_RegeneratesPlan()
    {
        // Arrange
        var rejectedMessage = new PlanRejectedMessage(
            _testTicketId,
            "Plan needs more detail",
            "Add more implementation details",
            false
        );

        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["plan_retry_count"] = 0
            },
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var planMessage = new PlanGeneratedMessage(_testTicketId, "revised plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_approval", result.State);

        // Should have called Planning agent again
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_WithPlanRejected_IncrementsRetryCount()
    {
        // Arrange
        var rejectedMessage = new PlanRejectedMessage(_testTicketId, "Try again", null, false);
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["plan_retry_count"] = 2
            },
            CreatedAt = DateTime.UtcNow
        };

        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? savedRejectionState = null;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "plan_rejected",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedRejectionState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "plan_rejected"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.NotNull(savedRejectionState);
        Assert.Equal(3, savedRejectionState["plan_retry_count"]);
        Assert.Equal("Try again", savedRejectionState["rejection_reason"]);
    }

    [Fact]
    public async Task ResumeAsync_WithPlanRejected_SavesRefinementInstructions()
    {
        // Arrange
        var rejectedMessage = new PlanRejectedMessage(
            _testTicketId,
            "Needs work",
            "Add more unit tests coverage",
            false
        );

        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["plan_retry_count"] = 0
            },
            CreatedAt = DateTime.UtcNow
        };

        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? savedState = null;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "plan_rejected",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "plan_rejected"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.NotNull(savedState);
        Assert.Equal("Add more unit tests coverage", savedState["refinement_instructions"]);
        Assert.False((bool)savedState["regenerate_completely"]);
    }

    [Fact]
    public async Task ResumeAsync_WithPlanRejected_RegenerateCompletelyFlag()
    {
        // Arrange
        var rejectedMessage = new PlanRejectedMessage(
            _testTicketId,
            "Start from scratch",
            null,
            true // RegenerateCompletely = true
        );

        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["plan_retry_count"] = 0
            },
            CreatedAt = DateTime.UtcNow
        };

        var planMessage = new PlanGeneratedMessage(_testTicketId, "plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? savedState = null;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "plan_rejected",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, _, state) => savedState = state)
            .Returns(Task.CompletedTask);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<string>(s => s != "plan_rejected"),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.NotNull(savedState);
        Assert.True((bool)savedState["regenerate_completely"]);
    }

    #endregion

    #region Retry Limit Tests

    [Fact]
    public async Task ResumeAsync_PlanRejectedAfterMaxRetries_FailsWorkflow()
    {
        // Arrange
        var rejectedMessage = new PlanRejectedMessage(_testTicketId, "Still not good enough", null, false);
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["plan_retry_count"] = 5 // Already at max retries
            },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("too_many_rejections", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("6 times", result.Error.Message);

        // Should NOT have called Planning agent
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_FifthRejection_StillRegenerates()
    {
        // Arrange
        var rejectedMessage = new PlanRejectedMessage(_testTicketId, "Try once more", null, false);
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object>
            {
                ["current_state"] = "awaiting_approval",
                ["plan_retry_count"] = 4 // One below max
            },
            CreatedAt = DateTime.UtcNow
        };

        var planMessage = new PlanGeneratedMessage(_testTicketId, "final plan", "files", "tests", 3);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_approval", result.State);

        // Should have regenerated
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_AgentExecutorThrowsException_ReturnsFailure()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanningAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Agent execution failed"));

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ExecuteAsync(inputMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("Agent execution failed", result.Error.Message);
    }

    [Fact]
    public async Task ResumeAsync_WithNoCheckpoint_ReturnsFailure()
    {
        // Arrange
        var approvedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "user@test.com");

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync((Checkpoint)null!);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, approvedMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("resume_failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("No checkpoint found", result.Error.Message);
    }

    [Fact]
    public async Task ResumeAsync_WithWrongMessageType_ThrowsInvalidOperationException()
    {
        // Arrange
        var wrongMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "awaiting_approval",
            State = new Dictionary<string, object> { ["current_state"] = "awaiting_approval" },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, wrongMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("resume_failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("Expected PlanApprovedMessage or PlanRejectedMessage", result.Error.Message);
    }

    [Fact]
    public async Task ResumeAsync_WithInvalidState_ThrowsInvalidOperationException()
    {
        // Arrange
        var approvedMessage = new PlanApprovedMessage(_testTicketId, DateTime.UtcNow, "user@test.com");
        var checkpoint = new Checkpoint
        {
            TicketId = _testTicketId,
            GraphId = "PlanningGraph",
            CheckpointId = "wrong_state",
            State = new Dictionary<string, object> { ["current_state"] = "wrong_state" },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _planningGraph.ResumeAsync(_testTicketId, approvedMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("resume_failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("Cannot resume from state wrong_state", result.Error.Message);
    }

    #endregion

    #region GraphId Test

    [Fact]
    public void GraphId_ReturnsPlanningGraph()
    {
        // Act
        var graphId = _planningGraph.GraphId;

        // Assert
        Assert.Equal("PlanningGraph", graphId);
    }

    #endregion
}
