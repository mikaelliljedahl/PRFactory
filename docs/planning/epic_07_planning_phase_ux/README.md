# Epic 07: Planning Phase UX & Collaboration - Implementation Plans

This directory contains detailed implementation plans for **Epic 07: Planning Phase UX & Collaboration Improvements**.

---

## Overview

Epic 07 focuses on enhancing the planning phase user experience with professional markdown editing, inline commenting, and structured review capabilities. This epic delivers a **Notion-like collaborative planning experience** within PRFactory.

**Status:** In Planning
**Effort:** 5-7 weeks
**Priority:** High

---

## Features

This epic delivers **4 core features** that improve the planning phase UX:

### 1. Enhanced Planning Prompts (1 week)
**File:** [01_enhanced_prompts.md](01_enhanced_prompts.md)

Domain-specific prompt templates that improve AI-generated plan quality by including:
- Architectural patterns from the project
- Technology stack with versions
- Code examples from similar features
- Code style guidelines

**Impact:** Plans reference actual project patterns from the start, reducing refinement cycles.

---

### 2. Rich Markdown Editor Component (3 weeks)
**File:** [02_rich_markdown_editor.md](02_rich_markdown_editor.md)

Professional split-view markdown editor with:
- Live preview (debounced 300ms)
- Formatting toolbar (15+ buttons)
- Keyboard shortcuts (Ctrl+B, Ctrl+I, etc.)
- Responsive design (desktop/tablet/mobile)
- Line numbers and scroll sync

**Impact:** Dramatically improves editing experience, comparable to Notion/Confluence.

---

### 3. Inline Comment Anchoring (2 weeks)
**File:** [03_inline_comment_anchoring.md](03_inline_comment_anchoring.md)

Enable reviewers to anchor comments to specific lines in plans:
- Text selection with "Add Comment" button
- Visual indicators for anchored comments
- Right sidebar with comment threads
- Click-to-scroll navigation

**Impact:** Contextual discussions reduce back-and-forth and improve review efficiency.

---

### 4. Review Checklist Templates (2 weeks)
**File:** [04_review_checklists.md](04_review_checklists.md)

Structured, domain-specific review checklists:
- YAML-based templates (web_ui, rest_api, database, background_jobs)
- Checklist progress tracking
- Approval validation (required items)
- Configurable per repository or tenant

**Impact:** Consistent, high-quality reviews with clear evaluation criteria.

---

## Relationship to Other Epics

### Epic 1 (Team Review & Collaboration)
**Status:** 🟡 Partially Implemented

Epic 1 provides the **foundation** for AI-powered validation, notifications, and version history:
- **Plan Validation Service** - AI quality scoring
- **Notification System** - @mention alerts, assignment notifications
- **Plan Revision History** - Version tracking and diff comparison

**Epic 07 focuses on UX**, while Epic 1 focuses on backend collaboration infrastructure.

### Epic 5 (Agent Framework)
**Status:** ✅ Implemented

Epic 5 delivered the CLI-based agent architecture that Epic 07 enhances:
- 5 graphs (Refinement, Planning, Implementation, CodeReview, Orchestrator)
- 20+ specialized agents
- Checkpoint/resume system

### Epic 6 (Admin UI)
**Status:** 🟢 In Development

Epic 6 provides the UI component library structure that Epic 07 extends:
- 26 pure UI components in `/UI/` directory
- Code-behind pattern for business components
- Radzen + Bootstrap 5 + Blazor Server architecture

---

## Implementation Order

### Phase 1: Enhanced Prompts (Week 1)
- Create domain-specific prompt templates
- Update PlanningAgent to load templates based on ticket type
- Test with Web UI, REST API, Database ticket types

**Deliverable:** Plans reference actual project patterns

---

### Phase 2: Rich Editor Foundation (Weeks 2-4)
- Week 2: Core split-view editor with basic toolbar
- Week 3: Live preview, keyboard shortcuts, responsive design
- Week 4: Polish, scroll sync, integration, testing

**Deliverable:** Professional markdown editing experience

---

### Phase 3: Inline Collaboration (Weeks 5-6)
- Week 5: InlineCommentAnchor entity, text selection handler
- Week 6: Inline comment panel, visual indicators, navigation

**Deliverable:** Contextual discussions on specific plan sections

---

### Phase 4: Structured Reviews (Week 7)
- YAML template system
- Checklist UI component
- Approval validation

**Deliverable:** Consistent plan evaluation with structured criteria

---

## Architecture Compliance

All implementation plans follow **CLAUDE.md guidelines**:

✅ **DO:**
- Use Blazor Server with code-behind pattern
- Inject services directly (no HTTP calls within process)
- Use existing UI component library (`/UI/*`)
- Pure Blazor (no custom JavaScript)
- Use Radzen + Bootstrap 5 only
- 80% unit test coverage minimum

❌ **DON'T:**
- Add new UI libraries (MudBlazor, Telerik, etc.)
- Use JavaScript (Blazor Server handles everything)
- Make HTTP calls from Blazor to API in same process
- Skip code-behind for business components

---

## Current Implementation Status

### What Exists Today

From codebase exploration (2025-11-14):

**Review System** (35 files, ~1,765 LOC):
- ✅ `PlanReview` entity - Multi-reviewer support
- ✅ `ReviewComment` entity - Markdown comments
- ✅ `PlanReviewService` - Assign reviewers, add comments, approval
- ✅ `PlanReviewSection.razor` - Review UI
- ✅ `ReviewCommentThread.razor` - Linear comments with Markdig

**Current Limitations**:
- ⚠️ `TicketUpdateEditor.razor` - Plain textarea (8 rows)
- ❌ No rich markdown editor
- ❌ No inline comment anchoring
- ❌ No review checklists
- ❌ No domain-specific planning prompts

---

## Testing Strategy

### Unit Tests (80% Coverage Target)

Each feature includes unit test requirements:
- Prompt template loading and variable substitution
- MarkdownEditor toolbar actions
- Inline comment anchor creation and validation
- Checklist validation logic

### Integration Tests

- MarkdownEditor component rendering
- Inline comment panel with anchored comments
- Checklist progress tracking
- Domain template selection based on ticket type

### Manual Testing

Scenarios included in each feature implementation plan.

---

## Database Schema Changes

### New Tables

**InlineCommentAnchors** (Feature 3):
```sql
CREATE TABLE InlineCommentAnchors (
    Id UUID PRIMARY KEY,
    ReviewCommentId UUID NOT NULL,
    StartLine INT NOT NULL,
    EndLine INT NOT NULL,
    TextSnippet VARCHAR(200) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);
```

**ReviewChecklists** (Feature 4):
```sql
CREATE TABLE ReviewChecklists (
    Id UUID PRIMARY KEY,
    PlanReviewId UUID NOT NULL,
    TemplateName VARCHAR(100) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);

CREATE TABLE ChecklistItems (
    Id UUID PRIMARY KEY,
    ReviewChecklistId UUID NOT NULL,
    Category VARCHAR(100) NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    Severity VARCHAR(20) NOT NULL,
    IsChecked BOOLEAN NOT NULL DEFAULT FALSE,
    CheckedAt TIMESTAMP,
    SortOrder INT NOT NULL DEFAULT 0
);
```

---

## Feature Flags

All features support gradual rollout:

- `EnableEnhancedPlanningPrompts` - Default: `true`
- `EnableRichMarkdownEditor` - Default: `true`
- `EnableInlineCommentAnchors` - Default: `true`
- `EnableReviewChecklists` - Default: `false` (opt-in initially)

---

## Success Metrics

**User Experience**:
- User satisfaction score > 4.0/5.0 for editing experience
- Average characters per plan edit session increases by 50%

**Review Quality**:
- Review comment specificity improves (% with line anchors > 60%)
- First-time plan acceptance rate increases by 20%

**Plan Quality**:
- Refinement requests decrease by 30% (due to better prompts)
- Plans reference project patterns 90%+ of the time

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| **Rich editor performance** | Debounce preview (300ms), lazy load for large plans |
| **Inline comment accuracy** | Store line range + text snippet for verification |
| **Checklist rigidity** | Make most items "recommended", allow custom templates |
| **Browser compatibility** | Use standard CSS Grid, test on all major browsers |
| **User adoption** | Provide tutorials, maintain plain textarea fallback |

---

## Related Documentation

- **Main Epic Document**: [/docs/planning/EPIC_07_PLANNING_PHASE_UX.md](/home/user/PRFactory/docs/planning/EPIC_07_PLANNING_PHASE_UX.md)
- **Epic 1 (Team Review)**: [/docs/planning/team_review/README.md](/home/user/PRFactory/docs/planning/team_review/README.md)
- **Epic 5 (Agent Framework)**: [/docs/planning/epic_05_agent_framework/README.md](/home/user/PRFactory/docs/planning/epic_05_agent_framework/README.md)
- **Epic 6 (Admin UI)**: [/docs/planning/epic_06_admin_ui/README.md](/home/user/PRFactory/docs/planning/epic_06_admin_ui/README.md)
- **Architecture Guidelines**: [/CLAUDE.md](/home/user/PRFactory/CLAUDE.md)
- **Current Status**: [/docs/IMPLEMENTATION_STATUS.md](/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md)

---

## Implementation Files

| File | Feature | Effort | Priority |
|------|---------|--------|----------|
| [01_enhanced_prompts.md](01_enhanced_prompts.md) | Enhanced Planning Prompts | 1 week | P1 |
| [02_rich_markdown_editor.md](02_rich_markdown_editor.md) | Rich Markdown Editor | 3 weeks | P1 |
| [03_inline_comment_anchoring.md](03_inline_comment_anchoring.md) | Inline Comment Anchoring | 2 weeks | P1 |
| [04_review_checklists.md](04_review_checklists.md) | Review Checklist Templates | 2 weeks | P2 |

**Total:** 8 weeks maximum, 5-7 weeks with parallel development

---

## Questions or Issues?

For questions about these implementation plans:
1. Review the detailed plan in each markdown file
2. Check the main EPIC 07 document for strategic context
3. Consult CLAUDE.md for architecture guidelines
4. Check Epic 1 for related collaboration features
5. Ask the team lead or product owner
