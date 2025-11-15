# Epic 4: Web-based Git Diff Visualization

**Status:** 🔴 Not Started
**Priority:** P2 (Important)
**Effort:** 1-2 weeks
**Dependencies:** Epic 1 (Team Review), Epic 05 Phase 4 (AG-UI - COMPLETE ✅)
**Architecture:** Blazor Server (NO custom JavaScript)
**Last Updated:** 2025-11-15

## ⚠️ IMPORTANT: Architecture Compliance

This epic has been **redesigned** to comply with PRFactory's Blazor Server architecture:

- ❌ **NO custom JavaScript files** (`wwwroot/js/diff-viewer.js` - REMOVED from original plan)
- ❌ **NO diff2html library** (requires JavaScript - incompatible with Blazor Server)
- ✅ **USE DiffPlex** (C# library for server-side diff rendering)
- ✅ **Component renamed** to `GitDiffViewer.razor` (avoid confusion with existing `TicketDiffViewer.razor`)
- ✅ **Server-side HTML generation** (no client-side JavaScript rendering)

**Rationale**: Per `CLAUDE.md`:
> "CRITICAL: This is a Blazor Server application. NEVER add custom JavaScript files."

## 🤝 Epic 05 Phase 4 Coordination

**Epic 05 Phase 4 (AG-UI/SSE Integration) completed Nov 15, 2025:**
- ✅ Modified `Detail.razor` (added `AgentChat` component at bottom)
- ✅ Created `/api/agent/chat/*` endpoints
- ✅ No collision with Epic 04 (different sections of Detail.razor, different API endpoints)

**Epic 04 will modify:**
- 🟡 `Detail.razor` (add diff viewer in `CodeGenerated` state section - lines 63-134)
- ✅ Different API namespace (`/api/code-diff/*` vs `/api/agent/chat/*`)
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

## Implementation Plan

### 1. CLI `code` - Generate Diff Artifact

**Current:** `cli code` commits and pushes to remote

**New:** `cli code` also generates `diff.patch` file for Web UI

```bash
# Execute implementation
cli code --plan workspace/PROJ-123/plan/

# After implementation, before final push:
# 1. Run: git diff HEAD > workspace/PROJ-123/diff.patch
# 2. Commit changes locally
# 3. Wait for user approval in Web UI
# 4. If approved, push to remote
```

**Implementation:**

```csharp
public class CodeCommand
{
    public async Task<int> ExecuteAsync()
    {
        // ... implementation logic ...

        // Generate diff before committing
        var diffPath = Path.Combine(WorkspaceDir, "diff.patch");
        await GenerateDiffAsync(diffPath);

        // Commit locally (don't push yet)
        await _gitService.CommitAsync(repositoryPath, "Implement ticket XYZ");

        Console.WriteLine($"Code implemented. Diff saved to: {diffPath}");
        Console.WriteLine("Review in Web UI, then approve to create pull request.");

        return 0;
    }

    private async Task GenerateDiffAsync(string outputPath)
    {
        // Run: git diff HEAD > output.patch
        var result = await _processExecutor.ExecuteAsync(
            "git",
            new[] { "diff", "HEAD" },
            workingDirectory: RepositoryPath);

        File.WriteAllText(outputPath, result.StandardOutput);
    }
}
```

---

### 2. Backend API - Diff Endpoint

**Create:** `GET /api/tickets/{ticketId}/diff`

```csharp
[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    [HttpGet("{ticketId}/diff")]
    public async Task<IActionResult> GetDiff(Guid ticketId)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null)
            return NotFound();

        var workspaceDir = _workspaceService.GetWorkspaceDirectory(ticketId);
        var diffPath = Path.Combine(workspaceDir, "diff.patch");

        if (!System.IO.File.Exists(diffPath))
            return NotFound(new { message = "Diff not yet generated. Run 'cli code' first." });

        var diffContent = await System.IO.File.ReadAllTextAsync(diffPath);

        return Ok(new { diff = diffContent });
    }
}
```

---

### 3. Web UI - Git Diff Viewer Component (Blazor Server)

**Install DiffPlex NuGet Package:**

```bash
dotnet add package DiffPlex --version 1.7.2
```

**Create Service:** `/PRFactory.Infrastructure/CodeDiff/DiffRenderService.cs`

```csharp
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

public interface IDiffRenderService
{
    string RenderDiffAsHtml(string diffPatch, DiffViewMode viewMode = DiffViewMode.SideBySide);
}

public class DiffRenderService : IDiffRenderService
{
    public string RenderDiffAsHtml(string diffPatch, DiffViewMode viewMode = DiffViewMode.SideBySide)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText: "", newText: diffPatch);

        return viewMode == DiffViewMode.SideBySide
            ? RenderSideBySide(diff)
            : RenderUnified(diff);
    }

    private string RenderSideBySide(DiffPaneModel diff)
    {
        var html = new StringBuilder();
        html.Append("<table class='diff-table'>");

        foreach (var line in diff.Lines)
        {
            var cssClass = line.Type switch
            {
                ChangeType.Inserted => "diff-line-inserted",
                ChangeType.Deleted => "diff-line-deleted",
                ChangeType.Modified => "diff-line-modified",
                _ => "diff-line-unchanged"
            };

            html.AppendFormat(
                "<tr class='{0}'><td class='diff-line-number'>{1}</td><td class='diff-line-content'>{2}</td></tr>",
                cssClass,
                line.Position,
                System.Web.HttpUtility.HtmlEncode(line.Text)
            );
        }

        html.Append("</table>");
        return html.ToString();
    }
}

public enum DiffViewMode { SideBySide, Unified }
```

**Create:** `/PRFactory.Web/Components/Code/GitDiffViewer.razor`

```razor
@inject IDiffRenderService DiffRenderer

<Card Title="Code Changes" Icon="file-diff">
    <div class="diff-viewer-controls mb-3">
        <ButtonGroup>
            <button class="btn btn-sm btn-outline-primary @(viewMode == DiffViewMode.SideBySide ? "active" : "")"
                    @onclick="() => SetViewMode(DiffViewMode.SideBySide)">
                <i class="bi bi-layout-split"></i> Side by Side
            </button>
            <button class="btn btn-sm btn-outline-primary @(viewMode == DiffViewMode.Unified ? "active" : "")"
                    @onclick="() => SetViewMode(DiffViewMode.Unified)">
                <i class="bi bi-list-ul"></i> Unified
            </button>
        </ButtonGroup>
    </div>

    <div class="diff-output border rounded">
        @((MarkupString)renderedDiff)
    </div>
</Card>
```

**Create:** `/PRFactory.Web/Components/Code/GitDiffViewer.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Code;

public partial class GitDiffViewer
{
    [Parameter, EditorRequired]
    public string DiffContent { get; set; } = string.Empty;

    [Inject]
    private IDiffRenderService DiffRenderer { get; set; } = null!;

    private DiffViewMode viewMode = DiffViewMode.SideBySide;
    private string renderedDiff = string.Empty;

    protected override void OnParametersSet()
    {
        RenderDiff();
    }

    private void RenderDiff()
    {
        if (string.IsNullOrWhiteSpace(DiffContent))
        {
            renderedDiff = "<p class='text-muted'>No changes to display.</p>";
            return;
        }

        renderedDiff = DiffRenderer.RenderDiffAsHtml(DiffContent, viewMode);
    }

    private void SetViewMode(DiffViewMode mode)
    {
        viewMode = mode;
        RenderDiff();
        StateHasChanged();
    }
}
```

**Create CSS:** `/PRFactory.Web/Components/Code/GitDiffViewer.razor.css`

```css
.diff-table {
    width: 100%;
    border-collapse: collapse;
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 0.875rem;
}

.diff-line-number {
    width: 50px;
    padding: 2px 10px;
    text-align: right;
    background-color: #f6f8fa;
    border-right: 1px solid #e1e4e8;
    color: #6a737d;
    user-select: none;
}

.diff-line-content {
    padding: 2px 10px;
    white-space: pre-wrap;
    word-break: break-all;
}

.diff-line-inserted {
    background-color: #e6ffed;
}

.diff-line-inserted .diff-line-content {
    background-color: #acf2bd;
}

.diff-line-deleted {
    background-color: #ffeef0;
}

.diff-line-deleted .diff-line-content {
    background-color: #fdb8c0;
}

.diff-line-modified {
    background-color: #fff5b1;
}

.diff-line-unchanged {
    background-color: white;
}
```

**Usage in** `/PRFactory.Web/Pages/Tickets/Detail.razor`:

```razor
@* Add after existing workflow state sections, before AgentChat section *@
@if (ticket.State == WorkflowState.CodeGenerated && diffContent != null)
{
    <GitDiffViewer DiffContent="@diffContent" />
}

@code {
    private string? diffContent;

    protected override async Task OnInitializedAsync()
    {
        // ... existing code ...

        // Fetch diff if code is generated
        if (ticket.State == WorkflowState.CodeGenerated)
        {
            diffContent = await TicketService.GetDiffContentAsync(ticket.Id);
        }
    }
}
```

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

## Migration Path

### Week 1: Diff Generation & Backend (No Collision Risk)
- Update `cli code` to generate diff.patch
- Create `GET /api/code-diff/{id}` endpoint (use `/api/code-diff/*` namespace to avoid collision)
- Install DiffPlex NuGet package
- Create `DiffRenderService` with server-side HTML generation
- Test diff generation end-to-end
- ✅ **Zero collision with Epic 05 Phase 4** (different API namespace)

### Week 2: Blazor UI & PR Creation (Low Collision Risk)
- Build `GitDiffViewer.razor` component (renamed from `DiffViewer` to avoid confusion)
- Create `.razor.cs` code-behind file
- Create `.razor.css` isolation file
- Update `Detail.razor` to show diff viewer in `CodeGenerated` state section
  - ⚠️ **Coordinate with Epic 05 Phase 4**: AgentChat added at bottom (lines 142-144)
  - ✅ **Easy merge**: Epic 04 adds to state-specific section (lines 63-134)
- Create `POST /api/code-diff/{id}/approve` endpoint
- Build approval UI workflow
- Test full end-to-end flow
- ✅ **Architecture compliance verified** (no JavaScript, Blazor Server only)

---

## Related Epics

- **Epic 1 (Team Review):** Diff viewer shows validation results from `cli review --validate`
- **Epic 3 (Deep Planning):** PR description includes all plan artifacts

---

**Next Steps:**
1. ✅ Architecture redesign complete (Nov 15, 2025) - JavaScript removed, DiffPlex adopted
2. ✅ Epic 05 Phase 4 coordination verified - Low collision risk
3. Create tickets for Week 1 (Backend) and Week 2 (Blazor UI)
4. Start with diff generation in CLI

---

## Architecture Compliance Summary

### ✅ COMPLIANT with Blazor Server Architecture

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| No custom JavaScript | ✅ COMPLIANT | DiffPlex (C# library) used instead of diff2html |
| Server-side rendering | ✅ COMPLIANT | HTML generated in `DiffRenderService` |
| Code-behind pattern | ✅ COMPLIANT | `GitDiffViewer.razor.cs` file specified |
| CSS isolation | ✅ COMPLIANT | `GitDiffViewer.razor.css` file specified |
| Component naming | ✅ COMPLIANT | `GitDiffViewer` (avoids confusion with `TicketDiffViewer`) |
| API namespace | ✅ COMPLIANT | `/api/code-diff/*` (avoids collision with `/api/agent/chat/*`) |

### 🤝 Epic 05 Phase 4 Coordination

| Aspect | Epic 05 Phase 4 | Epic 04 | Collision Risk |
|--------|-----------------|---------|----------------|
| **Detail.razor** | Added AgentChat at bottom (lines 142-144) | Adds diff viewer in state section (lines 63-134) | 🟡 LOW (different sections) |
| **API Endpoints** | `/api/agent/chat/*` | `/api/code-diff/*` | ✅ NONE (different namespaces) |
| **Blazor Components** | `Components/Agents/AgentChat.razor` | `Components/Code/GitDiffViewer.razor` | ✅ NONE (different namespaces) |
| **JavaScript** | None (Blazor Server) | None (DiffPlex C#) | ✅ NONE (both compliant) |

**Merge Complexity**: LOW - Estimated 15-30 minutes to resolve Detail.razor changes

**Ready for Implementation**: ✅ YES - Safe to deploy agents after Epic 05 Phase 4 completion
