# Refinement Workflow Enhancement Plan

## Executive Summary

This plan addresses critical gaps in the ticket refinement workflow to enable Claude Code (via SDK) to generate ticket updates with success criteria, provide preview/editing capabilities, and implement a configurable prompts system.

**Goal**: Have Claude Code write 95% of ticket updates automatically while allowing human review and manual adjustments before posting to Jira.

---

## Table of Contents

- [Current State Analysis](#current-state-analysis)
- [Proposed Architecture](#proposed-architecture)
- [Implementation Phases](#implementation-phases)
- [Technical Details](#technical-details)
- [Integration Points](#integration-points)
- [Testing Strategy](#testing-strategy)
- [Timeline Estimates](#timeline-estimates)

---

## Current State Analysis

### Existing Refinement Workflow

**Current Flow:**
```
┌────────────────────────────────────────────────────────────────┐
│ RefinementGraph (Current Implementation)                       │
├────────────────────────────────────────────────────────────────┤
│ 1. Trigger                                                     │
│ 2. RepositoryClone                                             │
│ 3. Analysis (with retry)                                       │
│ 4. QuestionGeneration                                          │
│ 5. JiraPost (posts questions)                                  │
│ 6. [SUSPEND - Wait for answers via webhook]                   │
│ 7. AnswerProcessing                                            │
│ 8. ✅ Mark as complete                                         │
└────────────────────────────────────────────────────────────────┘
```

**Problem**: After processing answers, the workflow just marks itself as complete. It does NOT:
- Generate an updated ticket description
- Create success criteria
- Provide a preview for human review
- Allow manual editing
- Update the Jira ticket

### Integration Gaps

#### 1. Claude Integration
**Current (`ClaudeClient.cs:92`):**
```csharp
_logger.LogWarning("ClaudeClient is using placeholder implementation.
                    Integrate Anthropic SDK for production use.");
```

**Issues:**
- Uses placeholder HTTP client
- No actual Claude API integration
- No Claude Code SDK integration
- Returns hardcoded "Placeholder response"

#### 2. Prompt System
**Current (`PromptTemplates.cs`):**
- All prompts are static `const string` fields
- No database storage
- No configuration UI
- No versioning
- No tenant-specific customization
- **Missing**: Ticket update prompt template

#### 3. Web UI
**Exists:**
- ✅ `QuestionAnswerForm.razor` - for answering questions
- ✅ `PlanReviewSection.razor` - for reviewing implementation plans

**Missing:**
- ❌ Ticket update preview component
- ❌ Ticket update editor component
- ❌ Success criteria editor
- ❌ Diff viewer (original vs. updated ticket)

---

## Proposed Architecture

### Enhanced Refinement Workflow

**New Flow:**
```
┌────────────────────────────────────────────────────────────────────────┐
│ RefinementGraph (Enhanced)                                             │
├────────────────────────────────────────────────────────────────────────┤
│ 1. Trigger                                                             │
│ 2. RepositoryClone                                                     │
│ 3. Analysis (with retry)                                               │
│ 4. QuestionGeneration                                                  │
│ 5. JiraPost (posts questions)                                          │
│ 6. [SUSPEND - Wait for answers via webhook]                           │
│ 7. AnswerProcessing                                                    │
│ 8. ✨ TicketUpdateGeneration (NEW)                                     │
│ 9. ✨ [PostToWebUI + SaveDraft] (parallel, NEW)                       │
│10. ✨ [SUSPEND - Wait for approval via web UI] (NEW)                  │
│11. ✨ TicketUpdatePost (conditional, NEW)                             │
│12. ✅ Mark as complete                                                 │
└────────────────────────────────────────────────────────────────────────┘
```

### New Workflow States

Add to `WorkflowState` enum (Domain/ValueObjects/WorkflowState.cs):

```csharp
public enum WorkflowState
{
    // ... existing states ...

    // NEW STATES for ticket update workflow
    TicketUpdateGenerated,      // AI generated ticket update
    TicketUpdateUnderReview,    // Awaiting human approval in web UI
    TicketUpdateRejected,       // Human rejected the update, needs regeneration
    TicketUpdateApproved,       // Human approved, ready to post to Jira
    TicketUpdatePosted,         // Update posted to Jira

    // ... rest of states ...
}
```

### New Domain Entities

#### 1. TicketUpdate Entity

```csharp
// Domain/Entities/TicketUpdate.cs
public class TicketUpdate
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    // Generated content
    public string UpdatedTitle { get; set; } = string.Empty;
    public string UpdatedDescription { get; set; } = string.Empty;
    public List<SuccessCriterion> SuccessCriteria { get; set; } = new();
    public string AcceptanceCriteria { get; set; } = string.Empty;

    // Metadata
    public int Version { get; set; } = 1;
    public bool IsDraft { get; set; } = true;
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }

    // Audit
    public DateTime GeneratedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public string GeneratedBy { get; set; } = "Claude"; // Could be "Human" if manually edited
}

public class SuccessCriterion
{
    public Guid Id { get; set; }
    public Guid TicketUpdateId { get; set; }
    public string Category { get; set; } = string.Empty; // "Functional", "Technical", "Testing"
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 0; // 0=must-have, 1=should-have, 2=nice-to-have
    public bool IsTestable { get; set; } = true;
}
```

#### 2. PromptTemplate Entity

```csharp
// Domain/Entities/PromptTemplate.cs
public class PromptTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // "ticket_update", "analysis", etc.
    public string DisplayName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty; // Can use {{placeholders}}

    // Configuration
    public Guid? TenantId { get; set; } // null = global template
    public Tenant? Tenant { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    // Metadata
    public string Category { get; set; } = string.Empty; // "refinement", "planning", "implementation"
    public Dictionary<string, string> Parameters { get; set; } = new(); // Available placeholders

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
```

---

## Implementation Phases

### Phase 1: Foundation - Claude Code SDK Integration ⚡

**Priority**: CRITICAL (Everything depends on this)

**Tasks:**
1. **Integrate Anthropic SDK**
   - Install NuGet package: `Anthropic.SDK` (official .NET SDK)
   - Replace placeholder `ClaudeClient` implementation
   - Implement proper API calls with streaming support
   - Add retry logic and error handling
   - Add token usage tracking

2. **Claude Code SDK Integration**
   - Research available .NET SDKs for Claude Code
   - Options:
     - **Option A**: Use Anthropic's official SDK with extended prompting
     - **Option B**: Integrate with Claude Desktop API (if available)
     - **Option C**: Use MCP (Model Context Protocol) for tool use
   - Implement chosen integration
   - Add configuration for Claude Code specific features

3. **Enhanced Context Building**
   - Update `ContextBuilder.cs` to provide better context for Claude
   - Include:
     - Original ticket
     - All Q&A pairs
     - Codebase analysis summary
     - Repository conventions

**Files to Modify:**
- `/src/PRFactory.Infrastructure/Claude/ClaudeClient.cs` (replace placeholder)
- `/src/PRFactory.Infrastructure/PRFactory.Infrastructure.csproj` (add NuGet packages)
- `/src/PRFactory.Infrastructure/Claude/ContextBuilder.cs` (enhance)
- Add: `/src/PRFactory.Infrastructure/Claude/ClaudeCodeClient.cs` (new)

**Estimated Time**: 8-12 hours

---

### Phase 2: Prompt Management System 📝

**Priority**: HIGH

**Tasks:**
1. **Database Schema**
   - Create `PromptTemplates` table
   - Create migration
   - Add EF Core configuration

2. **Domain Layer**
   - Create `PromptTemplate` entity
   - Create `IPromptTemplateRepository` interface
   - Add validation rules

3. **Infrastructure Layer**
   - Implement `PromptTemplateRepository`
   - Create `PromptService` for template rendering
   - Implement placeholder replacement (e.g., `{{ticket.title}}`)

4. **Seed Default Prompts**
   - Migration to seed default prompts from `PromptTemplates.cs`
   - Add new "ticket_update" prompt template:

```csharp
public const string TICKET_UPDATE_SYSTEM_PROMPT = @"
You are an expert business analyst refining software tickets based on Q&A sessions.

Your task is to:
1. Review the original ticket description
2. Incorporate answers to all clarifying questions
3. Generate a comprehensive, unambiguous ticket description
4. Create SMART success criteria (Specific, Measurable, Achievable, Relevant, Time-bound)
5. Define clear acceptance criteria

Guidelines:
- Keep the original intent but add necessary details
- Remove ambiguities
- Use clear, active language
- Break down complex requirements into discrete success criteria
- Ensure success criteria are testable
- Categorize success criteria: Functional, Technical, Testing, UX

Output Format (JSON):
{
  ""updatedTitle"": ""Clear, concise title"",
  ""updatedDescription"": ""Comprehensive description with context..."",
  ""successCriteria"": [
    {
      ""category"": ""Functional"",
      ""description"": ""User can authenticate using email and password"",
      ""priority"": 0,
      ""isTestable"": true
    }
  ],
  ""acceptanceCriteria"": ""Markdown list of acceptance criteria""
}

IMPORTANT: Return ONLY valid JSON, no additional text.
";
```

5. **API Endpoints**
   - `GET /api/prompts` - List all templates (with tenant filtering)
   - `GET /api/prompts/{id}` - Get specific template
   - `POST /api/prompts` - Create new template
   - `PUT /api/prompts/{id}` - Update template
   - `DELETE /api/prompts/{id}` - Delete template
   - `GET /api/prompts/category/{category}` - Get by category

**Files to Create:**
- `/src/PRFactory.Domain/Entities/PromptTemplate.cs`
- `/src/PRFactory.Domain/Repositories/IPromptTemplateRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Configurations/PromptTemplateConfiguration.cs`
- `/src/PRFactory.Infrastructure/Persistence/Repositories/PromptTemplateRepository.cs`
- `/src/PRFactory.Infrastructure/Services/PromptService.cs`
- `/src/PRFactory.Infrastructure/Persistence/Migrations/YYYYMMDD_AddPromptTemplates.cs`
- `/src/PRFactory.Api/Controllers/PromptsController.cs`

**Estimated Time**: 10-14 hours

---

### Phase 3: Ticket Update Generation (Backend) 🤖

**Priority**: HIGH

**Tasks:**
1. **Domain Layer**
   - Create `TicketUpdate` entity
   - Create `SuccessCriterion` value object
   - Create `ITicketUpdateRepository` interface

2. **Infrastructure Layer**
   - Implement `TicketUpdateRepository`
   - Create EF Core configuration and migration
   - Create `TicketUpdateGenerationAgent`

3. **New Agent: TicketUpdateGenerationAgent**

```csharp
// Infrastructure/Agents/TicketUpdateGenerationAgent.cs
public class TicketUpdateGenerationAgent : IAgent
{
    private readonly IClaudeService _claudeService;
    private readonly IPromptService _promptService;
    private readonly ITicketUpdateRepository _ticketUpdateRepo;

    public async Task<IAgentMessage> ExecuteAsync(
        IAgentMessage input,
        GraphContext context,
        CancellationToken ct)
    {
        // 1. Get ticket and Q&A history from context
        var ticket = context.GetTicket();
        var answers = context.GetAnswers();
        var analysis = context.GetAnalysis();

        // 2. Get prompt template (tenant-specific or default)
        var promptTemplate = await _promptService.GetTemplateAsync(
            "ticket_update",
            ticket.TenantId,
            ct);

        // 3. Render prompt with context
        var renderedPrompt = await _promptService.RenderPromptAsync(
            promptTemplate,
            new Dictionary<string, object>
            {
                ["ticket"] = ticket,
                ["answers"] = answers,
                ["analysis"] = analysis
            },
            ct);

        // 4. Call Claude to generate ticket update
        var response = await _claudeService.GenerateTicketUpdateAsync(
            renderedPrompt,
            maxTokens: 4000,
            ct);

        // 5. Parse JSON response
        var updateDto = JsonSerializer.Deserialize<TicketUpdateDto>(response);

        // 6. Create TicketUpdate entity
        var ticketUpdate = new TicketUpdate
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UpdatedTitle = updateDto.UpdatedTitle,
            UpdatedDescription = updateDto.UpdatedDescription,
            SuccessCriteria = updateDto.SuccessCriteria.Select(sc =>
                new SuccessCriterion
                {
                    Category = sc.Category,
                    Description = sc.Description,
                    Priority = sc.Priority,
                    IsTestable = sc.IsTestable
                }).ToList(),
            AcceptanceCriteria = updateDto.AcceptanceCriteria,
            Version = 1,
            IsDraft = true,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "Claude"
        };

        // 7. Save to database
        await _ticketUpdateRepo.CreateAsync(ticketUpdate, ct);

        // 8. Return message with ticket update ID
        return new TicketUpdateGeneratedMessage(ticket.Id, ticketUpdate.Id);
    }
}
```

4. **Update RefinementGraph**
   - Add `TicketUpdateGeneration` stage after `AnswerProcessing`
   - Add new suspension point for approval
   - Add retry logic for regeneration

5. **New Agent Messages**

```csharp
// Infrastructure/Agents/Messages/TicketUpdateGeneratedMessage.cs
public record TicketUpdateGeneratedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    DateTime GeneratedAt
) : IAgentMessage;

// Infrastructure/Agents/Messages/TicketUpdateApprovedMessage.cs
public record TicketUpdateApprovedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    DateTime ApprovedAt,
    string? ApproverNotes
) : IAgentMessage;

// Infrastructure/Agents/Messages/TicketUpdateRejectedMessage.cs
public record TicketUpdateRejectedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    string RejectionReason,
    DateTime RejectedAt
) : IAgentMessage;
```

6. **API Endpoints**
   - `GET /api/tickets/{id}/updates` - Get all updates for ticket
   - `GET /api/tickets/{id}/updates/latest` - Get latest draft
   - `PUT /api/tickets/{id}/updates/{updateId}` - Edit draft update
   - `POST /api/tickets/{id}/updates/{updateId}/approve` - Approve update
   - `POST /api/tickets/{id}/updates/{updateId}/reject` - Reject update
   - `POST /api/tickets/{id}/updates/{updateId}/regenerate` - Regenerate

**Files to Create:**
- `/src/PRFactory.Domain/Entities/TicketUpdate.cs`
- `/src/PRFactory.Domain/ValueObjects/SuccessCriterion.cs`
- `/src/PRFactory.Domain/Repositories/ITicketUpdateRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Configurations/TicketUpdateConfiguration.cs`
- `/src/PRFactory.Infrastructure/Persistence/Repositories/TicketUpdateRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Migrations/YYYYMMDD_AddTicketUpdates.cs`
- `/src/PRFactory.Infrastructure/Agents/TicketUpdateGenerationAgent.cs`
- `/src/PRFactory.Infrastructure/Agents/Messages/TicketUpdate*Messages.cs`
- `/src/PRFactory.Api/Controllers/TicketUpdatesController.cs`
- `/src/PRFactory.Api/Models/TicketUpdateDto.cs`

**Files to Modify:**
- `/src/PRFactory.Infrastructure/Agents/Graphs/RefinementGraph.cs` (add new stages)
- `/src/PRFactory.Domain/ValueObjects/WorkflowState.cs` (add new states)

**Estimated Time**: 12-16 hours

---

### Phase 4: Web UI - Preview & Edit Components 🎨

**Priority**: HIGH

**Tasks:**
1. **TicketUpdatePreview Component**

Create a Blazor component to display the generated ticket update with a diff view:

```razor
<!-- Web/Components/Tickets/TicketUpdatePreview.razor -->
@using PRFactory.Web.Models
@inject ITicketService TicketService

<div class="card mb-3">
    <div class="card-header bg-info text-white">
        <h4 class="mb-0">
            <i class="bi bi-file-earmark-text"></i> Refined Ticket (Generated by Claude)
        </h4>
    </div>
    <div class="card-body">
        @if (TicketUpdate == null)
        {
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        }
        else
        {
            <!-- Tabs: Preview vs. Edit vs. Diff -->
            <ul class="nav nav-tabs mb-3" role="tablist">
                <li class="nav-item">
                    <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#preview">
                        Preview
                    </button>
                </li>
                <li class="nav-item">
                    <button class="nav-link" data-bs-toggle="tab" data-bs-target="#edit">
                        Edit
                    </button>
                </li>
                <li class="nav-item">
                    <button class="nav-link" data-bs-toggle="tab" data-bs-target="#diff">
                        <i class="bi bi-arrow-left-right"></i> Compare
                    </button>
                </li>
            </ul>

            <div class="tab-content">
                <!-- Preview Tab -->
                <div class="tab-pane fade show active" id="preview">
                    <h5>@TicketUpdate.UpdatedTitle</h5>
                    <div class="ticket-description">
                        @((MarkupString)Markdig.Markdown.ToHtml(TicketUpdate.UpdatedDescription))
                    </div>

                    <h6 class="mt-4">Success Criteria</h6>
                    @foreach (var criterion in TicketUpdate.SuccessCriteria)
                    {
                        <div class="success-criterion mb-2">
                            <span class="badge bg-@GetCategoryColor(criterion.Category)">
                                @criterion.Category
                            </span>
                            <span class="badge bg-@GetPriorityColor(criterion.Priority)">
                                @GetPriorityLabel(criterion.Priority)
                            </span>
                            <span>@criterion.Description</span>
                            @if (criterion.IsTestable)
                            {
                                <i class="bi bi-check-circle text-success" title="Testable"></i>
                            }
                        </div>
                    }

                    <h6 class="mt-4">Acceptance Criteria</h6>
                    <div class="acceptance-criteria">
                        @((MarkupString)Markdig.Markdown.ToHtml(TicketUpdate.AcceptanceCriteria))
                    </div>
                </div>

                <!-- Edit Tab -->
                <div class="tab-pane fade" id="edit">
                    <TicketUpdateEditor TicketUpdate="@TicketUpdate"
                                      OnSave="HandleSave" />
                </div>

                <!-- Diff Tab -->
                <div class="tab-pane fade" id="diff">
                    <TicketDiffViewer Original="@OriginalTicket"
                                    Updated="@TicketUpdate" />
                </div>
            </div>

            <!-- Action Buttons -->
            <div class="d-flex justify-content-between mt-4">
                <button class="btn btn-outline-danger" @onclick="HandleReject">
                    <i class="bi bi-x-circle"></i> Reject & Regenerate
                </button>
                <button class="btn btn-success" @onclick="HandleApprove">
                    <i class="bi bi-check-circle"></i> Approve & Post to Jira
                </button>
            </div>
        }
    </div>
</div>
```

2. **TicketUpdateEditor Component**

Inline editor with markdown support:

```razor
<!-- Web/Components/Tickets/TicketUpdateEditor.razor -->
<EditForm Model="@editModel" OnValidSubmit="HandleSave">
    <div class="mb-3">
        <label class="form-label">Title</label>
        <InputText class="form-control" @bind-Value="editModel.UpdatedTitle" />
    </div>

    <div class="mb-3">
        <label class="form-label">Description (Markdown)</label>
        <textarea class="form-control" rows="10" @bind="editModel.UpdatedDescription"></textarea>
        <small class="text-muted">Supports Markdown formatting</small>
    </div>

    <div class="mb-3">
        <label class="form-label">Success Criteria</label>
        <SuccessCriteriaEditor Criteria="@editModel.SuccessCriteria" />
    </div>

    <div class="mb-3">
        <label class="form-label">Acceptance Criteria (Markdown)</label>
        <textarea class="form-control" rows="6" @bind="editModel.AcceptanceCriteria"></textarea>
    </div>

    <button type="submit" class="btn btn-primary">
        <i class="bi bi-save"></i> Save Draft
    </button>
</EditForm>
```

3. **TicketDiffViewer Component**

Side-by-side diff view:

```razor
<!-- Web/Components/Tickets/TicketDiffViewer.razor -->
<div class="row">
    <div class="col-md-6">
        <h6>Original Ticket</h6>
        <div class="diff-original">
            <h5>@Original.Title</h5>
            <div>@Original.Description</div>
        </div>
    </div>
    <div class="col-md-6">
        <h6>Updated Ticket</h6>
        <div class="diff-updated">
            <h5>@Updated.UpdatedTitle</h5>
            <div>@Updated.UpdatedDescription</div>
        </div>
    </div>
</div>

<!-- Use a diff library like DiffPlex for inline diff highlighting -->
```

4. **Update Ticket Detail Page**

Modify `/src/PRFactory.Web/Pages/Tickets/Detail.razor` to show ticket update preview:

```razor
@if (ticket.State == WorkflowState.TicketUpdateUnderReview)
{
    <TicketUpdatePreview TicketId="@ticket.Id"
                        TicketUpdate="@latestUpdate"
                        OriginalTicket="@ticket"
                        OnApproved="HandleUpdateApproved"
                        OnRejected="HandleUpdateRejected" />
}
```

5. **Add NuGet Packages**
   - `Markdig` - for Markdown rendering
   - `DiffPlex` - for diff visualization

**Files to Create:**
- `/src/PRFactory.Web/Components/Tickets/TicketUpdatePreview.razor`
- `/src/PRFactory.Web/Components/Tickets/TicketUpdateEditor.razor`
- `/src/PRFactory.Web/Components/Tickets/TicketDiffViewer.razor`
- `/src/PRFactory.Web/Components/Tickets/SuccessCriteriaEditor.razor`
- `/src/PRFactory.Web/Models/TicketUpdateDto.cs`

**Files to Modify:**
- `/src/PRFactory.Web/Pages/Tickets/Detail.razor`
- `/src/PRFactory.Web/Services/ITicketService.cs` (add update methods)
- `/src/PRFactory.Web/Services/TicketService.cs` (implement methods)
- `/src/PRFactory.Web/PRFactory.Web.csproj` (add NuGet packages)

**Estimated Time**: 14-18 hours

---

### Phase 5: Integration & Workflow Enhancement 🔄

**Priority**: MEDIUM

**Tasks:**
1. **Update RefinementGraph.cs**

Complete integration of new stages:

```csharp
protected override async Task<GraphExecutionResult> ResumeCoreAsync(
    IAgentMessage resumeMessage,
    GraphContext context,
    CancellationToken cancellationToken)
{
    var currentState = GetCurrentState(context);

    if (currentState == "awaiting_answers" && resumeMessage is AnswersReceivedMessage)
    {
        // Stage 7: Answer Processing
        var processedMessage = await ExecuteAgentAsync<AnswerProcessingAgent>(
            resumeMessage, context, "answer_processing", cancellationToken);
        await SaveCheckpointAsync(context, "answers_processed", "AnswerProcessingAgent");

        // Stage 8: Ticket Update Generation (NEW)
        Logger.LogInformation("Stage 8: Generating ticket update for {TicketId}", context.TicketId);
        var updateMessage = await ExecuteAgentAsync<TicketUpdateGenerationAgent>(
            processedMessage, context, "ticket_update_generation", cancellationToken);
        await SaveCheckpointAsync(context, "ticket_update_generated", "TicketUpdateGenerationAgent");

        // Stage 9: Post to Web UI and save draft (parallel)
        Logger.LogInformation("Stage 9: Posting update preview to web UI for {TicketId}", context.TicketId);
        context.State["is_suspended"] = true;
        context.State["waiting_for"] = "ticket_update_approval";
        await SaveCheckpointAsync(context, "ticket_update_pending_approval", "TicketUpdateGenerationAgent");

        // Emit event to notify web UI via SignalR
        await _eventBroadcaster.BroadcastTicketUpdateReadyAsync(context.TicketId);

        return GraphExecutionResult.Suspended("ticket_update_pending_approval", updateMessage);
    }
    else if (currentState == "ticket_update_pending_approval" &&
             resumeMessage is TicketUpdateApprovedMessage approvedMsg)
    {
        // Stage 10: Post approved update to Jira
        Logger.LogInformation("Stage 10: Posting approved update to Jira for {TicketId}", context.TicketId);
        await ExecuteAgentAsync<JiraUpdatePostAgent>(
            approvedMsg, context, "jira_update_post", cancellationToken);
        await SaveCheckpointAsync(context, "ticket_update_posted", "JiraUpdatePostAgent");

        // Mark refinement as complete
        context.State["is_completed"] = true;
        context.State["is_suspended"] = false;
        await SaveCheckpointAsync(context, "refinement_complete", "CompletedAgent");

        return GraphExecutionResult.Success("refinement_complete",
            new RefinementCompleteEvent(context.TicketId, DateTime.UtcNow));
    }
    else if (currentState == "ticket_update_pending_approval" &&
             resumeMessage is TicketUpdateRejectedMessage rejectedMsg)
    {
        // Retry loop: Regenerate ticket update
        var retryCount = context.State.TryGetValue("ticket_update_retry_count", out var count)
            ? Convert.ToInt32(count)
            : 0;

        if (retryCount >= 3)
        {
            throw new InvalidOperationException("Ticket update rejected 3 times. Manual intervention required.");
        }

        context.State["ticket_update_retry_count"] = retryCount + 1;
        context.State["last_rejection_reason"] = rejectedMsg.RejectionReason;

        // Go back to ticket update generation with rejection context
        var regenerateMessage = await ExecuteAgentAsync<TicketUpdateGenerationAgent>(
            rejectedMsg, context, "ticket_update_regeneration", cancellationToken);

        return GraphExecutionResult.Suspended("ticket_update_pending_approval", regenerateMessage);
    }

    throw new InvalidOperationException($"Cannot resume from state {currentState}");
}
```

2. **New Agent: JiraUpdatePostAgent**

```csharp
// Infrastructure/Agents/JiraUpdatePostAgent.cs
public class JiraUpdatePostAgent : IAgent
{
    private readonly IJiraService _jiraService;
    private readonly ITicketUpdateRepository _ticketUpdateRepo;

    public async Task<IAgentMessage> ExecuteAsync(
        IAgentMessage input,
        GraphContext context,
        CancellationToken ct)
    {
        var message = (TicketUpdateApprovedMessage)input;
        var ticketUpdate = await _ticketUpdateRepo.GetByIdAsync(message.TicketUpdateId, ct);

        // Build Jira update payload
        var jiraUpdate = new
        {
            fields = new
            {
                summary = ticketUpdate.UpdatedTitle,
                description = FormatForJira(ticketUpdate),
                // Add custom fields for success criteria if configured
            }
        };

        // Post to Jira
        await _jiraService.UpdateIssueAsync(ticket.TicketKey, jiraUpdate, ct);

        // Mark update as posted
        ticketUpdate.IsPosted = true;
        ticketUpdate.PostedAt = DateTime.UtcNow;
        await _ticketUpdateRepo.UpdateAsync(ticketUpdate, ct);

        return new TicketUpdatePostedMessage(ticket.Id, ticketUpdate.Id, DateTime.UtcNow);
    }

    private string FormatForJira(TicketUpdate update)
    {
        var sb = new StringBuilder();
        sb.AppendLine(update.UpdatedDescription);
        sb.AppendLine();
        sb.AppendLine("h2. Success Criteria");

        foreach (var criterion in update.SuccessCriteria)
        {
            sb.AppendLine($"* [{criterion.Category}] {criterion.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("h2. Acceptance Criteria");
        sb.AppendLine(update.AcceptanceCriteria);

        return sb.ToString();
    }
}
```

3. **SignalR Real-Time Updates**

Update web UI in real-time when ticket update is ready:

```csharp
// Web/Hubs/TicketHub.cs
public async Task NotifyTicketUpdateReady(Guid ticketId, Guid ticketUpdateId)
{
    await Clients.Group($"ticket:{ticketId}").SendAsync(
        "TicketUpdateReady",
        new { TicketId = ticketId, UpdateId = ticketUpdateId });
}
```

**Files to Create:**
- `/src/PRFactory.Infrastructure/Agents/JiraUpdatePostAgent.cs`
- `/src/PRFactory.Infrastructure/Services/EventBroadcaster.cs`

**Files to Modify:**
- `/src/PRFactory.Infrastructure/Agents/Graphs/RefinementGraph.cs`
- `/src/PRFactory.Web/Hubs/TicketHub.cs`

**Estimated Time**: 8-10 hours

---

### Phase 6: Configuration & Admin UI 🛠️

**Priority**: LOW (Nice to have)

**Tasks:**
1. **Prompt Template Admin Page**

Create admin UI to manage prompt templates:

```razor
<!-- Web/Pages/Admin/Prompts/Index.razor -->
@page "/admin/prompts"

<h2>Prompt Template Management</h2>

<table class="table">
    <thead>
        <tr>
            <th>Name</th>
            <th>Category</th>
            <th>Version</th>
            <th>Tenant</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var prompt in prompts)
        {
            <tr>
                <td>@prompt.DisplayName</td>
                <td>@prompt.Category</td>
                <td>v@prompt.Version</td>
                <td>@(prompt.TenantId.HasValue ? "Tenant-specific" : "Global")</td>
                <td>
                    <button @onclick="() => EditPrompt(prompt.Id)">Edit</button>
                    <button @onclick="() => TestPrompt(prompt.Id)">Test</button>
                </td>
            </tr>
        }
    </tbody>
</table>
```

2. **Prompt Template Editor**

Monaco Editor integration for editing prompts:

```razor
<!-- Web/Pages/Admin/Prompts/Edit.razor -->
<MonacoEditor @bind-Value="promptTemplate.SystemPrompt" Language="plaintext" />
```

**Files to Create:**
- `/src/PRFactory.Web/Pages/Admin/Prompts/Index.razor`
- `/src/PRFactory.Web/Pages/Admin/Prompts/Edit.razor`
- `/src/PRFactory.Web/Pages/Admin/Prompts/Test.razor`

**Estimated Time**: 6-8 hours

---

## Integration Points

### 1. Claude Code SDK Integration

**Research Required:** Determine best approach for Claude Code integration:

#### Option A: Anthropic SDK with Extended Context
```csharp
// Use official Anthropic SDK with MCP (Model Context Protocol)
var client = new AnthropicClient(apiKey);
var response = await client.Messages.CreateAsync(new MessageRequest
{
    Model = "claude-sonnet-4-5-20250929",
    MaxTokens = 8000,
    System = systemPrompt,
    Messages = conversationHistory,
    Tools = new[]
    {
        new Tool
        {
            Name = "read_file",
            Description = "Read a file from the repository",
            InputSchema = new { ... }
        }
    }
});
```

**Pros:**
- Official SDK
- Full API support
- Tool use capabilities (MCP)

**Cons:**
- Requires implementing tool handlers
- More complex integration

#### Option B: Claude Desktop API Integration
```csharp
// If Claude Desktop provides an API for CLI integration
var claudeDesktop = new ClaudeDesktopClient();
await claudeDesktop.SendProjectContextAsync(repositoryPath);
var response = await claudeDesktop.GenerateWithProjectContextAsync(prompt);
```

**Pros:**
- Automatic project context
- Optimized for code generation

**Cons:**
- May require Claude Desktop to be running
- Unclear if API exists for programmatic access

#### Recommended Approach: **Option A**

Use Anthropic SDK with extended prompting and MCP tools. This provides:
- Full control over context
- Tool use for repository exploration
- Production-ready integration

### 2. Prompt Rendering Pipeline

```
┌─────────────────────────────────────────────────────────┐
│ PromptTemplate (Database)                               │
│  - SystemPrompt: "You are an expert..."                │
│  - UserPromptTemplate: "Ticket: {{ticket.title}}..."   │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│ PromptService.RenderPromptAsync()                       │
│  1. Load template from DB                               │
│  2. Replace placeholders ({{ticket.title}}, etc.)       │
│  3. Inject conversation history                         │
│  4. Build context with codebase files                   │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│ ClaudeClient.SendMessageAsync()                         │
│  - Send system prompt + rendered user prompt            │
│  - Get structured response (JSON)                       │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│ Agent (TicketUpdateGenerationAgent)                     │
│  - Parse response                                       │
│  - Create TicketUpdate entity                           │
│  - Save to database                                     │
└─────────────────────────────────────────────────────────┘
```

### 3. Web UI Flow

```
User answers questions
     ↓
Submit to API
     ↓
Workflow resumed → TicketUpdateGenerationAgent runs
     ↓
SignalR notification sent to Web UI
     ↓
Web UI shows notification: "Ticket update ready for review"
     ↓
User navigates to ticket detail page
     ↓
TicketUpdatePreview component loads latest draft
     ↓
User can:
  - Preview (rendered markdown)
  - Edit (inline editor with live preview)
  - Compare (diff view: original vs. updated)
  - Approve → Posts to Jira, workflow continues
  - Reject → Regenerates with feedback
```

---

## Testing Strategy

### Unit Tests

1. **PromptService Tests**
   - Template rendering with placeholders
   - Tenant-specific template selection
   - Fallback to default templates

2. **TicketUpdateGenerationAgent Tests**
   - Mock Claude responses
   - JSON parsing edge cases
   - Error handling

3. **Repository Tests**
   - CRUD operations for TicketUpdate
   - Version management
   - Draft vs. approved states

### Integration Tests

1. **End-to-End Workflow Tests**
   - Trigger refinement
   - Answer questions
   - Generate ticket update
   - Approve update
   - Verify Jira post

2. **API Tests**
   - All CRUD endpoints for ticket updates
   - All CRUD endpoints for prompt templates
   - Approval/rejection workflows

### Manual Testing Checklist

- [ ] Can trigger refinement workflow
- [ ] Questions are generated and displayed
- [ ] Answers can be submitted
- [ ] Ticket update is generated automatically
- [ ] Preview displays correctly in web UI
- [ ] Edit mode allows modifications
- [ ] Diff view shows changes accurately
- [ ] Approve button posts to Jira
- [ ] Reject button regenerates with feedback
- [ ] SignalR updates UI in real-time
- [ ] Prompt templates can be managed via admin UI
- [ ] Tenant-specific prompts work correctly

---

## Timeline Estimates

| Phase | Description | Estimated Hours | Priority |
|-------|-------------|-----------------|----------|
| **Phase 1** | Claude Code SDK Integration | 8-12 hours | CRITICAL |
| **Phase 2** | Prompt Management System | 10-14 hours | HIGH |
| **Phase 3** | Ticket Update Generation (Backend) | 12-16 hours | HIGH |
| **Phase 4** | Web UI - Preview & Edit Components | 14-18 hours | HIGH |
| **Phase 5** | Integration & Workflow Enhancement | 8-10 hours | MEDIUM |
| **Phase 6** | Configuration & Admin UI | 6-8 hours | LOW |
| **Testing** | Unit, integration, and manual testing | 8-12 hours | HIGH |
| **Documentation** | Update docs, API docs, user guides | 4-6 hours | MEDIUM |

**Total Estimated Time**: 70-96 hours (approximately 2-3 weeks for one developer)

### Recommended Implementation Order

**Week 1:**
- Day 1-2: Phase 1 (Claude SDK integration) - **BLOCKING**
- Day 3-4: Phase 2 (Prompt system) - **BLOCKING**
- Day 5: Start Phase 3 (Backend)

**Week 2:**
- Day 1-2: Complete Phase 3 (Backend)
- Day 3-5: Phase 4 (Web UI)

**Week 3:**
- Day 1-2: Phase 5 (Integration)
- Day 3-4: Testing
- Day 5: Phase 6 (Admin UI) + Documentation

---

## Success Criteria

### Functional Requirements

✅ **MUST HAVE:**
1. After answering questions, Claude automatically generates ticket update
2. Ticket update includes:
   - Updated title
   - Comprehensive description
   - SMART success criteria
   - Acceptance criteria
3. Web UI displays preview of ticket update
4. User can edit ticket update inline
5. User can approve → posts to Jira
6. User can reject → regenerates with feedback
7. Prompts are stored in database (not hardcoded)

✅ **SHOULD HAVE:**
8. Diff view showing original vs. updated ticket
9. Tenant-specific prompt customization
10. Success criteria categorization (Functional/Technical/Testing)
11. Real-time UI updates via SignalR

✅ **NICE TO HAVE:**
12. Admin UI for managing prompts
13. Prompt versioning
14. A/B testing different prompts
15. Analytics on acceptance rates

### Non-Functional Requirements

- ⚡ **Performance**: Ticket update generation < 30 seconds
- 🔒 **Security**: Prompts can be tenant-specific (multi-tenant isolation)
- 🎨 **UX**: Preview loads instantly, editing is responsive
- 📊 **Observability**: All steps logged with correlation IDs
- ♿ **Accessibility**: Web UI is keyboard-navigable and screen-reader friendly

---

## Risk Mitigation

### Risk 1: Claude Code SDK Unavailable

**Mitigation:**
- Use official Anthropic SDK (reliable, well-documented)
- Implement rich context building manually
- Use MCP for tool use capabilities

### Risk 2: Generated Content Quality

**Mitigation:**
- Invest time in prompt engineering
- Allow manual editing before approval
- Track rejection reasons to improve prompts
- Implement A/B testing for prompt variations

### Risk 3: Jira Formatting Issues

**Mitigation:**
- Test Jira markdown/wiki formatting thoroughly
- Provide preview of how it will look in Jira
- Allow custom formatting templates per tenant

### Risk 4: User Adoption

**Mitigation:**
- Make preview UX excellent (fast, intuitive)
- Show diff clearly so users trust Claude's changes
- Allow easy editing (don't force full rewrites)
- Provide examples of good ticket updates

---

## Appendices

### A. Example Ticket Update Flow

**Original Ticket:**
```
Title: Add user login
Description: We need users to be able to log in
```

**Questions Generated:**
1. Should we use OAuth or username/password?
2. Do we need "remember me" functionality?
3. What should happen on failed login attempts?

**Answers Received:**
1. Username/password with email verification
2. Yes, remember me for 30 days
3. Lock account after 5 failed attempts, send email

**Generated Ticket Update:**
```json
{
  "updatedTitle": "Implement username/password authentication with email verification",
  "updatedDescription": "Implement a secure user authentication system that allows users to log in using their email and password. The system must verify email addresses before allowing login and include account security features.

## Context
Users currently cannot access protected features. This authentication system will serve as the foundation for all user-specific functionality.

## Technical Approach
- Use ASP.NET Core Identity for user management
- Integrate email verification via SendGrid
- Implement secure password hashing (bcrypt)
- Store sessions with 30-day expiration for 'remember me'

## Security Requirements
- Passwords must be hashed and salted
- HTTPS required for all auth endpoints
- Account lockout after 5 failed attempts
- Email notification on suspicious activity",

  "successCriteria": [
    {
      "category": "Functional",
      "description": "User can register with email and password",
      "priority": 0,
      "isTestable": true
    },
    {
      "category": "Functional",
      "description": "User receives email verification link after registration",
      "priority": 0,
      "isTestable": true
    },
    {
      "category": "Functional",
      "description": "User can log in after verifying email",
      "priority": 0,
      "isTestable": true
    },
    {
      "category": "Functional",
      "description": "'Remember me' checkbox keeps user logged in for 30 days",
      "priority": 0,
      "isTestable": true
    },
    {
      "category": "Security",
      "description": "Account is locked for 15 minutes after 5 failed login attempts",
      "priority": 0,
      "isTestable": true
    },
    {
      "category": "Security",
      "description": "User receives email notification when account is locked",
      "priority": 1,
      "isTestable": true
    },
    {
      "category": "Technical",
      "description": "Passwords are hashed using bcrypt with salt",
      "priority": 0,
      "isTestable": true
    },
    {
      "category": "Testing",
      "description": "Unit tests cover all authentication scenarios",
      "priority": 1,
      "isTestable": true
    }
  ],

  "acceptanceCriteria": "- [ ] User can register with valid email and strong password
- [ ] Email verification link is sent within 1 minute
- [ ] User cannot log in before email verification
- [ ] Valid login redirects to dashboard
- [ ] Invalid login shows error message
- [ ] 5 failed logins lock account for 15 minutes
- [ ] Locked account notification email is sent
- [ ] 'Remember me' maintains session for 30 days
- [ ] Session expires after 30 days if 'remember me' checked
- [ ] All passwords are stored hashed (never plaintext)
- [ ] Password reset flow works via email
- [ ] All endpoints use HTTPS in production"
}
```

### B. Prompt Template Variables

Available variables for prompt templates:

| Variable | Type | Description | Example |
|----------|------|-------------|---------|
| `{{ticket.id}}` | Guid | Ticket ID | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| `{{ticket.key}}` | string | Ticket key | `PROJ-123` |
| `{{ticket.title}}` | string | Original title | `Add user login` |
| `{{ticket.description}}` | string | Original description | `We need users to...` |
| `{{ticket.createdAt}}` | DateTime | Created timestamp | `2025-11-08T10:30:00Z` |
| `{{questions}}` | List | All questions asked | `[{text: "..."}]` |
| `{{answers}}` | Dictionary | Question-answer pairs | `{"q1": "OAuth"}` |
| `{{analysis.architecture}}` | string | Detected architecture | `Clean Architecture` |
| `{{analysis.patterns}}` | List | Code patterns found | `["Repository", "DI"]` |
| `{{analysis.files}}` | List | Relevant files | `["Auth.cs"]` |
| `{{repository.name}}` | string | Repo name | `PRFactory` |
| `{{repository.language}}` | string | Primary language | `C#` |
| `{{tenant.name}}` | string | Tenant name | `Acme Corp` |

### C. API Endpoints Summary

**Ticket Updates:**
```
GET    /api/tickets/{id}/updates
GET    /api/tickets/{id}/updates/latest
GET    /api/tickets/{id}/updates/{updateId}
POST   /api/tickets/{id}/updates
PUT    /api/tickets/{id}/updates/{updateId}
DELETE /api/tickets/{id}/updates/{updateId}
POST   /api/tickets/{id}/updates/{updateId}/approve
POST   /api/tickets/{id}/updates/{updateId}/reject
POST   /api/tickets/{id}/updates/{updateId}/regenerate
```

**Prompt Templates:**
```
GET    /api/prompts
GET    /api/prompts/{id}
GET    /api/prompts/category/{category}
POST   /api/prompts
PUT    /api/prompts/{id}
DELETE /api/prompts/{id}
POST   /api/prompts/{id}/test
```

---

## Conclusion

This plan provides a comprehensive roadmap to enhance the refinement workflow with:

1. ✅ **Automated ticket update generation** using Claude Code
2. ✅ **Preview & editing UI** for human review
3. ✅ **Configurable prompts** stored in database
4. ✅ **95% automation** with 5% human refinement

The architecture respects the existing multi-graph design from `CLAUDE.md` and extends it with new agents and workflow states.

**Next Steps:**
1. Review and approve this plan
2. Begin Phase 1 (Claude SDK integration)
3. Implement phases sequentially
4. Test thoroughly before production

**Questions?**
- Clarify Claude Code SDK preference (Option A vs. B)
- Confirm Jira formatting requirements
- Discuss prompt template seeding strategy
- Review UI mockups for ticket update preview
