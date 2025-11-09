# Team Review Design

**Epic**: Team Review (Prio 1)
**Goal**: Enable team-based review and approval of AI-generated implementation plans (Phase 2)
**Date**: 2025-11-09

## Problem Statement

Currently, PRFactory's plan approval process (Phase 2) is a single-user workflow:
- Only one person can approve/reject a plan
- No collaboration or discussion during review
- No way to require multiple approvals
- No comment threads for feedback

This limits PRFactory's usefulness in team environments where:
- Multiple stakeholders need to review plans (tech lead, architect, product owner)
- Teams want to discuss implementation approaches before approving
- Organizations require multiple sign-offs for code changes

## Goals

1. **Multi-Reviewer Assignment**: Assign specific team members to review a plan
2. **Collaborative Discussion**: Enable comment threads with @mentions during plan review
3. **Multi-Approval Logic**: Require X out of Y reviewers to approve before proceeding
4. **Audit Trail**: Track who approved, when, and with what comments
5. **Backward Compatibility**: Existing single-user workflow still works (default to require 1 approval)

## Non-Goals (Future Enhancements)

- Real-time collaboration (Figma-style cursors, live updates)
- Advanced notification system (email, Slack integration)
- Role-based reviewer assignment (auto-assign based on code ownership)
- Review delegation/re-assignment

## Design

### 1. Domain Model (New Entities)

#### User Entity
```csharp
// src/PRFactory.Domain/Entities/User.cs
namespace PRFactory.Domain.Entities;

public class User : EntityBase
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }

    // Authentication integration (future)
    public string? ExternalAuthId { get; private set; } // e.g., Auth0 user ID

    public DateTime CreatedAt { get; private set; }
    public DateTime? LastSeenAt { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; } = null!;
    public ICollection<PlanReview> PlanReviews { get; private set; } = new List<PlanReview>();
    public ICollection<ReviewComment> Comments { get; private set; } = new List<ReviewComment>();

    private User() { } // EF Core

    public User(Guid tenantId, string email, string displayName)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
    }
}
```

#### PlanReview Entity
```csharp
// src/PRFactory.Domain/Entities/PlanReview.cs
namespace PRFactory.Domain.Entities;

public class PlanReview : EntityBase
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid ReviewerId { get; private set; }

    public ReviewStatus Status { get; private set; } // Pending, Approved, RejectedForRefinement, RejectedForRegeneration
    public bool IsRequired { get; private set; } // Required vs optional reviewer

    public DateTime AssignedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public string? Decision { get; private set; } // Brief approval/rejection reason

    // Navigation
    public Ticket Ticket { get; private set; } = null!;
    public User Reviewer { get; private set; } = null!;

    private PlanReview() { } // EF Core

    public PlanReview(Guid ticketId, Guid reviewerId, bool isRequired)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        ReviewerId = reviewerId;
        IsRequired = isRequired;
        Status = ReviewStatus.Pending;
        AssignedAt = DateTime.UtcNow;
    }

    public void Approve(string? decision = null)
    {
        if (Status != ReviewStatus.Pending)
            throw new InvalidOperationException("Can only approve a pending review");

        Status = ReviewStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        Decision = decision;
    }

    public void Reject(string reason, bool regenerateCompletely)
    {
        if (Status != ReviewStatus.Pending)
            throw new InvalidOperationException("Can only reject a pending review");

        Status = regenerateCompletely
            ? ReviewStatus.RejectedForRegeneration
            : ReviewStatus.RejectedForRefinement;
        ReviewedAt = DateTime.UtcNow;
        Decision = reason;
    }

    public void ResetForNewPlan()
    {
        Status = ReviewStatus.Pending;
        ReviewedAt = null;
        Decision = null;
    }
}

public enum ReviewStatus
{
    Pending,
    Approved,
    RejectedForRefinement,
    RejectedForRegeneration
}
```

#### ReviewComment Entity
```csharp
// src/PRFactory.Domain/Entities/ReviewComment.cs
namespace PRFactory.Domain.Entities;

public class ReviewComment : EntityBase
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }

    public string Content { get; private set; }
    public List<Guid> MentionedUserIds { get; private set; } = new();

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Ticket Ticket { get; private set; } = null!;
    public User Author { get; private set; } = null!;

    private ReviewComment() { } // EF Core

    public ReviewComment(Guid ticketId, Guid authorId, string content, List<Guid>? mentionedUserIds = null)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorId = authorId;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        MentionedUserIds = mentionedUserIds ?? new List<Guid>();
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string content, List<Guid>? mentionedUserIds = null)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        MentionedUserIds = mentionedUserIds ?? new List<Guid>();
        UpdatedAt = DateTime.UtcNow;
    }
}
```

#### Ticket Entity Updates
```csharp
// src/PRFactory.Domain/Entities/Ticket.cs
// ADD these properties to existing Ticket entity:

public class Ticket : EntityBase
{
    // ... existing properties ...

    // Team Review additions:
    public int RequiredApprovalCount { get; private set; } = 1; // Default: 1 approval required

    // Navigation
    public ICollection<PlanReview> PlanReviews { get; private set; } = new List<PlanReview>();
    public ICollection<ReviewComment> ReviewComments { get; private set; } = new List<ReviewComment>();

    // ... existing methods ...

    // NEW: Assign reviewers to plan
    public void AssignReviewers(List<Guid> requiredReviewerIds, List<Guid>? optionalReviewerIds = null)
    {
        if (State != WorkflowState.PlanPosted && State != WorkflowState.PlanUnderReview)
            throw new InvalidOperationException("Can only assign reviewers when plan is ready for review");

        // Clear existing reviews
        PlanReviews.Clear();

        // Add required reviewers
        foreach (var reviewerId in requiredReviewerIds)
        {
            PlanReviews.Add(new PlanReview(Id, reviewerId, isRequired: true));
        }

        // Add optional reviewers
        if (optionalReviewerIds != null)
        {
            foreach (var reviewerId in optionalReviewerIds)
            {
                PlanReviews.Add(new PlanReview(Id, reviewerId, isRequired: false));
            }
        }

        RequiredApprovalCount = requiredReviewerIds.Count;
        TransitionTo(WorkflowState.PlanUnderReview);
    }

    // NEW: Check if plan has sufficient approvals
    public bool HasSufficientApprovals()
    {
        var requiredReviews = PlanReviews.Where(r => r.IsRequired).ToList();
        var approvedCount = requiredReviews.Count(r => r.Status == ReviewStatus.Approved);

        return approvedCount >= RequiredApprovalCount;
    }

    // NEW: Check if plan has been rejected by any reviewer
    public bool HasRejections()
    {
        return PlanReviews.Any(r =>
            r.Status == ReviewStatus.RejectedForRefinement ||
            r.Status == ReviewStatus.RejectedForRegeneration);
    }

    // NEW: Get rejection details
    public (string Reason, bool RegenerateCompletely)? GetRejectionDetails()
    {
        var rejection = PlanReviews.FirstOrDefault(r =>
            r.Status == ReviewStatus.RejectedForRefinement ||
            r.Status == ReviewStatus.RejectedForRegeneration);

        if (rejection == null)
            return null;

        return (rejection.Decision ?? "No reason provided",
                rejection.Status == ReviewStatus.RejectedForRegeneration);
    }

    // UPDATE: Override ApprovePlan to check multi-reviewer logic
    public override void ApprovePlan()
    {
        if (!HasSufficientApprovals())
            throw new InvalidOperationException(
                $"Insufficient approvals. Required: {RequiredApprovalCount}, " +
                $"Received: {PlanReviews.Count(r => r.IsRequired && r.Status == ReviewStatus.Approved)}");

        // Call base implementation
        base.ApprovePlan();
    }

    // NEW: Reset reviews when plan is regenerated
    public void ResetReviewsForNewPlan()
    {
        foreach (var review in PlanReviews)
        {
            review.ResetForNewPlan();
        }
    }
}
```

### 2. Database Schema

#### Users Table
```sql
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    TenantId UUID NOT NULL REFERENCES Tenants(Id),
    Email VARCHAR(255) NOT NULL,
    DisplayName VARCHAR(255) NOT NULL,
    AvatarUrl VARCHAR(500),
    ExternalAuthId VARCHAR(255),
    CreatedAt TIMESTAMP NOT NULL,
    LastSeenAt TIMESTAMP,
    UNIQUE(TenantId, Email)
);

CREATE INDEX IX_Users_TenantId ON Users(TenantId);
CREATE INDEX IX_Users_Email ON Users(Email);
```

#### PlanReviews Table
```sql
CREATE TABLE PlanReviews (
    Id UUID PRIMARY KEY,
    TicketId UUID NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    ReviewerId UUID NOT NULL REFERENCES Users(Id),
    Status INT NOT NULL, -- 0=Pending, 1=Approved, 2=RejectedForRefinement, 3=RejectedForRegeneration
    IsRequired BOOLEAN NOT NULL,
    AssignedAt TIMESTAMP NOT NULL,
    ReviewedAt TIMESTAMP,
    Decision TEXT,
    UNIQUE(TicketId, ReviewerId)
);

CREATE INDEX IX_PlanReviews_TicketId ON PlanReviews(TicketId);
CREATE INDEX IX_PlanReviews_ReviewerId ON PlanReviews(ReviewerId);
CREATE INDEX IX_PlanReviews_Status ON PlanReviews(Status);
```

#### ReviewComments Table
```sql
CREATE TABLE ReviewComments (
    Id UUID PRIMARY KEY,
    TicketId UUID NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    AuthorId UUID NOT NULL REFERENCES Users(Id),
    Content TEXT NOT NULL,
    MentionedUserIds JSONB, -- Array of user IDs
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);

CREATE INDEX IX_ReviewComments_TicketId ON ReviewComments(TicketId);
CREATE INDEX IX_ReviewComments_AuthorId ON ReviewComments(AuthorId);
CREATE INDEX IX_ReviewComments_CreatedAt ON ReviewComments(CreatedAt DESC);
```

#### Tickets Table Updates
```sql
ALTER TABLE Tickets
ADD COLUMN RequiredApprovalCount INT NOT NULL DEFAULT 1;
```

### 3. Application Service Updates

#### New: IUserService
```csharp
// src/PRFactory.Core/Application/Services/IUserService.cs
namespace PRFactory.Core.Application.Services;

public interface IUserService
{
    Task<User> CreateUserAsync(Guid tenantId, string email, string displayName, CancellationToken ct = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<User?> GetUserByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<List<User>> GetUsersByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<User>> SearchUsersAsync(Guid tenantId, string searchTerm, CancellationToken ct = default);
}
```

#### New: IPlanReviewService
```csharp
// src/PRFactory.Core/Application/Services/IPlanReviewService.cs
namespace PRFactory.Core.Application.Services;

public interface IPlanReviewService
{
    // Reviewer assignment
    Task AssignReviewersAsync(
        Guid ticketId,
        List<Guid> requiredReviewerIds,
        List<Guid>? optionalReviewerIds = null,
        CancellationToken ct = default);

    // Individual reviewer actions
    Task SubmitApprovalAsync(
        Guid ticketId,
        Guid reviewerId,
        string? decision = null,
        CancellationToken ct = default);

    Task SubmitRejectionAsync(
        Guid ticketId,
        Guid reviewerId,
        string reason,
        bool regenerateCompletely,
        CancellationToken ct = default);

    // Comment management
    Task<ReviewComment> AddCommentAsync(
        Guid ticketId,
        Guid authorId,
        string content,
        List<Guid>? mentionedUserIds = null,
        CancellationToken ct = default);

    Task<ReviewComment> UpdateCommentAsync(
        Guid commentId,
        string content,
        List<Guid>? mentionedUserIds = null,
        CancellationToken ct = default);

    Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default);

    // Query
    Task<List<PlanReview>> GetReviewsForTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task<List<ReviewComment>> GetCommentsForTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task<PlanReviewSummary> GetReviewSummaryAsync(Guid ticketId, CancellationToken ct = default);
}

public record PlanReviewSummary(
    int RequiredApprovalCount,
    int ReceivedApprovalCount,
    int PendingReviewCount,
    bool CanProceed,
    List<ReviewerSummary> Reviewers
);

public record ReviewerSummary(
    Guid ReviewerId,
    string ReviewerName,
    ReviewStatus Status,
    bool IsRequired,
    DateTime? ReviewedAt
);
```

#### Updated: TicketApplicationService
```csharp
// src/PRFactory.Infrastructure/Application/TicketApplicationService.cs
// ADD these methods:

public class TicketApplicationService : ITicketApplicationService
{
    private readonly IPlanReviewService _planReviewService;
    private readonly IWorkflowOrchestrator _orchestrator;

    // ... existing code ...

    /// <summary>
    /// Check if ticket has sufficient approvals and trigger workflow continuation if so.
    /// Called after each individual approval is submitted.
    /// </summary>
    public async Task CheckAndProcessApprovals(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId, ct);
        if (ticket == null)
            throw new NotFoundException($"Ticket {ticketId} not found");

        // Check for rejections first
        if (ticket.HasRejections())
        {
            var (reason, regenerate) = ticket.GetRejectionDetails()!.Value;
            ticket.TransitionTo(WorkflowState.PlanRejected);
            await _ticketRepo.UpdateAsync(ticket, ct);

            // Resume workflow with rejection
            var rejectionMsg = new PlanRejectedMessage(
                ticketId,
                reason,
                regenerate ? null : reason, // refinement instructions if not regenerating
                regenerate);
            await _orchestrator.ResumeAsync(ticketId, rejectionMsg, ct);
            return;
        }

        // Check if sufficient approvals
        if (ticket.HasSufficientApprovals())
        {
            ticket.ApprovePlan(); // Transitions to PlanApproved
            await _ticketRepo.UpdateAsync(ticket, ct);

            // Resume workflow with approval
            var approvalMsg = new PlanApprovedMessage(
                ticketId,
                DateTime.UtcNow,
                "Team"); // TODO: Get actual approver names
            await _orchestrator.ResumeAsync(ticketId, approvalMsg, ct);
        }
        // else: Still waiting for more approvals, do nothing
    }
}
```

### 4. UI Components

#### New: ReviewerAssignment.razor
```razor
<!-- src/PRFactory.Web/Components/PlanReview/ReviewerAssignment.razor -->
@inject IUserService UserService
@inject IPlanReviewService PlanReviewService

<Card Title="Assign Reviewers" Icon="people">
    <FormField Label="Search Team Members">
        <InputText @bind-Value="searchTerm"
                   @oninput="OnSearchChanged"
                   class="form-control"
                   placeholder="Search by name or email..." />
    </FormField>

    @if (searchResults.Any())
    {
        <div class="list-group mb-3">
            @foreach (var user in searchResults)
            {
                <button type="button"
                        class="list-group-item list-group-item-action d-flex justify-content-between align-items-center"
                        @onclick="() => AddReviewer(user, isRequired: true)">
                    <div>
                        <strong>@user.DisplayName</strong>
                        <br />
                        <small class="text-muted">@user.Email</small>
                    </div>
                    <i class="bi bi-plus-circle text-primary"></i>
                </button>
            }
        </div>
    }

    <h6>Required Reviewers (@requiredReviewers.Count)</h6>
    <div class="mb-3">
        @foreach (var reviewer in requiredReviewers)
        {
            <span class="badge bg-primary me-2 mb-2">
                @reviewer.DisplayName
                <button type="button" class="btn-close btn-close-white ms-2"
                        @onclick="() => RemoveReviewer(reviewer)"></button>
            </span>
        }
    </div>

    <h6>Optional Reviewers (@optionalReviewers.Count)</h6>
    <div class="mb-3">
        @foreach (var reviewer in optionalReviewers)
        {
            <span class="badge bg-secondary me-2 mb-2">
                @reviewer.DisplayName
                <button type="button" class="btn-close btn-close-white ms-2"
                        @onclick="() => RemoveReviewer(reviewer)"></button>
            </span>
        }
    </div>

    <LoadingButton OnClick="AssignReviewers"
                   IsLoading="@isAssigning"
                   Icon="check-circle"
                   CssClass="btn-primary">
        Assign Reviewers
    </LoadingButton>
</Card>
```

#### New: PlanReviewStatus.razor
```razor
<!-- src/PRFactory.Web/Components/PlanReview/PlanReviewStatus.razor -->
@inject IPlanReviewService PlanReviewService

<Card Title="Review Status" Icon="clipboard-check">
    @if (summary != null)
    {
        <div class="mb-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <span>Approvals Received</span>
                <strong class="@GetStatusClass()">
                    @summary.ReceivedApprovalCount / @summary.RequiredApprovalCount
                </strong>
            </div>

            <div class="progress" style="height: 25px;">
                <div class="progress-bar @GetProgressBarClass()"
                     role="progressbar"
                     style="width: @GetProgressPercentage()%">
                    @GetProgressPercentage()%
                </div>
            </div>
        </div>

        <h6>Reviewers</h6>
        <div class="list-group">
            @foreach (var reviewer in summary.Reviewers)
            {
                <div class="list-group-item d-flex justify-content-between align-items-center">
                    <div>
                        <strong>@reviewer.ReviewerName</strong>
                        @if (reviewer.IsRequired)
                        {
                            <span class="badge bg-danger ms-2">Required</span>
                        }
                        @if (reviewer.ReviewedAt.HasValue)
                        {
                            <br />
                            <small class="text-muted">
                                <RelativeTime Timestamp="@reviewer.ReviewedAt.Value" />
                            </small>
                        }
                    </div>
                    <StatusBadge State="@GetReviewStatusBadge(reviewer.Status)" />
                </div>
            }
        </div>
    }
</Card>
```

#### New: ReviewCommentThread.razor
```razor
<!-- src/PRFactory.Web/Components/PlanReview/ReviewCommentThread.razor -->
@inject IPlanReviewService PlanReviewService
@inject IUserService UserService

<Card Title="Discussion" Icon="chat-left-text">
    <!-- Comment list -->
    <div class="comment-thread mb-3" style="max-height: 400px; overflow-y: auto;">
        @foreach (var comment in comments.OrderBy(c => c.CreatedAt))
        {
            <div class="card mb-2">
                <div class="card-body py-2">
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <strong>@GetUserName(comment.AuthorId)</strong>
                        <small class="text-muted">
                            <RelativeTime Timestamp="@comment.CreatedAt" />
                        </small>
                    </div>
                    <div class="comment-content">
                        @((MarkupString)FormatCommentWithMentions(comment.Content, comment.MentionedUserIds))
                    </div>
                </div>
            </div>
        }
    </div>

    <!-- New comment form -->
    <FormField Label="Add Comment">
        <InputTextArea @bind-Value="newCommentContent"
                       class="form-control"
                       rows="3"
                       placeholder="Type @ to mention a team member..." />
    </FormField>

    <LoadingButton OnClick="PostComment"
                   IsLoading="@isPosting"
                   Icon="send"
                   CssClass="btn-primary">
        Post Comment
    </LoadingButton>
</Card>
```

#### Updated: PlanReviewSection.razor
```razor
<!-- src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor -->
<!-- REPLACE current implementation with team-aware version -->

@inject IPlanReviewService PlanReviewService
@inject ICurrentUserService CurrentUserService

<Section Title="Plan Review">
    <!-- Show reviewer assignment if no reviewers assigned yet -->
    @if (!hasReviewers)
    {
        <ReviewerAssignment TicketId="@Ticket.Id"
                          OnReviewersAssigned="HandleReviewersAssigned" />
    }
    else
    {
        <div class="row">
            <div class="col-md-6">
                <!-- Review status and approver list -->
                <PlanReviewStatus TicketId="@Ticket.Id" @ref="reviewStatus" />

                <!-- Current user's review actions -->
                @if (currentUserReview != null && currentUserReview.Status == ReviewStatus.Pending)
                {
                    <Card Title="Your Review" Icon="hand-thumbs-up" CssClass="mt-3">
                        <FormField Label="Decision (Optional)">
                            <InputTextArea @bind-Value="reviewDecision"
                                         class="form-control"
                                         rows="2"
                                         placeholder="Brief note about your decision..." />
                        </FormField>

                        <div class="btn-group w-100">
                            <LoadingButton OnClick="() => ApproveAsync()"
                                         IsLoading="@isSubmitting"
                                         Icon="check-circle"
                                         CssClass="btn-success">
                                Approve
                            </LoadingButton>

                            <LoadingButton OnClick="() => ShowRejectionForm()"
                                         IsLoading="@isSubmitting"
                                         Icon="x-circle"
                                         CssClass="btn-danger">
                                Reject
                            </LoadingButton>
                        </div>
                    </Card>
                }
                else if (currentUserReview != null)
                {
                    <InfoBox Type="AlertType.Info" Icon="info-circle">
                        You have already reviewed this plan: @currentUserReview.Status
                    </InfoBox>
                }
            </div>

            <div class="col-md-6">
                <!-- Comment thread for discussion -->
                <ReviewCommentThread TicketId="@Ticket.Id" />
            </div>
        </div>
    }
</Section>

<!-- Rejection modal (same as before) -->
@if (showRejectionModal)
{
    <!-- ... modal implementation ... -->
}
```

### 5. Workflow Changes

#### PlanningGraph Updates

No significant changes to PlanningGraph itself - it already supports looping on rejection.

**Key points**:
- Graph still suspends at `awaiting_approval` state
- Resume still happens via `PlanApprovedMessage` or `PlanRejectedMessage`
- The **difference** is that `TicketApplicationService.CheckAndProcessApprovals()` now orchestrates when to send these messages based on multi-reviewer logic

#### Message Flow (Before vs After)

**Before (Single User)**:
```
User clicks "Approve"
  → TicketApplicationService.ApprovePlanAsync()
    → WorkflowOrchestrator.ResumeAsync(PlanApprovedMessage)
```

**After (Team Review)**:
```
User 1 clicks "Approve"
  → PlanReviewService.SubmitApprovalAsync(ticketId, userId)
    → Update PlanReview.Status = Approved
    → TicketApplicationService.CheckAndProcessApprovals(ticketId)
      → if (HasSufficientApprovals())
          → WorkflowOrchestrator.ResumeAsync(PlanApprovedMessage)
        else
          → Wait for more approvals

User 2 clicks "Approve"
  → PlanReviewService.SubmitApprovalAsync(ticketId, userId)
    → Update PlanReview.Status = Approved
    → TicketApplicationService.CheckAndProcessApprovals(ticketId)
      → if (HasSufficientApprovals())  ✅ TRUE (2/2 approvals)
          → WorkflowOrchestrator.ResumeAsync(PlanApprovedMessage)
```

**Rejection Handling**:
```
Any user clicks "Reject"
  → PlanReviewService.SubmitRejectionAsync(ticketId, userId, reason, regenerate)
    → Update PlanReview.Status = RejectedFor*
    → TicketApplicationService.CheckAndProcessApprovals(ticketId)
      → if (HasRejections()) ✅ TRUE
          → WorkflowOrchestrator.ResumeAsync(PlanRejectedMessage)
```

### 6. Configuration

#### Repository-Level Settings
```csharp
// src/PRFactory.Domain/Entities/Repository.cs
// ADD to existing Repository entity:

public class Repository : EntityBase
{
    // ... existing properties ...

    // Team review configuration
    public bool RequireTeamReview { get; private set; } = false;
    public int DefaultRequiredApprovalCount { get; private set; } = 1;
    public bool AllowOptionalReviewers { get; private set; } = true;

    public void ConfigureTeamReview(
        bool requireTeamReview,
        int defaultRequiredApprovalCount = 1,
        bool allowOptionalReviewers = true)
    {
        RequireTeamReview = requireTeamReview;
        DefaultRequiredApprovalCount = Math.Max(1, defaultRequiredApprovalCount);
        AllowOptionalReviewers = allowOptionalReviewers;
    }
}
```

### 7. Authentication Integration (Placeholder)

For MVP, we'll use a simple "current user" concept:

```csharp
// src/PRFactory.Web/Services/ICurrentUserService.cs
namespace PRFactory.Web.Services;

public interface ICurrentUserService
{
    Guid GetCurrentUserId();
    Task<User> GetCurrentUserAsync();
}

// Stub implementation (replace with Auth0/IdentityServer later)
public class StubCurrentUserService : ICurrentUserService
{
    private readonly Guid _stubUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid GetCurrentUserId() => _stubUserId;

    public async Task<User> GetCurrentUserAsync()
    {
        // For MVP: Return a hardcoded user
        // TODO: Replace with actual authentication
        return new User(
            Guid.Parse("00000000-0000-0000-0000-000000000001"), // tenantId
            "dev@prfactory.local",
            "Dev User");
    }
}
```

## Implementation Plan

### Phase 1: Data Model (1-2 days)
1. Create User, PlanReview, ReviewComment entities
2. Update Ticket entity with team review properties
3. Create EF Core configuration and migrations
4. Update repositories (UserRepository, PlanReviewRepository)

### Phase 2: Application Services (1-2 days)
1. Implement UserService
2. Implement PlanReviewService
3. Update TicketApplicationService with CheckAndProcessApprovals
4. Add stub CurrentUserService

### Phase 3: UI Components (2-3 days)
1. Create ReviewerAssignment component
2. Create PlanReviewStatus component
3. Create ReviewCommentThread component
4. Update PlanReviewSection component
5. Add @mention parsing and formatting

### Phase 4: Integration & Testing (1-2 days)
1. End-to-end testing of multi-reviewer workflow
2. Test approval threshold logic (2/3, 3/5, etc.)
3. Test rejection workflow
4. Test comment threads with mentions

## Testing Scenarios

1. **Single Reviewer (Backward Compatibility)**
   - Assign 1 required reviewer
   - Approve → workflow continues immediately

2. **Multiple Required Reviewers**
   - Assign 3 required reviewers
   - 1st approves → workflow waits
   - 2nd approves → workflow waits
   - 3rd approves → workflow continues

3. **Mixed Required + Optional**
   - Assign 2 required, 2 optional
   - Required reviewers must approve
   - Optional reviewers can approve but doesn't count toward threshold
   - Verify only required approvals trigger workflow continuation

4. **Rejection**
   - Any reviewer rejects → workflow immediately loops to Planning
   - Verify other pending reviews are reset when new plan generated

5. **Comments and Mentions**
   - Post comment with @mentions
   - Verify mentions are parsed and stored
   - Verify mentions are formatted in UI

## Future Enhancements (Out of Scope)

- **Notifications**: Email/Slack when assigned, mentioned, or when plan ready
- **Review Delegation**: Transfer review to another user
- **Auto-Assignment**: Based on code ownership (CODEOWNERS file)
- **Review Templates**: Pre-defined review checklists
- **Approval Policies**: Branch protection-style rules (e.g., "Senior dev + architect required")
- **Real-time Updates**: SignalR for live review status updates
- **Review Analytics**: Time-to-approval metrics, reviewer workload

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Authentication complexity | Use stub CurrentUserService for MVP, integrate real auth later |
| @mention parsing complexity | Start with simple regex, iterate based on feedback |
| Notification overload | No notifications in MVP - future feature |
| Performance with large teams | Pagination in user search, limit reviewers per ticket |
| Concurrent approvals | Use optimistic concurrency (EF Core RowVersion) |

## Success Criteria

- ✅ Can assign multiple reviewers (required + optional) to a plan
- ✅ Plan approval requires X out of Y required reviewers
- ✅ Any rejection triggers plan regeneration
- ✅ Comment threads work with @mentions
- ✅ UI clearly shows approval progress (2/3 approved)
- ✅ Backward compatible: Single-user workflow still works (1 required reviewer)
- ✅ All existing tests pass
- ✅ End-to-end team review workflow validated

---

**Next Steps**: Review this design with team, then proceed to implementation Phase 1.
