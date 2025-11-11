# Deep Planning Implementation Plans

This directory contains detailed implementation plans for Epic 3: Deep Planning Phase (MetaGPT-Inspired Multi-Agent Architecture).

---

## Overview

Epic 3 transforms PRFactory's planning phase from generating a single `IMPLEMENTATION_PLAN.md` file into generating comprehensive, multi-artifact plans that simulate a full development team (Product Manager, Software Architect, QA Engineer, Tech Lead).

**Parent Epic:** [EPIC_03_DEEP_PLANNING.md](../EPIC_03_DEEP_PLANNING.md)

---

## Architecture Summary

### Agent-Based Implementation

- **5 specialized agents** generate planning artifacts (user stories, API design, database schema, test cases, implementation steps)
- **Agents inherit from `BaseAgent`** and use `ICliAgent` to call LLMs (Claude Code CLI wrapper)
- **PlanningGraph orchestrates execution** (sequential + parallel execution)
- **Revision workflow** via graph resumption with feedback analysis
- **Web UI** displays all artifacts in tabbed interface with versioning

### Key Points

- **NOT CLI commands** - agents are C# classes, not command-line tools
- **ICliAgent** wraps Claude Code CLI (`claude --headless --prompt "..."`)
- **Graph-based orchestration** (not simple function calls)
- **Multi-artifact storage** in database with versioning support

---

## Implementation Plans

### 01. Multi-Artifact Agents
**File:** [01_MULTI_ARTIFACT_AGENTS.md](01_MULTI_ARTIFACT_AGENTS.md)
**Effort:** 1-1.5 weeks
**Dependencies:** None

**Agents to implement:**
1. **PmUserStoriesAgent** - Product Manager persona (user stories, acceptance criteria)
2. **ArchitectApiDesignAgent** - Software Architect (OpenAPI specification)
3. **ArchitectDbSchemaAgent** - Database Architect (SQL DDL statements)
4. **QaTestCasesAgent** - QA Engineer (test scenarios, cases)
5. **TechLeadImplementationAgent** - Tech Lead (implementation steps)

**Key deliverables:**
- 5 agent classes inheriting from `BaseAgent`
- Comprehensive prompt templates for each persona
- Artifact extraction and validation logic
- Context passing between agents (`AgentContext.State`)
- Unit tests (80% coverage)
- Integration tests for agent chain

---

### 02. Database Schema Changes
**File:** [02_DATABASE_SCHEMA.md](02_DATABASE_SCHEMA.md)
**Effort:** 2-3 days
**Dependencies:** None (can be done in parallel)

**Schema updates:**
- Add 5 artifact columns to `Plans` table (UserStories, ApiDesign, DatabaseSchema, TestCases, ImplementationSteps)
- Create `PlanVersions` table for version history
- Add `Version` column (incremented on revision)
- Update domain entities (`Plan`, `PlanVersion`)
- Update repositories with version management methods

**Key deliverables:**
- EF Core migration `AddPlanArtifactsAndVersioning`
- Updated `Plan` entity with `UpdateArtifacts()` method
- `PlanVersion` entity for historical snapshots
- Repository methods for version queries
- Unit tests for domain entities
- Integration tests for repository methods

---

### 03. Web UI Updates
**File:** [03_WEB_UI.md](03_WEB_UI.md)
**Effort:** 3-4 days
**Dependencies:** Database schema changes (02)

**UI components:**
- **PlanArtifactsCard** - Tabbed interface for 5 artifacts
- **PlanRevisionCard** - Feedback input + approve/revise buttons
- **PlanVersionHistory** - Version list with metadata
- **MarkdownViewer** - Renders markdown artifacts
- **CodeBlock** - Displays YAML/SQL with syntax highlighting (CSS-only, no JavaScript)

**Key deliverables:**
- Radzen Blazor components with clean separation
- Code-behind pattern for business components
- TicketDetailPage integration
- TicketService methods for revision/approval
- Component tests (bUnit)
- Mobile-responsive design

---

### 04. Revision Workflow
**File:** [04_REVISION_WORKFLOW.md](04_REVISION_WORKFLOW.md)
**Effort:** 2-3 days
**Dependencies:** Agents (01), Database (02), Web UI (03)

**Workflow:**
1. User provides natural language feedback
2. **FeedbackAnalysisAgent** analyzes feedback → determines affected artifacts
3. **PlanRevisionAgent** regenerates ONLY affected artifacts
4. **PlanArtifactStorageAgent** saves updated artifacts (creates new version)
5. **GitPlanAgent** commits updates to Git
6. **JiraPostAgent** posts revision summary
7. **HumanWaitAgent** suspends for re-review

**Key deliverables:**
- `FeedbackAnalysisAgent` (LLM-powered feedback analysis)
- `PlanRevisionAgent` (selective artifact regeneration)
- Updated `PlanningGraph.ResumeCoreAsync()` with revision logic
- `PlanRejectedMessage` and `PlanApprovedMessage` types
- Unit tests for feedback analysis
- Integration tests for full revision workflow

---

## Implementation Timeline

### Week 1: Core Agents
**Days 1-2:**
- Implement `PmUserStoriesAgent`
- Implement `ArchitectApiDesignAgent`
- Unit tests

**Days 3-4:**
- Implement `ArchitectDbSchemaAgent`
- Implement `QaTestCasesAgent`
- Unit tests

**Day 5:**
- Implement `TechLeadImplementationAgent`
- Implement `PlanArtifactStorageAgent`
- Integration tests

### Week 2: Database & Graph Orchestration
**Days 1-2:**
- Create database migration
- Update domain entities
- Update repositories

**Days 3-4:**
- Update `PlanningGraph.ExecuteCoreAsync()` with new agent workflow
- Implement parallel execution (API + DB agents)
- Test sequential orchestration

**Day 5:**
- Implement `FeedbackAnalysisAgent`
- Implement `PlanRevisionAgent`
- Update `PlanningGraph.ResumeCoreAsync()` with revision logic

### Week 3: Web UI & Testing
**Days 1-2:**
- Build `PlanArtifactsCard` component
- Add syntax highlighting (Prism.js CSS)
- Build `MarkdownViewer` and `CodeBlock` components

**Days 3-4:**
- Build `PlanRevisionCard` component
- Implement approve/reject workflow in UI
- Wire up to `IWorkflowOrchestrator`

**Day 5:**
- End-to-end testing (trigger → analyze → plan → review → revise → approve)
- Performance testing (LLM latency, database writes)
- Bug fixes and polish

---

## Architecture Diagrams

### Agent Execution Flow

```
PlanningGraph.ExecuteCoreAsync()
  ↓
PmUserStoriesAgent
  → context.State["UserStories"] = "..."
  ↓
[Parallel Execution]
  ArchitectApiDesignAgent (reads UserStories)
    → context.State["ApiDesign"] = "..."
  ArchitectDbSchemaAgent (reads UserStories)
    → context.State["DatabaseSchema"] = "..."
  ↓
QaTestCasesAgent (reads UserStories, ApiDesign, DatabaseSchema)
  → context.State["TestCases"] = "..."
  ↓
TechLeadImplementationAgent (reads all artifacts)
  → context.State["ImplementationSteps"] = "..."
  ↓
PlanArtifactStorageAgent (saves to database)
  → Creates Plan entity with all 5 artifacts
  ↓
GitPlanAgent + JiraPostAgent (parallel)
  → Commits to Git + Posts to Jira
  ↓
HumanWaitAgent (suspend)
  → Workflow pauses for human review
```

### Revision Workflow

```
User provides feedback in Web UI
  ↓
Web UI → IWorkflowOrchestrator.ResumeAsync(PlanRejectedMessage)
  ↓
PlanningGraph.ResumeCoreAsync()
  ↓
FeedbackAnalysisAgent
  → Analyzes: "Add rate limiting to API"
  → Output: ["ApiDesign", "ImplementationSteps"]
  ↓
PlanRevisionAgent
  → Regenerates ApiDesign (ArchitectApiDesignAgent)
  → Regenerates ImplementationSteps (TechLeadImplementationAgent)
  ↓
PlanArtifactStorageAgent
  → Plan.UpdateArtifacts(apiDesign: "...", implementationSteps: "...")
  → Plan.Version = 2
  → PlanVersions.Add(snapshot of version 1)
  ↓
GitPlanAgent + JiraPostAgent (parallel)
  → Commits revision to Git
  → Posts "Plan revised (v2)" to Jira
  ↓
HumanWaitAgent (suspend)
  → Awaiting re-review
```

---

## Key Technologies

- **.NET 10** - Application framework
- **Entity Framework Core** - ORM for database access
- **Blazor Server** - Web UI framework
- **Radzen Blazor** - UI component library
- **ICliAgent + Claude Code CLI** - LLM integration
- **Markdig** - Markdown rendering
- **Prism.js (CSS)** - Syntax highlighting (no JavaScript)
- **xUnit** - Testing framework
- **Moq** - Mocking framework
- **bUnit** - Blazor component testing

---

## Testing Strategy

### Unit Tests
- Each agent: 80% coverage minimum
- Domain entities: Version management, artifact updates
- Repositories: Version queries, artifact storage
- Web components: Rendering, event handling (bUnit)

### Integration Tests
- Agent chain execution (PM → Architect → QA → Tech Lead)
- Graph orchestration (parallel + sequential execution)
- Revision workflow (feedback → analysis → regeneration → storage)
- Database roundtrip (save artifacts → retrieve with versions)

### End-to-End Tests
- Full workflow: Trigger → Analyze → Plan (5 artifacts) → Review → Revise → Approve
- Revision scenarios: Selective artifact regeneration
- Performance: LLM latency, database write times

---

## Success Metrics

**Quantitative:**
- 5 artifacts generated per planning phase (100% of tickets)
- Average planning time < 10 minutes
- Revision rate < 30% (most plans approved first time)
- Average revisions per plan < 1.5

**Qualitative:**
- Development teams report higher confidence in implementation
- Fewer implementation questions/blockers
- Faster code review (reviewers have comprehensive plan context)
- Improved test coverage (QA has test cases upfront)

---

## Related Documentation

- **Parent Epic:** [/docs/planning/EPIC_03_DEEP_PLANNING.md](../EPIC_03_DEEP_PLANNING.md)
- **Architecture:** [/docs/ARCHITECTURE.md](/docs/ARCHITECTURE.md)
- **Workflow:** [/docs/WORKFLOW.md](/docs/WORKFLOW.md)
- **Implementation Status:** [/docs/IMPLEMENTATION_STATUS.md](/docs/IMPLEMENTATION_STATUS.md)
- **CLAUDE.md:** [/CLAUDE.md](/CLAUDE.md) (AI agent guidelines)

---

## Questions?

For questions about implementation:
- Review the parent epic and detailed plans
- Check ARCHITECTURE.md for agent patterns
- Review existing agents (RefinementGraph, PlanningGraph) for reference
- Consult CLAUDE.md for coding standards and architectural patterns

---

**Last Updated:** 2025-11-11
**Status:** Ready for implementation
