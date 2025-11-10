# Service Inventory for DI Validation Tests

Complete list of all services registered in PRFactory, organized by category and application.

---

## Repository Services (11 total)

All are registered via `AddInfrastructure()` with **Scoped** lifetime.

| Interface | Implementation | File | Notes |
|-----------|---|---|---|
| `ITenantRepository` | `TenantRepository` | DependencyInjection.cs:66 | Multi-tenant isolation |
| `IRepositoryRepository` | `RepositoryRepository` | DependencyInjection.cs:67 | Git repository metadata |
| `ITicketRepository` | `TicketRepository` | DependencyInjection.cs:68 | Jira/Ticket entities |
| `ITicketUpdateRepository` | `TicketUpdateRepository` | DependencyInjection.cs:69 | Ticket update proposals |
| `IWorkflowEventRepository` | `WorkflowEventRepository` | DependencyInjection.cs:70 | Workflow state events |
| `ICheckpointRepository` | `CheckpointRepository` | DependencyInjection.cs:71 | Agent execution checkpoints |
| `IAgentPromptTemplateRepository` | `AgentPromptTemplateRepository` | DependencyInjection.cs:72 | Prompt templates |
| `IErrorRepository` | `ErrorRepository` | DependencyInjection.cs:73 | Error tracking |
| `IUserRepository` | `UserRepository` | DependencyInjection.cs:76 | Team review users |
| `IPlanReviewRepository` | `PlanReviewRepository` | DependencyInjection.cs:77 | Plan review approvals |
| `IReviewCommentRepository` | `ReviewCommentRepository` | DependencyInjection.cs:78 | Review feedback |

**Test Pattern:**
```csharp
[Fact]
public void AddInfrastructure_RegistersAllRepositories()
{
    // Verify 11 repositories total
    var repoCount = services.Where(d => 
        d.ServiceType.Name.EndsWith("Repository") && 
        d.Lifetime == ServiceLifetime.Scoped).Count();
    Assert.Equal(11, repoCount);
}
```

---

## Application Services (10 total)

All are registered via `AddInfrastructure()` with **Scoped** lifetime.

| Interface | Implementation | File | Purpose |
|-----------|---|---|---|
| `ITicketUpdateService` | `Application.TicketUpdateService` | DependencyInjection.cs:96 | Ticket update operations |
| `ITicketApplicationService` | `Application.TicketApplicationService` | DependencyInjection.cs:97 | Ticket business logic |
| `IRepositoryApplicationService` | `Application.RepositoryApplicationService` | DependencyInjection.cs:98 | Repository operations |
| `ITenantApplicationService` | `Application.TenantApplicationService` | DependencyInjection.cs:99 | Tenant management |
| `IErrorApplicationService` | `Application.ErrorApplicationService` | DependencyInjection.cs:100 | Error logging/tracking |
| `ITenantContext` | `Application.TenantContext` | DependencyInjection.cs:101 | Tenant resolution |
| `IQuestionApplicationService` | `Application.QuestionApplicationService` | DependencyInjection.cs:102 | Clarifying question generation |
| `IWorkflowEventApplicationService` | `Application.WorkflowEventApplicationService` | DependencyInjection.cs:103 | Workflow event handling |
| `IPlanService` | `Application.PlanService` | DependencyInjection.cs:104 | Implementation planning |
| `IUserService` | `Application.UserService` | DependencyInjection.cs:107 | Team review user management |

---

## Team Review Services (1 total)

Registered via `AddInfrastructure()` with **Scoped** lifetime.

| Interface | Implementation | File | Purpose |
|-----------|---|---|---|
| `IPlanReviewService` | `Application.PlanReviewService` | DependencyInjection.cs:108 | Plan review workflows |

---

## Infrastructure Core Services (8 total)

### Encryption & Security
- `IEncryptionService` → `AesEncryptionService` (**Singleton**) - DependencyInjection.cs:38
  - Validates encryption key in configuration
  - Factory registration with logger

### Database & Context
- `ApplicationDbContext` - DependencyInjection.cs:48 (**Scoped**)
  - SQLite database context
  - Development logging options

### Configuration
- `ITenantConfigurationService` → `TenantConfigurationService` (**Scoped**) - DependencyInjection.cs:93
  - Tenant-specific settings

### Memory Caching
- `IMemoryCache` (Microsoft.Extensions.Caching.Memory) - DependencyInjection.cs:90 (**Singleton**)
  - Built-in caching for performance

### Workflow State Management
- `IWorkflowStateStore` (from Agents.Graphs) → `WorkflowStateStore` (**Scoped**) - DependencyInjection.cs:84
  - Workflow state persistence

### Event Publishing
- `IEventPublisher` (from Agents.Graphs) → `EventPublisher` (**Scoped**) - DependencyInjection.cs:87
  - Event distribution to subscribers

### Checkpoint Storage
- `ICheckpointStore` (from Agents) → `GraphCheckpointStoreAdapter` (**Scoped**) - DependencyInjection.cs:81
  - Agent checkpoint persistence

### AI Context Building
- `Claude.IContextBuilder` → `Claude.ContextBuilder` (**Scoped**) - DependencyInjection.cs:116
  - Repository analysis context for Claude

---

## Agent Services (17 total)

All are registered via `AddInfrastructure()` with **Transient** lifetime.

### Refinement Phase Agents
| Agent | File | Purpose |
|-------|------|---------|
| `TriggerAgent` | DependencyInjection.cs:119 | Workflow initiation |
| `RepositoryCloneAgent` | DependencyInjection.cs:120 | Clone repository |
| `AnalysisAgent` | DependencyInjection.cs:121 | Analyze codebase |
| `QuestionGenerationAgent` | DependencyInjection.cs:122 | Generate clarifying questions |
| `JiraPostAgent` | DependencyInjection.cs:123 | Post to Jira |
| `HumanWaitAgent` | DependencyInjection.cs:124 | Suspend for human input |
| `AnswerProcessingAgent` | DependencyInjection.cs:125 | Process human answers |

### Planning Phase Agents
| Agent | File | Purpose |
|-------|------|---------|
| `PlanningAgent` | DependencyInjection.cs:126 | Generate implementation plan |
| `GitPlanAgent` | DependencyInjection.cs:127 | Commit plan to Git |

### Implementation Phase Agents
| Agent | File | Purpose |
|-------|------|---------|
| `ImplementationAgent` | DependencyInjection.cs:128 | Generate code |
| `GitCommitAgent` | DependencyInjection.cs:129 | Commit code to Git |
| `PullRequestAgent` | DependencyInjection.cs:130 | Create pull request |

### Workflow Agents
| Agent | File | Purpose |
|-------|------|---------|
| `CompletionAgent` | DependencyInjection.cs:131 | Mark workflow complete |
| `ApprovalCheckAgent` | DependencyInjection.cs:132 | Check approval status |
| `ErrorHandlingAgent` | DependencyInjection.cs:133 | Handle errors |

**Test Pattern:**
```csharp
[Theory]
[InlineData(typeof(TriggerAgent))]
[InlineData(typeof(RepositoryCloneAgent))]
[InlineData(typeof(AnalysisAgent))]
// ... 14 more agents
public void AddInfrastructure_AgentIsTransient(Type agentType)
{
    // Verify agent is Transient
    var descriptor = services.FirstOrDefault(d => d.ImplementationType == agentType);
    Assert.NotNull(descriptor);
    Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
}
```

---

## Agent Framework Services (via AddAgentFramework)

### Agent Prompt Services (2 total)
- `Agents.Services.IAgentPromptService` → `Agents.Services.AgentPromptService` (**Scoped**) - DependencyInjection.cs:112
- `Agents.Services.AgentPromptLoaderService` (**Scoped**) - DependencyInjection.cs:113

### Agent Registry
- `AgentRegistry` - Registered via `AddAgentRegistry()` in ServiceCollectionExtensions.cs
  - Discovers and registers all agent types

### Agent Executor
- `Agents.Graphs.IAgentExecutor` → `Agents.Graphs.AgentExecutor` (**Scoped**) - DependencyInjection.cs:136

### Middleware Services (if enabled via configuration)
- `LoggingMiddleware` (**Singleton**)
- `ErrorHandlingMiddleware` (**Singleton**)
- `RetryMiddleware` (**Singleton**)

### Checkpoint Repository
- `ICheckpointRepository` → `InMemoryCheckpointRepository` (**Singleton**)
  - Registered in ServiceCollectionExtensions.cs:61

---

## Git Platform Services (via AddGitPlatformIntegration)

### Git Service
- `ILocalGitService` → `LocalGitService` (**Scoped**) - GitServiceCollectionExtensions.cs:25

### Platform Providers (3 registered, 1 todo)
- `IGitPlatformProvider` → `GitHubProvider` (**Scoped**) - GitServiceCollectionExtensions.cs:28
- `IGitPlatformProvider` → `AzureDevOpsProvider` (**Scoped**) - GitServiceCollectionExtensions.cs:29
- `IGitPlatformProvider` → `BitbucketProvider` (**Scoped**) - GitServiceCollectionExtensions.cs:37-38
- *(TODO: GitLab provider)*

### Facade Service
- `IGitPlatformService` → `GitPlatformService` (**Scoped**) - GitServiceCollectionExtensions.cs:43-58
  - Factory registration with repository getter

**Test Pattern:**
```csharp
[Fact]
public void AddGitPlatformIntegration_RegistersAllProviders()
{
    // Verify all 3 providers available
    var providers = provider.GetRequiredService<IEnumerable<IGitPlatformProvider>>();
    Assert.NotEmpty(providers);
    Assert.Equal(3, providers.Count());
    Assert.True(providers.Any(p => p is GitHubProvider));
    Assert.True(providers.Any(p => p is AzureDevOpsProvider));
    Assert.True(providers.Any(p => p is BitbucketProvider));
}
```

---

## Web Layer Services (via Program.cs)

All are registered in `/src/PRFactory.Web/Program.cs` with **Scoped** lifetime.

### Web Facade Services (7 total)
| Interface | Implementation | Line | Purpose |
|-----------|---|---|---|
| `ITicketService` | `TicketService` | 33 | Ticket UI operations |
| `IRepositoryService` | `RepositoryService` | 34 | Repository UI operations |
| `IWorkflowEventService` | `WorkflowEventService` | 35 | Workflow event UI |
| `IAgentPromptService` | `AgentPromptService` | 36 | Agent prompt UI |
| `ITenantService` | `TenantService` | 37 | Tenant UI operations |
| `IErrorService` | `ErrorService` | 38 | Error display UI |
| `IToastService` | `ToastService` | 41 | Toast notification service |

### SignalR Services
- `IEventBroadcaster` (Infrastructure.Events) → `SignalREventBroadcaster` (**Scoped**) - Program.cs:26-27
  - Real-time event broadcasting via SignalR

### Infrastructure Services
- All services from `AddInfrastructure()` are available via Program.cs:30

### Database Services
- `DbSeeder` (**Scoped**) - DependencyInjection.cs:152
  - Demo data seeding in development

---

## Worker Service (via Program.cs)

Registered in `/src/PRFactory.Worker/Program.cs`:

### Worker Services
- `IWorkflowResumeHandler` → `WorkflowResumeHandler` (**Scoped**) - Program.cs:96
  - Handles workflow resumption from checkpoints

### Configuration
- `AgentHostOptions` - Configured from "AgentHost" section
  - `AgentHostService` registered as `IHostedService`

---

## CLI Agent Services (via AddInfrastructure)

### Process Execution
- `IProcessExecutor` → `ProcessExecutor` (**Scoped**) - DependencyInjection.cs:139
  - Executes external processes safely

### CLI Agents
- `ICliAgent` → `ClaudeCodeCliAdapter` (factory, **Scoped**) - DependencyInjection.cs:149
- `ClaudeCodeCliAdapter` (**Scoped**) - DependencyInjection.cs:145
- `CodexCliAdapter` (**Scoped**) - DependencyInjection.cs:146

### Configuration
- `ClaudeCodeCliOptions` - Configured from "ClaudeCodeCli" section - DependencyInjection.cs:142-143

---

## Summary Statistics

| Category | Count | Lifetime |
|----------|-------|----------|
| Repositories | 11 | Scoped |
| Application Services | 11 | Scoped |
| Infrastructure Core | 8 | Mixed |
| Agents | 17 | Transient |
| Agent Framework | 5 | Mixed |
| Git Platform | 4 | Scoped |
| Web Facade | 7 | Scoped |
| Worker | 1 | Scoped |
| CLI | 3 | Scoped |
| **Total** | **67+** | |

---

## Service Dependency Chains

### Critical Chain 1: Ticket Workflow
```
ITicketService
  └─ ITicketApplicationService
      ├─ ITicketRepository
      ├─ ITicketUpdateService
      │  └─ ITicketUpdateRepository
      ├─ IQuestionApplicationService
      ├─ IWorkflowEventApplicationService
      │  └─ IWorkflowEventRepository
      ├─ IPlanService
      └─ ITenantContext
```

### Critical Chain 2: Planning Workflow
```
PlanningAgent
  ├─ IRepositoryRepository
  ├─ ITicketRepository
  ├─ IPlanService
  ├─ Claude.IContextBuilder
  └─ IAgentPromptService
```

### Critical Chain 3: Git Operations
```
IGitPlatformService
  ├─ ILocalGitService
  ├─ IEnumerable<IGitPlatformProvider>
  │  ├─ GitHubProvider
  │  ├─ AzureDevOpsProvider
  │  └─ BitbucketProvider
  └─ IMemoryCache
```

---

## Testing Recommendations

### Phase 1: Test Basic Registration
- Verify each service can be resolved: 67+ tests
- Estimated time: 2-3 hours

### Phase 2: Test Lifetimes
- Verify all Scoped services are indeed Scoped: ~35 tests
- Verify all Transient services are indeed Transient: ~17 tests
- Verify all Singleton services are indeed Singleton: ~6 tests
- Estimated time: 1-2 hours

### Phase 3: Test Dependency Chains
- Verify critical chains resolve without error: 3-5 tests
- Estimated time: 1 hour

### Phase 4: Test Configuration
- Verify encryption key validation: 2 tests
- Verify optional configuration: 2-3 tests
- Estimated time: 30 minutes

**Total Estimated Time: 5-7 hours**

---

## Missing/TODO Services

### API Program (commented out)
- Infrastructure services not yet registered
- Will need to uncomment or add separate registration method

### Worker Program (commented out)
- Infrastructure services not yet registered
- Specific agent execution services needed

### Future Platforms
- GitLab provider (planned)
- Additional ticket platform providers (planned)

---

## Configuration Keys Referenced

Services depend on these configuration sections:

| Key | Purpose | Example Value | Required |
|-----|---------|---|---|
| `Encryption:Key` | Service encryption | Base64-encoded key | Yes |
| `ConnectionStrings:DefaultConnection` | Database connection | `Data Source=prfactory.db` | No (has default) |
| `Logging:EnableSensitiveDataLogging` | Debug mode for DB | `true` | No |
| `Logging:EnableDetailedErrors` | Detailed error info | `true` | No |
| `AgentHost:Enabled` | Agent execution | `true` | No |
| `ClaudeCodeCli:Path` | CLI tool location | `/usr/local/bin/claude` | No |

---

## Notes for Test Implementation

1. **DbContext Setup**: Tests must use in-memory database with unique IDs to avoid conflicts
2. **Logging**: Service collection needs `.AddLogging()` or services will fail to resolve loggers
3. **Configuration**: Must provide valid `Encryption:Key` or registration will throw
4. **Factory Registrations**: Some services use factory registration (encryption, git service) - verify factory logic works
5. **Provider Pattern**: Multiple `IGitPlatformProvider` registrations should all resolve via `IEnumerable<T>`
6. **Middleware**: Optional middleware should only register when enabled in configuration

