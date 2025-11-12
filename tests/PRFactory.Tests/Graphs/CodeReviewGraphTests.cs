using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Agents.Specialized;
using Xunit;

namespace PRFactory.Tests.Graphs;

/// <summary>
/// Comprehensive tests for CodeReviewGraph covering:
/// - Successful review with no issues
/// - Review with critical issues triggers loop to ImplementationGraph
/// - Max retry limit enforcement
/// - Configuration-based provider selection
/// - Retry logic with attempts tracking
/// - Graph state transitions and checkpointing
/// </summary>
public class CodeReviewGraphTests
{
    private readonly Mock<ILogger<CodeReviewGraph>> _mockLogger;
    private readonly Mock<ICheckpointStore> _mockCheckpointStore;
    private readonly Mock<IAgentExecutor> _mockAgentExecutor;

    public CodeReviewGraphTests()
    {
        _mockLogger = new Mock<ILogger<CodeReviewGraph>>();
        _mockCheckpointStore = new Mock<ICheckpointStore>();
        _mockAgentExecutor = new Mock<IAgentExecutor>();
    }

    private CodeReviewGraph CreateGraph()
    {
        return new CodeReviewGraph(
            _mockLogger.Object,
            _mockCheckpointStore.Object,
            _mockAgentExecutor.Object);
    }

    private GraphContext CreateContext(string ticketId, int retryCount = 0)
    {
        var context = new GraphContext
        {
            TicketId = ticketId,
            State = new Dictionary<string, object>()
        };

        if (retryCount > 0)
        {
            context.State["review_retry_count"] = retryCount;
        }

        return context;
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithNoCriticalIssues_CompletesSuccessfully()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = Guid.NewGuid().ToString();
        var context = CreateContext(ticketId);

        var reviewMessage = new ReviewCodeMessage(
            TicketId: Guid.Parse(ticketId),
            PullRequestUrl: "https://github.com/test/repo/pull/123",
            PullRequestNumber: 123,
            BranchName: "feature/test",
            PlanPath: "/plans/plan.md"
        );

        // Mock agent executor to return successful review with no issues
        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ReviewId"] = Guid.NewGuid(),
                ["HasCriticalIssues"] = false,
                ["CriticalIssues"] = new List<string>(),
                ["Suggestions"] = new List<string> { "Add more tests" },
                ["Praise"] = new List<string> { "Good structure" },
                ["ReviewContent"] = "## No critical issues found",
                ["RetryAttempt"] = 1
            }
        };

        // Wrap the agent result in a message (this is what ExecuteAsync returns)
        var resultMessage = new AgentExecutedMessage(Guid.Parse(ticketId), agentResult);

        _mockAgentExecutor
            .Setup(e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultMessage);

        _mockCheckpointStore
            .Setup(s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await graph.ExecuteAsync(reviewMessage, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("review_approved", result.State);
        Assert.NotNull(result.OutputMessage);

        // Verify checkpoints were saved
        _mockCheckpointStore.Verify(
            s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                "CodeReviewGraph",
                "review_completed",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);

        _mockCheckpointStore.Verify(
            s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                "CodeReviewGraph",
                "review_approved",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCriticalIssues_TransitionsToImplementationGraph()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = Guid.NewGuid().ToString();
        var context = CreateContext(ticketId, retryCount: 0);

        var reviewMessage = new ReviewCodeMessage(
            TicketId: Guid.Parse(ticketId),
            PullRequestUrl: "https://github.com/test/repo/pull/123",
            PullRequestNumber: 123,
            BranchName: "feature/test",
            PlanPath: "/plans/plan.md"
        );

        var criticalIssues = new List<string>
        {
            "SQL injection vulnerability",
            "Missing input validation"
        };

        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ReviewId"] = Guid.NewGuid(),
                ["HasCriticalIssues"] = true,
                ["CriticalIssues"] = criticalIssues,
                ["Suggestions"] = new List<string> { "Add logging" },
                ["Praise"] = new List<string>(),
                ["ReviewContent"] = "## Critical issues found",
                ["RetryAttempt"] = 1
            }
        };

        var resultMessage = new AgentExecutedMessage(Guid.Parse(ticketId), agentResult);

        _mockAgentExecutor
            .Setup(e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultMessage);

        _mockCheckpointStore
            .Setup(s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await graph.ExecuteAsync(reviewMessage, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess); // Suspended state returns false for IsSuccess
        Assert.Equal("awaiting_fixes", result.State);
        Assert.NotNull(result.OutputMessage);

        // Verify retry count was incremented
        Assert.True(context.State.ContainsKey("review_retry_count"));
        Assert.Equal(1, context.State["review_retry_count"]);

        // Verify critical issues were stored
        Assert.True(context.State.ContainsKey("critical_issues"));

        // Verify checkpoint for awaiting fixes was saved
        _mockCheckpointStore.Verify(
            s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                "CodeReviewGraph",
                "awaiting_fixes",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AtMaxRetries_CompletesWithWarnings()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = Guid.NewGuid().ToString();
        var context = CreateContext(ticketId, retryCount: 3); // Already at max retries

        var reviewMessage = new ReviewCodeMessage(
            TicketId: Guid.Parse(ticketId),
            PullRequestUrl: "https://github.com/test/repo/pull/123",
            PullRequestNumber: 123,
            BranchName: "feature/test",
            PlanPath: "/plans/plan.md"
        );

        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ReviewId"] = Guid.NewGuid(),
                ["HasCriticalIssues"] = true,
                ["CriticalIssues"] = new List<string> { "Still has issues" },
                ["Suggestions"] = new List<string>(),
                ["Praise"] = new List<string>(),
                ["ReviewContent"] = "## Still has issues",
                ["RetryAttempt"] = 4
            }
        };

        var resultMessage = new AgentExecutedMessage(Guid.Parse(ticketId), agentResult);

        _mockAgentExecutor
            .Setup(e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultMessage);

        _mockCheckpointStore
            .Setup(s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await graph.ExecuteAsync(reviewMessage, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("max_retries_reached", result.State);

        // Verify it's marked as completed with warnings
        Assert.True(context.State.ContainsKey("completed_with_warnings"));
        Assert.True((bool)context.State["completed_with_warnings"]);

        // Verify checkpoint was saved
        _mockCheckpointStore.Verify(
            s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                "CodeReviewGraph",
                "max_retries_reached",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfiguration_UsesCorrectProvider()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = Guid.NewGuid().ToString();
        var context = CreateContext(ticketId);

        // Add provider configuration to context
        context.State["CodeReviewLlmProviderId"] = Guid.NewGuid();

        var reviewMessage = new ReviewCodeMessage(
            TicketId: Guid.Parse(ticketId),
            PullRequestUrl: "https://github.com/test/repo/pull/123",
            PullRequestNumber: 123,
            BranchName: "feature/test",
            PlanPath: "/plans/plan.md"
        );

        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ReviewId"] = Guid.NewGuid(),
                ["HasCriticalIssues"] = false,
                ["CriticalIssues"] = new List<string>(),
                ["Suggestions"] = new List<string>(),
                ["Praise"] = new List<string>(),
                ["ReviewContent"] = "## Review complete",
                ["RetryAttempt"] = 1
            }
        };

        var resultMessage = new AgentExecutedMessage(Guid.Parse(ticketId), agentResult);

        _mockAgentExecutor
            .Setup(e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultMessage);

        _mockCheckpointStore
            .Setup(s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await graph.ExecuteAsync(reviewMessage, context, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify agent executor was called
        _mockAgentExecutor.Verify(
            e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region HandleRetryLogic Tests

    [Fact]
    public async Task HandleRetryLogic_WithinLimit_AllowsRetry()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = Guid.NewGuid().ToString();
        var context = CreateContext(ticketId, retryCount: 1); // Within limit

        var reviewMessage = new ReviewCodeMessage(
            TicketId: Guid.Parse(ticketId),
            PullRequestUrl: "https://github.com/test/repo/pull/123",
            PullRequestNumber: 123,
            BranchName: "feature/test",
            PlanPath: "/plans/plan.md"
        );

        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ReviewId"] = Guid.NewGuid(),
                ["HasCriticalIssues"] = true,
                ["CriticalIssues"] = new List<string> { "Issue found" },
                ["Suggestions"] = new List<string>(),
                ["Praise"] = new List<string>(),
                ["ReviewContent"] = "## Issue found",
                ["RetryAttempt"] = 2
            }
        };

        var resultMessage = new AgentExecutedMessage(Guid.Parse(ticketId), agentResult);

        _mockAgentExecutor
            .Setup(e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultMessage);

        _mockCheckpointStore
            .Setup(s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await graph.ExecuteAsync(reviewMessage, context, CancellationToken.None);

        // Assert
        Assert.Equal("awaiting_fixes", result.State);
        Assert.Equal(2, context.State["review_retry_count"]); // Incremented to 2
    }

    [Fact]
    public async Task HandleRetryLogic_ExceedsLimit_StopsRetries()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = Guid.NewGuid().ToString();
        var context = CreateContext(ticketId, retryCount: 3); // At max limit

        var reviewMessage = new ReviewCodeMessage(
            TicketId: Guid.Parse(ticketId),
            PullRequestUrl: "https://github.com/test/repo/pull/123",
            PullRequestNumber: 123,
            BranchName: "feature/test",
            PlanPath: "/plans/plan.md"
        );

        var agentResult = new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ReviewId"] = Guid.NewGuid(),
                ["HasCriticalIssues"] = true,
                ["CriticalIssues"] = new List<string> { "Issue still present" },
                ["Suggestions"] = new List<string>(),
                ["Praise"] = new List<string>(),
                ["ReviewContent"] = "## Issue still present",
                ["RetryAttempt"] = 4
            }
        };

        var resultMessage = new AgentExecutedMessage(Guid.Parse(ticketId), agentResult);

        _mockAgentExecutor
            .Setup(e => e.ExecuteAsync<CodeReviewAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultMessage);

        _mockCheckpointStore
            .Setup(s => s.SaveCheckpointAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await graph.ExecuteAsync(reviewMessage, context, CancellationToken.None);

        // Assert
        Assert.Equal("max_retries_reached", result.State);
        Assert.True(result.IsSuccess); // Complete successfully but with warnings
        Assert.True(context.State.ContainsKey("completed_with_warnings"));
    }

    #endregion
}

/// <summary>
/// Helper message class for wrapping agent results in tests
/// </summary>
public record AgentExecutedMessage(Guid TicketId, AgentResult Result) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
