# CLAUDE.md - Architecture Vision for AI Agents

> **Purpose**: This document guides AI agents (like Claude) working on the PRFactory codebase to understand what architectural decisions are INTENTIONAL and should be preserved vs. what can be simplified or removed.

---

## Table of Contents

- [Core Vision & Principles](#core-vision--principles)
- [What NOT to Simplify](#what-not-to-simplify)
- [What IS Overengineered](#what-is-overengineered)
- [Architecture Overview](#architecture-overview)
- [Multi-Platform Strategy](#multi-platform-strategy)
- [Development Guidelines](#development-guidelines)

---

## Core Vision & Principles

### 1. Flexible Agent Graph Architecture

**This is INTENTIONAL, NOT overengineering.**

The system uses a **multi-graph architecture** with four distinct graph types:

1. **RefinementGraph** - Handles ticket analysis and requirement clarification
2. **PlanningGraph** - Generates and manages implementation plans
3. **ImplementationGraph** - Executes code implementation (optional)
4. **WorkflowOrchestrator** - Coordinates transitions between graphs

```
┌────────────────────────────────────────────────────────┐
│              WorkflowOrchestrator                      │
│  (Manages graph transitions and workflow state)        │
└──────────┬─────────────┬──────────────┬────────────────┘
           │             │              │
           ▼             ▼              ▼
    RefinementGraph  PlanningGraph  ImplementationGraph
```

**Why This Matters:**

- **Flexibility**: Each graph can evolve independently with different retry logic, parallel execution, and conditional branching
- **Composability**: New graphs can be added for future workflows (e.g., CodeReviewGraph, TestingGraph, DeploymentGraph)
- **Fault Tolerance**: Graphs can suspend and resume at different points independently
- **Testability**: Each graph can be tested in isolation
- **Future Extensibility**: The architecture supports complex workflows like:
  - A/B testing different implementation strategies
  - Multi-stage approval processes
  - Parallel code generation with winner selection
  - Automated code refinement loops

**Key Files:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/RefinementGraph.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/ImplementationGraph.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/WorkflowOrchestrator.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/GraphBuilder.cs`

**DO NOT:**
- Collapse multiple graphs into a single monolithic workflow
- Remove the graph abstraction layer
- Simplify the WorkflowOrchestrator to direct agent calls

**DO:**
- Add new specialized graphs for new workflow types
- Extend GraphBuilder with new patterns (loops, fan-out/fan-in, etc.)
- Implement graph-level retry and error handling strategies

---

### 2. Multi-Platform Support is CORE

**This is a CORE PRODUCT FEATURE, NOT premature optimization.**

The system is designed from the ground up to support multiple platforms:

**Source Control Platforms:**
- GitHub (via Octokit)
- Bitbucket (via REST API)
- Azure DevOps (via Azure DevOps SDK)
- GitLab (planned)

**Ticket Management Platforms:**
- Jira (current implementation)
- Azure DevOps Work Items (planned)
- GitHub Issues (planned)
- GitLab Issues (planned)

**Architecture Pattern: Strategy Pattern**

```csharp
public interface IGitPlatformProvider
{
    string PlatformName { get; }
    Task<PullRequestInfo> CreatePullRequestAsync(...);
    Task AddPullRequestCommentAsync(...);
    Task<RepositoryInfo> GetRepositoryInfoAsync(...);
}

// Implementations:
// - GitHubProvider
// - BitbucketProvider
// - AzureDevOpsProvider
// - GitLabProvider (future)
```

**Why This Matters:**

- **Market Differentiation**: Enterprise customers use diverse toolchains. Supporting multiple platforms is a key selling point.
- **Customer Requirements**: Large enterprises often standardize on Azure DevOps or GitLab, not GitHub.
- **Migration Support**: Customers may be transitioning between platforms.
- **Vendor Independence**: Avoid lock-in to any single platform provider.

**Key Files:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/IGitPlatformProvider.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/Providers/GitHubProvider.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/Providers/BitbucketProvider.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/Providers/AzureDevOpsProvider.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/GitPlatformService.cs`

**DO NOT:**
- Remove provider implementations thinking they're unused
- Simplify to a single GitHub-only implementation
- Remove the strategy pattern abstraction layer
- Hardcode platform-specific logic in core business logic

**DO:**
- Complete GitLab provider implementation
- Add missing platform features
- Improve platform auto-detection logic
- Extend the provider interface as new platform capabilities are needed

---

### 3. LibGit2Sharp for Git Flexibility

**Using LibGit2Sharp instead of CLI git commands is INTENTIONAL.**

**Why LibGit2Sharp:**

1. **Cross-Platform Compatibility**: Works identically on Windows, Linux, macOS without shell dependencies
2. **No External Dependencies**: Doesn't require git CLI to be installed
3. **Process Safety**: No shell injection vulnerabilities, no subprocess management complexity
4. **Performance**: In-process operations are faster than spawning processes
5. **Fine-Grained Control**: Programmatic access to all git operations with detailed error handling
6. **Credential Management**: Better integration with secure credential storage
7. **Docker/Container Friendly**: Simpler container images without git installation

**Key Files:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/LocalGitService.cs` (wraps LibGit2Sharp)

**DO NOT:**
- Replace LibGit2Sharp with CLI git commands
- Introduce shell command execution for git operations

**DO:**
- Continue using LibGit2Sharp for all local git operations
- Add helper methods to LocalGitService as needed
- Handle LibGit2Sharp exceptions gracefully

---

## What NOT to Simplify

These architectural decisions should be **PRESERVED**:

### 1. Agent Graph Architecture

- **Keep all 4 graph types**: RefinementGraph, PlanningGraph, ImplementationGraph, WorkflowOrchestrator
- **Keep graph abstraction layer**: AgentGraphBase, IAgentGraph, GraphBuilder
- **Keep checkpoint-based resumption**: Critical for fault tolerance
- **Keep parallel execution support**: GitPlan + JiraPost in parallel is intentional

**Rationale**: This architecture enables future complex workflows and provides flexibility for enterprise requirements.

### 2. Multi-Platform Provider Support

- **Keep all Git platform providers**: GitHub, Bitbucket, Azure DevOps (and future GitLab)
- **Keep strategy pattern**: IGitPlatformProvider interface and implementations
- **Keep platform auto-detection**: Repository.GitPlatform-based routing
- **Keep provider-specific retry policies**: Different platforms have different rate limits

**Rationale**: Enterprise customers require multi-platform support. This is a core product feature.

### 3. Configuration Flexibility

- **Keep multi-tenant architecture**: Isolated tenant environments are critical
- **Keep tenant-level configuration**: Different tenants need different settings
- **Keep repository-level configuration**: Per-repo settings (branch names, approval rules)
- **Keep encrypted credential storage**: Security requirement

**Rationale**: SaaS/multi-tenant deployment is a core business model.

### 4. State Machine Pattern

- **Keep WorkflowState enum with 12 states**: Provides clear workflow visibility
- **Keep state transition validation**: Prevents invalid state changes
- **Keep checkpoint system**: Enables resume after failures

**Rationale**: Workflow complexity requires explicit state management.

### 5. Clean Architecture Layers

- **Keep Domain/Infrastructure/Application separation**: Enables testing and maintainability
- **Keep domain entities**: Ticket, Tenant, Repository, Checkpoint
- **Keep repository pattern**: Abstracts data access

**Rationale**: Standard architectural pattern for maintainable systems.

---

## What IS Overengineered

These items CAN be simplified or removed:

### 1. OpenTelemetry / Distributed Tracing ⚠️

**Status**: OVERENGINEERED for current use case

**Why it's overengineered:**
- Most workflows involve human review (suspended states)
- Workflow durations are measured in hours/days, not milliseconds
- Humans review Jira comments, not Jaeger traces
- Adds complexity without proportional value for current scale

**Files to simplify:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Base/Middleware/TelemetryMiddleware.cs`
- OpenTelemetry configuration in `Program.cs` files
- Jaeger setup in `docker-compose.yml`

**Recommendation:**
- Keep structured logging (Serilog) - this is valuable
- Remove OpenTelemetry ActivitySource and Meter
- Remove Jaeger container from docker-compose
- Keep basic metrics (execution counts, durations) in logs

**What to keep:**
- Structured logging with correlation IDs
- Basic timing information in logs
- Error tracking and reporting

### 2. Stub Implementations 🔧

**Status**: ✅ Completed - All stub implementations have been removed

**Removed files:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Stubs/` directory (deleted)
  - `CheckpointStore.cs`
  - `AgentGraphExecutor.cs`
  - `AgentExecutionQueue.cs`
  - `GraphCheckpointStore.cs`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/IAgentGraphBuilder.cs` (deleted)
  - Removed `AgentGraph` class with NotImplementedException
  - Removed `AgentGraphBuilder` and `IAgentGraphBuilder` interface

**Also removed:**
- Empty placeholder agent classes from graph implementation files
- `ResumeFromCheckpointAsync` extension method that only threw NotImplementedException

**Next steps when needed:**
- Implement proper checkpoint persistence using EF Core
- Implement agent execution queue with database or message broker
- Complete graph executor implementation or integrate with Microsoft.Agents.AI

### 3. Redundant Middleware Layers 🔄

**Status**: May have redundant logging/error handling middleware

**Review these:**
- Check if multiple middleware layers duplicate logging
- Consolidate error handling into fewer layers
- Remove middleware that doesn't add value

**Keep:**
- Essential middleware for cross-cutting concerns
- Authentication/authorization middleware
- Tenant context middleware

### 4. Duplicate Configuration Keys 📋

**Status**: Configuration may have redundant or unused keys

**Recommendation:**
- Audit appsettings.json files for unused keys
- Remove deprecated configuration sections
- Document required vs optional configuration
- Ensure configuration schema is validated on startup

### 5. Placeholder Agent Types 🤖

**Status**: Some agent types are declared but not fully implemented

**Files to review:**
- Agent placeholder classes at end of graph files (e.g., `public class PlanningAgent { }`)
- These should be replaced with actual implementations

**Recommendation:**
- Implement proper agent classes
- Remove placeholder declarations once real agents exist
- Ensure each agent has proper error handling and logging

---

## Architecture Overview

### 3-Phase Workflow Model

PRFactory orchestrates work through three distinct phases, each managed by its own graph:

#### Phase 1: Requirements Refinement (RefinementGraph)

**Purpose**: Understand what needs to be built

**Flow**:
```
Trigger → RepositoryClone → Analysis → QuestionGeneration → JiraPost → [HumanWait] → AnswerProcessing
```

**Key Features:**
- Clones repository and analyzes codebase context
- Generates clarifying questions using Claude AI
- Suspends workflow awaiting human answers
- Resumes when answers received via webhook
- Includes retry logic for analysis failures (up to 3 attempts)

**Suspension Point**: After posting questions to Jira, waits for `@claude` mention with answers

**Completion Event**: `RefinementCompleteEvent` → triggers PlanningGraph

#### Phase 2: Implementation Planning (PlanningGraph)

**Purpose**: Create a detailed, reviewable implementation plan

**Flow**:
```
Planning → [GitPlan + JiraPost (parallel)] → [HumanWait] → Approval/Rejection → Loop or Continue
```

**Key Features:**
- Generates implementation plan using Claude AI with full codebase context
- **Parallel execution**: Commits plan to git AND posts to Jira simultaneously
- Suspends workflow awaiting plan approval
- Loops back to regenerate if rejected (max 5 retries)
- Tracks rejection reasons for improved regeneration

**Suspension Point**: After posting plan, waits for approval/rejection

**Completion Event**:
- `PlanApprovedEvent` → triggers ImplementationGraph
- `PlanRejectedEvent` → loops back to Planning with rejection context

**Loop Behavior**: If plan rejected, incorporates feedback and regenerates (tracks retry count)

#### Phase 3: Code Implementation (ImplementationGraph)

**Purpose**: Optionally implement code based on approved plan

**Flow**:
```
[Check Config] → Implementation → GitCommit → [PullRequest + JiraPost (parallel)] → Completion
```

**Key Features:**
- **Conditional execution**: Only runs if `AutoImplementAfterPlanApproval` enabled
- Implements code following approved plan
- **Parallel execution**: Creates PR AND posts to Jira simultaneously
- Creates pull request for mandatory human review
- No suspension points - runs to completion or failure

**Configuration Check**: Tenant-level setting controls whether this phase executes

**Completion**: Workflow marked as completed, PR awaits human merge

---

### How Agent Graphs Orchestrate Work

#### Graph Execution Model

Each graph is a self-contained workflow with:

1. **Sequential Stages**: Agents execute in defined order
2. **Parallel Execution**: Multiple agents can run simultaneously (e.g., GitPlan + JiraPost)
3. **Checkpointing**: State saved after each significant operation
4. **Suspension/Resume**: Graphs can pause and resume on external events
5. **Error Handling**: Per-agent and per-graph error handling with retries
6. **State Validation**: Ensures only valid state transitions

#### WorkflowOrchestrator Responsibilities

The orchestrator manages the overall workflow lifecycle:

```csharp
// Coordinates transitions between graphs
RefinementGraph → (on RefinementCompleteEvent) → PlanningGraph
PlanningGraph   → (on PlanApprovedEvent)       → ImplementationGraph
ImplementationGraph → Workflow Completed
```

**Key Responsibilities:**
1. **Graph Lifecycle Management**: Start, suspend, resume, cancel workflows
2. **Event-Driven Transitions**: Listen for graph completion events and trigger next graph
3. **State Persistence**: Save/load workflow state across executions
4. **Error Recovery**: Handle graph failures and coordinate retries
5. **Event Publishing**: Emit workflow-level events (suspended, completed, failed, cancelled)

**Why This Separation Matters:**
- Graphs focus on their specific workflow logic
- Orchestrator handles cross-graph concerns
- Each can evolve independently
- Easy to add new graphs without modifying existing ones

#### Graph Communication

Graphs communicate via **typed messages**:

```csharp
// Example message flow:
TriggerTicketMessage → RefinementGraph
  ↓
RefinementCompleteEvent → PlanningGraph
  ↓
AnswersReceivedMessage → PlanningGraph
  ↓
PlanApprovedEvent → ImplementationGraph
  ↓
CompletedMessage
```

**Benefits:**
- Type safety and compile-time validation
- Clear contracts between graphs
- Easy to add new message types
- Enables graph testing with mock messages

---

### Why Flexibility Matters for Future Enhancements

The multi-graph architecture enables future capabilities **without major refactoring**:

#### 1. Advanced Code Review Workflows

**New Graph**: `CodeReviewGraph`

```
PlanningGraph → CodeReviewGraph → [Multiple Review Rounds] → ImplementationGraph
```

- Automated code review agent suggests improvements
- Human reviews and provides feedback
- Agent iterates on implementation before PR creation

#### 2. Testing & Validation Workflows

**New Graph**: `TestingGraph`

```
ImplementationGraph → TestingGraph → [Run Tests, Generate Additional Tests] → Merge or Fix
```

- Automated test generation for new code
- Test execution and result analysis
- Generate additional edge case tests
- Loop back to implementation if tests fail

#### 3. A/B Implementation Strategies

**Enhanced Graph**: `ParallelImplementationGraph`

```
PlanningGraph → [ImplementationGraph_A + ImplementationGraph_B] → ComparisonAgent → Select Winner
```

- Generate code using multiple approaches
- Compare results (code quality, test coverage, performance)
- Human selects best implementation

#### 4. Multi-Stage Approval Workflows

**New Graph**: `ApprovalGraph`

```
PlanningGraph → TeamLeadApproval → ArchitectApproval → SecurityReview → ImplementationGraph
```

- Multi-level approval requirements
- Different approval rules per tenant/repository
- Automated approval for low-risk changes

#### 5. Continuous Refinement Loops

**Enhanced Orchestrator**: Support feedback loops

```
ImplementationGraph → [CodeQualityCheck] → (if issues) → RefinementGraph → Improve
```

- Static analysis of generated code
- Loop back to refinement if quality below threshold
- Automated improvement iterations

#### 6. Deployment & Monitoring Workflows

**New Graph**: `DeploymentGraph`

```
PRMerged → DeploymentGraph → [Deploy to Staging] → [Run E2E Tests] → [Deploy to Prod]
```

- Automated deployment orchestration
- Post-deployment validation
- Rollback on failures

---

### Multi-Tenant Architecture

PRFactory is designed as a **multi-tenant SaaS application**:

#### Tenant Isolation

```
Tenant A                    Tenant B
  ├── Repositories            ├── Repositories
  ├── Tickets                 ├── Tickets
  ├── Credentials (encrypted) ├── Credentials (encrypted)
  └── Configuration           └── Configuration
```

**Isolation Guarantees:**
- Database queries filtered by `TenantId` (EF Core global filters)
- Workspace directories isolated by tenant
- Encrypted credentials per-tenant
- Configuration per-tenant (API limits, feature flags)

#### Tenant-Specific Configuration

```csharp
public class TenantConfiguration
{
    public bool AutoImplementAfterPlanApproval { get; set; }
    public int MaxTokensPerRequest { get; set; }
    public bool EnableCodeReview { get; set; }
    public string[] AllowedRepositories { get; set; }
    // ... more tenant-specific settings
}
```

**Why Configuration Flexibility Matters:**
- Different customers have different approval processes
- Token limits vary by customer subscription tier
- Feature flags enable gradual rollout
- Allows per-tenant customization without code changes

#### Security Considerations

1. **Credential Encryption**: All tokens/secrets encrypted at rest (AES-256)
2. **Workspace Isolation**: Each tenant's cloned repos in separate directories
3. **API Rate Limiting**: Per-tenant rate limits for Claude API
4. **Audit Logging**: All operations logged with tenant context
5. **No Cross-Tenant Data Access**: Enforced at database query level

---

## Multi-Platform Strategy

### The Strategy Pattern Implementation

```
                    IGitPlatformService (Facade)
                              │
                    ┌─────────┴─────────┐
                    │                   │
            LocalGitService    IGitPlatformProvider (Strategy)
            (LibGit2Sharp)              │
                                ┌───────┼───────┐
                                │       │       │
                           GitHub  Bitbucket  Azure DevOps
```

### Platform Selection

The system automatically selects the correct platform provider based on `Repository.GitPlatform` property:

```csharp
// Repository entity:
public class Repository
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string GitPlatform { get; set; } // "GitHub", "Bitbucket", "AzureDevOps", "GitLab"
    public string CloneUrl { get; set; }
    public string AccessToken { get; set; }
    // ...
}

// Platform provider automatically selected:
var pr = await _gitService.CreatePullRequestAsync(repositoryId, request);
// ↑ Uses GitHubProvider if GitPlatform == "GitHub"
// ↑ Uses BitbucketProvider if GitPlatform == "Bitbucket"
// ↑ Uses AzureDevOpsProvider if GitPlatform == "AzureDevOps"
```

### Why Each Platform Matters

#### GitHub
- **Market**: Open source, startups, mid-market
- **Features**: Rich API, excellent documentation, wide adoption
- **Integration**: Octokit .NET library (official)

#### Bitbucket
- **Market**: Atlassian ecosystem, enterprises using Jira
- **Features**: Tight Jira integration, self-hosted options
- **Integration**: REST API (no official .NET SDK)

#### Azure DevOps
- **Market**: Microsoft shops, enterprises with Azure
- **Features**: Integrated platform (repos + work items + pipelines)
- **Integration**: Official Azure DevOps SDK

#### GitLab (Planned)
- **Market**: Enterprises, DevOps-heavy organizations
- **Features**: Complete DevSecOps platform, self-hosted common
- **Integration**: GitLab.NET library available

### Adding a New Platform

To add support for a new platform (example: GitLab):

1. **Create Provider Implementation**:
```csharp
public class GitLabProvider : IGitPlatformProvider
{
    public string PlatformName => "GitLab";

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        // Implementation using GitLab.NET or REST API
    }

    // ... implement other interface methods
}
```

2. **Register in DI**:
```csharp
services.AddScoped<IGitPlatformProvider, GitLabProvider>();
```

3. **Update Repository Configuration**:
```csharp
var repo = new Repository
{
    GitPlatform = "GitLab", // <- New platform
    CloneUrl = "https://gitlab.com/org/repo.git",
    // ...
};
```

4. **That's it!** No changes to:
- Core business logic
- Domain entities
- Workflow graphs
- Agent implementations

---

## Development Guidelines

### For AI Agents Working on This Codebase

#### When Reviewing Code

**ASK YOURSELF:**
1. Is this architectural pattern (graphs, providers, etc.) part of the core design?
2. If I simplify this, will it limit future extensibility?
3. Does this support multi-platform or multi-tenant requirements?
4. Is this overengineering or intentional flexibility?

**RED FLAGS** (Don't simplify these):
- ❌ "This graph system is complex, let's use simple if/else"
- ❌ "We only use GitHub, let's remove provider abstractions"
- ❌ "LibGit2Sharp is overkill, let's use git CLI"
- ❌ "We don't need checkpointing, workflows will finish fast"
- ❌ "Let's merge all agents into one big workflow class"

**GREEN FLAGS** (Safe to simplify):
- ✅ "OpenTelemetry isn't adding value for human-reviewed workflows"
- ✅ "These stub implementations should be completed or removed"
- ✅ "This middleware duplicates logging from another layer"
- ✅ "These configuration keys are unused"

#### When Adding Features

**DO**:
- Add new graphs for new workflow types
- Implement missing platform providers
- Complete stub implementations
- Add configuration options for tenant customization
- Extend existing patterns (don't reinvent)

**DON'T**:
- Bypass the graph system for "quick" workflows
- Hardcode platform-specific logic in core code
- Use shell commands for git operations
- Store credentials unencrypted
- Skip state machine transitions

#### When Refactoring

**SAFE TO REFACTOR**:
- Extract duplicate code into shared utilities
- Improve error messages and logging
- Add type safety and validation
- Improve test coverage
- Optimize performance (without changing architecture)

**RISKY TO REFACTOR** (Verify with product owner):
- Changing graph architecture
- Removing platform providers
- Modifying state machine transitions
- Changing tenant isolation model
- Altering credential encryption

---

## Quick Reference

### Core Architectural Patterns

| Pattern | Purpose | Keep? |
|---------|---------|-------|
| **Multi-Graph Architecture** | Flexible workflow orchestration | ✅ YES |
| **Strategy Pattern (Providers)** | Multi-platform support | ✅ YES |
| **State Machine** | Workflow state management | ✅ YES |
| **Clean Architecture** | Separation of concerns | ✅ YES |
| **Multi-Tenancy** | SaaS isolation | ✅ YES |
| **Checkpoint-Based Resume** | Fault tolerance | ✅ YES |
| **LibGit2Sharp** | Cross-platform git | ✅ YES |
| **OpenTelemetry** | Distributed tracing | ❌ REMOVE |
| **Stub Implementations** | Incomplete code | ⚠️ COMPLETE OR REMOVE |

### File Locations

**Core Architecture**:
- Graphs: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Graphs/`
- Providers: `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/Providers/`
- Domain: `/home/user/PRFactory/src/PRFactory.Domain/`

**Documentation**:
- Architecture: `/home/user/PRFactory/docs/ARCHITECTURE.md`
- Workflow: `/home/user/PRFactory/docs/WORKFLOW.md`
- Setup: `/home/user/PRFactory/docs/SETUP.md`

**Configuration**:
- API: `/home/user/PRFactory/src/PRFactory.Api/appsettings.json`
- Worker: `/home/user/PRFactory/src/PRFactory.Worker/appsettings.json`

---

## Summary

**PRESERVE These Core Decisions:**
1. ✅ Multi-graph architecture (4 graph types + orchestrator)
2. ✅ Multi-platform provider support (GitHub, Bitbucket, Azure DevOps, GitLab)
3. ✅ LibGit2Sharp for git operations
4. ✅ Multi-tenant architecture with configuration flexibility
5. ✅ State machine pattern with checkpointing
6. ✅ Clean Architecture separation

**SIMPLIFY OR REMOVE:**
1. ❌ OpenTelemetry / Jaeger tracing (keep structured logging)
2. ❌ Stub implementations (complete or remove)
3. ❌ Redundant middleware layers
4. ❌ Unused configuration keys

**The Bottom Line:**

This system is designed for **enterprise flexibility** and **future extensibility**. What may look like over-engineering is actually **intentional architecture** to support:
- Multiple platforms (market requirement)
- Complex workflows (future capability)
- Multi-tenant SaaS (business model)
- Fault tolerance (production reliability)

When in doubt, **preserve flexibility** over simplicity. The architecture is optimized for long-term maintainability and feature evolution, not short-term code minimalism.

---

**Questions? Check:**
- [README.md](/home/user/PRFactory/README.md) - Project overview
- [ARCHITECTURE.md](/home/user/PRFactory/docs/ARCHITECTURE.md) - Detailed architecture
- [WORKFLOW.md](/home/user/PRFactory/docs/WORKFLOW.md) - Workflow details

**Still unsure?** Ask the human developer before making structural changes to core architecture.
