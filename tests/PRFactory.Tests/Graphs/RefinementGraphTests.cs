using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Tests.Fixtures;
using Xunit;

namespace PRFactory.Tests.Graphs;

/// <summary>
/// Comprehensive tests for RefinementGraph covering:
/// - Sequential agent execution
/// - Checkpointing after each agent
/// - Suspension logic (awaiting human answers and ticket update approval)
/// - Resume logic (with answers and approval/rejection)
/// - Retry logic (analysis failures and ticket update rejections)
/// - Completion events (RefinementCompleteEvent)
/// </summary>
public class RefinementGraphTests
{
    private readonly Mock<ILogger<RefinementGraph>> _mockLogger;
    private readonly Mock<ICheckpointStore> _mockCheckpointStore;
    private readonly Mock<IAgentExecutor> _mockAgentExecutor;
    private readonly TestDataFixture _testData;

    public RefinementGraphTests()
    {
        _mockLogger = new Mock<ILogger<RefinementGraph>>();
        _mockCheckpointStore = new Mock<ICheckpointStore>();
        _mockAgentExecutor = new Mock<IAgentExecutor>();
        _testData = new TestDataFixture();
    }

    #region Sequential Agent Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ExecutesAllAgentsSequentially()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        SetupSuccessfulAgentExecution(ticketId);

        // Act
        var result = await graph.ExecuteAsync(inputMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_answers", result.State);

        // Verify all agents executed in sequence
        VerifyAgentExecuted<TriggerAgent>();
        VerifyAgentExecuted<RepositoryCloneAgent>();
        VerifyAgentExecuted<AnalysisAgent>();
        VerifyAgentExecuted<QuestionGenerationAgent>();
        VerifyAgentExecuted<JiraPostAgent>();
    }

    [Fact]
    public async Task ExecuteAsync_SavesCheckpointAfterEachAgent()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        SetupSuccessfulAgentExecution(ticketId);

        // Act
        await graph.ExecuteAsync(inputMessage);

        // Assert - Verify checkpoints saved after each agent
        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "trigger_complete",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "clone_complete",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "analysis_complete",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "questions_generated",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "questions_posted",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);

        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "awaiting_answers",
                It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_StoresCorrectStateInContext()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        Dictionary<string, object> capturedState = null!;
        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "awaiting_answers",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>(
                (_, _, _, state) => capturedState = state)
            .Returns(Task.CompletedTask);

        SetupSuccessfulAgentExecution(ticketId);

        // Act
        await graph.ExecuteAsync(inputMessage);

        // Assert
        Assert.NotNull(capturedState);
        Assert.True((bool)capturedState["is_suspended"]);
        Assert.Equal("human_answers", capturedState["waiting_for"]);
    }

    #endregion

    #region Suspension Logic Tests

    [Fact]
    public async Task ExecuteAsync_SuspendsAfterPostingQuestions()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        SetupSuccessfulAgentExecution(ticketId);

        // Act
        var result = await graph.ExecuteAsync(inputMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_answers", result.State);
        Assert.NotNull(result.OutputMessage);
    }

    [Fact]
    public async Task ResumeAsync_SuspendsAfterTicketUpdateGeneration()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        SetupCheckpointForAwaitingAnswers(ticketId);

        var answersMessage = new AnswersReceivedMessage(
            ticketId,
            new Dictionary<string, string>
            {
                { "q1", "Answer 1" },
                { "q2", "Answer 2" }
            });

        SetupResumeAgentExecution(ticketId, ticketUpdateId);

        // Act
        var result = await graph.ResumeAsync(ticketId, answersMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_ticket_update_approval", result.State);
        Assert.NotNull(result.OutputMessage);
    }

    #endregion

    #region Resume Logic Tests

    [Fact]
    public async Task ResumeAsync_WithAnswers_ProcessesAnswersAndGeneratesTicketUpdate()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        SetupCheckpointForAwaitingAnswers(ticketId);

        var answersMessage = new AnswersReceivedMessage(
            ticketId,
            new Dictionary<string, string> { { "q1", "Answer 1" } });

        SetupResumeAgentExecution(ticketId, ticketUpdateId);

        // Act
        var result = await graph.ResumeAsync(ticketId, answersMessage);

        // Assert
        Assert.True(result.IsSuccess);
        VerifyAgentExecuted<AnswerProcessingAgent>();
        VerifyAgentExecuted<TicketUpdateGenerationAgent>();
    }

    [Fact]
    public async Task ResumeAsync_WithApproval_PostsTicketUpdateAndCompletes()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        SetupCheckpointForAwaitingTicketUpdateApproval(ticketId);

        var approvalMessage = new TicketUpdateApprovedMessage(
            ticketId,
            ticketUpdateId,
            DateTime.UtcNow,
            "test.user@example.com");

        SetupApprovalAgentExecution(ticketId);

        // Act
        var result = await graph.ResumeAsync(ticketId, approvalMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("refinement_complete", result.State);
        Assert.NotNull(result.OutputMessage);
        Assert.IsType<RefinementCompleteEvent>(result.OutputMessage);

        VerifyAgentExecuted<TicketUpdatePostAgent>();
    }

    [Fact]
    public async Task ResumeAsync_WithRejection_RegeneratesTicketUpdate()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        SetupCheckpointForAwaitingTicketUpdateApproval(ticketId);

        var rejectionMessage = new TicketUpdateRejectedMessage(
            ticketId,
            ticketUpdateId,
            "Needs more details");

        SetupRejectionAgentExecution(ticketId, ticketUpdateId);

        // Act
        var result = await graph.ResumeAsync(ticketId, rejectionMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_ticket_update_approval", result.State);

        // Verify ticket update regenerated
        VerifyAgentExecuted<TicketUpdateGenerationAgent>();
    }

    [Fact]
    public async Task ResumeAsync_WithInvalidState_ThrowsException()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;

        var checkpoint = new Checkpoint
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            GraphId = "RefinementGraph",
            CheckpointId = "invalid_state",
            State = new Dictionary<string, object>
            {
                { "current_state", "invalid_state" },
                { "current_agent", "unknown" }
            },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(ticketId, "RefinementGraph"))
            .ReturnsAsync((Checkpoint?)checkpoint);

        var answersMessage = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());

        // Act & Assert
        var result = await graph.ResumeAsync(ticketId, answersMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal("resume_failed", result.State);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task ExecuteAsync_AnalysisFailure_RetriesUpToThreeTimes()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        SetupTriggerAndCloneAgents(ticketId);

        // Setup Analysis agent to fail 2 times, then succeed
        var analysisCallCount = 0;
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<AnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<IAgentMessage, GraphContext, CancellationToken>((msg, ctx, ct) =>
            {
                analysisCallCount++;
                if (analysisCallCount < 3)
                {
                    throw new InvalidOperationException($"Analysis failed attempt {analysisCallCount}");
                }
                return Task.FromResult<IAgentMessage>(
                    new CodebaseAnalyzedMessage(
                        ticketId,
                        new List<string> { "file1.cs" },
                        "Clean Architecture",
                        new List<string> { "Pattern1" },
                        new Dictionary<string, string>()));
            });

        SetupQuestionAndJiraAgents(ticketId);

        // Act
        var result = await graph.ExecuteAsync(inputMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, analysisCallCount); // Retried 2 times + 1 success

        // Verify retry checkpoint saved
        _mockCheckpointStore.Verify(
            x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                It.Is<string>(s => s.StartsWith("analysis_retry_")),
                It.IsAny<Dictionary<string, object>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_AnalysisFailsThreeTimes_ThrowsException()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        SetupTriggerAndCloneAgents(ticketId);

        // Setup Analysis agent to always fail
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<AnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Analysis failed"));

        // Act
        var result = await graph.ExecuteAsync(inputMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResumeAsync_TicketUpdateRejectedThreeTimes_FailsWorkflow()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        // Setup checkpoint with retry count at max
        var checkpoint = new Checkpoint
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            GraphId = "RefinementGraph",
            CheckpointId = "awaiting_ticket_update_approval",
            State = new Dictionary<string, object>
            {
                { "current_state", "awaiting_ticket_update_approval" },
                { "current_agent", "HumanWaitAgent" },
                { "ticket_update_retry_count", 3 } // Already at max retries
            },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(ticketId, "RefinementGraph"))
            .ReturnsAsync((Checkpoint?)checkpoint);

        var rejectionMessage = new TicketUpdateRejectedMessage(
            ticketId,
            ticketUpdateId,
            "Still needs more details");

        // Act
        var result = await graph.ResumeAsync(ticketId, rejectionMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ticket_update_failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Contains("rejected 3 times", result.Error.Message);
    }

    [Fact]
    public async Task ResumeAsync_TicketUpdateRejectedTwice_RetriesSuccessfully()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        // Setup checkpoint with retry count at 2
        var checkpoint = new Checkpoint
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            GraphId = "RefinementGraph",
            CheckpointId = "awaiting_ticket_update_approval",
            State = new Dictionary<string, object>
            {
                { "current_state", "awaiting_ticket_update_approval" },
                { "current_agent", "HumanWaitAgent" },
                { "ticket_update_retry_count", 2 }
            },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(ticketId, "RefinementGraph"))
            .ReturnsAsync((Checkpoint?)checkpoint);

        var rejectionMessage = new TicketUpdateRejectedMessage(
            ticketId,
            ticketUpdateId,
            "Needs improvement");

        SetupRejectionAgentExecution(ticketId, ticketUpdateId);

        // Act
        var result = await graph.ResumeAsync(ticketId, rejectionMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("awaiting_ticket_update_approval", result.State);

        // Verify regeneration occurred
        VerifyAgentExecuted<TicketUpdateGenerationAgent>();
    }

    #endregion

    #region Completion Event Tests

    [Fact]
    public async Task ResumeAsync_WithApproval_PublishesRefinementCompleteEvent()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        SetupCheckpointForAwaitingTicketUpdateApproval(ticketId);

        var approvalMessage = new TicketUpdateApprovedMessage(
            ticketId,
            ticketUpdateId,
            DateTime.UtcNow,
            "test.user@example.com");

        SetupApprovalAgentExecution(ticketId);

        // Act
        var result = await graph.ResumeAsync(ticketId, approvalMessage);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.OutputMessage);

        var completionEvent = Assert.IsType<RefinementCompleteEvent>(result.OutputMessage);
        Assert.Equal(ticketId, completionEvent.TicketId);
    }

    [Fact]
    public async Task ResumeAsync_WithApproval_SetsCompletionStateInContext()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var ticketUpdateId = Guid.NewGuid();

        Dictionary<string, object> capturedState = null!;
        _mockCheckpointStore
            .Setup(x => x.SaveCheckpointAsync(
                ticketId,
                "RefinementGraph",
                "refinement_complete",
                It.IsAny<Dictionary<string, object>>()))
            .Callback<Guid, string, string, Dictionary<string, object>>(
                (_, _, _, state) => capturedState = state)
            .Returns(Task.CompletedTask);

        SetupCheckpointForAwaitingTicketUpdateApproval(ticketId);

        var approvalMessage = new TicketUpdateApprovedMessage(
            ticketId,
            ticketUpdateId,
            DateTime.UtcNow,
            "test.user@example.com");

        SetupApprovalAgentExecution(ticketId);

        // Act
        await graph.ResumeAsync(ticketId, approvalMessage);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(true, capturedState["is_completed"]);
        Assert.Equal(false, capturedState["is_suspended"]);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithAgentException_ReturnsFailureResult()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;
        var inputMessage = new TriggerTicketMessage(
            "TEST-123",
            _testData.DefaultTenantId,
            _testData.DefaultRepositoryId,
            "Jira"
        )
        { TicketId = ticketId };

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TriggerAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Agent execution failed"));

        // Act
        var result = await graph.ExecuteAsync(inputMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.State);
        Assert.NotNull(result.Error);
        Assert.Equal("Agent execution failed", result.Error.Message);
    }

    [Fact]
    public async Task ResumeAsync_WithNoCheckpoint_ReturnsFailureResult()
    {
        // Arrange
        var graph = CreateGraph();
        var ticketId = _testData.DefaultTicketId;

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(ticketId, "RefinementGraph"))
            .ReturnsAsync((Checkpoint?)null);

        var answersMessage = new AnswersReceivedMessage(ticketId, new Dictionary<string, string>());

        // Act
        var result = await graph.ResumeAsync(ticketId, answersMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("resume_failed", result.State);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region Helper Methods

    private RefinementGraph CreateGraph()
    {
        return new RefinementGraph(
            _mockLogger.Object,
            _mockCheckpointStore.Object,
            _mockAgentExecutor.Object);
    }

    private void SetupSuccessfulAgentExecution(Guid ticketId)
    {
        SetupTriggerAndCloneAgents(ticketId);
        SetupAnalysisAgent(ticketId);
        SetupQuestionAndJiraAgents(ticketId);
    }

    private void SetupTriggerAndCloneAgents(Guid ticketId)
    {
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TriggerAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketTriggeredMessage(
                ticketId,
                "TEST-123",
                "Test Ticket",
                "Description",
                _testData.DefaultRepositoryId));

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<RepositoryCloneAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryClonedMessage(
                ticketId,
                "/tmp/repo",
                "main"));
    }

    private void SetupAnalysisAgent(Guid ticketId)
    {
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<AnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodebaseAnalyzedMessage(
                ticketId,
                new List<string> { "file1.cs" },
                "Clean Architecture",
                new List<string> { "Pattern1" },
                new Dictionary<string, string>()));
    }

    private void SetupQuestionAndJiraAgents(Guid ticketId)
    {
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<QuestionGenerationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuestionsGeneratedMessage(
                ticketId,
                new List<Question>
                {
                    new("q1", "Question 1?", "Functional", true)
                }));

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessagePostedMessage(
                ticketId,
                "questions",
                DateTime.UtcNow));
    }

    private void SetupResumeAgentExecution(Guid ticketId, Guid ticketUpdateId)
    {
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<AnswerProcessingAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodebaseAnalyzedMessage(
                ticketId,
                new List<string> { "file1.cs" },
                "Clean Architecture",
                new List<string> { "Pattern1" },
                new Dictionary<string, string>()));

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TicketUpdateGenerationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketUpdateGeneratedMessage(
                ticketId,
                ticketUpdateId,
                1,
                "Updated Title"));
    }

    private void SetupApprovalAgentExecution(Guid ticketId)
    {
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TicketUpdatePostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketUpdatePostedMessage(
                ticketId,
                Guid.NewGuid(),
                DateTime.UtcNow));
    }

    private void SetupRejectionAgentExecution(Guid ticketId, Guid ticketUpdateId)
    {
        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync<TicketUpdateGenerationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketUpdateGeneratedMessage(
                ticketId,
                ticketUpdateId,
                2,
                "Regenerated Title"));
    }

    private void SetupCheckpointForAwaitingAnswers(Guid ticketId)
    {
        var checkpoint = new Checkpoint
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            GraphId = "RefinementGraph",
            CheckpointId = "awaiting_answers",
            State = new Dictionary<string, object>
            {
                { "current_state", "awaiting_answers" },
                { "current_agent", "HumanWaitAgent" },
                { "is_suspended", true },
                { "waiting_for", "human_answers" }
            },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(ticketId, "RefinementGraph"))
            .ReturnsAsync((Checkpoint?)checkpoint);
    }

    private void SetupCheckpointForAwaitingTicketUpdateApproval(Guid ticketId)
    {
        var checkpoint = new Checkpoint
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            GraphId = "RefinementGraph",
            CheckpointId = "awaiting_ticket_update_approval",
            State = new Dictionary<string, object>
            {
                { "current_state", "awaiting_ticket_update_approval" },
                { "current_agent", "HumanWaitAgent" },
                { "is_suspended", true },
                { "waiting_for", "ticket_update_approval" },
                { "ticket_update_retry_count", 0 }
            },
            CreatedAt = DateTime.UtcNow
        };

        _mockCheckpointStore
            .Setup(x => x.LoadCheckpointAsync(ticketId, "RefinementGraph"))
            .ReturnsAsync((Checkpoint?)checkpoint);
    }

    private void VerifyAgentExecuted<TAgent>()
    {
        _mockAgentExecutor.Verify(
            x => x.ExecuteAsync<TAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            $"{typeof(TAgent).Name} should have been executed");
    }

    #endregion
}
