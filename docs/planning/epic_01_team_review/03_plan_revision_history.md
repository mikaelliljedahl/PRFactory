# Implementation Plan: Plan Revision History

**Feature:** Track and compare plan revisions when refined or regenerated
**Priority:** P2
**Estimated Effort:** 3-4 days
**Dependencies:** Existing plan service, LibGit2Sharp integration, Epic 7 MarkdownEditor component

---

## Overview

Track all versions of implementation plans as they evolve through refinement and regeneration. Users can:
- View revision history timeline
- Compare two revisions side-by-side (diff viewer)
- See what triggered each revision (initial, refined, regenerated)
- Optionally restore a previous revision
- View who made changes (AI vs human attribution)

This provides full audit trail and helps teams understand plan evolution.

**Note on Storage Strategy:**
- **Git is source of truth** - Plans stored as markdown files in git branches
- **Database stores metadata** - `PlanRevisions` table for fast queries, attribution
- **Hybrid approach** - `CommitHash` links database revisions to git history
- Users can view git blame for detailed attribution

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 Plan Changes Trigger                         │
│  - Initial plan generated                                    │
│  - Plan refined with instructions                            │
│  - Plan regenerated from scratch                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  IPlanService                                │
│            CreateRevisionAsync()                             │
│     (Snapshot current plan to PlanRevisions)                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              PlanRevisionRepository                          │
│         (Store revisions in database)                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│          PlanRevisionHistory Component                       │
│  - Timeline view                                             │
│  - Diff viewer (side-by-side comparison)                     │
└─────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### PlanRevisions Table

```sql
CREATE TABLE PlanRevisions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TicketId UNIQUEIDENTIFIER NOT NULL,
    RevisionNumber INT NOT NULL,
    BranchName NVARCHAR(255) NOT NULL,
    MarkdownPath NVARCHAR(500) NOT NULL,
    CommitHash NVARCHAR(100) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL, -- Markdown content snapshot
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedByUserId UNIQUEIDENTIFIER NULL, -- NULL for AI-generated
    RevisionReason NVARCHAR(50) NOT NULL, -- Initial, Refined, Regenerated

    CONSTRAINT FK_PlanRevisions_Tickets FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
    CONSTRAINT FK_PlanRevisions_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

CREATE INDEX IX_PlanRevisions_TicketId ON PlanRevisions(TicketId);
CREATE INDEX IX_PlanRevisions_RevisionNumber ON PlanRevisions(TicketId, RevisionNumber);
```

**Fields:**
- `Id` - Unique revision ID
- `TicketId` - Related ticket
- `RevisionNumber` - Sequential number (1, 2, 3...)
- `BranchName` - Git branch where plan is stored
- `MarkdownPath` - Path to markdown file
- `CommitHash` - Git commit hash (for traceability to git)
- `Content` - Full markdown content (snapshot)
- `CreatedAt` - When revision was created
- `CreatedByUserId` - Who triggered it (NULL for AI)
- `RevisionReason` - Why this revision was created

---

## Entity

**File:** `/src/PRFactory.Domain/Entities/PlanRevision.cs`

```csharp
namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a snapshot of an implementation plan at a point in time
/// </summary>
public class PlanRevision
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public int RevisionNumber { get; private set; }
    public string BranchName { get; private set; } = string.Empty;
    public string MarkdownPath { get; private set; } = string.Empty;
    public string CommitHash { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public PlanRevisionReason Reason { get; private set; }

    // Navigation properties
    public Ticket Ticket { get; private set; } = null!;
    public User? CreatedBy { get; private set; }

    private PlanRevision() { } // EF Core

    public static PlanRevision Create(
        Guid ticketId,
        int revisionNumber,
        string branchName,
        string markdownPath,
        string commitHash,
        string content,
        PlanRevisionReason reason,
        Guid? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name is required", nameof(branchName));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        return new PlanRevision
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            RevisionNumber = revisionNumber,
            BranchName = branchName,
            MarkdownPath = markdownPath,
            CommitHash = commitHash,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            Reason = reason
        };
    }
}

/// <summary>
/// Reason for creating a plan revision
/// </summary>
public enum PlanRevisionReason
{
    Initial = 1,      // First plan generated
    Refined = 2,      // Plan refined with instructions (keep structure)
    Regenerated = 3   // Plan regenerated from scratch
}
```

**Lines of Code:** ~65 lines

---

## Repository Interface

**File:** `/src/PRFactory.Domain/Interfaces/IPlanRevisionRepository.cs`

```csharp
namespace PRFactory.Domain.Interfaces;

public interface IPlanRevisionRepository
{
    Task<PlanRevision?> GetByIdAsync(Guid revisionId);
    Task<List<PlanRevision>> GetByTicketIdAsync(Guid ticketId);
    Task<PlanRevision?> GetLatestByTicketIdAsync(Guid ticketId);
    Task<PlanRevision?> GetByRevisionNumberAsync(Guid ticketId, int revisionNumber);
    Task<int> GetNextRevisionNumberAsync(Guid ticketId);
    Task CreateAsync(PlanRevision revision);
    Task DeleteByTicketIdAsync(Guid ticketId); // When ticket deleted
}
```

**Lines of Code:** ~15 lines

---

## Repository Implementation

**File:** `/src/PRFactory.Infrastructure/Persistence/Repositories/PlanRevisionRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Persistence.Repositories;

public class PlanRevisionRepository : IPlanRevisionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanRevisionRepository> _logger;

    public PlanRevisionRepository(
        ApplicationDbContext context,
        ILogger<PlanRevisionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PlanRevision?> GetByIdAsync(Guid revisionId)
    {
        return await _context.PlanRevisions
            .Include(r => r.Ticket)
            .Include(r => r.CreatedBy)
            .FirstOrDefaultAsync(r => r.Id == revisionId);
    }

    public async Task<List<PlanRevision>> GetByTicketIdAsync(Guid ticketId)
    {
        return await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .OrderBy(r => r.RevisionNumber)
            .ToListAsync();
    }

    public async Task<PlanRevision?> GetLatestByTicketIdAsync(Guid ticketId)
    {
        return await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .OrderByDescending(r => r.RevisionNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<PlanRevision?> GetByRevisionNumberAsync(Guid ticketId, int revisionNumber)
    {
        return await _context.PlanRevisions
            .FirstOrDefaultAsync(r => r.TicketId == ticketId && r.RevisionNumber == revisionNumber);
    }

    public async Task<int> GetNextRevisionNumberAsync(Guid ticketId)
    {
        var maxRevision = await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .MaxAsync(r => (int?)r.RevisionNumber);

        return (maxRevision ?? 0) + 1;
    }

    public async Task CreateAsync(PlanRevision revision)
    {
        _context.PlanRevisions.Add(revision);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created plan revision {RevisionId} (rev #{RevisionNumber}) for ticket {TicketId} with reason {Reason}",
            revision.Id, revision.RevisionNumber, revision.TicketId, revision.Reason);
    }

    public async Task DeleteByTicketIdAsync(Guid ticketId)
    {
        var revisions = await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .ToListAsync();

        _context.PlanRevisions.RemoveRange(revisions);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted {Count} plan revisions for ticket {TicketId}",
            revisions.Count, ticketId);
    }
}
```

**Lines of Code:** ~85 lines

---

## Service Extension

**File to Modify:** `/src/PRFactory.Core/Application/Services/IPlanService.cs`

Add methods:

```csharp
/// <summary>
/// Get all revisions for a ticket
/// </summary>
Task<List<PlanRevisionDto>> GetPlanRevisionsAsync(Guid ticketId);

/// <summary>
/// Get a specific revision
/// </summary>
Task<PlanRevisionDto?> GetPlanRevisionAsync(Guid revisionId);

/// <summary>
/// Create a snapshot of the current plan as a revision
/// </summary>
Task<PlanRevisionDto> CreateRevisionAsync(
    Guid ticketId,
    PlanRevisionReason reason,
    Guid? createdByUserId = null);

/// <summary>
/// Compare two revisions and return diff
/// </summary>
Task<PlanRevisionComparisonDto> CompareRevisionsAsync(Guid revision1Id, Guid revision2Id);
```

**File to Modify:** `/src/PRFactory.Infrastructure/Application/PlanService.cs`

Add implementation:

```csharp
private readonly IPlanRevisionRepository _planRevisionRepo;

// Add to constructor

public async Task<List<PlanRevisionDto>> GetPlanRevisionsAsync(Guid ticketId)
{
    var revisions = await _planRevisionRepo.GetByTicketIdAsync(ticketId);
    return revisions.Select(PlanRevisionDto.FromEntity).ToList();
}

public async Task<PlanRevisionDto?> GetPlanRevisionAsync(Guid revisionId)
{
    var revision = await _planRevisionRepo.GetByIdAsync(revisionId);
    return revision != null ? PlanRevisionDto.FromEntity(revision) : null;
}

public async Task<PlanRevisionDto> CreateRevisionAsync(
    Guid ticketId,
    PlanRevisionReason reason,
    Guid? createdByUserId = null)
{
    _logger.LogInformation(
        "Creating plan revision for ticket {TicketId} with reason {Reason}",
        ticketId, reason);

    // Get current plan
    var plan = await GetPlanAsync(ticketId);
    if (plan == null)
    {
        throw new InvalidOperationException($"No plan found for ticket {ticketId}");
    }

    // Get next revision number
    var revisionNumber = await _planRevisionRepo.GetNextRevisionNumberAsync(ticketId);

    // Get commit hash from git (using LibGit2Sharp)
    var commitHash = await GetLatestCommitHashAsync(plan.BranchName);

    // Create revision
    var revision = PlanRevision.Create(
        ticketId: ticketId,
        revisionNumber: revisionNumber,
        branchName: plan.BranchName,
        markdownPath: plan.MarkdownPath ?? "",
        commitHash: commitHash,
        content: plan.Content,
        reason: reason,
        createdByUserId: createdByUserId);

    await _planRevisionRepo.CreateAsync(revision);

    _logger.LogInformation(
        "Created plan revision {RevisionId} (rev #{RevisionNumber}) for ticket {TicketId}",
        revision.Id, revisionNumber, ticketId);

    return PlanRevisionDto.FromEntity(revision);
}

public async Task<PlanRevisionComparisonDto> CompareRevisionsAsync(
    Guid revision1Id,
    Guid revision2Id)
{
    var revision1 = await _planRevisionRepo.GetByIdAsync(revision1Id);
    var revision2 = await _planRevisionRepo.GetByIdAsync(revision2Id);

    if (revision1 == null || revision2 == null)
    {
        throw new ArgumentException("One or both revisions not found");
    }

    if (revision1.TicketId != revision2.TicketId)
    {
        throw new ArgumentException("Revisions must be from the same ticket");
    }

    // Generate diff (simple line-by-line comparison)
    var diff = GenerateTextDiff(revision1.Content, revision2.Content);

    return new PlanRevisionComparisonDto
    {
        Revision1 = PlanRevisionDto.FromEntity(revision1),
        Revision2 = PlanRevisionDto.FromEntity(revision2),
        DiffLines = diff
    };
}

private async Task<string> GetLatestCommitHashAsync(string branchName)
{
    // Use LibGit2Sharp to get latest commit hash
    // Implementation depends on existing LocalGitService
    // For now, return placeholder
    return await Task.FromResult("placeholder-commit-hash");
}

private List<DiffLine> GenerateTextDiff(string content1, string content2)
{
    var lines1 = content1.Split('\n');
    var lines2 = content2.Split('\n');

    var diffLines = new List<DiffLine>();

    // Simple line-by-line comparison
    // For production, use a proper diff algorithm (diff-match-patch, DiffPlex, etc.)
    var maxLines = Math.Max(lines1.Length, lines2.Length);

    for (int i = 0; i < maxLines; i++)
    {
        var line1 = i < lines1.Length ? lines1[i] : null;
        var line2 = i < lines2.Length ? lines2[i] : null;

        if (line1 == line2)
        {
            diffLines.Add(new DiffLine
            {
                LineNumber = i + 1,
                Type = DiffLineType.Unchanged,
                Content = line1 ?? ""
            });
        }
        else if (line1 == null)
        {
            diffLines.Add(new DiffLine
            {
                LineNumber = i + 1,
                Type = DiffLineType.Added,
                Content = line2 ?? ""
            });
        }
        else if (line2 == null)
        {
            diffLines.Add(new DiffLine
            {
                LineNumber = i + 1,
                Type = DiffLineType.Removed,
                Content = line1
            });
        }
        else
        {
            diffLines.Add(new DiffLine
            {
                LineNumber = i + 1,
                Type = DiffLineType.Modified,
                OldContent = line1,
                Content = line2
            });
        }
    }

    return diffLines;
}
```

**Lines of Code:** ~150 lines

---

## DTOs

**File:** `/src/PRFactory.Web/Models/PlanRevisionDto.cs`

```csharp
using PRFactory.Domain.Entities;

namespace PRFactory.Web.Models;

public class PlanRevisionDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public int RevisionNumber { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string MarkdownPath { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = "AI Generated";
    public string Reason { get; set; } = string.Empty;

    public static PlanRevisionDto FromEntity(PlanRevision revision)
    {
        return new PlanRevisionDto
        {
            Id = revision.Id,
            TicketId = revision.TicketId,
            RevisionNumber = revision.RevisionNumber,
            BranchName = revision.BranchName,
            MarkdownPath = revision.MarkdownPath,
            CommitHash = revision.CommitHash,
            Content = revision.Content,
            CreatedAt = revision.CreatedAt,
            CreatedByUserId = revision.CreatedByUserId,
            CreatedByName = revision.CreatedBy?.DisplayName ?? "AI Generated",
            Reason = revision.Reason.ToString()
        };
    }
}

public class PlanRevisionComparisonDto
{
    public PlanRevisionDto Revision1 { get; set; } = null!;
    public PlanRevisionDto Revision2 { get; set; } = null!;
    public List<DiffLine> DiffLines { get; set; } = new();
}

public class DiffLine
{
    public int LineNumber { get; set; }
    public DiffLineType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? OldContent { get; set; } // For Modified lines
}

public enum DiffLineType
{
    Unchanged,
    Added,
    Removed,
    Modified
}
```

**Lines of Code:** ~65 lines

---

## Blazor Components

### PlanRevisionHistory Component (Business Logic)

**File:** `/src/PRFactory.Web/Components/Plans/PlanRevisionHistory.razor`

```razor
@namespace PRFactory.Web.Components.Plans
@using PRFactory.Web.Models
@using PRFactory.Web.UI.Display
@using PRFactory.Web.UI.Dialogs

<Card Title="Plan Revision History" Icon="clock-history" Variant="secondary">
    @if (revisions.Any())
    {
        <RevisionTimeline Revisions="@revisions"
                          OnViewRevision="@ViewRevision"
                          OnSelectForComparison="@SelectForComparison"
                          SelectedRevision1="@selectedRevision1"
                          SelectedRevision2="@selectedRevision2" />

        @if (selectedRevision1 != null && selectedRevision2 != null)
        {
            <div class="alert alert-info mt-3">
                <strong>Comparing revisions selected.</strong>
                <button class="btn btn-primary btn-sm ms-3" @onclick="CompareSelected">
                    <i class="bi bi-compare me-1"></i> Show Comparison
                </button>
                <button class="btn btn-outline-secondary btn-sm ms-2" @onclick="ClearSelection">
                    Clear
                </button>
            </div>
        }
    }
    else
    {
        <EmptyState Icon="inbox" Message="No revision history available" />
    }

    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <AlertMessage Type="AlertType.Danger" Message="@errorMessage" Icon="exclamation-triangle" />
    }
</Card>

@if (viewingRevision != null)
{
    <RevisionViewerModal Revision="@viewingRevision"
                         OnClose="@CloseViewer" />
}

@if (comparison != null)
{
    <RevisionComparisonModal Comparison="@comparison"
                             OnClose="@CloseComparison" />
}
```

**Code-behind:** `PlanRevisionHistory.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Plans;

public partial class PlanRevisionHistory
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Inject]
    private IPlanService PlanService { get; set; } = null!;

    private List<PlanRevisionDto> revisions = new();
    private PlanRevisionDto? viewingRevision;
    private PlanRevisionComparisonDto? comparison;
    private Guid? selectedRevision1;
    private Guid? selectedRevision2;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadRevisions();
    }

    private async Task LoadRevisions()
    {
        try
        {
            revisions = await PlanService.GetPlanRevisionsAsync(TicketId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load revision history: {ex.Message}";
        }
    }

    private async Task ViewRevision(Guid revisionId)
    {
        try
        {
            viewingRevision = await PlanService.GetPlanRevisionAsync(revisionId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load revision: {ex.Message}";
        }
    }

    private void CloseViewer()
    {
        viewingRevision = null;
    }

    private void SelectForComparison(Guid revisionId)
    {
        if (selectedRevision1 == null)
        {
            selectedRevision1 = revisionId;
        }
        else if (selectedRevision2 == null && selectedRevision1 != revisionId)
        {
            selectedRevision2 = revisionId;
        }
    }

    private async Task CompareSelected()
    {
        if (selectedRevision1 == null || selectedRevision2 == null)
        {
            errorMessage = "Please select two revisions to compare";
            return;
        }

        try
        {
            comparison = await PlanService.CompareRevisionsAsync(
                selectedRevision1.Value,
                selectedRevision2.Value);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to compare revisions: {ex.Message}";
        }
    }

    private void CloseComparison()
    {
        comparison = null;
        ClearSelection();
    }

    private void ClearSelection()
    {
        selectedRevision1 = null;
        selectedRevision2 = null;
    }

}
```

**Lines of Code:** ~40 lines (razor) + ~80 lines (code-behind) = ~120 lines

**Note:** All timeline, modal UI extracted to pure components following CLAUDE.md patterns.

---

### RevisionTimeline Component (Pure UI)

**File:** `/src/PRFactory.Web/UI/Display/RevisionTimeline.razor`

```razor
@namespace PRFactory.Web.UI.Display
@using PRFactory.Web.Models

<div class="revision-timeline">
    @foreach (var revision in Revisions.OrderByDescending(r => r.RevisionNumber))
    {
        <div class="timeline-item mb-3">
            <div class="timeline-marker bg-@GetReasonColor(revision.Reason)"></div>
            <div class="timeline-content">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>Revision #@revision.RevisionNumber</strong>
                        <span class="badge bg-@GetReasonColor(revision.Reason) ms-2">
                            @revision.Reason
                        </span>
                    </div>
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-primary" @onclick="() => HandleViewRevision(revision.Id)">
                            <i class="bi bi-eye me-1"></i> View
                        </button>
                        @if (SelectedRevision1 == null || SelectedRevision2 != null)
                        {
                            <button class="btn btn-outline-secondary"
                                    @onclick="() => HandleSelectForComparison(revision.Id)">
                                <i class="bi bi-check2-square me-1"></i> Compare
                            </button>
                        }
                    </div>
                </div>
                <div class="text-muted small mt-1">
                    <i class="bi bi-calendar3 me-1"></i>
                    @revision.CreatedAt.ToString("MMM d, yyyy 'at' h:mm tt")
                    <i class="bi bi-person ms-3 me-1"></i>
                    @revision.CreatedByName
                </div>
                <div class="text-muted small">
                    <i class="bi bi-git me-1"></i>
                    Branch: <code>@revision.BranchName</code>
                    <i class="bi bi-hash ms-3 me-1"></i>
                    Commit: <code class="small">@revision.CommitHash.Substring(0, 8)</code>
                </div>
            </div>
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public List<PlanRevisionDto> Revisions { get; set; } = new();

    [Parameter]
    public EventCallback<Guid> OnViewRevision { get; set; }

    [Parameter]
    public EventCallback<Guid> OnSelectForComparison { get; set; }

    [Parameter]
    public Guid? SelectedRevision1 { get; set; }

    [Parameter]
    public Guid? SelectedRevision2 { get; set; }

    private async Task HandleViewRevision(Guid revisionId)
    {
        if (OnViewRevision.HasDelegate)
        {
            await OnViewRevision.InvokeAsync(revisionId);
        }
    }

    private async Task HandleSelectForComparison(Guid revisionId)
    {
        if (OnSelectForComparison.HasDelegate)
        {
            await OnSelectForComparison.InvokeAsync(revisionId);
        }
    }

    private string GetReasonColor(string reason)
    {
        return reason switch
        {
            "Initial" => "primary",
            "Refined" => "info",
            "Regenerated" => "warning",
            _ => "secondary"
        };
    }
}
```

**Lines of Code:** ~80 lines

---

### RevisionViewerModal Component (Pure UI)

**File:** `/src/PRFactory.Web/UI/Dialogs/RevisionViewerModal.razor`

**Note:** Uses Epic 7's `MarkdownEditor` in preview-only mode for proper rendering.

```razor
@namespace PRFactory.Web.UI.Dialogs
@using PRFactory.Web.Models
@using PRFactory.Web.UI.Editors

<Modal IsOpen="true"
       Title="@($"Revision #{Revision.RevisionNumber} - {Revision.Reason}")"
       Size="ModalSize.ExtraLarge"
       OnClose="@HandleClose">
    <Body>
        <div class="mb-3">
            <div class="text-muted small">
                <i class="bi bi-calendar3 me-1"></i>
                @Revision.CreatedAt.ToString("MMM d, yyyy 'at' h:mm tt")
                <i class="bi bi-person ms-3 me-1"></i>
                @Revision.CreatedByName
                <i class="bi bi-git ms-3 me-1"></i>
                <code>@Revision.CommitHash.Substring(0, 8)</code>
            </div>
        </div>

        <!-- Use Epic 7 MarkdownEditor in preview-only mode -->
        <MarkdownEditor Value="@Revision.Content"
                        Height="600px"
                        ShowToolbar="false"
                        InitialViewMode="ViewMode.PreviewOnly"
                        @bind-Value="@dummyValue" />
    </Body>
    <Footer>
        <button type="button" class="btn btn-secondary" @onclick="HandleClose">Close</button>
    </Footer>
</Modal>

@code {
    [Parameter, EditorRequired]
    public PlanRevisionDto Revision { get; set; } = null!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    private string dummyValue = string.Empty; // MarkdownEditor requires @bind-Value

    private async Task HandleClose()
    {
        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }
}
```

**Lines of Code:** ~45 lines

---

### RevisionComparisonModal Component (Pure UI)

**File:** `/src/PRFactory.Web/UI/Dialogs/RevisionComparisonModal.razor`

```razor
@namespace PRFactory.Web.UI.Dialogs
@using PRFactory.Web.Models

<Modal IsOpen="true"
       Title="@($"Compare Revision #{Comparison.Revision1.RevisionNumber} vs #{Comparison.Revision2.RevisionNumber}")"
       Size="ModalSize.ExtraLarge"
       OnClose="@HandleClose">
    <Body>
        <DiffViewer DiffLines="@Comparison.DiffLines" />
    </Body>
    <Footer>
        <button type="button" class="btn btn-secondary" @onclick="HandleClose">Close</button>
    </Footer>
</Modal>

@code {
    [Parameter, EditorRequired]
    public PlanRevisionComparisonDto Comparison { get; set; } = null!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    private async Task HandleClose()
    {
        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }
}
```

**Lines of Code:** ~30 lines

---

### DiffViewer Component (Pure UI)

**File:** `/src/PRFactory.Web/UI/Display/DiffViewer.razor`

```razor
@namespace PRFactory.Web.UI.Display
@using PRFactory.Web.Models

<div class="diff-viewer">
    @foreach (var line in DiffLines)
    {
        <div class="diff-line diff-@line.Type.ToString().ToLower()">
            <span class="line-number">@line.LineNumber</span>
            <span class="line-content">
                @if (line.Type == DiffLineType.Modified && !string.IsNullOrEmpty(line.OldContent))
                {
                    <del class="text-danger">@line.OldContent</del>
                    <ins class="text-success">@line.Content</ins>
                }
                else
                {
                    @line.Content
                }
            </span>
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public List<DiffLine> DiffLines { get; set; } = new();
}
```

**Lines of Code:** ~30 lines

---

### Styling (Separate CSS File)

**File:** `/src/PRFactory.Web/wwwroot/css/revision-timeline.css`

```css
/* Revision Timeline */
.revision-timeline {
    position: relative;
    padding-left: 30px;
}

.revision-timeline::before {
    content: '';
    position: absolute;
    left: 10px;
    top: 0;
    bottom: 0;
    width: 2px;
    background: #dee2e6;
}

.timeline-item {
    position: relative;
}

.timeline-marker {
    position: absolute;
    left: -24px;
    width: 12px;
    height: 12px;
    border-radius: 50%;
    border: 2px solid white;
}

.timeline-content {
    background: #f8f9fa;
    padding: 1rem;
    border-radius: 0.375rem;
    border: 1px solid #dee2e6;
}

/* Diff Viewer */
.diff-viewer {
    font-family: 'Courier New', monospace;
    font-size: 0.875rem;
    line-height: 1.5;
    background: #f8f9fa;
    padding: 1rem;
    border-radius: 0.375rem;
    overflow-x: auto;
}

.diff-line {
    display: flex;
    padding: 2px 0;
}

.line-number {
    min-width: 50px;
    padding-right: 10px;
    color: #6c757d;
    text-align: right;
    user-select: none;
}

.line-content {
    flex: 1;
    white-space: pre-wrap;
}

.diff-unchanged {
    background-color: white;
}

.diff-added {
    background-color: #d4edda;
}

.diff-removed {
    background-color: #f8d7da;
}

.diff-modified {
    background-color: #fff3cd;
}
```

**Lines of Code:** ~75 lines

**Total Component Lines:** ~120 (business) + ~80 (timeline) + ~45 (viewer modal) + ~30 (comparison modal) + ~30 (diff viewer) + ~75 (CSS) = ~380 lines

**Benefits:**
- ✅ Follows CLAUDE.md component architecture
- ✅ Pure UI components reusable elsewhere
- ✅ Uses Epic 7 MarkdownEditor for rendering
- ✅ Clean separation of concerns
- ✅ No inline styles in business components

---

## Automatic Revision Creation

### Hook into Plan Generation Workflow

**File to Modify:** `/src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs`

After plan is generated, create initial revision:

```csharp
// After plan committed to git branch
await _planService.CreateRevisionAsync(
    ticketId,
    PlanRevisionReason.Initial);
```

**File to Modify:** `/src/PRFactory.Infrastructure/Application/TicketApplicationService.cs`

In `RefinePlanAsync()`:

```csharp
// After plan refined
await _planService.CreateRevisionAsync(
    ticketId,
    PlanRevisionReason.Refined,
    createdByUserId: currentUserId);
```

In `RejectPlanAsync()` (with regeneration):

```csharp
// After plan regenerated
await _planService.CreateRevisionAsync(
    ticketId,
    PlanRevisionReason.Regenerated,
    createdByUserId: currentUserId);
```

---

## Summary

**Total Estimated Lines of Code:**
- Entity: ~65 lines
- Repository Interface: ~15 lines
- Repository Implementation: ~85 lines
- Service Extension: ~150 lines
- DTOs: ~65 lines
- PlanRevisionHistory Component: ~120 lines (business logic)
- RevisionTimeline Component: ~80 lines (pure UI)
- RevisionViewerModal Component: ~45 lines (pure UI)
- RevisionComparisonModal Component: ~30 lines (pure UI)
- DiffViewer Component: ~30 lines (pure UI)
- CSS file: ~75 lines
- Integration hooks: ~15 lines
- Migration: ~30 lines

**Total: ~805 lines of code**

**Estimated Effort:** 3-4 days
- Day 1: Database schema, entity, repository
- Day 2: Service extension for revisions and comparison (use proper diff algorithm)
- Day 3: Pure UI components (timeline, diff viewer, modals)
- Day 4: Business component integration, testing, and refinement

**Note:** Reuses Epic 7's MarkdownEditor for rendering revision content.

---

## User Attribution via Git

To enable tracking who made manual changes to plans:

**Update LocalGitService.CommitAsync():**

```csharp
public async Task CommitAsync(
    string repoPath,
    Dictionary<string, string> files,
    string message,
    string author,
    string? email = null) // Add optional email parameter
{
    // Use real user signature for manual edits
    var signature = email != null
        ? new Signature(author, email, DateTimeOffset.Now)
        : new Signature("Claude", "claude@prfactory.ai", DateTimeOffset.Now);

    // ... rest of commit logic
}
```

**When creating manual plan revisions:**

```csharp
await _localGitService.CommitAsync(
    repoPath,
    files,
    "Manual plan edit via UI",
    currentUser.DisplayName,
    currentUser.Email); // Pass real user email

await _planRevisionRepo.CreateAsync(
    PlanRevision.Create(
        ticketId: ticketId,
        revisionNumber: nextRevisionNumber,
        // ...
        reason: PlanRevisionReason.Refined,
        createdByUserId: currentUser.Id)); // Track user in DB
```

**Result:** Git blame shows actual user attribution:
```bash
git blame PLAN.md
# Shows: John Doe <john.doe@company.com> 2025-11-14 ...
```

---

## Future Enhancements

1. **Better Diff Algorithm** (P1)
   - ✅ Current: Simple line-by-line comparison
   - 🎯 Use DiffPlex NuGet package for proper diff algorithm
   - Add syntax highlighting for markdown diffs

2. **Restore Previous Revision** (P2)
   - Allow restoring an old revision as current
   - Create new revision when restoring (don't overwrite history)
   - Track restoration in audit trail

3. **Revision Comments** (P3)
   - Allow users to add notes to revisions
   - Track why each change was made
   - Link to review comments that triggered changes

4. **Export Revisions** (P3)
   - Download revision as markdown file
   - Export comparison as PDF with diff highlighting
   - Generate revision history report
