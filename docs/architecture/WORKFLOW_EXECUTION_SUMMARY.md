# PRFactory Workflow Execution Architecture - Quick Summary

## Key Findings

### 1. **Three-Service Architecture** (Distributed, NOT Monolithic)

```
Web (Blazor)  →  API (Webhooks)  →  Worker (Background Service)
     ↓                ↓                    ↓
     └────────────────┴────────────────────┘
              Shared SQLite Database
```

- **PRFactory.Web** - Blazor Server UI (port 5000)
- **PRFactory.Api** - REST API for webhooks (port 5001)
- **PRFactory.Worker** - Background service for workflow execution (polling-based)

All three services share a single SQLite database as coordination point.

### 2. **Workflow Execution Model: Polling Queue (NOT Message Broker)**

The Worker service continuously polls the database for work:

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    // 1. Poll for pending NEW executions
    var pending = await queue.GetPendingExecutionsAsync(batchSize);
    
    // 2. Poll for SUSPENDED workflows ready to resume
    var suspended = await queue.GetSuspendedWorkflowsWithEventsAsync(batchSize);
    
    // 3. Execute/resume with max 10 concurrent (configurable)
    // 4. Wait 5 seconds before next poll
}
```

**Advantages**: No RabbitMQ/Kafka needed, simple debugging, ACID transactions
**Disadvantage**: 5-second latency (acceptable for human-in-loop workflows)

### 3. **Three-Graph Orchestration** 

```
TriggerTicketMessage
  ↓
Phase 1: RefinementGraph
  → TriggerAgent → RepositoryClone → Analysis → Questions → Post → [SUSPEND]
  ↓ [Resume with answers]
  → AnswerProcessing
  ↓
Phase 2: PlanningGraph
  → PlanningAgent → [GitPlan + JiraPost in parallel] → [SUSPEND]
  ↓ [Resume with approval/rejection]
  → [If approved] Loop back or continue
  ↓
Phase 3: ImplementationGraph (optional)
  → Implementation → GitCommit → [PR + JiraPost in parallel] → Complete
```

### 4. **Suspension & Resumption via Checkpoints**

Workflows **suspend** at human interaction points:

| Graph | Suspends At | Resume With |
|-------|------------|-------------|
| Refinement | HumanWaitAgent | AnswersReceivedMessage |
| Planning | HumanWaitAgent | PlanApprovedMessage or PlanRejectedMessage |
| Implementation | None | (runs to completion) |

**Checkpoint Storage**: EF Core repositories persisting to database

### 5. **Webhook-Triggered Flow**

```
Jira Comment @prfactory
  ↓ Webhook
API receives → Validates → Creates Ticket & AgentExecutionRequest in DB → Returns 200 OK immediately
  ↓
Worker polls (5s interval) → Finds pending execution
  ↓
Executes RefinementGraph (up to 10 concurrent) → Posts questions to Jira → Suspends
  ↓
[Waiting for human...]
  ↓
User adds comment with answers
  ↓ Webhook
API receives → Creates resume message in SuspendedWorkflow queue
  ↓
Worker polls → Finds suspended workflow with resume message → Resumes from checkpoint
  ↓
Graph continues → Posts plan to Jira → Suspends for approval
  ↓
[Repeats for each suspension point]
```

### 6. **Developer Machine Integration**

Agents can invoke local CLI tools (Claude Desktop, Codex):

```
Worker Service (on server)
  → Agent needs AI analysis
  → ICliAgent abstraction
    → ClaudeCodeCliAdapter
      → ProcessExecutor.ExecuteAsync("claude --headless --prompt '...'")
        → Spawns subprocess on local machine
```

Uses **safe subprocess execution** (no shell injection, proper escaping, timeouts).

### 7. **Current Implementation Status**

✅ **Fully Implemented**:
- AgentHostService (polling loop, concurrency, graceful shutdown)
- RefinementGraph, PlanningGraph, ImplementationGraph
- WorkflowOrchestrator (graph coordination)
- AgentExecutor (DI-based agent resolution)
- CheckpointStore (EF Core persistence)
- ProcessExecutor (safe subprocess execution)

⚠️ **Partially Implemented**:
- WorkflowResumeHandler (framework exists but resume not wired)
- IAgentExecutionQueue (interface defined, implementation missing)
- WebhookController (stubbed QueueWorkflowAsync())
- Web UI workflow triggers (display only, no trigger logic)

❌ **Not Yet Implemented**:
- Resume from checkpoint (TODO in WorkflowResumeHandler.cs line 138-147)
- Message broker alternative (only database polling)
- Distributed worker execution (single worker instance)
- Priority queues, scheduling, dead letter queue

### 8. **Key Architectural Decisions**

| Decision | Why | Tradeoff |
|----------|-----|----------|
| **Database polling** | Single database, ACID, easy debugging | 5-second latency |
| **Three graphs** | Separation of concerns, extensible | More complex than monolithic |
| **Checkpoint per agent** | Fine-grained recovery, clear visibility | More DB writes |
| **Background service** | Independent scaling, lifecycle control | Requires separate deployment |
| **Suspend/resume** | Human-friendly (approval at checkpoints) | Workflow can pause for hours |

### 9. **File Paths (Essential)**

```
PRFactory.Worker/
├── Program.cs                           # Entry point
├── AgentHostService.cs                  # Polling loop ← Main execution engine
├── WorkflowResumeHandler.cs             # Resume logic (incomplete)
└── appsettings.json                     # Configuration (poll interval, max concurrent)

PRFactory.Api/
├── Program.cs                           # REST setup
└── Controllers/WebhookController.cs     # Webhook ingestion → Queue to DB

PRFactory.Web/
├── Program.cs                           # Blazor Server setup
└── Services/TicketService.cs            # UI facade

PRFactory.Infrastructure/
├── DependencyInjection.cs               # Service registration
├── Agents/
│   ├── IAgentWorkflowInterfaces.cs      # Queue interfaces (not implemented!)
│   ├── Graphs/
│   │   ├── RefinementGraph.cs           # Phase 1
│   │   ├── PlanningGraph.cs             # Phase 2
│   │   ├── ImplementationGraph.cs       # Phase 3
│   │   ├── WorkflowOrchestrator.cs      # Coordinator
│   │   └── AgentExecutor.cs             # Agent resolution & execution
│   └── Adapters/
│       └── ClaudeCodeCliAdapter.cs   # Local CLI wrapper
└── Execution/ProcessExecutor.cs         # Safe subprocess execution
```

### 10. **Deployment Options**

**Single Process** (Development):
```
One executable with Web + API + Worker (simple, but blocks web if workflow busy)
```

**Separate Processes** (Production - Recommended):
```
Three independent services:
  - PRFactory.Web (Blazor Server, port 5000)
  - PRFactory.Api (REST API, port 5001)
  - PRFactory.Worker (Background service, configurable load)
  
All connect to shared SQLite database
```

**Docker**:
```
Three containers (web, api, worker) + database container
Each can scale independently
```

### 11. **Concurrency Model**

```
Single polling thread creates multiple worker threads:

Max 10 concurrent workflows (configurable)
Semaphore tracks available slots
Graceful shutdown waits for all slots to become available (5-minute timeout)

Poll Loop:
  Poll #1 (0s) → Find 3 pending → Start 3 threads
  Poll #2 (5s) → 1 completed (slot released) → Find 2 more + 1 resume → Start 3 threads
  Poll #3 (10s) → Continue...
```

### 12. **What's Missing / TODOs**

Critical:
1. **IAgentExecutionQueue implementation** - Queue interface defined but no DB implementation
2. **WebhookController.QueueWorkflowAsync()** - Stubbed, doesn't actually queue
3. **WorkflowResumeHandler.ResumeFromCheckpoint()** - Checkpoint loads but resume not wired

Important:
4. Web UI workflow triggering
5. Complete integration of resume flow
6. Actual queue persistence layer

### 13. **Questions This Architecture Answers**

**Q: Where do workflows execute?**
A: In PRFactory.Worker background service, NOT in Web UI or API. Triggered by webhooks or UI, executed asynchronously in Worker.

**Q: How do they scale?**
A: Polling queue with max concurrent limit. Add more Worker instances for horizontal scaling (would need shared queue).

**Q: How long do they wait?**
A: Up to 5 seconds before Worker processes. Workflows can suspend for hours waiting for human approval.

**Q: How are they persisted?**
A: Checkpoints saved to database after each agent. Suspended workflows stored with resume message.

**Q: How are developer machines involved?**
A: Agents invoke local Claude/Codex CLI via ProcessExecutor. Output captured and fed to next agent.

**Q: Can workflows run distributed across machines?**
A: No, current architecture has single Worker. Database polling would need refactoring for message broker to support distributed workers.

---

## Document Location

Full detailed documentation: `/home/user/PRFactory/docs/WORKFLOW_EXECUTION_ARCHITECTURE.md`

Contains:
- 14 detailed sections
- Architectural diagrams (ASCII art)
- Code examples
- Integration points
- Deployment patterns
- Current status
- Known limitations
