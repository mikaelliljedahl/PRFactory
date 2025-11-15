# Feature 3: Inline Comment Anchoring

**Goal**: Enable reviewers to anchor comments to specific lines or text selections in plans.

**Estimated Effort**: 2 weeks
**Priority**: P1 (High Impact)
**Dependencies**: Feature 2 (Rich Markdown Editor), Epic 1 (ReviewComment entity)

---

## Executive Summary

Currently, ReviewComment entities apply to the entire ticket with no way to reference specific sections. Reviewers must manually quote context, which is error-prone and time-consuming.

**This feature** enables:
- Text selection in markdown preview → "Add Comment" button
- InlineCommentAnchor entity stores line range + text snippet
- Visual indicators (icons/highlights) in preview pane
- Right sidebar with anchored comment threads
- Click-to-scroll navigation

**Expected Impact**: 60%+ of comments will use anchors, reducing review back-and-forth by 40%.

---

## Current State Analysis

### Existing ReviewComment Entity

```csharp
public class ReviewComment
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; }  // Markdown
    public List<Guid> MentionedUserIds { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

**Limitations**:
- No line number or position information
- Comments apply to entire ticket
- No visual connection between comment and referenced text

---

## Implementation Plan

### Week 1: Domain & Infrastructure

#### Day 1-2: InlineCommentAnchor Entity

**File**: `/src/PRFactory.Domain/Entities/InlineCommentAnchor.cs`

```csharp
namespace PRFactory.Domain.Entities;

public class InlineCommentAnchor
{
    public Guid Id { get; private set; }
    public Guid ReviewCommentId { get; private set; }
    public int StartLine { get; private set; }
    public int EndLine { get; private set; }
    public string TextSnippet { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public ReviewComment ReviewComment { get; private set; } = null!;

    private InlineCommentAnchor() { }

    public static InlineCommentAnchor Create(
        Guid reviewCommentId,
        int startLine,
        int endLine,
        string textSnippet)
    {
        if (startLine < 1 || endLine < startLine)
            throw new ArgumentException("Invalid line range");

        if (string.IsNullOrWhiteSpace(textSnippet))
            throw new ArgumentException("Text snippet required");

        return new InlineCommentAnchor
        {
            Id = Guid.NewGuid(),
            ReviewCommentId = reviewCommentId,
            StartLine = startLine,
            EndLine = endLine,
            TextSnippet = textSnippet.Substring(0, Math.Min(200, textSnippet.Length)),
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**File**: `/src/PRFactory.Infrastructure/Persistence/Configurations/InlineCommentAnchorConfiguration.cs`

```csharp
public class InlineCommentAnchorConfiguration : IEntityTypeConfiguration<InlineCommentAnchor>
{
    public void Configure(EntityTypeBuilder<InlineCommentAnchor> builder)
    {
        builder.ToTable("InlineCommentAnchors");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartLine)
            .IsRequired();

        builder.Property(a => a.EndLine)
            .IsRequired();

        builder.Property(a => a.TextSnippet)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(a => a.ReviewComment)
            .WithOne()
            .HasForeignKey<InlineCommentAnchor>(a => a.ReviewCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.ReviewCommentId);
    }
}
```

**Migration**:
```bash
dotnet ef migrations add AddInlineCommentAnchors -p src/PRFactory.Infrastructure
```

---

#### Day 3-4: Repository & Service

**File**: `/src/PRFactory.Domain/Interfaces/IInlineCommentAnchorRepository.cs`

```csharp
public interface IInlineCommentAnchorRepository
{
    Task<InlineCommentAnchor?> GetByIdAsync(Guid id);
    Task<InlineCommentAnchor?> GetByCommentIdAsync(Guid commentId);
    Task<List<InlineCommentAnchor>> GetByTicketIdAsync(Guid ticketId);
    Task CreateAsync(InlineCommentAnchor anchor);
    Task UpdateAsync(InlineCommentAnchor anchor);
    Task DeleteAsync(Guid id);
}
```

**Update PlanReviewService**:

```csharp
public async Task<ReviewComment> AddCommentWithAnchorAsync(
    Guid ticketId,
    Guid authorId,
    string content,
    int? startLine,
    int? endLine,
    string? textSnippet)
{
    // Create comment
    var comment = await AddCommentAsync(ticketId, authorId, content, null);

    // Create anchor if line range provided
    if (startLine.HasValue && endLine.HasValue && !string.IsNullOrWhiteSpace(textSnippet))
    {
        var anchor = InlineCommentAnchor.Create(
            comment.Id,
            startLine.Value,
            endLine.Value,
            textSnippet);

        await _anchorRepo.CreateAsync(anchor);
    }

    return comment;
}
```

---

### Week 2: UI Components

#### Day 5-7: InlineCommentPanel Component

**File**: `/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor`

```razor
@namespace PRFactory.Web.UI.Comments

<div class="inline-comment-panel">
    <div class="panel-header">
        <h6>Comments (@anchors.Count)</h6>
        <div class="filter-controls">
            <button class="btn btn-sm @(showAll ? "btn-secondary" : "btn-outline-secondary")"
                    @onclick="() => showAll = !showAll">
                @(showAll ? "All" : "Unresolved")
            </button>
        </div>
    </div>

    <div class="panel-body">
        @if (anchors.Any())
        {
            @foreach (var anchor in GetFilteredAnchors())
            {
                <div class="anchor-comment-group"
                     @onclick="() => ScrollToAnchor(anchor)">
                    <div class="anchor-indicator">
                        <span class="badge bg-info">Lines @anchor.StartLine-@anchor.EndLine</span>
                    </div>
                    <div class="text-snippet text-muted small">
                        @anchor.TextSnippet
                    </div>
                    <CommentThread CommentId="@anchor.ReviewCommentId"
                                   Compact="true" />
                </div>
            }
        }
        else
        {
            <EmptyState Icon="chat-dots"
                        Message="No inline comments yet"
                        Description="Select text in the plan to add a comment" />
        }
    </div>
</div>

@code {
    [Parameter]
    public Guid TicketId { get; set; }

    [Parameter]
    public EventCallback<InlineCommentAnchorDto> OnAnchorSelected { get; set; }

    [Inject]
    private IPlanReviewService PlanReviewService { get; set; } = null!;

    private List<InlineCommentAnchorDto> anchors = new();
    private bool showAll = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadAnchors();
    }

    private async Task LoadAnchors()
    {
        anchors = await PlanReviewService.GetInlineCommentAnchorsAsync(TicketId);
    }

    private IEnumerable<InlineCommentAnchorDto> GetFilteredAnchors()
    {
        if (showAll)
            return anchors;

        return anchors.Where(a => !a.IsResolved);
    }

    private async Task ScrollToAnchor(InlineCommentAnchorDto anchor)
    {
        if (OnAnchorSelected.HasDelegate)
        {
            await OnAnchorSelected.InvokeAsync(anchor);
        }
    }
}
```

---

#### Day 8-9: Text Selection Handler

Enhance `MarkdownPreview.razor` to support text selection:

```razor
<div class="markdown-preview"
     @onmouseup="HandleTextSelection"
     @ref="previewRef">
    @((MarkupString)RenderMarkdown(Content))

    @if (showAddCommentButton)
    {
        <div class="add-comment-button"
             style="top: @selectionY; left: @selectionX;">
            <button class="btn btn-sm btn-primary"
                    @onclick="AddCommentAtSelection">
                <i class="bi bi-chat-dots"></i> Add Comment
            </button>
        </div>
    }
</div>

@code {
    [Parameter]
    public EventCallback<CommentAnchorRequest> OnAddCommentRequest { get; set; }

    private bool showAddCommentButton = false;
    private string selectionX = "0px";
    private string selectionY = "0px";
    private string selectedText = string.Empty;
    private int selectionStartLine = 0;
    private int selectionEndLine = 0;

    private async Task HandleTextSelection(MouseEventArgs e)
    {
        // In real implementation, would need JSInterop to get selection
        // For now, simplified approach

        // Get window.getSelection() via JSInterop
        // Calculate line numbers from selection
        // Position button near selection

        showAddCommentButton = !string.IsNullOrEmpty(selectedText);
        StateHasChanged();
    }

    private async Task AddCommentAtSelection()
    {
        if (OnAddCommentRequest.HasDelegate)
        {
            var request = new CommentAnchorRequest
            {
                StartLine = selectionStartLine,
                EndLine = selectionEndLine,
                TextSnippet = selectedText
            };

            await OnAddCommentRequest.InvokeAsync(request);
        }

        showAddCommentButton = false;
    }
}

public class CommentAnchorRequest
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string TextSnippet { get; set; } = string.Empty;
}
```

---

#### Day 10: Integration

**Update PlanReviewSection.razor**:

```razor
<div class="plan-review-container">
    <div class="plan-content">
        <MarkdownEditor @bind-Value="@planContent"
                        ShowToolbar="true"
                        ShowPreview="true" />
    </div>

    <InlineCommentPanel TicketId="@Ticket.Id"
                        OnAnchorSelected="ScrollToPlanAnchor" />
</div>

<style>
.plan-review-container {
    display: grid;
    grid-template-columns: 1fr 400px;
    gap: 1rem;
}

@media (max-width: 992px) {
    .plan-review-container {
        grid-template-columns: 1fr;
    }
}
</style>
```

---

## Acceptance Criteria

- [ ] InlineCommentAnchor entity created
- [ ] Database migration applied
- [ ] Repository and service methods implemented
- [ ] InlineCommentPanel component created
- [ ] Text selection handler in MarkdownPreview
- [ ] Visual indicators for anchored comments
- [ ] Click-to-scroll navigation works
- [ ] Filter: All / Unresolved comments
- [ ] Unit tests (80%+ coverage)
- [ ] Integration with PlanReviewSection
- [ ] Manual test: Select text → Add comment → Anchor created → Indicator appears

---

## Files Created/Modified

### New Files (8 files)

- `/src/PRFactory.Domain/Entities/InlineCommentAnchor.cs`
- `/src/PRFactory.Domain/Interfaces/IInlineCommentAnchorRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Repositories/InlineCommentAnchorRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Configurations/InlineCommentAnchorConfiguration.cs`
- `/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor`
- `/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor.cs`
- `/src/PRFactory.Web/Models/InlineCommentAnchorDto.cs`
- `/tests/PRFactory.Domain.Tests/Entities/InlineCommentAnchorTests.cs`

### Modified Files (3 files)

- `/src/PRFactory.Domain/Entities/ReviewComment.cs` - Add navigation
- `/src/PRFactory.Infrastructure/Application/PlanReviewService.cs` - Add anchor support
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add panel

---

## Note on Epic 1 Overlap

**Comment threading** (parent-child relationships) and **@mention notifications** are in **Epic 1** scope. This feature focuses solely on anchoring comments to specific lines.

---

**End of Feature 3: Inline Comment Anchoring**
