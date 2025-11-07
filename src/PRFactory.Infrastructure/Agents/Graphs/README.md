# PRFactory Agent Workflow Graphs

This directory contains the agent workflow graphs for PRFactory, built using Microsoft Agent Framework patterns.

## Overview

The PRFactory workflow is decomposed into three specialized graphs that are orchestrated together:

1. **RefinementGraph** - Ticket refinement workflow
2. **PlanningGraph** - Plan generation workflow
3. **ImplementationGraph** - Code implementation workflow (optional)

These graphs are coordinated by the **WorkflowOrchestrator**, which manages the overall workflow state and transitions between graphs.

## Graph Implementations

### 1. RefinementGraph.cs

**Purpose:** Refine vague Jira tickets by analyzing the codebase and generating clarifying questions.

**Flow:**
```
Trigger → RepositoryClone → Analysis → QuestionGeneration → JiraPost → HumanWait → AnswerProcessing
```

**Features:**
- **Retry Logic:** Analysis agent retries up to 3 times on failure with exponential backoff
- **Checkpointing:** Saves checkpoint after each agent completes
- **Suspension:** Enters suspended state while waiting for human answers
- **Event Emission:** Emits `RefinementCompleteEvent` on success

**Key Methods:**
- `ExecuteCoreAsync()` - Main execution flow
- `ResumeCoreAsync()` - Resume from `awaiting_answers` state
- `ExecuteAnalysisWithRetryAsync()` - Analysis with retry logic

### 2. PlanningGraph.cs

**Purpose:** Generate implementation plan and get human approval.

**Flow:**
```
Planning → GitPlan + JiraPost (parallel) → HumanWait (approval)
```

**Features:**
- **Parallel Execution:** GitPlan and JiraPost run concurrently for efficiency
- **Conditional Logic:**
  - If approved → emit `PlanApprovedEvent`
  - If rejected → loop back to Planning (max 5 retries)
- **Checkpointing:** Saves after plan generation and after parallel execution
- **Plan Iteration:** Supports plan rejection with retry counter

**Key Methods:**
- `GenerateAndPostPlanAsync()` - Generate plan and post in parallel
- `HandlePlanApprovalAsync()` - Handle approval and emit event
- `HandlePlanRejectionAsync()` - Handle rejection and retry

### 3. ImplementationGraph.cs

**Purpose:** Implement approved plan and create pull request (optional based on tenant config).

**Flow:**
```
Implementation → GitCommit → PullRequest + JiraPost (parallel) → Completion
```

**Features:**
- **Conditional Execution:** Only runs if `AutoImplementAfterPlanApproval` is enabled
- **Parallel Execution:** PullRequest creation and Jira posting run concurrently
- **Tenant Configuration:** Checks tenant settings before executing
- **Skip Logic:** Can skip entire graph if auto-implementation is disabled

**Key Methods:**
- `CheckAutoImplementationEnabledAsync()` - Check tenant configuration
- `ExecuteCoreAsync()` - Main implementation flow with conditional execution

### 4. WorkflowOrchestrator.cs

**Purpose:** Coordinate the three graphs and manage overall workflow state.

**Responsibilities:**
- Start new workflows from trigger messages
- Resume suspended workflows from webhooks
- Transition between graphs based on events
- Track overall workflow status
- Publish workflow lifecycle events

**Flow:**
```
Start → RefinementGraph
      → (on RefinementCompleteEvent) → PlanningGraph
      → (on PlanApprovedEvent) → ImplementationGraph
      → (on completion) → Workflow Complete
```

**Key Methods:**
- `StartWorkflowAsync()` - Start new workflow with trigger message
- `ResumeWorkflowAsync()` - Resume suspended workflow with input
- `HandleGraphTransitionAsync()` - Transition between graphs based on events
- `GetWorkflowStatusAsync()` - Get current workflow status
- `CancelWorkflowAsync()` - Cancel running workflow

**Events Emitted:**
- `WorkflowSuspendedEvent` - When workflow suspends for human input
- `WorkflowCompletedEvent` - When workflow completes successfully
- `WorkflowFailedEvent` - When workflow fails
- `WorkflowCancelledEvent` - When workflow is cancelled

### 5. GraphBuilder.cs

**Purpose:** Fluent API for constructing custom agent graphs.

**Features:**
- **Fluent Interface:** Chain methods to build complex graphs
- **Agent Nodes:** Add agents to the execution graph
- **Conditional Branching:** Add conditional logic with true/false branches
- **Parallel Execution:** Execute multiple agents concurrently
- **Checkpoint Support:** Add explicit checkpoints
- **Retry Logic:** Configure per-agent retry behavior
- **Error Handling:** Custom error handlers per node

**Usage Example:**
```csharp
var graph = new GraphBuilder("CustomGraph", serviceProvider)
    .AddAgent<TriggerAgent>()
    .AddCheckpoint("triggered")
    .AddParallel(
        b => b.AddAgent<Agent1>(),
        b => b.AddAgent<Agent2>()
    )
    .WithRetry(maxAttempts: 3, delay: TimeSpan.FromSeconds(2))
    .AddConditional(
        condition: (ctx, msg) => ctx.State.ContainsKey("approved"),
        trueBranch: b => b.AddAgent<ApprovalAgent>(),
        falseBranch: b => b.AddAgent<RejectionAgent>()
    )
    .AddAgent<CompletionAgent>()
    .Build();

// Execute the graph
var result = await graph.ExecuteAsync(inputMessage);
```

**Node Types:**
- `AgentNode<TAgent>` - Execute a specific agent
- `ConditionalNode` - Branch based on condition
- `ParallelNode` - Execute multiple branches concurrently
- `CheckpointNode` - Save checkpoint

## Supporting Infrastructure

### Base Classes

**AgentGraphBase** (`/Base/AgentGraphBase.cs`)
- Abstract base class for all graphs
- Provides common functionality: checkpointing, error handling, telemetry
- Template methods: `ExecuteCoreAsync()`, `ResumeCoreAsync()`

**IAgentGraph** (`/Base/IAgentGraph.cs`)
- Interface for graph execution
- Methods: `ExecuteAsync()`, `ResumeAsync()`, `GetStatusAsync()`

### Message Definitions

All message types are defined in `/Messages/AgentMessages.cs`:

**Core Messages:**
- `TriggerTicketMessage` - Start workflow
- `TicketTriggeredMessage` - Ticket initialized
- `RepositoryClonedMessage` - Repo cloned
- `CodebaseAnalyzedMessage` - Analysis complete
- `QuestionsGeneratedMessage` - Questions ready
- `MessagePostedMessage` - Posted to Jira
- `AnswersReceivedMessage` - Human provided answers

**Planning Messages:**
- `PlanGeneratedMessage` - Plan created
- `PlanCommittedMessage` - Plan committed to git
- `PlanApprovedMessage` - Human approved plan
- `PlanRejectedMessage` - Human rejected plan

**Implementation Messages:**
- `CodeImplementedMessage` - Code generated
- `PRCreatedMessage` - Pull request created
- `WorkflowCompletedMessage` - Workflow finished

**Event Messages:**
- `RefinementCompleteEvent` - Refinement graph done
- `PlanApprovedEvent` - Planning graph done with approval

## Checkpointing Strategy

Each graph saves checkpoints at key stages:

**RefinementGraph Checkpoints:**
- `trigger_complete` - After trigger
- `clone_complete` - After repository clone
- `analysis_complete` - After analysis (with retries)
- `questions_generated` - After question generation
- `questions_posted` - After posting to Jira
- `awaiting_answers` - Suspended, waiting for human
- `answers_processed` - After processing answers
- `refinement_complete` - Graph complete

**PlanningGraph Checkpoints:**
- `plan_generated` - After plan generation
- `plan_posted` - After parallel git commit + Jira post
- `awaiting_approval` - Suspended, waiting for approval
- `plan_approved` - Approved
- `plan_rejected` - Rejected (loops back)

**ImplementationGraph Checkpoints:**
- `skipped` - If auto-implementation disabled
- `code_implemented` - After code generation
- `code_committed` - After git commit
- `pr_created` - After parallel PR + Jira post
- `completed` - Graph complete

## Error Handling

### Retry Strategies

**Analysis Agent (RefinementGraph):**
- Max retries: 3
- Backoff: Exponential (2^attempt seconds)
- Saves checkpoint after each retry

**Plan Rejection (PlanningGraph):**
- Max retries: 5
- Loops back to planning agent
- Includes rejection reason in context

**General Agent Execution:**
- Configurable per agent via `WithRetry()`
- Custom error handlers via `WithErrorHandler()`

### Failure Handling

When a graph fails:
1. Error is logged with full context
2. State is marked as `is_failed = true`
3. Checkpoint saved with failure state
4. `GraphExecutionResult.Failure()` returned
5. Orchestrator publishes `WorkflowFailedEvent`

## State Management

### GraphContext

Passed to all agents during execution:

```csharp
public class GraphContext
{
    public Guid TicketId { get; set; }
    public string GraphId { get; set; }
    public DateTime StartedAt { get; set; }
    public Dictionary<string, object> State { get; set; }
    public int RetryCount { get; set; }
    public string CurrentCheckpoint { get; set; }
}
```

### WorkflowState

Maintained by orchestrator:

```csharp
public class WorkflowState
{
    public Guid WorkflowId { get; set; }
    public Guid TicketId { get; set; }
    public string CurrentGraph { get; set; }
    public string CurrentState { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ErrorMessage { get; set; }
}
```

**Workflow Statuses:**
- `NotFound` - No workflow exists
- `Running` - Currently executing
- `Suspended` - Waiting for human input
- `Completed` - Successfully finished
- `Failed` - Encountered error
- `Cancelled` - Manually cancelled

## Human-in-the-Loop

Graphs suspend execution at human interaction points:

**RefinementGraph:**
- Suspends at: `awaiting_answers`
- Resumes with: `AnswersReceivedMessage`

**PlanningGraph:**
- Suspends at: `awaiting_approval`
- Resumes with: `PlanApprovedMessage` or `PlanRejectedMessage`

**Resume Flow:**
1. Webhook received (Jira comment)
2. Comment parsed to create resume message
3. `WorkflowOrchestrator.ResumeWorkflowAsync()` called
4. Loads checkpoint from state store
5. Calls appropriate graph's `ResumeAsync()`
6. Graph continues from suspended point

## Integration with Microsoft Agent Framework

While this implementation follows Microsoft Agent Framework patterns, it provides a custom graph execution engine optimized for PRFactory's workflow:

**Framework Concepts Used:**
- **Agents:** Autonomous units processing specific tasks
- **Graphs:** Directed workflows connecting agents
- **Checkpointing:** State persistence for resumption
- **Message Passing:** Strongly-typed agent communication
- **Observability:** OpenTelemetry integration via ActivitySource

**Custom Extensions:**
- Specialized graphs for ticket workflow
- Human-in-the-loop suspension/resume
- Multi-graph orchestration
- Tenant-based conditional execution

## Service Registration

Register all graphs in your DI container:

```csharp
// In Startup.cs or Program.cs
services.AddAgentGraphs();

// Or manually:
services.AddScoped<RefinementGraph>();
services.AddScoped<PlanningGraph>();
services.AddScoped<ImplementationGraph>();
services.AddScoped<WorkflowOrchestrator>();
services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();
```

## Usage Example

### Starting a Workflow

```csharp
var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();

var triggerMessage = new TriggerTicketMessage(
    TicketKey: "PROJ-123",
    TenantId: tenantId,
    RepositoryId: repoId,
    TicketSystem: "Jira"
);

var workflowId = await orchestrator.StartWorkflowAsync(triggerMessage);
```

### Resuming from Webhook

```csharp
// When Jira webhook received with answers
var answersMessage = new AnswersReceivedMessage(
    ticketId,
    answers: new Dictionary<string, string>
    {
        ["question_1"] = "Answer 1",
        ["question_2"] = "Answer 2"
    }
);

await orchestrator.ResumeWorkflowAsync(ticketId, answersMessage);
```

### Checking Status

```csharp
var status = await orchestrator.GetWorkflowStatusAsync(ticketId);

switch (status)
{
    case WorkflowStatus.Running:
        // Workflow in progress
        break;
    case WorkflowStatus.Suspended:
        // Waiting for human input
        break;
    case WorkflowStatus.Completed:
        // Successfully finished
        break;
    case WorkflowStatus.Failed:
        // Error occurred
        break;
}
```

## Dependencies

Each graph requires:

- `ILogger<T>` - Structured logging
- `ICheckpointStore` - State persistence
- `IAgentExecutor` - Agent execution
- Additional services specific to each graph

Orchestrator requires:

- All three graphs
- `IWorkflowStateStore` - Workflow state persistence
- `IEventPublisher` - Event publishing

## Testing

Unit test each graph independently:

```csharp
[Fact]
public async Task RefinementGraph_CompletesSuccessfully()
{
    // Arrange
    var mockAgentExecutor = new Mock<IAgentExecutor>();
    var mockCheckpointStore = new Mock<ICheckpointStore>();

    var graph = new RefinementGraph(
        logger,
        mockCheckpointStore.Object,
        mockAgentExecutor.Object
    );

    var triggerMessage = new TriggerTicketMessage(...);

    // Act
    var result = await graph.ExecuteAsync(triggerMessage);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("awaiting_answers", result.State);
}
```

Integration test the orchestrator:

```csharp
[Fact]
public async Task Orchestrator_HandlesFullWorkflow()
{
    // Test complete flow from trigger to completion
    var workflowId = await orchestrator.StartWorkflowAsync(trigger);

    // Simulate human input
    await orchestrator.ResumeWorkflowAsync(ticketId, answers);

    // Simulate approval
    await orchestrator.ResumeWorkflowAsync(ticketId, approval);

    // Verify completion
    var status = await orchestrator.GetWorkflowStatusAsync(ticketId);
    Assert.Equal(WorkflowStatus.Completed, status);
}
```

## Future Enhancements

Potential improvements:

1. **Graph Composition:** Compose graphs from smaller reusable sub-graphs
2. **Dynamic Routing:** Runtime graph selection based on ticket type
3. **Parallel Graphs:** Run multiple graphs concurrently
4. **Graph Versioning:** Support multiple graph versions for A/B testing
5. **Visual Designer:** UI for building graphs without code
6. **Rollback Support:** Undo operations if graph fails mid-execution
7. **Monitoring Dashboard:** Real-time graph execution visualization

## Related Documentation

- [Microsoft Agent Framework Integration Plan](/home/user/PRFactory/docs/architecture/microsoft-agent-framework-integration-plan.md)
- [Implementation Plan](/home/user/PRFactory/docs/architecture/implementation-plan-agent-framework.md)
- [Architecture Overview](/home/user/PRFactory/docs/architecture/overview.md)

## File Structure

```
/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/
├── Base/
│   ├── IAgentGraph.cs                 - Graph interface and base types
│   └── AgentGraphBase.cs              - Base graph implementation
├── Messages/
│   └── AgentMessages.cs               - All message type definitions
└── Graphs/
    ├── RefinementGraph.cs             - Ticket refinement workflow
    ├── PlanningGraph.cs               - Plan generation workflow
    ├── ImplementationGraph.cs         - Code implementation workflow
    ├── WorkflowOrchestrator.cs        - Main orchestrator
    ├── GraphBuilder.cs                - Fluent graph builder
    └── README.md                      - This file
```
