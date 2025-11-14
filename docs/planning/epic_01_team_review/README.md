# Team Review & Collaboration - Implementation Plans

This directory contains detailed implementation plans for **Epic 1: Team Review & Collaboration**.

---

## Overview

Epic 1 transforms PRFactory from single-player to multi-player by enabling team collaboration on AI-generated plans through commenting, discussion, formal approvals, and AI-powered validation.

**Status:** 🟡 Partially Implemented
- ✅ Core team review infrastructure exists (PlanReview, ReviewComment entities)
- ✅ Application services implemented (IPlanReviewService, IPlanService)
- ✅ Blazor components exist (PlanReviewSection, PlanReviewStatus)
- ⚠️ Some features need implementation (AI validation, notifications, revision history)

---

## Implementation Plans

### 1. Plan Validation Service (P1 - Highest Priority)
**File:** [01_plan_validation_service.md](01_plan_validation_service.md)

AI-powered plan validation with security, completeness, and performance checks.

**Features:**
- Security vulnerability analysis
- Completeness assessment
- Performance bottleneck detection
- Code-vs-plan alignment validation
- Blazor UI integration

**Effort:** 3-5 days (~992 LOC)

**Key Files to Create:**
- `/src/PRFactory.Core/Application/Services/IPlanValidationService.cs`
- `/src/PRFactory.Infrastructure/Application/PlanValidationService.cs`
- `/prompts/review/anthropic/plan_security_check.txt`
- `/prompts/review/anthropic/plan_completeness_check.txt`
- `/prompts/review/anthropic/plan_performance_check.txt`
- `/prompts/review/anthropic/code_plan_validation.txt`
- `/src/PRFactory.Web/Components/Plans/PlanValidationPanel.razor`

---

### 2. Notification System (P1)
**File:** [02_notification_system.md](02_notification_system.md)

In-app notification system for reviewer assignments, @mentions, and plan status changes.

**Features:**
- Notify when assigned as reviewer
- Notify when @mentioned in comments
- Notify when plan approved/rejected
- Notification bell in navbar with unread count
- Mark as read/unread functionality

**Effort:** 3-4 days (~705 LOC)

**Key Files to Create:**
- `/src/PRFactory.Domain/Entities/Notification.cs`
- `/src/PRFactory.Domain/Interfaces/INotificationRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Repositories/NotificationRepository.cs`
- `/src/PRFactory.Core/Application/Services/INotificationService.cs`
- `/src/PRFactory.Infrastructure/Application/NotificationService.cs`
- `/src/PRFactory.Web/Models/NotificationDto.cs`
- `/src/PRFactory.Web/Components/Notifications/NotificationBell.razor`

**Integration Points:**
- `PlanReviewService.AssignReviewersAsync()` - trigger ReviewerAssigned notifications
- `PlanReviewService.AddCommentAsync()` - trigger MentionedInComment notifications
- Ticket approval/rejection workflows - trigger status change notifications

---

### 3. Plan Revision History (P2)
**File:** [03_plan_revision_history.md](03_plan_revision_history.md)

Track and compare plan revisions when refined or regenerated.

**Features:**
- Timeline view of all revisions
- Track revision reason (initial, refined, regenerated)
- Side-by-side diff viewer
- Compare any two revisions
- Automatic revision creation on plan changes

**Effort:** 3-4 days (~745 LOC)

**Key Files to Create:**
- `/src/PRFactory.Domain/Entities/PlanRevision.cs`
- `/src/PRFactory.Domain/Interfaces/IPlanRevisionRepository.cs`
- `/src/PRFactory.Infrastructure/Persistence/Repositories/PlanRevisionRepository.cs`
- `/src/PRFactory.Web/Models/PlanRevisionDto.cs`
- `/src/PRFactory.Web/Components/Plans/PlanRevisionHistory.razor`

**Integration Points:**
- `PlanningGraph` - create Initial revision after plan generated
- `TicketApplicationService.RefinePlanAsync()` - create Refined revision
- `TicketApplicationService.RejectPlanAsync()` - create Regenerated revision

---

## Implementation Order

### Phase 1: High-Value Features (Week 1)
**Priority:** Deliver immediate value with AI validation

1. **Plan Validation Service** (Days 1-5)
   - Create service interfaces and implementation
   - Create prompt templates
   - Integrate with web services
   - Build Blazor UI component
   - Write unit tests

**Deliverable:** Users can validate plans for security, completeness, and performance before approval

---

### Phase 2: Collaboration Enhancements (Week 2)
**Priority:** Improve team communication and awareness

2. **Notification System** (Days 6-9)
   - Database schema and entity
   - Repository and service implementation
   - Blazor notification bell component
   - Integration with review workflows

**Deliverable:** Users receive in-app notifications for reviewer assignments and @mentions

3. **Plan Revision History** (Days 10-13)
   - Database schema and entity
   - Service methods for revision tracking
   - Blazor component with timeline and diff viewer
   - Automatic revision creation hooks

**Deliverable:** Users can view plan evolution and compare revisions

---

## Architecture Compliance

All implementation plans follow **Blazor Server architecture** guidelines from CLAUDE.md:

✅ **DO:**
- Use direct service injection (`@inject ITicketService`)
- Create application services (IPlanValidationService, INotificationService)
- Use code-behind pattern for Blazor components
- Follow Clean Architecture layers (Domain → Application → Web)
- Use LibGit2Sharp for git operations
- Use existing UI component library (/UI/*)

❌ **DON'T:**
- Create API controllers for internal Blazor use
- Make HTTP calls within the same process
- Use JavaScript (Blazor Server handles everything)
- Add new UI component libraries (use Blazor + Radzen only)

---

## Total Effort Estimate

**Lines of Code:**
- Plan Validation Service: ~992 LOC
- Notification System: ~705 LOC
- Plan Revision History: ~745 LOC
- **Total: ~2,442 LOC**

**Time Estimate:**
- Week 1: Plan Validation Service (5 days)
- Week 2: Notification System (4 days) + Plan Revision History (4 days)
- **Total: 13 days (~2-3 weeks with testing and refinement)**

---

## Testing Strategy

### Unit Tests
- Service logic (validation, notifications, revisions)
- Entity methods (Notification.MarkAsRead, PlanRevision.Create)
- Parsing logic (validation response extraction)

### Integration Tests
- Service integration (create notification → retrieve)
- Repository operations (CRUD operations)
- Workflow integration (assign reviewer → notification created)

### E2E Tests
- User triggers plan validation from UI
- User receives notification when mentioned
- User views plan revision history and compares revisions

---

## Dependencies

### Existing Infrastructure (Already Available)
- ✅ `IPlanService` - for reading plan content
- ✅ `IAgentPromptService` - for loading prompt templates
- ✅ `ILlmProvider` - for calling Claude API
- ✅ `ITicketRepository` - for ticket data
- ✅ `IUserRepository` - for user data
- ✅ `ICurrentUserService` - for authenticated user
- ✅ Logging infrastructure

### New Dependencies
- None (uses existing infrastructure)

---

## Future Enhancements (Post-Epic 1)

1. **Email Notifications**
   - Send email for critical notifications
   - User preferences for email frequency

2. **Real-time Updates (SignalR)**
   - Push notifications to connected clients
   - Live plan updates without polling

3. **Advanced Diff Viewer**
   - Syntax highlighting for markdown
   - Use DiffPlex or diff-match-patch library
   - Inline diff view

4. **Slack/Teams Integration**
   - Post notifications to team channels
   - DM users on external platforms

5. **Analytics Dashboard**
   - Approval rates and review times
   - Most active reviewers
   - Common rejection reasons

---

## Related Documentation

- **Epic Overview:** [/docs/planning/EPIC_01_TEAM_REVIEW.md](../EPIC_01_TEAM_REVIEW.md)
- **Current Implementation Status:** [/docs/IMPLEMENTATION_STATUS.md](/docs/IMPLEMENTATION_STATUS.md)
- **Architecture Guidelines:** [/CLAUDE.md](/CLAUDE.md)
- **Roadmap:** [/docs/ROADMAP.md](/docs/ROADMAP.md)

---

## Questions or Issues?

If you have questions about these implementation plans:
1. Review the detailed plan in each markdown file
2. Check the main EPIC document for context
3. Consult CLAUDE.md for architecture guidelines
4. Ask the team lead or product owner
