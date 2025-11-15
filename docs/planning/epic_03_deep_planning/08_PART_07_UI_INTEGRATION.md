# Part 7: Ticket Detail UI Integration - Implementation Plan

**Epic**: Deep Planning (Epic 03)
**Component**: Blazor Web UI Integration for Multi-Artifact Plans
**Estimated Effort**: 2-3 days
**Dependencies**: Part 1 (Agents), Part 2 (Database), Part 3 (Storage), Part 4 (Revision), Part 5 (Graph)
**Status**: 🚧 Not Implemented

---

## Overview

This part integrates the multi-artifact planning system with the Blazor UI, enabling users to:

1. **View all 5 plan artifacts** in a tabbed interface
2. **Review and approve/reject plans** with natural language feedback
3. **Track plan revisions** with version history
4. **Monitor plan status** within the ticket detail page

---

## Architecture

### Component Hierarchy

```
Pages/Tickets/Detail.razor
├── TicketHeader (existing)
├── TicketStatusCard (existing)
├── PlanArtifactsCard (NEW)
│   ├── Tabs (Radzen)
│   │   ├── UserStoriesTab
│   │   ├── ApiDesignTab
│   │   ├── DatabaseSchemaTab
│   │   ├── TestCasesTab
│   │   ├── ImplementationStepsTab
│   │   └── VersionHistoryTab (optional)
│   └── PlanActionsCard
│       ├── ApproveButton
│       ├── RejectButton
│       └── FeedbackInput
└── RelatedTicketsCard (existing)
```

### Data Flow

```
Ticket Detail Page Loads
  ↓
ITicketService.GetByIdAsync(ticketId)
  ↓
[Fetch from DB]
  ↓
Return TicketDto with nested PlanDto
  ↓
Blazor component renders tabs
  ↓
User provides feedback
  ↓
Approve/Revise buttons call IWorkflowOrchestrator
  ↓
Graph resumes with PlanApprovedMessage or PlanRejectedMessage
  ↓
Ticket status updates
  ↓
UI refreshes to show new state
```

---

## Implementation

### Part 1: DTOs and Models

**File**: `src/PRFactory.Web/Pages/Tickets/Dtos/PlanDto.cs`

```csharp
using System;
using System.Collections.Generic;

namespace PRFactory.Web.Pages.Tickets.Dtos;

/// <summary>
/// DTO representing a complete plan with all artifacts.
/// </summary>
public class PlanDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    // Multi-artifact plan
    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    // Versioning
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PlanVersionDto> Versions { get; set; } = new();

    // Computed properties
    public bool HasMultipleArtifacts =>
        !string.IsNullOrEmpty(UserStories) ||
        !string.IsNullOrEmpty(ApiDesign) ||
        !string.IsNullOrEmpty(DatabaseSchema) ||
        !string.IsNullOrEmpty(TestCases) ||
        !string.IsNullOrEmpty(ImplementationSteps);

    public int ArtifactCount =>
        (string.IsNullOrEmpty(UserStories) ? 0 : 1) +
        (string.IsNullOrEmpty(ApiDesign) ? 0 : 1) +
        (string.IsNullOrEmpty(DatabaseSchema) ? 0 : 1) +
        (string.IsNullOrEmpty(TestCases) ? 0 : 1) +
        (string.IsNullOrEmpty(ImplementationSteps) ? 0 : 1);

    public DateTime LastModified =>
        UpdatedAt ?? CreatedAt;
}

public class PlanVersionDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int Version { get; set; }

    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? RevisionReason { get; set; }
}

public class TicketDto
{
    public Guid Id { get; set; }
    public string TicketKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Nested plan
    public PlanDto? Plan { get; set; }

    // Other nested entities...
    public List<CommentDto> Comments { get; set; } = new();
    public List<QuestionDto> Questions { get; set; } = new();
}
```

**File**: `src/PRFactory.Web/Pages/Tickets/Dtos/EnumDto.cs`

```csharp
namespace PRFactory.Web.Pages.Tickets.Dtos;

/// <summary>
/// Workflow state enum - must match domain entity.
/// </summary>
public enum WorkflowState
{
    Draft = 0,
    AnalysisInProgress = 1,
    AnalysisComplete = 2,
    RefinementInProgress = 3,
    RefinementComplete = 4,
    PlanningInProgress = 5,
    PlanGenerated = 6,
    PlanApproved = 7,
    ImplementationInProgress = 8,
    ImplementationComplete = 9,
    PlanRejected = 10,
    Failed = 11
}
```

---

### Part 2: Mapper Configuration

**File**: `src/PRFactory.Web/Pages/Tickets/Mappers/PlanMapper.cs`

```csharp
using PRFactory.Domain.Entities;
using PRFactory.Web.Pages.Tickets.Dtos;
using System.Collections.Generic;

namespace PRFactory.Web.Pages.Tickets.Mappers;

/// <summary>
/// Maps between Plan domain entity and DTO.
/// </summary>
public static class PlanMapper
{
    public static PlanDto ToDto(Plan entity)
    {
        if (entity == null)
            return null!;

        return new PlanDto
        {
            Id = entity.Id,
            TicketId = entity.TicketId,
            UserStories = entity.UserStories,
            ApiDesign = entity.ApiDesign,
            DatabaseSchema = entity.DatabaseSchema,
            TestCases = entity.TestCases,
            ImplementationSteps = entity.ImplementationSteps,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Versions = entity.Versions?.ConvertAll(v => ToPlanVersionDto(v)) ?? new()
        };
    }

    public static PlanVersionDto ToPlanVersionDto(PlanVersion entity)
    {
        if (entity == null)
            return null!;

        return new PlanVersionDto
        {
            Id = entity.Id,
            PlanId = entity.PlanId,
            Version = entity.Version,
            UserStories = entity.UserStories,
            ApiDesign = entity.ApiDesign,
            DatabaseSchema = entity.DatabaseSchema,
            TestCases = entity.TestCases,
            ImplementationSteps = entity.ImplementationSteps,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            RevisionReason = entity.RevisionReason
        };
    }
}
```

---

### Part 3: Service Layer Integration

**File**: `src/PRFactory.Web/Services/TicketService.cs` (Update)

```csharp
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Web.Pages.Tickets.Dtos;
using PRFactory.Web.Pages.Tickets.Mappers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PRFactory.Web.Services;

/// <summary>
/// Facade service for Blazor components to access ticket data.
/// Uses dependency injection to avoid HTTP calls within Blazor Server.
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        IWorkflowOrchestrator workflowOrchestrator,
        ILogger<TicketService> logger)
    {
        ArgumentNullException.ThrowIfNull(ticketRepository);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        ArgumentNullException.ThrowIfNull(logger);

        _ticketRepository = ticketRepository;
        _workflowOrchestrator = workflowOrchestrator;
        _logger = logger;
    }

    public async Task<TicketDto?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);

        if (ticket == null)
            return null;

        return MapTicketToDto(ticket);
    }

    public async Task<TicketDto?> GetByKeyAsync(
        string ticketKey,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByKeyAsync(ticketKey, cancellationToken);

        if (ticket == null)
            return null;

        return MapTicketToDto(ticket);
    }

    /// <summary>
    /// Requests revision of the plan with user feedback.
    /// Resumes the PlanningGraph with rejection message.
    /// </summary>
    public async Task RequestPlanRevisionAsync(
        Guid ticketId,
        string feedback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            throw new ArgumentException("Feedback cannot be empty", nameof(feedback));

        _logger.LogInformation(
            "Requesting plan revision for ticket {TicketId}",
            ticketId);

        var rejectionMessage = new Domain.Messages.PlanRejectedMessage(
            ticketId,
            feedback,
            Regenerate: true,
            Timestamp: DateTime.UtcNow);

        await _workflowOrchestrator.ResumeAsync(ticketId, rejectionMessage, cancellationToken);
    }

    /// <summary>
    /// Approves the current plan and proceeds to implementation phase.
    /// Resumes the PlanningGraph with approval message.
    /// </summary>
    public async Task ApprovePlanAsync(
        Guid ticketId,
        string? approvedBy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Approving plan for ticket {TicketId}",
            ticketId);

        var approvalMessage = new Domain.Messages.PlanApprovedMessage(
            ticketId,
            approvedBy: approvedBy ?? "web-user",
            Timestamp: DateTime.UtcNow);

        await _workflowOrchestrator.ResumeAsync(ticketId, approvalMessage, cancellationToken);
    }

    private TicketDto MapTicketToDto(Domain.Entities.Ticket ticket)
    {
        return new TicketDto
        {
            Id = ticket.Id,
            TicketKey = ticket.TicketKey,
            Title = ticket.Title,
            Description = ticket.Description,
            State = (WorkflowState)ticket.State,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Plan = ticket.Plan != null ? PlanMapper.ToDto(ticket.Plan) : null,
            Comments = ticket.Comments?.ConvertAll(c => MapCommentToDto(c)) ?? new(),
            Questions = ticket.Questions?.ConvertAll(q => MapQuestionToDto(q)) ?? new()
        };
    }

    // Helper mapping methods...
    private CommentDto MapCommentToDto(Domain.Entities.Comment comment) => /* ... */;
    private QuestionDto MapQuestionToDto(Domain.Entities.Question question) => /* ... */;
}

public interface ITicketService
{
    Task<TicketDto?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<TicketDto?> GetByKeyAsync(string ticketKey, CancellationToken cancellationToken = default);
    Task RequestPlanRevisionAsync(Guid ticketId, string feedback, CancellationToken cancellationToken = default);
    Task ApprovePlanAsync(Guid ticketId, string? approvedBy = null, CancellationToken cancellationToken = default);
}
```

---

### Part 4: UI Components

#### PlanArtifactsCard Component

**File**: `src/PRFactory.Web/Components/Tickets/PlanArtifactsCard.razor`

```razor
@using PRFactory.Web.Pages.Tickets.Dtos
@using Radzen.Blazor

<Card Title="Implementation Plan Artifacts" Icon="folder" Class="mb-4">
    @if (Plan?.HasMultipleArtifacts == true)
    {
        <div class="plan-artifacts">
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
                                <EmptyState
                                    Message="User stories not yet generated"
                                    Icon="file-earmark-text" />
                            }
                        </div>
                    </RadzenTabsItem>

                    @* API Design Tab *@
                    <RadzenTabsItem Text="API Design (OpenAPI)">
                        <div class="p-3">
                            @if (!string.IsNullOrEmpty(Plan.ApiDesign))
                            {
                                <CodeEditor
                                    Content="@Plan.ApiDesign"
                                    Language="yaml"
                                    ReadOnly="true"
                                    Height="600px" />
                            }
                            else
                            {
                                <EmptyState
                                    Message="API design not yet generated"
                                    Icon="code-slash" />
                            }
                        </div>
                    </RadzenTabsItem>

                    @* Database Schema Tab *@
                    <RadzenTabsItem Text="Database Schema (SQL)">
                        <div class="p-3">
                            @if (!string.IsNullOrEmpty(Plan.DatabaseSchema))
                            {
                                <CodeEditor
                                    Content="@Plan.DatabaseSchema"
                                    Language="sql"
                                    ReadOnly="true"
                                    Height="600px" />
                            }
                            else
                            {
                                <EmptyState
                                    Message="Database schema not yet generated"
                                    Icon="database" />
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
                                <EmptyState
                                    Message="Test cases not yet generated"
                                    Icon="check2-square" />
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
                                <EmptyState
                                    Message="Implementation steps not yet generated"
                                    Icon="list-ol" />
                            }
                        </div>
                    </RadzenTabsItem>

                    @* Version History Tab (Optional) *@
                    @if (Plan.Versions?.Count > 1)
                    {
                        <RadzenTabsItem Text="Version History">
                            <div class="p-3">
                                <PlanVersionHistory
                                    Plan="@Plan"
                                    Versions="@Plan.Versions"
                                    OnVersionSelected="HandleVersionSelected" />
                            </div>
                        </RadzenTabsItem>
                    }
                </Tabs>
            </RadzenTabs>

            @* Plan Actions Card *@
            @if (CanReviewPlan)
            {
                <PlanActionsCard
                    TicketId="@TicketId"
                    OnPlanApproved="OnPlanApproved"
                    OnPlanRevision="OnPlanRevision" />
            }
        </div>
    }
    else if (Plan != null)
    {
        @* Fallback for legacy single-file plans *@
        <MarkdownViewer Content="@Plan.UserStories" />
    }
    else
    {
        <EmptyState
            Message="No plan has been generated for this ticket yet"
            Icon="file-earmark-text" />
    }
</Card>

@code {
    [Parameter]
    [EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public PlanDto? Plan { get; set; }

    [Parameter]
    public WorkflowState TicketState { get; set; }

    [Parameter]
    public EventCallback OnPlanApproved { get; set; }

    [Parameter]
    public EventCallback<string> OnPlanRevision { get; set; }

    private bool CanReviewPlan =>
        TicketState == WorkflowState.PlanGenerated ||
        TicketState == WorkflowState.PlanRejected;

    private async Task HandleVersionSelected(PlanVersionDto version)
    {
        // Implementation for version selection
        // Could show diff or full version details
    }
}
```

#### PlanActionsCard Component

**File**: `src/PRFactory.Web/Components/Tickets/PlanActionsCard.razor`

```razor
@using PRFactory.Web.Pages.Tickets.Dtos

<Card Title="Review & Approve" Icon="check-circle" Class="mb-4">
    <div class="plan-actions">
        <FormField Label="Feedback (optional for approval)">
            <InputTextArea
                @bind-Value="@revisionFeedback"
                rows="4"
                class="form-control"
                placeholder="e.g., Add rate limiting to API, include caching strategy in implementation steps" />
        </FormField>

        <div class="d-flex gap-2 mt-3">
            <LoadingButton
                OnClick="HandleApprove"
                IsLoading="@isApproving"
                Icon="check-circle"
                Class="btn-success">
                Approve Plan
            </LoadingButton>

            <LoadingButton
                OnClick="HandleRevision"
                IsLoading="@isRevising"
                Icon="arrow-repeat"
                Class="btn-warning"
                Disabled="@string.IsNullOrWhiteSpace(revisionFeedback)">
                Request Revision
            </LoadingButton>
        </div>

        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <AlertMessage
                Type="AlertType.Danger"
                Message="@errorMessage"
                Dismissible="true"
                OnDismiss="ClearError" />
        }

        @if (!string.IsNullOrEmpty(successMessage))
        {
            <AlertMessage
                Type="AlertType.Success"
                Message="@successMessage"
                Dismissible="true"
                OnDismiss="ClearSuccess" />
        }
    </div>
</Card>

@code {
    [Parameter]
    [EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public EventCallback OnPlanApproved { get; set; }

    [Parameter]
    public EventCallback<string> OnPlanRevision { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private string revisionFeedback = string.Empty;
    private bool isApproving;
    private bool isRevising;
    private string? errorMessage;
    private string? successMessage;

    private async Task HandleApprove()
    {
        isApproving = true;
        try
        {
            await TicketService.ApprovePlanAsync(TicketId);
            successMessage = "Plan approved successfully. Moving to implementation phase...";
            await OnPlanApproved.InvokeAsync();

            // Refresh page after brief delay
            await Task.Delay(1500);
            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to approve plan: {ex.Message}";
        }
        finally
        {
            isApproving = false;
        }
    }

    private async Task HandleRevision()
    {
        if (string.IsNullOrWhiteSpace(revisionFeedback))
        {
            errorMessage = "Feedback is required for revision requests";
            return;
        }

        isRevising = true;
        try
        {
            await TicketService.RequestPlanRevisionAsync(TicketId, revisionFeedback);
            successMessage = "Revision request submitted. Plan will be regenerated...";
            await OnPlanRevision.InvokeAsync(revisionFeedback);

            revisionFeedback = string.Empty;

            // Refresh page after brief delay
            await Task.Delay(1500);
            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to request revision: {ex.Message}";
        }
        finally
        {
            isRevising = false;
        }
    }

    private void ClearError() => errorMessage = null;
    private void ClearSuccess() => successMessage = null;
}
```

#### PlanVersionHistory Component

**File**: `src/PRFactory.Web/Components/Tickets/PlanVersionHistory.razor`

```razor
@using PRFactory.Web.Pages.Tickets.Dtos

<div class="version-history">
    @if (Versions?.Count > 0)
    {
        <div class="timeline">
            @foreach (var version in Versions.OrderByDescending(v => v.Version))
            {
                <div class="timeline-item">
                    <div class="timeline-header">
                        <span class="version-badge">v@version.Version</span>
                        <span class="version-date">
                            @version.CreatedAt.ToString("g")
                        </span>
                        @if (!string.IsNullOrEmpty(version.CreatedBy))
                        {
                            <span class="version-author">by @version.CreatedBy</span>
                        }
                    </div>

                    @if (!string.IsNullOrEmpty(version.RevisionReason))
                    {
                        <div class="version-reason">
                            <strong>Revision reason:</strong>
                            <p>@version.RevisionReason</p>
                        </div>
                    }

                    <div class="version-artifacts">
                        <small>Artifacts in this version:</small>
                        <div class="artifact-badges">
                            @if (!string.IsNullOrEmpty(version.UserStories))
                            {
                                <span class="badge bg-info">User Stories</span>
                            }
                            @if (!string.IsNullOrEmpty(version.ApiDesign))
                            {
                                <span class="badge bg-info">API Design</span>
                            }
                            @if (!string.IsNullOrEmpty(version.DatabaseSchema))
                            {
                                <span class="badge bg-info">Database Schema</span>
                            }
                            @if (!string.IsNullOrEmpty(version.TestCases))
                            {
                                <span class="badge bg-info">Test Cases</span>
                            }
                            @if (!string.IsNullOrEmpty(version.ImplementationSteps))
                            {
                                <span class="badge bg-info">Implementation Steps</span>
                            }
                        </div>
                    </div>

                    @if (version.Version != Versions.Max(v => v.Version))
                    {
                        <button class="btn btn-sm btn-outline-secondary mt-2"
                                @onclick="() => OnVersionSelected.InvokeAsync(version)">
                            View Full Version
                        </button>
                    }
                </div>
            }
        </div>
    }
    else
    {
        <p class="text-muted">No version history available</p>
    }
</div>

<style>
    .version-history {
        padding: 1rem 0;
    }

    .timeline {
        position: relative;
        padding-left: 2rem;
    }

    .timeline::before {
        content: '';
        position: absolute;
        left: 0.5rem;
        top: 0;
        bottom: 0;
        width: 2px;
        background: #dee2e6;
    }

    .timeline-item {
        position: relative;
        margin-bottom: 1.5rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid #f0f0f0;
    }

    .timeline-item::before {
        content: '';
        position: absolute;
        left: -1.25rem;
        top: 0.25rem;
        width: 1rem;
        height: 1rem;
        border-radius: 50%;
        background: #fff;
        border: 2px solid #0d6efd;
    }

    .timeline-header {
        display: flex;
        gap: 1rem;
        align-items: center;
        font-weight: 500;
    }

    .version-badge {
        display: inline-block;
        background: #e7f1ff;
        color: #0d6efd;
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
        font-size: 0.875rem;
    }

    .version-date {
        color: #6c757d;
        font-size: 0.875rem;
    }

    .version-author {
        color: #6c757d;
        font-size: 0.875rem;
        font-style: italic;
    }

    .version-reason {
        margin-top: 0.5rem;
        padding: 0.75rem;
        background: #f8f9fa;
        border-left: 3px solid #ffc107;
        border-radius: 4px;
    }

    .version-reason p {
        margin: 0.5rem 0 0 0;
    }

    .version-artifacts {
        margin-top: 0.75rem;
    }

    .artifact-badges {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-top: 0.5rem;
    }
</style>

@code {
    [Parameter]
    public PlanDto? Plan { get; set; }

    [Parameter]
    public List<PlanVersionDto> Versions { get; set; } = new();

    [Parameter]
    public EventCallback<PlanVersionDto> OnVersionSelected { get; set; }
}
```

---

### Part 5: Detail Page Integration

**File**: `src/PRFactory.Web/Pages/Tickets/Detail.razor` (Update)

```razor
@page "/tickets/{TicketId:guid}"
@using PRFactory.Web.Pages.Tickets.Dtos

<PageHeader Title="@ticket?.Title" Subtitle="@ticket?.TicketKey" Icon="ticket">
    <StatusBadge State="@ticket?.State" />
</PageHeader>

<div class="container-fluid mt-4">
    <GridLayout>
        <GridColumn Cols="12" Lg="8">
            @* Existing ticket header and details *@
            <TicketHeader Ticket="@ticket" />

            @* NEW: Plan Artifacts Section *@
            @if (ticket?.Plan != null)
            {
                <PlanArtifactsCard
                    TicketId="@TicketId"
                    Plan="@ticket.Plan"
                    TicketState="@ticket.State"
                    OnPlanApproved="HandlePlanApproved"
                    OnPlanRevision="HandlePlanRevision" />
            }

            @* Existing related sections *@
            <CommentsCard TicketId="@TicketId" Comments="@ticket?.Comments" />
        </GridColumn>

        <GridColumn Cols="12" Lg="4">
            <TicketMetadataCard Ticket="@ticket" />
            <QuestionAnswerForm TicketId="@TicketId" />
        </GridColumn>
    </GridLayout>
</div>

@code {
    [Parameter]
    public Guid TicketId { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private TicketDto? ticket;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadTicketAsync();
    }

    private async Task LoadTicketAsync()
    {
        try
        {
            isLoading = true;
            ticket = await TicketService.GetByIdAsync(TicketId);

            if (ticket == null)
            {
                errorMessage = "Ticket not found";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load ticket: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandlePlanApproved()
    {
        // Reload ticket to reflect new state
        await LoadTicketAsync();
    }

    private async Task HandlePlanRevision(string feedback)
    {
        // Reload ticket to show updated plan
        await Task.Delay(2000); // Wait for revision to complete
        await LoadTicketAsync();
    }
}
```

**File**: `src/PRFactory.Web/Pages/Tickets/Detail.razor.css`

```css
/* Plan artifacts styling */
.plan-artifacts {
    margin-top: 1rem;
}

.plan-actions {
    margin-top: 1.5rem;
}

/* Code editor styling */
:deep(.code-editor) {
    border: 1px solid #dee2e6;
    border-radius: 4px;
    background: #f8f9fa;
}

/* Markdown viewer styling */
:deep(.markdown-viewer) {
    line-height: 1.6;
    color: #333;
}

:deep(.markdown-viewer h1,
:deep(.markdown-viewer h2,
:deep(.markdown-viewer h3 {
    margin-top: 1rem;
    margin-bottom: 0.5rem;
    border-bottom: 1px solid #eee;
    padding-bottom: 0.5rem;
}

:deep(.markdown-viewer code {
    background: #f4f4f4;
    padding: 0.2rem 0.4rem;
    border-radius: 3px;
    font-family: 'Courier New', monospace;
}

:deep(.markdown-viewer pre {
    background: #f4f4f4;
    padding: 1rem;
    border-radius: 4px;
    overflow-x: auto;
}

:deep(.markdown-viewer ul,
:deep(.markdown-viewer ol {
    margin-left: 1.5rem;
}

/* Tab styling */
:deep(.rz-tabs) {
    margin-top: 1rem;
}

:deep(.rz-tab-label) {
    padding: 0.75rem 1rem;
    font-weight: 500;
}

:deep(.rz-tab-content) {
    padding: 1rem 0;
}
```

---

### Part 6: Unit and Integration Tests

**File**: `tests/PRFactory.Tests/Web/Pages/Tickets/DetailPageTests.cs`

```csharp
using Xunit;
using Moq;
using Bunit;
using PRFactory.Web.Pages.Tickets;
using PRFactory.Web.Pages.Tickets.Dtos;
using PRFactory.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PRFactory.Tests.Web.Pages.Tickets;

public class DetailPageTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<NavigationManager> _mockNavigation;

    public DetailPageTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockNavigation = new Mock<NavigationManager>();

        Services.AddScoped(_ => _mockTicketService.Object);
        Services.AddScoped(_ => _mockNavigation.Object);
    }

    [Fact]
    public async Task Page_LoadsWithValidTicket_DisplaysPlan()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketDto = CreateTicketDtoWithPlan(ticketId);

        _mockTicketService
            .Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticketDto);

        // Act
        var component = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        await component.InvokeAsync(async () => await Task.Delay(100));

        // Assert
        component.FindComponent<PlanArtifactsCard>();
        component.Find(".plan-artifacts");
    }

    [Fact]
    public async Task Page_WithoutPlan_DoesNotShowPlanCard()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketDto = CreateTicketDtoWithoutPlan(ticketId);

        _mockTicketService
            .Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticketDto);

        // Act
        var component = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        await component.InvokeAsync(async () => await Task.Delay(100));

        // Assert
        Assert.Throws<ElementNotFoundException>(() => component.Find(".plan-artifacts"));
    }

    [Fact]
    public async Task PlanActionsCard_WithValidApproval_CallsService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketDto = CreateTicketDtoWithPlan(ticketId);

        _mockTicketService
            .Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticketDto);

        _mockTicketService
            .Setup(x => x.ApprovePlanAsync(ticketId, null, default))
            .Returns(Task.CompletedTask);

        var component = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        await component.InvokeAsync(async () => await Task.Delay(100));

        // Act
        var actionCard = component.FindComponent<PlanActionsCard>();
        var approveButton = actionCard.Find(".btn-success");
        await approveButton.ClickAsync(new());

        // Assert
        _mockTicketService.Verify(
            x => x.ApprovePlanAsync(ticketId, null, default),
            Times.Once);
    }

    [Fact]
    public async Task PlanActionsCard_WithRevisionFeedback_CallsRevisionService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Add rate limiting";
        var ticketDto = CreateTicketDtoWithPlan(ticketId);

        _mockTicketService
            .Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticketDto);

        _mockTicketService
            .Setup(x => x.RequestPlanRevisionAsync(ticketId, feedback, default))
            .Returns(Task.CompletedTask);

        var component = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        await component.InvokeAsync(async () => await Task.Delay(100));

        // Act
        var actionCard = component.FindComponent<PlanActionsCard>();
        var feedbackInput = actionCard.Find("textarea");
        await feedbackInput.ChangeAsync(feedback);

        var reviseButton = actionCard.Find(".btn-warning");
        await reviseButton.ClickAsync(new());

        // Assert
        _mockTicketService.Verify(
            x => x.RequestPlanRevisionAsync(ticketId, feedback, default),
            Times.Once);
    }

    [Fact]
    public async Task PlanVersionHistory_WithMultipleVersions_DisplaysTimeline()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticketDto = CreateTicketDtoWithPlanVersions(ticketId, 3);

        _mockTicketService
            .Setup(x => x.GetByIdAsync(ticketId, default))
            .ReturnsAsync(ticketDto);

        // Act
        var component = RenderComponent<Detail>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        await component.InvokeAsync(async () => await Task.Delay(100));

        // Assert
        var versionHistory = component.FindComponent<PlanVersionHistory>();
        Assert.NotNull(versionHistory);
        Assert.Equal(3, versionHistory.Instance.Versions.Count);
    }

    // Helper methods

    private TicketDto CreateTicketDtoWithPlan(Guid ticketId)
    {
        return new TicketDto
        {
            Id = ticketId,
            TicketKey = "PROJ-100",
            Title = "Test Feature",
            Description = "Test Description",
            State = WorkflowState.PlanGenerated,
            CreatedAt = DateTime.UtcNow,
            Plan = new PlanDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                UserStories = "## User Story 1\nAs a user...",
                ApiDesign = "openapi: 3.0.0\ninfo:\n  title: Test API",
                DatabaseSchema = "CREATE TABLE...",
                TestCases = "## Test Case 1\nGiven...",
                ImplementationSteps = "## Step 1\nImplement...",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private TicketDto CreateTicketDtoWithoutPlan(Guid ticketId)
    {
        return new TicketDto
        {
            Id = ticketId,
            TicketKey = "PROJ-101",
            Title = "Test Feature",
            Description = "Test Description",
            State = WorkflowState.Draft,
            CreatedAt = DateTime.UtcNow,
            Plan = null
        };
    }

    private TicketDto CreateTicketDtoWithPlanVersions(Guid ticketId, int versionCount)
    {
        var ticket = CreateTicketDtoWithPlan(ticketId);

        for (int i = 2; i <= versionCount; i++)
        {
            ticket.Plan!.Versions.Add(new PlanVersionDto
            {
                Id = Guid.NewGuid(),
                PlanId = ticket.Plan.Id,
                Version = i,
                UserStories = $"Updated user stories v{i}",
                ApiDesign = $"Updated API v{i}",
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                CreatedBy = "reviewer@example.com",
                RevisionReason = $"Revision feedback for version {i}"
            });
        }

        return ticket;
    }
}
```

---

## Acceptance Criteria

### UI Components
- [ ] `PlanArtifactsCard` displays all 5 artifacts in tabs
- [ ] Code editor (YAML/SQL) with syntax highlighting
- [ ] Markdown viewer for user stories/test cases
- [ ] Empty state messages for missing artifacts
- [ ] Optional version history tab

### Plan Actions
- [ ] "Approve Plan" button available when state is PlanGenerated/PlanRejected
- [ ] "Request Revision" button with feedback input
- [ ] Revision feedback validation (non-empty)
- [ ] Success/error notifications on approve/revise
- [ ] Page refresh after approval/revision

### Data Mapping
- [ ] `PlanDto` with all 5 artifact properties
- [ ] `PlanVersionDto` for version history
- [ ] `TicketDto` includes nested `PlanDto`
- [ ] `TicketService` provides `RequestPlanRevisionAsync()` and `ApprovePlanAsync()`
- [ ] No HTTP calls within Blazor Server (direct DI)

### Service Integration
- [ ] `TicketService` injects `IWorkflowOrchestrator`
- [ ] `ApprovePlanAsync()` sends `PlanApprovedMessage`
- [ ] `RequestPlanRevisionAsync()` sends `PlanRejectedMessage`
- [ ] Error handling with try-catch blocks
- [ ] Logging for approval/revision events

### Testing
- [ ] Unit tests for component rendering (12+ tests)
- [ ] Integration tests for service calls (8+ tests)
- [ ] bUnit tests for Blazor components
- [ ] Mock `ITicketService` for testing
- [ ] xUnit assertions only (no FluentAssertions)

---

## Implementation Checklist

- [ ] Create `PlanDto` and `PlanVersionDto` DTOs
- [ ] Create `WorkflowState` enum in DTOs
- [ ] Create `PlanMapper` static mapping methods
- [ ] Update `TicketService` with revision/approval methods
- [ ] Create `ITicketService` interface
- [ ] Create `PlanArtifactsCard.razor` component
- [ ] Create `PlanActionsCard.razor` component
- [ ] Create `PlanVersionHistory.razor` component
- [ ] Update `Detail.razor` page to include PlanArtifactsCard
- [ ] Add CSS styling for plan artifacts
- [ ] Create 12+ component integration tests
- [ ] Create 8+ service unit tests
- [ ] Verify all tests pass with xUnit
- [ ] Verify >80% code coverage
- [ ] Run `dotnet test` successfully

---

## Files to Create/Modify

**Create:**
- `src/PRFactory.Web/Pages/Tickets/Dtos/PlanDto.cs`
- `src/PRFactory.Web/Pages/Tickets/Dtos/EnumDto.cs`
- `src/PRFactory.Web/Pages/Tickets/Mappers/PlanMapper.cs`
- `src/PRFactory.Web/Components/Tickets/PlanArtifactsCard.razor`
- `src/PRFactory.Web/Components/Tickets/PlanActionsCard.razor`
- `src/PRFactory.Web/Components/Tickets/PlanVersionHistory.razor`
- `src/PRFactory.Web/Pages/Tickets/Detail.razor.css`
- `tests/PRFactory.Tests/Web/Pages/Tickets/DetailPageTests.cs`

**Modify:**
- `src/PRFactory.Web/Services/TicketService.cs` - Add `RequestPlanRevisionAsync()` and `ApprovePlanAsync()`
- `src/PRFactory.Web/Pages/Tickets/Detail.razor` - Add PlanArtifactsCard integration

---

## Dependencies

**Internal:**
- `ITicketService` (data access)
- `IWorkflowOrchestrator` (workflow resumption)
- `ITicketRepository` (database)
- Message types: `PlanApprovedMessage`, `PlanRejectedMessage`
- UI Components: `Card`, `FormField`, `LoadingButton`, `AlertMessage`, `EmptyState`
- Radzen.Blazor (tabs)

**External:**
- Blazor Server components
- Bootstrap 5 (CSS utilities)
- Radzen Blazor components

---

## Estimated Effort

- **DTOs and Mappers**: 2-3 hours
- **Service Layer Updates**: 2-3 hours
- **UI Components (3 components)**: 4-6 hours
- **Detail Page Integration**: 2-3 hours
- **Component Tests**: 4-6 hours
- **CSS Styling**: 1-2 hours
- **Total**: 2-3 days

---

## Related Parts

- **Part 1-5**: All previous parts (required dependencies)
- **Part 1**: Multi-artifact agents (data source)
- **Part 5**: Graph orchestration (provides approval/revision logic)

---

## Success Metrics

- All 5 artifacts displayed in separate tabs
- Approve/revise workflow completes without errors
- Page refreshes automatically after workflow actions
- 20+ unit/integration tests, all passing
- >80% code coverage
- No HTTP calls from Blazor components (direct DI only)
- Code-behind pattern used for business components
- Radzen and Blazor only (no custom JS)
