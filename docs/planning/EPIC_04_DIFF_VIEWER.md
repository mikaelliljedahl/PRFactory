# Epic 4: Web-based Git Diff Visualization

**Status:** 🟡 Planning Complete - Ready for Implementation
**Priority:** P2 (Important)
**Effort:** 28-38 hours (3.5-4.75 days development + testing)
**Dependencies:** Epic 05 Phase 4 (AG-UI - COMPLETE ✅), Epic 08 (Consolidation - COMPLETE ✅)
**Architecture:** Blazor Server (NO custom JavaScript)
**Last Updated:** 2025-11-15

## ⚠️ IMPORTANT: Architecture Decisions (Updated)

This epic has been **fully planned** with corrected architecture assumptions:

### Key Architecture Decisions

1. **Workflow State**: Use existing `WorkflowState.Implementing` (NOT new `CodeGenerated` state)
   - Simpler implementation, no enum changes needed
   - Semantically correct: code IS implementing during this state

2. **Code Generation**: Leverage `ICliAgent` interface from Epic 05
   - Configurable: Claude Code CLI, builtin agents, external CLIs
   - `ImplementationAgent` generates code, then calls `ILocalGitService.GetDiffAsync()`
   - No new CLI project needed

3. **Workspace Management**: Create new `IWorkspaceService` abstraction
   - Centralizes workspace path logic (currently scattered)
   - Methods: `GetWorkspaceDirectory()`, `GetRepositoryPath()`, `GetDiffPath()`, `ReadDiffAsync()`, `WriteDiffAsync()`

4. **Service Layer**: Direct DI injection (NO HTTP calls per CLAUDE.md)
   - Blazor components: `@inject ITicketService` → Application Service → Repository
   - API controllers: Only for external clients (webhooks, mobile apps)

5. **Project Structure**: Post-Epic 08 consolidation
   - All code in `PRFactory.Web` project (no separate `PRFactory.Api`)
   - Controllers in `/src/PRFactory.Web/Controllers/`

### Blazor Server Compliance

- ✅ **NO custom JavaScript files** (Blazor Server only)
- ✅ **USE DiffPlex** (C# library for server-side diff rendering)
- ✅ **Component: GitDiffViewer.razor** with code-behind pattern
- ✅ **Server-side HTML generation** (no client-side JavaScript)
- ✅ **Direct service injection** (NO HTTP calls within Blazor Server)

**Rationale**: Per `CLAUDE.md`:
> "CRITICAL: This is a Blazor Server application. NEVER add custom JavaScript files."
> "NEVER Use HTTP Calls Within Blazor Server"

## 🤝 Epic 05 Phase 4 Coordination

**Epic 05 Phase 4 (AG-UI/SSE Integration) completed Nov 15, 2025:**
- ✅ Modified `Detail.razor` (added `AgentChat` component at bottom)
- ✅ Created `/api/agent/chat/*` endpoints
- ✅ No collision with Epic 04 (different sections of Detail.razor, different API endpoints)

**Epic 04 will modify:**
- 🟡 `Detail.razor` (add diff viewer in `Implementing` state section - lines 63-134)
- ✅ NO API endpoints needed (Blazor uses direct service injection per CLAUDE.md)
- ✅ Easy merge (Epic 05 added bottom section, Epic 04 adds state-specific section)

---

## Strategic Goal

Create a standalone web experience. Users never leave the PRFactory UI to review code changes. Provide "Warp-like" clean diff visualization directly in the browser.

**Current Pain:** Users must go to GitHub/GitLab to see code changes. Breaks the flow.

**Solution:** Build rich diff viewer in Blazor using **DiffPlex** (C# library). Server-side HTML generation with syntax highlighting. Display AI-generated code inline.

---

## Success Criteria

✅ **Must Have:**
- `cli code` outputs structured `diff.patch` file
- Web UI diff viewer component with side-by-side or unified view
- "Approve and Create Pull Request" workflow entirely in UI
- PR automatically includes plan artifacts in description

✅ **Nice to Have:**
- Inline commenting on diff lines
- File tree navigation for large diffs
- Syntax highlighting for all languages
- Dark mode support

---

## 📋 Detailed Implementation Plans

**IMPORTANT**: This epic has comprehensive phase-by-phase implementation plans in:

```
/docs/planning/epic_04_diff_viewer/
├── README.md                          (Overview & architecture decisions)
├── 001_workspace_diff_generation.md   (Phase 1: Backend - 4-6 hours)
├── 002_service_layer_dtos.md          (Phase 2: Services - 3-4 hours)
├── 003_diffplex_integration.md        (Phase 3: Diff rendering - 6-8 hours)
├── 004_blazor_ui_components.md        (Phase 4: UI - 6-8 hours)
├── 005_pr_creation_workflow.md        (Phase 5: PR workflow - 5-6 hours)
└── 006_testing_integration.md         (Phase 6: Testing - 4-6 hours)
```

**Total Estimated Effort**: 28-38 hours (3.5-4.75 days)

### Implementation Phases Summary

1. **Phase 1**: Workspace & Diff Generation
   - Create `IWorkspaceService` abstraction
   - Enhance `ImplementationAgent` to generate diffs
   - Dependencies: None

2. **Phase 2**: Service Layer & DTOs
   - Application service methods for diff retrieval
   - DTOs: `DiffContentDto`, `CreatePRResponse`
   - Dependencies: Phase 1

3. **Phase 3**: DiffPlex Integration (parallel with Phase 1-2)
   - Install DiffPlex NuGet package
   - Create `IDiffRenderService` with git patch parsing
   - Dependencies: None

4. **Phase 4**: Blazor UI Components
   - `GitDiffViewer.razor` with code-behind
   - Update `Detail.razor` for `Implementing` state
   - Dependencies: Phase 2, Phase 3

5. **Phase 5**: PR Creation Workflow
   - Build PR description with plan artifacts
   - Multi-platform PR creation
   - Dependencies: Phase 4

6. **Phase 6**: Testing & Integration
   - Unit tests (80% coverage minimum)
   - Integration tests
   - End-to-end validation
   - Dependencies: All phases

---

## Implementation Plan (High-Level Overview)

**NOTE**: This section provides a high-level overview. For detailed implementation specifications, see `/docs/planning/epic_04_diff_viewer/` folder.

### 1. Diff Generation (Updated Approach)

**Approach**: Use existing `ImplementationAgent` + `ICliAgent` interface (from Epic 05)

**Flow:**
1. `ImplementationAgent` executes configured CLI agent (Claude Code, builtin, or external)
2. After code generation, agent calls `ILocalGitService.GetDiffAsync(repoPath)`
3. Diff saved via `IWorkspaceService.WriteDiffAsync(ticketId, diffContent)`
4. Ticket remains in `Implementing` state (diff ready for review)

**Key Components:**
- `IWorkspaceService`: New abstraction for workspace file management
- `ILocalGitService.GetDiffAsync()`: Already exists (LibGit2Sharp wrapper)
- `ImplementationAgent`: Enhanced to generate diffs after code generation

**Details**: See `/docs/planning/epic_04_diff_viewer/001_workspace_diff_generation.md`

### 2. Service Layer & DTOs

**Approach**: Blazor components inject services directly (NO HTTP endpoints per CLAUDE.md)

**Architecture:**
```
Blazor Component (@inject ITicketService)
  → Web Facade (TicketService.cs)
    → Application Service (TicketApplicationService.cs)
      → WorkspaceService + Repositories
```

**Key Services:**
- `ITicketService.GetDiffContentAsync()`: Returns `DiffContentDto`
- `ITicketService.CreatePullRequestAsync()`: Returns `CreatePRResponse`
- `ITicketApplicationService`: Business logic layer (shared by Blazor & API controllers)

**Details**: See `/docs/planning/epic_04_diff_viewer/002_service_layer_dtos.md`

---

### 3. DiffPlex Integration

**Library**: DiffPlex v1.7.2 (C# library for server-side diff rendering)

**Key Components:**
- `IDiffRenderService`: Interface for diff rendering
- `DiffRenderService`: Implementation with git patch parsing
- Parses git patch format into HTML (unified and side-by-side views)
- HTML-encodes output (XSS protection)

**Features:**
- Unified view mode (MVP)
- Side-by-side view mode (simplified MVP, full alignment in future)
- File change stats (files changed, lines +/-)
- File type icons (added/deleted/modified/renamed)

**Details**: See `/docs/planning/epic_04_diff_viewer/003_diffplex_integration.md`

---

### 4. Blazor UI Components

**Component Structure:**
- `/src/PRFactory.Web/Components/Code/GitDiffViewer.razor` - Markup
- `/src/PRFactory.Web/Components/Code/GitDiffViewer.razor.cs` - Code-behind (REQUIRED per CLAUDE.md)
- `/src/PRFactory.Web/Components/Code/GitDiffViewer.razor.css` - CSS isolation

**Features:**
- View mode toggle (Unified / Side-by-Side)
- File stats display (files changed, lines +/-)
- Inject `IDiffRenderService` for rendering
- Used in `Detail.razor` when `ticket.State == WorkflowState.Implementing`

**Integration in Detail.razor:**
```razor
@if (Ticket?.State == WorkflowState.Implementing && diffContent != null)
{
    <GitDiffViewer DiffContent="@diffContent" />
    <Card Title="Review & Approve">
        <LoadingButton OnClick="HandleApprovePR">Approve & Create PR</LoadingButton>
    </Card>
}
```

**Details**: See `/docs/planning/epic_04_diff_viewer/004_blazor_ui_components.md`

---

### 4. Approve & Create Pull Request Workflow

**Update:** `/PRFactory.Web/Pages/Tickets/Detail.razor`

```razor
@* IMPORTANT: Epic 05 Phase 4 added AgentChat section at bottom (lines 142-144) *@
@* This change adds diff viewer in state-specific section (lines 63-134) *@
@* No collision - different sections of same file *@

@if (ticket.State == WorkflowState.CodeGenerated)
{
    <Card Title="Review & Approve" Icon="check-circle">
        <InfoBox Type="Info">
            Review the code changes below. If approved, a pull request will be created automatically.
        </InfoBox>

        @if (diffContent != null)
        {
            <GitDiffViewer DiffContent="@diffContent" />
        }

        <div class="mt-3">
            <LoadingButton OnClick="HandleApprovePR"
                           IsLoading="@isCreatingPR"
                           Icon="git-pull-request"
                           Color="success">
                Approve & Create Pull Request
            </LoadingButton>

            <LoadingButton OnClick="HandleReject"
                           IsLoading="@false"
                           Icon="x-circle"
                           Color="danger">
                Reject & Request Revisions
            </LoadingButton>
        </div>
    </Card>
}

@code {
    private async Task HandleApprovePR()
    {
        isCreatingPR = true;
        try
        {
            // Call backend to create PR
            var response = await Http.PostAsync($"/api/tickets/{TicketId}/create-pr", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreatePRResponse>();
                // Show success message with PR link
                // Navigate to PR or show confirmation
            }
        }
        finally
        {
            isCreatingPR = false;
        }
    }
}
```

**Backend API:**

```csharp
[HttpPost("{ticketId}/create-pr")]
public async Task<IActionResult> CreatePullRequest(Guid ticketId)
{
    var ticket = await _ticketRepo.GetByIdAsync(ticketId);
    if (ticket == null)
        return NotFound();

    // Get plan artifacts
    var plan = await _planRepo.GetLatestByTicketIdAsync(ticketId);

    // Push code to remote
    var workspaceDir = _workspaceService.GetWorkspaceDirectory(ticketId);
    var repositoryPath = Path.Combine(workspaceDir, "repo");

    await _gitService.PushAsync(repositoryPath, $"feature/ticket-{ticket.Key}");

    // Create PR with plan artifacts in description
    var prDescription = BuildPRDescription(ticket, plan);

    var pr = await _gitPlatformService.CreatePullRequestAsync(
        ticket.RepositoryId,
        new CreatePullRequestRequest
        {
            SourceBranch = $"feature/ticket-{ticket.Key}",
            TargetBranch = "main",
            Title = $"{ticket.Key}: {ticket.Title}",
            Description = prDescription
        });

    // Update ticket status
    ticket.MarkPRCreated(pr.Number, pr.Url);
    await _ticketRepo.UpdateAsync(ticket);

    return Ok(new CreatePRResponse
    {
        PullRequestUrl = pr.Url,
        PullRequestNumber = pr.Number
    });
}

private string BuildPRDescription(Ticket ticket, Plan plan)
{
    return $@"
## Ticket: {ticket.Key}

{ticket.Description}

---

## Plan Artifacts

### User Stories
{plan.UserStories}

### API Design
```yaml
{plan.ApiDesign}
```

### Database Schema
```sql
{plan.DatabaseSchema}
```

### Test Cases
{plan.TestCases}

### Implementation Steps
{plan.ImplementationSteps}

---

Generated by PRFactory
";
}
```

---

## Acceptance Criteria

### CLI
- [ ] `cli code` generates `workspace/{ticket-id}/diff.patch`
- [ ] Diff includes all changes (staged and unstaged)
- [ ] Code committed locally (not pushed until approval)

### Backend
- [ ] `GET /api/tickets/{id}/diff` endpoint returns diff content
- [ ] `POST /api/tickets/{id}/create-pr` creates PR with plan artifacts
- [ ] PR description includes all plan artifacts (markdown formatted)

### Web UI
- [ ] `GitDiffViewer` component renders diff using DiffPlex (C# server-side)
- [ ] Side-by-side and unified view modes
- [ ] Syntax highlighting with HTML encoding
- [ ] CSS isolation (`.razor.css` file)
- [ ] Code-behind pattern (`.razor.cs` file)
- [ ] "Approve & Create PR" button visible after code generation
- [ ] PR created successfully with link displayed
- [ ] ✅ NO custom JavaScript files (architecture compliance)

### Integration
- [ ] Full flow: cli code → review diff in UI → approve → PR created
- [ ] PR includes plan artifacts in description
- [ ] Users stay in PRFactory UI (no need to visit GitHub)

---

## Implementation Roadmap

### Phase 1-2: Backend Foundation (7-10 hours)
- Workspace service + diff generation
- Service layer + DTOs
- Can run in parallel with Phase 3
- ✅ **Zero collision risk** (new code only)

### Phase 3: DiffPlex Integration (6-8 hours, parallel)
- Install package + implement rendering service
- Can develop independently
- ✅ **Zero collision risk** (new code only)

### Phase 4-5: UI & PR Creation (11-14 hours)
- Blazor components + PR workflow
- Update `Detail.razor` in `Implementing` state section
  - ⚠️ **Coordinate with Epic 05 Phase 4**: AgentChat at bottom (lines 142-144)
  - ✅ **Easy merge**: Different sections, low collision risk
- ✅ **Architecture compliant** (Blazor Server, no HTTP calls)

### Phase 6: Testing & Integration (4-6 hours)
- Unit tests (80%+ coverage required)
- Integration tests + E2E validation
- Documentation updates

---

## Related Epics

- **Epic 1 (Team Review):** Diff viewer shows validation results from `cli review --validate`
- **Epic 3 (Deep Planning):** PR description includes all plan artifacts

---

**Next Steps:**
1. ✅ Planning complete (2025-11-15) - Architecture gaps resolved
2. ✅ Detailed implementation plans created (6 phases)
3. Review and approve architecture decisions
4. Create feature branch: `feature/epic-04-diff-viewer`
5. Begin Phase 1 (can parallelize with Phase 3)
6. Update IMPLEMENTATION_STATUS.md after merge

---

## Architecture Compliance Summary

### ✅ COMPLIANT with Blazor Server Architecture & CLAUDE.md

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| No custom JavaScript | ✅ COMPLIANT | DiffPlex (C# library), NO diff2html |
| Server-side rendering | ✅ COMPLIANT | HTML generated in `DiffRenderService` |
| Code-behind pattern | ✅ COMPLIANT | `GitDiffViewer.razor.cs` mandatory |
| CSS isolation | ✅ COMPLIANT | `GitDiffViewer.razor.css` mandatory |
| Direct service injection | ✅ COMPLIANT | NO HTTP calls (use `@inject ITicketService`) |
| Component naming | ✅ COMPLIANT | `GitDiffViewer` (avoids confusion with `TicketDiffViewer`) |
| Post-Epic 08 structure | ✅ COMPLIANT | All code in `PRFactory.Web` project |
| Workflow state | ✅ COMPLIANT | Uses existing `Implementing` state |
| Agent integration | ✅ COMPLIANT | Leverages `ICliAgent` from Epic 05 |

### 🤝 Epic 05 Phase 4 Coordination

| Aspect | Epic 05 Phase 4 | Epic 04 | Collision Risk |
|--------|-----------------|---------|----------------|
| **Detail.razor** | Added AgentChat at bottom | Adds diff viewer in `Implementing` state section | 🟡 LOW (different sections) |
| **Blazor Components** | `Components/Agents/AgentChat.razor` | `Components/Code/GitDiffViewer.razor` | ✅ NONE (different namespaces) |
| **Service Layer** | `IAgentChatService` | `ITicketService` extensions | ✅ NONE (different services) |
| **JavaScript** | None (Blazor Server) | None (DiffPlex C#) | ✅ NONE (both compliant) |

**Merge Complexity**: LOW - Estimated 15-30 minutes to resolve Detail.razor changes (if any)

**Ready for Implementation**: ✅ YES - Planning complete, architecture validated
