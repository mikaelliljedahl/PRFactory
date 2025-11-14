# EPIC 08: System Architecture Cleanup - Document Summary

**Created**: 2025-11-14
**Status**: Ready for Implementation
**Total Duration**: 8-9 weeks (6 phases)

---

## Document Overview

This epic combines two complementary architectural improvements:
1. **Project Consolidation** - Merge Api/Worker/Web into single executable
2. **UI Architecture Refinement** - Improve components, CSS, data fetching

---

## Implementation Documents

### 📋 Orchestrator Guide

**File**: `README.md` (19.8 KB)

**Purpose**: Main coordination document for orchestrator agent

**Contains**:
- Overview and prerequisites
- Phase execution strategy
- Quality gates and rollback procedures
- Progress tracking
- Communication protocols

**Key sections**:
- Phase descriptions with dependencies
- Agent type recommendations per phase
- Quality gate validation checklists
- Rollback strategies

---

### 🔧 Phase 1: Project Consolidation (4-5 days)

**File**: `PHASE_01_PROJECT_CONSOLIDATION.md` (34.7 KB)

**Objective**: Merge 3 projects into 1

**What it covers**:
- Step-by-step consolidation instructions
- Moving Controllers, BackgroundServices, Middleware
- Merging configuration files
- Updating Program.cs
- Creating unified Dockerfile
- Updating docker-compose.yml
- CI/CD pipeline updates
- Testing procedures

**Risk**: 🔴 High (structural changes)

**Success criteria**: Single `dotnet run` starts everything

---

### 🎨 Phase 2: CSS Isolation (1 week)

**File**: `PHASE_02_CSS_ISOLATION.md` (10.5 KB)

**Objective**: Migrate inline styles to CSS isolation

**What it covers**:
- Auditing components for inline `<style>` tags
- Migrating to `.razor.css` files
- Creating CSS strategy document
- Visual regression testing

**Risk**: 🟡 Medium

**Success criteria**: Zero inline styles, all components use CSS isolation

---

### 📊 Phase 3: Server-Side Pagination (1 week)

**File**: `PHASE_03_PAGINATION.md` (15.0 KB)

**Objective**: Replace in-memory filtering with database queries

**What it covers**:
- Creating PagedResult<T> and PaginationParams DTOs
- Updating repository, service, and web layers
- Refactoring Tickets/Index page
- Performance testing

**Risk**: 🟡 Medium

**Success criteria**: Page load <500ms for 1000+ tickets

---

### 🧩 Phase 4: Missing UI Components (1-2 weeks)

**File**: `PHASE_04_MISSING_UI_COMPONENTS.md` (11.7 KB)

**Objective**: Add 5 new UI components

**What it covers**:
- PageHeader component
- GridLayout/GridColumn components
- Section component (collapsible)
- InfoBox component
- ProgressBar component

**Risk**: 🟢 Low

**Success criteria**: 5 new components created, used in 3+ pages each

**Parallelizable**: Yes - each component is independent

---

### 🗺️ Phase 5: DTO Mapping (1 week)

**File**: `PHASE_05_DTO_MAPPING.md` (14.5 KB)

**Objective**: Centralize mapping with Mapperly

**What it covers**:
- Installing Mapperly NuGet package
- Creating mapper interfaces for all entities
- Registering mappers in DI
- Updating services to use mappers
- Removing manual mapping code

**Risk**: 🟡 Medium

**Success criteria**: All mapping uses Mapperly, zero manual mapping

---

### ✨ Phase 6: Final Polish (1-2 weeks)

**File**: `PHASE_06_FINAL_POLISH.md` (15.8 KB)

**Objective**: Refactor pages, clean up code, update docs

**What it covers**:
- Refactoring high-traffic pages
- Standardizing error display
- Code cleanup
- Documentation updates (CLAUDE.md, ARCHITECTURE.md, etc.)
- Epic retrospective

**Risk**: 🟢 Low

**Success criteria**: All pages use component library, docs updated

---

## Reference Documents

### Original Planning Documents

1. **EPIC_08_SYSTEM_ARCHITECTURE_CLEANUP.md** (in parent folder)
   - Original project consolidation plan
   - 1,856 lines
   - Detailed technical analysis

2. **Component-Architecture-Refactoring-Plan.md** (43.3 KB)
   - Original UI architecture plan
   - Detailed component specifications
   - Before/after examples

---

## Quick Reference

### Phase Dependencies (Sequential)

```
Phase 1 (Foundation)
  ↓
Phase 2 (CSS Isolation)
  ↓
Phase 3 (Pagination)
  ↓
Phase 4 (UI Components)
  ↓
Phase 5 (DTO Mapping)
  ↓
Phase 6 (Final Polish)
```

**CRITICAL**: Must complete each phase and merge to main before starting next.

---

### Agent Recommendations

| Phase | Primary Agent | Alternative | Parallelizable? |
|-------|--------------|-------------|-----------------|
| Phase 1 | code-implementation-specialist | general-purpose | ❌ No |
| Phase 2 | code-implementation-specialist | simple-code-implementation | ✅ Per file |
| Phase 3 | code-implementation-specialist | general-purpose | ✅ Some tasks |
| Phase 4 | code-implementation-specialist | simple-code-implementation | ✅ Per component |
| Phase 5 | code-implementation-specialist | general-purpose | ✅ Some tasks |
| Phase 6 | code-implementation-specialist + general-purpose | - | ✅ Per page/doc |

---

### Quality Gates (Between All Phases)

Before proceeding to next phase:

```bash
# Build & Test
source /tmp/dotnet-proxy-setup.sh
dotnet build                        # Must succeed
dotnet test                         # Must pass 100%
dotnet format --verify-no-changes  # Must pass

# Functional validation
# - All phase success criteria met
# - No regressions

# Documentation
# - Code documented
# - Phase retrospective completed

# Approval
# - Ready to merge to main
```

---

## Expected Outcomes

### Metrics Improvement

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| **Projects** | 3 | 1 | -66% |
| **Terminals to run locally** | 3 | 1 | -66% |
| **Docker containers** | 2-3 | 1 | -66% |
| **Configuration files** | 9 | 3 | -66% |
| **Inline styles** | 5+ | 0 | -100% |
| **UI components** | 33 | 38 | +15% |
| **Manual mapping** | Yes | No (Mapperly) | 100% automated |
| **Page load (1000 tickets)** | ~3s | <500ms | -83% |
| **CI/CD build time** | ~5 min | ~2 min | -60% |

---

## How to Use These Documents

### For Orchestrator Agent

1. **Start**: Read `README.md` for full orchestrator instructions
2. **Each Phase**: Read corresponding `PHASE_XX_*.md` document
3. **Assign**: Use agent recommendations to assign to specialized agents
4. **Validate**: Use quality gates between phases
5. **Track**: Use progress tracking templates in README.md

### For Implementation Agents

1. **Receive**: Orchestrator assigns you a phase
2. **Read**: Read the phase document thoroughly (e.g., `PHASE_01_PROJECT_CONSOLIDATION.md`)
3. **Implement**: Follow step-by-step instructions
4. **Test**: Run all tests and validations
5. **Report**: Mark complete when validation checklist passes

### For Human Reviewers

1. **Planning**: Review README.md for epic overview
2. **Per Phase**: Review phase document before agent starts
3. **Validation**: Check quality gate results before approving merge
4. **Retrospective**: Review Phase 6 retrospective after completion

---

## File Locations

All documents in: `C:/code/github/PRFactory/docs/planning/epic_08_architecture_cleanup/`

```
epic_08_architecture_cleanup/
├── README.md                                    # Orchestrator guide (START HERE)
├── EPIC_SUMMARY.md                             # This file
├── Component-Architecture-Refactoring-Plan.md  # Original UI plan (reference)
├── PHASE_01_PROJECT_CONSOLIDATION.md           # Phase 1 task doc
├── PHASE_02_CSS_ISOLATION.md                   # Phase 2 task doc
├── PHASE_03_PAGINATION.md                      # Phase 3 task doc
├── PHASE_04_MISSING_UI_COMPONENTS.md           # Phase 4 task doc
├── PHASE_05_DTO_MAPPING.md                     # Phase 5 task doc
└── PHASE_06_FINAL_POLISH.md                    # Phase 6 task doc
```

Parent folder also contains:
- `EPIC_08_SYSTEM_ARCHITECTURE_CLEANUP.md` (original consolidation plan)

---

## Timeline Summary

| Week | Phase | Focus | Agent Type |
|------|-------|-------|------------|
| **Week 1** | Phase 1 | Project consolidation | code-implementation-specialist |
| Week 2 | Phase 2 | CSS isolation | code-implementation-specialist |
| Week 3 | Phase 3 | Server-side pagination | code-implementation-specialist |
| Week 4-5 | Phase 4 | Missing UI components | code-implementation-specialist |
| Week 6 | Phase 5 | DTO mapping | code-implementation-specialist |
| Week 7-8 | Phase 6 | Final polish & docs | code-implementation-specialist + general-purpose |

**Total**: 8-9 weeks (can be faster with parallel work in Phases 4-6)

---

## Success Criteria Summary

**Phase 1**: ✅ Single project, single `dotnet run`, single container

**Phase 2**: ✅ Zero inline styles, CSS isolation everywhere

**Phase 3**: ✅ Server-side pagination, <500ms page loads

**Phase 4**: ✅ 5 new components, used in 3+ pages each

**Phase 5**: ✅ Mapperly mapping, zero manual mapping

**Phase 6**: ✅ All pages refactored, docs updated

**Epic Complete**: ✅ All 6 phases merged, production stable

---

## Getting Started

**For Orchestrator**:
1. Read `README.md` first (orchestrator guide)
2. Review this summary for overview
3. Start Phase 1 assignment

**For Implementation Agent**:
1. Receive phase assignment from orchestrator
2. Read assigned phase document (e.g., `PHASE_01_*.md`)
3. Follow instructions step by step
4. Report completion with validation results

---

## Questions?

- **Technical questions**: Check phase documents and CLAUDE.md
- **Process questions**: Check README.md (orchestrator guide)
- **Architectural questions**: Check EPIC_08_SYSTEM_ARCHITECTURE_CLEANUP.md or Component-Architecture-Refactoring-Plan.md

---

**Last Updated**: 2025-11-14
**Status**: ✅ Ready for Implementation
**Next Step**: Orchestrator reads README.md and starts Phase 1

---

## Document Stats

- **Total documents**: 8 (1 orchestrator guide + 6 phase docs + 1 summary)
- **Total size**: ~165 KB
- **Total lines**: ~4,000 lines
- **Estimated reading time**: 2-3 hours (all documents)
- **Implementation time**: 8-9 weeks

---

**Good luck with EPIC 08!** 🚀
