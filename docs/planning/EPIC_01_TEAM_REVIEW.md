# Epic 1: Team Review & Collaboration

**Status:** 🟡 Partially Implemented (Core entities and services exist)
**Priority:** P1 (Critical)
**Effort:** 1-2 weeks (implementation and testing)
**Dependencies:** None

---

## Strategic Goal

Transform PRFactory from **single-player** to **multi-player**. Enable team collaboration on AI-generated plans through commenting, discussion, and formal approval workflows.

**Current State:**
- ✅ Core entities exist (PlanReview, ReviewComment, ReviewStatus)
- ✅ Application services implemented (IPlanReviewService, IPlanService)
- ✅ Repositories implemented (PlanReviewRepository, ReviewCommentRepository)
- ✅ Blazor components exist (PlanReviewSection, PlanReviewStatus)
- ⚠️ Some features need enhancement (AI validation, advanced UI)

**Remaining Work:**
- Agent-based plan validation (`cli review` commands)
- Enhanced UI components for better collaboration
- Notification system for @mentions
- Advanced analytics and reporting

---

## Success Criteria

✅ **Already Implemented:**
- Team members can be assigned as reviewers (required/optional)
- Review workflow exists (Pending → Approved/Rejected)
- Comments with @mentions
- Plan cannot proceed until sufficient approvals received
- Blazor Server architecture with service injection (NO HTTP calls)

✅ **Must Have (This Epic):**
- `cli review` agent can validate plans with AI
- Code-vs-plan alignment validation
- Enhanced comment threading UI
- Notification system for @mentions
- Plan revision history viewer

✅ **Nice to Have (Future):**
- Email notifications for comments/approvals
- Real-time collaboration (SignalR)
- Plan diff viewer (show changes between plan revisions)
- Analytics dashboard (approval rates, review times)

---

## Current Architecture (What Already Exists)

### Domain Entities

**Ticket Entity** (Plan data stored here):
- `PlanBranchName` (string) - Branch where plan is stored (LibGit2Sharp)
- `PlanMarkdownPath` (string) - Path to plan markdown file
- `PlanApprovedAt` (DateTime?) - When approved
- `RequiredApprovalCount` (int) - Number of required approvals

**PlanReview Entity** (`/PRFactory.Domain/Entities/PlanReview.cs`):
- `Id`, `TicketId`, `ReviewerId`, `Status`, `IsRequired`, `AssignedAt`, `ReviewedAt`, `Decision`
- Methods: `Approve()`, `Reject()`, `ResetForNewPlan()`, `SetRequired()`

**ReviewComment Entity** (`/PRFactory.Domain/Entities/ReviewComment.cs`):
- `Id`, `TicketId`, `AuthorId`, `Content`, `MentionedUserIds`, `CreatedAt`, `UpdatedAt`
- Methods: `Update()`, `MentionsUser()`, `AddMention()`, `RemoveMention()`

**ReviewStatus Enum**:
- `Pending` = 0
- `Approved` = 1
- `RejectedForRefinement` = 2 (refine while keeping structure)
- `RejectedForRegeneration` = 3 (regenerate from scratch)

### Application Services

**IPlanService** (`/PRFactory.Core/Application/Services/IPlanService.cs`):
- `GetPlanAsync(Guid ticketId)` - Returns PlanInfo (reads markdown from git branch using LibGit2Sharp)

**IPlanReviewService** (`/PRFactory.Core/Application/Services/IPlanReviewService.cs`):
- `AssignReviewersAsync()` - Assign reviewers (required/optional)
- `GetReviewsByTicketIdAsync()` - Get all reviews
- `GetPendingReviewsForReviewerAsync()` - Get pending reviews
- `ApproveReviewAsync()` - Approve a review
- `RejectReviewAsync()` - Reject a review (refine or regenerate)
- `AddCommentAsync()` - Add comment with @mentions
- `GetCommentsByTicketIdAsync()` - Get all comments
- `UpdateCommentAsync()` - Update comment
- `DeleteCommentAsync()` - Delete comment
- `HasSufficientApprovalsAsync()` - Check approval threshold
- `ResetReviewsForNewPlanAsync()` - Reset reviews when plan regenerated

**Implementation:** `/PRFactory.Infrastructure/Application/PlanService.cs` and `PlanReviewService.cs`
- Uses LibGit2Sharp to read plan files from git branches
- Delegates to domain methods on Ticket entity
- Uses repositories for data access
- Logs all review actions

### Web Services (Blazor Server Facades)

**ITicketService** (`/PRFactory.Web/Services/ITicketService.cs`):

**Team Review Methods:**
- `GetReviewersAsync()` - Returns List&lt;ReviewerDto&gt;
- `AssignReviewersAsync()` - Assign reviewers
- `ApproveReviewAsync()` - Approve review
- `RejectReviewAsync()` - Reject review
- `GetCommentsAsync()` - Returns List&lt;ReviewCommentDto&gt;
- `AddCommentAsync()` - Add comment
- `HasSufficientApprovalsAsync()` - Check approvals

**Plan Methods:**
- `GetPlanAsync()` - Returns PlanDto (markdown content)
- `ApprovePlanAsync()` - Approve and proceed
- `RejectPlanAsync()` - Reject with reason
- `RefinePlanAsync()` - Refine with instructions

**Implementation:** `/PRFactory.Web/Services/TicketService.cs`
- **Blazor Server architecture** - Uses direct service injection (NO HTTP calls)
- Injects `IPlanService` and `IPlanReviewService`
- Converts between domain entities and DTOs

### Blazor Components

**PlanReviewSection.razor** (`/PRFactory.Web/Components/Tickets/PlanReviewSection.razor`):
- Displays plan details (branch name, markdown path)
- Single-user review actions: Approve, Request Refinements, Reject & Regenerate
- Team review features: Reviewer assignment, status display, comment thread
- Validates sufficient approvals before allowing plan approval

**PlanReviewStatus.razor** (`/PRFactory.Web/Components/Tickets/PlanReviewStatus.razor`):
- Shows required/optional reviewers with approval count badge
- Displays reviewer avatars with status
- Shows reviewer decisions and relative time

**Related Components:**
- `ReviewerAssignment.razor` - UI for assigning reviewers
- `ReviewCommentThread.razor` - Discussion thread UI
- `ReviewerAvatar.razor` - Avatar display with status badge

### Architecture Pattern (Blazor Server)

```
┌─────────────────────────────────────────────────────────────┐
│                 Blazor Server Components                     │
│           (PlanReviewSection.razor.cs)                       │
└────────────────────────┬────────────────────────────────────┘
                         │ @inject ITicketService
                         ▼
┌─────────────────────────────────────────────────────────────┐
│          Web Layer Service (Facade Pattern)                  │
│              PRFactory.Web/Services/                         │
│               TicketService.cs                               │
│                                                               │
│   - Converts between DTOs and domain entities                │
│   - Facade for multiple application services                 │
│   - Injected into Blazor components                          │
└────────────────────────┬────────────────────────────────────┘
                         │ Injects application services
                         ▼
┌─────────────────────────────────────────────────────────────┐
│        Application Service Layer (Business Logic)            │
│         PRFactory.Infrastructure/Application/                │
│            PlanService.cs, PlanReviewService.cs              │
│                                                               │
│   - Encapsulates business logic                              │
│   - Coordinates multiple repositories                        │
│   - Shared by Blazor AND external clients                    │
└────────────────────────┬────────────────────────────────────┘
                         │ Injects repositories
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Infrastructure Layer                            │
│       PlanReviewRepository, ReviewCommentRepository          │
└─────────────────────────────────────────────────────────────┘
```

**IMPORTANT:** This is a **Blazor Server** application.
- ❌ DO NOT create API controllers for internal Blazor use
- ✅ DO use direct service injection (`@inject ITicketService`)
- ❌ DO NOT make HTTP calls within the same process
- ✅ DO use application services (IPlanService, IPlanReviewService)

---

## Implementation Plan

### 1. Agent: `cli review` - Plan Validation (NEW)

**Command 1: Plan Review**

```bash
# User provides natural language prompt
cli review --plan ./plan/05-implementation_steps.md --prompt "Is this plan secure and scalable?"

# Or use pre-defined prompts
cli review --plan ./plan/ --check security
cli review --plan ./plan/ --check completeness
cli review --plan ./plan/ --check performance
```

**Prompts to Create:**

`/prompts/review/anthropic/plan_security_check.txt`:
```
You are a security expert reviewing an implementation plan.

Analyze the provided plan and identify:
1. Security vulnerabilities or risks
2. Missing authentication/authorization checks
3. Data validation gaps
4. Exposure of sensitive information
5. OWASP Top 10 concerns

Plan:
{plan_content}

Provide a security assessment with:
- Risk level (Low, Medium, High, Critical)
- List of findings
- Recommended mitigations
```

`/prompts/review/anthropic/plan_completeness_check.txt`:
```
You are a technical architect reviewing an implementation plan.

Analyze the provided plan and identify:
1. Missing functional requirements
2. Gaps in error handling
3. Missing edge cases
4. Incomplete test coverage
5. Undefined API contracts
6. Missing database migrations

Plan:
{plan_content}

Provide a completeness assessment with:
- Completeness score (0-100)
- List of gaps
- Recommendations to fill gaps
```

**Service to Create:**

`/PRFactory.Infrastructure/Application/PlanValidationService.cs`:
```csharp
public interface IPlanValidationService
{
    Task<PlanValidationResult> ValidatePlanAsync(Guid ticketId, string checkType);
    Task<PlanValidationResult> ValidatePlanWithPromptAsync(Guid ticketId, string customPrompt);
    Task<CodePlanAlignmentResult> ValidateCodeVsPlanAsync(Guid ticketId, string diffContent);
}

public class PlanValidationResult
{
    public string CheckType { get; set; }
    public int Score { get; set; }
    public string RiskLevel { get; set; }
    public List<string> Findings { get; set; }
    public List<string> Recommendations { get; set; }
    public string RawResponse { get; set; }
}
```

**Command 2: Code-vs-Plan Validation**

```bash
# Validate that implemented code matches approved plan
cli review --validate \
  --plan-dir ./plan/ \
  --diff ./workspace/task-123/diff.patch
```

**Validation Prompt:**

`/prompts/review/anthropic/code_plan_validation.txt`:
```
You are a meticulous code reviewer validating implementation against an approved plan.

Your task:
1. Compare the code diff against ALL plan artifacts
2. Verify every requirement in the plan is implemented
3. Identify code that was written but NOT in the plan
4. Check for deviations from the specified approach

Plan Artifacts:
{plan_artifacts}

Code Diff:
{code_diff}

Provide validation results:
- ✅ Requirements successfully implemented (list each)
- ❌ Requirements missing or incomplete (list each)
- ⚠️ Deviations from plan (list each with explanation)
- ❓ Code written not specified in plan (list each)
- Overall score: 0-100 (100 = perfect alignment)
```

**Web UI Integration:**

Add "Run Plan Validation" to PlanReviewSection component:

```razor
<Card Title="Plan Analysis" Icon="shield-check">
    <FormField Label="Analysis Type">
        <InputSelect @bind-Value="selectedCheck" class="form-control">
            <option value="security">Security Review</option>
            <option value="completeness">Completeness Check</option>
            <option value="performance">Performance Analysis</option>
        </InputSelect>
    </FormField>

    <LoadingButton OnClick="HandleRunAnalysis" IsLoading="@isAnalyzing" Icon="play-circle">
        Run Analysis
    </LoadingButton>

    @if (analysisResult != null)
    {
        <Card Title="Analysis Results" Icon="file-earmark-text">
            <div class="validation-result">
                <div class="alert alert-@GetAlertClass(analysisResult.RiskLevel)">
                    <strong>Risk Level:</strong> @analysisResult.RiskLevel
                    <br />
                    <strong>Score:</strong> @analysisResult.Score / 100
                </div>

                <h6>Findings:</h6>
                <ul>
                    @foreach (var finding in analysisResult.Findings)
                    {
                        <li>@finding</li>
                    }
                </ul>

                <h6>Recommendations:</h6>
                <ul>
                    @foreach (var rec in analysisResult.Recommendations)
                    {
                        <li>@rec</li>
                    }
                </ul>
            </div>
        </Card>
    }
</Card>
```

**Files to Create:**
- `/prompts/review/anthropic/plan_security_check.txt`
- `/prompts/review/anthropic/plan_completeness_check.txt`
- `/prompts/review/anthropic/plan_performance_check.txt`
- `/prompts/review/anthropic/code_plan_validation.txt`
- `/PRFactory.Core/Application/Services/IPlanValidationService.cs`
- `/PRFactory.Infrastructure/Application/PlanValidationService.cs`
- CLI command implementation (integrate with existing CLI structure)

---

### 2. Enhanced UI - Notification System (NEW)

**Goal:** Notify users when mentioned in comments or assigned as reviewers

**Service to Create:**

`/PRFactory.Infrastructure/Application/NotificationService.cs`:
```csharp
public interface INotificationService
{
    Task NotifyReviewerAssignedAsync(Guid reviewerId, Guid ticketId, bool isRequired);
    Task NotifyMentionedInCommentAsync(List<Guid> mentionedUserIds, Guid ticketId, Guid commentId);
    Task NotifyPlanApprovedAsync(Guid ticketId, List<Guid> reviewerIds);
    Task NotifyPlanRejectedAsync(Guid ticketId, List<Guid> reviewerIds, string reason);
}
```

**Implementation:**
- In-app notifications (show in UI navbar)
- Future: Email notifications (via SendGrid/SMTP)
- Future: Slack/Teams integration

**Blazor Component:**

`/PRFactory.Web/Components/Notifications/NotificationBell.razor`:
- Show notification count badge
- Dropdown with recent notifications
- Mark as read functionality

---

### 3. Enhanced UI - Comment Threading Improvements (NEW)

**Current:** ReviewCommentThread.razor exists but may need enhancement

**Enhancements:**
- Rich text editor for comments (markdown support with preview)
- @mention autocomplete (type @ to see user list)
- Edit/delete comments (if author)
- Reply threading (nested comments)
- Emoji reactions (👍, ❤️, etc.)

**Files to Enhance:**
- `/PRFactory.Web/Components/Tickets/ReviewCommentThread.razor`
- `/PRFactory.Web/UI/Forms/MarkdownEditor.razor` (NEW - rich editor component)

---

### 4. Plan Revision History (NEW)

**Goal:** Track plan changes over time when refined/regenerated

**Database Schema:**

```sql
CREATE TABLE PlanRevisions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TicketId UNIQUEIDENTIFIER NOT NULL,
    RevisionNumber INT NOT NULL,
    BranchName NVARCHAR(255) NOT NULL,
    MarkdownPath NVARCHAR(500) NOT NULL,
    CommitHash NVARCHAR(100) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
    RevisionReason NVARCHAR(50) NOT NULL, -- Initial, Refined, Regenerated

    CONSTRAINT FK_PlanRevisions_Tickets FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
    CONSTRAINT FK_PlanRevisions_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

CREATE INDEX IX_PlanRevisions_TicketId ON PlanRevisions(TicketId);
```

**Entity to Create:**

`/PRFactory.Domain/Entities/PlanRevision.cs`

**Service Methods to Add:**

`IPlanService`:
- `GetPlanRevisionsAsync(Guid ticketId)` - Get revision history
- `GetPlanRevisionAsync(Guid revisionId)` - Get specific revision
- `CreateRevisionAsync(Guid ticketId, string reason)` - Snapshot current plan

**Blazor Component:**

`/PRFactory.Web/Components/Plans/PlanRevisionHistory.razor`:
- Timeline view of revisions
- Compare revisions (diff viewer)
- Restore previous revision option

---

## Acceptance Criteria

### Agent-Based Validation
- [ ] `cli review --plan` command validates plans with custom or pre-defined prompts
- [ ] `cli review --check security|completeness|performance` shortcuts work
- [ ] `cli review --validate` compares code diff against plan artifacts
- [ ] Validation returns exit code 0 (success) or 1 (failure) based on score
- [ ] Prompt templates created and loaded correctly
- [ ] IPlanValidationService and implementation created
- [ ] Web UI can trigger validation and display results

### Notification System
- [ ] INotificationService and implementation created
- [ ] In-app notifications displayed in UI navbar
- [ ] Users notified when assigned as reviewers
- [ ] Users notified when mentioned in comments
- [ ] Notifications marked as read

### Enhanced Comments
- [ ] Markdown editor with preview for comments
- [ ] @mention autocomplete shows user list
- [ ] Edit/delete comments (if author)
- [ ] Emoji reactions on comments

### Plan Revision History
- [ ] PlanRevisions table and entity created
- [ ] Service methods for revision tracking implemented
- [ ] Revision created automatically when plan refined/regenerated
- [ ] PlanRevisionHistory component displays timeline
- [ ] Diff viewer compares revisions
- [ ] Restore previous revision functionality

### Integration
- [ ] Validation results displayed in PlanReviewSection
- [ ] Notifications integrate with existing workflow
- [ ] Plan revision history accessible from ticket detail page
- [ ] All features work with Blazor Server architecture (no API controllers)

---

## Testing Plan

### Unit Tests
- [ ] PlanValidationService logic
- [ ] NotificationService notification creation
- [ ] PlanRevision entity methods
- [ ] Comment threading logic with @mentions

### Integration Tests
- [ ] Validate plan via service
- [ ] Create notification and retrieve
- [ ] Create plan revision and retrieve history
- [ ] Add comment with @mentions

### E2E Tests
- [ ] User triggers plan validation from UI
- [ ] User receives notification when mentioned
- [ ] User views plan revision history
- [ ] User compares two plan revisions
- [ ] Validation fails if code doesn't match plan

---

## Migration Path

### Phase 1: Agent-Based Validation (Week 1)
1. Create prompt templates for validation
2. Implement IPlanValidationService and PlanValidationService
3. Integrate with existing agent infrastructure
4. Add validation UI to PlanReviewSection component
5. Write unit and integration tests
6. CLI command implementation (if needed)

### Phase 2: Notification System (Week 1-2)
1. Create INotificationService and NotificationService
2. Add notification creation to review/comment flows
3. Build NotificationBell Blazor component
4. Add notification repository and entity (if needed)
5. Test notification delivery

### Phase 3: Enhanced UI Features (Week 2)
1. Create PlanRevisions table and entity
2. Implement revision tracking in PlanService
3. Build PlanRevisionHistory component with diff viewer
4. Enhance ReviewCommentThread with markdown editor and @mentions
5. Add emoji reactions
6. Test all UI enhancements

---

## Risks & Mitigations

**Risk:** LLM validation may give inconsistent results
**Mitigation:** Use structured prompts, extract scores with regex, set clear thresholds (e.g., 90%)

**Risk:** Notification spam if too many @mentions
**Mitigation:** Batch notifications, allow user preferences for notification frequency

**Risk:** Plan revision storage can grow large
**Mitigation:** Archive old revisions, compress markdown content, implement retention policy

**Risk:** Complexity of diff viewer for large plans
**Mitigation:** Use proven diff libraries (Monaco Editor, diff-match-patch), paginate results

---

## Related Epics

- **Epic 2 (Multi-LLM):** Review prompts should work with multiple LLM providers
- **Epic 3 (Deep Planning):** More plan artifacts = more validation needed
- **Epic 4 (Diff Viewer):** Code-vs-plan validation displayed in diff viewer

---

## Notes

**Architecture Compliance:**
- ✅ Follows Blazor Server architecture (no API controllers for internal use)
- ✅ Uses service injection pattern (ITicketService → IPlanReviewService)
- ✅ Uses LibGit2Sharp for git operations
- ✅ Follows Clean Architecture layers (Domain → Application → Web)
- ✅ Uses code-behind pattern for Blazor components
- ✅ Uses existing UI component library (/UI/*)

**Next Steps:**
1. Review this epic with team
2. Assign engineer(s) to implement
3. Create tickets for Phase 1, 2, 3
4. Start with agent validation (highest value)
