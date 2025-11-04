# PRFactory Implementation Plan - Microsoft Agent Framework

## Revised Strategy: Build with Agent Framework from Day 1

Since this is a greenfield project with AI-assisted development, we'll build directly on Microsoft Agent Framework rather than migrating later.

## Architecture Stack

- **.NET 8** - Core framework
- **Microsoft.Agents.AI** - Agent orchestration
- **Entity Framework Core 8** - Data persistence (SQLite for PoC, PostgreSQL for production)
- **ASP.NET Core** - Web API for webhooks
- **Refit** - Typed HTTP clients (Jira, Git platforms)
- **LibGit2Sharp** - Local git operations
- **Anthropic SDK** - Claude AI integration
- **Serilog + OpenTelemetry** - Logging and tracing

## Component Breakdown for Parallel Implementation

### Group 1: Core Domain & Data (Foundation)
1. **Domain Entities** - Ticket, Repository, Tenant, WorkflowEvent
2. **Database Context** - EF Core setup with migrations
3. **Repository Interfaces** - ITicketRepository, IRepositoryRepository, ITenantRepository

### Group 2: External Integrations (Infrastructure)
4. **Jira Integration** - Webhook controller, REST client, comment parser
5. **Git Integration** - LocalGitService (LibGit2Sharp), GitHub/Bitbucket/Azure DevOps providers
6. **Claude Integration** - Client wrapper, context builder, prompt templates

### Group 3: Agent Framework Core (Orchestration)
7. **Base Agent Infrastructure** - Base agent classes, middleware setup
8. **Specialized Agents** - AnalysisAgent, PlanningAgent, ImplementationAgent, etc.
9. **Agent Graphs** - RefinementGraph, PlanningGraph, ImplementationGraph
10. **Human-in-the-Loop** - HumanWaitAgent, approval handlers

### Group 4: API & Services (Presentation)
11. **Web API** - Controllers, DTOs, validation
12. **Worker Service** - Background agent execution host
13. **Configuration** - appsettings, DI setup, middleware pipeline

## Implementation Phases

### Phase 1: Foundation (Parallel)
- Domain entities and EF Core setup
- Basic Jira webhook receiver
- Agent Framework infrastructure

### Phase 2: Integrations (Parallel)
- Jira REST client and comment parsing
- Git operations (clone, branch, commit, push)
- Claude AI client and prompts

### Phase 3: Agent Implementation (Parallel)
- Build all specialized agents
- Create workflow graphs
- Wire up human-in-the-loop

### Phase 4: Integration & Testing
- End-to-end workflow testing
- Error handling and resilience
- Documentation

## File Structure to Create

```
src/
├── PRFactory.Domain/
│   ├── Entities/
│   │   ├── Ticket.cs
│   │   ├── Repository.cs
│   │   ├── Tenant.cs
│   │   └── WorkflowEvent.cs
│   ├── Interfaces/
│   │   ├── ITicketRepository.cs
│   │   ├── IRepositoryRepository.cs
│   │   └── ITenantRepository.cs
│   └── ValueObjects/
│
├── PRFactory.Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Jira/
│   │   ├── IJiraClient.cs
│   │   ├── JiraService.cs
│   │   ├── JiraWebhookValidator.cs
│   │   └── JiraCommentParser.cs
│   ├── Git/
│   │   ├── LocalGitService.cs
│   │   ├── IGitPlatformProvider.cs
│   │   ├── GitHubProvider.cs
│   │   ├── BitbucketProvider.cs
│   │   └── AzureDevOpsProvider.cs
│   ├── Claude/
│   │   ├── ClaudeClient.cs
│   │   ├── ContextBuilder.cs
│   │   └── PromptTemplates.cs
│   └── Agents/
│       ├── Base/
│       │   ├── BaseAgent.cs
│       │   └── AgentMiddleware.cs
│       ├── AnalysisAgent.cs
│       ├── QuestionGenerationAgent.cs
│       ├── PlanningAgent.cs
│       ├── ImplementationAgent.cs
│       ├── HumanWaitAgent.cs
│       └── Graphs/
│           ├── RefinementGraph.cs
│           ├── PlanningGraph.cs
│           └── ImplementationGraph.cs
│
├── PRFactory.Api/
│   ├── Controllers/
│   │   └── WebhookController.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── PRFactory.Worker/
    ├── Program.cs
    └── AgentHostService.cs
```

## Agent Design

### Specialized Agents

1. **TriggerAgent** - Initialize workflow from Jira webhook
2. **RepositoryCloneAgent** - Clone repository for analysis
3. **AnalysisAgent** - Use Claude to analyze codebase
4. **QuestionGenerationAgent** - Generate clarifying questions
5. **JiraPostAgent** - Post questions/plans to Jira
6. **HumanWaitAgent** - Suspend until human response via webhook
7. **AnswerProcessingAgent** - Parse and validate user answers
8. **PlanningAgent** - Generate implementation plan with Claude
9. **GitPlanAgent** - Commit plan to feature branch
10. **ImplementationAgent** - (Optional) Generate code with Claude
11. **PullRequestAgent** - Create PR on git platform
12. **CompletionAgent** - Finalize workflow and cleanup

### Agent Graphs

**RefinementGraph**: Trigger → Clone → Analyze → GenerateQuestions → PostToJira → HumanWait → AnswerProcessing

**PlanningGraph**: Planning → GitPlan → PostPlanToJira → HumanWait (approval) → [Complete or Implementation]

**ImplementationGraph**: Implementation → Commit → Push → CreatePR → PostToJira → Complete

## Key Design Decisions

1. **Agent Framework from Start** - No Hangfire, pure agent orchestration
2. **Graph Checkpoints** - Use Agent Framework's built-in checkpointing for resume
3. **Human-in-the-Loop** - HumanWaitAgent suspends execution, webhook resumes
4. **Parallel Execution** - Analysis + Static checks, PostToJira + Git operations
5. **OpenTelemetry** - Built into Agent Framework, auto-tracing
6. **State Storage** - Agent checkpoint state + Ticket entity for queries

## Success Metrics

- End-to-end ticket processing in <5 minutes (excluding human wait)
- All 3 git platforms supported (GitHub, Bitbucket, Azure DevOps)
- 100% webhook replay capability via checkpointing
- Full distributed tracing with OpenTelemetry
- Clean architecture with testable agents

Let's build it! 🚀
