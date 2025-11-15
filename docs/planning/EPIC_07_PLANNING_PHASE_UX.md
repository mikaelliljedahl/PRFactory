# EPIC 07: Planning Phase UX & Collaboration Improvements

> **Status**: ✅ **COMPLETE** (November 14, 2025)
> **Completion Date**: 2025-11-14
> **PR**: #75 (Commit: d909878)
> **Priority**: High
> **Actual Effort**: 5 weeks
> **Dependencies**: Epic 1 (Team Review & Collaboration) - provides foundation for AI validation, notifications, and revision history
> **Target Audience**: Development teams using PRFactory for collaborative planning
> **Related Epics**: Epic 1 (Team Review), Epic 5 (Agent Framework), Epic 6 (Admin UI)

## ✅ Implementation Complete

All 4 phases have been successfully implemented:
- ✅ **Phase 1**: Enhanced Planning Prompts with domain-specific templates
- ✅ **Phase 2**: Rich Markdown Editor with live preview and formatting toolbar
- ✅ **Phase 3**: Inline Comment Anchoring for contextual discussions
- ✅ **Phase 4**: Review Checklists with YAML-based templates

**See**: [IMPLEMENTATION_STATUS.md - Epic 07](../IMPLEMENTATION_STATUS.md#epic-07-planning-phase-ux--collaboration-improvements-november-2025) for full details.

---

## Executive Summary

Enhance the planning phase UX with rich markdown editing, inline commenting, and structured review checklists to provide a professional, Notion-like experience for collaborative plan refinement.

**Current State** (as of 2025-11-14):
- ✅ Multi-reviewer approval system implemented (required/optional reviewers)
- ✅ Basic comment support with markdown rendering
- ✅ PlanningGraph with checkpoint/resume
- ✅ Side-by-side diff viewer
- ⚠️ Plain textarea editing (limited UX)
- ❌ No rich markdown editor with live preview
- ❌ No inline comment anchoring
- ❌ No review checklists

**Future State**:
Teams can collaboratively edit plans in a rich split-view markdown editor with formatting toolbar, anchor comments to specific lines, follow structured review checklists, and benefit from AI-powered quality validation (Epic 1).

**Note on Epic 1 Overlap**: Many features originally scoped in early EPIC 07 drafts are now being implemented in **Epic 1 (Team Review & Collaboration)**:
- AI Plan Validation Service (Epic 1)
- Notification System for @mentions and assignments (Epic 1)
- Plan Revision History with diff comparison (Epic 1)

EPIC 07 focuses specifically on **UX improvements** for the planning phase editor and review experience.

---

## Business Value

### Problems Being Solved

1. **Poor Editing Experience**: Plain textareas make it hard to edit large markdown documents
   - No formatting toolbar (users must know markdown syntax)
   - No live preview (must toggle between edit and view)
   - No syntax highlighting for code blocks

2. **Inefficient Inline Discussions**: Comments apply to entire plan, not specific sections
   - Can't anchor comments to specific lines or paragraphs
   - Difficult to discuss specific implementation details
   - Reviewers must quote text manually

3. **Unstructured Reviews**: Reviewers lack clear criteria for plan evaluation
   - No checklist of what to verify
   - Inconsistent review quality across reviewers
   - Missing items not caught until implementation

4. **Poor Codebase Context**: Planning prompts don't provide enough architectural guidance
   - Generated plans don't follow project patterns
   - Missing domain-specific considerations (web/API/database)
   - Repetitive refinement requests

### Expected Benefits

- **Better User Experience**: Professional markdown editor comparable to Notion/Confluence
- **Faster Reviews**: Inline comments with line anchors reduce back-and-forth
- **Consistent Quality**: Review checklists ensure completeness
- **Higher Plan Quality**: Enhanced prompts with architectural context reduce refinements
- **User Satisfaction**: Modern editing experience reduces friction

### Success Metrics

- User satisfaction score > 4.0/5.0 for editing experience
- Average characters per plan edit session increases by 50% (easier to make larger edits)
- Review comment specificity improves (% of comments with line anchors > 60%)
- First-time plan acceptance rate increases by 20%

---

## Current Implementation Status

### What Exists Today (✅ Implemented)

From exploration of the codebase (2025-11-14):

**Review System Core** (35 files, ~1,765 LOC in UI components):
- `PlanReview` entity - Multi-reviewer support (required/optional)
- `ReviewComment` entity - Markdown comments with @mention field
- `ReviewStatus` enum - Pending, Approved, RejectedForRefinement, RejectedForRegeneration
- `PlanReviewService` - Assign reviewers, add comments, approval logic
- `Ticket.HasSufficientApprovals()` - Domain logic for approval gates

**UI Components**:
- `PlanReviewSection.razor` - Main review container with approve/refine/regenerate actions
- `PlanReviewStatus.razor` - Shows reviewer progress (required/optional split)
- `ReviewerAssignment.razor` - Multi-reviewer assignment UI
- `ReviewCommentThread.razor` - Linear comment list with markdown rendering (Markdig)
- `TicketUpdateEditor.razor` - ⚠️ Plain textarea (8 rows) with validation
- `TicketDiffViewer.razor` - Side-by-side original vs updated comparison

**Agent Infrastructure** (from Epic 5):
- CLI-based agent architecture (LLM-agnostic)
- 5 graphs: RefinementGraph, PlanningGraph, ImplementationGraph, CodeReviewGraph, WorkflowOrchestrator
- Checkpoint/resume system across all phases
- 20+ specialized agents

**Markdown Support**:
- Markdig library (v0.40.0) with advanced extensions
- Rendering in comments, ticket descriptions, plan content
- No WYSIWYG or live preview editing

### What's Missing (❌ Not Implemented)

**Epic 7 Scope**:
- Rich Markdown Editor with split-view and formatting toolbar
- Inline comment anchoring to specific line ranges
- Review checklists with structured criteria
- Enhanced planning prompts with domain-specific templates

**Epic 1 Scope** (separate epic, in planning):
- AI Plan Validation Service
- Notification system for @mentions
- Plan revision history with timeline
- Threaded comment discussions (parent-child relationships)

### Architecture Changes Since Initial Draft

**Original EPIC 07 (early draft)**: Assumed custom agent framework with explicit tool system

**Current Reality (as of Epic 5 implementation)**:
- **CLI-based agents**: Simpler, LLM-agnostic architecture
- **Prompt-based execution**: Agents receive comprehensive prompts with all context
- **Markdown output**: Plans generated as Markdown (not JSON)
- **Cross-provider workflows**: Can use GPT-4, Claude, or other LLMs interchangeably

**Impact on EPIC 07**:
- PlanReviewAgent (AI validation) moved to Epic 1
- Focus shifts to UX/UI improvements vs agent infrastructure
- Prompts are text files in `/prompts/` directory, not C# classes

---

## User Stories

### Epic-Level User Stories

1. **As a developer**, I want to edit plans in a split-view markdown editor with live preview, so I can see formatted output as I type without toggling views.

2. **As a reviewer**, I want to anchor comments to specific lines in the plan, so discussions stay contextual and I don't have to quote text manually.

3. **As a reviewer**, I want a structured checklist when reviewing plans (completeness, security, testability), so I consistently evaluate all important criteria.

4. **As a team lead**, I want planning prompts to include our architectural patterns and domain-specific guidance, so generated plans follow our standards from the start.

5. **As a developer**, I want a formatting toolbar in the editor (bold, headings, lists, code blocks), so I don't have to remember markdown syntax.

---

## Features (Updated Scope)

### Feature 1: Enhanced Planning Prompts ✨ NEW

**Goal**: Improve plan generation quality through better prompt engineering with domain-specific templates and architectural context.

**Current State**:
- Prompts in `/prompts/plan/anthropic/` directory
- Single system and user template
- Generic context (file list, ticket description)

**Proposed Enhancement**:
- Domain-specific prompt templates:
  - `web_ui.txt` - Blazor component guidance, routing, state management
  - `rest_api.txt` - Controller patterns, DTO mapping, error handling
  - `background_jobs.txt` - Hangfire patterns, retry logic, idempotency
  - `database.txt` - Migration patterns, EF configuration, indexing strategies
- Codebase architectural patterns (from `/docs/ARCHITECTURE.md`)
- Technology stack with versions
- Code style guidelines (from `.editorconfig`)
- Example code snippets from similar features

**Acceptance Criteria**:
- [ ] Domain-specific prompt templates in `/prompts/plan/anthropic/domains/`
- [ ] TicketType field drives template selection (Web UI, REST API, Background Job, Database)
- [ ] System prompt includes quality criteria checklist
- [ ] User prompt includes 3+ relevant code snippets
- [ ] Generated plans reference actual project patterns

**Files to Create**:
- `/prompts/plan/anthropic/domains/web_ui.txt`
- `/prompts/plan/anthropic/domains/rest_api.txt`
- `/prompts/plan/anthropic/domains/background_jobs.txt`
- `/prompts/plan/anthropic/domains/database.txt`
- `/prompts/plan/anthropic/domains/refactoring.txt`

**Files to Modify**:
- `/src/PRFactory.Infrastructure/Agents/PlanningAgent.cs` - Load domain templates based on ticket type

**Estimated Effort**: 1 week

---

### Feature 2: Rich Markdown Editor Component ✨ NEW

**Goal**: Provide professional split-view markdown editing with live preview, formatting toolbar, and keyboard shortcuts.

**Current State**:
- `TicketUpdateEditor.razor` - Plain `<textarea>` with 8 rows
- No toolbar, no preview, no shortcuts
- Markdown rendering only after save

**Proposed Component**: `MarkdownEditor.razor`

**Features**:
- **Split-view layout**: Editor (left) + Live Preview (right)
- **View modes**: Split, Editor-only, Preview-only, Fullscreen
- **Formatting toolbar**:
  - Text: Bold, Italic, Strikethrough
  - Structure: H1-H6, Blockquote, Horizontal Rule
  - Lists: Unordered, Ordered, Checklist
  - Insert: Link, Image, Table, Code Block
  - Actions: Undo, Redo, Fullscreen
- **Keyboard shortcuts**: Ctrl+B (bold), Ctrl+I (italic), Ctrl+K (link), etc.
- **Live preview**: Debounced 300ms, uses Markdig with advanced extensions
- **Responsive**: Desktop (side-by-side), Tablet (tabs), Mobile (editor-only with preview button)
- **Line numbers** in editor pane
- **Scroll sync** between editor and preview

**Technology**:
- Pure Blazor Server (no JavaScript required per CLAUDE.md)
- `<textarea>` with `@oninput` for live updates
- `@bind` for two-way binding
- Radzen components for toolbar buttons
- CSS Grid for split layout
- Markdig for preview rendering

**Acceptance Criteria**:
- [ ] MarkdownEditor.razor component in `/src/PRFactory.Web/UI/Editors/`
- [ ] Formatting toolbar with 15+ buttons
- [ ] Split-view with live preview (300ms debounce)
- [ ] Keyboard shortcuts (Ctrl+B, Ctrl+I, Ctrl+K, etc.)
- [ ] View mode toggle (split/editor/preview/fullscreen)
- [ ] Responsive layout (desktop/tablet/mobile)
- [ ] Line numbers in editor
- [ ] Scroll synchronization between panes
- [ ] Unit tests for toolbar actions
- [ ] Integration with TicketUpdateEditor and PlanReviewSection

**Files to Create**:
- `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor`
- `/src/PRFactory.Web/UI/Editors/MarkdownEditor.razor.cs`
- `/src/PRFactory.Web/UI/Editors/MarkdownToolbar.razor`
- `/src/PRFactory.Web/UI/Editors/MarkdownPreview.razor`
- `/src/PRFactory.Web/wwwroot/css/markdown-editor.css`
- `/tests/PRFactory.Web.Tests/UI/Editors/MarkdownEditorTests.cs`

**Files to Modify**:
- `/src/PRFactory.Web/Components/Tickets/TicketUpdateEditor.razor` - Replace textarea with MarkdownEditor
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Use MarkdownEditor for plan editing
- `/src/PRFactory.Web/Components/Tickets/ReviewCommentThread.razor` - Use MarkdownEditor for comment replies

**Estimated Effort**: 3 weeks

---

### Feature 3: Inline Comment Anchoring ✨ NEW (Partial overlap with Epic 1)

**Goal**: Enable reviewers to anchor comments to specific lines or text selections in plans.

**Current State**:
- `ReviewComment` entity - comments apply to entire ticket
- No line number or text range fields
- Reviewers must manually quote context

**Proposed Enhancement**:

**Domain Model**:
```csharp
// New entity
public class InlineCommentAnchor
{
    public Guid Id { get; private set; }
    public Guid ReviewCommentId { get; private set; }
    public int StartLine { get; private set; }
    public int EndLine { get; private set; }
    public string TextSnippet { get; private set; }  // First 100 chars for display
}
```

**UI Flow**:
1. User selects text in markdown preview pane
2. "Add Comment" button appears next to selection
3. Click opens comment editor with anchor metadata
4. Comment saved with line range
5. Anchored comments displayed in right sidebar aligned with text

**Features**:
- Text selection detection in preview pane
- Floating "Add Comment" button on selection
- Inline comment indicators (icons/highlights) in preview
- Right sidebar with anchored comment threads
- Click comment to scroll to anchor location
- Filter: Show All / Show Unresolved

**Acceptance Criteria**:
- [ ] InlineCommentAnchor entity created
- [ ] Text selection handler in MarkdownPreview component
- [ ] Comment editor accepts line range metadata
- [ ] Right sidebar component (InlineCommentPanel.razor)
- [ ] Visual indicators for anchored comments in preview
- [ ] Click-to-scroll navigation
- [ ] Filter controls for resolved/unresolved

**Files to Create**:
- `/src/PRFactory.Domain/Entities/InlineCommentAnchor.cs`
- `/src/PRFactory.Domain/Interfaces/IInlineCommentAnchorRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Repositories/InlineCommentAnchorRepository.cs`
- `/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor`
- `/src/PRFactory.Web/UI/Comments/InlineCommentPanel.razor.cs`
- `/src/PRFactory.Web/UI/Comments/CommentAnchorIndicator.razor`

**Files to Modify**:
- `/src/PRFactory.Domain/Entities/ReviewComment.cs` - Add navigation to InlineCommentAnchor
- `/src/PRFactory.Infrastructure/Application/PlanReviewService.cs` - Handle anchor metadata
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add inline comment panel

**Database Migration**:
```sql
CREATE TABLE InlineCommentAnchors (
    Id UUID PRIMARY KEY,
    ReviewCommentId UUID NOT NULL REFERENCES ReviewComments(Id) ON DELETE CASCADE,
    StartLine INT NOT NULL,
    EndLine INT NOT NULL,
    TextSnippet VARCHAR(200) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_InlineCommentAnchors_ReviewCommentId
    ON InlineCommentAnchors(ReviewCommentId);
```

**Note**: Comment threading (parent-child relationships) and @mention notifications are in **Epic 1** scope.

**Estimated Effort**: 2 weeks

---

### Feature 4: Review Checklist Templates ✨ NEW

**Goal**: Provide reviewers with structured, domain-specific checklists to ensure consistent plan evaluation.

**Current State**:
- No structured review criteria
- Reviewers use ad-hoc evaluation
- Inconsistent review quality

**Proposed Enhancement**:

**Checklist Template System**:
- YAML-based templates in `/config/checklists/`
- Domain-specific checklists (web_ui, rest_api, database, background_jobs)
- Configurable per repository or per tenant
- Items marked as "required" or "recommended"
- Checklist completion tracked in PlanReview entity

**Example Template** (`web_ui.yaml`):
```yaml
name: "Web UI Implementation Review"
domain: "web_ui"
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

  - name: "Security"
    items:
      - title: "Authorization checks present"
        description: "Plan includes authorization logic for protected pages"
        severity: "required"
      - title: "Input validation specified"
        description: "Form validation and sanitization mentioned"
        severity: "required"
```

**Domain Model**:
```csharp
public class ReviewChecklist
{
    public Guid Id { get; private set; }
    public Guid PlanReviewId { get; private set; }
    public string TemplateName { get; private set; }
    public List<ChecklistItem> Items { get; private set; }

    public int CompletionPercentage =>
        Items.Any() ? (Items.Count(i => i.IsChecked) * 100) / Items.Count : 0;
}

public class ChecklistItem
{
    public Guid Id { get; private set; }
    public string Category { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Severity { get; private set; }  // "required" or "recommended"
    public bool IsChecked { get; private set; }
    public DateTime? CheckedAt { get; private set; }
}
```

**UI Features**:
- Checklist panel in review page
- Collapsible categories
- Progress indicator (% complete)
- Validation: Block approval if required items unchecked
- Warning: Show alerts if recommended items unchecked

**Acceptance Criteria**:
- [ ] ReviewChecklist and ChecklistItem entities
- [ ] YAML template loader service
- [ ] Default templates for web/API/database/jobs domains
- [ ] ReviewChecklistPanel.razor component
- [ ] Progress tracking (% complete)
- [ ] Approval validation (required items must be checked)
- [ ] Admin UI to create/edit custom templates (optional)

**Files to Create**:
- `/config/checklists/web_ui.yaml`
- `/config/checklists/rest_api.yaml`
- `/config/checklists/database.yaml`
- `/config/checklists/background_jobs.yaml`
- `/src/PRFactory.Domain/Entities/ReviewChecklist.cs`
- `/src/PRFactory.Domain/Entities/ChecklistItem.cs`
- `/src/PRFactory.Core/Application/Services/IChecklistTemplateService.cs`
- `/src/PRFactory.Infrastructure/Application/ChecklistTemplateService.cs`
- `/src/PRFactory.Web/UI/Checklists/ReviewChecklistPanel.razor`
- `/src/PRFactory.Web/UI/Checklists/ReviewChecklistPanel.razor.cs`
- `/src/PRFactory.Web/UI/Checklists/ChecklistItemRow.razor`

**Files to Modify**:
- `/src/PRFactory.Domain/Entities/PlanReview.cs` - Add ReviewChecklist navigation
- `/src/PRFactory.Infrastructure/Application/PlanReviewService.cs` - Validate checklist before approval
- `/src/PRFactory.Web/Components/Tickets/PlanReviewSection.razor` - Add checklist panel

**Database Migration**:
```sql
CREATE TABLE ReviewChecklists (
    Id UUID PRIMARY KEY,
    PlanReviewId UUID NOT NULL REFERENCES PlanReviews(Id) ON DELETE CASCADE,
    TemplateName VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE ChecklistItems (
    Id UUID PRIMARY KEY,
    ReviewChecklistId UUID NOT NULL REFERENCES ReviewChecklists(Id) ON DELETE CASCADE,
    Category VARCHAR(100) NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    Severity VARCHAR(20) NOT NULL,
    IsChecked BOOLEAN NOT NULL DEFAULT FALSE,
    CheckedAt TIMESTAMP,
    SortOrder INT NOT NULL DEFAULT 0
);

CREATE INDEX IX_ReviewChecklists_PlanReviewId
    ON ReviewChecklists(PlanReviewId);
CREATE INDEX IX_ChecklistItems_ReviewChecklistId
    ON ChecklistItems(ReviewChecklistId);
```

**Estimated Effort**: 2 weeks

---

## Implementation Phases

### Phase 1: Enhanced Prompts (Week 1)
**Goal**: Improve plan quality through better prompt engineering

- Feature 1: Enhanced Planning Prompts
- Domain-specific templates
- Architectural pattern injection
- Code snippet examples

**Deliverable**: Plans reference actual project patterns and generate with higher quality

---

### Phase 2: Rich Editor Foundation (Weeks 2-4)
**Goal**: Professional markdown editing experience

- Feature 2: Rich Markdown Editor Component
  - Week 2: Core split-view editor with basic toolbar
  - Week 3: Live preview, keyboard shortcuts, responsive design
  - Week 4: Polish, scroll sync, integration, testing

**Deliverable**: Teams can edit plans in split-view editor with live preview and formatting toolbar

---

### Phase 3: Inline Collaboration (Weeks 5-6)
**Goal**: Contextual discussions on specific plan sections

- Feature 3: Inline Comment Anchoring
  - Week 5: InlineCommentAnchor entity, text selection handler, comment editor
  - Week 6: Inline comment panel, visual indicators, navigation

**Deliverable**: Reviewers can anchor comments to specific lines with visual indicators

---

### Phase 4: Structured Reviews (Weeks 7)
**Goal**: Consistent plan evaluation

- Feature 4: Review Checklist Templates
  - YAML template system
  - Checklist UI component
  - Approval validation

**Deliverable**: Reviewers follow structured checklists with progress tracking

---

## Epic 1 (Team Review) Features (Reference)

These features are **NOT in EPIC 07 scope** - they are part of Epic 1:

### From `/docs/planning/team_review/`:

1. **Plan Validation Service** (P1, 3-5 days)
   - AI-powered plan validation (security, completeness, performance)
   - Quality scoring (0-100)
   - Auto-refinement loop

2. **Notification System** (P1, 3-4 days)
   - In-app notifications
   - @mention alerts
   - Reviewer assignment notifications
   - Plan approval/rejection notifications

3. **Plan Revision History** (P2, 3-4 days)
   - PlanRevision entity with version tracking
   - Timeline view
   - Side-by-side diff viewer
   - Compare arbitrary versions

### Epic 1 Status (as of 2025-11-14):
- Status: 🟡 Partially Implemented
- Core infrastructure exists (PlanReview, ReviewComment entities)
- ✅ Application services implemented
- ✅ Blazor components exist
- ⚠️ Features need implementation (validation, notifications, history)

---

## Testing Strategy

### Unit Tests

**Coverage Target**: 80% minimum (per CLAUDE.md)

- Enhanced prompt template loading
- MarkdownEditor toolbar actions
- Inline comment anchor creation
- Checklist validation logic
- Line range parsing

### Integration Tests

- MarkdownEditor component rendering
- Inline comment panel with anchored comments
- Checklist progress tracking
- Domain template selection based on ticket type

### Manual Testing Scenarios

1. **Rich Editor Experience**
   - User edits plan in split view → Live preview updates → Formatting toolbar works → Keyboard shortcuts work → Fullscreen mode toggles

2. **Inline Comments**
   - User selects text → Clicks "Add Comment" → Comment anchored to line range → Visual indicator appears → Click scrolls to anchor

3. **Review Checklist**
   - Reviewer opens checklist → Checks items → Progress updates → Tries to approve with required items unchecked → Validation blocks → Checks all required → Approval succeeds

4. **Enhanced Prompts**
   - Create Web UI ticket → PlanningAgent loads web_ui template → Generated plan references Blazor patterns → Includes code-behind guidance

---

## Rollout Plan

### Feature Flags

- `EnableRichMarkdownEditor` - Split-view editor vs plain textarea (default: true)
- `EnableInlineCommentAnchors` - Inline anchoring vs full-ticket comments (default: true)
- `EnableReviewChecklists` - Structured checklists (default: false - opt-in initially)
- `EnableEnhancedPlanningPrompts` - Domain-specific prompts (default: true)

### Migration Strategy

1. **No Breaking Changes**: All features are additive
2. **Backward Compatibility**: Plain textarea still works if rich editor disabled
3. **Gradual Adoption**: Feature flags allow teams to opt-in per feature
4. **Data Migration**:
   - ReviewComment table: No changes (InlineCommentAnchor is separate table)
   - New tables only: InlineCommentAnchors, ReviewChecklists, ChecklistItems

### Deployment Phases

1. **Alpha (Internal)**: Deploy to PRFactory team with all flags enabled
2. **Beta (Early Adopters)**: Deploy to 3-5 customer teams for feedback
3. **GA (General Availability)**: Roll out to all customers with flags enabled by default

---

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| **Rich editor performance on large plans** | High | Low | Debounce preview updates (300ms); lazy load for plans > 100KB; virtualization for very long documents |
| **Inline comment positioning accuracy** | Medium | Medium | Store both line range AND text snippet for anchor verification; graceful degradation if text changed |
| **Checklist rigidity** | Low | Medium | Make most items "recommended" not "required"; allow custom templates; provide "skip checklist" option for admins |
| **Browser compatibility** | Medium | Low | Test on Chrome, Firefox, Safari, Edge; use standard CSS Grid (widely supported); graceful fallback for older browsers |
| **Prompt template maintenance** | Medium | Medium | Version control templates; document template variables; provide template validation in admin UI |
| **User adoption resistance** | Medium | Medium | Provide tutorials/onboarding; maintain plain textarea fallback; gather user feedback early |

---

## Dependencies

### Internal Dependencies

- ✅ Epic 1 (Team Review) - Provides AI validation, notifications, revision history
- ✅ Epic 5 (Agent Framework) - CLI-based agents, checkpoint/resume, PlanningGraph
- ✅ Epic 6 (Admin UI) - UI component library structure, Blazor patterns
- ✅ Existing multi-reviewer system (PlanReview entity)
- ✅ Existing Markdig markdown rendering
- ✅ Existing PlanReviewService and TicketService

### External Dependencies

- ✅ Blazor Server (existing)
- ✅ Radzen.Blazor v5.9.0 (existing)
- ✅ Bootstrap 5 (existing)
- ✅ Markdig v0.40.0 (existing)
- **No new UI libraries required** (per CLAUDE.md restrictions)

---

## Documentation Updates Required

After EPIC 07 implementation:

- `/docs/ARCHITECTURE.md` - Add rich editor component architecture
- `/docs/WORKFLOW.md` - Update planning phase workflow with new UX
- `/docs/IMPLEMENTATION_STATUS.md` - Mark EPIC 07 features as implemented
- `/docs/USER_GUIDE.md` - Add markdown editor and inline comment guides
- `/docs/ADMIN_GUIDE.md` - Add checklist template customization
- `/README.md` - Update feature list with planning UX improvements

---

## Success Criteria

### Must Have (MVP)

- ✅ Enhanced planning prompts with domain templates
- ✅ Rich markdown editor with live preview and toolbar
- ✅ Inline comment anchoring with line ranges
- ✅ Review checklists with YAML templates

### Should Have (Full Epic)

- ✅ Keyboard shortcuts in editor
- ✅ Fullscreen mode
- ✅ Scroll synchronization
- ✅ Responsive design (desktop/tablet/mobile)
- ✅ Visual indicators for anchored comments
- ✅ Checklist progress tracking

### Nice to Have (Future)

- Real-time collaborative editing (Google Docs-style with CRDT)
- Plan template library (common patterns for new projects)
- AI suggestions during editing ("Did you consider error handling?")
- Export plans as Confluence/Notion pages
- Rich diff viewer with syntax highlighting (beyond current TicketDiffViewer)

---

## Open Questions

1. **Markdown Editor Library**: Should we use a third-party library (Monaco, CodeMirror) or build pure Blazor?
   - **Recommendation**: Start with pure Blazor (per CLAUDE.md), evaluate third-party later if performance issues

2. **Inline Comment Limits**: Should we limit comments per plan to avoid clutter?
   - **Recommendation**: No hard limit, but UI groups by section and supports collapse/expand

3. **Checklist Customization**: Should custom checklists be repository-level or tenant-level?
   - **Recommendation**: Both - tenant default, per-repository override

4. **Prompt Template Versioning**: How to handle template updates without breaking existing plans?
   - **Recommendation**: Version templates (v1, v2), store template version used in Ticket entity

5. **Rich Editor vs Plain Textarea**: Provide toggle or force migration?
   - **Recommendation**: Feature flag with toggle in user preferences, default to rich editor

---

## Related Documentation

- **Epic 1 Overview**: [/docs/planning/team_review/README.md](/home/user/PRFactory/docs/planning/team_review/README.md)
- **Epic 5 Agent Framework**: [/docs/planning/epic_05_agent_framework/README.md](/home/user/PRFactory/docs/planning/epic_05_agent_framework/README.md)
- **Epic 6 Admin UI**: [/docs/planning/epic_06_admin_ui/README.md](/home/user/PRFactory/docs/planning/epic_06_admin_ui/README.md)
- **Current Implementation Status**: [/docs/IMPLEMENTATION_STATUS.md](/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md)
- **Architecture Guidelines**: [/CLAUDE.md](/home/user/PRFactory/CLAUDE.md)
- **Roadmap**: [/docs/ROADMAP.md](/home/user/PRFactory/docs/ROADMAP.md)

---

## Appendix A: Component Hierarchy

### Rich Markdown Editor Component Tree

```
MarkdownEditor.razor (root component)
├── MarkdownToolbar.razor (formatting buttons)
│   ├── ButtonGroup: Text Formatting (Bold, Italic, Strikethrough)
│   ├── ButtonGroup: Structure (H1-H6, Blockquote, HR)
│   ├── ButtonGroup: Lists (UL, OL, Checklist)
│   ├── ButtonGroup: Insert (Link, Image, Table, Code)
│   └── ButtonGroup: Actions (Undo, Redo, Fullscreen)
├── EditorPane (textarea with line numbers)
└── MarkdownPreview.razor (Markdig rendering)
    └── InlineCommentIndicator.razor (anchored comment icons)
```

### Inline Comment Panel

```
InlineCommentPanel.razor (sidebar)
├── CommentAnchorIndicator.razor (highlight in preview)
├── CommentThread.razor (comment list at anchor)
│   └── CommentEditor.razor (reply form)
└── FilterControls (All / Unresolved)
```

### Review Checklist Panel

```
ReviewChecklistPanel.razor (panel)
├── ProgressBar (completion %)
├── ChecklistCategory.razor (collapsible section)
│   └── ChecklistItemRow.razor (checkbox + title + description)
└── ValidationMessage (required items warning)
```

---

## Appendix B: Prompt Template Variables

### Available Variables for Domain Templates

All domain-specific prompt templates have access to:

```handlebars
{{ticketId}}              - Ticket GUID
{{ticketTitle}}           - Ticket title
{{ticketDescription}}     - Original ticket description
{{acceptanceCriteria}}    - Success criteria
{{ticketType}}            - Web UI, REST API, Background Job, Database, etc.
{{repositoryName}}        - Repository name
{{branchName}}            - Feature branch name
{{relevantFiles}}         - List of potentially relevant file paths
{{codeSnippets}}          - Array of code examples from similar features
{{architecturePatterns}}  - Project architectural patterns
{{technologyStack}}       - Tech stack with versions
{{codeStyleGuidelines}}   - Formatting and naming conventions
```

### Example Template Usage

```text
You are an expert {{ticketType}} developer working on the {{repositoryName}} project.

The project uses {{technologyStack}} and follows these architectural patterns:
{{architecturePatterns}}

Analyze this ticket:
Title: {{ticketTitle}}
Description: {{ticketDescription}}
Acceptance Criteria: {{acceptanceCriteria}}

Relevant code examples from the codebase:
{{#each codeSnippets}}
File: {{this.filePath}}
```{{this.language}}
{{this.code}}
```
{{/each}}

Generate an implementation plan that follows the project's patterns...
```

---

**End of EPIC 07: Planning Phase UX & Collaboration Improvements**
