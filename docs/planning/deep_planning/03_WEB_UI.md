# Web UI Updates Implementation Plan

**Epic:** Deep Planning Phase (Epic 3)
**Component:** Web UI Multi-Artifact Viewer
**Estimated Effort:** 3-4 days
**Dependencies:** Database schema changes (02_DATABASE_SCHEMA.md)

---

## Overview

This implementation plan covers the Blazor Server UI updates to display multiple planning artifacts in a tabbed interface with revision capabilities.

---

## UI Component Architecture

### Component Hierarchy

```
TicketDetailPage (Pages/Tickets/Detail.razor)
  └─ PlanArtifactsCard (new component)
      ├─ RadzenTabs
      │   ├─ UserStoriesTab
      │   │   └─ MarkdownViewer
      │   ├─ ApiDesignTab
      │   │   └─ CodeEditor (YAML)
      │   ├─ DatabaseSchemaTab
      │   │   └─ CodeEditor (SQL)
      │   ├─ TestCasesTab
      │   │   └─ MarkdownViewer
      │   ├─ ImplementationStepsTab
      │   │   └─ MarkdownViewer
      │   └─ VersionHistoryTab (optional)
      │       └─ PlanVersionHistory
      └─ PlanRevisionCard (new component)
          ├─ FormField (revision instructions)
          ├─ LoadingButton (Revise)
          └─ LoadingButton (Approve)
```

---

## Implementation Steps

### Step 1: Create Reusable UI Components

#### 1.1 MarkdownViewer Component

**File:** `/src/PRFactory.Web/UI/Display/MarkdownViewer.razor`

```razor
@using Markdig

<div class="markdown-content">
    @((MarkupString)RenderedHtml)
</div>

@code {
    [Parameter, EditorRequired]
    public string Content { get; set; } = string.Empty;

    private string RenderedHtml => Markdown.ToHtml(Content ?? string.Empty,
        new MarkdigPipelineBuilder()
            .UseAdvancedExtensions()  // Tables, task lists, etc.
            .Build());
}
```

**Styles:** `/src/PRFactory.Web/wwwroot/css/markdown.css`

```css
.markdown-content {
    line-height: 1.6;
    font-size: 14px;
}

.markdown-content h1 {
    font-size: 24px;
    font-weight: 600;
    margin-top: 24px;
    margin-bottom: 16px;
    padding-bottom: 8px;
    border-bottom: 1px solid #dee2e6;
}

.markdown-content h2 {
    font-size: 20px;
    font-weight: 600;
    margin-top: 20px;
    margin-bottom: 12px;
}

.markdown-content h3 {
    font-size: 16px;
    font-weight: 600;
    margin-top: 16px;
    margin-bottom: 8px;
}

.markdown-content ul, .markdown-content ol {
    padding-left: 24px;
    margin-bottom: 16px;
}

.markdown-content li {
    margin-bottom: 4px;
}

.markdown-content code {
    background-color: #f6f8fa;
    padding: 2px 6px;
    border-radius: 3px;
    font-family: 'Courier New', monospace;
    font-size: 13px;
}

.markdown-content pre {
    background-color: #f6f8fa;
    padding: 16px;
    border-radius: 6px;
    overflow-x: auto;
    margin-bottom: 16px;
}

.markdown-content pre code {
    background-color: transparent;
    padding: 0;
}

.markdown-content table {
    border-collapse: collapse;
    width: 100%;
    margin-bottom: 16px;
}

.markdown-content th, .markdown-content td {
    border: 1px solid #dee2e6;
    padding: 8px 12px;
    text-align: left;
}

.markdown-content th {
    background-color: #f6f8fa;
    font-weight: 600;
}
```

**Package Reference:** Add Markdig to `PRFactory.Web.csproj`

```xml
<PackageReference Include="Markdig" Version="0.37.0" />
```

#### 1.2 CodeEditor Component

**File:** `/src/PRFactory.Web/UI/Display/CodeEditor.razor`

```razor
@* Uses Monaco Editor via Radzen *@
<div class="code-editor-wrapper" style="height: @Height;">
    <RadzenHtmlEditor @bind-Value="@EditorContent"
                      Mode="HtmlEditorMode.Source"
                      class="code-editor"
                      ReadOnly="@ReadOnly" />
</div>

@code {
    [Parameter, EditorRequired]
    public string Content { get; set; } = string.Empty;

    [Parameter]
    public string Language { get; set; } = "text";

    [Parameter]
    public bool ReadOnly { get; set; } = true;

    [Parameter]
    public string Height { get; set; } = "400px";

    private string EditorContent
    {
        get => $"```{Language}\n{Content}\n```";
        set { }  // Read-only
    }
}
```

**Alternative:** Use CodeMirror or Monaco Editor directly for better syntax highlighting.

For full Monaco Editor integration:

**File:** `/src/PRFactory.Web/wwwroot/js/monaco-loader.js`

```javascript
// Monaco Editor loader (only if needed - avoid JavaScript if possible)
window.MonacoEditor = {
    create: function (element, value, language, readOnly) {
        require.config({ paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs' } });
        require(['vs/editor/editor.main'], function () {
            monaco.editor.create(element, {
                value: value,
                language: language,
                readOnly: readOnly,
                theme: 'vs-light',
                automaticLayout: true
            });
        });
    }
};
```

**Blazor Component:**

```razor
<div @ref="editorElement" style="height: @Height; border: 1px solid #dee2e6;"></div>

@code {
    private ElementReference editorElement;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("MonacoEditor.create", editorElement, Content, Language, ReadOnly);
        }
    }
}
```

**IMPORTANT:** Per CLAUDE.md, avoid custom JavaScript. Use Radzen components instead.

**Recommended Approach:** Use Radzen's built-in code display component or simple `<pre><code>` with Prism.js CSS-only syntax highlighting.

#### 1.3 Simple Code Display (No JavaScript)

**File:** `/src/PRFactory.Web/UI/Display/CodeBlock.razor`

```razor
<div class="code-block">
    <pre class="language-@Language"><code>@Content</code></pre>
</div>

@code {
    [Parameter, EditorRequired]
    public string Content { get; set; } = string.Empty;

    [Parameter]
    public string Language { get; set; } = "text";
}
```

**Add Prism.js CSS (no JavaScript, CSS-only highlighting):**

`/src/PRFactory.Web/wwwroot/css/prism.css`

Download from: https://prismjs.com/download.html (CSS only, themes available)

**Reference in `_Host.cshtml`:**

```html
<link href="css/prism.css" rel="stylesheet" />
```

---

### Step 2: Create Plan Artifacts Components

#### 2.1 PlanArtifactsCard Component

**File:** `/src/PRFactory.Web/Components/Plans/PlanArtifactsCard.razor`

```razor
@if (Plan != null && Plan.HasMultipleArtifacts)
{
    <Card Title="Implementation Plan Artifacts" Icon="folder" Class="@Class">
        <RadzenTabs>
            <Tabs>
                @* User Stories Tab *@
                <RadzenTabsItem Text="User Stories">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(Plan.UserStories))
                        {
                            <MarkdownViewer Content="@Plan.UserStories" />
                        }
                        else
                        {
                            <EmptyState Message="User stories not yet generated" Icon="file-earmark-text" />
                        }
                    </div>
                </RadzenTabsItem>

                @* API Design Tab *@
                <RadzenTabsItem Text="API Design">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(Plan.ApiDesign))
                        {
                            <CodeBlock Content="@Plan.ApiDesign" Language="yaml" />
                        }
                        else
                        {
                            <EmptyState Message="API design not yet generated" Icon="code-slash" />
                        }
                    </div>
                </RadzenTabsItem>

                @* Database Schema Tab *@
                <RadzenTabsItem Text="Database Schema">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(Plan.DatabaseSchema))
                        {
                            <CodeBlock Content="@Plan.DatabaseSchema" Language="sql" />
                        }
                        else
                        {
                            <EmptyState Message="Database schema not yet generated" Icon="database" />
                        }
                    </div>
                </RadzenTabsItem>

                @* Test Cases Tab *@
                <RadzenTabsItem Text="Test Cases">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(Plan.TestCases))
                        {
                            <MarkdownViewer Content="@Plan.TestCases" />
                        }
                        else
                        {
                            <EmptyState Message="Test cases not yet generated" Icon="check2-square" />
                        }
                    </div>
                </RadzenTabsItem>

                @* Implementation Steps Tab *@
                <RadzenTabsItem Text="Implementation Steps">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(Plan.ImplementationSteps))
                        {
                            <MarkdownViewer Content="@Plan.ImplementationSteps" />
                        }
                        else
                        {
                            <EmptyState Message="Implementation steps not yet generated" Icon="list-ol" />
                        }
                    </div>
                </RadzenTabsItem>

                @* Version History Tab (optional) *@
                @if (Plan.Versions?.Any() == true)
                {
                    <RadzenTabsItem Text="Version History">
                        <div class="p-3">
                            <PlanVersionHistory Versions="@Plan.Versions"
                                                OnVersionSelected="HandleVersionSelected" />
                        </div>
                    </RadzenTabsItem>
                }
            </Tabs>
        </RadzenTabs>
    </Card>
}
else if (Plan != null)
{
    @* Fallback for old single-file plans *@
    <Card Title="Implementation Plan" Icon="file-earmark-text" Class="@Class">
        <MarkdownViewer Content="@Plan.Content" />
    </Card>
}

@code {
    [Parameter, EditorRequired]
    public PlanDto? Plan { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public EventCallback<PlanVersionDto> OnVersionSelected { get; set; }

    private Task HandleVersionSelected(PlanVersionDto version)
    {
        return OnVersionSelected.InvokeAsync(version);
    }
}
```

**Code-Behind:** `/src/PRFactory.Web/Components/Plans/PlanArtifactsCard.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using PRFactory.Core.DTOs;

namespace PRFactory.Web.Components.Plans;

public partial class PlanArtifactsCard
{
    // Properties defined in @code block above
}
```

#### 2.2 PlanRevisionCard Component

**File:** `/src/PRFactory.Web/Components/Plans/PlanRevisionCard.razor`

```razor
<Card Title="Revise Plan" Icon="pencil" Class="@Class">
    <FormField Label="Revision Instructions">
        <InputTextArea @bind-Value="RevisionFeedback"
                       rows="4"
                       class="form-control"
                       placeholder="e.g., Add rate limiting to the API endpoints, include caching strategy in implementation steps"
                       disabled="@IsProcessing" />
    </FormField>

    <div class="d-flex gap-2 mt-3">
        <LoadingButton OnClick="HandleRevise"
                       IsLoading="@IsRevising"
                       Icon="arrow-repeat"
                       Class="btn-warning"
                       Disabled="@(string.IsNullOrWhiteSpace(RevisionFeedback))">
            Revise Plan
        </LoadingButton>

        <LoadingButton OnClick="HandleApprove"
                       IsLoading="@IsApproving"
                       Icon="check-circle"
                       Class="btn-success">
            Approve Plan
        </LoadingButton>
    </div>

    @if (!string.IsNullOrEmpty(ErrorMessage))
    {
        <AlertMessage Type="AlertType.Danger" Message="@ErrorMessage" Dismissible="true" Class="mt-3" />
    }

    @if (!string.IsNullOrEmpty(SuccessMessage))
    {
        <AlertMessage Type="AlertType.Success" Message="@SuccessMessage" Dismissible="true" Class="mt-3" />
    }
</Card>

@code {
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public EventCallback OnRevisionStarted { get; set; }

    [Parameter]
    public EventCallback OnApproved { get; set; }

    private string? RevisionFeedback { get; set; }
    private bool IsRevising { get; set; }
    private bool IsApproving { get; set; }
    private bool IsProcessing => IsRevising || IsApproving;
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    private async Task HandleRevise()
    {
        if (string.IsNullOrWhiteSpace(RevisionFeedback))
        {
            ErrorMessage = "Please provide revision instructions.";
            return;
        }

        IsRevising = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await OnRevisionStarted.InvokeAsync();

            // TODO: Call revision API or service
            // await WorkflowOrchestrator.ResumeAsync(...)

            SuccessMessage = "Plan revision started. Please wait for agents to regenerate artifacts.";
            RevisionFeedback = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start revision: {ex.Message}";
        }
        finally
        {
            IsRevising = false;
        }
    }

    private async Task HandleApprove()
    {
        IsApproving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await OnApproved.InvokeAsync();

            // TODO: Call approval API or service
            // await WorkflowOrchestrator.ResumeAsync(...)

            SuccessMessage = "Plan approved. Proceeding to next phase.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to approve plan: {ex.Message}";
        }
        finally
        {
            IsApproving = false;
        }
    }
}
```

#### 2.3 PlanVersionHistory Component

**File:** `/src/PRFactory.Web/Components/Plans/PlanVersionHistory.razor`

```razor
<div class="version-history">
    @if (Versions == null || !Versions.Any())
    {
        <EmptyState Message="No version history available" Icon="clock-history" />
    }
    else
    {
        <RadzenDataList Data="@Versions" TItem="PlanVersionDto">
            <Template Context="version">
                <div class="version-item card mb-2">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h6 class="mb-1">
                                    Version @version.Version
                                    @if (!string.IsNullOrEmpty(version.RevisionReason))
                                    {
                                        <span class="badge bg-secondary ms-2">@version.RevisionReason</span>
                                    }
                                </h6>
                                <small class="text-muted">
                                    <i class="bi bi-clock me-1"></i>
                                    <RelativeTime Timestamp="@version.CreatedAt" />
                                    @if (!string.IsNullOrEmpty(version.CreatedBy))
                                    {
                                        <span class="ms-2">
                                            <i class="bi bi-person me-1"></i>@version.CreatedBy
                                        </span>
                                    }
                                </small>
                            </div>
                            <button class="btn btn-sm btn-outline-primary"
                                    @onclick="() => SelectVersion(version)">
                                <i class="bi bi-eye me-1"></i>View
                            </button>
                        </div>
                    </div>
                </div>
            </Template>
        </RadzenDataList>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public List<PlanVersionDto>? Versions { get; set; }

    [Parameter]
    public EventCallback<PlanVersionDto> OnVersionSelected { get; set; }

    private Task SelectVersion(PlanVersionDto version)
    {
        return OnVersionSelected.InvokeAsync(version);
    }
}
```

---

### Step 3: Update Ticket Detail Page

**File:** `/src/PRFactory.Web/Pages/Tickets/Detail.razor`

```razor
@page "/tickets/{TicketId:guid}"
@using PRFactory.Core.DTOs
@using PRFactory.Web.Components.Plans

<PageContainer Title="@($"Ticket {ticket?.TicketKey}")">

    @* Ticket Header *@
    @if (ticket != null)
    {
        <TicketHeader Ticket="@ticket" Class="mb-4" />
    }

    @* Plan Artifacts *@
    @if (ticket?.Plan != null)
    {
        <PlanArtifactsCard Plan="@ticket.Plan"
                           OnVersionSelected="HandleVersionSelected"
                           Class="mb-4" />

        @* Plan Revision (only show if plan is pending review) *@
        @if (ticket.State == WorkflowState.PlanGenerated ||
             ticket.State == WorkflowState.PlanRejected)
        {
            <PlanRevisionCard TicketId="@TicketId"
                              OnRevisionStarted="HandleRevisionStarted"
                              OnApproved="HandlePlanApproved"
                              Class="mb-4" />
        }
    }

    @* Other sections... *@

</PageContainer>
```

**Code-Behind:** `/src/PRFactory.Web/Pages/Tickets/Detail.razor.cs`

```csharp
using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Core.DTOs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Agents.Graphs;

namespace PRFactory.Web.Pages.Tickets;

public partial class Detail
{
    [Parameter]
    public Guid TicketId { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IWorkflowOrchestrator WorkflowOrchestrator { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private TicketDto? ticket;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadTicketAsync();
    }

    private async Task LoadTicketAsync()
    {
        isLoading = true;
        try
        {
            ticket = await TicketService.GetByIdAsync(TicketId);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleVersionSelected(PlanVersionDto version)
    {
        // TODO: Implement version diff viewer
        // Show side-by-side comparison of current vs selected version
    }

    private async Task HandleRevisionStarted()
    {
        // Reload ticket to show updated state
        await LoadTicketAsync();
    }

    private async Task HandlePlanApproved()
    {
        // Resume workflow with approval message
        await WorkflowOrchestrator.ResumeAsync(
            TicketId,
            new PlanApprovedMessage(
                TicketId,
                ApprovedBy: "current-user",  // TODO: Get from auth context
                Timestamp: DateTime.UtcNow));

        // Navigate or reload
        await LoadTicketAsync();
    }
}
```

---

### Step 4: Update Ticket Service

**File:** `/src/PRFactory.Web/Services/TicketService.cs`

Update to include plan with versions:

```csharp
public async Task<TicketDto?> GetByIdAsync(Guid ticketId)
{
    // Inject ITicketRepository and IPlanRepository
    var ticket = await _ticketRepository.GetByIdAsync(ticketId);
    if (ticket == null) return null;

    var plan = await _planRepository.GetByTicketIdAsync(ticketId);

    var dto = MapToDto(ticket);
    dto.Plan = plan != null ? MapPlanToDto(plan) : null;

    return dto;
}

private PlanDto MapPlanToDto(Plan plan)
{
    return new PlanDto
    {
        Id = plan.Id,
        TicketId = plan.TicketId,
        Content = plan.Content,
        UserStories = plan.UserStories,
        ApiDesign = plan.ApiDesign,
        DatabaseSchema = plan.DatabaseSchema,
        TestCases = plan.TestCases,
        ImplementationSteps = plan.ImplementationSteps,
        Version = plan.Version,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt,
        Versions = plan.Versions?
            .OrderByDescending(v => v.Version)
            .Select(v => new PlanVersionDto
            {
                Id = v.Id,
                PlanId = v.PlanId,
                Version = v.Version,
                UserStories = v.UserStories,
                ApiDesign = v.ApiDesign,
                DatabaseSchema = v.DatabaseSchema,
                TestCases = v.TestCases,
                ImplementationSteps = v.ImplementationSteps,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy,
                RevisionReason = v.RevisionReason
            })
            .ToList()
    };
}
```

---

## Testing

### Component Tests

Use bUnit for Blazor component testing:

**File:** `/tests/PRFactory.Web.Tests/Components/Plans/PlanArtifactsCardTests.cs`

```csharp
using Bunit;
using PRFactory.Web.Components.Plans;
using PRFactory.Core.DTOs;
using Xunit;

namespace PRFactory.Web.Tests.Components.Plans;

public class PlanArtifactsCardTests : TestContext
{
    [Fact]
    public void Render_WithMultipleArtifacts_ShowsAllTabs()
    {
        // Arrange
        var plan = new PlanDto
        {
            UserStories = "# User Stories\n...",
            ApiDesign = "openapi: 3.0.0\n...",
            DatabaseSchema = "CREATE TABLE...",
            TestCases = "# Test Cases\n...",
            ImplementationSteps = "# Implementation\n..."
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert
        Assert.Contains("User Stories", cut.Markup);
        Assert.Contains("API Design", cut.Markup);
        Assert.Contains("Database Schema", cut.Markup);
        Assert.Contains("Test Cases", cut.Markup);
        Assert.Contains("Implementation Steps", cut.Markup);
    }

    [Fact]
    public void Render_WithLegacyPlan_ShowsSinglePlan()
    {
        // Arrange
        var plan = new PlanDto
        {
            Content = "# Implementation Plan\n..."
        };

        // Act
        var cut = RenderComponent<PlanArtifactsCard>(parameters =>
            parameters.Add(p => p.Plan, plan));

        // Assert
        Assert.Contains("Implementation Plan", cut.Markup);
        Assert.DoesNotContain("User Stories", cut.Markup);
    }
}
```

---

## Acceptance Criteria

- [ ] `MarkdownViewer` component renders markdown with syntax highlighting
- [ ] `CodeBlock` component displays YAML and SQL with syntax highlighting (CSS-only, no JavaScript)
- [ ] `PlanArtifactsCard` displays all 5 artifacts in tabs
- [ ] Empty state shown for missing artifacts
- [ ] `PlanRevisionCard` accepts feedback and triggers revision
- [ ] `PlanVersionHistory` displays version list with metadata
- [ ] Ticket detail page integrates all components
- [ ] Revision and approval buttons wire up to workflow orchestrator
- [ ] Component tests (80% coverage)
- [ ] Mobile-responsive design
- [ ] Accessibility (ARIA labels, keyboard navigation)

---

## Styling Guidelines

Follow Bootstrap 5 and Radzen Blazor component styles. Ensure:

- Consistent spacing (use Bootstrap utility classes)
- Proper card shadows and borders
- Readable typography (line-height, font sizes)
- Color scheme matches existing UI
- Dark mode support (optional, future enhancement)

---

## Next Steps

After completing Web UI:
1. Implement plan revision workflow (see `04_REVISION_WORKFLOW.md`)
2. Wire up revision agent to regenerate specific artifacts
3. Add real-time updates (SignalR) for long-running revisions
