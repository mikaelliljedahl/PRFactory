# EPIC 08: System Architecture Cleanup - Implementation Guide

**Status**: Ready for Implementation
**Epic Branch**: `claude/epic-08-architecture-cleanup-011qsr23waxxd4j1uZxEvuHH`
**Total Estimated Duration**: 8-9 weeks
**Phases**: 6 sequential phases

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Phase Execution Strategy](#phase-execution-strategy)
- [Phase Descriptions](#phase-descriptions)
- [Orchestrator Instructions](#orchestrator-instructions)
- [Quality Gates](#quality-gates)
- [Rollback Strategy](#rollback-strategy)

---

## Overview

This epic combines two complementary architectural improvements:

1. **Project Consolidation** (Phases 1) - Merge Api/Worker/Web into single executable
2. **UI Architecture Refinement** (Phases 2-6) - Improve component architecture, CSS, data fetching

### Why This Order?

**Phase 1 must complete first** because:
- Establishes new project structure that subsequent phases build upon
- Changes file paths (Controllers, BackgroundServices move to Web project)
- Simplifies development workflow (single `dotnet run` instead of 3 terminals)
- Reduces deployment complexity (1 container instead of 3)

**Phases 2-6** can then improve the UI architecture with:
- CSS isolation for better maintainability
- Server-side pagination for scalability
- Missing UI components for consistency
- Centralized DTO mapping
- Page refactoring and polish

---

## Prerequisites

### Before Starting

✅ **Code Preparation:**
- [ ] All tests passing on main branch
- [ ] No pending PRs that modify `PRFactory.Web`, `PRFactory.Api`, or `PRFactory.Worker`
- [ ] Clean working directory (`git status` shows no uncommitted changes)
- [ ] Create epic branch: `git checkout -b epic/08-architecture-cleanup`

✅ **Environment Setup:**
- [ ] .NET 10 SDK installed
- [ ] Docker installed and running
- [ ] NuGet proxy configured (for Claude Code web sessions)
- [ ] Test database accessible

✅ **Documentation Review:**
- [ ] Read `EPIC_08_SYSTEM_ARCHITECTURE_CLEANUP.md` (project consolidation details)
- [ ] Read `Component-Architecture-Refactoring-Plan.md` (UI improvements details)
- [ ] Review `/CLAUDE.md` for architectural principles
- [ ] Review `/docs/ARCHITECTURE.md` for current architecture

---

## Phase Execution Strategy

### Sequential Phases (MUST be done in order)

| Phase | Duration | Can Start After | Parallelizable? | Risk Level |
|-------|----------|------------------|-----------------|------------|
| **Phase 1: Project Consolidation** | 4-5 days | Immediate | ❌ No (foundation) | 🔴 High |
| **Phase 2: CSS Isolation** | 1 week | Phase 1 merged | ✅ Some tasks | 🟡 Medium |
| **Phase 3: Pagination** | 1 week | Phase 2 merged | ✅ Some tasks | 🟡 Medium |
| **Phase 4: Missing UI Components** | 1-2 weeks | Phase 3 merged | ✅ Yes (per component) | 🟢 Low |
| **Phase 5: DTO Mapping** | 1 week | Phase 4 merged | ✅ Some tasks | 🟡 Medium |
| **Phase 6: Final Polish** | 1-2 weeks | Phase 5 merged | ✅ Some tasks | 🟢 Low |

**Total: 8-9 weeks**

### Critical Path Dependencies

```
Phase 1 (Project Consolidation)
    └──> MUST merge before starting Phase 2
         └──> Phase 2 (CSS Isolation)
              └──> MUST merge before starting Phase 3
                   └──> Phase 3 (Pagination)
                        └──> MUST merge before starting Phase 4
                             └──> Phase 4 (Missing UI Components)
                                  └──> CAN overlap with Phase 5 if careful
                                       └──> Phase 5 (DTO Mapping)
                                            └──> Phase 6 (Final Polish)
```

**DO NOT:**
- ❌ Start Phase 2 before Phase 1 is merged to main
- ❌ Work on multiple phases simultaneously on the same branch
- ❌ Skip quality gates between phases

**DO:**
- ✅ Merge each phase to main before starting the next
- ✅ Run full test suite between phases
- ✅ Parallelize tasks within a phase when safe
- ✅ Document any deviations from the plan

---

## Phase Descriptions

### Phase 1: Project Consolidation (4-5 days) 🔴 **CRITICAL**

**Goal**: Merge `PRFactory.Api`, `PRFactory.Worker`, and `PRFactory.Web` into a single `PRFactory.Web` project.

**What Changes:**
- Controllers moved from Api to Web
- Background services moved from Worker to Web
- Configuration files merged
- Dockerfile consolidated
- docker-compose simplified (3 containers → 1 container)

**Task Document**: `PHASE_01_PROJECT_CONSOLIDATION.md`

**Success Criteria:**
- ✅ Single `dotnet run` starts everything (UI + API + Worker)
- ✅ All existing tests pass
- ✅ Docker build succeeds
- ✅ Jira webhooks work
- ✅ Background agent execution works
- ✅ OAuth login works

**Deliverables:**
- Consolidated `PRFactory.Web` project
- Updated Dockerfile and docker-compose.yml
- Merged appsettings.json
- Updated documentation
- Updated CI/CD pipeline

---

### Phase 2: CSS Isolation (1 week) 🟡

**Goal**: Migrate inline `<style>` tags to CSS isolation (`.razor.css` files) and establish CSS standards.

**What Changes:**
- `TicketHeader.razor` inline styles → `TicketHeader.razor.css`
- Create CSS isolation for existing UI components
- Document CSS strategy (when to use isolation vs Bootstrap utilities)

**Task Document**: `PHASE_02_CSS_ISOLATION.md`

**Success Criteria:**
- ✅ Zero `<style>` tags in `.razor` files
- ✅ All complex components have `.razor.css` files
- ✅ Visual regression tests pass (no styling changes visible to users)
- ✅ CSS strategy documented

**Deliverables:**
- CSS isolation files for 15-20 components
- `docs/CSS-Strategy.md`
- Updated components with no inline styles

---

### Phase 3: Server-Side Pagination (1 week) 🟡

**Goal**: Replace in-memory filtering/pagination with server-side implementation for scalability.

**What Changes:**
- Create `PagedResult<T>` and `PaginationParams` DTOs
- Update repositories to support filtering/pagination
- Update services to use pagination
- Refactor `Tickets/Index.razor` to use server-side pagination

**Task Document**: `PHASE_03_PAGINATION.md`

**Success Criteria:**
- ✅ No more `GetAllTicketsAsync()` calls that load everything
- ✅ Page load time <500ms for 1000+ tickets
- ✅ Memory usage reduced
- ✅ All pagination queries use server-side filtering

**Deliverables:**
- `PagedResult<T>` infrastructure
- Updated repository methods
- Updated service layer
- Refactored ticket list page

---

### Phase 4: Missing UI Components (1-2 weeks) 🟢

**Goal**: Add missing UI components to eliminate remaining Bootstrap spaghetti code.

**What Changes:**
- Create `PageHeader` component
- Create `GridLayout`/`GridColumn` components
- Create `Section` component (collapsible)
- Create `InfoBox` component
- Create `ProgressBar` component

**Task Document**: `PHASE_04_MISSING_UI_COMPONENTS.md`

**Success Criteria:**
- ✅ All 5 new components created and tested
- ✅ Components documented with usage examples
- ✅ At least 3 pages use each new component
- ✅ Zero raw Bootstrap markup for common patterns

**Deliverables:**
- 5 new UI components with `.razor`, `.razor.cs`, `.razor.css` files
- Component usage examples
- Updated pages using new components

---

### Phase 5: DTO Mapping with Mapperly (1 week) 🟡

**Goal**: Centralize entity-to-DTO mapping using Mapperly (compile-time source generation).

**What Changes:**
- Install `Riok.Mapperly` NuGet package
- Create mapper interfaces (`TicketMapper`, `RepositoryMapper`, etc.)
- Update services to use mappers instead of manual conversion
- Remove manual mapping code

**Task Document**: `PHASE_05_DTO_MAPPING.md`

**Success Criteria:**
- ✅ All mappers registered in DI
- ✅ No manual entity-to-DTO conversion in services
- ✅ Mapperly generates code at compile time
- ✅ All tests pass with mappers

**Deliverables:**
- Mapper interfaces for all entities
- Updated service layer using mappers
- Removed manual mapping code

---

### Phase 6: Final Polish & Page Refactoring (1-2 weeks) 🟢

**Goal**: Refactor pages to use component library consistently, standardize patterns, update documentation.

**What Changes:**
- Refactor high-traffic pages to use new components
- Standardize error display (use `AlertMessage` everywhere)
- Update documentation (CLAUDE.md, ARCHITECTURE.md, etc.)
- Final cleanup and polish

**Task Document**: `PHASE_06_FINAL_POLISH.md`

**Success Criteria:**
- ✅ All pages use component library consistently
- ✅ No raw Bootstrap markup for common patterns
- ✅ Documentation updated
- ✅ All tests pass
- ✅ Code coverage maintained or improved

**Deliverables:**
- Refactored pages
- Updated documentation
- Final cleanup commits

---

## Orchestrator Instructions

### For the Orchestrator Agent

You are responsible for **coordinating the implementation of this epic** across 6 sequential phases. Your role is to:

1. **Assign tasks to specialized agents**
2. **Ensure phases complete in correct order**
3. **Validate quality gates between phases**
4. **Handle rollbacks if needed**
5. **Track progress and communicate status**

### Phase Execution Workflow

For each phase, follow this workflow:

#### Step 1: Pre-Phase Checklist

```markdown
## Phase X Pre-Flight Checklist

- [ ] Previous phase merged to main (or this is Phase 1)
- [ ] All tests passing on main branch
- [ ] Epic branch up to date with main: `git pull origin main`
- [ ] Phase task document reviewed: `PHASE_0X_*.md`
- [ ] Specialized agent identified for this phase
- [ ] Estimated timeline confirmed
```

#### Step 2: Assign to Agent

Use the **agent type mapping** below to select the right agent:

| Phase | Recommended Agent Type | Alternative |
|-------|------------------------|-------------|
| Phase 1 | `code-implementation-specialist` | `general-purpose` |
| Phase 2 | `code-implementation-specialist` | `simple-code-implementation` (for single files) |
| Phase 3 | `code-implementation-specialist` | `general-purpose` |
| Phase 4 | `code-implementation-specialist` + `simple-code-implementation` | Parallelize per component |
| Phase 5 | `code-implementation-specialist` | `general-purpose` |
| Phase 6 | `code-implementation-specialist` + `general-purpose` | Divide by page/doc updates |

**Agent Invocation Pattern:**

```markdown
Use the Task tool with:
- subagent_type: [agent type from table above]
- description: "Phase X: [phase name]"
- prompt: """
You are implementing Phase X of EPIC 08: System Architecture Cleanup.

**Task Document**: Read and follow `C:\code\github\PRFactory\docs\planning\epic_08_architecture_cleanup\PHASE_0X_*.md`

**Your Responsibilities**:
1. Read the phase task document thoroughly
2. Implement all changes listed in the document
3. Follow the success criteria
4. Run all tests and ensure they pass
5. Commit changes with clear commit messages

**Important**:
- Do NOT start implementing before reading the task document
- Follow the CLAUDE.md guidelines for coding standards
- Source /tmp/dotnet-proxy-setup.sh before running dotnet commands
- Ensure all tests pass before marking complete
- Document any deviations from the plan

**Deliverables**:
[List from phase document]

Begin implementation now.
"""
```

#### Step 3: Monitor Progress

As the orchestrator, you should:

1. **Check in periodically** - Review agent progress, unblock issues
2. **Validate intermediate deliverables** - Ensure quality standards met
3. **Handle blockers** - If agent is stuck, provide guidance or reassign
4. **Track timeline** - Alert if phase is taking longer than expected

#### Step 4: Quality Gate Validation

Before moving to the next phase, verify:

```markdown
## Phase X Quality Gate

### Build & Test
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes 100% of tests
- [ ] `dotnet format --verify-no-changes` passes (no encoding/formatting issues)

### Functional Validation
- [ ] All success criteria from phase document met
- [ ] Manual smoke tests passed (if applicable)
- [ ] No regressions in existing functionality

### Documentation
- [ ] Code changes documented with XML comments
- [ ] Any new components have usage examples
- [ ] CLAUDE.md updated if architectural changes made

### Code Quality
- [ ] No compiler warnings introduced
- [ ] Code follows project conventions
- [ ] No security vulnerabilities introduced
- [ ] Performance benchmarks met (if applicable)

### Git Hygiene
- [ ] Commits follow conventional commit format
- [ ] Commit messages are clear and descriptive
- [ ] No merge conflicts with main
- [ ] Branch ready to merge
```

#### Step 5: Merge Phase

Once quality gate passes:

```bash
# Ensure branch is up to date
git checkout epic/08-architecture-cleanup
git pull origin main

# Merge to main (or create PR)
git checkout main
git merge --no-ff epic/08-architecture-cleanup
git push origin main

# Tag the completion
git tag -a phase-X-complete -m "Phase X: [phase name] complete"
git push origin --tags
```

#### Step 6: Phase Retrospective

After each phase, document:

```markdown
## Phase X Retrospective

**Duration**: [actual time taken]
**Blockers**: [any issues encountered]
**Deviations**: [changes from original plan]
**Lessons Learned**: [what went well, what could improve]
**Next Phase Adjustments**: [any changes to plan based on learnings]
```

---

## Quality Gates

### Between All Phases

Before proceeding to the next phase:

1. **Build & Test Gate**
   ```bash
   source /tmp/dotnet-proxy-setup.sh
   dotnet build                                # Must succeed
   dotnet test                                 # Must pass 100%
   dotnet format --verify-no-changes          # Must pass
   ```

2. **Functional Gate**
   - All phase success criteria met
   - Manual smoke tests passed
   - No known regressions

3. **Documentation Gate**
   - Code documented with XML comments
   - Any architectural changes reflected in CLAUDE.md
   - Phase retrospective completed

4. **Approval Gate**
   - Code review completed (if using PRs)
   - All feedback addressed
   - Ready to merge to main

**DO NOT** proceed to next phase until all gates pass.

---

## Rollback Strategy

### If Phase Fails Quality Gate

**Option 1: Fix Forward (Preferred)**
```bash
# Continue on same branch, fix issues
git checkout epic/08-architecture-cleanup
# Make fixes
git commit -m "fix: address quality gate issues for Phase X"
# Re-run quality gate
```

**Option 2: Rollback Phase**
```bash
# Reset to before phase started
git checkout epic/08-architecture-cleanup
git reset --hard phase-[X-1]-complete  # Tag from previous phase
# Start phase again with fixes
```

**Option 3: Abort Epic**
```bash
# If fundamental issues discovered
git checkout main
git branch -D epic/08-architecture-cleanup
# Create tag for future reference
git tag -a epic-08-abandoned-[date] -m "Reason for abandonment"
# Document reasons in retrospective
```

### Critical Rollback Points

**After Phase 1 (Project Consolidation):**
- If consolidated project has critical issues
- If performance degrades significantly
- If deployment complexity increases unexpectedly

**Action:** Rollback to pre-consolidation state, keep Api/Worker/Web separate

**After Phase 3 (Pagination):**
- If pagination causes data loss or corruption
- If query performance degrades
- If in-memory filtering was actually acceptable for scale

**Action:** Revert pagination changes, keep in-memory filtering

---

## Progress Tracking

### Epic-Level Tracking

Use this checklist to track overall epic progress:

```markdown
## EPIC 08 Progress

### Phase 1: Project Consolidation (4-5 days)
- [ ] Started: [date]
- [ ] Quality gate passed: [date]
- [ ] Merged to main: [date]
- [ ] Retrospective completed: [date]

### Phase 2: CSS Isolation (1 week)
- [ ] Started: [date]
- [ ] Quality gate passed: [date]
- [ ] Merged to main: [date]
- [ ] Retrospective completed: [date]

### Phase 3: Server-Side Pagination (1 week)
- [ ] Started: [date]
- [ ] Quality gate passed: [date]
- [ ] Merged to main: [date]
- [ ] Retrospective completed: [date]

### Phase 4: Missing UI Components (1-2 weeks)
- [ ] Started: [date]
- [ ] Quality gate passed: [date]
- [ ] Merged to main: [date]
- [ ] Retrospective completed: [date]

### Phase 5: DTO Mapping (1 week)
- [ ] Started: [date]
- [ ] Quality gate passed: [date]
- [ ] Merged to main: [date]
- [ ] Retrospective completed: [date]

### Phase 6: Final Polish (1-2 weeks)
- [ ] Started: [date]
- [ ] Quality gate passed: [date]
- [ ] Merged to main: [date]
- [ ] Retrospective completed: [date]

### Epic Complete
- [ ] All phases merged
- [ ] Final documentation updated
- [ ] Epic retrospective completed
- [ ] Branch archived
```

---

## Communication Protocol

### Status Updates

Provide regular status updates in this format:

```markdown
## EPIC 08 Status Update - [date]

**Current Phase**: Phase X - [phase name]
**Progress**: [X]% complete (based on success criteria)
**Timeline**: On track / Delayed by [X days]
**Blockers**: [List any blockers]
**Next Milestone**: [Next quality gate or phase completion]
**Risk Level**: 🟢 Low / 🟡 Medium / 🔴 High

### Recent Accomplishments
- [What was completed this week]

### Upcoming Work
- [What's planned for next week]

### Help Needed
- [Any assistance required from team]
```

---

## Success Metrics

### Epic-Level Success Criteria

At the end of EPIC 08, verify:

**Project Consolidation**:
- ✅ Single `PRFactory.Web` project contains all functionality
- ✅ Single `dotnet run` starts everything
- ✅ Single Docker container deployment
- ✅ All existing functionality works (no regressions)

**UI Architecture**:
- ✅ Zero inline `<style>` tags in components
- ✅ 5+ new UI components added
- ✅ Server-side pagination implemented
- ✅ Centralized DTO mapping with Mapperly
- ✅ Consistent component usage across all pages

**Quality**:
- ✅ All tests passing (100%)
- ✅ No compiler warnings
- ✅ Code coverage maintained or improved
- ✅ Performance benchmarks met

**Documentation**:
- ✅ CLAUDE.md updated
- ✅ ARCHITECTURE.md updated
- ✅ IMPLEMENTATION_STATUS.md updated
- ✅ All phase retrospectives completed

---

## Questions?

If you encounter issues or have questions during implementation:

1. **Check existing documentation**: `CLAUDE.md`, `ARCHITECTURE.md`, phase task documents
2. **Review quality gates**: Ensure you're not proceeding before gates pass
3. **Document the issue**: Add to phase retrospective
4. **Ask for help**: Tag this epic for review

---

## Summary for Orchestrator

**Your Mission**: Successfully coordinate the implementation of all 6 phases of EPIC 08, ensuring:

1. **Sequential execution** - Each phase completes before the next starts
2. **Quality standards** - All quality gates pass between phases
3. **Agent coordination** - Right agents assigned to right tasks
4. **Progress tracking** - Regular status updates and retrospectives
5. **Risk management** - Identify and mitigate risks early

**Remember**: This is a foundational epic that affects the entire codebase. Take your time, validate thoroughly, and don't skip quality gates.

**Good luck!** 🚀
