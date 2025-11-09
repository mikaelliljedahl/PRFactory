# Epic 4: Web-based Git Visualization

**Status:** 🔴 Not Started
**Priority:** P2 (Important)
**Effort:** 1-2 weeks
**Dependencies:** Epic 1 (Team Review)

---

## Strategic Goal

Create a standalone web experience. Users never leave the PRFactory UI to review code changes. Provide "Warp-like" clean diff visualization directly in the browser.

**Current Pain:** Users must go to GitHub/GitLab to see code changes. Breaks the flow.

**Solution:** Build rich diff viewer in Web UI using diff2html library. Display AI-generated code inline.

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

### 3. Web UI - Diff Viewer Component

**Install diff2html:**

```bash
npm install diff2html
```

**Create:** `/PRFactory.Web/Components/Code/DiffViewer.razor`

```razor
@inject IJSRuntime JS

<Card Title="Code Changes" Icon="file-diff">
    <div class="diff-viewer-controls mb-3">
        <ButtonGroup>
            <button class="btn btn-sm btn-outline-primary @(viewMode == "side-by-side" ? "active" : "")"
                    @onclick="() => SetViewMode("side-by-side")">
                <i class="bi bi-layout-split"></i> Side by Side
            </button>
            <button class="btn btn-sm btn-outline-primary @(viewMode == "unified" ? "active" : "")"
                    @onclick="() => SetViewMode("unified")">
                <i class="bi bi-list-ul"></i> Unified
            </button>
        </ButtonGroup>

        <ButtonGroup class="ms-3">
            <button class="btn btn-sm btn-outline-secondary @(isDarkMode ? "active" : "")"
                    @onclick="ToggleDarkMode">
                <i class="bi bi-moon"></i> Dark Mode
            </button>
        </ButtonGroup>
    </div>

    <div id="diff-output" class="border rounded"></div>
</Card>

@code {
    [Parameter, EditorRequired]
    public string DiffContent { get; set; }

    private string viewMode = "side-by-side";
    private bool isDarkMode = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender || diffContentChanged)
        {
            await RenderDiffAsync();
        }
    }

    private async Task RenderDiffAsync()
    {
        await JS.InvokeVoidAsync("renderDiff", DiffContent, viewMode, isDarkMode);
    }

    private async Task SetViewMode(string mode)
    {
        viewMode = mode;
        await RenderDiffAsync();
    }

    private async Task ToggleDarkMode()
    {
        isDarkMode = !isDarkMode;
        await RenderDiffAsync();
    }
}
```

**Create:** `/PRFactory.Web/wwwroot/js/diff-viewer.js`

```javascript
// Diff rendering using diff2html
window.renderDiff = function(diffContent, viewMode, darkMode) {
    const targetElement = document.getElementById('diff-output');

    if (!diffContent) {
        targetElement.innerHTML = '<p class="text-muted">No changes to display.</p>';
        return;
    }

    // Use diff2html to render
    const outputFormat = viewMode === 'side-by-side' ? 'side-by-side' : 'line-by-line';

    const html = Diff2Html.html(diffContent, {
        drawFileList: true,
        fileListToggle: true,
        fileListStartVisible: true,
        fileContentToggle: true,
        matching: 'lines',
        outputFormat: outputFormat,
        synchronisedScroll: true,
        highlight: true,
        renderNothingWhenEmpty: false
    });

    targetElement.innerHTML = html;

    // Apply dark mode class
    if (darkMode) {
        targetElement.classList.add('diff-dark-mode');
    } else {
        targetElement.classList.remove('diff-dark-mode');
    }
};
```

**Add to** `/PRFactory.Web/Pages/_Host.cshtml`:

```html
<script src="https://cdn.jsdelivr.net/npm/diff2html/bundles/js/diff2html-ui.min.js"></script>
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/diff2html/bundles/css/diff2html.min.css" />
<script src="~/js/diff-viewer.js"></script>
```

**Usage in** `/PRFactory.Web/Pages/Tickets/Detail.razor`:

```razor
@if (diffContent != null)
{
    <DiffViewer DiffContent="@diffContent" />
}

@code {
    private string? diffContent;

    protected override async Task OnInitializedAsync()
    {
        // Fetch diff
        var response = await Http.GetFromJsonAsync<DiffResponse>($"/api/tickets/{TicketId}/diff");
        diffContent = response?.Diff;
    }
}
```

---

### 4. Approve & Create Pull Request Workflow

**Update:** `/PRFactory.Web/Pages/Tickets/Detail.razor`

```razor
@if (ticket.Status == TicketStatus.CodeGenerated)
{
    <Card Title="Review & Approve" Icon="check-circle">
        <InfoBox Type="Info">
            Review the code changes below. If approved, a pull request will be created automatically.
        </InfoBox>

        @if (diffContent != null)
        {
            <DiffViewer DiffContent="@diffContent" />
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
- [ ] `DiffViewer` component renders diff using diff2html
- [ ] Side-by-side and unified view modes
- [ ] Syntax highlighting works
- [ ] Dark mode toggle
- [ ] "Approve & Create PR" button visible after code generation
- [ ] PR created successfully with link displayed

### Integration
- [ ] Full flow: cli code → review diff in UI → approve → PR created
- [ ] PR includes plan artifacts in description
- [ ] Users stay in PRFactory UI (no need to visit GitHub)

---

## Migration Path

### Week 1: Diff Generation & API
- Update `cli code` to generate diff.patch
- Create `GET /api/tickets/{id}/diff` endpoint
- Test diff generation end-to-end

### Week 2: Web UI & PR Creation
- Install diff2html library
- Build `DiffViewer` component
- Create PR creation API endpoint
- Build approval UI workflow
- Test full end-to-end flow

---

## Related Epics

- **Epic 1 (Team Review):** Diff viewer shows validation results from `cli review --validate`
- **Epic 3 (Deep Planning):** PR description includes all plan artifacts

---

**Next Steps:**
1. Validate diff2html library compatibility
2. Create tickets for Week 1 and Week 2
3. Start with diff generation in CLI
