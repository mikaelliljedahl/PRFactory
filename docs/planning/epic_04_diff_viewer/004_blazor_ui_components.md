# Phase 4: Blazor UI Components

**Status**: Not Started
**Estimated Effort**: 6-8 hours
**Dependencies**: Phase 2 (Service Layer), Phase 3 (DiffPlex)
**Risk Level**: Low

## Objective

Create Blazor components for diff visualization with view mode toggle and integrate into Detail.razor page.

## Component Structure

```
/src/PRFactory.Web/Components/Code/
├── GitDiffViewer.razor         (Markup)
├── GitDiffViewer.razor.cs      (Code-behind - REQUIRED per CLAUDE.md)
└── GitDiffViewer.razor.css     (CSS isolation)
```

## Tasks

### Task 4.1: Create `GitDiffViewer.razor` Component

**File**: `/src/PRFactory.Web/Components/Code/GitDiffViewer.razor`

```razor
@using PRFactory.Core.Application.Services

<Card Title="Code Changes" Icon="file-diff" Variant="CardVariant.Light">
    @if (string.IsNullOrEmpty(DiffContent))
    {
        <p class="text-muted">No changes to display.</p>
    }
    else
    {
        <div class="diff-viewer">
            <div class="diff-controls mb-3">
                <div class="btn-group" role="group">
                    <button type="button"
                            class="btn btn-sm @(ViewMode == DiffViewMode.Unified ? "btn-primary" : "btn-outline-primary")"
                            @onclick="() => SetViewMode(DiffViewMode.Unified)">
                        <i class="bi bi-list-ul"></i> Unified
                    </button>
                    <button type="button"
                            class="btn btn-sm @(ViewMode == DiffViewMode.SideBySide ? "btn-primary" : "btn-outline-primary")"
                            @onclick="() => SetViewMode(DiffViewMode.SideBySide)">
                        <i class="bi bi-layout-split"></i> Side by Side
                    </button>
                </div>

                @if (FileStats != null && FileStats.Any())
                {
                    <div class="file-stats-summary ms-3">
                        <span class="badge bg-secondary">
                            <i class="bi bi-file-earmark-diff"></i> @FileStats.Count file(s)
                        </span>
                        <span class="badge bg-success">
                            <i class="bi bi-plus"></i> @TotalLinesAdded
                        </span>
                        <span class="badge bg-danger">
                            <i class="bi bi-dash"></i> @TotalLinesDeleted
                        </span>
                    </div>
                }
            </div>

            <div class="diff-output">
                @((MarkupString)RenderedDiff)
            </div>
        </div>
    }
</Card>
```

### Task 4.2: Create Code-Behind File

**File**: `/src/PRFactory.Web/Components/Code/GitDiffViewer.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;

namespace PRFactory.Web.Components.Code;

/// <summary>
/// Component for rendering git diffs with DiffPlex.
/// Supports unified and side-by-side view modes.
/// </summary>
public partial class GitDiffViewer
{
    [Parameter, EditorRequired]
    public string DiffContent { get; set; } = string.Empty;

    [Inject]
    private IDiffRenderService DiffRenderer { get; set; } = null!;

    private DiffViewMode ViewMode { get; set; } = DiffViewMode.Unified;
    private string RenderedDiff { get; set; } = string.Empty;
    private List<FileChangeInfo>? FileStats { get; set; }

    private int TotalLinesAdded => FileStats?.Sum(f => f.LinesAdded) ?? 0;
    private int TotalLinesDeleted => FileStats?.Sum(f => f.LinesDeleted) ?? 0;

    protected override void OnParametersSet()
    {
        RenderDiff();
    }

    private void SetViewMode(DiffViewMode mode)
    {
        if (ViewMode == mode)
            return;

        ViewMode = mode;
        RenderDiff();
        StateHasChanged();
    }

    private void RenderDiff()
    {
        if (string.IsNullOrEmpty(DiffContent))
        {
            RenderedDiff = string.Empty;
            FileStats = null;
            return;
        }

        try
        {
            // Render diff HTML
            RenderedDiff = DiffRenderer.RenderDiffAsHtml(DiffContent, ViewMode);

            // Parse file stats
            FileStats = DiffRenderer.ParseFileChanges(DiffContent);
        }
        catch (Exception ex)
        {
            RenderedDiff = $"<div class='alert alert-danger'>Error rendering diff: {ex.Message}</div>";
            FileStats = null;
        }
    }
}
```

### Task 4.3: Create CSS Isolation File

**File**: `/src/PRFactory.Web/Components/Code/GitDiffViewer.razor.css`

```css
.diff-viewer {
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
    font-size: 0.875rem;
}

.diff-controls {
    display: flex;
    align-items: center;
    padding: 0.5rem;
    background-color: #f8f9fa;
    border-radius: 4px;
}

.file-stats-summary {
    display: inline-flex;
    gap: 0.5rem;
}

.diff-output {
    max-height: 600px;
    overflow-y: auto;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    background-color: #ffffff;
}

/* Diff table styles */
::deep .diff-table {
    width: 100%;
    border-collapse: collapse;
    margin: 0;
}

::deep .file-diff-section {
    margin-bottom: 1.5rem;
}

::deep .file-header {
    padding: 0.5rem 1rem;
    background-color: #f8f9fa;
    border-bottom: 2px solid #dee2e6;
    font-weight: 600;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

::deep .file-header.file-added {
    background-color: #d4edda;
    border-color: #c3e6cb;
}

::deep .file-header.file-deleted {
    background-color: #f8d7da;
    border-color: #f5c6cb;
}

::deep .file-header.file-modified {
    background-color: #fff3cd;
    border-color: #ffeaa7;
}

::deep .file-path {
    font-family: 'Consolas', 'Monaco', monospace;
}

::deep .file-stats {
    font-size: 0.875rem;
    color: #6c757d;
}

::deep .diff-line-content {
    padding: 2px 10px;
    white-space: pre-wrap;
    word-break: break-all;
}

::deep .diff-line-added {
    background-color: #e6ffed;
}

::deep .diff-line-added .diff-line-content {
    background-color: #acf2bd;
}

::deep .diff-line-deleted {
    background-color: #ffeef0;
}

::deep .diff-line-deleted .diff-line-content {
    background-color: #fdb8c0;
}

::deep .diff-line-hunk-header {
    background-color: #f1f8ff;
    color: #0366d6;
    font-weight: 600;
}

::deep .diff-line-unchanged {
    background-color: white;
}
```

### Task 4.4: Update `Detail.razor` to Show Diff Viewer

**File**: `/src/PRFactory.Web/Pages/Tickets/Detail.razor`

**Add conditional rendering** (around line 70-80, in state-specific sections):

```razor
@* Show diff viewer when in Implementing state with available diff *@
@if (Ticket != null && Ticket.State == WorkflowState.Implementing && diffContent != null)
{
    <GitDiffViewer DiffContent="@diffContent" />

    <Card Title="Review & Approve" Icon="check-circle" Variant="CardVariant.Info" class="mt-3">
        <InfoBox Type="InfoBoxType.Info">
            Review the code changes above. If approved, a pull request will be created automatically with plan artifacts.
        </InfoBox>

        <div class="d-flex gap-2 mt-3">
            <LoadingButton OnClick="HandleApprovePR"
                           IsLoading="@isCreatingPR"
                           Icon="git-pull-request"
                           Color="ButtonColor.Success"
                           Disabled="@isCreatingPR">
                Approve & Create Pull Request
            </LoadingButton>

            <LoadingButton OnClick="HandleRejectChanges"
                           IsLoading="@false"
                           Icon="x-circle"
                           Color="ButtonColor.Danger"
                           Disabled="@isCreatingPR">
                Reject & Request Revisions
            </LoadingButton>
        </div>
    </Card>
}
```

### Task 4.5: Update `Detail.razor.cs` Code-Behind

**File**: `/src/PRFactory.Web/Pages/Tickets/Detail.razor.cs`

**Add fields and logic**:

```csharp
private string? diffContent;
private bool isCreatingPR;

protected override async Task OnInitializedAsync()
{
    await base.OnInitializedAsync();

    // ... existing initialization ...

    // Load diff if in Implementing state
    if (Ticket?.State == WorkflowState.Implementing)
    {
        var diffDto = await TicketService.GetDiffContentAsync(Ticket.Id);
        diffContent = diffDto?.DiffContent;
    }
}

private async Task HandleApprovePR()
{
    if (Ticket == null)
        return;

    isCreatingPR = true;

    try
    {
        var result = await TicketService.CreatePullRequestAsync(Ticket.Id, User?.Identity?.Name);

        if (result.Success)
        {
            // Show success message
            ToastService.ShowSuccess($"Pull request #{result.PullRequestNumber} created successfully!");

            // Navigate to PR URL or refresh ticket
            await LoadTicketAsync();
        }
        else
        {
            ToastService.ShowError($"Failed to create PR: {result.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        ToastService.ShowError($"Error creating pull request: {ex.Message}");
    }
    finally
    {
        isCreatingPR = false;
        StateHasChanged();
    }
}

private async Task HandleRejectChanges()
{
    // TODO: Phase 5 - implement rejection workflow
    ToastService.ShowWarning("Rejection workflow not yet implemented");
}
```

## Acceptance Criteria

- [ ] `GitDiffViewer` component created with code-behind
- [ ] CSS isolation file with proper styling
- [ ] View mode toggle (Unified / Side-by-Side) works
- [ ] File stats displayed (files changed, lines added/deleted)
- [ ] `Detail.razor` shows diff viewer in `Implementing` state
- [ ] Approve & Create PR button calls service (Phase 5 implementation)
- [ ] Component follows Blazor Server patterns (NO JavaScript)
- [ ] Responsive design (mobile-friendly)

## Testing

Manual testing checklist:
- [ ] Navigate to ticket in `Implementing` state
- [ ] Diff viewer renders with changes
- [ ] Toggle between Unified/Side-by-Side views
- [ ] File stats show correct counts
- [ ] Approve button triggers PR creation (Phase 5)
- [ ] Component styling matches PRFactory theme

## Next Steps

- **Phase 5**: Implement PR creation workflow with plan artifacts
