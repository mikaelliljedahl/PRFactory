# PRFactory Workflow Execution - Critical Integration Gaps

## Overview

The workflow execution architecture is largely implemented, but **three critical pieces are incomplete**, preventing workflows from actually executing end-to-end. This document identifies these gaps and their locations.

---

## Critical Gap #1: Execution Queue Implementation

### Status: ❌ NOT IMPLEMENTED

### The Problem
The `IAgentExecutionQueue` interface exists and is called by `AgentHostService`, but **no implementation is registered in the dependency injection container**. This breaks the entire polling loop.

### Files Involved

**Interface Definition**:
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/IAgentWorkflowInterfaces.cs` (lines 50-87)

```csharp
public interface IAgentExecutionQueue
{
    Task<List<AgentExecutionRequest>> GetPendingExecutionsAsync(
        int batchSize, CancellationToken cancellationToken);
    
    Task<List<SuspendedWorkflow>> GetSuspendedWorkflowsWithEventsAsync(
        int batchSize, CancellationToken cancellationToken);
    
    Task MarkExecutionCompletedAsync(
        Guid executionId, WorkflowExecutionResult result, CancellationToken cancellationToken);
    
    Task MarkExecutionFailedAsync(
        Guid executionId, string error, CancellationToken cancellationToken);
    
    Task ScheduleRetryAsync(
        AgentExecutionRequest execution, CancellationToken cancellationToken);
    
    Task MarkWorkflowResumedAsync(
        Guid ticketId, WorkflowExecutionResult result, CancellationToken cancellationToken);
    
    Task MarkWorkflowResumeFailedAsync(
        Guid ticketId, string error, CancellationToken cancellationToken);
    
    Task ScheduleResumeRetryAsync(
        SuspendedWorkflow workflow, CancellationToken cancellationToken);
}
```

**Where It's Used**:
- `/home/user/PRFactory/src/PRFactory.Worker/AgentHostService.cs` (lines 79-80, 100-101, 176-177, etc.)

```csharp
// Line 79-80: Used in PollAndExecuteWorkflowsAsync
var executionQueue = scope.ServiceProvider
    .GetRequiredService<IAgentExecutionQueue>();

var pendingExecutions = await executionQueue
    .GetPendingExecutionsAsync(_options.BatchSize, cancellationToken);
```

**Missing Implementation**:
- No repository class like `AgentExecutionQueueRepository` exists
- Not registered in DI container (`DependencyInjection.cs`)

### What Needs to Be Done

1. **Create Implementation Class**:
```csharp
// File: PRFactory.Infrastructure/Persistence/Repositories/AgentExecutionQueueRepository.cs
public class AgentExecutionQueueRepository : IAgentExecutionQueue
{
    private readonly ApplicationDbContext _dbContext;
    
    public async Task<List<AgentExecutionRequest>> GetPendingExecutionsAsync(
        int batchSize, CancellationToken cancellationToken)
    {
        // Query database for:
        // - Status = "Pending" 
        // - NextRetryAt <= Now (or null for first attempt)
        // - RetryCount < MaxRetries
        // Order by CreatedAt (FIFO)
        // Limit to batchSize
    }
    
    public async Task<List<SuspendedWorkflow>> GetSuspendedWorkflowsWithEventsAsync(
        int batchSize, CancellationToken cancellationToken)
    {
        // Query database for:
        // - SuspendedWorkflows with ResumeMessage not null
        // - Order by SuspendedAt
        // - Limit to batchSize
    }
    
    public async Task MarkExecutionCompletedAsync(
        Guid executionId, WorkflowExecutionResult result, CancellationToken cancellationToken)
    {
        // Update status to "Completed"
        // Set CompletedAt timestamp
        // Store result metadata
    }
    
    // ... implement other methods
}
```

2. **Register in DI**:
```csharp
// File: PRFactory.Infrastructure/DependencyInjection.cs (in AddInfrastructure method)
services.AddScoped<IAgentExecutionQueue, AgentExecutionQueueRepository>();
```

### Impact Without This
- Worker starts but throws DependencyResolutionException
- Polling loop never executes
- No workflows can run
- System is completely non-functional

---

## Critical Gap #2: Webhook Queue Integration

### Status: ⚠️ PARTIALLY STUBBED

### The Problem
`WebhookController.QueueWorkflowAsync()` is called but doesn't actually queue the workflow. It's just a placeholder that logs.

### Files Involved

**Stubbed Method**:
- `/home/user/PRFactory/src/PRFactory.Api/Controllers/WebhookController.cs` (lines 146-162)

```csharp
private async Task QueueWorkflowAsync(
    JiraWebhookPayload payload, 
    string workflowType, 
    string activityId)
{
    // TODO: Implement agent orchestration service integration
    // For now, just log that we would queue the workflow
    _logger.LogInformation(
        "Workflow queued: Type={WorkflowType}, IssueKey={IssueKey}, ActivityId={ActivityId}",
        workflowType,
        payload.Issue?.Key,
        activityId);

    // In the real implementation, this would:
    // 1. Create/update Ticket entity in database
    // 2. Trigger appropriate agent graph based on workflowType
    // 3. Use Agent Framework's checkpoint mechanism for persistence

    await Task.CompletedTask;
}
```

### What Needs to Be Done

```csharp
private async Task QueueWorkflowAsync(
    JiraWebhookPayload payload, 
    string workflowType, 
    string activityId,
    CancellationToken cancellationToken = default)
{
    using var scope = _httpContextAccessor.HttpContext?.RequestServices.CreateScope()
        ?? throw new InvalidOperationException("Cannot create service scope");
    
    var executionQueue = scope.ServiceProvider
        .GetRequiredService<IAgentExecutionQueue>();
    
    var ticketRepository = scope.ServiceProvider
        .GetRequiredService<ITicketRepository>();
    
    var tenantRepository = scope.ServiceProvider
        .GetRequiredService<ITenantRepository>();
    
    // 1. Get or create Ticket entity
    var ticketKey = payload.Issue.Key;
    var ticket = await ticketRepository.GetByKeyAsync(
        ticketKey, cancellationToken);
    
    if (ticket == null)
    {
        // Create new ticket
        ticket = new Ticket
        {
            TicketKey = ticketKey,
            Title = payload.Issue.Summary,
            Description = payload.Issue.Description,
            Source = TicketSource.Jira,
            State = WorkflowState.New,
            CreatedAt = DateTime.UtcNow
        };
        
        await ticketRepository.AddAsync(ticket, cancellationToken);
        await ticketRepository.SaveChangesAsync(cancellationToken);
    }
    
    // 2. Create AgentExecutionRequest
    var executionRequest = new AgentExecutionRequest
    {
        ExecutionId = Guid.NewGuid(),
        TicketId = ticket.Id,
        WorkflowType = workflowType,
        InitialMessage = CreateInitialMessage(payload, workflowType),
        CreatedAt = DateTime.UtcNow,
        RetryCount = 0
    };
    
    // 3. Queue for execution
    await executionQueue.SaveAsync(executionRequest, cancellationToken);
    
    _logger.LogInformation(
        "Workflow queued successfully: Type={WorkflowType}, TicketId={TicketId}, IssueKey={IssueKey}",
        workflowType, ticket.Id, ticketKey);
}
```

### Impact Without This
- Webhooks are received and return 200 OK
- But nothing is queued in the database
- Worker finds no pending executions
- Workflows never start despite webhook being sent

---

## Critical Gap #3: Resume from Checkpoint

### Status: ⚠️ FRAMEWORK EXISTS, NOT WIRED

### The Problem
`WorkflowResumeHandler.ResumeWorkflowAsync()` loads the checkpoint but doesn't actually resume graph execution. It throws NotImplementedException.

### Files Involved

**Incomplete Method**:
- `/home/user/PRFactory/src/PRFactory.Worker/WorkflowResumeHandler.cs` (lines 138-147)

```csharp
// 5. Create agent context from checkpoint
var agentContext = CreateAgentContextFromCheckpoint(checkpoint, ticket);

// 6. Resume graph execution from the next agent
// TODO: Implement ResumeFromCheckpoint functionality in IAgentGraphExecutor
_logger.LogWarning(
    "Resume from checkpoint not yet implemented for ticket {TicketId}",
    workflow.TicketId);

return new WorkflowExecutionResult
{
    IsSuccess = false,
    Message = "ResumeFromCheckpoint functionality not yet implemented"
};
```

### Supporting Interface

**In `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/IAgentWorkflowInterfaces.cs`**:

```csharp
public interface IAgentGraphExecutor
{
    Task<WorkflowExecutionResult> ExecuteGraphAsync(
        string workflowType,
        IAgentMessage initialMessage,
        CancellationToken cancellationToken);
    
    // ❌ MISSING: ResumeFromCheckpointAsync
    // Should be:
    // Task<WorkflowExecutionResult> ResumeFromCheckpointAsync(
    //     string graphId,
    //     CheckpointData checkpoint,
    //     IAgentMessage resumeMessage,
    //     CancellationToken cancellationToken);
}
```

### What Needs to Be Done

1. **Add method to IAgentGraphExecutor interface**:
```csharp
public interface IAgentGraphExecutor
{
    Task<WorkflowExecutionResult> ExecuteGraphAsync(
        string workflowType,
        IAgentMessage initialMessage,
        CancellationToken cancellationToken);
    
    // NEW METHOD
    Task<WorkflowExecutionResult> ResumeFromCheckpointAsync(
        string graphId,
        CheckpointData checkpoint,
        IAgentMessage resumeMessage,
        CancellationToken cancellationToken);
}
```

2. **Implement in AgentExecutor**:
```csharp
public class AgentExecutor : IAgentExecutor
{
    // ... existing code ...
    
    public async Task<WorkflowExecutionResult> ResumeFromCheckpointAsync(
        string graphId,
        CheckpointData checkpoint,
        IAgentMessage resumeMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resuming graph {GraphId} from checkpoint {CheckpointId}",
            graphId, checkpoint.CheckpointId);
        
        // 1. Resolve the graph by GraphId
        var graph = graphId switch
        {
            "RefinementGraph" => _serviceProvider.GetRequiredService<RefinementGraph>(),
            "PlanningGraph" => _serviceProvider.GetRequiredService<PlanningGraph>(),
            "ImplementationGraph" => _serviceProvider.GetRequiredService<ImplementationGraph>(),
            _ => throw new InvalidOperationException($"Unknown graph: {graphId}")
        };
        
        // 2. Recreate GraphContext from checkpoint
        var context = new GraphContext
        {
            TicketId = checkpoint.TicketId,
            GraphId = graphId,
            State = checkpoint.State,
            StartedAt = checkpoint.SavedAt,
            RetryCount = checkpoint.RetryCount,
            CurrentCheckpoint = checkpoint.CheckpointId.ToString()
        };
        
        // 3. Call graph's ResumeAsync
        return await graph.ResumeAsync(resumeMessage, context, cancellationToken);
    }
}
```

3. **Fix WorkflowResumeHandler to use it**:
```csharp
// In WorkflowResumeHandler.ResumeWorkflowAsync (around line 137)

// Before (broken):
// TODO: Implement ResumeFromCheckpoint functionality in IAgentGraphExecutor
_logger.LogWarning("Resume from checkpoint not yet implemented...");
return new WorkflowExecutionResult { IsSuccess = false, ... };

// After (fixed):
var result = await _graphExecutor.ResumeFromCheckpointAsync(
    checkpoint.GraphId,
    checkpoint,
    nextAgentName switch
    {
        "AnswerProcessingAgent" => new AnswersReceivedMessage(...),
        "PlanningAgent" => message,  // resumeMessage from webhook
        _ => throw new InvalidOperationException($"Cannot resume at {nextAgentName}")
    },
    cancellationToken);

return result;
```

### Impact Without This
- Suspended workflows are detected correctly
- Checkpoint is loaded successfully
- But execution never resumes
- User's answers/approvals are ignored
- Workflow appears stuck waiting forever

---

## Integration Dependency Chain

These three gaps form a dependency chain. **All three must be implemented for workflows to work**:

```
Jira Webhook arrives
  ↓
1. WebhookController.QueueWorkflowAsync() [GAP #2]
   Must create Ticket + AgentExecutionRequest in database
  ↓
2. Worker polls and finds pending execution
   AgentHostService calls IAgentExecutionQueue [GAP #1]
   Must return pending executions from database
  ↓
3. Worker executes RefinementGraph
   Graph posts questions and suspends
  ↓
User answers in Jira comment
  ↓
4. Webhook received again
   WorkflowResumeHandler creates resume message
   Stores SuspendedWorkflow in database
  ↓
5. Worker polls and finds suspended workflow
   AgentHostService calls IAgentExecutionQueue [GAP #1]
   Must return suspended workflows with resume messages
  ↓
6. Worker calls ResumeWorkflowAsync
   WorkflowResumeHandler loads checkpoint
   Calls IAgentGraphExecutor.ResumeFromCheckpointAsync() [GAP #3]
   Must resume graph from suspension point
  ↓
7. Graph continues execution...
```

**If any gap is unfilled, the chain breaks at that point.**

---

## Missing Database Schema

To support these implementations, the database schema needs:

```sql
-- Execution queue table
CREATE TABLE AgentExecutionRequests (
    ExecutionId PRIMARY KEY,
    TicketId FOREIGN KEY,
    WorkflowType,
    InitialMessage (JSON),
    Status (Pending, Running, Completed, Failed),
    RetryCount,
    LastError,
    LastErrorDetails,
    CreatedAt,
    LastAttemptAt,
    NextRetryAt
);

-- Suspended workflows table
CREATE TABLE SuspendedWorkflows (
    TicketId PRIMARY KEY,
    SuspendedAgentName,
    CheckpointId FOREIGN KEY,
    ResumeMessage (JSON),
    SuspendedAt,
    ResumeAttempts,
    LastResumeError,
    LastResumeAttemptAt
);

-- Checkpoints table (may already exist)
CREATE TABLE Checkpoints (
    CheckpointId PRIMARY KEY,
    TicketId FOREIGN KEY,
    GraphId,
    State (JSON),
    NextAgentType,
    SavedAt,
    RetryCount
);
```

---

## Priority for Implementation

### Highest Priority (Blocks Everything)
1. **IAgentExecutionQueue implementation** - Without this, Worker can't poll
2. **WebhookController.QueueWorkflowAsync()** - Without this, workflows never start

### High Priority (Blocks Resume)
3. **IAgentGraphExecutor.ResumeFromCheckpointAsync()** - Without this, suspended workflows can't resume

### Supporting Work
4. Database schema creation
5. Entity mappings (AgentExecutionRequest, SuspendedWorkflow)
6. Unit tests for each component

---

## Testing Strategy

Once implemented, test in this order:

1. **Queue Storage Test**
```csharp
// Create AgentExecutionRequest
// Store via IAgentExecutionQueue
// Query database directly
// Verify presence
```

2. **Webhook Integration Test**
```csharp
// Send webhook to /api/webhooks/jira
// Verify Ticket created in database
// Verify AgentExecutionRequest queued
```

3. **Polling Test**
```csharp
// Queue execution manually
// Start Worker
// Verify execution picked up and processed
```

4. **Suspension Test**
```csharp
// Run workflow
// Verify suspension at HumanWaitAgent
// Verify checkpoint saved
```

5. **Resume Test**
```csharp
// Create resume message
// Call ResumeWorkflowAsync
// Verify workflow continues from checkpoint
```

6. **End-to-End Test**
```csharp
// Send Jira webhook
// Wait for questions posted
// Send answer webhook
// Wait for plan posted
// Send approval webhook
// Wait for PR created
```

---

## Summary

The workflow execution architecture is **80% complete** but blocked by three specific gaps:

| Gap | Location | Status | Impact |
|-----|----------|--------|--------|
| IAgentExecutionQueue | Infrastructure/Agents/IAgentWorkflowInterfaces.cs + Persistence/Repositories | Missing | Worker can't poll - BLOCKING |
| WebhookController.QueueWorkflowAsync() | Api/Controllers/WebhookController.cs | Stubbed | Webhooks ignored - BLOCKING |
| ResumeFromCheckpoint | Agents/Graphs/ + WorkflowResumeHandler.cs | Missing | Resumes fail - BLOCKING |

All three **must be implemented** for end-to-end workflow execution to work.

---

## Document Location

- Quick Summary: `/home/user/PRFactory/WORKFLOW_EXECUTION_SUMMARY.md`
- Full Details: `/home/user/PRFactory/docs/WORKFLOW_EXECUTION_ARCHITECTURE.md`
- This Document: `/home/user/PRFactory/docs/WORKFLOW_EXECUTION_CRITICAL_GAPS.md`
