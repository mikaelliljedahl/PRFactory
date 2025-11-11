# Plan Revision Workflow Implementation Plan

**Epic:** Deep Planning Phase (Epic 3)
**Component:** Plan Revision via Graph Resumption
**Estimated Effort:** 2-3 days
**Dependencies:** Multi-artifact agents (01), Database schema (02), Web UI (03)

---

## Overview

This implementation plan covers the revision workflow that allows users to provide natural language feedback on generated plans, automatically determines which artifacts need regeneration, and resumes the PlanningGraph to update only the affected artifacts.

---

## Workflow Architecture

### High-Level Flow

```
User reviews plan in Web UI
  ↓
User provides revision feedback (natural language)
  ↓
Web UI calls IWorkflowOrchestrator.ResumeAsync(PlanRejectedMessage)
  ↓
PlanningGraph.ResumeCoreAsync() invoked
  ↓
FeedbackAnalysisAgent analyzes feedback → determines affected artifacts
  ↓
PlanRevisionAgent regenerates ONLY affected artifacts
  ↓
PlanArtifactStorageAgent saves updated artifacts (creates new version)
  ↓
GitPlanAgent commits updated artifacts to Git
  ↓
JiraPostAgent posts revision summary to Jira
  ↓
HumanWaitAgent suspends workflow for re-review
  ↓
User re-reviews plan → approves or rejects again
```

---

## Implementation Steps

### Step 1: Define Revision Messages

**File:** `/src/PRFactory.Infrastructure/Agents/Messages/PlanRevisionMessages.cs`

```csharp
using System;

namespace PRFactory.Infrastructure.Agents.Messages;

/// <summary>
/// Message sent when user rejects a plan and requests revision.
/// </summary>
public record PlanRejectedMessage(
    Guid TicketId,
    string Feedback,
    bool Regenerate,
    DateTime Timestamp
) : IAgentMessage;

/// <summary>
/// Message sent when user approves a plan.
/// </summary>
public record PlanApprovedMessage(
    Guid TicketId,
    string? ApprovedBy,
    DateTime Timestamp
) : IAgentMessage;

/// <summary>
/// Internal message with analysis results.
/// </summary>
public record FeedbackAnalysisResult(
    Guid TicketId,
    string Feedback,
    List<string> AffectedArtifacts,  // e.g., ["UserStories", "ApiDesign"]
    string AnalysisSummary,
    DateTime Timestamp
) : IAgentMessage;
```

---

### Step 2: Implement FeedbackAnalysisAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/FeedbackAnalysisAgent.cs`

```csharp
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Analyzes user feedback to determine which plan artifacts need regeneration.
/// Uses LLM to understand intent and map to specific artifacts.
/// </summary>
public class FeedbackAnalysisAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly ILogger<FeedbackAnalysisAgent> _logger;

    public override string Name => "Feedback Analysis Agent";
    public override string Description => "Analyzes revision feedback to determine affected artifacts";

    public FeedbackAnalysisAgent(
        ICliAgent cliAgent,
        ILogger<FeedbackAnalysisAgent> logger)
    {
        _cliAgent = cliAgent;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var feedback = GetRequiredStateValue<string>(context, "RevisionFeedback");

        _logger.LogInformation(
            "Analyzing feedback for ticket {TicketKey}: {Feedback}",
            context.Ticket.TicketKey,
            feedback);

        // Build prompt for LLM to analyze feedback
        var prompt = BuildFeedbackAnalysisPrompt(feedback);

        var cliResponse = await _cliAgent.ExecutePromptAsync(prompt, cancellationToken);

        if (!cliResponse.Success)
        {
            return AgentResult.Failed($"Feedback analysis failed: {cliResponse.ErrorMessage}");
        }

        // Parse JSON response
        var analysisJson = ExtractJsonFromResponse(cliResponse.Content);
        var analysis = JsonSerializer.Deserialize<FeedbackAnalysisDto>(analysisJson);

        if (analysis == null || !analysis.AffectedArtifacts.Any())
        {
            _logger.LogWarning("No affected artifacts identified. Defaulting to regenerate all.");
            analysis = new FeedbackAnalysisDto
            {
                AffectedArtifacts = new List<string>
                {
                    "UserStories", "ApiDesign", "DatabaseSchema", "TestCases", "ImplementationSteps"
                },
                Summary = "Unable to determine specific artifacts, regenerating all."
            };
        }

        // Store analysis result in context
        context.State["AffectedArtifacts"] = analysis.AffectedArtifacts;
        context.State["AnalysisSummary"] = analysis.Summary;

        return AgentResult.Completed(new Dictionary<string, object>
        {
            ["AffectedArtifacts"] = analysis.AffectedArtifacts,
            ["Summary"] = analysis.Summary
        });
    }

    private string BuildFeedbackAnalysisPrompt(string feedback)
    {
        return $@"You are analyzing user feedback on an implementation plan to determine which artifacts need regeneration.

<feedback>
{feedback}
</feedback>

<artifacts>
The plan consists of 5 artifacts:
1. UserStories - User stories with acceptance criteria
2. ApiDesign - OpenAPI specification (YAML)
3. DatabaseSchema - SQL DDL statements
4. TestCases - Test scenarios and cases
5. ImplementationSteps - Step-by-step implementation guide
</artifacts>

<task>
Analyze the feedback and determine which artifacts are affected and need regeneration.

Keywords to look for:
- ""user stor"", ""acceptance criteria"" → UserStories
- ""api"", ""endpoint"", ""request"", ""response"" → ApiDesign
- ""database"", ""schema"", ""table"", ""column"", ""index"" → DatabaseSchema
- ""test"", ""qa"", ""scenario"" → TestCases
- ""implementation"", ""step"", ""code"" → ImplementationSteps

Output a JSON object with:
{{
  ""affectedArtifacts"": [""ArtifactName1"", ""ArtifactName2""],
  ""summary"": ""Brief explanation of what needs to change""
}}

Important:
- If feedback is unclear or mentions multiple areas, include all related artifacts
- If feedback mentions ""plan"" or ""everything"", include all artifacts
- Output ONLY valid JSON (no preamble or explanation)
</task>";
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Try to extract JSON from markdown code blocks
        var jsonPattern = @"```json\s*(.*?)\s*```";
        var match = Regex.Match(response, jsonPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Try to find JSON object directly
        var jsonObjectPattern = @"\{[\s\S]*\}";
        match = Regex.Match(response, jsonObjectPattern);

        if (match.Success)
        {
            return match.Value.Trim();
        }

        // Fallback: assume entire response is JSON
        return response.Trim();
    }

    private T GetRequiredStateValue<T>(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} not found in context");
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"{key} is not of expected type {typeof(T).Name}");
        }

        return typedValue;
    }

    private class FeedbackAnalysisDto
    {
        [JsonPropertyName("affectedArtifacts")]
        public List<string> AffectedArtifacts { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
    }
}
```

---

### Step 3: Implement PlanRevisionAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/PlanRevisionAgent.cs`

```csharp
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Regenerates specific plan artifacts based on feedback analysis.
/// Coordinates execution of only the affected artifact agents.
/// </summary>
public class PlanRevisionAgent : BaseAgent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlanRevisionAgent> _logger;

    public override string Name => "Plan Revision Agent";
    public override string Description => "Regenerates specific artifacts based on feedback";

    public PlanRevisionAgent(
        IServiceProvider serviceProvider,
        ILogger<PlanRevisionAgent> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var affectedArtifacts = GetRequiredStateValue<List<string>>(context, "AffectedArtifacts");
        var feedback = GetRequiredStateValue<string>(context, "RevisionFeedback");

        _logger.LogInformation(
            "Regenerating artifacts for ticket {TicketKey}: {Artifacts}",
            context.Ticket.TicketKey,
            string.Join(", ", affectedArtifacts));

        // Store feedback in context for agents to use
        context.State["RevisionInstructions"] = feedback;

        // Regenerate each affected artifact
        foreach (var artifact in affectedArtifacts)
        {
            await RegenerateArtifactAsync(artifact, context, cancellationToken);
        }

        return AgentResult.Completed(new Dictionary<string, object>
        {
            ["RegeneratedArtifacts"] = affectedArtifacts,
            ["ArtifactCount"] = affectedArtifacts.Count
        });
    }

    private async Task RegenerateArtifactAsync(
        string artifactName,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Regenerating artifact: {Artifact}", artifactName);

        BaseAgent? agent = artifactName switch
        {
            "UserStories" => _serviceProvider.GetService<PmUserStoriesAgent>(),
            "ApiDesign" => _serviceProvider.GetService<ArchitectApiDesignAgent>(),
            "DatabaseSchema" => _serviceProvider.GetService<ArchitectDbSchemaAgent>(),
            "TestCases" => _serviceProvider.GetService<QaTestCasesAgent>(),
            "ImplementationSteps" => _serviceProvider.GetService<TechLeadImplementationAgent>(),
            _ => null
        };

        if (agent == null)
        {
            _logger.LogWarning("Unknown artifact type: {Artifact}", artifactName);
            return;
        }

        // Execute agent to regenerate artifact
        var result = await agent.ExecuteWithMiddlewareAsync(context, cancellationToken);

        if (result.Status != AgentStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Failed to regenerate {artifactName}: {result.Error}");
        }

        _logger.LogInformation("Successfully regenerated artifact: {Artifact}", artifactName);
    }

    private T GetRequiredStateValue<T>(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} not found in context");
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"{key} is not of expected type {typeof(T).Name}");
        }

        return typedValue;
    }
}
```

---

### Step 4: Update PlanningGraph with Revision Logic

**File:** `/src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs`

Update `ResumeCoreAsync` method:

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

    switch (resumeMessage)
    {
        case PlanApprovedMessage approved:
            return await HandlePlanApprovalAsync(approved, context, cancellationToken);

        case PlanRejectedMessage rejected:
            return await HandlePlanRejectionAsync(rejected, context, cancellationToken);

        default:
            throw new InvalidOperationException(
                $"Unexpected resume message type: {resumeMessage.GetType().Name}");
    }
}

private async Task<GraphExecutionResult> HandlePlanApprovalAsync(
    PlanApprovedMessage approved,
    GraphContext context,
    CancellationToken cancellationToken)
{
    _logger.LogInformation("Plan approved for ticket {TicketId}", approved.TicketId);

    // Update ticket state
    await UpdateTicketStateAsync(approved.TicketId, WorkflowState.PlanApproved);

    // Emit event for WorkflowOrchestrator to start ImplementationGraph (if enabled)
    return new GraphExecutionResult
    {
        Status = GraphExecutionStatus.Completed,
        EmittedEvent = new PlanApprovedEvent(approved.TicketId, DateTime.UtcNow)
    };
}

private async Task<GraphExecutionResult> HandlePlanRejectionAsync(
    PlanRejectedMessage rejected,
    GraphContext context,
    CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "Plan rejected for ticket {TicketId} with feedback: {Feedback}",
        rejected.TicketId,
        rejected.Feedback);

    // Store feedback in context
    context.AgentContext.State["RevisionFeedback"] = rejected.Feedback;

    // Step 1: Analyze feedback to determine affected artifacts
    var analysisResult = await _executor.ExecuteAsync<FeedbackAnalysisAgent>(
        rejected,
        context,
        cancellationToken);

    if (analysisResult == null)
    {
        throw new InvalidOperationException("Feedback analysis failed");
    }

    // Step 2: Regenerate affected artifacts
    var revisionResult = await _executor.ExecuteAsync<PlanRevisionAgent>(
        analysisResult,
        context,
        cancellationToken);

    // Step 3: Store updated artifacts in database (increment version)
    var storageMessage = new PlanArtifactsUpdatedMessage(
        rejected.TicketId,
        DateTime.UtcNow,
        RevisionReason: rejected.Feedback);

    await _executor.ExecuteAsync<PlanArtifactStorageAgent>(
        storageMessage,
        context,
        cancellationToken);

    // Step 4: Commit updated artifacts to Git
    var gitTask = _executor.ExecuteAsync<GitPlanAgent>(
        storageMessage, context, cancellationToken);

    // Step 5: Post revision summary to Jira
    var jiraTask = _executor.ExecuteAsync<JiraPostAgent>(
        storageMessage, context, cancellationToken);

    await Task.WhenAll(gitTask, jiraTask);

    // Step 6: Suspend again for re-review
    await _executor.ExecuteAsync<HumanWaitAgent>(
        storageMessage, context, cancellationToken);

    _logger.LogInformation(
        "Plan revision completed for ticket {TicketId}. Awaiting re-review.",
        rejected.TicketId);

    return new GraphExecutionResult
    {
        Status = GraphExecutionStatus.Suspended,
        SuspensionReason = "Awaiting plan re-approval after revision",
        SuspendedAt = DateTime.UtcNow,
        NextExpectedMessage = typeof(PlanApprovedMessage).Name
    };
}
```

---

### Step 5: Update PlanArtifactStorageAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/PlanArtifactStorageAgent.cs`

```csharp
/// <summary>
/// Stores plan artifacts in the database.
/// Supports both initial creation and version updates.
/// </summary>
public class PlanArtifactStorageAgent : BaseAgent
{
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<PlanArtifactStorageAgent> _logger;

    public override string Name => "Plan Artifact Storage Agent";
    public override string Description => "Stores plan artifacts in database with versioning";

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = context.Ticket.Id;

        // Get artifacts from context
        var userStories = context.State.GetValueOrDefault("UserStories") as string;
        var apiDesign = context.State.GetValueOrDefault("ApiDesign") as string;
        var dbSchema = context.State.GetValueOrDefault("DatabaseSchema") as string;
        var testCases = context.State.GetValueOrDefault("TestCases") as string;
        var implementationSteps = context.State.GetValueOrDefault("ImplementationSteps") as string;

        // Check if this is an update or initial creation
        var existingPlan = await _planRepository.GetByTicketIdAsync(ticketId, cancellationToken);

        if (existingPlan != null)
        {
            // Update existing plan with new version
            var revisionReason = context.State.GetValueOrDefault("RevisionFeedback") as string;
            var createdBy = context.State.GetValueOrDefault("ApprovedBy") as string;

            existingPlan.UpdateArtifacts(
                userStories: userStories,
                apiDesign: apiDesign,
                databaseSchema: dbSchema,
                testCases: testCases,
                implementationSteps: implementationSteps,
                createdBy: createdBy,
                revisionReason: revisionReason);

            await _planRepository.UpdateAsync(existingPlan, cancellationToken);

            _logger.LogInformation(
                "Updated plan for ticket {TicketKey} to version {Version}",
                context.Ticket.TicketKey,
                existingPlan.Version);
        }
        else
        {
            // Create new plan
            var plan = new Plan
            {
                TicketId = ticketId,
                UserStories = userStories,
                ApiDesign = apiDesign,
                DatabaseSchema = dbSchema,
                TestCases = testCases,
                ImplementationSteps = implementationSteps,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _planRepository.AddAsync(plan, cancellationToken);

            _logger.LogInformation(
                "Created plan for ticket {TicketKey} (version 1)",
                context.Ticket.TicketKey);
        }

        return AgentResult.Completed(new Dictionary<string, object>
        {
            ["TicketId"] = ticketId,
            ["Version"] = existingPlan?.Version ?? 1
        });
    }
}
```

---

### Step 6: Update Web UI Service

**File:** `/src/PRFactory.Web/Services/TicketService.cs`

Add methods for revision:

```csharp
public async Task RequestPlanRevisionAsync(Guid ticketId, string feedback)
{
    // Resume workflow with rejection message
    await _workflowOrchestrator.ResumeAsync(
        ticketId,
        new PlanRejectedMessage(
            ticketId,
            feedback,
            Regenerate: true,
            Timestamp: DateTime.UtcNow));

    // Update ticket state to PlanRejected
    var ticket = await _ticketRepository.GetByIdAsync(ticketId);
    if (ticket != null)
    {
        ticket.State = WorkflowState.PlanRejected;
        await _ticketRepository.UpdateAsync(ticket);
    }
}

public async Task ApprovePlanAsync(Guid ticketId, string? approvedBy = null)
{
    // Resume workflow with approval message
    await _workflowOrchestrator.ResumeAsync(
        ticketId,
        new PlanApprovedMessage(
            ticketId,
            approvedBy,
            Timestamp: DateTime.UtcNow));

    // Update ticket state to PlanApproved
    var ticket = await _ticketRepository.GetByIdAsync(ticketId);
    if (ticket != null)
    {
        ticket.State = WorkflowState.PlanApproved;
        await _ticketRepository.UpdateAsync(ticket);
    }
}
```

---

## Testing

### Unit Tests

**File:** `/tests/PRFactory.Infrastructure.Tests/Agents/Planning/FeedbackAnalysisAgentTests.cs`

```csharp
public class FeedbackAnalysisAgentTests
{
    [Theory]
    [InlineData("Add rate limiting to the API", new[] { "ApiDesign", "ImplementationSteps" })]
    [InlineData("Update database schema to add indexes", new[] { "DatabaseSchema" })]
    [InlineData("Add more test cases for edge scenarios", new[] { "TestCases" })]
    [InlineData("Revise everything", new[] { "UserStories", "ApiDesign", "DatabaseSchema", "TestCases", "ImplementationSteps" })]
    public async Task AnalyzeFeedback_IdentifiesCorrectArtifacts(
        string feedback,
        string[] expectedArtifacts)
    {
        // Arrange
        var mockCliAgent = new Mock<ICliAgent>();
        var mockResponse = CreateMockAnalysisResponse(expectedArtifacts);
        mockCliAgent
            .Setup(x => x.ExecutePromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        var agent = new FeedbackAnalysisAgent(mockCliAgent.Object, Mock.Of<ILogger<FeedbackAnalysisAgent>>());

        var context = new AgentContext
        {
            State = new Dictionary<string, object>
            {
                ["RevisionFeedback"] = feedback
            }
        };

        // Act
        var result = await agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        var affectedArtifacts = context.State["AffectedArtifacts"] as List<string>;
        Assert.NotNull(affectedArtifacts);
        Assert.Equal(expectedArtifacts.Length, affectedArtifacts.Count);
        Assert.All(expectedArtifacts, artifact => Assert.Contains(artifact, affectedArtifacts));
    }
}
```

### Integration Tests

**File:** `/tests/PRFactory.Infrastructure.Tests/Agents/Graphs/PlanningGraphRevisionTests.cs`

```csharp
public class PlanningGraphRevisionTests : IClassFixture<GraphTestFixture>
{
    [Fact]
    public async Task ResumeCoreAsync_WithRejection_RegeneratesAffectedArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = CreateGraphContext(ticketId);

        // Simulate initial plan generated
        context.AgentContext.State["UserStories"] = "Original user stories";
        context.AgentContext.State["ApiDesign"] = "Original API design";

        var rejectionMessage = new PlanRejectedMessage(
            ticketId,
            Feedback: "Add rate limiting to the API endpoints",
            Regenerate: true,
            Timestamp: DateTime.UtcNow);

        // Act
        var result = await _planningGraph.ResumeCoreAsync(
            rejectionMessage,
            context,
            CancellationToken.None);

        // Assert
        Assert.Equal(GraphExecutionStatus.Suspended, result.Status);
        Assert.Contains("re-approval", result.SuspensionReason);

        // Verify artifacts were regenerated
        Assert.True(context.AgentContext.State.ContainsKey("ApiDesign"));
        var updatedApiDesign = context.AgentContext.State["ApiDesign"] as string;
        Assert.NotEqual("Original API design", updatedApiDesign);
    }
}
```

---

## Acceptance Criteria

- [ ] `FeedbackAnalysisAgent` correctly identifies affected artifacts from natural language
- [ ] `PlanRevisionAgent` regenerates only affected artifacts
- [ ] `PlanningGraph.ResumeCoreAsync()` handles rejection and approval messages
- [ ] Plan versions incremented on revision
- [ ] Version history captured with feedback reason
- [ ] Web UI triggers revision workflow correctly
- [ ] Git commits show revision updates
- [ ] Jira receives revision notifications
- [ ] Unit tests for feedback analysis (80% coverage)
- [ ] Integration tests for full revision workflow

---

## Edge Cases

| Scenario | Handling |
|----------|----------|
| User provides vague feedback (e.g., "make it better") | Default to regenerating all artifacts |
| LLM fails to analyze feedback | Fallback to keyword-based detection |
| Revision fails for specific artifact | Retry with exponential backoff, notify user |
| User rejects plan multiple times (>3) | Escalate to human review (flag ticket) |
| Concurrent revisions on same ticket | Lock ticket during revision, queue subsequent requests |

---

## Performance Considerations

- **Selective Regeneration:** Only regenerate affected artifacts (saves LLM API costs)
- **Parallel Execution:** If multiple artifacts affected, regenerate in parallel where possible
- **Caching:** Cache codebase context between revisions (don't re-scan repository)
- **Version Limits:** Archive old versions after 10 revisions (keep latest 10 only)

---

## Next Steps

After completing revision workflow:
1. Add real-time progress updates (SignalR) for long-running revisions
2. Implement plan diff viewer (compare versions side-by-side)
3. Add revision analytics (track common feedback patterns)
4. Implement approval workflows (multi-approver support)
