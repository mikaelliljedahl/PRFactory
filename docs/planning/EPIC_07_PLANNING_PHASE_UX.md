# EPIC 07: Planning Phase UX & Collaboration Improvements

> **Status**: Draft
> **Priority**: High
> **Estimated Effort**: 8-12 weeks
> **Dependencies**: None (enhances existing planning phase)
> **Target Audience**: Development teams using PRFactory for collaborative planning

---

## Executive Summary

Enhance the planning phase with AI-powered plan review, rich markdown editing, and collaborative features to enable teams to iteratively refine implementation plans in a web browser before code implementation begins.

**Current State**: Planning phase generates plans and supports multi-reviewer approval, but uses plain textarea editing, lacks AI quality checks, and has limited collaboration features.

**Future State**: Teams can collaboratively edit plans in a rich markdown editor with live preview, receive AI-powered quality feedback, track version history, and use structured review criteria.

---

## Business Value

### Problems Being Solved

1. **Quality Assurance Gap**: Plans go straight to human review without AI validation
2. **Poor Editing Experience**: Plain textareas make it hard to edit large markdown documents
3. **Inefficient Collaboration**: No inline comments, threading, or structured discussion
4. **Lack of Transparency**: Teams can't see how plans evolved through refinements
5. **Unstructured Reviews**: Reviewers lack clear criteria for evaluating plan quality

### Expected Benefits

- **Faster Review Cycles**: AI catches quality issues before human review (estimated 30% time savings)
- **Better Plan Quality**: Structured prompts and review checklists improve completeness
- **Improved Collaboration**: Rich editor and inline comments enable team refinement
- **Audit Trail**: Version history provides transparency and learning opportunities
- **User Satisfaction**: Professional editing experience comparable to Notion/Confluence

### Success Metrics

- Average review cycles reduced from 2.5 to 1.5 iterations
- Plan completeness score (AI-evaluated) > 85% before human review
- User satisfaction score > 4.0/5.0 for editing experience
- 80% of plans use collaborative editing features

---

## User Stories

### Epic-Level User Stories

1. **As a developer**, I want AI to review generated plans for quality issues before I review them, so I can focus on business logic rather than catching basic mistakes.

2. **As a team lead**, I want my team to collaboratively edit plans in a rich markdown editor with live preview, so we can iterate quickly without context switching.

3. **As a reviewer**, I want structured review criteria and checklists, so I can consistently evaluate plan quality.

4. **As a project manager**, I want to see version history of plan refinements, so I can understand how decisions evolved and learn from past iterations.

5. **As a developer**, I want better planning prompts that guide the AI to generate more complete plans, so I spend less time requesting refinements.

---

## Architecture Overview

### Components to Build

```
┌─────────────────────────────────────────────────────────────┐
│                    PLANNING PHASE                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 1: Plan Generation (Enhanced)                        │
│  ─────────────────────────────────────────                  │
│  • PlanningAgent with improved prompts                      │
│  • Structured output format (JSON + Markdown)               │
│  • Domain-specific templates (web, API, infrastructure)     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 2: AI Plan Review (NEW)                              │
│  ─────────────────────────────────────                      │
│  • PlanReviewAgent validates generated plan                 │
│  • Checks completeness, consistency, best practices         │
│  • Generates quality score (0-100) and feedback             │
│  • Auto-requests refinement if score < threshold            │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 3: Git Commit + Jira Post (Existing)                 │
│  ─────────────────────────────────────────                  │
│  • GitPlanAgent commits to feature branch                   │
│  • JiraPostAgent posts to ticket with AI review summary     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 4: Human Collaborative Review (Enhanced)             │
│  ─────────────────────────────────────────                  │
│  • Rich Markdown Editor with live preview                   │
│  • Inline comments and threaded discussions                 │
│  • Review checklist with structured criteria                │
│  • Version history and diff viewer                          │
│  • @Mention notifications for collaboration                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
                  [Approve/Refine/Regenerate]
```

### Technology Stack

**Approved Libraries** (per CLAUDE.md):
- **Markdig**: Existing markdown parser (already installed)
- **Blazor Server**: Existing UI framework
- **Radzen Blazor**: Existing component library
- **SignalR**: Existing real-time framework
- **Bootstrap 5**: Existing CSS framework

**NO NEW UI LIBRARIES REQUIRED** - All features can be built with approved stack.

---

## Detailed Requirements

### Feature 1: Enhanced Planning Prompts

**Goal**: Improve plan generation quality through better AI prompts.

#### Requirements

1. **Structured Prompt Templates**
   - System prompt defines role, output format, quality criteria
   - User prompt includes codebase context, patterns, architecture
   - Templates for different domains (web UI, REST API, background jobs, database)
   - Handlebars templates for dynamic context injection

2. **Prompt Includes Quality Criteria**
   - Completeness: All sections required (overview, steps, tests, risks)
   - Specificity: File paths, line numbers, method signatures
   - Testability: Clear test cases for each change
   - Rollback: How to undo changes if needed
   - Dependencies: External packages, services, configurations

3. **Codebase Context Enrichment**
   - Include relevant file snippets (not just file names)
   - Architectural patterns used in the project
   - Technology stack with versions
   - Existing testing patterns
   - Code style guidelines

#### Acceptance Criteria

- [ ] New prompt templates in `/prompts/plan/anthropic/v2/`
- [ ] System prompt includes quality criteria checklist
- [ ] User prompt includes 3+ code snippets from relevant files
- [ ] Domain-specific templates for web/API/jobs/database
- [ ] Template tests validate all variables render correctly

#### Files to Create/Modify

**New Files**:
- `/prompts/plan/anthropic/v2/system.txt` - Enhanced system prompt
- `/prompts/plan/anthropic/v2/user_template.hbs` - Enhanced user template
- `/prompts/plan/anthropic/v2/web_domain.txt` - Web UI-specific guidance
- `/prompts/plan/anthropic/v2/api_domain.txt` - REST API-specific guidance
- `/prompts/plan/anthropic/v2/background_domain.txt` - Background jobs guidance
- `/prompts/plan/anthropic/v2/database_domain.txt` - Database schema guidance

**Modified Files**:
- `/src/PRFactory.Infrastructure/Agents/PlanningAgent.cs` - Load new prompt templates

#### Estimated Effort: 1 week

---

### Feature 2: AI Plan Review Agent

**Goal**: Automatically validate generated plans before human review.

#### Requirements

1. **PlanReviewAgent Implementation**
   - New agent that executes after PlanningAgent
   - Uses Claude to analyze generated plan
   - Evaluates against quality criteria checklist
   - Returns structured feedback with score (0-100)

2. **Quality Evaluation Criteria**
   - **Completeness** (25 points): All required sections present
   - **Specificity** (25 points): File paths, signatures, clear instructions
   - **Testability** (20 points): Test cases cover changes
   - **Safety** (15 points): Rollback plan, error handling, security
   - **Dependencies** (15 points): Packages, configs, prerequisites

3. **Automated Refinement Loop**
   - If score < 70: Auto-request refinement with specific feedback
   - If score >= 70: Proceed to human review
   - Max 3 automated refinement attempts
   - After 3 attempts: Proceed to human review with warnings

4. **Review Summary Storage**
   - Store AI review results in database
   - Include score, feedback, timestamp
   - Display in UI before human review

#### Acceptance Criteria

- [ ] PlanReviewAgent class implemented
- [ ] Quality scoring algorithm with 5 criteria
- [ ] Automated refinement loop (max 3 attempts)
- [ ] PlanReviewResult entity with score + feedback
- [ ] PlanningGraph updated with review stage
- [ ] UI displays AI review summary before human review
- [ ] Audit log tracks automated refinements

#### Files to Create/Modify

**New Files**:
- `/src/PRFactory.Infrastructure/Agents/PlanReviewAgent.cs`
- `/src/PRFactory.Domain/Entities/PlanReviewResult.cs`
- `/src/PRFactory.Infrastructure/Persistence/Configurations/PlanReviewResultConfiguration.cs`
- `/prompts/plan_review/anthropic/system.txt`
- `/prompts/plan_review/anthropic/user_template.hbs`
- `/src/PRFactory.Core/Application/DTOs/PlanReviewResultDto.cs`

**Modified Files**:
- `/src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs` - Add review stage
- `/src/PRFactory.Domain/Entities/Ticket.cs` - Add `LatestPlanReviewResult` property
- `/src/PRFactory.Infrastructure/Application/TicketApplicationService.cs` - Expose review results
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Display AI review

#### Database Schema

```sql
CREATE TABLE PlanReviewResults (
    Id UUID PRIMARY KEY,
    TicketId UUID NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    ReviewedAt TIMESTAMP NOT NULL,
    OverallScore INT NOT NULL CHECK (OverallScore >= 0 AND OverallScore <= 100),
    CompletenessScore INT NOT NULL,
    SpecificityScore INT NOT NULL,
    TestabilityScore INT NOT NULL,
    SafetyScore INT NOT NULL,
    DependencyScore INT NOT NULL,
    Feedback TEXT NOT NULL,
    AutoRefinementAttempt INT NOT NULL DEFAULT 0,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_planreviewresults_ticketid ON PlanReviewResults(TicketId);
```

#### Estimated Effort: 2 weeks

---

### Feature 3: Rich Markdown Editor Component

**Goal**: Provide professional markdown editing experience in the browser.

#### Requirements

1. **MarkdownEditor.razor Component**
   - Split-view layout: Editor (left) + Live Preview (right)
   - Toggle between split, editor-only, preview-only modes
   - Formatting toolbar with common markdown buttons
   - Syntax highlighting in editor pane
   - Live preview updates as user types (debounced 300ms)

2. **Formatting Toolbar**
   - Bold (**text**), Italic (*text*), Strikethrough (~~text~~)
   - Headings (H1-H6), Blockquote, Code block
   - Unordered list, Ordered list, Checklist
   - Link, Image, Table
   - Undo/Redo
   - Keyboard shortcuts (Ctrl+B, Ctrl+I, etc.)

3. **Editor Features**
   - Line numbers
   - Auto-indent
   - Tab key inserts spaces (configurable)
   - Bracket matching
   - Search/replace (Ctrl+F)
   - Full-screen mode

4. **Preview Features**
   - Consistent rendering using Markdig advanced pipeline
   - Syntax highlighting for code blocks (using highlight.js or Prism)
   - Scroll synchronization between editor and preview
   - CSS styling matching ticket display

5. **Responsive Design**
   - Desktop: Side-by-side split view
   - Tablet: Tabbed view (switch between edit/preview)
   - Mobile: Edit-only with preview button

#### Acceptance Criteria

- [ ] MarkdownEditor.razor component in `/UI/Editors/`
- [ ] Formatting toolbar with 15+ buttons
- [ ] Split-view with live preview (300ms debounce)
- [ ] Keyboard shortcuts for common formatting
- [ ] Syntax highlighting in code blocks
- [ ] Responsive layout (desktop/tablet/mobile)
- [ ] Full-screen mode toggle
- [ ] Search/replace functionality
- [ ] Scroll sync between editor and preview
- [ ] Unit tests for markdown parsing and toolbar actions

#### Files to Create/Modify

**New Files**:
- `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor`
- `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor.cs`
- `/src/PRFactory.Web/UI/Editors/MarkdownToolbar.razor`
- `/src/PRFactory.Web/UI/Editors/MarkdownPreview.razor`
- `/src/PRFactory.Web/wwwroot/css/markdown-editor.css`

**Modified Files**:
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Use MarkdownEditor
- `/src/PRFactory.Web/Components/Tickets/ReviewCommentThread.razor` - Use MarkdownEditor
- `/src/PRFactory.Web/Components/Tickets/TicketUpdateEditor.razor` - Use MarkdownEditor

#### Technology Notes

**NO JAVASCRIPT REQUIRED** (per CLAUDE.md):
- Use Blazor's `@bind` for two-way binding
- Use `@oninput` event for live preview
- Use CSS for layout and toolbar styling
- Use Radzen components for toolbar buttons/dropdowns

**Minimal JavaScript ONLY for**:
- Textarea focus management
- Scroll position sync (if unavoidable in pure Blazor)

#### Estimated Effort: 3 weeks

---

### Feature 4: Inline Comments & Threaded Discussions

**Goal**: Enable reviewers to comment on specific sections of the plan.

#### Requirements

1. **Inline Comment Anchors**
   - Users can select text in markdown preview
   - Click "Add Comment" to create anchor at selection
   - Anchor stored with line range (start/end) or text snippet
   - Comments displayed in right sidebar aligned with anchors

2. **Comment Threading**
   - Comments can have replies (parent-child relationship)
   - Nested replies up to 3 levels deep
   - Resolve/unresolve comment threads
   - Only show unresolved threads by default (toggle to show all)

3. **@Mention Notifications**
   - Type `@username` to mention team members
   - Autocomplete dropdown shows matching users
   - Mentioned users receive in-app notification
   - Email notification (optional, configurable)

4. **Comment UI Improvements**
   - Rich comment display with user avatars
   - Edit/delete own comments
   - Timestamp with relative time ("2 hours ago")
   - Reaction emojis (👍 👎 ✅ ❌)
   - Comment count badge on plan sections

#### Acceptance Criteria

- [ ] InlineCommentAnchor entity with line range
- [ ] ReviewComment parent-child relationship (threading)
- [ ] Comment resolution status (resolved/unresolved)
- [ ] @Mention parsing and user lookup
- [ ] In-app notification system for mentions
- [ ] Email notification service (optional)
- [ ] UI: Inline comment sidebar in plan viewer
- [ ] UI: Threaded comment display with nesting
- [ ] UI: @Mention autocomplete in comment editor
- [ ] UI: Comment edit/delete with permissions
- [ ] UI: Reaction emojis on comments
- [ ] SignalR real-time updates for new comments

#### Files to Create/Modify

**New Files**:
- `/src/PRFactory.Domain/Entities/InlineCommentAnchor.cs`
- `/src/PRFactory.Domain/Entities/Notification.cs`
- `/src/PRFactory.Core/Application/Services/INotificationService.cs`
- `/src/PRFactory.Infrastructure/Application/NotificationService.cs`
- `/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor`
- `/src/PRFactory.Web/UI/Comments/CommentThread.razor`
- `/src/PRFactory.Web/UI/Comments/CommentEditor.razor`
- `/src/PRFactory.Web/UI/Layout/NotificationBell.razor`

**Modified Files**:
- `/src/PRFactory.Domain/Entities/ReviewComment.cs` - Add ParentCommentId, IsResolved, Reactions
- `/src/PRFactory.Infrastructure/Persistence/Configurations/ReviewCommentConfiguration.cs`
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add inline comment panel
- `/src/PRFactory.Web/Hubs/TicketHub.cs` - Broadcast comment events

#### Database Schema

```sql
-- Extend ReviewComment table
ALTER TABLE ReviewComments ADD COLUMN ParentCommentId UUID REFERENCES ReviewComments(Id);
ALTER TABLE ReviewComments ADD COLUMN IsResolved BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE ReviewComments ADD COLUMN ResolvedAt TIMESTAMP;
ALTER TABLE ReviewComments ADD COLUMN ResolvedByUserId UUID REFERENCES Users(Id);

-- New table for inline comment anchors
CREATE TABLE InlineCommentAnchors (
    Id UUID PRIMARY KEY,
    ReviewCommentId UUID NOT NULL REFERENCES ReviewComments(Id) ON DELETE CASCADE,
    StartLine INT NOT NULL,
    EndLine INT NOT NULL,
    TextSnippet TEXT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

-- New table for notifications
CREATE TABLE Notifications (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Type VARCHAR(50) NOT NULL, -- 'Mention', 'CommentReply', 'ReviewRequest', etc.
    TicketId UUID NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    CommentId UUID REFERENCES ReviewComments(Id) ON DELETE CASCADE,
    Message TEXT NOT NULL,
    IsRead BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_userid_isread ON Notifications(UserId, IsRead);
CREATE INDEX idx_inlinecommentanchors_commentid ON InlineCommentAnchors(ReviewCommentId);
```

#### Estimated Effort: 3 weeks

---

### Feature 5: Plan Version History & Diff Viewer

**Goal**: Track plan evolution through refinements with visual diffs.

#### Requirements

1. **Plan Version Storage**
   - Store every plan version (not just latest)
   - Version metadata: author (human/AI), timestamp, change reason
   - Link to parent version (refinement chain)
   - Store as markdown in git commits (leverage existing git history)

2. **Version History UI**
   - Timeline view showing all versions
   - Each version shows: timestamp, author, AI review score, change summary
   - Click version to view full plan at that point in time
   - Compare any two versions side-by-side

3. **Visual Diff Viewer**
   - Side-by-side markdown diff (before/after)
   - Syntax-highlighted diff (added=green, removed=red, changed=yellow)
   - Word-level diff for changed lines
   - Navigation: Jump to next/previous change
   - Toggle between unified/split view

4. **Audit Trail**
   - Track who approved/rejected each version
   - Track AI refinement attempts
   - Export history as PDF/HTML report

#### Acceptance Criteria

- [ ] PlanVersion entity with version number, author, parent link
- [ ] Git history used for plan storage (one commit per version)
- [ ] Version history API returns all versions with metadata
- [ ] UI: Timeline component showing version history
- [ ] UI: Diff viewer comparing two plan versions
- [ ] UI: Export audit trail as PDF
- [ ] Integration with existing TicketDiffViewer component
- [ ] Git service methods to retrieve historical commits

#### Files to Create/Modify

**New Files**:
- `/src/PRFactory.Domain/Entities/PlanVersion.cs`
- `/src/PRFactory.Infrastructure/Persistence/Configurations/PlanVersionConfiguration.cs`
- `/src/PRFactory.Core/Application/Services/IPlanVersionService.cs`
- `/src/PRFactory.Infrastructure/Application/PlanVersionService.cs`
- `/src/PRFactory.Web/UI/Display/VersionTimeline.razor`
- `/src/PRFactory.Web/UI/Display/PlanDiffViewer.razor`
- `/src/PRFactory.Web/Pages/Plans/History.razor`

**Modified Files**:
- `/src/PRFactory.Infrastructure/Git/LocalGitService.cs` - Add GetCommitHistory, GetFileAtCommit
- `/src/PRFactory.Infrastructure/Agents/GitPlanAgent.cs` - Create version commit
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add version history link
- `/src/PRFactory.Web/Components/Tickets/TicketDiffViewer.razor` - Enhance for plan diffs

#### Database Schema

```sql
CREATE TABLE PlanVersions (
    Id UUID PRIMARY KEY,
    TicketId UUID NOT NULL REFERENCES Tickets(Id) ON DELETE CASCADE,
    VersionNumber INT NOT NULL,
    GitCommitSha VARCHAR(40) NOT NULL,
    AuthorType VARCHAR(20) NOT NULL, -- 'Human', 'AI'
    AuthorUserId UUID REFERENCES Users(Id),
    ParentVersionId UUID REFERENCES PlanVersions(Id),
    ChangeReason TEXT,
    AIReviewScore INT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (TicketId, VersionNumber)
);

CREATE INDEX idx_planversions_ticketid ON PlanVersions(TicketId);
CREATE INDEX idx_planversions_parentid ON PlanVersions(ParentVersionId);
```

#### Estimated Effort: 2 weeks

---

### Feature 6: Review Checklist & Structured Criteria

**Goal**: Provide reviewers with clear, structured evaluation criteria.

#### Requirements

1. **Review Checklist Template**
   - Domain-specific checklist (web UI, API, database, etc.)
   - Checklist items map to plan quality criteria
   - Each item has: title, description, severity (required/recommended)
   - Reviewer checks off items as they review

2. **Checklist Categories**
   - **Completeness**: All sections present, sufficient detail
   - **Correctness**: Accurate file paths, method signatures, logic
   - **Testability**: Clear test cases, edge cases covered
   - **Security**: Auth/authz, input validation, secrets handling
   - **Performance**: Scalability, caching, database indexes
   - **Maintainability**: Code organization, documentation, logging

3. **Checklist UI**
   - Display checklist in review panel
   - Track completion percentage
   - Require all "required" items checked before approval
   - Optional items show warnings if unchecked
   - Comments can reference checklist items

4. **Custom Checklists**
   - Admins can create custom checklist templates
   - Per-repository or per-tenant customization
   - Version controlled in git (YAML files)

#### Acceptance Criteria

- [ ] ReviewChecklist entity with template reference
- [ ] ChecklistItem entity with status (checked/unchecked/n/a)
- [ ] Default checklist templates in `/config/checklists/`
- [ ] UI: Checklist panel in plan review
- [ ] UI: Completion percentage tracker
- [ ] Validation: Block approval if required items unchecked
- [ ] Admin UI: Create/edit custom checklists
- [ ] YAML schema for checklist templates

#### Files to Create/Modify

**New Files**:
- `/src/PRFactory.Domain/Entities/ReviewChecklist.cs`
- `/src/PRFactory.Domain/Entities/ChecklistItem.cs`
- `/src/PRFactory.Core/Application/Services/IChecklistTemplateService.cs`
- `/src/PRFactory.Infrastructure/Application/ChecklistTemplateService.cs`
- `/config/checklists/default_web.yaml`
- `/config/checklists/default_api.yaml`
- `/config/checklists/default_database.yaml`
- `/src/PRFactory.Web/UI/Checklists/ReviewChecklistPanel.razor`
- `/src/PRFactory.Web/UI/Checklists/ChecklistItemRow.razor`
- `/src/PRFactory.Web/Pages/Admin/Checklists.razor`

**Modified Files**:
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add checklist panel
- `/src/PRFactory.Infrastructure/Application/TicketApplicationService.cs` - Validate checklist before approval

#### Checklist Template YAML Example

```yaml
name: "Web UI Implementation Review"
domain: "web"
version: "1.0"
categories:
  - name: "Completeness"
    items:
      - title: "All UI components identified"
        description: "Plan lists all Blazor components to create/modify"
        severity: "required"
      - title: "Routing defined"
        description: "Page routes and navigation clearly specified"
        severity: "required"
      - title: "State management specified"
        description: "How component state will be managed"
        severity: "recommended"

  - name: "Correctness"
    items:
      - title: "File paths are accurate"
        description: "All referenced files exist in codebase or will be created"
        severity: "required"
      - title: "Dependencies available"
        description: "NuGet packages, services, models are available"
        severity: "required"

  - name: "Security"
    items:
      - title: "Authorization checks present"
        description: "Plan includes authorization logic for protected pages"
        severity: "required"
      - title: "Input validation specified"
        description: "Form validation and sanitization mentioned"
        severity: "required"
```

#### Estimated Effort: 2 weeks

---

## Implementation Phases

### Phase 1: Foundation (Weeks 1-3)
**Goal**: Enhanced prompts and AI review agent

- Feature 1: Enhanced Planning Prompts (1 week)
- Feature 2: AI Plan Review Agent (2 weeks)

**Deliverable**: Plans are auto-reviewed by AI before human review

---

### Phase 2: Rich Editing (Weeks 4-6)
**Goal**: Professional markdown editing experience

- Feature 3: Rich Markdown Editor Component (3 weeks)

**Deliverable**: Teams can edit plans in split-view editor with live preview

---

### Phase 3: Collaboration (Weeks 7-9)
**Goal**: Team-based plan refinement

- Feature 4: Inline Comments & Threaded Discussions (3 weeks)

**Deliverable**: Teams can discuss plans inline with @mentions and threading

---

### Phase 4: Transparency (Weeks 10-12)
**Goal**: Audit trail and structured reviews

- Feature 5: Plan Version History & Diff Viewer (2 weeks)
- Feature 6: Review Checklist & Structured Criteria (2 weeks)

**Deliverable**: Full version history with diffs and structured review criteria

---

## Testing Strategy

### Unit Tests

**Coverage Target**: 80% minimum

- PlanReviewAgent scoring algorithm
- Markdown editor toolbar actions
- Comment threading logic
- Version diff calculation
- Checklist validation rules

### Integration Tests

- PlanningGraph with AI review stage
- MarkdownEditor component rendering
- SignalR comment broadcasting
- Git history retrieval for versions

### Manual Testing Scenarios

1. **Plan Generation & Review**
   - Create ticket → Generate plan → AI reviews with score < 70 → Auto-refines → Score >= 70 → Human review

2. **Collaborative Editing**
   - Reviewer edits plan in markdown editor → Real-time preview updates → Saves changes → Other reviewers see updates via SignalR

3. **Inline Comments**
   - Reviewer selects text → Adds comment → Mentions teammate → Teammate receives notification → Replies to thread → Resolves thread

4. **Version History**
   - Plan refined 3 times → View version timeline → Compare v1 vs v3 → See word-level diffs → Export audit trail PDF

5. **Review Checklist**
   - Reviewer opens checklist → Checks off items → Tries to approve with required items unchecked → Validation blocks → Checks all required → Approval succeeds

---

## Rollout Plan

### Feature Flags

- `EnableAIPlanReview` - AI plan review agent (default: true)
- `EnableRichMarkdownEditor` - Rich editor vs plain textarea (default: true)
- `EnableInlineComments` - Inline comments and threading (default: true)
- `EnablePlanVersionHistory` - Version history tracking (default: true)
- `EnableReviewChecklists` - Structured review checklists (default: false - opt-in)

### Migration Strategy

1. **No Breaking Changes**: All features are additive
2. **Backward Compatibility**: Plain textarea still works if rich editor disabled
3. **Gradual Adoption**: Feature flags allow teams to opt-in per feature
4. **Data Migration**: None required (new tables only)

### Deployment Phases

1. **Alpha (Internal)**: Deploy to PRFactory team with all flags enabled
2. **Beta (Early Adopters)**: Deploy to 3-5 customer teams for feedback
3. **GA (General Availability)**: Roll out to all customers with flags enabled by default

---

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| **AI review agent generates false positives** | Medium | Medium | Tune scoring thresholds based on real data; allow bypass |
| **Rich editor performance issues on large plans** | High | Low | Debounce preview updates; lazy load for plans > 100KB |
| **Comment threading complexity** | Medium | Medium | Limit nesting to 3 levels; provide "flatten" view option |
| **Version history storage growth** | Medium | Medium | Use git commits (already compressed); prune old versions after 90 days |
| **Review checklist too rigid** | Low | Medium | Allow custom templates; make most items "recommended" not "required" |
| **SignalR connection stability** | High | Low | Existing infrastructure proven; use exponential backoff reconnect |

---

## Dependencies

### Internal Dependencies
- Existing PlanningGraph and agent infrastructure
- Existing multi-reviewer system (PlanReview entity)
- Existing SignalR TicketHub for real-time updates
- Existing Markdig markdown rendering

### External Dependencies
- None (uses approved libraries only per CLAUDE.md)

---

## Documentation Updates Required

- `/docs/ARCHITECTURE.md` - Add AI review agent architecture
- `/docs/WORKFLOW.md` - Update planning phase workflow diagram
- `/docs/IMPLEMENTATION_STATUS.md` - Mark features as implemented
- `/docs/USER_GUIDE.md` - Add markdown editor and collaboration features
- `/docs/ADMIN_GUIDE.md` - Add checklist template customization
- `/README.md` - Update feature list with planning UX improvements

---

## Success Criteria

### Must Have (MVP)
- ✅ AI plan review agent with quality scoring
- ✅ Rich markdown editor with live preview
- ✅ Inline comments with @mentions
- ✅ Version history with basic diff viewer

### Should Have (Full Epic)
- ✅ Threaded comment discussions
- ✅ Review checklist with templates
- ✅ Comment reactions and resolution
- ✅ Enhanced planning prompts

### Nice to Have (Future)
- Collaborative real-time editing (Google Docs-style)
- Plan templates library (common patterns)
- AI suggestions during editing ("Did you consider...?")
- Export plans as Confluence/Notion pages

---

## Open Questions

1. **AI Review Threshold**: Should score threshold be configurable per tenant or global?
   - **Recommendation**: Configurable per tenant (default: 70)

2. **Max Inline Comments**: Should we limit comments per plan to avoid clutter?
   - **Recommendation**: No hard limit, but UI groups by section

3. **Email Notifications**: Should @mentions always send email or make it optional?
   - **Recommendation**: User preference setting (default: in-app only)

4. **Version Retention**: How long to keep plan version history?
   - **Recommendation**: Keep all versions (git storage is cheap); allow manual cleanup

5. **Checklist Enforcement**: Should required checklist items block approval?
   - **Recommendation**: Yes for "required" items, warnings for "recommended"

---

## Appendix A: UI Mockups

### Markdown Editor Split View

```
┌────────────────────────────────────────────────────────────────┐
│ IMPLEMENTATION_PLAN.md                          [Split] [▢] [×]│
├────────────────────────────────────────────────────────────────┤
│ [B][I][H1][▼]  [··] [•·] [☑] [🔗] [📷] [⊞]    [←][→] [⊗]    │
├─────────────────────────────┬──────────────────────────────────┤
│ Editor                      │ Preview                          │
├─────────────────────────────┼──────────────────────────────────┤
│  1  ## Overview             │  Overview                        │
│  2                          │  ─────────                       │
│  3  This plan implements... │  This plan implements...         │
│  4                          │                                  │
│  5  ### Objectives          │  Objectives                      │
│  6                          │  • Add user authentication       │
│  7  - Add user auth         │  • Implement JWT tokens          │
│  8  - Implement JWT tokens  │                                  │
│  9                          │                                  │
│ 10  ## Implementation       │  Implementation Steps            │
│ 11                          │  ─────────────────               │
│ 12  **Step 1**: Create...   │  Step 1: Create AuthController  │
│ 13                          │                                  │
│ 14  File: `src/Auth...`     │  File: src/AuthController.cs     │
│                             │                                  │
└─────────────────────────────┴──────────────────────────────────┘
```

### Inline Comment Panel

```
┌────────────────────────────────────────────────────────────────┐
│ Plan Preview                             Comments (3) [+] [▼]  │
├──────────────────────────────────────────┬─────────────────────┤
│ Implementation Steps                     │ 💬 Line 12-15       │
│ ─────────────────────                    │ @john: Should we... │
│                                          │ ↳ @jane: Good point │
│ Step 1: Create AuthController            │   [Resolve]         │
│                                          │                     │
│ File: src/AuthController.cs              │ 💬 Line 24 ⚠️      │
│                                          │ @mike: Missing...   │
│ Add the following methods:               │ [Reply] [Resolve]   │
│ • Login(username, password)              │                     │
│ • Logout()                               │                     │
│ • RefreshToken(token) ← 📌 Highlighted   │                     │
│                                          │                     │
└──────────────────────────────────────────┴─────────────────────┘
```

### Review Checklist

```
┌────────────────────────────────────────────────────────────────┐
│ Review Checklist (Web UI Implementation)        [80% Complete] │
├────────────────────────────────────────────────────────────────┤
│ ✅ Completeness (4/4)                                          │
│    ✅ All UI components identified                            │
│    ✅ Routing defined                                         │
│    ✅ State management specified                              │
│    ✅ Props and events documented                             │
│                                                                │
│ ⚠️  Correctness (2/3)                                          │
│    ✅ File paths are accurate                                 │
│    ✅ Dependencies available                                  │
│    ❌ Method signatures match existing code      [Required]   │
│                                                                │
│ ✅ Security (2/2)                                              │
│    ✅ Authorization checks present                            │
│    ✅ Input validation specified                              │
│                                                                │
│ [ Approve Plan ] (disabled - complete required items first)   │
└────────────────────────────────────────────────────────────────┘
```

### Version History Timeline

```
┌────────────────────────────────────────────────────────────────┐
│ Plan Version History                               [Export PDF]│
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ●═════●═════●═════●═════● (v4 - current)                    │
│  v1    v2    v3    v4                                         │
│                                                                │
│  ┌────────────────────────────────────┐                       │
│  │ v4 - Current (Approved)            │                       │
│  │ 2025-11-14 10:30 AM               │                       │
│  │ Author: @jane (Human)              │                       │
│  │ AI Score: 88/100                   │                       │
│  │ • Added error handling details     │                       │
│  │ [View] [Compare with v3]           │                       │
│  └────────────────────────────────────┘                       │
│                                                                │
│  ┌────────────────────────────────────┐                       │
│  │ v3 - Refined                       │                       │
│  │ 2025-11-14 9:45 AM                │                       │
│  │ Author: AI (Auto-refinement)       │                       │
│  │ AI Score: 75/100                   │                       │
│  │ • Improved test coverage section   │                       │
│  │ [View] [Compare with v4]           │                       │
│  └────────────────────────────────────┘                       │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## Appendix B: API Specifications

### PlanReviewAgent API

```csharp
public interface IPlanReviewAgent
{
    /// <summary>
    /// Reviews a generated plan and returns quality score with feedback
    /// </summary>
    Task<PlanReviewResult> ReviewPlanAsync(
        string planMarkdown,
        TicketContext context,
        CancellationToken cancellationToken = default);
}

public class PlanReviewResult
{
    public int OverallScore { get; set; }          // 0-100
    public int CompletenessScore { get; set; }     // 0-25
    public int SpecificityScore { get; set; }      // 0-25
    public int TestabilityScore { get; set; }      // 0-20
    public int SafetyScore { get; set; }           // 0-15
    public int DependencyScore { get; set; }       // 0-15
    public string Feedback { get; set; }           // Markdown formatted
    public List<string> SpecificIssues { get; set; }
    public List<string> Suggestions { get; set; }
}
```

### MarkdownEditor Component API

```razor
<MarkdownEditor @bind-Value="@planContent"
                Height="600px"
                ShowToolbar="true"
                ShowPreview="true"
                SplitViewMode="SideBySide"
                OnSave="HandleSave"
                OnCancel="HandleCancel">
</MarkdownEditor>

@code {
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public EventCallback OnSave { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public string Height { get; set; } = "400px";

    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    [Parameter]
    public bool ShowPreview { get; set; } = true;

    [Parameter]
    public SplitViewMode SplitViewMode { get; set; } = SplitViewMode.SideBySide;
}

public enum SplitViewMode
{
    SideBySide,   // Editor left, preview right
    EditorOnly,   // Editor only (no preview)
    PreviewOnly,  // Preview only (read-only)
    Tabs          // Tabbed (mobile-friendly)
}
```

### Plan Version Service API

```csharp
public interface IPlanVersionService
{
    Task<PlanVersion> CreateVersionAsync(
        Guid ticketId,
        string commitSha,
        AuthorType authorType,
        Guid? authorUserId,
        Guid? parentVersionId,
        string? changeReason,
        int? aiReviewScore,
        CancellationToken cancellationToken = default);

    Task<List<PlanVersionDto>> GetVersionHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<PlanDiffDto> ComparePlansAsync(
        Guid ticketId,
        int fromVersion,
        int toVersion,
        CancellationToken cancellationToken = default);
}

public class PlanDiffDto
{
    public int FromVersion { get; set; }
    public int ToVersion { get; set; }
    public List<DiffHunk> Hunks { get; set; }
}

public class DiffHunk
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public DiffType Type { get; set; }  // Added, Removed, Modified
    public string OldText { get; set; }
    public string NewText { get; set; }
}
```

---

**End of EPIC 07: Planning Phase UX & Collaboration Improvements**
