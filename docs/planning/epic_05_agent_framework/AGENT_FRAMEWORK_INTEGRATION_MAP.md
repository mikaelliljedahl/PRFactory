# Agent Framework Integration Map for PRFactory

**Date**: November 13, 2025  
**Purpose**: Comprehensive map of where Anthropic Agent Framework agents should integrate with PRFactory's current graph-based agent architecture  
**Scope**: Very thorough analysis of all integration points, data flows, and configuration patterns

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current Architecture Overview](#current-architecture-overview)
3. [Agent Framework Integration Architecture](#agent-framework-integration-architecture)
4. [Integration Points by Workflow Phase](#integration-points-by-workflow-phase)
5. [Specialized Agent Roles & PRFactory Mapping](#specialized-agent-roles--prfactory-mapping)
6. [Data & Message Flow Integration](#data--message-flow-integration)
7. [Configuration & Dependency Injection](#configuration--dependency-injection)
8. [UI/UX Integration Points](#uiux-integration-points)
9. [State Management & Checkpointing](#state-management--checkpointing)
10. [Implementation Roadmap](#implementation-roadmap)

---

## Executive Summary

### Current State
PRFactory implements a **multi-graph workflow orchestration system** with 20+ custom agents, 5 graph types, and checkpoint-based resumption. The system orchestrates work through three phases (Refinement, Planning, Implementation) with optional Code Review.

### Agent Framework Opportunity
Anthropic's Agent Framework provides **specialized, composable agent roles** designed for complex workflows. PRFactory can leverage these agents within its existing graph architecture by:

1. **Wrapping Agent Framework agents** in adapter classes that conform to PRFactory's `BaseAgent` interface
2. **Composing specialized roles** (e.g., CodeExecutor, Analyzer, Planner) into PRFactory's graphs
3. **Maintaining checkpoint-based resumption** across hybrid workflows
4. **Preserving multi-tenancy and security** isolation
5. **Supporting graceful fallback** to custom agents when needed

### Key Benefits
- **Better code execution** - Use Agent Framework's CodeExecutor instead of custom implementation
- **Specialized reasoning** - Leverage role-specific agents for analysis, planning, implementation
- **Production robustness** - Built-in error handling, retry logic, and validation
- **Future flexibility** - Easy to add new specialized roles as they become available
- **Backward compatible** - Hybrid approach maintains existing agents during transition

---

## Current Architecture Overview

### 1. Graph-Based Workflow Orchestration

```
WorkflowOrchestrator (Main coordinator)
├── RefinementGraph (Phase 1: Understand requirements)
│   ├── TriggerAgent
│   ├── RepositoryCloneAgent
│   ├── AnalysisAgent
│   ├── QuestionGenerationAgent
│   ├── JiraPostAgent
│   └── AnswerProcessingAgent
│
├── PlanningGraph (Phase 2: Create implementation plan)
│   ├── PlanningAgent
│   ├── GitPlanAgent
│   └── JiraPostAgent (parallel)
│
├── ImplementationGraph (Phase 3: Code generation)
│   ├── ImplementationAgent
│   ├── GitCommitAgent
│   ├── PullRequestAgent
│   └── JiraPostAgent (parallel)
│
└── CodeReviewGraph (Optional: AI-powered code review)
    ├── CodeReviewAgent
    ├── PostReviewCommentsAgent
    └── PostApprovalCommentAgent
```

**Key Characteristics:**
- **Sequential with parallel branches** - Some stages run in parallel (e.g., GitPlan + JiraPost)
- **Suspension/resumption** - Workflows suspend waiting for human input (questions answered, plans approved)
- **Event-driven transitions** - WorkflowOrchestrator listens to events and routes to next graph
- **Checkpoint persistence** - Each agent stage saves checkpoint for recovery
- **Tenant isolation** - All data tagged with TenantId, queries auto-filtered
- **Multi-LLM support** - Configurable LLM provider per agent and per tenant

### 2. Core Infrastructure

```
Domain Layer (Business Logic)
├── Ticket (aggregate root for workflow)
├── Repository (Git configuration)
├── Tenant (multi-tenancy root)
├── Checkpoint (workflow resumption)
├── TenantLlmProvider (LLM configuration)
└── WorkflowState (workflow tracking)

Application Services
├── ITicketApplicationService
├── IPlanService
├── IRepositoryApplicationService
├── ITenantConfigurationService
├── ITenantLlmProviderService
└── IUserManagementService

Agent Infrastructure
├── BaseAgent (abstract base with logging, retry, checkpoint)
├── AgentGraphBase (graph base with checkpoint management)
├── AgentExecutor (DI-based agent resolution)
├── AgentRegistry (agent type mapping)
├── ICheckpointStore (checkpoint persistence)
└── IWorkflowStateStore (workflow state persistence)

Adapters & Integration
├── ClaudeCodeCliAdapter (LLM execution)
├── ILlmProvider interface (multi-provider abstraction)
├── IGitPlatformProvider (GitHub, Bitbucket, Azure DevOps)
├── ProcessExecutor (safe CLI execution)
└── EventPublisher (SignalR + DB persistence)
```

### 3. Message-Based Communication

```
Agent Messages (IAgentMessage base interface)
├── TriggerTicketMessage
├── AnalysisResultMessage
├── QuestionsGeneratedMessage
├── AnswersReceivedMessage
├── PlanGeneratedMessage
├── PlanApprovedMessage / PlanRejectedMessage
├── CodeGeneratedMessage
├── PullRequestCreatedMessage
└── Custom messages per agent

Message Flow:
Agent1 produces Message1 → Graph passes to Agent2 → Agent2 produces Message2 → Graph passes to Agent3
```

### 4. Checkpoint System

**Purpose**: Enable workflow resumption from exact point of suspension

```
Checkpoint Structure
├── CheckpointId (unique identifier)
├── TicketId (which workflow)
├── TenantId (multi-tenancy)
├── GraphId (which graph)
├── AgentName (which agent created checkpoint)
├── StateJson (serialized execution state)
├── Status (Active, Resumed, Expired)
├── CreatedAt, ResumedAt (timestamps)
└── Data field (arbitrary checkpoint data)

Resume Flow:
1. Workflow suspended at HumanWaitAgent checkpoint
2. External event received (human answer, plan approval)
3. WorkflowOrchestrator calls Graph.ResumeAsync(ticketId, resumeMessage)
4. Graph loads checkpoint from store
5. Graph deserializes state
6. Graph resumes at next agent in sequence
7. New checkpoint created for each subsequent agent
```

### 5. Multi-Tenant & Multi-LLM Architecture

```
Tenant Isolation
├── Each Tenant has isolated repositories, credentials, configuration
├── Global query filters auto-filter by TenantId
├── Encrypted credential storage (AES-256-GCM)
└── Workspace isolation per tenant

LLM Configuration
├── Tenant default LLM provider (e.g., Anthropic Native)
├── Per-agent provider override (Analysis, Planning, Implementation, CodeReview)
├── Ticket-level provider selection (PreferredLlmProviderId)
├── Support for 6+ provider types:
│   ├── Anthropic Native (OAuth)
│   ├── Z.ai unified API
│   ├── Minimax M2
│   ├── OpenRouter
│   ├── Together AI
│   └── Custom providers
```

---

## Agent Framework Integration Architecture

### 1. Adapter Pattern for Agent Framework Integration

**Design**: Wrap Agent Framework agents in adapters that implement PRFactory's `BaseAgent` interface

```
┌─────────────────────────────────────────────────────────────────────┐
│                  PRFactory Workflow Graph                            │
└────────────────────────────────────┬────────────────────────────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
            ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
            │ Custom Agent │ │   AF Adapter │ │ Custom Agent │
            │              │ │              │ │              │
            └──────────────┘ └──────┬───────┘ └──────────────┘
                                    │
                        ┌───────────┴─────────────┐
                        │                         │
                ┌──────────────────────┐ ┌──────────────────────┐
                │  Agent Framework     │ │  Agent Framework     │
                │  Agent (e.g., Code   │ │  Agent (e.g.,        │
                │  Executor)           │ │  Analyzer)           │
                └──────────────────────┘ └──────────────────────┘

Adapter Responsibilities:
1. Convert PRFactory IAgentMessage to AF agent input format
2. Execute Agent Framework agent (with checkpointing)
3. Convert AF agent output to PRFactory IAgentMessage
4. Handle errors, retries, timeouts
5. Persist checkpoints
```

### 2. Agent Framework Agent Types & PRFactory Mapping

```
Agent Framework Specialized Roles → PRFactory Use Cases

┌─────────────────────────────────────────────────────────────────────────────┐
│ REFINEMENT PHASE (Understanding Requirements)                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ Analyzer                                                                     │
│ ├─ Current: AnalysisAgent (custom)                                          │
│ ├─ AF Integration: Use AF Analyzer for codebase analysis                    │
│ ├─ Benefits: Better code understanding, structured output                   │
│ ├─ Location: Replace/enhance AnalysisAgent in RefinementGraph              │
│ └─ Checkpoint: Save analysis results and repository context                 │
│                                                                              │
│ Questioner (planned AF role)                                                │
│ ├─ Current: QuestionGenerationAgent (custom)                                │
│ ├─ AF Integration: Use AF Questioner for clarifying questions               │
│ ├─ Benefits: More nuanced question generation, context awareness            │
│ ├─ Location: Replace QuestionGenerationAgent in RefinementGraph            │
│ └─ Checkpoint: Save generated questions and rationale                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ PLANNING PHASE (Create Implementation Plan)                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ Planner                                                                      │
│ ├─ Current: PlanningAgent (custom)                                          │
│ ├─ AF Integration: Use AF Planner for detailed implementation plans         │
│ ├─ Benefits: Structured planning, better decomposition, validation          │
│ ├─ Location: Replace/enhance PlanningAgent in PlanningGraph                │
│ ├─ Checkpoint: Save generated plan, architectural decisions                 │
│ └─ Approval gate: Human reviews plan, provides feedback                     │
│                                                                              │
│ CodeExecutor (for plan validation)                                          │
│ ├─ Current: Not used (optional simulation)                                  │
│ ├─ AF Integration: Use AF CodeExecutor to validate plan syntax              │
│ ├─ Benefits: Pre-implementation validation, error detection                 │
│ ├─ Location: Optional post-PlanningAgent in PlanningGraph                  │
│ └─ Checkpoint: Save validation results                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ IMPLEMENTATION PHASE (Code Generation)                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ CodeExecutor                                                                 │
│ ├─ Current: ImplementationAgent (custom CLI to Claude)                      │
│ ├─ AF Integration: Use AF CodeExecutor for actual code generation          │
│ ├─ Benefits: Better testing, artifact management, error handling            │
│ ├─ Location: Replace ImplementationAgent in ImplementationGraph            │
│ ├─ Checkpoint: Save generated code, test results                            │
│ └─ Iteration: Loop back to planning if tests fail                           │
│                                                                              │
│ Reviewer (planned AF role)                                                  │
│ ├─ Current: CodeReviewAgent (custom)                                        │
│ ├─ AF Integration: Use AF Reviewer for code review                          │
│ ├─ Benefits: More sophisticated review logic, cross-provider                │
│ ├─ Location: Replace CodeReviewAgent in CodeReviewGraph                    │
│ └─ Checkpoint: Save review results, issue tracking                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ SUPPORTING ROLES (Applicable Across Phases)                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ Artifact Manager                                                             │
│ ├─ Current: GitCommitAgent + PullRequestAgent (custom)                     │
│ ├─ AF Integration: Use AF ArtifactManager for file management               │
│ ├─ Benefits: Better artifact tracking, version management                   │
│ ├─ Location: Enhance GitCommitAgent, PullRequestAgent                      │
│ └─ Checkpoint: Track artifact versions, git operations                      │
│                                                                              │
│ Executor (Process execution)                                                │
│ ├─ Current: ProcessExecutor service + ClaudeCodeCliAdapter                 │
│ ├─ AF Integration: Use AF Executor for safe command execution               │
│ ├─ Benefits: Better sandboxing, timeout handling, resource limits           │
│ ├─ Location: Enhance ProcessExecutor, CLI adapters                          │
│ └─ Checkpoint: Save execution logs, outputs                                 │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Integration Points by Workflow Phase

### Phase 1: Refinement Graph

**Current Flow:**
```
Trigger → Clone → Analysis → Questions → JiraPost → HumanWait → AnswerProcessing → Complete
```

**Integration Points:**

#### 1.1 Analysis Stage
```
CURRENT IMPLEMENTATION:
┌──────────────────────────────────────────┐
│ AnalysisAgent (custom)                   │
├──────────────────────────────────────────┤
│ - Clone repository                       │
│ - Run codebase analysis with Claude      │
│ - Extract key findings                   │
│ - Identify technical constraints         │
│ - Generate analysis result message       │
└──────────────────────────────────────────┘

AGENT FRAMEWORK INTEGRATION:
┌──────────────────────────────────────────┐
│ AnalysisAgentAdapter (new)               │
├──────────────────────────────────────────┤
│ Inherits: BaseAgent                      │
│ Wraps: AF.Analyzer agent                 │
│                                          │
│ Execution:                               │
│ 1. Restore checkpoint if resumed         │
│ 2. Prepare repository context            │
│ 3. Invoke AF Analyzer with repo path     │
│ 4. Collect analysis output               │
│ 5. Validate analysis completeness        │
│ 6. Create AnalysisResultMessage          │
│ 7. Save checkpoint                       │
│                                          │
│ Checkpoint Data:                         │
│ ├─ repository_state (SHA, branches)      │
│ ├─ analysis_findings (JSON)              │
│ ├─ technical_constraints                 │
│ └─ execution_time (duration)             │
└──────────────────────────────────────────┘

MESSAGE FLOW:
Input:  TriggerTicketMessage
Output: AnalysisResultMessage
  {
    ticketId: Guid,
    analysisFindings: {
      codebase_structure: string,
      key_modules: string[],
      dependencies: string[],
      architectural_patterns: string[],
      technical_constraints: string[]
    },
    execution_time: TimeSpan,
    repository_context: {
      path: string,
      branch: string,
      commit_sha: string
    }
  }

CONFIGURATION:
services.AddScoped<IAnalysisAgent, AnalysisAgentAdapter>(
  sp => new AnalysisAgentAdapter(
    logger: sp.GetRequiredService<ILogger<AnalysisAgentAdapter>>(),
    afClient: sp.GetRequiredService<IAgentFrameworkClient>(),
    checkpointStore: sp.GetRequiredService<ICheckpointStore>(),
    localGitService: sp.GetRequiredService<ILocalGitService>(),
    configuration: sp.GetRequiredService<IOptions<AnalysisAgentConfig>>()
  )
);
```

#### 1.2 Question Generation Stage
```
CURRENT IMPLEMENTATION:
┌──────────────────────────────────────────┐
│ QuestionGenerationAgent (custom)         │
├──────────────────────────────────────────┤
│ - Take analysis findings                 │
│ - Generate clarifying questions          │
│ - Post to Jira/Webhook                   │
│ - Create questions in database           │
└──────────────────────────────────────────┘

AGENT FRAMEWORK INTEGRATION:
┌──────────────────────────────────────────┐
│ QuestionAgentAdapter (new)               │
├──────────────────────────────────────────┤
│ Inherits: BaseAgent                      │
│ Wraps: AF.Questioner agent               │
│                                          │
│ Execution:                               │
│ 1. Load analysis from checkpoint         │
│ 2. Invoke AF Questioner with context     │
│ 3. Collect generated questions           │
│ 4. Validate question quality             │
│ 5. Create Question entities              │
│ 6. Return QuestionsGeneratedMessage      │
│ 7. Save checkpoint                       │
│                                          │
│ Checkpoint Data:                         │
│ ├─ analysis_summary                      │
│ ├─ questions_generated (JSON)            │
│ └─ generation_timestamp                  │
└──────────────────────────────────────────┘
```

### Phase 2: Planning Graph

**Current Flow:**
```
Planning → GitPlan (parallel) + JiraPost → Approval Gate → Complete
```

**Integration Points:**

#### 2.1 Planning Stage
```
CURRENT IMPLEMENTATION:
┌──────────────────────────────────────────┐
│ PlanningAgent (custom)                   │
├──────────────────────────────────────────┤
│ - Take analysis + answers                │
│ - Generate implementation plan           │
│ - Create plan markdown files             │
│ - Commit plan to git branch              │
└──────────────────────────────────────────┘

AGENT FRAMEWORK INTEGRATION:
┌──────────────────────────────────────────┐
│ PlannerAgentAdapter (new)                │
├──────────────────────────────────────────┤
│ Inherits: BaseAgent                      │
│ Wraps: AF.Planner agent                  │
│                                          │
│ Execution:                               │
│ 1. Load analysis + answers from DB       │
│ 2. Invoke AF Planner with full context   │
│ 3. Collect generated plan                │
│ 4. Validate plan completeness            │
│ 5. Format plan as markdown               │
│ 6. Create Plan entity in database        │
│ 7. Return PlanGeneratedMessage           │
│ 8. Save checkpoint                       │
│                                          │
│ Checkpoint Data:                         │
│ ├─ plan_content (markdown)               │
│ ├─ plan_structure (decomposition)        │
│ ├─ estimated_effort                      │
│ └─ risk_assessment                       │
│                                          │
│ Integration with GitPlanAgent:           │
│ - PlannerAgentAdapter outputs plan      │
│ - GitPlanAgent commits plan to branch   │
│ - Both run via Task.WhenAll in graph    │
└──────────────────────────────────────────┘
```

#### 2.2 Plan Validation (Optional)
```
NEW OPTIONAL STAGE:

┌──────────────────────────────────────────┐
│ PlanValidationAgentAdapter (optional)    │
├──────────────────────────────────────────┤
│ Inherits: BaseAgent                      │
│ Wraps: AF.CodeExecutor agent             │
│                                          │
│ Purpose:                                 │
│ - Validate plan syntax/structure         │
│ - Check plan against codebase            │
│ - Identify potential issues early        │
│ - Provide feedback before human review   │
│                                          │
│ When to Use:                             │
│ - After PlanningAgent                    │
│ - Before human approval                  │
│ - For high-risk changes                  │
│                                          │
│ Checkpoint Data:                         │
│ ├─ validation_results                    │
│ ├─ found_issues (if any)                 │
│ └─ validation_timestamp                  │
└──────────────────────────────────────────┘
```

### Phase 3: Implementation Graph

**Current Flow:**
```
Implementation → GitCommit → PullRequest (parallel) + JiraPost → Complete
```

**Integration Points:**

#### 3.1 Code Implementation Stage
```
CURRENT IMPLEMENTATION:
┌──────────────────────────────────────────┐
│ ImplementationAgent (custom)             │
├──────────────────────────────────────────┤
│ - Take approved plan                     │
│ - Generate code via ClaudeCodeCliAdapter │
│ - Execute tests                          │
│ - Return code artifacts                  │
└──────────────────────────────────────────┘

AGENT FRAMEWORK INTEGRATION:
┌──────────────────────────────────────────┐
│ CodeExecutorAgentAdapter (new)           │
├──────────────────────────────────────────┤
│ Inherits: BaseAgent                      │
│ Wraps: AF.CodeExecutor agent             │
│                                          │
│ Execution:                               │
│ 1. Load approved plan from database      │
│ 2. Prepare repository context            │
│ 3. Invoke AF CodeExecutor with plan      │
│ 4. Collect generated code                │
│ 5. Run tests (embedded in AF execution)  │
│ 6. Validate test results                 │
│ 7. Create CodeGeneratedMessage           │
│ 8. Save checkpoint with code artifacts   │
│                                          │
│ Checkpoint Data:                         │
│ ├─ generated_code (file contents)        │
│ ├─ test_results (pass/fail)              │
│ ├─ coverage_metrics                      │
│ ├─ execution_logs                        │
│ └─ artifact_references                   │
│                                          │
│ Iteration Loop:                          │
│ If tests fail:                           │
│ - CodeReviewGraph detects issues         │
│ - Loop back to CodeExecutorAgentAdapter  │
│ - Max 3 iterations (configurable)        │
│ - Provide failure feedback to agent      │
└──────────────────────────────────────────┘

MESSAGE FLOW:
Input:  PlanApprovedMessage
Output: CodeGeneratedMessage
  {
    ticketId: Guid,
    generated_code: {
      files: { path: string, content: string }[],
      new_files_count: int,
      modified_files_count: int
    },
    test_results: {
      total_tests: int,
      passed: int,
      failed: int,
      skipped: int,
      success: bool
    },
    execution_time: TimeSpan,
    artifact_location: string
  }
```

#### 3.2 Code Review Stage
```
CURRENT IMPLEMENTATION:
┌──────────────────────────────────────────┐
│ CodeReviewGraph                          │
│ ├─ CodeReviewAgent (custom)              │
│ ├─ PostReviewCommentsAgent (custom)      │
│ └─ PostApprovalCommentAgent (custom)     │
├──────────────────────────────────────────┤
│ - Analyze generated code                 │
│ - Identify issues and improvements       │
│ - Post comments to PR                    │
│ - Loop back if issues found              │
└──────────────────────────────────────────┘

AGENT FRAMEWORK INTEGRATION:
┌──────────────────────────────────────────┐
│ ReviewerAgentAdapter (new)               │
├──────────────────────────────────────────┤
│ Inherits: BaseAgent                      │
│ Wraps: AF.Reviewer agent                 │
│                                          │
│ Execution:                               │
│ 1. Load generated code from PR           │
│ 2. Fetch PR details from git platform    │
│ 3. Invoke AF Reviewer with code context  │
│ 4. Collect review findings               │
│ 5. Extract critical issues               │
│ 6. Create CodeReviewResult entity        │
│ 7. Post review comments to PR            │
│ 8. Save checkpoint                       │
│                                          │
│ Checkpoint Data:                         │
│ ├─ review_findings (JSON)                │
│ ├─ critical_issues (array)               │
│ ├─ suggestions (array)                   │
│ ├─ praise_points (array)                 │
│ └─ review_timestamp                      │
│                                          │
│ Cross-Provider Support:                  │
│ - Reviewer provider can differ from      │
│    CodeExecutor provider                 │
│ - E.g., GPT-4 reviews Claude-generated   │
│    code                                  │
│                                          │
│ Iteration:                               │
│ If critical issues found:                │
│ 1. Post detailed comments to PR          │
│ 2. Loop to CodeExecutorAgentAdapter      │
│ 3. Pass review findings as context       │
│ 4. Max 3 iterations (configurable)       │
└──────────────────────────────────────────┘
```

---

## Specialized Agent Roles & PRFactory Mapping

### Role: Analyzer
**AF Description**: Analyzes code, documents, and systems to understand structure and behavior

**PRFactory Current Usage**: `AnalysisAgent`  
**PRFactory Location**: RefinementGraph - Stage 3  
**AF Integration**: Replace with `AnalysisAgentAdapter` wrapping AF Analyzer

**Responsibilities**:
- Analyze repository structure and dependencies
- Extract architectural patterns
- Identify technical constraints
- Generate structured analysis output
- Store findings for downstream agents

**Checkpoint Requirements**:
- Repository state (SHA, branch, path)
- Analysis findings (JSON structure)
- Execution time and resource usage

**Multi-Tenant Considerations**:
- Repository credentials (OAuth2 or PAT)
- Workspace isolation per tenant
- Encrypted credential storage

---

### Role: Planner
**AF Description**: Creates structured plans and breaks down complex tasks

**PRFactory Current Usage**: `PlanningAgent`  
**PRFactory Location**: PlanningGraph - Stage 1  
**AF Integration**: Replace with `PlannerAgentAdapter` wrapping AF Planner

**Responsibilities**:
- Take analysis and human answers
- Generate detailed implementation plan
- Decompose into subtasks
- Estimate effort and risk
- Create markdown plan files
- Return structured plan message

**Checkpoint Requirements**:
- Plan content (markdown)
- Plan structure (task decomposition)
- Risk assessments
- Effort estimates
- Approval status tracking

**Iteration Loop**:
- Human reviews plan in UI
- Provides feedback/approval/rejection
- If rejected: retry planning with feedback
- Max 5 retries (configurable)

---

### Role: CodeExecutor
**AF Description**: Executes code generation, testing, and artifact management

**PRFactory Current Usage**: `ImplementationAgent`  
**PRFactory Location**: ImplementationGraph - Stage 1  
**AF Integration**: Replace with `CodeExecutorAgentAdapter` wrapping AF CodeExecutor

**Responsibilities**:
- Generate code from approved plan
- Execute tests automatically
- Manage code artifacts
- Handle file operations
- Provide detailed execution logs
- Return generated code message

**Checkpoint Requirements**:
- Generated code (file contents)
- Test results (pass/fail metrics)
- Coverage metrics
- Execution logs
- Artifact references

**Iteration Loop**:
- CodeReviewGraph analyzes generated code
- If critical issues: feedback to CodeExecutor
- Retry implementation with issue context
- Max 3 iterations (configurable)
- Fallback to manual implementation if needed

---

### Role: Reviewer (Planned AF Role)
**AF Description**: Reviews code and provides constructive feedback

**PRFactory Current Usage**: `CodeReviewAgent` + `PostReviewCommentsAgent`  
**PRFactory Location**: CodeReviewGraph  
**AF Integration**: Replace with `ReviewerAgentAdapter` wrapping AF Reviewer

**Responsibilities**:
- Analyze generated code
- Identify bugs and issues
- Suggest improvements
- Validate against best practices
- Generate structured review feedback
- Post comments to PR

**Checkpoint Requirements**:
- Review findings (JSON)
- Critical issues identified
- Suggestions for improvement
- Praise points (positive feedback)
- Review timestamp and metadata

**Features**:
- Cross-provider support (GPT-4 reviews Claude code)
- Configurable review depth
- Custom review criteria per tenant
- Auto-approval if no issues found

---

### Supporting Roles

#### Artifact Manager
**AF Role**: Manages code artifacts, versions, and dependencies

**PRFactory Integration Points**:
- `GitCommitAgent` - Commit code to repository
- `PullRequestAgent` - Create PR with artifacts
- Plan file management in planning phase

**Checkpoint Requirements**:
- File operation logs
- Artifact versions
- Git operation details

---

#### Executor
**AF Role**: Executes commands safely within resource constraints

**PRFactory Integration Points**:
- `ProcessExecutor` service - Run tests, CLI commands
- `ClaudeCodeCliAdapter` - Execute Claude Code CLI
- Test execution in CodeExecutor

**Checkpoint Requirements**:
- Execution logs
- Command history
- Output/error streams
- Resource usage

---

## Data & Message Flow Integration

### 1. Message Type Extensions for AF Integration

**Current Base Class**:
```csharp
public interface IAgentMessage
{
    Guid TicketId { get; }
    Guid TenantId { get; }
    DateTime CreatedAt { get; }
}
```

**New AF-Specific Messages**:
```csharp
// Analysis phase
public class AnalysisResultMessage : IAgentMessage
{
    public AnalysisFindings Findings { get; set; } = new();
    public RepositoryContext Repository { get; set; } = new();
}

// Planning phase
public class PlanGeneratedMessage : IAgentMessage
{
    public string PlanContent { get; set; } = string.Empty;
    public PlanStructure Structure { get; set; } = new();
    public List<string> Files { get; set; } = new();
}

// Implementation phase
public class CodeGeneratedMessage : IAgentMessage
{
    public List<FileArtifact> Files { get; set; } = new();
    public TestResults Tests { get; set; } = new();
    public ExecutionMetrics Metrics { get; set; } = new();
}

// Code review phase
public class CodeReviewMessage : IAgentMessage
{
    public List<ReviewIssue> Issues { get; set; } = new();
    public List<ReviewSuggestion> Suggestions { get; set; } = new();
    public List<ReviewPraise> Praise { get; set; } = new();
}
```

### 2. Checkpoint Data Models for AF Agents

**Pattern**:
```csharp
public class AfAgentCheckpointData
{
    // AF-specific checkpoint fields
    public string AgentFrameworkExecutionId { get; set; } = string.Empty;
    public string AgentFrameworkRole { get; set; } = string.Empty;
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
    
    // AF output cache
    public string OutputJson { get; set; } = string.Empty;
    
    // PRFactory integration
    public DateTime CheckpointCreatedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
```

### 3. State Flow Through Graphs

```
RefinementGraph State Transitions:
┌─────────────────────────────────────────────────────────────────┐
│ REFINEMENT GRAPH STATE MACHINE                                  │
└─────────────────────────────────────────────────────────────────┘

Checkpoint: TriggerTicketMessage
    ↓
[TriggerAgent] → Checkpoint: ticket_triggered
    ↓
[RepositoryCloneAgent] → Checkpoint: repo_cloned
    ↓
[AnalysisAgentAdapter] → Checkpoint: analysis_complete
    (wraps AF Analyzer)
    ↓
[QuestionAgentAdapter] → Checkpoint: questions_generated
    (wraps AF Questioner)
    ↓
[JiraPostAgent] → Checkpoint: questions_posted
    (optional, syncs to Jira)
    ↓
[HumanWaitAgent] → Checkpoint: awaiting_answers (SUSPENDED)
    ↓
(Webhook received: AnswersReceivedMessage)
    ↓
[AnswerProcessingAgent] → Checkpoint: answers_received
    ↓
Emit: RefinementCompleteEvent
    ↓
WorkflowOrchestrator: Transition to PlanningGraph


PlanningGraph State Transitions:
┌─────────────────────────────────────────────────────────────────┐
│ PLANNING GRAPH STATE MACHINE                                    │
└─────────────────────────────────────────────────────────────────┘

Checkpoint: AnswersReceivedMessage
    ↓
[PlannerAgentAdapter] → Checkpoint: plan_generated
    (wraps AF Planner)
    ↓
Parallel Execution:
├─ [GitPlanAgent] → Checkpoint: plan_committed
│  └─ Commit plan markdown to branch
│
└─ [JiraPostAgent] → Checkpoint: plan_posted
   └─ Sync plan to Jira

Convergence:
    ↓
[ApprovalCheckAgent] → Checkpoint: plan_approved
    (Human approval in UI)
    ↓
Emit: PlanApprovedEvent
    ↓
WorkflowOrchestrator: Transition to ImplementationGraph


ImplementationGraph State Transitions:
┌─────────────────────────────────────────────────────────────────┐
│ IMPLEMENTATION GRAPH STATE MACHINE                              │
└─────────────────────────────────────────────────────────────────┘

Checkpoint: PlanApprovedMessage
    ↓
[CodeExecutorAgentAdapter] → Checkpoint: code_generated
    (wraps AF CodeExecutor)
    │ Tests: PASS ✓ → Continue
    │ Tests: FAIL ✗ → Retry (max 3)
    ↓
[GitCommitAgent] → Checkpoint: code_committed
    ↓
Parallel Execution:
├─ [PullRequestAgent] → Checkpoint: pr_created
│  └─ Create PR with generated code
│
└─ [JiraPostAgent] → Checkpoint: pr_posted
   └─ Sync PR link to Jira

Convergence:
    ↓
Emit: ImplementationCompleteEvent
    ↓
WorkflowOrchestrator: Transition to CodeReviewGraph
    (if code_review_enabled=true)


CodeReviewGraph State Transitions:
┌─────────────────────────────────────────────────────────────────┐
│ CODE REVIEW GRAPH STATE MACHINE                                 │
└─────────────────────────────────────────────────────────────────┘

Checkpoint: CodeGeneratedMessage (from Implementation)
    ↓
[ReviewerAgentAdapter] → Checkpoint: review_complete
    (wraps AF Reviewer)
    │ Issues: NONE ✓ → PostApprovalComment
    │ Issues: FOUND ✗ → PostReviewComments → Retry
    ↓
If Issues Found:
├─ [PostReviewCommentsAgent] → Post detailed feedback to PR
│   ↓
│   [ImplementationGraph] → Resume with review feedback
│   │ (Max 3 iterations total)
│   ↓
│   [CodeExecutorAgentAdapter] → Generate fix
│   │
│   [CodeReviewGraph] → Review again
│   ↓
│   (Loop: if still issues, repeat; if resolved, continue)

If No Issues:
└─ [PostApprovalCommentAgent] → Post approval to PR


Final State:
    ↓
Emit: WorkflowCompleteEvent
    ↓
[CompletionAgent] → Checkpoint: workflow_complete
    └─ Cleanup, sync to external systems
    ↓
WORKFLOW COMPLETE
```

---

## Configuration & Dependency Injection

### 1. AF Adapter Registration Pattern

**Location**: `/src/PRFactory.Infrastructure/Agents/Configuration/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Agent Framework agent adapters to the service collection.
    /// Call after AddInfrastructure() to register AF-specific agents.
    /// </summary>
    public static IServiceCollection AddAgentFrameworkAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register AF client (from Anthropic SDK)
        services.AddScoped<IAgentFrameworkClient>(sp =>
            new AgentFrameworkClient(
                apiKey: configuration["Anthropic:ApiKey"]
                    ?? throw new InvalidOperationException("Anthropic:ApiKey not configured"),
                httpClient: sp.GetRequiredService<HttpClient>()
            )
        );

        // Register AF-wrapped adapters for each role
        
        // Analysis Phase
        services.AddScoped<IAnalysisAgent, AnalysisAgentAdapter>(sp =>
            new AnalysisAgentAdapter(
                logger: sp.GetRequiredService<ILogger<AnalysisAgentAdapter>>(),
                afClient: sp.GetRequiredService<IAgentFrameworkClient>(),
                checkpointStore: sp.GetRequiredService<ICheckpointStore>(),
                localGitService: sp.GetRequiredService<ILocalGitService>(),
                options: sp.GetRequiredService<IOptions<AnalysisAgentOptions>>()
            )
        );

        // Planning Phase
        services.AddScoped<IPlanningAgent, PlannerAgentAdapter>(sp =>
            new PlannerAgentAdapter(
                logger: sp.GetRequiredService<ILogger<PlannerAgentAdapter>>(),
                afClient: sp.GetRequiredService<IAgentFrameworkClient>(),
                checkpointStore: sp.GetRequiredService<ICheckpointStore>(),
                ticketService: sp.GetRequiredService<ITicketApplicationService>(),
                options: sp.GetRequiredService<IOptions<PlannerAgentOptions>>()
            )
        );

        // Implementation Phase
        services.AddScoped<ICodeExecutor, CodeExecutorAgentAdapter>(sp =>
            new CodeExecutorAgentAdapter(
                logger: sp.GetRequiredService<ILogger<CodeExecutorAgentAdapter>>(),
                afClient: sp.GetRequiredService<IAgentFrameworkClient>(),
                checkpointStore: sp.GetRequiredService<ICheckpointStore>(),
                localGitService: sp.GetRequiredService<ILocalGitService>(),
                processExecutor: sp.GetRequiredService<IProcessExecutor>(),
                options: sp.GetRequiredService<IOptions<CodeExecutorOptions>>()
            )
        );

        // Code Review Phase
        services.AddScoped<ICodeReviewer, ReviewerAgentAdapter>(sp =>
            new ReviewerAgentAdapter(
                logger: sp.GetRequiredService<ILogger<ReviewerAgentAdapter>>(),
                afClient: sp.GetRequiredService<IAgentFrameworkClient>(),
                checkpointStore: sp.GetRequiredService<ICheckpointStore>(),
                gitPlatformService: sp.GetRequiredService<IGitPlatformService>(),
                options: sp.GetRequiredService<IOptions<ReviewerAgentOptions>>()
            )
        );

        // Configure options from appsettings
        services.Configure<AnalysisAgentOptions>(
            configuration.GetSection("AgentFramework:Analysis"));
        services.Configure<PlannerAgentOptions>(
            configuration.GetSection("AgentFramework:Planning"));
        services.Configure<CodeExecutorOptions>(
            configuration.GetSection("AgentFramework:CodeExecution"));
        services.Configure<ReviewerAgentOptions>(
            configuration.GetSection("AgentFramework:CodeReview"));

        return services;
    }
}
```

### 2. appsettings.json Configuration

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-opus-4-1-20250805"
  },
  "AgentFramework": {
    "Enabled": true,
    "Analysis": {
      "Enabled": true,
      "ModelOverride": null,
      "Timeout": 300,
      "MaxRetries": 3,
      "AnalysisDepth": "detailed",
      "IncludeDependencies": true
    },
    "Planning": {
      "Enabled": true,
      "ModelOverride": null,
      "Timeout": 600,
      "MaxRetries": 5,
      "DecompositionLevel": "detailed",
      "IncludeRiskAssessment": true
    },
    "CodeExecution": {
      "Enabled": true,
      "ModelOverride": null,
      "Timeout": 900,
      "MaxRetries": 3,
      "IncludeTests": true,
      "RunCoverage": true,
      "TestTimeout": 300
    },
    "CodeReview": {
      "Enabled": true,
      "ModelOverride": "gpt-4",
      "Timeout": 600,
      "MaxRetries": 2,
      "ReviewSeverity": "strict",
      "AutoApproveIfNoIssues": true,
      "CrossProviderReview": true
    }
  }
}
```

### 3. Graph Registration Updates

**Location**: `/src/PRFactory.Infrastructure/Agents/Graphs/GraphBuilder.cs`

```csharp
public static class GraphBuilder
{
    /// <summary>
    /// Registers all workflow graphs with AF adapter support.
    /// </summary>
    public static IServiceCollection AddGraphs(this IServiceCollection services)
    {
        // Existing graph registrations
        services.AddScoped<RefinementGraph>();
        services.AddScoped<PlanningGraph>();
        services.AddScoped<ImplementationGraph>();
        services.AddScoped<CodeReviewGraph>();
        services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

        // Register graph instances as IAgentGraph for dynamic resolution
        services.AddScoped<IAgentGraph, RefinementGraph>(sp => 
            sp.GetRequiredService<RefinementGraph>());
        services.AddScoped<IAgentGraph, PlanningGraph>(sp => 
            sp.GetRequiredService<PlanningGraph>());
        services.AddScoped<IAgentGraph, ImplementationGraph>(sp => 
            sp.GetRequiredService<ImplementationGraph>());
        services.AddScoped<IAgentGraph, CodeReviewGraph>(sp => 
            sp.GetRequiredService<CodeReviewGraph>());

        return services;
    }
}
```

---

## UI/UX Integration Points

### 1. Workflow Status Display

**New Components Needed**:
```
/src/PRFactory.Web/Components/Workflow/
├── AgentExecutionStatus.razor          (Show current AF agent execution)
├── CheckpointTimeline.razor            (Timeline of checkpoints)
├── AgentFeedbackDisplay.razor          (Show iteration feedback)
└── ExecutionMetricsPanel.razor         (Show execution time, cost, tokens)
```

**Data Binding**:
```csharp
[Parameter]
public Guid TicketId { get; set; }

[Inject]
public IWorkflowStateStore WorkflowStateStore { get; set; } = null!;

[Inject]
public ICheckpointStore CheckpointStore { get; set; } = null!;

protected override async Task OnInitializedAsync()
{
    // Load workflow state
    var state = await WorkflowStateStore.GetByTicketIdAsync(TicketId);
    
    // Load recent checkpoints
    var checkpoints = await CheckpointStore.GetCheckpointsAsync(TicketId.ToString());
    
    // Update UI in real-time via SignalR
    _eventBroadcaster.Subscribe<WorkflowStateChangedEvent>(
        state => this.StateHasChanged()
    );
}
```

### 2. Agent Configuration UI

**New Pages Needed**:
```
/admin/agent-configuration/
├── Index.razor                         (List AF agents with status)
├── Configure.razor                     (Configure AF agent parameters)
└── Metrics.razor                       (View agent execution metrics)
```

**Configuration Options Per Agent**:
- Model selection (GPT-4, Claude, custom)
- Timeout settings
- Retry behavior
- Validation rules
- Cross-provider settings

### 3. Iteration Loop UI

**New Components**:
```
/src/PRFactory.Web/Components/Workflow/
├── IterationFeedback.razor             (Show why code iteration happened)
├── ReviewFeedbackDisplay.razor         (Show code review comments)
└── RetryHistory.razor                  (Timeline of retries)
```

---

## State Management & Checkpointing

### 1. AF Checkpoint Serialization

**Challenge**: AF agents produce rich, potentially large outputs

**Solution**: Structured checkpoint data with compression

```csharp
public class AgentFrameworkCheckpoint
{
    /// AF-specific data
    public string AgentRole { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
    
    /// Compressed output cache
    [Serializable]
    public byte[] CompressedOutput { get; set; } = Array.Empty<byte>();
    
    /// Metadata for quick status checks
    public ExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan Duration { get; set; }
    
    /// Resume information
    public string? ResumeReason { get; set; }
    public Dictionary<string, object> ResumeContext { get; set; } = new();
}
```

### 2. Checkpoint Migration Strategy

**Current**: Checkpoint entity stores StateJson (uncompressed)  
**New**: Support both current format and AF-compressed format

```csharp
// Migration helper
public class CheckpointVersionAdapter
{
    public static async Task<Dictionary<string, object>> DecompressCheckpointAsync(
        string stateJson, 
        int version)
    {
        if (version == 1)
        {
            // Current format - parse JSON directly
            return JsonSerializer.Deserialize<Dictionary<string, object>>(stateJson) ?? new();
        }
        else if (version == 2)
        {
            // AF format - decompress then parse
            var bytes = Convert.FromBase64String(stateJson);
            using var stream = new MemoryStream(bytes);
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        }
        
        throw new InvalidOperationException($"Unsupported checkpoint version: {version}");
    }
}
```

---

## Implementation Roadmap

### Phase 1: Foundation (Weeks 1-2)

**Objectives**: Establish adapter pattern and basic AF integration

```
Week 1:
├─ Create AF SDK integration layer
├─ Implement BaseAgentFrameworkAdapter abstract class
├─ Add checkpoint compression/decompression helpers
├─ Update DI configuration for AF services
└─ Add unit tests for adapters

Week 2:
├─ Implement AnalysisAgentAdapter (wraps AF Analyzer)
├─ Update RefinementGraph to use AnalysisAgentAdapter
├─ Add AF-specific message types (AnalysisResultMessage)
├─ Integration test: Trigger → Analysis → Questions flow
└─ Document AF adapter pattern
```

**Deliverables**:
- AnalysisAgentAdapter fully functional
- Integration tests passing
- Documentation of adapter pattern
- Configuration schema for AF agents

---

### Phase 2: Planning Integration (Weeks 3-4)

**Objectives**: Integrate AF Planner into planning workflow

```
Week 3:
├─ Implement PlannerAgentAdapter (wraps AF Planner)
├─ Update PlanningGraph to use PlannerAgentAdapter
├─ Add plan validation (optional CodeExecutor integration)
├─ Implement iteration loop for plan rejection
└─ Add checkpointing for plan generation

Week 4:
├─ Integration test: Analysis → Planning → Git Commit flow
├─ Add admin UI for planning configuration
├─ Performance testing and optimization
└─ Document planning phase integration
```

**Deliverables**:
- PlannerAgentAdapter fully functional
- Plan validation optional stage
- Integration tests passing
- Admin UI for configuration

---

### Phase 3: Code Execution (Weeks 5-7)

**Objectives**: Replace custom implementation with AF CodeExecutor

```
Week 5:
├─ Implement CodeExecutorAgentAdapter (wraps AF CodeExecutor)
├─ Update ImplementationGraph to use CodeExecutorAgentAdapter
├─ Add test result processing and artifact management
├─ Implement iteration loop for test failures
└─ Add execution metrics collection

Week 6:
├─ Integration test: Full Refinement → Planning → Implementation flow
├─ Add code coverage reporting
├─ Performance benchmarking
├─ Add execution metrics to UI
└─ Document implementation phase

Week 7:
├─ Stress testing with large codebases
├─ Timeout and cancellation testing
├─ Memory usage optimization
└─ Final integration testing
```

**Deliverables**:
- CodeExecutorAgentAdapter fully functional
- Full E2E workflow working
- Execution metrics and reporting
- Performance optimizations

---

### Phase 4: Code Review (Weeks 8-9)

**Objectives**: Integrate AF Reviewer for code review

```
Week 8:
├─ Implement ReviewerAgentAdapter (wraps AF Reviewer)
├─ Update CodeReviewGraph to use ReviewerAgentAdapter
├─ Implement cross-provider review support
├─ Add review iteration loop (max 3 iterations)
└─ Add review metrics and reporting

Week 9:
├─ Integration test: Full workflow including code review
├─ Add code review UI components
├─ Performance testing
├─ Admin configuration UI
└─ Complete documentation
```

**Deliverables**:
- ReviewerAgentAdapter fully functional
- Full workflow with code review
- UI for code review feedback
- Comprehensive documentation

---

### Phase 5: Production Hardening (Weeks 10-12)

**Objectives**: Production-ready implementation

```
Week 10:
├─ Security audit of AF integration
├─ Credential handling review
├─ Multi-tenant isolation verification
├─ Load testing
└─ Cost optimization (API calls, tokens)

Week 11:
├─ Comprehensive E2E testing
├─ Fallback mechanisms testing
├─ Error handling and recovery
├─ Documentation updates
└─ Training materials

Week 12:
├─ Performance monitoring setup
├─ Alerting configuration
├─ Runbook creation
├─ Final testing and validation
└─ Release preparation
```

**Deliverables**:
- Production-ready AF integration
- Comprehensive monitoring
- Security audit passed
- Complete documentation

---

## Key Integration Patterns

### Pattern 1: Adapter Implementation Template

```csharp
/// <summary>
/// Adapter for [Agent Framework Role] to PRFactory BaseAgent
/// Wraps AF [Role] for use within workflow graphs
/// </summary>
public class [RoleNameAgentAdapter] : BaseAgent
{
    private readonly IAgentFrameworkClient _afClient;
    private readonly ICheckpointStore _checkpointStore;
    private readonly ILogger<[RoleNameAgentAdapter]> _logger;
    private readonly IOptions<[RoleNameOptions]> _options;

    public override string Name => "[Role Name] Agent (AF)";
    public override string Description => "AF-powered [role] for [workflow phase]";

    public [RoleNameAgentAdapter](
        ILogger<[RoleNameAgentAdapter]> logger,
        IAgentFrameworkClient afClient,
        ICheckpointStore checkpointStore,
        // Additional dependencies...
        IOptions<[RoleNameOptions]> options)
        : base(logger)
    {
        _afClient = afClient ?? throw new ArgumentNullException(nameof(afClient));
        _checkpointStore = checkpointStore ?? throw new ArgumentNullException(nameof(checkpointStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting {AgentName} for ticket {TicketId}", Name, context.TicketId);

            // 1. Load checkpoint if resuming
            var checkpoint = await RestoreCheckpointAsync(context, cancellationToken);
            if (checkpoint != null)
            {
                // Resume from checkpoint - reconstruct state
                var cachedOutput = DecompressCheckpointData(checkpoint.Data);
                return new AgentResult
                {
                    Status = AgentStatus.Success,
                    Data = cachedOutput,
                    Error = null
                };
            }

            // 2. Prepare AF input from context
            var afInput = PrepareAfInput(context);

            // 3. Call AF agent
            _logger.LogDebug("Invoking AF agent for {Role}", Name);
            var afOutput = await _afClient.ExecuteAgentAsync(
                role: GetAfRole(),
                input: afInput,
                timeout: _options.Value.TimeoutSeconds,
                cancellationToken: cancellationToken
            );

            // 4. Validate AF output
            if (afOutput == null)
            {
                throw new InvalidOperationException("AF agent returned null output");
            }

            // 5. Convert AF output to PRFactory message
            var result = ConvertAfOutputToPrFactoryMessage(afOutput);

            // 6. Save checkpoint
            var checkpointData = CompressCheckpointData(afOutput);
            await SaveCheckpointAsync(context, checkpointData, cancellationToken);

            _logger.LogInformation("Completed {AgentName} for ticket {TicketId}", Name, context.TicketId);

            return new AgentResult
            {
                Status = AgentStatus.Success,
                Data = result,
                Error = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed executing {AgentName} for ticket {TicketId}", Name, context.TicketId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Data = null,
                Error = ex.Message
            };
        }
    }

    private string GetAfRole() => "[AF role name]";

    private object PrepareAfInput(AgentContext context)
    {
        // Prepare input object for AF agent
        return new { /* ... */ };
    }

    private IAgentMessage ConvertAfOutputToPrFactoryMessage(object afOutput)
    {
        // Convert AF output to PRFactory message type
        return new [MessageType] { /* ... */ };
    }

    private byte[] CompressCheckpointData(object data)
    {
        var json = JsonSerializer.Serialize(data);
        using var stream = new MemoryStream();
        using var gzip = new GZipStream(stream, CompressionMode.Compress);
        using var writer = new StreamWriter(gzip);
        writer.Write(json);
        writer.Flush();
        gzip.Flush();
        return stream.ToArray();
    }

    private object DecompressCheckpointData(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<object>(json) ?? new();
    }
}
```

### Pattern 2: Graph Integration Template

```csharp
// In RefinementGraph.cs, replace AnalysisAgent:

// OLD:
currentMessage = await ExecuteAgentAsync<AnalysisAgent>(
    currentMessage, context, "analysis", cancellationToken);

// NEW:
currentMessage = await ExecuteAgentAsync<AnalysisAgentAdapter>(
    currentMessage, context, "analysis_af", cancellationToken);
```

### Pattern 3: Error Handling & Fallback

```csharp
public async Task<IAgentMessage> ExecuteWithFallbackAsync(
    Func<Task<IAgentMessage>> primaryAgent,
    Func<Task<IAgentMessage>> fallbackAgent,
    CancellationToken cancellationToken)
{
    try
    {
        _logger.LogInformation("Attempting AF agent execution");
        return await primaryAgent();
    }
    catch (Exception ex) when (ShouldFallback(ex))
    {
        _logger.LogWarning(ex, "AF agent failed, falling back to custom implementation");
        return await fallbackAgent();
    }
}

private bool ShouldFallback(Exception ex)
{
    // Fallback on transient errors, not on permanent failures
    return ex is TimeoutException
        || ex is HttpRequestException
        || ex.InnerException is IOException;
}
```

---

## Summary: Where Agent Framework Fits

### High-Level View

```
PRFactory Workflow Architecture
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  WebUI                                                          │
│  (Ticket creation, approval gates, feedback)                   │
│                                                                 │
│  ↓ ↑                                                            │
│  └─────────────────────────────────────────────────────────┐   │
│        WorkflowOrchestrator (State machine + routing)      │   │
│        ┌─────────────────────────────────────────────────┐ │   │
│        │ RefinementGraph                                 │ │   │
│        │ ├─ TriggerAgent (custom)                        │ │   │
│        │ ├─ RepositoryCloneAgent (custom)                │ │   │
│        │ ├─ AnalysisAgentAdapter ← AF Analyzer           │ │   │
│        │ ├─ QuestionAgentAdapter ← AF Questioner         │ │   │
│        │ ├─ JiraPostAgent (custom)                       │ │   │
│        │ └─ AnswerProcessingAgent (custom)               │ │   │
│        └─────────────────────────────────────────────────┘ │   │
│                         ↓                                    │   │
│        ┌─────────────────────────────────────────────────┐ │   │
│        │ PlanningGraph                                   │ │   │
│        │ ├─ PlannerAgentAdapter ← AF Planner             │ │   │
│        │ ├─ PlanValidationAdapter ← AF CodeExecutor      │ │   │
│        │ ├─ GitPlanAgent (custom)                        │ │   │
│        │ └─ JiraPostAgent (custom)                       │ │   │
│        └─────────────────────────────────────────────────┘ │   │
│                         ↓                                    │   │
│        ┌─────────────────────────────────────────────────┐ │   │
│        │ ImplementationGraph                             │ │   │
│        │ ├─ CodeExecutorAgentAdapter ← AF CodeExecutor   │ │   │
│        │ ├─ GitCommitAgent (custom)                      │ │   │
│        │ ├─ PullRequestAgent (custom)                    │ │   │
│        │ └─ JiraPostAgent (custom)                       │ │   │
│        └─────────────────────────────────────────────────┘ │   │
│                         ↓                                    │   │
│        ┌─────────────────────────────────────────────────┐ │   │
│        │ CodeReviewGraph                                 │ │   │
│        │ ├─ ReviewerAgentAdapter ← AF Reviewer           │ │   │
│        │ ├─ PostReviewCommentsAgent (custom)             │ │   │
│        │ └─ PostApprovalCommentAgent (custom)            │ │   │
│        └─────────────────────────────────────────────────┘ │   │
│                         ↓                                    │   │
│        ┌─────────────────────────────────────────────────┐ │   │
│        │ CompletionAgent (custom)                        │ │   │
│        └─────────────────────────────────────────────────┘ │   │
│                                                              │   │
│  Checkpoint Store ← All agents persist state here          │   │
│  Event Publisher ← All graphs emit events                  │   │
│  Multi-Tenant Context ← All DI services use tenant context │   │
│                                                              │   │
└─────────────────────────────────────────────────────────────┘
```

### Integration Summary

| Phase | Current | Agent Framework | Benefit |
|-------|---------|-----------------|---------|
| Refinement | AnalysisAgent | AF Analyzer (AnalysisAgentAdapter) | Better code understanding, structured analysis |
| Refinement | QuestionGenerationAgent | AF Questioner (QuestionAgentAdapter) | More nuanced questions, context awareness |
| Planning | PlanningAgent | AF Planner (PlannerAgentAdapter) | Better decomposition, risk assessment |
| Planning | (none) | AF CodeExecutor (validation) | Pre-implementation validation |
| Implementation | ImplementationAgent | AF CodeExecutor (CodeExecutorAgentAdapter) | Better testing, artifact management |
| Code Review | CodeReviewAgent | AF Reviewer (ReviewerAgentAdapter) | More sophisticated review, cross-provider |
| Supporting | ProcessExecutor | AF Executor | Safe command execution, resource limits |

### Key Architectural Benefits

1. **Specialized Roles**: Each AF agent optimized for its specific task
2. **Checkpoint Resilience**: Full resumption support across AF boundaries
3. **Multi-Tenant Safety**: Tenant isolation maintained throughout
4. **Flexible LLM Selection**: Use different models per role/phase
5. **Production Robustness**: Built-in error handling and retry logic
6. **Easy Iteration**: Feedback loops for code improvement
7. **Backward Compatible**: Gradual migration, not "big bang" replacement
8. **Cost Efficient**: Use right model for right task

---

## Next Steps

1. **Review** this integration map with the team
2. **Prioritize** which AF roles to integrate first (suggest: Analyzer → Planner → CodeExecutor)
3. **Create** detailed design documents for Phase 1 adapters
4. **Estimate** effort per adapter based on AF SDK complexity
5. **Plan** rollout strategy and canary testing
6. **Set up** AF SDK integration in development environment
7. **Begin** Phase 1 implementation (AnalysisAgentAdapter)

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-13  
**Status**: Ready for Team Review

