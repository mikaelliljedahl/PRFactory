# Epic 1: Team Review & Collaboration

**Status:** 🔴 Not Started
**Priority:** P1 (Critical)
**Effort:** 2-3 weeks
**Dependencies:** None

---

## Strategic Goal

Transform PRFactory from **single-player** to **multi-player**. Enable team collaboration on AI-generated plans through commenting, discussion, and formal approval workflows.

**Current Pain:** AI generates plans, but only one person can review. Teams can't collaborate or discuss before implementation.

**Solution:** Build collaborative review features directly into the Web UI, making Phase 2 (Planning) a team-based function.

---

## Success Criteria

✅ **Must Have:**
- Team members can comment on AI-generated plans
- Threaded discussions with replies
- Formal approval workflow (Draft → PendingReview → ChangesRequested → Approved)
- Plan cannot proceed to implementation until approved
- `cli review` agent can validate plans and code-vs-plan alignment

✅ **Nice to Have:**
- @mentions to notify team members
- Comment resolution workflow (Active → Resolved)
- Email notifications for comments/approvals
- Plan diff viewer (show changes between plan revisions)

---

## Implementation Plan

### 1. Web UI - Commenting System

**Database Schema:**

```sql
CREATE TABLE PlanComments (
    CommentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanID UNIQUEIDENTIFIER NOT NULL,
    UserID UNIQUEIDENTIFIER NOT NULL,
    ParentCommentID UNIQUEIDENTIFIER NULL,  -- For threading
    Content NVARCHAR(MAX) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',  -- Active, Resolved

    CONSTRAINT FK_PlanComments_Plans FOREIGN KEY (PlanID) REFERENCES Plans(Id),
    CONSTRAINT FK_PlanComments_Users FOREIGN KEY (UserID) REFERENCES Users(Id),
    CONSTRAINT FK_PlanComments_Parent FOREIGN KEY (ParentCommentID) REFERENCES PlanComments(CommentID)
);

CREATE INDEX IX_PlanComments_PlanID ON PlanComments(PlanID);
CREATE INDEX IX_PlanComments_ParentCommentID ON PlanComments(ParentCommentID);
```

**Backend API Endpoints:**

```csharp
// POST /api/plans/{planId}/comments
public class CreateCommentRequest
{
    public string Content { get; set; }
    public Guid? ParentCommentId { get; set; }  // null for top-level
}

// GET /api/plans/{planId}/comments
public class CommentDto
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }
    public Guid? ParentCommentId { get; set; }
    public List<CommentDto> Replies { get; set; }  // Nested structure
}

// PUT /api/comments/{commentId}
// DELETE /api/comments/{commentId}
```

**Frontend UI (Blazor):**

Create `/PRFactory.Web/Components/Plans/PlanCommentThread.razor`:

```razor
<Card Title="Team Discussion" Icon="chat-dots">
    @foreach (var comment in topLevelComments)
    {
        <CommentItem Comment="@comment" OnReply="HandleReply" />

        @if (comment.Replies.Any())
        {
            <div class="ms-4">
                @foreach (var reply in comment.Replies)
                {
                    <CommentItem Comment="@reply" OnReply="HandleReply" />
                }
            </div>
        }
    }

    <FormField Label="Add Comment">
        <InputTextArea @bind-Value="newCommentContent" rows="3" />
    </FormField>
    <LoadingButton OnClick="HandlePostComment" Icon="send">
        Post Comment
    </LoadingButton>
</Card>
```

**Files to Create:**
- `/PRFactory.Core/Entities/PlanComment.cs` (domain entity)
- `/PRFactory.Core/Repositories/IPlanCommentRepository.cs` (interface)
- `/PRFactory.Infrastructure/Repositories/PlanCommentRepository.cs` (EF Core)
- `/PRFactory.Infrastructure/Application/PlanCommentService.cs` (business logic)
- `/PRFactory.Api/Controllers/PlanCommentsController.cs` (API endpoints)
- `/PRFactory.Web/Components/Plans/PlanCommentThread.razor` (UI component)
- `/PRFactory.Web/Components/Plans/CommentItem.razor` (individual comment)

---

### 2. Web UI - Approval Workflow

**Database Schema:**

```sql
ALTER TABLE Plans
ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Draft';

-- Possible values: Draft, PendingReview, ChangesRequested, Approved
```

**State Machine Logic:**

```csharp
public enum PlanStatus
{
    Draft,
    PendingReview,
    ChangesRequested,
    Approved
}

public class Plan
{
    public PlanStatus Status { get; private set; } = PlanStatus.Draft;

    public void RequestReview(Guid requestedBy)
    {
        if (Status != PlanStatus.Draft && Status != PlanStatus.ChangesRequested)
            throw new InvalidOperationException("Can only request review from Draft or ChangesRequested");

        Status = PlanStatus.PendingReview;
        // Emit event: PlanReviewRequestedEvent
    }

    public void RequestChanges(Guid reviewerId, string reason)
    {
        if (Status != PlanStatus.PendingReview)
            throw new InvalidOperationException("Can only request changes when PendingReview");

        Status = PlanStatus.ChangesRequested;
        // Emit event: PlanChangesRequestedEvent
        // Create comment with reason
    }

    public void Approve(Guid approverId)
    {
        if (Status != PlanStatus.PendingReview)
            throw new InvalidOperationException("Can only approve when PendingReview");

        Status = PlanStatus.Approved;
        // Emit event: PlanApprovedEvent
    }
}
```

**Backend API Endpoints:**

```csharp
// POST /api/plans/{planId}/request-review
public Task<IActionResult> RequestReview(Guid planId);

// POST /api/plans/{planId}/request-changes
public class RequestChangesRequest
{
    public string Reason { get; set; }  // Required
}
public Task<IActionResult> RequestChanges(Guid planId, RequestChangesRequest request);

// POST /api/plans/{planId}/approve
public Task<IActionResult> Approve(Guid planId);
```

**Frontend UI (Blazor):**

Update `/PRFactory.Web/Pages/Plans/Detail.razor`:

```razor
<Card Title="Plan Status" Icon="diagram-3">
    <StatusBadge Status="@plan.Status" />

    @if (plan.Status == PlanStatus.Draft || plan.Status == PlanStatus.ChangesRequested)
    {
        <LoadingButton OnClick="HandleRequestReview" Icon="send-check">
            Request Review
        </LoadingButton>
    }

    @if (plan.Status == PlanStatus.PendingReview && IsReviewer(currentUser))
    {
        <LoadingButton OnClick="HandleApprove" Icon="check-circle" Color="success">
            Approve Plan
        </LoadingButton>

        <LoadingButton OnClick="HandleRequestChanges" Icon="x-circle" Color="warning">
            Request Changes
        </LoadingButton>
    }

    @if (plan.Status == PlanStatus.Approved)
    {
        <LoadingButton OnClick="HandleStartImplementation" Icon="code-slash">
            Start Implementation (Phase 3)
        </LoadingButton>
    }
    else
    {
        <InfoBox Type="Warning">
            Plan must be approved before implementation can begin.
        </InfoBox>
    }
</Card>
```

**Business Logic:**

```csharp
// Check user roles for approval permissions
public bool CanApprove(User user, Plan plan)
{
    // Only team leads or plan owners can approve
    return user.Roles.Contains("TeamLead") || plan.OwnerId == user.Id;
}
```

**Files to Modify:**
- `/PRFactory.Core/Entities/Plan.cs` (add Status property, state machine methods)
- `/PRFactory.Infrastructure/Application/PlanService.cs` (workflow methods)
- `/PRFactory.Api/Controllers/PlansController.cs` (approval endpoints)
- `/PRFactory.Web/Pages/Plans/Detail.razor` (approval UI)

---

### 3. Agent: `cli review` - Plan Validation

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

**CLI Implementation:**

```csharp
// cli review --plan <path> --prompt <text>
public class ReviewPlanCommand
{
    [Option("--plan", Required = true)]
    public string PlanPath { get; set; }

    [Option("--prompt", Required = false)]
    public string? CustomPrompt { get; set; }

    [Option("--check", Required = false)]
    public string? CheckType { get; set; }  // security, completeness, performance

    public async Task<int> ExecuteAsync()
    {
        // Load plan content
        var planContent = File.ReadAllText(PlanPath);

        // Select prompt
        var promptTemplate = CheckType switch
        {
            "security" => LoadPrompt("review/plan_security_check.txt"),
            "completeness" => LoadPrompt("review/plan_completeness_check.txt"),
            _ => CustomPrompt ?? throw new ArgumentException("Must provide --prompt or --check")
        };

        // Call LLM
        var response = await _llmProvider.SendMessageAsync(
            prompt: promptTemplate.Replace("{plan_content}", planContent));

        // Output results
        Console.WriteLine(response.Content);
        return 0;
    }
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

**CLI Implementation:**

```csharp
public class ValidateCodeCommand
{
    [Option("--validate", Required = true)]
    public bool Validate { get; set; }

    [Option("--plan-dir", Required = true)]
    public string PlanDirectory { get; set; }

    [Option("--diff", Required = true)]
    public string DiffFilePath { get; set; }

    public async Task<int> ExecuteAsync()
    {
        // Load all plan artifacts
        var planFiles = Directory.GetFiles(PlanDirectory, "*.md");
        var planContent = string.Join("\n\n---\n\n",
            planFiles.Select(f => $"## {Path.GetFileName(f)}\n\n{File.ReadAllText(f)}"));

        // Load diff
        var diff = File.ReadAllText(DiffFilePath);

        // Load validation prompt
        var promptTemplate = LoadPrompt("review/code_plan_validation.txt");
        var prompt = promptTemplate
            .Replace("{plan_artifacts}", planContent)
            .Replace("{code_diff}", diff);

        // Call LLM
        var response = await _llmProvider.SendMessageAsync(prompt);

        // Parse score from response
        var score = ExtractScore(response.Content);

        // Output results
        Console.WriteLine(response.Content);

        // Exit code: 0 if score >= 90, 1 otherwise
        return score >= 90 ? 0 : 1;
    }
}
```

**Web UI Integration:**

Add "Run Plan Validation" button in plan detail page:

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

    @if (!string.IsNullOrEmpty(analysisResult))
    {
        <Card Title="Analysis Results" Icon="file-earmark-text">
            <pre>@analysisResult</pre>
        </Card>
    }
</Card>
```

**Files to Create:**
- `/prompts/review/anthropic/plan_security_check.txt`
- `/prompts/review/anthropic/plan_completeness_check.txt`
- `/prompts/review/anthropic/plan_performance_check.txt`
- `/prompts/review/anthropic/code_plan_validation.txt`
- CLI command implementation (exact location TBD based on CLI structure)

---

## Acceptance Criteria

### Database & Backend
- [ ] `PlanComments` table created with migrations
- [ ] `Plans.Status` column added
- [ ] `IPlanCommentRepository` and implementation created
- [ ] `PlanCommentService` with business logic
- [ ] API endpoints for comments (POST, GET, PUT, DELETE)
- [ ] API endpoints for approval workflow (request-review, request-changes, approve)

### Frontend (Blazor)
- [ ] `PlanCommentThread.razor` component displays threaded comments
- [ ] Users can post top-level comments
- [ ] Users can reply to comments (threading)
- [ ] Plan status badge shows current workflow state
- [ ] Approval buttons visible based on user role and plan status
- [ ] Implementation button locked until plan approved

### CLI Agent
- [ ] `cli review --plan` command validates plans with custom or pre-defined prompts
- [ ] `cli review --check security|completeness|performance` shortcuts work
- [ ] `cli review --validate` compares code diff against plan artifacts
- [ ] Validation returns exit code 0 (success) or 1 (failure) based on score
- [ ] Prompt templates created and loaded correctly

### Integration
- [ ] Web UI can trigger `cli review` commands
- [ ] Analysis results displayed in Web UI
- [ ] Code-vs-plan validation runs before PR creation
- [ ] Users cannot create PR if validation score < 90

---

## Testing Plan

### Unit Tests
- [ ] Plan state machine transitions (Draft → PendingReview → Approved)
- [ ] Invalid state transitions throw exceptions
- [ ] Comment threading logic
- [ ] Authorization checks (who can approve)

### Integration Tests
- [ ] Create comment via API
- [ ] Retrieve comments with nested replies
- [ ] Approval workflow API calls
- [ ] `cli review` executes and returns results

### E2E Tests
- [ ] User posts comment on plan
- [ ] User requests review on plan
- [ ] Reviewer approves plan
- [ ] Implementation starts only after approval
- [ ] Validation fails if code doesn't match plan

---

## Migration Path

### Phase 1: Database & Backend (Week 1)
1. Create `PlanComments` table and entity
2. Add `Status` to `Plans` table
3. Implement repositories and services
4. Create API endpoints
5. Write unit tests

### Phase 2: Frontend UI (Week 2)
1. Build `PlanCommentThread` component
2. Add approval workflow UI to plan detail page
3. Add status badges and buttons
4. Wire up API calls
5. Test UI flows

### Phase 3: CLI Agent (Week 2-3)
1. Create prompt templates
2. Implement `cli review --plan` command
3. Implement `cli review --validate` command
4. Add Web UI integration
5. Test end-to-end validation workflow

---

## Risks & Mitigations

**Risk:** Complex threading logic for comments
**Mitigation:** Use proven patterns (e.g., recursive CTE for comment trees), test thoroughly

**Risk:** LLM validation may give inconsistent results
**Mitigation:** Use structured prompts, extract scores with regex, set clear thresholds (e.g., 90%)

**Risk:** Users bypass approval workflow
**Mitigation:** Enforce at API level, not just UI (return 403 if plan not approved)

---

## Related Epics

- **Epic 2 (Multi-LLM):** Review prompts should work with multiple LLM providers
- **Epic 3 (Deep Planning):** More plan artifacts = more validation needed
- **Epic 4 (Diff Viewer):** Code-vs-plan validation displayed in diff viewer

---

**Next Steps:**
1. Review this epic with team
2. Assign engineer(s) to implement
3. Create tickets for Phase 1, 2, 3
4. Start with database schema and backend API
