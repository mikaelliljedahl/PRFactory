# PRFactory Documentation Restructure Plan

**Date**: 2025-11-08
**Purpose**: Clearly distinguish implemented features from planned enhancements
**Status**: Proposed

---

## Problem Statement

Current documentation mixes three types of content without clear distinction:

1. **Implemented Features** - Code that exists and works
2. **Planned Enhancements** - Future roadmap items
3. **Historical Context** - Earlier design iterations and proposals

This makes it difficult for developers to know what's actually built vs. what's planned.

---

## Proposed Documentation Structure

### 1. ROOT LEVEL DOCUMENTATION

#### README.md (KEEP - UPDATE)
**Purpose**: Project overview and quick start
**Changes Needed**:
- ✅ Add implementation status badges
- ✅ Update agent count (15+ implemented, not "14 specialized")
- ✅ Add "What Works Today" vs "Roadmap" sections
- ✅ Clarify WebUI-first approach (not Jira-first)
- ❌ Remove ambiguous claims about features

**New Structure**:
```markdown
# PRFactory

## What Works Today ✅
- Multi-graph workflow orchestration (RefinementGraph, PlanningGraph, ImplementationGraph)
- Multi-platform Git support (GitHub, Bitbucket, Azure DevOps)
- 15+ specialized agents
- Web UI for ticket management
- Checkpoint-based workflow resumption
- Multi-tenant isolation

## In Progress 🚧
- Comprehensive test suite
- Web UI polish and additional features

## Planned 🔮
- GitLab provider integration
- Advanced approval workflows
- Real-time WebUI updates with SignalR
```

#### CLAUDE.md (KEEP - MINIMAL UPDATES)
**Purpose**: Architecture vision and AI agent guidance
**Status**: Already excellent (rated 10/10)
**Changes Needed**:
- ✅ Update "What IS Overengineered" section with current stub removal status
- ✅ Add reference to IMPLEMENTATION_STATUS.md (new file)
- ❌ Keep all architectural guidance intact

#### ARCHITECTURE_REVIEW.md (ARCHIVE)
**Action**: Move to `/docs/archive/`
**Reason**: Historical snapshot from 2025-11-07, valuable but will become outdated
**New Location**: `/docs/archive/ARCHITECTURE_REVIEW_2025-11-07.md`

---

### 2. CORE DOCUMENTATION (/docs/)

#### NEW: IMPLEMENTATION_STATUS.md
**Purpose**: Single source of truth for what's built vs. planned
**Location**: `/docs/IMPLEMENTATION_STATUS.md`

**Content Structure**:
```markdown
# Implementation Status

Last Updated: 2025-11-08

## Status Legend
- ✅ **COMPLETE** - Fully implemented and functional
- ⚠️ **PARTIAL** - Implemented but incomplete or needs polish
- 🚧 **IN PROGRESS** - Currently being worked on
- 📋 **PLANNED** - Designed but not yet started
- ❌ **NOT PLANNED** - Not in current roadmap

## Core Components

### Workflow Engine
| Component | Status | Completeness | Notes |
|-----------|--------|--------------|-------|
| RefinementGraph | ✅ COMPLETE | 100% | 240 lines, full retry logic |
| PlanningGraph | ✅ COMPLETE | 100% | 280 lines, parallel execution |
| ImplementationGraph | ✅ COMPLETE | 100% | 213 lines, conditional execution |
| WorkflowOrchestrator | ✅ COMPLETE | 100% | 443 lines, event-driven transitions |

### Git Platform Providers
| Provider | Status | Completeness | Notes |
|----------|--------|--------------|-------|
| GitHub | ✅ COMPLETE | 100% | Octokit integration, full retry policy |
| Bitbucket | ✅ COMPLETE | 100% | REST API, complete implementation |
| Azure DevOps | ✅ COMPLETE | 100% | Official SDK, full feature set |
| GitLab | 📋 PLANNED | 0% | Architecture ready, not implemented |

### Agent System
| Agent Type | Status | Count | Notes |
|------------|--------|-------|-------|
| Workflow Agents | ✅ COMPLETE | 15+ | All inherit from BaseAgent |
| Analysis Agent | ✅ COMPLETE | 1 | Codebase analysis with retry logic |
| Planning Agent | ✅ COMPLETE | 1 | Implementation planning |
| Question Generation | ✅ COMPLETE | 1 | Clarifying questions |
| Implementation Agent | ✅ COMPLETE | 1 | Optional code generation |

### Infrastructure
| Feature | Status | Completeness | Notes |
|---------|--------|--------------|-------|
| Multi-tenant isolation | ✅ COMPLETE | 100% | TenantId in all entities, global filters |
| Checkpoint system | ✅ COMPLETE | 100% | Entity, repository, graph integration |
| AES-256 encryption | ✅ COMPLETE | 100% | AES-GCM for credentials |
| LibGit2Sharp integration | ✅ COMPLETE | 100% | LocalGitService wrapper |
| Event publishing | ✅ COMPLETE | 100% | WorkflowEvents with TPH inheritance |

### User Interface
| Component | Status | Completeness | Notes |
|-----------|--------|--------------|-------|
| Pure UI components (/UI/*) | ✅ COMPLETE | 100% | 8 reusable components (416 lines) |
| Business components | ⚠️ PARTIAL | 80% | Core components done, polish needed |
| Pages (Tickets) | ⚠️ PARTIAL | 80% | Index, Detail with code-behind |
| Real-time updates (SignalR) | 📋 PLANNED | 0% | Polling works, real-time planned |

### Testing
| Area | Status | Coverage | Notes |
|------|--------|----------|-------|
| Unit tests | 🚧 IN PROGRESS | 0% | Framework configured, no tests written |
| Integration tests | 🚧 IN PROGRESS | 0% | Test project scaffolded |
| E2E tests | 📋 PLANNED | 0% | Not started |

### Documentation
| Document | Status | Accuracy | Notes |
|----------|--------|----------|-------|
| CLAUDE.md | ✅ COMPLETE | 100% | Excellent architectural guidance |
| ARCHITECTURE.md | ⚠️ PARTIAL | 85% | Needs status updates, minor inconsistencies |
| WORKFLOW.md | ⚠️ PARTIAL | 85% | Needs WebUI-first clarification |
| SETUP.md | ✅ COMPLETE | 95% | Accurate setup instructions |

## Architectural Gaps

### Critical (Blocking Production)
- None identified - core architecture is complete

### Important (Needed for Production)
- Comprehensive test suite (unit + integration)
- GitLab provider (if multi-platform is selling point)
- Performance testing under load

### Nice to Have (Post-MVP)
- Real-time WebUI updates with SignalR
- Advanced approval workflows
- A/B testing workflows
- Deployment graph for CI/CD

## What Changed Since Initial Design

1. **WebUI became primary** (was Jira-first in ORIGINAL_PROPOSAL.md)
2. **Agent count increased** from planned 14 to 15+ implemented
3. **State machine expanded** from documented 12 states to 17 (improvement)
4. **Checkpoint system fully built** (was described as planned)
5. **All graphs implemented** (were described as architecture-only)

## References
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Detailed architecture
- [WORKFLOW.md](./WORKFLOW.md) - Workflow details
- [ROADMAP.md](./ROADMAP.md) - Future enhancements (new)
```

#### UPDATE: ARCHITECTURE.md
**Changes Needed**:
- ✅ Add status indicators to each section
- ✅ Update state count (12 → 17 states)
- ✅ Reference IMPLEMENTATION_STATUS.md for current status
- ✅ Move "Future Enhancements" to ROADMAP.md

**Add to top of file**:
```markdown
> **Implementation Status**: See [IMPLEMENTATION_STATUS.md](./IMPLEMENTATION_STATUS.md) for what's currently built vs. planned.
```

#### UPDATE: WORKFLOW.md
**Changes Needed**:
- ✅ Clarify WebUI as primary trigger (not just Jira webhooks)
- ✅ Update sequence diagrams to show WebUI first, Jira optional
- ✅ Add "Current Implementation" vs "Future Enhancement" sections

**New section to add**:
```markdown
## Current Implementation vs Future Enhancements

### ✅ Currently Implemented
- WebUI as primary ticket creation interface
- Checkpoint-based workflow suspension/resume
- Parallel execution (GitPlan + JiraPost, PullRequest + JiraPost)
- Multi-platform PR creation
- Event-driven graph transitions

### 📋 Planned Enhancements
- Real-time status updates via SignalR
- @claude mentions in external systems (Jira, GitHub Issues)
- Advanced approval workflows with multiple reviewers
- A/B testing of implementation strategies
```

#### NEW: ROADMAP.md
**Purpose**: Clear future vision separated from current state
**Location**: `/docs/ROADMAP.md`

**Content**:
```markdown
# PRFactory Roadmap

This document outlines planned enhancements beyond the current MVP implementation.

## Short Term (Next 3 Months)

### Testing & Quality
- [ ] Comprehensive unit test suite (target: 80% coverage)
- [ ] Integration tests for all graphs
- [ ] E2E tests for complete workflows

### Platform Completion
- [ ] GitLab provider implementation
- [ ] GitHub Issues integration
- [ ] Azure DevOps Work Items integration

### UI/UX Polish
- [ ] Real-time status updates with SignalR
- [ ] Improved error messaging
- [ ] Mobile-responsive design
- [ ] Dark mode support

## Medium Term (3-6 Months)

### Advanced Workflows
- [ ] Multi-stage approval workflows
- [ ] Code review graph integration
- [ ] Automated testing graph
- [ ] Deployment orchestration graph

### Enterprise Features
- [ ] SSO/SAML authentication
- [ ] Audit logging dashboard
- [ ] Advanced tenant configuration UI
- [ ] Usage analytics and reporting

### Developer Experience
- [ ] CLI tool for local testing
- [ ] Agent development toolkit
- [ ] Custom agent templates
- [ ] Workflow visualization tools

## Long Term (6-12 Months)

### Advanced AI Capabilities
- [ ] A/B testing of implementation strategies
- [ ] Automated code quality assessment
- [ ] Performance optimization suggestions
- [ ] Security vulnerability scanning

### Platform Expansion
- [ ] Self-hosted deployment options
- [ ] Kubernetes operator
- [ ] On-premises air-gapped deployment
- [ ] Custom LLM provider support (beyond Claude)

### Ecosystem
- [ ] Marketplace for custom agents
- [ ] Workflow template library
- [ ] Integration with more platforms (Asana, Linear, etc.)
- [ ] API for third-party extensions

## Research & Exploration

### Under Investigation
- Advanced context building with vector embeddings
- Multi-repository change orchestration
- Continuous learning from approved implementations
- Graph optimization with RL

### Ideas for Future
- Visual workflow designer
- Natural language workflow configuration
- Automated documentation generation from code
- Test generation from specifications

## What We're NOT Planning

- Full autonomous deployment (humans always approve)
- Code generation without human review
- Direct access to production environments
- Bypassing security policies

---

**Note**: This roadmap is subject to change based on customer feedback and priorities.
```

---

### 3. ARCHIVE OUTDATED DOCUMENTS

#### Move to /docs/archive/

1. **ORIGINAL_PROPOSAL.md** → `/docs/archive/ORIGINAL_PROPOSAL.md`
   - **Reason**: Historical, describes Jira-first approach (now WebUI-first)
   - **Add header**:
     ```markdown
     > **ARCHIVED**: This proposal described the initial Jira-first design.
     > The current implementation uses WebUI as primary interface.
     > See [ARCHITECTURE.md](../ARCHITECTURE.md) for current design.
     ```

2. **IMPLEMENTATION_PLAN_WEB_UI.md** → `/docs/archive/IMPLEMENTATION_PLAN_WEB_UI_2025-11-XX.md`
   - **Reason**: Planning document for WebUI transition (now implemented)
   - **Add header**:
     ```markdown
     > **ARCHIVED**: This plan guided the WebUI implementation transition.
     > The WebUI is now the primary interface. See [IMPLEMENTATION_STATUS.md](../IMPLEMENTATION_STATUS.md).
     ```

3. **ARCHITECTURE_REVIEW.md** → `/docs/archive/ARCHITECTURE_REVIEW_2025-11-07.md`
   - **Reason**: Point-in-time snapshot, will become outdated
   - **Add header**:
     ```markdown
     > **ARCHIVED**: Snapshot from 2025-11-07. For current status, see [IMPLEMENTATION_STATUS.md](../IMPLEMENTATION_STATUS.md).
     ```

4. **AGENT_PROMPTS_INTEGRATION.md** → `/docs/archive/AGENT_PROMPTS_INTEGRATION.md`
   - **Reason**: Implementation guide for completed feature
   - **Alternative**: Could move relevant parts to ARCHITECTURE.md and archive
   - **Add header**:
     ```markdown
     > **ARCHIVED**: This guided the agent prompts system implementation (now complete).
     > See [ARCHITECTURE.md](../ARCHITECTURE.md) section on Agent Prompts.
     ```

---

### 4. COMPONENT README FILES

#### Pattern for All Component READMEs

Add status badges to top of each:

```markdown
# [Component Name]

**Status**: ✅ Complete | ⚠️ Partial | 🚧 In Progress | 📋 Planned

**Last Updated**: 2025-11-08

---
```

**Files to update**:
- `/src/PRFactory.Api/README.md`
- `/src/PRFactory.Domain/README.md`
- `/src/PRFactory.Infrastructure/README.md`
- `/src/PRFactory.Infrastructure/Agents/README.md`
- `/src/PRFactory.Infrastructure/Agents/Graphs/README.md`
- `/src/PRFactory.Infrastructure/Claude/README.md`
- `/src/PRFactory.Infrastructure/Git/README.md`
- `/src/PRFactory.Worker/README.md`

---

## Implementation Priority

### Phase 1: Quick Wins (Immediate)
1. ✅ Create `/docs/IMPLEMENTATION_STATUS.md`
2. ✅ Update root README.md with status sections
3. ✅ Create `/docs/archive/` directory
4. ✅ Move outdated docs to archive with headers

### Phase 2: Documentation Updates (This Week)
1. ✅ Create `/docs/ROADMAP.md`
2. ✅ Update ARCHITECTURE.md with status references
3. ✅ Update WORKFLOW.md with implementation status
4. ✅ Update CLAUDE.md stub removal status

### Phase 3: Component Documentation (Next Week)
1. ✅ Add status badges to all component READMEs
2. ✅ Update component docs with current state
3. ✅ Verify all cross-references work

### Phase 4: Ongoing Maintenance
1. ✅ Update IMPLEMENTATION_STATUS.md as features complete
2. ✅ Move completed roadmap items to IMPLEMENTATION_STATUS.md
3. ✅ Archive planning docs after implementation

---

## Success Metrics

**Before Restructure**:
- ❓ Unclear what's built vs. planned
- 📚 Outdated docs mixed with current
- 🔍 Hard to find implementation status

**After Restructure**:
- ✅ Single source of truth: IMPLEMENTATION_STATUS.md
- 📁 Historical docs clearly archived
- 🎯 Roadmap separate from current state
- 🚀 New contributors know what to work on

---

## Migration Checklist

- [ ] Create `/docs/archive/` directory
- [ ] Create `IMPLEMENTATION_STATUS.md`
- [ ] Create `ROADMAP.md`
- [ ] Update root `README.md`
- [ ] Update `CLAUDE.md`
- [ ] Update `ARCHITECTURE.md`
- [ ] Update `WORKFLOW.md`
- [ ] Move `ORIGINAL_PROPOSAL.md` to archive
- [ ] Move `IMPLEMENTATION_PLAN_WEB_UI.md` to archive
- [ ] Move `ARCHITECTURE_REVIEW.md` to archive
- [ ] Move `AGENT_PROMPTS_INTEGRATION.md` to archive
- [ ] Add status badges to component READMEs
- [ ] Update `/docs/README.md` with new structure
- [ ] Test all cross-references and links
- [ ] Commit changes with clear message

---

## Questions to Resolve

1. **Test Coverage Target**: What's acceptable coverage for MVP? (Suggested: 80%)
2. **GitLab Priority**: Is GitLab provider needed for MVP or can it wait?
3. **WebUI Polish**: What specific UI features are blockers vs nice-to-have?
4. **Documentation Freeze**: Should we freeze CLAUDE.md as architectural guidance?

---

**Author**: Claude
**Review Status**: Awaiting human approval
**Next Step**: Implement Phase 1 (Quick Wins)
