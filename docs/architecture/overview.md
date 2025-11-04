# PR Factory - Architecture Overview

## Executive Summary

PR Factory is a .NET-based system that automates the journey from vague Jira tickets to implementation-ready pull requests through AI-assisted refinement, planning, and optional implementation.

## Technology Stack

### Core Platform
- **.NET 8** (LTS) - Core framework
- **ASP.NET Core** - Web API and hosting
- **Blazor Server** - Web UI for developers
- **C# 12** - Primary language

### Key Libraries
- **Hangfire** (1.8.x) - Background job processing
- **Polly** (8.x) - Resilience and retry policies
- **FluentValidation** (11.x) - Input validation
- **Refit** (7.x) - Typed HTTP clients
- **Serilog** (3.x) - Structured logging

### Data Storage
- **SQLite** - Primary database (tickets, workflows, state)
- **Entity Framework Core 8** - ORM
- **In-Memory Cache** - Simple caching (can upgrade to Redis later)

### External Integrations
- **Anthropic SDK** - Claude AI API
- **LibGit2Sharp** - Local git operations
- **Octokit** - GitHub API
- **Azure DevOps SDK** - Azure Repos API
- **Bitbucket.Net** - Bitbucket API

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Developer Browser                            │
│                  (Blazor Server UI)                             │
└───────────────────────────┬─────────────────────────────────────┘
                            │ SignalR
┌───────────────────────────┴─────────────────────────────────────┐
│                    ASP.NET Core Host                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Web API    │  │ Blazor Server│  │  Hangfire    │          │
│  │  (Webhooks)  │  │   (UI)       │  │  Dashboard   │          │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘          │
│         │                 │                                     │
│  ┌──────┴─────────────────┴──────────────────────────┐          │
│  │         Application Layer (Services)              │          │
│  │  TicketService  │  WorkflowService  │  etc.       │          │
│  └──────┬─────────────────────────────────┬──────────┘          │
│         │                                 │                     │
│  ┌──────┴─────────────────────────────────┴──────────┐          │
│  │              Domain Layer                         │          │
│  │  Workflow Engine  │  State Machine  │  Entities   │          │
│  └──────┬─────────────────────────────────┬──────────┘          │
│         │                                 │                     │
│  ┌──────┴─────────────────────────────────┴──────────┐          │
│  │         Infrastructure Layer                      │          │
│  │  Jira│Git│Claude│Database│Cache│Storage│Logging   │          │
│  └───────────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
   ┌────▼────┐         ┌────▼────┐         ┌───▼────┐
   │  Jira   │         │   Git   │         │ Claude │
   │  Cloud  │         │Platform │         │   AI   │
   └─────────┘         └─────────┘         └────────┘
```

## Solution Structure

```
PRFactory/
├── src/
│   ├── PRFactory.Api/                      # ASP.NET Core Web API
│   │   ├── Controllers/                    # API endpoints (webhooks)
│   │   ├── Hubs/                           # SignalR hubs for real-time updates
│   │   └── Program.cs
│   │
│   ├── PRFactory.Web/                      # Blazor Server UI
│   │   ├── Pages/                          # Blazor pages
│   │   ├── Components/                     # Reusable UI components
│   │   ├── Services/                       # UI-specific services
│   │   └── Program.cs
│   │
│   ├── PRFactory.Core/                     # Domain & Application Logic
│   │   ├── Domain/
│   │   │   ├── Entities/                   # Core entities
│   │   │   ├── ValueObjects/               # Value objects
│   │   │   └── Interfaces/                 # Repository interfaces
│   │   │
│   │   ├── Services/                       # Application services
│   │   │   ├── TicketService.cs
│   │   │   ├── WorkflowService.cs
│   │   │   └── ...
│   │   │
│   │   └── StateMachine/
│   │       ├── WorkflowState.cs            # State enum
│   │       ├── WorkflowEngine.cs           # State machine engine
│   │       └── Transitions/                # State transition rules
│   │
│   ├── PRFactory.Infrastructure/           # External Dependencies
│   │   ├── Jira/
│   │   │   ├── JiraClient.cs               # Refit client
│   │   │   ├── JiraWebhookValidator.cs     # HMAC validation
│   │   │   └── Models/                     # DTOs
│   │   │
│   │   ├── Git/
│   │   │   ├── IGitPlatformProvider.cs     # Abstraction
│   │   │   ├── GitHubProvider.cs
│   │   │   ├── BitbucketProvider.cs
│   │   │   ├── AzureDevOpsProvider.cs
│   │   │   └── LocalGitService.cs          # LibGit2Sharp wrapper
│   │   │
│   │   ├── Claude/
│   │   │   ├── ClaudeClient.cs             # Anthropic SDK wrapper
│   │   │   ├── PromptTemplates/            # Prompt templates
│   │   │   └── ContextBuilder.cs           # Context management
│   │   │
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs     # EF Core context
│   │   │   ├── Repositories/               # Repository implementations
│   │   │   └── Migrations/
│   │   │
│   │   └── Caching/
│   │       └── RedisCacheService.cs
│   │
│   ├── PRFactory.Worker/                   # Background Worker
│   │   ├── Jobs/                           # Hangfire jobs
│   │   └── Program.cs
│   │
│   └── PRFactory.Shared/                   # Shared Utilities
│       ├── Extensions/
│       ├── Helpers/
│       └── Constants/
│
├── tests/
│   ├── PRFactory.Core.Tests/
│   ├── PRFactory.Infrastructure.Tests/
│   └── PRFactory.Integration.Tests/
│
├── docs/
│   └── architecture/                       # This folder
│
└── PRFactory.sln
```

## Key Architectural Patterns

### 1. Clean Architecture (Onion)
- **Domain** (Core) - Business logic, entities, state machine
- **Application** - Services, business workflows
- **Infrastructure** - External integrations (Jira, Git, Claude, DB)
- **Presentation** - Web API + Blazor UI

Dependencies flow inward: Infrastructure → Application → Domain

### 2. Service Layer Pattern
```csharp
// Direct service calls
public class TicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IWorkflowEngine _workflowEngine;

    public async Task<Result> TriggerTicketAsync(string jiraKey, Guid tenantId, Guid repoId)
    {
        // Create ticket
        var ticket = Ticket.Create(jiraKey, tenantId, repoId);

        // Transition state
        await _workflowEngine.TransitionAsync(ticket, WorkflowState.Analyzing);

        // Save
        await _ticketRepository.AddAsync(ticket);

        // Enqueue job
        BackgroundJob.Enqueue<RefineTicketJob>(job => job.ExecuteAsync(ticket.Id));

        return Result.Success();
    }
}
```

### 3. Strategy Pattern for Git Platforms
```csharp
public interface IGitPlatformProvider
{
    Task<Repository> CloneAsync(string repoUrl);
    Task<Branch> CreateBranchAsync(string name);
    Task<PullRequest> CreatePRAsync(CreatePRRequest request);
}

// Implementations: GitHubProvider, BitbucketProvider, AzureDevOpsProvider
```

### 4. State Machine for Workflow
```csharp
public enum WorkflowState
{
    Triggered,
    Analyzing,
    QuestionsPosted,
    AnswersReceived,
    Planning,
    PlanPosted,
    PlanApproved,
    Implementing,
    PRCreated,
    Completed,
    Failed
}
```

## Data Flow: Typical Workflow

### 1. Ticket Triggered (Jira → System)
```
POST /api/webhooks/jira
  → JiraWebhookController
  → ValidateWebhook
  → TicketService.TriggerTicketAsync()
  → Hangfire.Enqueue<RefineTicketJob>
```

### 2. Refinement Phase (System → Jira)
```
RefineTicketJob
  → CloneRepository
  → AnalyzeCodebase (Claude)
  → GenerateQuestions (Claude)
  → PostQuestionsToJira
  → UpdateTicketState(QuestionsPosted)
```

### 3. Developer Responds (Jira → System → Jira)
```
POST /api/webhooks/jira (comment with @claude)
  → JiraWebhookController
  → TicketService.SubmitAnswersAsync()
  → UpdateTicketState(AnswersReceived)
  → Hangfire.Enqueue<GeneratePlanJob>

GeneratePlanJob
  → ClaudeService.GeneratePlan()
  → GitService.CreateFeatureBranch()
  → GitService.CommitPlanFiles()
  → GitService.PushBranch()
  → JiraService.PostPlanToJira()
  → WorkflowEngine.TransitionState(PlanPosted)
```

### 4. Plan Approved (Jira → System → Git)
```
POST /api/webhooks/jira (approval comment)
  → JiraWebhookController
  → TicketService.ApprovePlanAsync()
  → WorkflowEngine.TransitionState(PlanApproved)
  → [Optional] Hangfire.Enqueue<ImplementPlanJob>

ImplementPlanJob
  → ClaudeService.ImplementCode()
  → GitService.Commit & Push()
  → GitService.CreatePullRequest()
  → JiraService.LinkPRToTicket()
  → WorkflowEngine.TransitionState(PRCreated)
```

## Configuration & Deployment

### Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "Database": "Data Source=prfactory.db"
  },
  "Jira": {
    "BaseUrl": "https://yourcompany.atlassian.net",
    "WebhookSecret": "..."
  },
  "Claude": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 8000
  },
  "Workspace": {
    "BasePath": "/var/prfactory/workspace"
  }
}
```

### Deployment Options

**Option 1: Docker Compose (Recommended for PoC)**
```yaml
services:
  web:
    image: prfactory:latest
    ports: ["5000:8080"]
    volumes:
      - ./data:/app/data

  worker:
    image: prfactory-worker:latest
    volumes:
      - ./data:/app/data
```

**Option 2: Windows Server (IIS + Windows Service)**
- IIS hosts Web API + Blazor UI
- Windows Service runs Hangfire Worker
- SQLite database on shared volume

**Option 3: Azure (Cloud)**
- Azure App Service (Web + Worker)
- SQLite on Azure Files or upgrade to Azure SQL
- Azure Key Vault for secrets

## Security Considerations

### Authentication & Authorization
- Jira webhooks validated with HMAC signatures
- API endpoints require API key or OAuth
- Per-tenant isolation (multi-tenancy)

### Secrets Management
- Azure Key Vault integration
- PATs encrypted at rest
- Never log sensitive data

### Git Operations
- Read-only access during analysis
- Write access only to feature branches
- No merge permissions
- Repository clones isolated per ticket

### Audit Trail
- All operations logged with correlation IDs
- Jira comments for transparency
- Git history for all changes

## Scalability & Performance

### Horizontal Scaling
- Stateless API (multiple instances behind load balancer)
- Multiple Hangfire workers
- File-based or database-based locking for single instance
- For true horizontal scaling, upgrade to Redis/PostgreSQL

### Performance Optimizations
- Repository caching (avoid re-cloning)
- Incremental analysis (only changed files)
- Parallel question generation
- Streaming AI responses

### Resource Management
- Workspace cleanup (old repos deleted after N days)
- Token usage tracking
- Rate limiting per tenant

## Monitoring & Observability

### Logging (Serilog)
- Structured JSON logs
- Correlation IDs for tracing
- Integration with ELK or Azure Monitor

### Metrics
- Hangfire dashboard for job monitoring
- Custom metrics: tickets processed, success rate, avg time per phase
- AI token usage and cost tracking

### Alerting
- Failed jobs (retry exhausted)
- Git operation failures
- API rate limit warnings

## Next Steps

1. **Review detailed component designs:**
   - [Core Workflow Engine](./core-engine.md)
   - [Jira Integration](./jira-integration.md)
   - [Git Integration](./git-integration.md)
   - [Claude AI Integration](./claude-integration.md)

2. **Start with MVP:**
   - Core workflow engine with state machine
   - Jira webhook + basic API client
   - Single git platform (GitHub)
   - Basic Claude integration (analysis + planning only)
   - Simple Blazor UI for monitoring

3. **Iterate:**
   - Add more git platforms
   - Implement optional code generation phase
   - Enhance UI with real-time updates
   - Add analytics and reporting
