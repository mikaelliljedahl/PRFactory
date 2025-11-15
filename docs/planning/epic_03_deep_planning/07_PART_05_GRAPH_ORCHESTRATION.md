# Part 5: Planning Graph Updates - Implementation Plan

**Epic**: Deep Planning (Epic 03)
**Component**: Enhanced PlanningGraph with Multi-Agent Orchestration
**Estimated Effort**: 2-3 days
**Dependencies**: Part 1 (Agents), Part 2 (Database), Part 3 (Storage), Part 4 (Revision Agents)
**Status**: 🚧 Not Implemented

---

## Overview

This part updates the `PlanningGraph` to orchestrate all planning agents in the correct sequence, including:

1. Sequential execution of foundational agents (PM User Stories)
2. Parallel execution of independent agents (API Design + DB Schema)
3. Sequential dependent agents (QA Test Cases, Tech Lead Implementation)
4. Storage and Git integration
5. Revision workflow resumption with feedback analysis and targeted regeneration

---

## Architecture

### Planning Graph Execution Flow

```
START (Initial Workflow)
  ↓
[1. PmUserStoriesAgent] → Generates user stories as foundation
  ↓
[2. Parallel Execution]
  ├─ ArchitectApiDesignAgent → API design (uses user stories)
  └─ ArchitectDbSchemaAgent → DB schema (uses user stories)
  ↓
[3. QaTestCasesAgent] → Test cases (uses API + schema)
  ↓
[4. TechLeadImplementationAgent] → Implementation steps (uses all artifacts)
  ↓
[5. PlanArtifactStorageAgent] → Store in database
  ↓
[6. Parallel Execution]
  ├─ GitPlanAgent → Commit to feature branch
  └─ JiraPostAgent → Post summary to Jira
  ↓
[7. HumanWaitAgent] → Suspend for review
  ↓
---

RESUME (Revision Workflow)
  ↓
[User provides feedback]
  ↓
[8. FeedbackAnalysisAgent] → Determine affected artifacts
  ↓
[9. PlanRevisionAgent] → Regenerate only affected artifacts
  ↓
[5. PlanArtifactStorageAgent] → Store revised artifacts (new version)
  ↓
[6. Parallel Execution]
  ├─ GitPlanAgent → Commit revisions
  └─ JiraPostAgent → Post revision summary
  ↓
[7. HumanWaitAgent] → Suspend for re-review
```

---

## Implementation

### File: Update PlanningGraph

**Path**: `src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs`

#### Part 1: Enhanced ExecuteCoreAsync

```csharp
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Agents.Planning;
using PRFactory.Domain.Entities;
using PRFactory.Core.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Infrastructure.Agents.Graphs;

/// <summary>
/// Enhanced planning graph orchestrating multi-artifact generation with revision support.
/// Executes 5+ specialized agents in optimal sequence (parallel where possible).
/// Supports resumption for revision workflows.
/// </summary>
public class PlanningGraph : AgentGraphBase
{
    private readonly IAgentExecutor _executor;
    private readonly IWorkflowCheckpointService _checkpointService;
    private readonly ILogger<PlanningGraph> _logger;

    public override string Name => "Planning Graph";
    public override string Description => "Multi-agent orchestration for comprehensive planning";

    public PlanningGraph(
        IAgentExecutor executor,
        IWorkflowCheckpointService checkpointService,
        ILogger<PlanningGraph> logger)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(checkpointService);
        ArgumentNullException.ThrowIfNull(logger);

        _executor = executor;
        _checkpointService = checkpointService;
        _logger = logger;
    }

    protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
        IAgentMessage inputMessage,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = inputMessage.TicketId;

        try
        {
            _logger.LogInformation(
                "Starting enhanced planning workflow for ticket {TicketId}",
                ticketId);

            // STEP 1: PM User Stories (Foundation - Sequential)
            _logger.LogInformation("Step 1: Generating user stories");

            var userStoriesResult = await _executor.ExecuteAsync<PmUserStoriesAgent>(
                inputMessage,
                context,
                cancellationToken);

            if (userStoriesResult.Status != AgentStatus.Completed)
            {
                return GraphExecutionResult.Failed(
                    $"User stories generation failed: {userStoriesResult.Error}");
            }

            // Store user stories in context for next agents
            context.AgentContext.State["UserStories"] =
                userStoriesResult.Output.GetValueOrDefault("UserStories", "");

            // STEP 2: Parallel Execution (API Design + DB Schema)
            _logger.LogInformation("Step 2: Generating API design and database schema (parallel)");

            var apiDesignTask = ExecuteAgentAsync<ArchitectApiDesignAgent>(
                userStoriesResult,
                context,
                cancellationToken);

            var dbSchemaTask = ExecuteAgentAsync<ArchitectDbSchemaAgent>(
                userStoriesResult,
                context,
                cancellationToken);

            var parallelResults = await Task.WhenAll(apiDesignTask, dbSchemaTask);
            var apiDesignResult = parallelResults[0];
            var dbSchemaResult = parallelResults[1];

            if (apiDesignResult.Status != AgentStatus.Completed)
            {
                _logger.LogWarning("API design generation failed: {Error}", apiDesignResult.Error);
            }
            else
            {
                context.AgentContext.State["ApiDesign"] =
                    apiDesignResult.Output.GetValueOrDefault("ApiDesign", "");
            }

            if (dbSchemaResult.Status != AgentStatus.Completed)
            {
                _logger.LogWarning("Database schema generation failed: {Error}", dbSchemaResult.Error);
            }
            else
            {
                context.AgentContext.State["DatabaseSchema"] =
                    dbSchemaResult.Output.GetValueOrDefault("DatabaseSchema", "");
            }

            // STEP 3: QA Test Cases (Sequential - depends on API + Schema)
            _logger.LogInformation("Step 3: Generating test cases");

            var testCasesResult = await _executor.ExecuteAsync<QaTestCasesAgent>(
                new AgentMessage(ticketId),
                context,
                cancellationToken);

            if (testCasesResult.Status != AgentStatus.Completed)
            {
                _logger.LogWarning("Test cases generation failed: {Error}", testCasesResult.Error);
            }
            else
            {
                context.AgentContext.State["TestCases"] =
                    testCasesResult.Output.GetValueOrDefault("TestCases", "");
            }

            // STEP 4: Tech Lead Implementation Steps (Sequential)
            _logger.LogInformation("Step 4: Generating implementation steps");

            var implementationResult = await _executor.ExecuteAsync<TechLeadImplementationAgent>(
                new AgentMessage(ticketId),
                context,
                cancellationToken);

            if (implementationResult.Status != AgentStatus.Completed)
            {
                _logger.LogWarning("Implementation steps generation failed: {Error}", implementationResult.Error);
            }
            else
            {
                context.AgentContext.State["ImplementationSteps"] =
                    implementationResult.Output.GetValueOrDefault("ImplementationSteps", "");
            }

            // STEP 5: Store all artifacts in database
            _logger.LogInformation("Step 5: Storing artifacts in database");

            var storageResult = await _executor.ExecuteAsync<PlanArtifactStorageAgent>(
                new AgentMessage(ticketId),
                context,
                cancellationToken);

            if (storageResult.Status != AgentStatus.Completed)
            {
                return GraphExecutionResult.Failed(
                    $"Artifact storage failed: {storageResult.Error}");
            }

            // Store plan ID for later retrieval
            context.AgentContext.State["PlanId"] =
                storageResult.Output.GetValueOrDefault("PlanId", Guid.Empty);

            // STEP 6: Commit to Git and Post to Jira (Parallel)
            _logger.LogInformation("Step 6: Committing artifacts and posting to Jira (parallel)");

            var gitTask = ExecuteAgentAsync<GitPlanAgent>(
                storageResult,
                context,
                cancellationToken);

            var jiraTask = ExecuteAgentAsync<JiraPostAgent>(
                storageResult,
                context,
                cancellationToken);

            var finalResults = await Task.WhenAll(gitTask, jiraTask);

            var gitResult = finalResults[0];
            var jiraResult = finalResults[1];

            if (gitResult.Status != AgentStatus.Completed)
            {
                _logger.LogWarning("Git commit failed: {Error}", gitResult.Error);
            }

            if (jiraResult.Status != AgentStatus.Completed)
            {
                _logger.LogWarning("Jira post failed: {Error}", jiraResult.Error);
            }

            // STEP 7: Create checkpoint and suspend for review
            _logger.LogInformation("Step 7: Suspending for human review");

            var checkpointData = new Dictionary<string, object>
            {
                ["UserStories"] = context.AgentContext.State.GetValueOrDefault("UserStories", ""),
                ["ApiDesign"] = context.AgentContext.State.GetValueOrDefault("ApiDesign", ""),
                ["DatabaseSchema"] = context.AgentContext.State.GetValueOrDefault("DatabaseSchema", ""),
                ["TestCases"] = context.AgentContext.State.GetValueOrDefault("TestCases", ""),
                ["ImplementationSteps"] = context.AgentContext.State.GetValueOrDefault("ImplementationSteps", ""),
                ["PlanId"] = context.AgentContext.State.GetValueOrDefault("PlanId", Guid.Empty),
                ["FeatureBranch"] = gitResult.Output.GetValueOrDefault("FeatureBranch", ""),
                ["JiraCommentUrl"] = jiraResult.Output.GetValueOrDefault("CommentUrl", "")
            };

            await _checkpointService.CreateCheckpointAsync(
                ticketId,
                "PlanningGraph",
                "AwaitingApproval",
                checkpointData,
                cancellationToken);

            return new GraphExecutionResult
            {
                Status = GraphExecutionStatus.Suspended,
                SuspensionReason = "Plan generation complete. Awaiting human review and approval.",
                SuspendedAt = DateTime.UtcNow,
                NextExpectedMessage = "PlanApprovedMessage or PlanRejectedMessage",
                CheckpointData = checkpointData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced planning workflow failed for ticket {TicketId}", ticketId);

            return GraphExecutionResult.Failed($"Planning workflow error: {ex.Message}");
        }
    }

    // ... ResumeCoreAsync method in next section
}
```

#### Part 2: ResumeCoreAsync for Revision Workflow

```csharp
    protected override async Task<GraphExecutionResult> ResumeCoreAsync(
        IAgentMessage resumeMessage,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resuming planning workflow for ticket {TicketId} with message {MessageType}",
            resumeMessage.TicketId,
            resumeMessage.GetType().Name);

        try
        {
            return resumeMessage switch
            {
                PlanApprovedMessage approved => await HandlePlanApprovedAsync(approved, context, cancellationToken),
                PlanRejectedMessage rejected => await HandlePlanRejectedAsync(rejected, context, cancellationToken),
                _ => GraphExecutionResult.Failed($"Unexpected resume message type: {resumeMessage.GetType().Name}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming planning workflow for ticket {TicketId}", resumeMessage.TicketId);
            return GraphExecutionResult.Failed($"Resume error: {ex.Message}");
        }
    }

    private async Task<GraphExecutionResult> HandlePlanApprovedAsync(
        PlanApprovedMessage approved,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Plan approved for ticket {TicketId} by {ApprovedBy}",
            approved.TicketId,
            approved.ApprovedBy ?? "system");

        // Update ticket state
        context.AgentContext.State["PlanApprovedAt"] = DateTime.UtcNow;
        context.AgentContext.State["PlanApprovedBy"] = approved.ApprovedBy ?? "system";

        // Emit event to trigger ImplementationGraph
        return new GraphExecutionResult
        {
            Status = GraphExecutionStatus.Completed,
            CompletionReason = "Plan approved. Ready for implementation phase.",
            Output = new Dictionary<string, object>
            {
                ["ApprovedBy"] = approved.ApprovedBy ?? "system",
                ["ApprovedAt"] = approved.Timestamp
            }
        };
    }

    private async Task<GraphExecutionResult> HandlePlanRejectedAsync(
        PlanRejectedMessage rejected,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = rejected.TicketId;

        _logger.LogInformation(
            "Plan rejected for ticket {TicketId}. Feedback: {Feedback}",
            ticketId,
            rejected.Feedback[..Math.Min(100, rejected.Feedback.Length)]);

        // STEP 1: Analyze feedback to determine affected artifacts
        _logger.LogInformation("Analyzing feedback to determine affected artifacts");

        context.AgentContext.State["RejectionMessage"] = rejected;
        context.AgentContext.State["RevisionFeedback"] = rejected.Feedback;

        var analysisResult = await _executor.ExecuteAsync<FeedbackAnalysisAgent>(
            new AgentMessage(ticketId),
            context,
            cancellationToken);

        if (analysisResult.Status != AgentStatus.Completed)
        {
            _logger.LogWarning("Feedback analysis failed: {Error}", analysisResult.Error);
            // Fallback to regenerating all artifacts
            context.AgentContext.State["AffectedArtifacts"] = GetAllArtifactTypes();
        }
        else
        {
            var affectedArtifacts = analysisResult.Output.GetValueOrDefault("AffectedArtifacts") as List<string>
                ?? GetAllArtifactTypes();

            context.AgentContext.State["AffectedArtifacts"] = affectedArtifacts;

            _logger.LogInformation(
                "Affected artifacts identified: {Artifacts}",
                string.Join(", ", affectedArtifacts));
        }

        // STEP 2: Regenerate only affected artifacts
        _logger.LogInformation("Regenerating affected artifacts");

        var revisionResult = await _executor.ExecuteAsync<PlanRevisionAgent>(
            new AgentMessage(ticketId),
            context,
            cancellationToken);

        if (revisionResult.Status != AgentStatus.Completed)
        {
            _logger.LogWarning("Artifact regeneration failed: {Error}", revisionResult.Error);
            // Continue with incomplete artifacts rather than failing
        }

        // STEP 3: Update artifact context with revised versions
        foreach (var (key, value) in revisionResult.Output)
        {
            context.AgentContext.State[key] = value;
        }

        // STEP 4: Store revised artifacts (creates new version)
        _logger.LogInformation("Storing revised artifacts");

        context.AgentContext.State["IsRevision"] = true;
        context.AgentContext.State["RevisionReason"] = rejected.Feedback;
        context.AgentContext.State["RevisionVersion"] =
            (int?)context.AgentContext.State.GetValueOrDefault("RevisionVersion", 1) + 1;

        var storageResult = await _executor.ExecuteAsync<PlanArtifactStorageAgent>(
            new AgentMessage(ticketId),
            context,
            cancellationToken);

        if (storageResult.Status != AgentStatus.Completed)
        {
            _logger.LogWarning("Storage of revised artifacts failed: {Error}", storageResult.Error);
        }

        // STEP 5: Commit revisions and post update to Jira (Parallel)
        _logger.LogInformation("Committing revisions and posting updates (parallel)");

        var gitTask = ExecuteAgentAsync<GitPlanAgent>(
            storageResult,
            context,
            cancellationToken);

        var jiraTask = ExecuteAgentAsync<JiraPostAgent>(
            storageResult,
            context,
            cancellationToken);

        var finalResults = await Task.WhenAll(gitTask, jiraTask);

        var gitResult = finalResults[0];
        var jiraResult = finalResults[1];

        if (gitResult.Status != AgentStatus.Completed)
        {
            _logger.LogWarning("Git revision commit failed: {Error}", gitResult.Error);
        }

        if (jiraResult.Status != AgentStatus.Completed)
        {
            _logger.LogWarning("Jira revision update failed: {Error}", jiraResult.Error);
        }

        // STEP 6: Create checkpoint and suspend for re-review
        _logger.LogInformation("Suspending for plan re-review");

        var checkpointData = new Dictionary<string, object>
        {
            ["UserStories"] = context.AgentContext.State.GetValueOrDefault("UserStories", ""),
            ["ApiDesign"] = context.AgentContext.State.GetValueOrDefault("ApiDesign", ""),
            ["DatabaseSchema"] = context.AgentContext.State.GetValueOrDefault("DatabaseSchema", ""),
            ["TestCases"] = context.AgentContext.State.GetValueOrDefault("TestCases", ""),
            ["ImplementationSteps"] = context.AgentContext.State.GetValueOrDefault("ImplementationSteps", ""),
            ["PlanId"] = context.AgentContext.State.GetValueOrDefault("PlanId", Guid.Empty),
            ["RevisionReason"] = rejected.Feedback,
            ["RevisionVersion"] = context.AgentContext.State.GetValueOrDefault("RevisionVersion", 2)
        };

        await _checkpointService.CreateCheckpointAsync(
            ticketId,
            "PlanningGraph",
            "AwaitingReReview",
            checkpointData,
            cancellationToken);

        return new GraphExecutionResult
        {
            Status = GraphExecutionStatus.Suspended,
            SuspensionReason = "Plan revision complete. Awaiting re-review and approval.",
            SuspendedAt = DateTime.UtcNow,
            NextExpectedMessage = "PlanApprovedMessage or PlanRejectedMessage",
            CheckpointData = checkpointData
        };
    }

    private async Task<AgentExecutionResult> ExecuteAgentAsync<T>(
        AgentExecutionResult previousResult,
        GraphContext context,
        CancellationToken cancellationToken)
        where T : IAgent
    {
        var message = new AgentMessage(context.AgentContext.Ticket.Id);

        // Copy output from previous agent into context for next agent
        foreach (var (key, value) in previousResult.Output)
        {
            context.AgentContext.State[key] = value;
        }

        return await _executor.ExecuteAsync<T>(message, context, cancellationToken);
    }

    private List<string> GetAllArtifactTypes()
    {
        return new List<string>
        {
            "UserStories",
            "ApiDesign",
            "DatabaseSchema",
            "TestCases",
            "ImplementationSteps"
        };
    }
}
```

---

## Integration Tests

### File: PlanningGraphTests.cs

**Path**: `tests/PRFactory.Tests/Agents/Graphs/PlanningGraphTests.cs`

```csharp
using Xunit;
using Moq;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Tests.Agents.Graphs;

public class PlanningGraphTests
{
    private readonly Mock<IAgentExecutor> _mockExecutor;
    private readonly Mock<IWorkflowCheckpointService> _mockCheckpointService;
    private readonly Mock<ILogger<PlanningGraph>> _mockLogger;
    private readonly PlanningGraph _graph;

    public PlanningGraphTests()
    {
        _mockExecutor = new Mock<IAgentExecutor>();
        _mockCheckpointService = new Mock<IWorkflowCheckpointService>();
        _mockLogger = new Mock<ILogger<PlanningGraph>>();
        _graph = new PlanningGraph(
            _mockExecutor.Object,
            _mockCheckpointService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void GraphName_ReturnsCorrectName()
    {
        Assert.Equal("Planning Graph", _graph.Name);
    }

    [Fact]
    public async Task ExecuteCore_WithValidInput_ExecutesAllAgents()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var message = new AgentMessage(ticketId);
        var context = CreateGraphContext(ticketId);

        SetupAgentExecutorForSuccess();

        // Act
        var result = await _graph.ExecuteAsync(message, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Suspended, result.Status);
        Assert.Contains("awaiting", result.SuspensionReason!.ToLower());

        // Verify checkpoint was created
        _mockCheckpointService.Verify(
            x => x.CreateCheckpointAsync(
                ticketId,
                "PlanningGraph",
                "AwaitingApproval",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCore_WithUserStoriesFailure_ReturnsFailed()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var message = new AgentMessage(ticketId);
        var context = CreateGraphContext(ticketId);

        _mockExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Failed,
                Error = "Failed to generate user stories"
            });

        // Act
        var result = await _graph.ExecuteAsync(message, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Failed, result.Status);
        Assert.Contains("user stories", result.Error!.ToLower());
    }

    [Fact]
    public async Task ExecuteCore_WithParallelAgentExecution_RunsInParallel()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var message = new AgentMessage(ticketId);
        var context = CreateGraphContext(ticketId);

        var executionTimes = new List<DateTime>();

        SetupAgentExecutorWithTiming(executionTimes);

        // Act
        var result = await _graph.ExecuteAsync(message, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Suspended, result.Status);

        // API and DB agents should execute concurrently
        // This is a basic test; more sophisticated timing checks could be added
        var apiDbCallCount = executionTimes.Count(
            x => x != default);

        Assert.True(apiDbCallCount >= 2, "At least API and DB agents should execute");
    }

    [Fact]
    public async Task Resume_WithPlanApprovedMessage_CompletesWorkflow()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var approvedMessage = new PlanApprovedMessage(
            ticketId,
            "reviewer@example.com",
            DateTime.UtcNow);
        var context = CreateGraphContext(ticketId);

        // Act
        var result = await _graph.ResumeAsync(approvedMessage, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Completed, result.Status);
        Assert.Contains("implementation", result.CompletionReason!.ToLower());
    }

    [Fact]
    public async Task Resume_WithPlanRejectedMessage_RegeneratesAffectedArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var rejectedMessage = new PlanRejectedMessage(
            ticketId,
            "Add rate limiting to API endpoints",
            true,
            DateTime.UtcNow);
        var context = CreateGraphContext(ticketId);

        SetupAgentExecutorForRevisionWorkflow();

        // Act
        var result = await _graph.ResumeAsync(rejectedMessage, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Suspended, result.Status);
        Assert.Contains("re-review", result.SuspensionReason!.ToLower());

        // Verify revision agents were called
        _mockExecutor.Verify(
            x => x.ExecuteAsync<FeedbackAnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockExecutor.Verify(
            x => x.ExecuteAsync<PlanRevisionAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resume_WithInvalidMessage_ReturnsFailed()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var invalidMessage = new InvalidAgentMessage(ticketId);
        var context = CreateGraphContext(ticketId);

        // Act
        var result = await _graph.ResumeAsync(invalidMessage, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Failed, result.Status);
        Assert.Contains("unexpected", result.Error!.ToLower());
    }

    [Fact]
    public async Task Resume_WithFeedbackAnalysisFailure_FallsBackToAllArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var rejectedMessage = new PlanRejectedMessage(
            ticketId,
            "Redesign everything",
            true,
            DateTime.UtcNow);
        var context = CreateGraphContext(ticketId);

        // FeedbackAnalysisAgent fails
        _mockExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Failed,
                Error = "Analysis failed"
            });

        // But revision continues with all artifacts
        SetupAgentExecutorForRevisionWorkflow(excludeFeedbackAnalysis: true);

        // Act
        var result = await _graph.ResumeAsync(rejectedMessage, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Suspended, result.Status);

        // Revision agent should still execute (with all artifacts)
        _mockExecutor.Verify(
            x => x.ExecuteAsync<PlanRevisionAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCore_WithPartialAgentFailures_StoreAvailableArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var message = new AgentMessage(ticketId);
        var context = CreateGraphContext(ticketId);

        var callCount = 0;
        _mockExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["UserStories"] = "Stories" }
            });

        // API design fails, but DB schema succeeds
        _mockExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Failed,
                Error = "API design failed"
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["DatabaseSchema"] = "Schema" }
            });

        // Rest succeed
        SetupRemainingAgentsForSuccess();

        // Act
        var result = await _graph.ExecuteAsync(message, context, CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Suspended, result.Status);
        // Should still suspend for review even with partial failures
    }

    // Helper methods

    private GraphContext CreateGraphContext(Guid ticketId)
    {
        var ticket = new Ticket
        {
            Id = ticketId,
            TicketKey = "PROJ-999",
            Title = "Test Feature",
            Description = "Test description"
        };

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "test-repo",
            Url = "https://github.com/test/repo"
        };

        var agentContext = new AgentContext
        {
            Ticket = ticket,
            Repository = repository,
            RepositoryPath = "/test/repo",
            State = new Dictionary<string, object>()
        };

        return new GraphContext
        {
            AgentContext = agentContext,
            ExecutionId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow
        };
    }

    private void SetupAgentExecutorForSuccess()
    {
        _mockExecutor
            .Setup(x => x.ExecuteAsync<PmUserStoriesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["UserStories"] = "User Stories Content" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["ApiDesign"] = "OpenAPI Spec" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<ArchitectDbSchemaAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["DatabaseSchema"] = "SQL DDL" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["TestCases"] = "Test Cases" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["ImplementationSteps"] = "Steps" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["PlanId"] = Guid.NewGuid() }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["FeatureBranch"] = "feature/PROJ-999-plan" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["CommentUrl"] = "https://jira.example.com/..." }
            });
    }

    private void SetupAgentExecutorForRevisionWorkflow(bool excludeFeedbackAnalysis = false)
    {
        _mockExecutor
            .Setup(x => x.ExecuteAsync<FeedbackAnalysisAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["AffectedArtifacts"] = new List<string> { "ApiDesign" }
                }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<PlanRevisionAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["ApiDesign"] = "Revised API Design" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["PlanId"] = Guid.NewGuid() }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["FeatureBranch"] = "feature/PROJ-999-plan" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["CommentUrl"] = "https://jira.example.com/..." }
            });
    }

    private void SetupRemainingAgentsForSuccess()
    {
        _mockExecutor
            .Setup(x => x.ExecuteAsync<QaTestCasesAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["TestCases"] = "Test Cases" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<TechLeadImplementationAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["ImplementationSteps"] = "Steps" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<PlanArtifactStorageAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["PlanId"] = Guid.NewGuid() }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<GitPlanAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["FeatureBranch"] = "feature/PROJ-999-plan" }
            });

        _mockExecutor
            .Setup(x => x.ExecuteAsync<JiraPostAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExecutionResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object> { ["CommentUrl"] = "https://jira.example.com/..." }
            });
    }

    private void SetupAgentExecutorWithTiming(List<DateTime> executionTimes)
    {
        SetupAgentExecutorForSuccess();

        _mockExecutor
            .Setup(x => x.ExecuteAsync<ArchitectApiDesignAgent>(
                It.IsAny<IAgentMessage>(),
                It.IsAny<GraphContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (IAgentMessage msg, GraphContext ctx, CancellationToken ct) =>
            {
                executionTimes.Add(DateTime.UtcNow);
                await Task.Delay(10);
                return new AgentExecutionResult
                {
                    Status = AgentStatus.Completed,
                    Output = new Dictionary<string, object> { ["ApiDesign"] = "API" }
                };
            });
    }
}

// Mock message for invalid message test
public class InvalidAgentMessage : IAgentMessage
{
    public Guid TicketId { get; init; }

    public InvalidAgentMessage(Guid ticketId) => TicketId = ticketId;
}
```

---

## Acceptance Criteria

### ExecuteCoreAsync Flow
- [ ] Step 1: PmUserStoriesAgent executes sequentially
- [ ] Step 2: ArchitectApiDesignAgent and ArchitectDbSchemaAgent run in parallel
- [ ] Step 3: QaTestCasesAgent executes after parallel completion
- [ ] Step 4: TechLeadImplementationAgent executes in sequence
- [ ] Step 5: PlanArtifactStorageAgent stores all artifacts
- [ ] Step 6: GitPlanAgent and JiraPostAgent run in parallel
- [ ] Step 7: Graph suspends with checkpoint for human review
- [ ] All agent outputs copied to AgentContext for next agents
- [ ] Partial failures logged but don't prevent suspension

### ResumeCoreAsync Flow
- [ ] PlanApprovedMessage → completes workflow
- [ ] PlanRejectedMessage → runs FeedbackAnalysisAgent
- [ ] Feedback analysis determines affected artifacts
- [ ] PlanRevisionAgent regenerates only affected artifacts
- [ ] Revised artifacts stored with new version
- [ ] Git and Jira updates run in parallel
- [ ] Graph suspends again for re-review
- [ ] Falls back gracefully if feedback analysis fails

### Checkpoint Management
- [ ] Checkpoint created on initial suspension
- [ ] Checkpoint contains all artifact data
- [ ] Checkpoint created again after revision
- [ ] Checkpoint includes revision metadata
- [ ] Checkpoint data can be restored on resume

### Error Handling
- [ ] User stories failure → returns failed
- [ ] Partial artifact failures → logs warnings, continues
- [ ] Storage failure → returns failed
- [ ] Git/Jira parallel failures → logs warnings, continues
- [ ] Invalid resume messages → returns failed
- [ ] Feedback analysis failure → falls back to all artifacts

---

## Implementation Checklist

- [ ] Update `PlanningGraph.ExecuteCoreAsync()` with 7-step orchestration
- [ ] Implement parallel execution for API + DB agents
- [ ] Implement parallel execution for Git + Jira agents
- [ ] Create checkpoint on initial suspension
- [ ] Implement `PlanningGraph.ResumeCoreAsync()` with revision flow
- [ ] Implement `HandlePlanApprovedAsync()`
- [ ] Implement `HandlePlanRejectedAsync()` with feedback analysis
- [ ] Create 12+ integration tests for ExecuteCore flow
- [ ] Create 8+ integration tests for Resume flow
- [ ] Verify all tests pass
- [ ] Verify xUnit assertions only (no FluentAssertions)
- [ ] Run `dotnet test` successfully with >80% coverage

---

## Files to Create/Modify

**Modify:**
- `src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs` - Update ExecuteCoreAsync and ResumeCoreAsync

**Create:**
- `tests/PRFactory.Tests/Agents/Graphs/PlanningGraphTests.cs` - Integration tests

---

## Dependencies

**Internal:**
- All agents: `PmUserStoriesAgent`, `ArchitectApiDesignAgent`, `ArchitectDbSchemaAgent`, `QaTestCasesAgent`, `TechLeadImplementationAgent`, `PlanArtifactStorageAgent`, `GitPlanAgent`, `JiraPostAgent`, `FeedbackAnalysisAgent`, `PlanRevisionAgent`
- `IAgentExecutor` (agent orchestration)
- `IWorkflowCheckpointService` (checkpoint persistence)
- Message types: `PlanApprovedMessage`, `PlanRejectedMessage`

**External:**
- System.Threading (Task.WhenAll for parallel execution)
- Microsoft.Extensions.Logging (logging)

---

## Estimated Effort

- **ExecuteCoreAsync Implementation**: 4-6 hours
- **ResumeCoreAsync Implementation**: 4-6 hours
- **Integration Tests**: 6-8 hours
- **Code Review & Fixes**: 2-4 hours
- **Total**: 2-3 days

---

## Related Parts

- **Part 1**: Multi-artifact agents (required)
- **Part 2**: Database schema (required)
- **Part 3**: Storage agent (required)
- **Part 4**: Revision agents (required)
- **Part 7**: Web UI integration (depends on this)

---

## Success Metrics

- All agents execute in correct sequence
- Parallel agents complete concurrently
- 20+ integration tests, all passing
- >80% code coverage for graph logic
- Checkpoint creation succeeds on suspension
- Revision workflow completes successfully with selective regeneration
