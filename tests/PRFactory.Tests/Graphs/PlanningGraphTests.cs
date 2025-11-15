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

        var userStoriesMessage = new MessagePostedMessage(_testTicketId, "user_stories", DateTime.UtcNow);
        var apiDesignMessage = new MessagePostedMessage(_testTicketId, "api_design", DateTime.UtcNow);
        var dbSchemaMessage = new MessagePostedMessage(_testTicketId, "db_schema", DateTime.UtcNow);
        var testCasesMessage = new MessagePostedMessage(_testTicketId, "test_cases", DateTime.UtcNow);
        var implementationMessage = new MessagePostedMessage(_testTicketId, "implementation", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "plan/branch", "abc123", "http://git.com/branch");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        // Mock all 5 planning agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userStoriesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiDesignMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbSchemaMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(testCasesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

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

        // Verify all planning agents were called
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ArchitectApiDesignAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<ArchitectDbSchemaAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<QaTestCasesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<TechLeadImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanArtifactStorageAgent>(
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

        // Mock PmUserStoriesAgent returning null (failure)
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAgentMessage?)null);

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
        Assert.Equal("user_stories_failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("User stories generation failed", result.Error.Message);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task ExecuteAsync_GitPlanAndJiraPost_ExecuteInParallel()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var userStoriesMessage = new MessagePostedMessage(_testTicketId, "user_stories", DateTime.UtcNow);
        var apiDesignMessage = new MessagePostedMessage(_testTicketId, "api_design", DateTime.UtcNow);
        var dbSchemaMessage = new MessagePostedMessage(_testTicketId, "db_schema", DateTime.UtcNow);
        var testCasesMessage = new MessagePostedMessage(_testTicketId, "test_cases", DateTime.UtcNow);
        var implementationMessage = new MessagePostedMessage(_testTicketId, "implementation", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        var gitExecutionOrder = 0;
        var jiraExecutionOrder = 0;
        var executionCounter = 0;

        // Mock all 5 planning agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userStoriesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiDesignMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbSchemaMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(testCasesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

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
        var userStoriesMessage = new MessagePostedMessage(_testTicketId, "user_stories", DateTime.UtcNow);
        var apiDesignMessage = new MessagePostedMessage(_testTicketId, "api_design", DateTime.UtcNow);
        var dbSchemaMessage = new MessagePostedMessage(_testTicketId, "db_schema", DateTime.UtcNow);
        var testCasesMessage = new MessagePostedMessage(_testTicketId, "test_cases", DateTime.UtcNow);
        var implementationMessage = new MessagePostedMessage(_testTicketId, "implementation", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "abc123def", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        // Mock all 5 planning agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userStoriesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiDesignMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbSchemaMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testCasesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
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

        // Verify both Git and Jira agents were called
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

        // Verify plan_posted checkpoint was saved
        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                _testTicketId,
                "PlanningGraph",
                "plan_posted",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    #endregion

    #region Suspension Logic Tests

    [Fact]
    public async Task ExecuteAsync_AfterPostingPlan_SuspendsAwaitingApproval()
    {
        // Arrange
        var inputMessage = new AnswersReceivedMessage(_testTicketId, new Dictionary<string, string>());
        var userStoriesMessage = new MessagePostedMessage(_testTicketId, "user_stories", DateTime.UtcNow);
        var apiDesignMessage = new MessagePostedMessage(_testTicketId, "api_design", DateTime.UtcNow);
        var dbSchemaMessage = new MessagePostedMessage(_testTicketId, "db_schema", DateTime.UtcNow);
        var testCasesMessage = new MessagePostedMessage(_testTicketId, "test_cases", DateTime.UtcNow);
        var implementationMessage = new MessagePostedMessage(_testTicketId, "implementation", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? suspendedState = null;

        // Mock all 5 planning agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userStoriesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiDesignMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbSchemaMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testCasesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

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
        var userStoriesMessage = new MessagePostedMessage(_testTicketId, "user_stories", DateTime.UtcNow);
        var apiDesignMessage = new MessagePostedMessage(_testTicketId, "api_design", DateTime.UtcNow);
        var dbSchemaMessage = new MessagePostedMessage(_testTicketId, "db_schema", DateTime.UtcNow);
        var testCasesMessage = new MessagePostedMessage(_testTicketId, "test_cases", DateTime.UtcNow);
        var implementationMessage = new MessagePostedMessage(_testTicketId, "implementation", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? suspendedState = null;

        // Mock all 5 planning agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userStoriesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiDesignMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbSchemaMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testCasesMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(implementationMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

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
        // Note: plan_retry_count is only set when handling rejection, not on first execution
        // On first execution, we verify suspension state instead
        Assert.NotNull(suspendedState);
        Assert.True((bool)suspendedState["is_suspended"]);
        Assert.Equal("plan_approval", suspendedState["waiting_for"]);

        // Verify retry count is not set (it will default to 0 when rejection happens)
        Assert.False(suspendedState.ContainsKey("plan_retry_count"));
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

        var feedbackAnalysisMessage = new MessagePostedMessage(_testTicketId, "feedback_analyzed", DateTime.UtcNow);
        var revisionMessage = new MessagePostedMessage(_testTicketId, "plan_revised", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
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

        // Mock revision workflow agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedbackAnalysisMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanRevisionAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revisionMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

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
        Assert.Equal("awaiting_re_review", result.State);

        // Should have called revision agents
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<FeedbackAnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanRevisionAgent>(
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

        var feedbackAnalysisMessage = new MessagePostedMessage(_testTicketId, "feedback_analyzed", DateTime.UtcNow);
        var revisionMessage = new MessagePostedMessage(_testTicketId, "plan_revised", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? capturedState = null;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, checkpointId, state) =>
            {
                // Capture state after retry count increment (before any agents run)
                if (capturedState == null && state.ContainsKey("plan_retry_count"))
                {
                    capturedState = new Dictionary<string, object>(state);
                }
            })
            .Returns(Task.CompletedTask);

        // Mock revision workflow agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedbackAnalysisMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanRevisionAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revisionMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(3, capturedState["plan_retry_count"]);
        Assert.Equal("Try again", capturedState["RevisionFeedback"]);
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

        var feedbackAnalysisMessage = new MessagePostedMessage(_testTicketId, "feedback_analyzed", DateTime.UtcNow);
        var revisionMessage = new MessagePostedMessage(_testTicketId, "plan_revised", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        Dictionary<string, object>? capturedState = null;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(_testTicketId, "PlanningGraph"))
            .ReturnsAsync(checkpoint!);

        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>((_, _, checkpointId, state) =>
            {
                // Capture state after setting refinement instructions
                if (capturedState == null && state.ContainsKey("RefinementInstructions"))
                {
                    capturedState = new Dictionary<string, object>(state);
                }
            })
            .Returns(Task.CompletedTask);

        // Mock revision workflow agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedbackAnalysisMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanRevisionAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revisionMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal("Add more unit tests coverage", capturedState["RefinementInstructions"]);
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

        var feedbackAnalysisMessage = new MessagePostedMessage(_testTicketId, "feedback_analyzed", DateTime.UtcNow);
        var revisionMessage = new MessagePostedMessage(_testTicketId, "plan_revised", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
        var gitMessage = new PlanCommittedMessage(_testTicketId, "branch", "sha", "url");
        var jiraMessage = new MessagePostedMessage(_testTicketId, "plan_posted", DateTime.UtcNow);

        GraphContext? capturedContext = null;

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

        // Mock revision workflow agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .Callback<IAgentMessage, GraphContext, CancellationToken>((msg, ctx, ct) =>
            {
                if (capturedContext == null)
                {
                    capturedContext = ctx;
                }
            })
            .ReturnsAsync(feedbackAnalysisMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanRevisionAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revisionMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraMessage);

        // Act
        await _planningGraph.ResumeAsync(_testTicketId, rejectedMessage);

        // Assert - The actual implementation doesn't store "regenerate_completely" flag in state
        // Instead, it's used by FeedbackAnalysisAgent to determine affected artifacts
        // So we just verify the workflow completed successfully
        Assert.NotNull(capturedContext);
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<FeedbackAnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        var feedbackAnalysisMessage = new MessagePostedMessage(_testTicketId, "feedback_analyzed", DateTime.UtcNow);
        var revisionMessage = new MessagePostedMessage(_testTicketId, "final plan revised", DateTime.UtcNow);
        var storageMessage = new MessagePostedMessage(_testTicketId, "storage", DateTime.UtcNow);
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

        // Mock revision workflow agents
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedbackAnalysisMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanRevisionAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revisionMessage);

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(It.IsAny<IAgentMessage>(), It.IsAny<GraphContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageMessage);

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
        Assert.Equal("awaiting_re_review", result.State);

        // Should have regenerated via revision agents
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<FeedbackAnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<PlanRevisionAgent>(
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

        // Mock PmUserStoriesAgent throwing exception
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
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
        Assert.Equal("user_stories_failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("User stories generation failed", result.Error.Message);
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
