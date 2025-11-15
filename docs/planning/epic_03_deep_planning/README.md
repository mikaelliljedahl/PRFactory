# Deep Planning Implementation Plans

This directory contains detailed implementation plans for Epic 3: Deep Planning Phase (MetaGPT-Inspired Multi-Agent Architecture).

---

## ⚠️ PHASE 1 IMPLEMENTATION STATUS (Updated: 2025-11-15)

**Status:** Phase 1 Partial - Refactoring Required Before Continuing

### Completed Parts ✅

| Part | Status | Commit | Notes |
|------|--------|--------|-------|
| **Part 1: Database Foundation** | ✅ COMPLETED | `f47cf99` | Plan & PlanVersion entities, migration, repositories |
| **Part 2: Core Planning Agents** | ⚠️ NEEDS REFACTORING | `21f3e8d` | 5 agents implemented but using custom context instead of Epic 07 |
| **Part 6: Web UI Components** | ✅ COMPLETED | `21f3e8d` | MarkdownViewer, CodeBlock, PlanArtifactsCard, PlanRevisionCard |

### Not Yet Implemented 🚧

| Part | Status | Detailed Plan |
|------|--------|---------------|
| **Part 3: Storage & Context** | 🚧 NOT IMPLEMENTED | [05_PART_03_STORAGE.md](05_PART_03_STORAGE.md) |
| **Part 4: Revision Workflow** | 🚧 NOT IMPLEMENTED | [06_PART_04_REVISION_AGENTS.md](06_PART_04_REVISION_AGENTS.md) |
| **Part 5: Graph Orchestration** | 🚧 NOT IMPLEMENTED | [07_PART_05_GRAPH_ORCHESTRATION.md](07_PART_05_GRAPH_ORCHESTRATION.md) |
| **Part 7: Web UI Integration** | 🚧 NOT IMPLEMENTED | [08_PART_07_UI_INTEGRATION.md](08_PART_07_UI_INTEGRATION.md) |

### Critical Issue: Epic 07 Integration Required

**Problem:** Part 2 agents currently use custom `IContextBuilder` extensions that duplicate functionality from Epic 07's existing infrastructure:
- Epic 07 provides `ArchitectureContextService` with methods for architecture patterns, technology stack, code style, and code snippets
- Part 2 agents implemented custom `BuildApiDesignContextAsync()`, `BuildDatabaseSchemaContextAsync()`, etc.
- This creates maintenance burden and misses Epic 07's checklist validation system

**Solution:** Part 3 (05_PART_03_STORAGE.md) contains detailed refactoring plan to replace custom context building with Epic 07's `ArchitectureContextService`.

### Instructions for Orchestrator Agent

**Before implementing Parts 4-7:**
1. **FIRST: Complete Part 3 Epic 07 refactoring** (see 05_PART_03_STORAGE.md sections 3-4)
2. Remove custom `IContextBuilder` methods from Part 2 agents
3. Replace with `IArchitectureContextService` dependency injection
4. Run full test suite to ensure refactoring doesn't break existing tests

**Then proceed with:**
1. Part 4: Revision workflow agents (06_PART_04_REVISION_AGENTS.md)
2. Part 5: Graph orchestration updates (07_PART_05_GRAPH_ORCHESTRATION.md)
3. Part 7: Web UI integration (08_PART_07_UI_INTEGRATION.md)

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

### Original Planning Documents (High-Level)

These documents provide the initial high-level planning for Epic 03:

1. **[01_MULTI_ARTIFACT_AGENTS.md](01_MULTI_ARTIFACT_AGENTS.md)** - 5 specialized agents (PM, Architect API/DB, QA, Tech Lead)
2. **[02_DATABASE_SCHEMA.md](02_DATABASE_SCHEMA.md)** - Plan & PlanVersion entities, migration
3. **[03_WEB_UI.md](03_WEB_UI.md)** - Web UI components for artifact display
4. **[04_REVISION_WORKFLOW.md](04_REVISION_WORKFLOW.md)** - Feedback analysis & revision workflow

**Status:** These plans were used for Phase 1 implementation (Parts 1, 2, 6 completed).

---

### Detailed Implementation Plans (Phase 2+)

After Phase 1 completion, detailed implementation plans were created for the remaining work:

#### 05. Part 3: Storage & Context Building
**File:** [05_PART_03_STORAGE.md](05_PART_03_STORAGE.md)
**Status:** 🚧 NOT IMPLEMENTED
**Effort:** 2-3 days
**Dependencies:** Part 2 refactoring (Epic 07 integration)

**Key deliverables:**
- `PlanArtifactStorageAgent` implementation
- **CRITICAL: Refactor Part 2 agents to use Epic 07's `ArchitectureContextService`**
- Remove custom `IContextBuilder` methods
- Update agent constructors to inject `IArchitectureContextService`
- 7 unit tests for storage agent
- Integration tests for Epic 07 service usage

**MUST READ:** Sections 3-4 contain detailed Epic 07 refactoring guide with before/after code examples.

---

#### 06. Part 4: Revision Workflow Agents
**File:** [06_PART_04_REVISION_AGENTS.md](06_PART_04_REVISION_AGENTS.md)
**Status:** 🚧 NOT IMPLEMENTED
**Effort:** 2-3 days
**Dependencies:** Part 3 completion

**Key deliverables:**
- `FeedbackAnalysisAgent` (analyzes user feedback → affected artifacts)
- `PlanRevisionAgent` (selective artifact regeneration)
- 16 unit tests with full implementations
- Integration tests for revision workflow

---

#### 07. Part 5: Graph Orchestration
**File:** [07_PART_05_GRAPH_ORCHESTRATION.md](07_PART_05_GRAPH_ORCHESTRATION.md)
**Status:** 🚧 NOT IMPLEMENTED
**Effort:** 2-3 days
**Dependencies:** Parts 3 & 4 completion

**Key deliverables:**
- Update `PlanningGraph.ExecuteCoreAsync()` with 5-agent workflow
- Update `PlanningGraph.ResumeCoreAsync()` with revision logic
- Parallel execution (ArchitectApiDesignAgent + ArchitectDbSchemaAgent)
- 20+ integration tests
- End-to-end workflow tests

---

#### 08. Part 7: Web UI Integration
**File:** [08_PART_07_UI_INTEGRATION.md](08_PART_07_UI_INTEGRATION.md)
**Status:** 🚧 NOT IMPLEMENTED
**Effort:** 3-4 days
**Dependencies:** Parts 5 completion

**Key deliverables:**
- Update Ticket Detail page with `PlanArtifactsCard` and `PlanRevisionCard`
- `TicketService.ApprovePlanAsync()` and `RequestPlanRevisionAsync()` methods
- Wire up to `IWorkflowOrchestrator`
- 20+ bUnit component tests
- End-to-end UI workflow tests

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

## Phase 1 Deliverables Summary

**Completed (Commits `f47cf99`, `21f3e8d`):**
- ✅ 2 new domain entities (Plan, PlanVersion) with 98 unit tests
- ✅ 1 EF Core migration (AddPlanArtifactsAndVersioning)
- ✅ 5 specialized planning agents (PM, Architect API/DB, QA, Tech Lead)
- ✅ 4 new Web UI components (MarkdownViewer, CodeBlock, PlanArtifactsCard, PlanRevisionCard)
- ✅ 2,634 tests passing (100% pass rate)
- ✅ Detailed implementation plans for remaining parts (05-08)

**Requires Refactoring:**
- ⚠️ Part 2 agents need Epic 07 integration (replace custom IContextBuilder with ArchitectureContextService)

**Pending Implementation:**
- 🚧 PlanArtifactStorageAgent (Part 3)
- 🚧 FeedbackAnalysisAgent & PlanRevisionAgent (Part 4)
- 🚧 PlanningGraph orchestration updates (Part 5)
- 🚧 Ticket Detail page integration (Part 7)

**See Also:**
- [PHASE_01_IMPLEMENTATION_SUMMARY.md](PHASE_01_IMPLEMENTATION_SUMMARY.md) - Detailed Phase 1 summary with metrics and known issues
- [IMPLEMENTATION_STATUS.md](/docs/IMPLEMENTATION_STATUS.md) - Epic 03 Phase 1 section

---

**Last Updated:** 2025-11-15
**Status:** Phase 1 Partial - Epic 07 Refactoring Required
