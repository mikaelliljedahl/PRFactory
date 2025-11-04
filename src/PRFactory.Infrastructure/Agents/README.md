# PRFactory Specialized Agents

This directory contains the specialized agents that implement the PRFactory workflow.

## Architecture

All agents inherit from `BaseAgent` (located in `Base/BaseAgent.cs`) and share a common `AgentContext` for passing data between workflow steps.

## Agent Workflow

The agents are designed to work in sequence to process a Jira ticket from trigger to completion:

```
1. TriggerAgent
   ↓
2. RepositoryCloneAgent
   ↓
3. AnalysisAgent
   ↓
4. QuestionGenerationAgent
   ↓
5. JiraPostAgent (questions)
   ↓
6. HumanWaitAgent (wait for answers)
   ↓
7. AnswerProcessingAgent
   ↓
8. PlanningAgent
   ↓
9. GitPlanAgent
   ↓
10. JiraPostAgent (plan)
    ↓
11. HumanWaitAgent (wait for approval)
    ↓
12. ImplementationAgent (optional)
    ↓
13. PullRequestAgent
    ↓
14. CompletionAgent
```

## Agent Descriptions

### 1. TriggerAgent
- **File:** `TriggerAgent.cs`
- **Purpose:** Initialize workflow from Jira webhook
- **Input:** Jira webhook payload (via context metadata)
- **Output:** Created Ticket entity with validated tenant/repository
- **Key Actions:**
  - Validates tenant and repository exist and are active
  - Creates Ticket entity from Jira data
  - Transitions ticket to `Analyzing` state
  - Persists ticket to database

### 2. RepositoryCloneAgent
- **File:** `RepositoryCloneAgent.cs`
- **Purpose:** Clone repository to local workspace
- **Dependencies:** LocalGitService
- **Key Actions:**
  - Clones repository to workspace (or pulls if already exists)
  - Implements caching to avoid re-cloning
  - Stores repository path in context

### 3. AnalysisAgent
- **File:** `AnalysisAgent.cs`
- **Purpose:** Analyze codebase using Claude AI
- **Dependencies:** ClaudeClient, ContextBuilder
- **Output:** CodebaseAnalysis object with:
  - Architecture summary
  - Affected files list
  - Technical considerations
- **Key Actions:**
  - Builds codebase context from repository
  - Sends analysis prompt to Claude
  - Parses JSON response
  - Stores analysis in context and ticket entity

### 4. QuestionGenerationAgent
- **File:** `QuestionGenerationAgent.cs`
- **Purpose:** Generate 3-7 clarifying questions
- **Dependencies:** ClaudeClient
- **Input:** Ticket + CodebaseAnalysis
- **Output:** List of Question objects categorized by type
- **Key Actions:**
  - Generates questions using Claude
  - Validates 3-7 questions generated
  - Adds questions to Ticket entity

### 5. JiraPostAgent
- **File:** `JiraPostAgent.cs`
- **Purpose:** Generic agent for posting to Jira
- **Configuration:** Via context.Metadata["PostType"]
- **Supported Types:**
  - `questions` - Posts formatted questions
  - `plan` - Posts plan summary with approval instructions
  - `custom` - Posts custom message from metadata
- **Key Actions:**
  - Formats content based on post type
  - Posts comment to Jira ticket
  - Provides user instructions for responses

### 6. HumanWaitAgent
- **File:** `HumanWaitAgent.cs`
- **Purpose:** Suspend workflow until human input
- **Configuration:** Via context.Metadata["WaitType"]
- **Wait Types:**
  - `answers` - Transitions to `AwaitingAnswers`
  - `planapproval` - Transitions to `PlanUnderReview`
- **Key Actions:**
  - Transitions ticket to waiting state
  - Creates checkpoint for resumption
  - Returns `Pending` status to halt workflow
  - Workflow resumes via webhook when user responds

### 7. AnswerProcessingAgent
- **File:** `AnswerProcessingAgent.cs`
- **Purpose:** Process user answers from Jira comment
- **Input:** context.Metadata["AnswerText"] (from webhook)
- **Key Actions:**
  - Parses answers using regex patterns (Q1: answer, Q2: answer, etc.)
  - Validates completeness (warns if questions unanswered)
  - Adds answers to Ticket entity
  - Transitions to `AnswersReceived` state

### 8. PlanningAgent
- **File:** `PlanningAgent.cs`
- **Purpose:** Generate detailed implementation plan
- **Dependencies:** ClaudeClient
- **Input:** Full context (ticket + analysis + answers)
- **Output:** Markdown implementation plan
- **Key Actions:**
  - Builds comprehensive context for Claude
  - Requests detailed markdown plan
  - Stores plan in context
  - Transitions to `Planning` state

### 9. GitPlanAgent
- **File:** `GitPlanAgent.cs`
- **Purpose:** Commit plan to feature branch
- **Dependencies:** LocalGitService
- **Key Actions:**
  - Creates feature branch (e.g., `feature/proj-123-implementation-plan`)
  - Writes `IMPLEMENTATION_PLAN.md` to repository
  - Commits and pushes to remote
  - Updates ticket with branch name
  - Transitions to `PlanPosted` state

### 10. ImplementationAgent (Optional)
- **File:** `ImplementationAgent.cs`
- **Purpose:** Generate code implementation using Claude
- **Dependencies:** ClaudeClient, ContextBuilder
- **Configuration:** Only runs if `tenant.Configuration.AutoImplementAfterPlanApproval == true`
- **Key Actions:**
  - Generates actual code based on approved plan
  - Returns file changes as JSON
  - Stores implementation in context
  - Can be skipped if manual implementation preferred

### 11. PullRequestAgent
- **File:** `PullRequestAgent.cs`
- **Purpose:** Create pull request on git platform
- **Dependencies:** IGitPlatformProvider
- **Key Actions:**
  - Determines source branch (implementation or plan branch)
  - Creates PR with formatted description
  - Links to Jira ticket
  - Updates ticket with PR URL and number
  - Transitions to `PRCreated` state

### 12. CompletionAgent
- **File:** `CompletionAgent.cs`
- **Purpose:** Finalize workflow and cleanup
- **Key Actions:**
  - Transitions ticket to final state (`InReview` or `Completed`)
  - Optionally cleans up workspace
  - Deletes checkpoint data
  - Marks workflow as complete

## AgentContext

The `AgentContext` class (in `Base/AgentContext.cs`) is the shared state object passed between all agents. It contains:

### Core Entities
- `Ticket` - The ticket being processed
- `Tenant` - Tenant configuration
- `Repository` - Repository information

### Workflow Data
- `RepositoryPath` - Local path to cloned repository
- `Analysis` - CodebaseAnalysis from AnalysisAgent
- `ImplementationPlan` - Markdown plan from PlanningAgent
- `PlanBranchName` - Git branch for plan
- `ImplementationBranchName` - Git branch for implementation
- `PullRequestUrl` - URL of created PR
- `PullRequestNumber` - PR number

### State Management
- `State` - Dictionary for agent framework compatibility
- `Metadata` - Additional data for agent configuration
- `Checkpoint` - Checkpoint data for resumption
- `Status` - Current workflow status (Running/Suspended/Completed/Failed)

## Error Handling

All agents implement consistent error handling:
- Return `AgentResult` with status and error information
- Log errors with structured logging
- Update context with error details
- Support retry via Polly policies (configured in BaseAgent)

## Checkpointing

Agents use checkpointing for human-in-the-loop:
- `HumanWaitAgent` creates checkpoints
- Checkpoints store:
  - Current state
  - Next agent to execute
  - Timestamp
- Workflow can resume from checkpoint via webhook

## Testing

Each agent can be unit tested independently:
- Mock dependencies (ClaudeClient, GitService, JiraClient, etc.)
- Provide test AgentContext
- Assert on result status and output
- Verify ticket state transitions

## Usage Example

```csharp
// Create context
var context = new AgentContext
{
    TicketId = ticketId,
    TenantId = tenantId,
    RepositoryId = repositoryId,
    Ticket = ticket,
    Tenant = tenant,
    Repository = repository
};

// Execute agents in sequence
var triggerAgent = new TriggerAgent(logger, ticketRepo, tenantRepo, repoRepo);
var result1 = await triggerAgent.ExecuteAsync(context, cancellationToken);

if (result1.Status == AgentStatus.Completed)
{
    var cloneAgent = new RepositoryCloneAgent(logger, gitService, config);
    var result2 = await cloneAgent.ExecuteAsync(context, cancellationToken);
    
    // Continue with other agents...
}
```

## Configuration

Agents can be configured via:
- `appsettings.json` - Global settings (workspace path, etc.)
- `TenantConfiguration` - Per-tenant settings (auto-implementation, etc.)
- `AgentContext.Metadata` - Per-execution configuration (post type, wait type, etc.)

## Dependencies

Agents require the following services (injected via constructor):
- `ILogger<T>` - Logging
- `ITicketRepository` - Ticket persistence
- `ITenantRepository` - Tenant data
- `IRepositoryRepository` - Repository data
- `IClaudeClient` - Claude AI integration
- `ILocalGitService` - Git operations
- `IGitPlatformProvider` - Platform-specific git operations (GitHub, Bitbucket, etc.)
- `IJiraClient` - Jira API client
- `IContextBuilder` - Codebase context building

## Next Steps

1. Implement missing service interfaces
2. Create agent orchestration graph
3. Add comprehensive unit tests
4. Integrate with webhook handler
5. Add monitoring and metrics
