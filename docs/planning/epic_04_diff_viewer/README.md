# Epic 04: Diff Viewer - Detailed Implementation Plans

This folder contains detailed implementation plans for Epic 04 (Web-based Git Diff Visualization).

## Overview

Epic 04 enables users to review AI-generated code changes directly in the PRFactory Web UI without leaving for GitHub/GitLab. The implementation uses:

- **Blazor Server** architecture (NO custom JavaScript)
- **DiffPlex** C# library for server-side diff rendering
- **ICliAgent** interface for configurable code generation (Claude Code, builtin agents, external CLIs)
- **WorkflowState.Implementing** for UI conditional rendering

## Architecture Decisions

### 1. Workflow State: Use Existing `Implementing` State

**Decision**: Use `WorkflowState.Implementing` instead of creating new `CodeGenerated` state.

**Rationale**:
- Simpler implementation (no enum changes, state transitions, or migrations)
- Semantically correct: code IS being implemented during this state
- Diff viewer shows changes made by implementation agent/CLI
- State already exists and is used by ImplementationAgent

### 2. Code Generation: Leverage Existing `ICliAgent` Interface

**Decision**: Use existing `ICliAgent` interface with configurable agents instead of building new CLI command.

**Rationale**:
- Epic 05 already established agent framework with `ICliAgent` interface
- Supports multiple implementations: `CliAgentStub`, Claude Code CLI, custom agents
- Configurable via `AgentConfiguration` entity (no hardcoded tooling)
- `ImplementationAgent` can generate code, then call `ILocalGitService.GetDiffAsync()`
- `ProcessExecutor` infrastructure already handles external CLI execution

**Implementation Approach**:
1. `ImplementationAgent` (or configured CLI agent) generates code in workspace
2. Agent calls `ILocalGitService.GetDiffAsync(repoPath)` to generate diff
3. Diff stored in workspace as `diff.patch` file
4. Ticket transitions to `Implementing` state with diff ready for review
5. Web UI loads diff for user approval

### 3. Workspace Management: Create `IWorkspaceService` Abstraction

**Decision**: Create new `IWorkspaceService` to abstract workspace file management.

**Rationale**:
- Centralizes workspace path logic (currently scattered across agents)
- Provides consistent API for workspace operations
- Makes testing easier (mockable interface)
- Future-proof for workspace cleanup, archiving, multi-tenant isolation

**Key Methods**:
```csharp
public interface IWorkspaceService
{
    string GetWorkspaceDirectory(Guid ticketId);
    string GetRepositoryPath(Guid ticketId);
    string GetDiffPath(Guid ticketId);
    Task<string?> ReadDiffAsync(Guid ticketId);
    Task WriteDiffAsync(Guid ticketId, string diffContent);
}
```

### 4. Service Layer: Direct Injection (NO HTTP Calls)

**Decision**: Blazor components inject application services directly (NOT via HTTP).

**Rationale**:
- Per CLAUDE.md: "NEVER Use HTTP Calls Within Blazor Server"
- Blazor Server runs in same process - HTTP serialization is unnecessary overhead
- Service layer shared between Blazor and API controllers (external clients)

**Architecture**:
```
Blazor Component (@inject ITicketService)
  → Web Service Facade (PRFactory.Web/Services/TicketService.cs)
    → Application Service (PRFactory.Infrastructure/Application/TicketApplicationService.cs)
      → Repository + LocalGitService + WorkspaceService
```

**API Controllers** (for external clients only):
```
External Client (webhook, mobile app)
  → HTTP POST
    → API Controller (PRFactory.Web/Controllers/TicketsController.cs)
      → Same Application Service as Blazor uses
```

### 5. Project Structure: Post-Epic 08 Consolidation

**Decision**: All code goes in `PRFactory.Web` project (consolidated structure).

**Rationale**:
- Epic 08 (Nov 2025) merged PRFactory.Api, PRFactory.Worker, PRFactory.Web into one project
- Controllers now in `/src/PRFactory.Web/Controllers/` (not `/PRFactory.Api/Controllers/`)
- Simplifies deployment, reduces project complexity

## Implementation Phases

### Phase 1: Workspace & Diff Generation (Backend)
**Files**: `001_workspace_diff_generation.md`

- Create `IWorkspaceService` interface and implementation
- Enhance `ImplementationAgent` to generate diffs after code generation
- Add workspace diff storage logic
- Update dependency injection registrations

**Estimated Effort**: 4-6 hours
**Dependencies**: None
**Risk**: Low

### Phase 2: Service Layer & DTOs
**Files**: `002_service_layer_dtos.md`

- Create application service methods for diff retrieval
- Define DTOs (`DiffContentDto`, `CreatePRResponse`)
- Extend `ITicketService` web facade
- NO API controllers needed (Blazor uses direct injection)

**Estimated Effort**: 3-4 hours
**Dependencies**: Phase 1
**Risk**: Low

### Phase 3: DiffPlex Integration
**Files**: `003_diffplex_integration.md`

- Install DiffPlex NuGet package
- Create `IDiffRenderService` interface
- Implement `DiffRenderService` with git patch parsing
- Add unified and side-by-side rendering modes
- Register service in DI container

**Estimated Effort**: 6-8 hours
**Dependencies**: None (can develop in parallel with Phase 1-2)
**Risk**: Medium (git patch parsing complexity)

### Phase 4: Blazor UI Components
**Files**: `004_blazor_ui_components.md`

- Create `GitDiffViewer.razor` component with code-behind
- Implement view mode toggle (Side-by-Side / Unified)
- Create CSS isolation file (`GitDiffViewer.razor.css`)
- Update `Detail.razor` to show diff in `Implementing` state
- Add "Approve & Create PR" workflow buttons

**Estimated Effort**: 6-8 hours
**Dependencies**: Phase 2, Phase 3
**Risk**: Low

### Phase 5: PR Creation Workflow
**Files**: `005_pr_creation_workflow.md`

- Implement PR creation logic in application service
- Build PR description from plan artifacts
- Integrate with existing `IGitPlatformProvider` (GitHub, Bitbucket, Azure DevOps)
- Handle state transitions (Implementing → PRCreated)
- Add error handling and user feedback

**Estimated Effort**: 5-6 hours
**Dependencies**: Phase 4
**Risk**: Medium (multi-platform PR creation)

### Phase 6: Testing & Integration
**Files**: `006_testing_integration.md`

- Unit tests for WorkspaceService, DiffRenderService
- Integration tests for diff generation workflow
- End-to-end test: generate code → review diff → create PR
- Test coverage: 80% minimum

**Estimated Effort**: 4-6 hours
**Dependencies**: All phases
**Risk**: Low

## Total Estimated Effort

- **Development**: 28-38 hours (3.5 - 4.75 days)
- **Testing**: Included in Phase 6
- **Documentation**: Ongoing (update IMPLEMENTATION_STATUS.md, ROADMAP.md after merge)

## File Organization

```
docs/planning/epic_04_diff_viewer/
├── README.md (this file)
├── 001_workspace_diff_generation.md
├── 002_service_layer_dtos.md
├── 003_diffplex_integration.md
├── 004_blazor_ui_components.md
├── 005_pr_creation_workflow.md
└── 006_testing_integration.md
```

## Next Steps

1. Review and approve this architecture/phasing
2. Begin Phase 1 implementation (can run in parallel with Phase 3)
3. Update main EPIC_04_DIFF_VIEWER.md with corrected assumptions
4. Create feature branch: `feature/epic-04-diff-viewer`
5. Implement phases sequentially with PR reviews after each phase
