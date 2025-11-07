# PRFactory Architecture

Comprehensive architecture documentation for the PRFactory system.

## Table of Contents

- [Executive Summary](#executive-summary)
- [System Overview](#system-overview)
- [Architecture Patterns](#architecture-patterns)
- [Component Architecture](#component-architecture)
- [Agent System](#agent-system)
- [Workflow State Machine](#workflow-state-machine)
- [Data Architecture](#data-architecture)
- [Integration Architecture](#integration-architecture)
- [Security Architecture](#security-architecture)
- [Deployment Architecture](#deployment-architecture)

## Executive Summary

PRFactory is a .NET 8-based system that automates the journey from Jira tickets to GitHub pull requests using Claude AI. The system follows Clean Architecture principles with a domain-driven design approach.

**Key Characteristics:**
- **Language**: C# 12 with .NET 8 (LTS)
- **Architecture Style**: Clean Architecture (Onion Architecture)
- **Design Pattern**: Domain-Driven Design (DDD)
- **Processing Model**: Event-driven with background job processing
- **Multi-tenancy**: Isolated tenant environments
- **Deployment**: Containerized (Docker) or traditional hosting (IIS/Azure)

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         External Systems                            │
│                                                                     │
│   ┌───────────┐        ┌───────────┐        ┌───────────┐          │
│   │   Jira    │        │  GitHub/  │        │  Claude   │          │
│   │   Cloud   │        │  GitLab   │        │    AI     │          │
│   └─────┬─────┘        └─────┬─────┘        └─────┬─────┘          │
│         │                    │                    │                │
└─────────┼────────────────────┼────────────────────┼────────────────┘
          │                    │                    │
          │ Webhooks           │ Git API            │ AI API
          │                    │                    │
┌─────────┴────────────────────┴────────────────────┴────────────────┐
│                      PRFactory System                              │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    API Layer (ASP.NET Core)                 │   │
│  │  ┌──────────────────┐  ┌──────────────────┐                 │   │
│  │  │ WebhookController│  │TicketController  │                 │   │
│  │  │  (Jira events)   │  │  (CRUD ops)      │                 │   │
│  │  └────────┬─────────┘  └────────┬─────────┘                 │   │
│  └───────────┼────────────────────────┼──────────────────────────┘   │
│              │                        │                            │
│  ┌───────────┴────────────────────────┴──────────────────────────┐   │
│  │              Application Services Layer                      │   │
│  │                                                               │   │
│  │  TicketService  │  WorkflowService  │  TenantService         │   │
│  │  RepositoryService  │  StateTransitionService                │   │
│  └───────────┬───────────────────────────────────────────────────┘   │
│              │                                                    │
│  ┌───────────┴───────────────────────────────────────────────────┐   │
│  │                    Domain Layer                              │   │
│  │                                                               │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │   │
│  │  │   Entities   │  │State Machine │  │Value Objects │        │   │
│  │  │ (Ticket,     │  │ (12 states,  │  │ (Jira keys,  │        │   │
│  │  │  Tenant,     │  │  validated   │  │  Git refs)   │        │   │
│  │  │  Repository) │  │  transitions)│  │              │        │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘        │   │
│  │                                                               │   │
│  │  ┌───────────────────────────────────────────────────────┐    │   │
│  │  │              Business Rules & Invariants              │    │   │
│  │  │  - Workflow state transitions                         │    │   │
│  │  │  - Multi-tenant isolation                             │    │   │
│  │  │  - Credential encryption                              │    │   │
│  │  └───────────────────────────────────────────────────────┘    │   │
│  └───────────┬───────────────────────────────────────────────────┘   │
│              │                                                    │
│  ┌───────────┴───────────────────────────────────────────────────┐   │
│  │              Infrastructure Layer                            │   │
│  │                                                               │   │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌────────────────┐   │   │
│  │  │  Jira   │  │   Git   │  │ Claude  │  │   Persistence  │   │   │
│  │  │ Client  │  │ Service │  │ Client  │  │   (EF Core)    │   │   │
│  │  └─────────┘  └─────────┘  └─────────┘  └────────────────┘   │   │
│  │                                                               │   │
│  │  ┌─────────────────────────────────────────────────────────┐  │   │
│  │  │         Repositories (ITicketRepository, etc.)          │  │   │
│  │  └─────────────────────────────────────────────────────────┘  │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │              Worker Service (Background Processing)         │   │
│  │                                                               │   │
│  │  ┌───────────────────────────────────────────────────────┐   │   │
│  │  │           14 Specialized Agent Types                  │   │   │
│  │  │                                                        │   │   │
│  │  │  TriggerAgent → AnalysisAgent → QuestionGeneration    │   │   │
│  │  │  → QuestionPosting → AnswerRetrieval → Planning       │   │   │
│  │  │  → PlanGeneration → PlanCommit → PlanPosting          │   │   │
│  │  │  → ApprovalCheck → Implementation → PullRequest       │   │   │
│  │  │  → CompletionAgent → ErrorHandling                    │   │   │
│  │  └───────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  │  Polls database for tickets → Executes agent workflows       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Runtime** | .NET 8 (LTS), C# 12 |
| **Web Framework** | ASP.NET Core 8.0 |
| **Database** | SQLite (dev), SQL Server / PostgreSQL (prod) |
| **ORM** | Entity Framework Core 8 |
| **AI Integration** | Anthropic SDK (Claude Sonnet 4.5) |
| **Git Operations** | LibGit2Sharp |
| **Git Platform APIs** | Octokit (GitHub), Azure DevOps SDK |
| **Resilience** | Polly 8.x (retry, circuit breaker) |
| **Logging** | Serilog 3.x (structured logging) |
| **Tracing** | OpenTelemetry with Jaeger exporter |
| **Containerization** | Docker, Docker Compose |
| **CI/CD** | GitHub Actions |

## Architecture Patterns

### 1. Clean Architecture (Onion Architecture)

The system follows Clean Architecture with dependency inversion:

```
┌─────────────────────────────────────────────┐
│         Infrastructure Layer                │  ← External Dependencies
│  (Jira, Git, Claude, Database, Logging)     │
└──────────────────┬──────────────────────────┘
                   │ depends on ↓
┌──────────────────┴──────────────────────────┐
│         Application Services Layer          │  ← Use Cases
│  (TicketService, WorkflowService, etc.)     │
└──────────────────┬──────────────────────────┘
                   │ depends on ↓
┌──────────────────┴──────────────────────────┐
│            Domain Layer                     │  ← Business Logic
│  (Entities, Value Objects, Interfaces)      │  (No external deps)
└─────────────────────────────────────────────┘
```

**Key Principles:**
- Domain layer has no external dependencies
- Dependencies point inward (Infrastructure → Application → Domain)
- Interfaces defined in Domain, implemented in Infrastructure
- Testable and maintainable

### 2. Domain-Driven Design (DDD)

**Entities** - Objects with identity that persist:
- `Ticket` - Core entity representing a Jira ticket workflow
- `Tenant` - Multi-tenant isolation (customer/organization)
- `Repository` - Git repository configuration
- `Checkpoint` - Workflow checkpoint state
- `AgentExecution` - Agent execution history

**Value Objects** - Immutable objects without identity:
- `JiraTicketKey` - Validated Jira ticket identifier (e.g., PROJ-123)
- `WorkflowState` - Enumeration with transition rules
- `GitBranch` - Branch name with naming conventions
- `PullRequestUrl` - Validated PR URL

**Aggregates**:
- `Ticket` is the aggregate root, controlling access to checkpoints and executions

**Domain Events** (future enhancement):
- `TicketTriggered`
- `QuestionsPosted`
- `PlanApproved`
- `PRCreated`

### 3. Agent Pattern

The system uses 14 specialized agents, each responsible for a single workflow step:

```
Agent Interface:
  Execute(Ticket ticket) → Result

Each agent:
  1. Retrieves necessary context
  2. Performs its specific task
  3. Updates ticket state
  4. Returns success/failure
```

**Benefits:**
- Single Responsibility Principle
- Easy to test individual steps
- Can retry failed steps independently
- Clear separation of concerns

### 4. State Machine Pattern

Workflow progression is governed by a state machine:

```csharp
public enum WorkflowState
{
    Triggered,          // Initial state
    Analyzing,          // Codebase analysis in progress
    QuestionsPosted,    // Waiting for user answers
    AnswersReceived,    // Answers collected
    Planning,           // Plan generation in progress
    PlanPosted,         // Plan waiting for approval
    PlanApproved,       // Plan approved, ready for implementation
    Implementing,       // Code implementation in progress
    PRCreated,          // Pull request created
    Completed,          // Workflow complete
    Failed,             // Error occurred
    Cancelled           // User cancelled
}
```

**Transition Rules** enforce valid state changes (defined in `WorkflowStateTransitions.cs`).

## Component Architecture

### 1. API Layer (`PRFactory.Api`)

**Responsibilities:**
- Expose REST endpoints for external systems
- Validate incoming webhooks (HMAC)
- Handle HTTP concerns (CORS, authentication)
- Serialize/deserialize requests

**Key Components:**
- `WebhookController` - Receives Jira webhooks
- `TicketController` - CRUD operations for tickets
- `TenantController` - Tenant management
- `RepositoryController` - Repository configuration

### 2. Domain Layer (`PRFactory.Domain`)

**Responsibilities:**
- Define business entities and rules
- Enforce invariants (e.g., state transition rules)
- Provide domain interfaces (repositories, services)
- No external dependencies

**Key Components:**
- `Entities/` - Ticket, Tenant, Repository, etc.
- `ValueObjects/` - JiraTicketKey, WorkflowState, etc.
- `Interfaces/` - ITicketRepository, IWorkflowEngine, etc.

### 3. Infrastructure Layer (`PRFactory.Infrastructure`)

**Responsibilities:**
- Implement domain interfaces
- Integrate with external systems
- Database access via EF Core
- Logging, caching, file I/O

**Key Subsystems:**

#### 3.1 Jira Integration
- `JiraClient` - HTTP client for Jira API (using Refit)
- `JiraWebhookValidator` - HMAC signature validation
- `JiraCommentService` - Post comments/questions/plans

#### 3.2 Git Integration
- `IGitPlatformProvider` - Abstraction for Git platforms
- `GitHubProvider` - GitHub API implementation (Octokit)
- `AzureDevOpsProvider` - Azure Repos implementation
- `GitLabProvider` - GitLab API implementation (future)
- `LocalGitService` - Clone, branch, commit, push (LibGit2Sharp)

#### 3.3 Claude AI Integration
- `ClaudeClient` - Anthropic SDK wrapper
- `PromptTemplates/` - Prompts for analysis, questions, planning
- `ContextBuilder` - Build context from codebase for Claude

#### 3.4 Persistence
- `ApplicationDbContext` - EF Core DbContext
- `Repositories/` - Repository implementations
- `Configurations/` - Entity configurations (fluent API)
- `Migrations/` - Database migrations
- `Encryption/` - Credential encryption service

### 4. Worker Service (`PRFactory.Worker`)

**Responsibilities:**
- Background job processing
- Poll database for tickets in appropriate states
- Execute agent workflows
- Checkpoint-based resumption (fault tolerance)

**Key Components:**
- `WorkflowOrchestrator` - Coordinates agent execution
- `AgentFactory` - Creates appropriate agent instances
- `CheckpointService` - Save/restore workflow state
- `14 Agent Implementations` - One per workflow step

## Agent System

The agent system is the heart of PRFactory's workflow execution.

### Agent Hierarchy

```
IAgent (interface)
  ├─ TriggerAgent           - Validates trigger and initializes ticket
  ├─ AnalysisAgent          - Clones repo and analyzes codebase
  ├─ QuestionGenerationAgent - Generates clarifying questions
  ├─ QuestionPostingAgent   - Posts questions to Jira
  ├─ AnswerRetrievalAgent   - Retrieves answers from Jira comments
  ├─ PlanningAgent          - Generates implementation plan
  ├─ PlanGenerationAgent    - Creates plan markdown files
  ├─ PlanCommitAgent        - Commits plan to feature branch
  ├─ PlanPostingAgent       - Posts plan summary to Jira
  ├─ ApprovalCheckAgent     - Checks for plan approval
  ├─ ImplementationAgent    - Implements code based on plan
  ├─ PullRequestAgent       - Creates pull request
  ├─ CompletionAgent        - Finalizes workflow
  └─ ErrorHandlingAgent     - Handles failures and retries
```

### Agent Execution Flow

```mermaid
sequenceDiagram
    participant Worker
    participant Orchestrator
    participant Agent
    participant Ticket
    participant External

    Worker->>Orchestrator: Process next ticket
    Orchestrator->>Ticket: Load ticket with state
    Orchestrator->>Agent: Create agent for current state
    Agent->>Ticket: Validate can execute
    Agent->>External: Perform action (Claude, Git, Jira)
    External-->>Agent: Result
    Agent->>Ticket: Update state and checkpoint
    Agent->>Orchestrator: Return result
    Orchestrator->>Worker: Continue or pause
```

### Checkpoint-Based Resumption

Each agent saves checkpoints before external operations:

```csharp
public class Checkpoint
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public WorkflowState State { get; set; }
    public string Data { get; set; } // JSON serialized context
    public DateTime CreatedAt { get; set; }
}
```

**Benefits:**
- Fault tolerance - resume from last checkpoint after crash
- Audit trail - see exactly what happened
- Debugging - inspect state at each step

## Workflow State Machine

### State Diagram

```mermaid
stateDiagram-v2
    [*] --> Triggered
    Triggered --> Analyzing
    Analyzing --> QuestionsPosted
    QuestionsPosted --> AnswersReceived
    AnswersReceived --> Planning
    Planning --> PlanPosted
    PlanPosted --> PlanApproved
    PlanApproved --> Implementing
    Implementing --> PRCreated
    PRCreated --> Completed
    Completed --> [*]

    QuestionsPosted --> QuestionsPosted: More questions needed
    PlanPosted --> AnswersReceived: Plan rejected, more clarification
    Implementing --> Implementing: Iteration needed

    Analyzing --> Failed
    Planning --> Failed
    Implementing --> Failed
    Failed --> [*]

    Triggered --> Cancelled
    QuestionsPosted --> Cancelled
    PlanPosted --> Cancelled
    Cancelled --> [*]
```

### Valid Transitions

Enforced by `WorkflowStateTransitions` class:

```csharp
private static readonly Dictionary<WorkflowState, List<WorkflowState>> ValidTransitions = new()
{
    { WorkflowState.Triggered, new() { Analyzing, Failed, Cancelled } },
    { WorkflowState.Analyzing, new() { QuestionsPosted, Failed } },
    { WorkflowState.QuestionsPosted, new() { AnswersReceived, Cancelled } },
    { WorkflowState.AnswersReceived, new() { Planning, QuestionsPosted } },
    { WorkflowState.Planning, new() { PlanPosted, Failed } },
    { WorkflowState.PlanPosted, new() { PlanApproved, AnswersReceived, Cancelled } },
    { WorkflowState.PlanApproved, new() { Implementing, Cancelled } },
    { WorkflowState.Implementing, new() { PRCreated, Failed } },
    { WorkflowState.PRCreated, new() { Completed } },
    // Terminal states have no outbound transitions
    { WorkflowState.Completed, new() },
    { WorkflowState.Failed, new() },
    { WorkflowState.Cancelled, new() }
};
```

## Data Architecture

### Database Schema

See [database-schema.md](database-schema.md) for full details.

**Core Tables:**
- `Tenants` - Multi-tenant isolation
- `Repositories` - Git repository configurations
- `Tickets` - Workflow instances
- `Checkpoints` - Workflow state snapshots
- `AgentExecutions` - Execution history and logs

**Relationships:**
- Tenant (1) → (N) Repositories
- Tenant (1) → (N) Tickets
- Ticket (1) → (N) Checkpoints
- Ticket (1) → (N) AgentExecutions

### Encryption

Sensitive data is encrypted at rest using AES-256:
- Jira API tokens
- Claude API keys
- Git access tokens

Encryption key stored in secure configuration (Azure Key Vault, AWS Secrets Manager).

## Integration Architecture

### Jira Integration

```
Jira Cloud
    │
    │ Webhook (HTTP POST)
    │ HMAC-signed payload
    ↓
WebhookController
    │
    │ 1. Validate HMAC signature
    │ 2. Parse webhook payload
    │ 3. Extract event type (issue.created, comment.created)
    ↓
TicketService
    │
    │ 4. Create or update ticket
    │ 5. Transition state based on event
    ↓
Database (Ticket persisted)
```

**Outbound (PRFactory → Jira):**
- Post comments (questions, plans)
- Update ticket status
- Link pull requests

### Git Integration

**Strategy Pattern** for multi-platform support:

```csharp
public interface IGitPlatformProvider
{
    Task<CloneResult> CloneAsync(string url, string path, string token);
    Task<Branch> CreateBranchAsync(string branchName);
    Task<PushResult> PushAsync(string remoteName, string branchName);
    Task<PullRequest> CreatePullRequestAsync(CreatePRRequest request);
}
```

**Implementations:**
- `GitHubProvider` - Uses Octokit for GitHub API
- `AzureDevOpsProvider` - Uses Azure DevOps SDK
- `GitLabProvider` - Uses GitLab.NET (future)

**Local Git Operations** (LibGit2Sharp):
- Clone repositories
- Create branches
- Commit changes
- Push to remote

### Claude AI Integration

**Request Flow:**

```
Agent (needs AI response)
    ↓
ClaudeClient
    │
    │ 1. Build context (codebase, ticket, history)
    │ 2. Load appropriate prompt template
    │ 3. Call Anthropic API
    ↓
Anthropic API (Claude Sonnet 4.5)
    ↓
ClaudeClient
    │
    │ 4. Parse response
    │ 5. Extract structured data (questions, plan, code)
    ↓
Agent (continues execution)
```

**Prompt Templates:**
- `analyze-codebase.txt` - For codebase analysis
- `generate-questions.txt` - For clarifying questions
- `generate-plan.txt` - For implementation plans
- `implement-code.txt` - For code generation

## Security Architecture

### Multi-Tenancy Isolation

**Data Isolation:**
- All queries filtered by `TenantId`
- EF Core global query filters enforce isolation
- No cross-tenant data access possible

**Credential Isolation:**
- Each tenant has own encrypted credentials
- Encryption keys per-tenant (future enhancement)

### Authentication & Authorization

**API Security:**
- Webhook endpoints validate HMAC signatures
- CRUD endpoints require API key or OAuth (future)
- Rate limiting per tenant

**Git Security:**
- Read-only access during analysis phase
- Write access limited to feature branches
- No merge permissions (PRs only)
- Temporary clones deleted after use

### Secrets Management

**Development:**
- User secrets (`dotnet user-secrets`)
- Environment variables

**Production:**
- Azure Key Vault integration
- AWS Secrets Manager integration
- Encrypted at rest in database (fallback)

### Audit Trail

**Logging:**
- All operations logged with correlation IDs
- Structured logging (Serilog → JSON)
- Sensitive data redacted

**Traceability:**
- Jira comments show all AI interactions
- Git history shows all code changes
- AgentExecutions table stores execution logs

## Deployment Architecture

### Option 1: Docker Compose (Development / PoC)

```yaml
services:
  api:
    image: prfactory-api:latest
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Data Source=/data/prfactory.db
    volumes:
      - ./data:/data

  worker:
    image: prfactory-worker:latest
    environment:
      - ConnectionStrings__DefaultConnection=Data Source=/data/prfactory.db
    volumes:
      - ./data:/data

  jaeger:
    image: jaegertracing/all-in-one:1.51
    ports:
      - "16686:16686"
```

### Option 2: Azure App Service

```
Azure App Service (API)
    │
    ├─ App Service Plan (Linux, B1 or higher)
    ├─ Application Insights (monitoring)
    └─ Azure Key Vault (secrets)

Azure Container Instances (Worker)
    │
    ├─ Container running worker service
    └─ Scheduled scaling

Azure Files or Blob Storage (workspace)
    │
    └─ Shared file storage for git repos

Azure SQL Database (production)
    │
    └─ Replaces SQLite
```

### Option 3: On-Premises (Windows Server)

```
IIS (hosts API)
    │
    └─ ASP.NET Core Module

Windows Service (hosts Worker)
    │
    └─ Background job processor

SQL Server (database)

Network share (workspace for git repos)
```

## Performance Considerations

### Caching Strategy

- **Repository clones** - Cache cloned repos, invalidate after N hours
- **Jira metadata** - Cache ticket metadata for 5 minutes
- **Claude responses** - Optional caching for identical queries

### Scalability

**Horizontal Scaling:**
- API: Stateless, can run multiple instances behind load balancer
- Worker: Multiple worker instances can process different tickets
- Database: Use PostgreSQL or SQL Server with connection pooling

**Vertical Scaling:**
- Increase worker memory for larger repository analysis
- Increase API memory for high webhook volume

### Resource Management

- **Workspace cleanup** - Delete old repos after N days
- **Token usage tracking** - Monitor Claude API costs per tenant
- **Rate limiting** - Limit API requests per tenant

## Observability

### Logging

```
Serilog
  ├─ Console Sink (development)
  ├─ File Sink (production, rolling files)
  └─ Azure Application Insights Sink (cloud)
```

**Structured Logging:**
```csharp
_logger.LogInformation(
    "Ticket {TicketKey} transitioned from {FromState} to {ToState}",
    ticket.JiraKey, oldState, newState);
```

### Distributed Tracing

OpenTelemetry with Jaeger exporter:
- Trace webhook → service → agent → external API
- Identify bottlenecks
- Debug failures across systems

### Metrics

**Custom Metrics:**
- Tickets processed per hour
- Average time per workflow phase
- Success rate
- Claude API token usage
- Git operation durations

## Design Decisions & Rationale

### Why Clean Architecture?

- **Testability** - Business logic isolated from infrastructure
- **Maintainability** - Clear separation of concerns
- **Flexibility** - Swap Git providers without changing domain logic

### Why Agents?

- **Single Responsibility** - Each agent does one thing well
- **Fault Tolerance** - Retry individual steps without restarting workflow
- **Observability** - Clear visibility into what each step is doing

### Why SQLite (default)?

- **Simplicity** - No separate database server for PoC
- **Portability** - Single file database
- **Upgrade Path** - Easy to switch to PostgreSQL/SQL Server later

### Why Polling (Worker) instead of Webhooks?

- **Simplicity** - No need for reverse HTTP from external systems to worker
- **Fault Tolerance** - Missed polls can be caught up
- **Control** - Easy to scale workers independently

## Future Enhancements

1. **Real-time UI** - Blazor/SignalR dashboard for live workflow monitoring
2. **Domain Events** - Event sourcing for complete audit trail
3. **CQRS** - Separate read/write models for better scalability
4. **Advanced Retry** - Exponential backoff, circuit breakers for all external calls
5. **Metrics Dashboard** - Grafana dashboards for observability
6. **Multi-Model Support** - Support GPT-4, Gemini in addition to Claude
7. **Approval Workflows** - Configurable approval processes (e.g., manager approval)

## Additional Resources

- [Setup Guide](SETUP.md) - Installation and configuration
- [Workflow Details](WORKFLOW.md) - Detailed workflow explanation
- [Database Schema](database-schema.md) - Database structure
- [Component READMEs](../src/) - Component-specific documentation
