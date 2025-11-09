# PRFactory Workflow Execution Architecture

## Executive Summary

PRFactory uses a **distributed, multi-process architecture** with three separate services that coordinate through a shared database:

1. **PRFactory.Web** (Blazor Server) - UI and synchronous operations
2. **PRFactory.Api** (REST API) - Webhook ingestion and external integrations  
3. **PRFactory.Worker** (Background Service) - Asynchronous workflow execution

Workflows are **not** executed in a single monolithic process. Instead, they run as background jobs in the dedicated Worker service, triggered by the API or Web UI, persisted in the database, and executed via a polling-based queue system.

---

## 1. Workflow Execution Architecture

### 1.1 Three-Service Model

```
┌─────────────────────────────────────────────────────────────────┐
│                      Shared Database                             │
│  (Tickets, Repositories, Workflows, Checkpoints, Execution Queues)│
└─────────────────────────────────────────────────────────────────┘
  ▲                           ▲                           ▲
  │                           │                           │
  │                           │                           │
┌─────────────────┐  ┌──────────────────┐  ┌───────────────────┐
│ PRFactory.Web   │  │  PRFactory.Api   │  │ PRFactory.Worker  │
│ (Blazor Server) │  │   (REST API)     │  │ (Background Svcs) │
│                 │  │                  │  │                   │
│ - Displays UI   │  │ - Receives       │  │ - Polls queue     │
│ - Triggers via  │  │   webhooks from  │  │ - Executes graphs │
│   UI buttons    │  │   Jira/GitHub    │  │ - Manages retries │
│ - Shows status  │  │ - Routes to      │  │ - Resumes from    │
│ - Approves/    │  │   Worker via DB  │  │   checkpoints     │
│   rejects plans │  │ - Returns 200    │  │ - Graceful        │
│                 │  │   immediately    │  │   shutdown        │
└─────────────────┘  └──────────────────┘  └───────────────────┘
```

### 1.2 Component Responsibilities

#### **PRFactory.Web (Blazor Server)**
- **Role**: User interface and synchronous operations
- **Execution**: Runs in browser with SignalR connection to server
- **Workflow Trigger**: Can trigger workflows via buttons (TBD - needs implementation)
- **Workflow Status**: Displays real-time status via SignalR hub
- **File**: `/home/user/PRFactory/src/PRFactory.Web/Program.cs`
- **Key Service**: `TicketService` in `/PRFactory.Web/Services/`
- **Limitation**: Does NOT execute workflows directly (uses Worker service)

```csharp
// Web UI is a facade - it injects application services directly
// but does NOT execute long-running workflows
public class TicketService : ITicketService
{
    // Direct injection of application services (Blazor Server pattern)
    private readonly ITicketApplicationService _ticketApplicationService;
    private readonly ITicketUpdateService _ticketUpdateService;
    
    // Does NOT make HTTP calls to /api/tickets/ endpoint
    // Instead injects services directly
}
```

#### **PRFactory.Api (REST API)**
- **Role**: External webhook ingestion and API access
- **Main Function**: Receives webhooks from Jira/GitHub and queues workflows
- **File**: `/home/user/PRFactory/src/PRFactory.Api/Program.cs`
- **Webhook Endpoint**: `POST /api/webhooks/jira`
- **Controller**: `/home/user/PRFactory/src/PRFactory.Api/Controllers/WebhookController.cs`
- **Execution Model**:
  ```csharp
  // Webhook received → Return 200 immediately → Queue in database
  [HttpPost("jira")]
  public async Task<IActionResult> ReceiveJiraWebhook([FromBody] JiraWebhookPayload payload)
  {
      // 1. Validate payload
      // 2. Determine workflow type
      // 3. Queue workflow (stores in database)
      // 4. Return 200 OK immediately
      // 5. Worker picks it up from queue
      
      await QueueWorkflowAsync(payload, workflowType, activityId);
      return Ok(new WebhookResponse { ... });
  }
  ```

#### **PRFactory.Worker (Background Service)**
- **Role**: Executes workflows asynchronously in background
- **Model**: Polling-based job queue (NOT message broker)
- **File**: `/home/user/PRFactory/src/PRFactory.Worker/Program.cs`
- **Host Service**: `AgentHostService` in `/home/user/PRFactory/src/PRFactory.Worker/AgentHostService.cs`
- **Resume Handler**: `WorkflowResumeHandler` in `/home/user/PRFactory/src/PRFactory.Worker/WorkflowResumeHandler.cs`
- **Execution Model**: Runs as Windows Service or Linux systemd service
- **Polling Pattern**:
  ```csharp
  // Continuous polling loop
  while (!stoppingToken.IsCancellationRequested)
  {
      // 1. Poll database for pending executions
      var pendingExecutions = await executionQueue
          .GetPendingExecutionsAsync(batchSize, cancellationToken);
      
      // 2. Process each execution
      foreach (var execution in pendingExecutions)
      {
          await ProcessWorkflowExecutionAsync(execution, cancellationToken);
      }
      
      // 3. Poll for suspended workflows needing resume
      var suspendedWorkflows = await executionQueue
          .GetSuspendedWorkflowsWithEventsAsync(batchSize, cancellationToken);
      
      // 4. Resume each workflow
      foreach (var workflow in suspendedWorkflows)
      {
          await ResumeWorkflowAsync(workflow, cancellationToken);
      }
      
      // 5. Wait before next poll (default: 5 seconds)
      await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
  }
  ```

---

## 2. Workflow Triggering Flow

### 2.1 Webhook-Based Trigger (Jira Comment)

```
Jira Ticket
  ├─ User adds comment "@prfactory solve this"
  │
  └─> Jira Webhook
        │
        ├─> POST /api/webhooks/jira
        │
        ├─> WebhookController validates & routes
        │
        ├─> Create/Update Ticket in database
        │
        ├─> Create AgentExecutionRequest in database
        │   (Status: Pending, WorkflowType: "trigger", etc.)
        │
        ├─> Return 200 OK immediately
        │
        └─> PRFactory.Worker polling (5-second interval)
              │
              └─> AgentHostService.PollAndExecuteWorkflowsAsync()
                    │
                    ├─> GetPendingExecutionsAsync() finds new execution
                    │
                    └─> ProcessWorkflowExecutionAsync(execution)
                          │
                          └─> graphExecutor.ExecuteGraphAsync("Refinement", ...)
                                │
                                └─> Runs RefinementGraph → Questions posted to Jira
                                      │
                                      └─> Graph suspends at HumanWaitAgent
                                            │
                                            └─> SaveCheckpoint() in database
```

### 2.2 Web UI Trigger (Future - Not Yet Implemented)

```
User clicks "Start Workflow" button in PRFactory Web UI
  │
  ├─> Blazor component calls TicketService.StartWorkflowAsync()
  │
  ├─> TicketService calls ITicketApplicationService (direct injection)
  │
  ├─> Application service creates:
  │   - Ticket entity
  │   - AgentExecutionRequest queued in database
  │
  ├─> Returns control immediately to Blazor
  │
  └─> SignalR hub subscribes to workflow events
        │
        └─> Receives real-time updates as Worker executes
```

---

## 3. Workflow Execution Model: Graphs

### 3.1 Graph Architecture

The Worker executes **three specialized graphs** that handle different workflow phases:

```
TriggerTicketMessage
  │
  └─> WorkflowOrchestrator.StartWorkflowAsync()
        │
        ├─> Phase 1: RefinementGraph
        │   ├─> TriggerAgent
        │   ├─> RepositoryCloneAgent
        │   ├─> AnalysisAgent (3x retry on failure)
        │   ├─> QuestionGenerationAgent
        │   ├─> JiraPostAgent
        │   └─> HumanWaitAgent [SUSPEND]
        │         │
        │         └─> [Webhook resume with AnswersReceivedMessage]
        │
        ├─> AnswerProcessingAgent (resume)
        │
        └─> [If refinement complete]
              │
              └─> Phase 2: PlanningGraph
                  ├─> PlanningAgent
                  ├─> GitPlanAgent + JiraPostAgent (PARALLEL)
                  └─> HumanWaitAgent [SUSPEND]
                        │
                        ├─> [Webhook resume with PlanApprovedMessage]
                        │   └─> Phase 3: ImplementationGraph
                        │       ├─> ImplementationAgent
                        │       ├─> GitCommitAgent
                        │       ├─> PullRequestAgent + JiraPostAgent (PARALLEL)
                        │       └─> CompletionAgent
                        │
                        └─> [Webhook resume with PlanRejectedMessage]
                            └─> Loop back to PlanningAgent (max 5 retries)
```

### 3.2 Graph Execution Mechanism

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/`

**Key Files**:
- `RefinementGraph.cs` - Phase 1: Analysis & questions
- `PlanningGraph.cs` - Phase 2: Plan generation & approval
- `ImplementationGraph.cs` - Phase 3: Code implementation (optional)
- `WorkflowOrchestrator.cs` - Coordinates all three graphs
- `AgentExecutor.cs` - Executes individual agents from DI container
- `GraphBuilder.cs` - Fluent API for building custom graphs

**Base Class**: `AgentGraphBase` in `/Base/AgentGraphBase.cs`

### 3.3 Agent Execution Flow

```csharp
// 1. Graph calls agent executor
var currentMessage = await ExecuteAgentAsync<TriggerAgent>(
    inputMessage, context, "trigger", cancellationToken);

// 2. AgentExecutor resolves agent from DI
// Uses type map: "TriggerAgent" -> typeof(TriggerAgent)
var agent = _serviceProvider.GetService(typeof(TriggerAgent)) as BaseAgent;

// 3. Agent processes message
var agentContext = CreateAgentContextFromMessage(inputMessage);
var outputMessage = await agent.ExecuteAsync(agentContext, cancellationToken);

// 4. Graph checkpoints result
await SaveCheckpointAsync(context, "trigger_complete", "TriggerAgent");

// 5. Returns to graph for next agent
```

---

## 4. Workflow Suspension & Resumption

### 4.1 Suspension Points

Workflows **suspend** (pause) at human interaction points:

| Graph | Suspension Point | Wait Reason | Resume With |
|-------|------------------|------------|-------------|
| **RefinementGraph** | `HumanWaitAgent` | Waiting for user to answer clarifying questions | `AnswersReceivedMessage` |
| **PlanningGraph** | `HumanWaitAgent` | Waiting for user to approve/reject plan | `PlanApprovedMessage` or `PlanRejectedMessage` |
| **ImplementationGraph** | None | Runs to completion (optional) | N/A |

### 4.2 Checkpoint System

When a graph suspends:

```csharp
// 1. Graph saves checkpoint with current state
await SaveCheckpointAsync(context, "awaiting_answers", "HumanWaitAgent");

// 2. Checkpoint stored in database:
// - CheckpointId (Guid)
// - TicketId (Guid)
// - State (Dictionary<string, object>)
// - NextAgentType ("HumanWaitAgent")
// - SavedAt (DateTime.UtcNow)
// - GraphId ("RefinementGraph")

// 3. Returns GraphExecutionResult.Suspended()
return GraphExecutionResult.Suspended("awaiting_answers", currentMessage);
```

**Checkpoint Storage**: `ICheckpointStore` interface in `/Agents/IAgentWorkflowInterfaces.cs`
**Implementation**: `GraphCheckpointStoreAdapter` in `/Agents/Adapters/GraphCheckpointStoreAdapter.cs`
**Persistence**: EF Core repository `CheckpointRepository` in `/Persistence/Repositories/`

### 4.3 Resume Flow via Webhook

```
Jira Webhook (comment: "Q1: Answer 1\nQ2: Answer 2")
  │
  ├─> POST /api/webhooks/jira
  │
  ├─> WebhookController routes to resume handler
  │
  ├─> WorkflowResumeHandler.ValidateAndCreateResumeMessageAsync()
  │   ├─> Parse comment to extract answers
  │   ├─> Create AnswersReceivedMessage
  │   └─> Store in SuspendedWorkflow queue in database
  │
  ├─> Return 200 OK
  │
  └─> PRFactory.Worker polling (5-second interval)
        │
        └─> AgentHostService.PollAndExecuteWorkflowsAsync()
              │
              ├─> GetSuspendedWorkflowsWithEventsAsync() finds workflow with resume message
              │
              └─> ResumeWorkflowAsync(suspendedWorkflow)
                    │
                    ├─> Load checkpoint from database
                    │
                    ├─> Validate checkpoint is HumanWait
                    │
                    ├─> Create AgentContext from checkpoint state
                    │
                    └─> Call appropriate graph's ResumeAsync()
                          │
                          └─> AnswerProcessingAgent continues from checkpoint
```

**Resume Handler**: `/home/user/PRFactory/src/PRFactory.Worker/WorkflowResumeHandler.cs`

---

## 5. Queue & Execution Model

### 5.1 Execution Queue Interface

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/IAgentWorkflowInterfaces.cs`

```csharp
public interface IAgentExecutionQueue
{
    // Get pending NEW executions to start
    Task<List<AgentExecutionRequest>> GetPendingExecutionsAsync(
        int batchSize, CancellationToken cancellationToken);
    
    // Get suspended workflows with resume messages
    Task<List<SuspendedWorkflow>> GetSuspendedWorkflowsWithEventsAsync(
        int batchSize, CancellationToken cancellationToken);
    
    // Mark execution as complete
    Task MarkExecutionCompletedAsync(
        Guid executionId, WorkflowExecutionResult result, CancellationToken cancellationToken);
    
    // Mark execution as permanently failed
    Task MarkExecutionFailedAsync(
        Guid executionId, string error, CancellationToken cancellationToken);
    
    // Schedule retry with backoff
    Task ScheduleRetryAsync(
        AgentExecutionRequest execution, CancellationToken cancellationToken);
    
    // Similar methods for workflow resumption
}
```

### 5.2 Polling Model (NOT Message Broker)

PRFactory uses **database polling**, NOT a message broker (RabbitMQ, AWS SQS, etc.):

**Advantages**:
- ✅ No external dependencies (RabbitMQ, Kafka, etc.)
- ✅ Simple single-database deployment model
- ✅ Automatic deduplication (database transaction)
- ✅ Easy debugging (query the database directly)
- ✅ Graceful shutdown (wait for semaphore)

**Disadvantages**:
- ⚠️ Latency: 5-second polling interval (vs millisecond message delivery)
- ⚠️ Database load: Continuous polling queries
- ⚠️ Limited scalability for very high throughput

**Configuration** (from `AgentHostOptions`):
```csharp
public int MaxConcurrentExecutions { get; set; } = 10;        // Max parallel workflows
public int PollIntervalSeconds { get; set; } = 5;              // Poll frequency
public int BatchSize { get; set; } = 20;                        // Executions per poll
public int MaxRetries { get; set; } = 3;                        // Retry attempts
public int RetryDelayBaseSeconds { get; set; } = 30;            // Exponential backoff base
public int GracefulShutdownTimeoutSeconds { get; set; } = 300;  // 5-minute shutdown grace period
```

---

## 6. Developer Machine Integration

### 6.1 CLI Agent Execution

Some agents delegate to CLI tools (Claude Desktop, Codex) running on developer machines:

**Architecture**:
```
PRFactory.Worker (running on server)
  │
  ├─> Agent needs AI analysis
  │
  └─> ICliAgent abstraction
        │
        ├─> ClaudeDesktopCliAdapter
        │   └─> ProcessExecutor.ExecuteAsync("claude --headless --prompt '...'")
        │       └─> Spawns subprocess on local machine
        │           (Claude CLI must be installed locally)
        │
        └─> CodexCliAdapter
            └─> ProcessExecutor.ExecuteAsync("codex --prompt '...'")
                └─> Spawns subprocess on local machine
```

**Key Files**:
- Adapter: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Adapters/ClaudeDesktopCliAdapter.cs`
- Executor: `/home/user/PRFactory/src/PRFactory.Infrastructure/Execution/ProcessExecutor.cs`

**Configuration**:
```csharp
// In appsettings.json
"ClaudeDesktopCli": {
    "CommandPath": "claude",              // Where to find 'claude' command
    "SupportsHeadless": true,             // CLI supports --headless flag
    "TimeoutSeconds": 900,                // 15-minute timeout
    "MaxConcurrentRequests": 5            // Rate limiting
}
```

### 6.2 Process Execution Safety

`ProcessExecutor` provides safe subprocess management:

```csharp
public class ProcessExecutor : IProcessExecutor
{
    public async Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        // ✅ Safe: No shell execution (UseShellExecute = false)
        // ✅ Safe: Proper argument escaping (ArgumentList)
        // ✅ Safe: Timeout support prevents hangs
        // ✅ Safe: Cancellation token support
        // ✅ Safe: Full output/error capture
        
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,        // KEY: No shell injection possible
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };
        
        // Execute with timeout and cancellation
        return await ExecuteCoreAsync(startInfo, timeoutSeconds, cancellationToken);
    }
}
```

### 6.3 Workspace Isolation

Each tenant/workflow gets isolated workspace:

```
/workspaces/
├── <TenantId>/
│   ├── <RepositoryId>/
│   │   ├── .git/                    # LibGit2Sharp operations
│   │   ├── src/                     # Cloned repository
│   │   ├── .claude/                 # Claude project config
│   │   └── checkpoints/             # Workflow checkpoints
│   └── <RepositoryId2>/
└── <TenantId2>/
```

**Isolation Guarantees**:
- Database queries filtered by `TenantId` (EF Core global filters)
- Workspace directories isolated per tenant
- File system permissions enforced per tenant
- No cross-tenant data access possible

---

## 7. Deployment Architecture

### 7.1 Single Process Mode (Development)

For simplicity in dev, Web + Worker can run in same process:

```csharp
// In PRFactory.Api Program.cs (or Web Program.cs)
builder.Services.AddHostedService<AgentHostService>();

// Then all three services in one executable:
// - Web (Blazor Server) on port 5000
// - API (REST) on port 5001
// - Worker (BackgroundService) running continuously
```

**Advantages**: Simple local development, single deployment
**Disadvantages**: Workflow execution blocks web requests if semaphore full

### 7.2 Separate Process Mode (Recommended Production)

Three independent deployments:

```
+-------------------+  ┌────────────────┐  ┌──────────────────┐
│ PRFactory.Web:5000│  │ PRFactory.Api  │  │ PRFactory.Worker │
│ (Blazor Server)   │  │ :5001 (REST)   │  │ (Service)        │
└───────────────────┘  └────────────────┘  └──────────────────┘
        │                      │                      │
        └──────────────────────┼──────────────────────┘
                               │
                        ┌──────▼──────┐
                        │   Database  │
                        │  (SQLite)   │
                        └─────────────┘
```

**Windows Service** (PRFactory.Worker):
```powershell
sc.exe create "PRFactory Worker" binPath="C:\path\to\PRFactory.Worker.exe"
sc.exe start "PRFactory Worker"
```

**Linux systemd Service**:
```ini
[Unit]
Description=PRFactory Worker Service
After=network.target

[Service]
Type=notify
ExecStart=/usr/local/bin/prfactory-worker/PRFactory.Worker
WorkingDirectory=/usr/local/bin/prfactory-worker
Restart=always
RestartSec=10
User=prfactory
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### 7.3 Docker Deployment

Each service in separate container:

```dockerfile
# Dockerfile for Worker
FROM mcr.microsoft.com/dotnet/runtime:10 AS runtime
COPY --from=builder /app/publish /app
WORKDIR /app
ENTRYPOINT ["./PRFactory.Worker"]
```

**docker-compose.yml**:
```yaml
services:
  web:
    image: prfactory-web:latest
    ports: ["5000:5000"]
    depends_on: [db]
  
  api:
    image: prfactory-api:latest
    ports: ["5001:5001"]
    depends_on: [db]
  
  worker:
    image: prfactory-worker:latest
    depends_on: [db]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;...
  
  db:
    image: sqlite:latest
    volumes:
      - ./data:/data
```

---

## 8. Current Implementation Status

### 8.1 Fully Implemented ✅

- ✅ **AgentHostService** - Polling loop, concurrency control, graceful shutdown
- ✅ **RefinementGraph** - Full 3-phase workflow with retry logic
- ✅ **PlanningGraph** - Parallel execution, rejection loop (max 5 retries)
- ✅ **ImplementationGraph** - Conditional execution based on tenant config
- ✅ **WorkflowOrchestrator** - Graph coordination and state management
- ✅ **AgentExecutor** - Agent resolution from DI and execution
- ✅ **CheckpointStore** - Persistence via EF Core repositories
- ✅ **ProcessExecutor** - Safe subprocess execution with timeouts
- ✅ **ClaudeDesktopCliAdapter** - Local CLI invocation
- ✅ **GraphBuilder** - Fluent API for custom graphs

### 8.2 Partially Implemented ⚠️

- ⚠️ **WorkflowResumeHandler** - Framework in place but `ResumeFromCheckpoint` not fully implemented (line 138-147)
- ⚠️ **IAgentExecutionQueue** - Interface defined but implementation not registered in DI
- ⚠️ **WebhookController** - Routes determined but `QueueWorkflowAsync()` is stubbed (line 148)
- ⚠️ **Web UI Triggers** - UI can display tickets but cannot trigger workflows directly

### 8.3 Not Yet Implemented ❌

- ❌ **Complete resume checkpoint restoration** - Checkpoint loads but graph execution resumes via todo
- ❌ **Message broker alternative** - Only database polling; no RabbitMQ/Kafka integration
- ❌ **Distributed execution** - All workflows must execute on single Worker service
- ❌ **Priority queues** - All workflows same priority (FIFO)
- ❌ **Workflow scheduling** - No cron-based triggers
- ❌ **Dead letter queue** - Failed workflows marked but not recoverable
- ❌ **Real-time progress WebSockets** - Status updates via polling, not push

---

## 9. Process Model Summary

### 9.1 What Runs Where

| Component | Location | Process | Model |
|-----------|----------|---------|-------|
| **Blazor UI** | Browser + PRFactory.Web Server | Web Server | Request/Response + SignalR |
| **Graph Execution** | PRFactory.Worker | Worker Service | Async polling queue |
| **Webhook Ingestion** | PRFactory.Api | API Server | HTTP endpoint |
| **Database** | Shared SQLite | Database | Shared ACID transactions |
| **Local CLI** | Developer machine | Subprocess | Process.Start() calls |

### 9.2 Data Flow for Jira-Triggered Workflow

```
1. User comments on Jira ticket
   ↓
2. Jira sends webhook to POST /api/webhooks/jira
   ↓
3. PRFactory.Api receives, validates, creates Ticket & AgentExecutionRequest in database
   ↓
4. API returns 200 OK immediately (non-blocking)
   ↓
5. PRFactory.Worker polls database every 5 seconds
   ↓
6. Worker finds pending AgentExecutionRequest
   ↓
7. Worker executes RefinementGraph on separate thread (up to 10 concurrent)
   ↓
8. Graph runs through agents: Trigger → Clone → Analysis → Questions → Post
   ↓
9. Graph reaches HumanWaitAgent and suspends (saves checkpoint)
   ↓
10. Web UI polls for status updates
    ↓
11. User reads questions on Jira, adds comment with answers
    ↓
12. Jira sends webhook again with comment data
    ↓
13. PRFactory.Api extracts answers, creates resume message in SuspendedWorkflow queue
    ↓
14. Worker finds suspended workflow on next poll
    ↓
15. Worker loads checkpoint, resumes from suspension point
    ↓
16. AnswerProcessingAgent continues → Plans generated → Posted to Jira
    ↓
17. User approves plan in Jira comment
    ↓
18. Worker resumes PlanningGraph with approval message
    ↓
19. ImplementationGraph runs (if auto-implement enabled)
    ↓
20. Pull request created, workflow completed
```

### 9.3 Concurrency Model

```
AgentHostService (single thread) runs polling loop:

┌─ Poll #1 (t=0s)
│  ├─ Find 3 pending executions
│  └─ Start 3 threads via ThreadPool (within semaphore limit of 10)
│     ├─ Thread 1: ProcessWorkflowExecutionAsync(execution1)
│     ├─ Thread 2: ProcessWorkflowExecutionAsync(execution2)
│     └─ Thread 3: ProcessWorkflowExecutionAsync(execution3)
│
├─ Poll #2 (t=5s)
│  ├─ 1 of 3 executions completed (semaphore slot released)
│  ├─ Find 2 more pending + 1 suspended with events
│  └─ Start 2 + 1 = 3 more threads
│     ├─ Thread 4: ProcessWorkflowExecutionAsync(execution4)
│     ├─ Thread 5: ProcessWorkflowExecutionAsync(execution5)
│     └─ Thread 6: ResumeWorkflowAsync(workflow1)
│
└─ [Continue polling until all work complete]

Max concurrent: 10 (configurable)
Graceful shutdown: Wait for all semaphore slots to become available
```

---

## 10. File Path Reference

### Core Execution Files

```
/home/user/PRFactory/src/

PRFactory.Worker/
├── Program.cs                    # Entry point, configures services
├── AgentHostService.cs           # Main polling loop (BackgroundService)
├── WorkflowResumeHandler.cs      # Resume suspension logic
├── appsettings.json              # Configuration
└── README.md                     # Worker documentation

PRFactory.Api/
├── Program.cs                    # REST API setup
├── Controllers/
│   └── WebhookController.cs      # Receives Jira webhooks
└── Models/
    └── JiraWebhookPayload.cs     # Webhook payload types

PRFactory.Web/
├── Program.cs                    # Blazor Server setup
└── Services/
    ├── TicketService.cs          # Facade for UI
    └── WorkflowEventService.cs   # SignalR event broadcasting

PRFactory.Infrastructure/
├── DependencyInjection.cs        # Service registration
├── Agents/
│   ├── IAgentWorkflowInterfaces.cs  # Queue & execution interfaces
│   ├── Graphs/
│   │   ├── RefinementGraph.cs       # Phase 1
│   │   ├── PlanningGraph.cs         # Phase 2
│   │   ├── ImplementationGraph.cs   # Phase 3
│   │   ├── WorkflowOrchestrator.cs  # Coordinator
│   │   ├── AgentExecutor.cs         # Agent execution
│   │   └── GraphBuilder.cs          # Custom graph DSL
│   ├── Adapters/
│   │   └── ClaudeDesktopCliAdapter.cs  # Local CLI wrapper
│   ├── Base/
│   │   ├── AgentGraphBase.cs        # Base class
│   │   └── CheckpointData.cs        # Checkpoint model
│   └── [individual agents]
├── Execution/
│   └── ProcessExecutor.cs        # Safe subprocess execution
└── Persistence/
    └── Repositories/
        ├── CheckpointRepository.cs  # Checkpoint persistence
        └── [other repositories]
```

---

## 11. Key Design Decisions

### 11.1 Why Polling Over Message Broker?

**Decision**: Database polling instead of RabbitMQ/Kafka

**Rationale**:
1. **Single database deployment** - No additional infrastructure
2. **Atomic operations** - ACID guarantees for workflow state
3. **Simple debugging** - Query database directly to see queue
4. **Perfect for human-in-the-loop** - Waiting for webhooks (5-second latency acceptable)
5. **Flexible suspension** - Checkpoints stored in database with full context

**Tradeoff**: 5-second polling latency vs millisecond message broker delivery

### 11.2 Why Three Separate Graphs?

**Decision**: RefinementGraph + PlanningGraph + ImplementationGraph

**Rationale**:
1. **Separation of concerns** - Each graph handles distinct workflow phase
2. **Independent evolution** - Graphs can change without affecting others
3. **Fault isolation** - Failure in one graph doesn't crash others
4. **Future extensibility** - Easy to add CodeReviewGraph, TestingGraph, etc.
5. **Clear suspension points** - Each graph knows where humans wait

### 11.3 Why Checkpoint Every Agent?

**Decision**: Save state after each agent completes

**Rationale**:
1. **Recovery granularity** - Can resume from any agent
2. **Error debugging** - See exact point of failure
3. **Human visibility** - Can track progress through UI
4. **Retry support** - Retry single agent without re-running all previous

### 11.4 Why Background Service (Not Library)?

**Decision**: Worker runs as separate Windows Service / systemd service

**Rationale**:
1. **Independent scaling** - Web and Worker can scale separately
2. **Lifecycle control** - Can restart/upgrade Worker without affecting Web
3. **Process isolation** - Worker crash doesn't affect Web API
4. **Graceful shutdown** - Service controller signals shutdown properly
5. **Monitoring** - Service status visible in OS (systemctl, Services.msc)

---

## 12. Integration Points for Developers

### 12.1 Adding a New Workflow Trigger

```csharp
// 1. Add to WebhookController.DetermineWorkflowType()
"custom_event" => WorkflowTypes.CustomWorkflow

// 2. In QueueWorkflowAsync(), create execution request
var executionRequest = new AgentExecutionRequest
{
    ExecutionId = Guid.NewGuid(),
    TicketId = ticket.Id,
    WorkflowType = "custom_workflow",
    InitialMessage = new CustomMessage(...),
    CreatedAt = DateTime.UtcNow,
    RetryCount = 0
};
await executionQueue.SaveAsync(executionRequest);

// 3. Worker automatically picks it up on next poll
```

### 12.2 Adding a New Graph

```csharp
// 1. Create new graph class
public class CustomGraph : AgentGraphBase
{
    protected override async Task<GraphExecutionResult> ExecuteCoreAsync(...)
    {
        // Add your agents here
    }
}

// 2. Register in WorkflowOrchestrator
var customGraph = serviceProvider.GetRequiredService<CustomGraph>();
if (workflowType == "custom")
{
    return await customGraph.ExecuteAsync(message, context, ct);
}

// 3. Register in DI
services.AddScoped<CustomGraph>();
```

### 12.3 Adding a New Agent

```csharp
// 1. Create agent inheriting from BaseAgent
public class MyAgent : BaseAgent
{
    public override async Task<IAgentMessage> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Your logic here
        return new MyOutputMessage(...);
    }
}

// 2. Register in DI
services.AddTransient<MyAgent>();

// 3. Add to AgentExecutor type map
_agentTypeMap["MyAgent"] = typeof(MyAgent);

// 4. Use in graph
currentMessage = await ExecuteAgentAsync<MyAgent>(
    currentMessage, context, "my_agent", cancellationToken);
```

---

## 13. Monitoring & Observability

### 13.1 Logs

**Worker Service Logs**: `/logs/worker-YYYY-MM-DD.log`

```
[10:23:45 INF] Agent Host Service starting. Max concurrent executions: 10, Poll interval: 5s
[10:23:50 INF] Found 1 pending workflow execution(s) to process
[10:23:50 INF] Starting workflow execution for ticket PROJ-123, type: refinement
[10:23:55 INF] Executing agent TriggerAgent for ticket 12345678-...
[10:24:00 INF] Workflow execution completed successfully for ticket PROJ-123
[10:24:05 INF] Found 1 suspended workflow(s) ready to resume
[10:24:10 INF] Resuming workflow for ticket PROJ-123 from agent HumanWaitAgent
```

### 13.2 Database Inspection

```sql
-- Check pending executions
SELECT * FROM AgentExecutionRequests WHERE Status = 'Pending';

-- Check suspended workflows
SELECT * FROM SuspendedWorkflows WHERE ResumeMessage IS NOT NULL;

-- Check checkpoint history
SELECT * FROM Checkpoints WHERE TicketId = 'ticket-guid' ORDER BY SavedAt DESC;

-- Check workflow status
SELECT * FROM Tickets WHERE Id = 'ticket-guid';
SELECT * FROM WorkflowStates WHERE TicketId = 'ticket-guid';
```

### 13.3 SignalR Events

Web UI receives real-time updates via SignalR:

```csharp
// Hub: TicketHub
public class TicketHub : Hub
{
    public async Task Subscribe(string ticketId)
    {
        await Groups.AddToGroupAsync(Connection.ConnectionId, $"ticket-{ticketId}");
    }
}

// Events broadcast:
await _ticketHub.Clients
    .Group($"ticket-{ticketId}")
    .SendAsync("WorkflowStatusChanged", new { Status = "executing", CurrentAgent = "AnalysisAgent" });
```

---

## 14. Known Limitations & TODOs

### 14.1 Incomplete Features

1. **Resume from checkpoint** - Framework exists but not fully wired
   - File: `WorkflowResumeHandler.cs` line 138-147
   - TODO: Implement `graphExecutor.ResumeFromCheckpointAsync()`

2. **Execution queue implementation** - Interface defined but no implementation registered
   - File: `IAgentWorkflowInterfaces.cs` line 50-87
   - TODO: Implement `AgentExecutionQueueRepository`
   - TODO: Register in `DependencyInjection.cs`

3. **Webhook queuing** - Not actually persisting to queue
   - File: `WebhookController.cs` line 146-162
   - TODO: Call `executionQueue.SaveAsync(executionRequest)`

### 14.2 Future Enhancements

- [ ] Message broker alternative (RabbitMQ, Kafka)
- [ ] Distributed Worker execution (horizontal scaling)
- [ ] Priority queue support
- [ ] Workflow scheduling (cron-based)
- [ ] Advanced retry policies (circuit breaker)
- [ ] Pause/resume via API
- [ ] Real-time WebSocket updates
- [ ] Dead letter queue

---

## Summary

PRFactory's workflow execution architecture is:

1. **Event-Driven**: Webhooks trigger workflows, no polling from external services
2. **Database-Backed**: Shared SQLite database coordinates Web, API, and Worker
3. **Polling-Based**: Worker polls database every 5 seconds for work (not message broker)
4. **Graph-Based**: Three specialized graphs handle different workflow phases
5. **Checkpoint-Based**: Workflows suspend/resume via database-persisted checkpoints
6. **Process-Isolated**: Separate Worker service enables independent scaling
7. **Human-in-Loop**: Long-running workflows suspend waiting for human approval
8. **Fault-Tolerant**: Automatic retries, graceful shutdown, error isolation

The architecture prioritizes **simplicity** (single database, no message broker) and **human-friendly workflows** (suspend/resume at approval points) over high-throughput asynchronous processing.

