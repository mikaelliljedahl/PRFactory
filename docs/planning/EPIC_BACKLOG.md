# Epic Backlog - PRFactory

> **Purpose:** Master roadmap for AI agents. Shows what to build next, in what order, and why.

**Last Updated:** 2025-11-09

---

## How to Use This Backlog

1. **Start at Prio 1** - These are the next critical features to implement
2. **Follow dependencies** - Some epics depend on others being completed first
3. **Read the epic details** - Each epic has a detailed implementation plan in its own file
4. **Track progress** - Update status as epics are completed

---

## Priority 1: CRITICAL (Implement Next)

### Epic 1: Team Review & Collaboration
**Status:** 🔴 Not Started
**Effort:** 2-3 weeks
**Owner:** To be assigned
**File:** [EPIC_01_TEAM_REVIEW.md](./EPIC_01_TEAM_REVIEW.md)

**Goal:** Transform PRFactory from single-player to multi-player. Enable team collaboration on AI-generated plans.

**Key Features:**
- Threaded commenting system for plan discussions
- Formal approval workflow (Draft → PendingReview → ChangesRequested → Approved)
- Enhanced `cli review` agent for plan validation
- Code-vs-plan validation before PR creation

**Why This Matters:**
Our biggest weakness is the "single-player" experience. Teams need to collaborate on AI-generated plans before implementation.

**Dependencies:** None - can start immediately

**Deliverables:**
- [ ] `PlanComments` database table and API endpoints
- [ ] Blazor commenting UI component with threading
- [ ] Plan approval workflow state machine
- [ ] `cli review --plan` command for plan analysis
- [ ] `cli review --validate` command for code-vs-plan validation

---

### Epic 2: AI Agnosticism (Multi-LLM Support)
**Status:** 🟡 Partially Complete (Claude Code CLI only)
**Effort:** 2-3 weeks
**Owner:** To be assigned
**File:** [EPIC_02_MULTI_LLM.md](./EPIC_02_MULTI_LLM.md)

**Goal:** Remove vendor lock-in to Claude AI. Support multiple LLM providers (Anthropic, OpenAI, Google).

**Key Features:**
- Global CLI parameters: `--provider anthropic|openai|google` and `--model <model-name>`
- `ILlmProvider` interface and `LlmProviderFactory`
- Externalized prompt templates (different prompts for different models)
- Support for Claude Code CLI, Gemini CLI, OpenAI CLI

**Why This Matters:**
Critical technical debt. We're locked to Claude AI. Customers want choice (GPT-4, Gemini, etc.).

**Dependencies:** None - can start immediately (runs parallel to Epic 1)

**Deliverables:**
- [ ] `ILlmProvider` interface and factory pattern
- [ ] `BaseAgentOptions` with `--provider` and `--model` flags
- [ ] Prompt template system: `/prompts/{agent}/{provider}/system.txt`
- [ ] `ClaudeCodeCliAdapter` (already exists, enhance)
- [ ] `GeminiCliAdapter` (new)
- [ ] `OpenAiCliAdapter` (new)
- [ ] Provider selection configuration

---

## Priority 2: IMPORTANT (After Prio 1)

### Epic 3: Deeper Planning Phase (MetaGPT-Inspired)
**Status:** 🔴 Not Started
**Effort:** 2-3 weeks
**Owner:** To be assigned
**File:** [EPIC_03_DEEP_PLANNING.md](./EPIC_03_DEEP_PLANNING.md)

**Goal:** Make Phase 2 (Planning) the core value proposition. Generate comprehensive, multi-artifact plans.

**Key Features:**
- Multi-artifact plan generation (user stories, API design, database schema, test cases, implementation steps)
- `cli plan` orchestrator with multiple LLM personas (PM, Architect, QA, Tech Lead)
- `cli revise` command for iterative plan refinement
- Web UI to display all plan artifacts

**Why This Matters:**
Differentiation from simple code generators. PRFactory creates enterprise-grade plans that teams can review and approve.

**Dependencies:**
- ✅ Requires Epic 1 (team review) to be useful
- ⚠️ Should wait for Epic 2 (multi-LLM) to avoid Claude lock-in

**Deliverables:**
- [ ] `cli plan` orchestrator with 4-step persona workflow
- [ ] Plan artifact templates (user stories, API design, schema, test cases)
- [ ] `cli revise` command for plan iteration
- [ ] Web UI multi-artifact viewer
- [ ] Plan storage and versioning

---

### Epic 4: Web-based Git Visualization
**Status:** 🔴 Not Started
**Effort:** 1-2 weeks
**Owner:** To be assigned
**File:** [EPIC_04_DIFF_VIEWER.md](./EPIC_04_DIFF_VIEWER.md)

**Goal:** Create a standalone web experience. Users never leave PRFactory UI to review code changes.

**Key Features:**
- `cli code` outputs structured `diff.patch` artifact
- Web UI diff viewer component (using diff2html library)
- Side-by-side or unified diff display
- "Approve and Create Pull Request" workflow entirely in UI

**Why This Matters:**
"Warp-like" UX. Keep users in our ecosystem from ticket analysis → plan review → code review → PR creation.

**Dependencies:**
- ✅ Requires Epic 1 (approval workflow) to be complete
- ⚠️ Optionally waits for Epic 3 (deeper planning) for best UX

**Deliverables:**
- [ ] `cli code` generates `workspace/{task-id}/diff.patch`
- [ ] `GET /api/tasks/{id}/diff` API endpoint
- [ ] Blazor diff viewer component (diff2html integration)
- [ ] "Approve and Create PR" button in UI
- [ ] Automated PR creation with plan artifacts in description

---

## Future Work: DEFERRED (After Prio 2)

### Epic 5: Microsoft Agent Framework Integration
**Status:** 🔴 Not Started (Research Needed)
**Effort:** 2-3 weeks (after PoC)
**Owner:** To be assigned
**File:** [EPIC_05_AGENT_FRAMEWORK.md](./EPIC_05_AGENT_FRAMEWORK.md)

**Goal:** Enable autonomous agentic workflows with tool use (file operations, git, Jira API).

**Key Features:**
- Microsoft Semantic Kernel integration
- Tool plugins (FileToolPlugin, GitToolPlugin, JiraToolPlugin)
- Direct Anthropic Messages API calls (bypass CLI)
- Multi-turn conversations with memory

**Why This Matters:**
Advanced autonomous workflows. Agents can use tools to analyze code, commit changes, post to Jira without explicit commands.

**Dependencies:**
- ✅ Should wait for Epic 2 (multi-LLM) to be complete
- ⚠️ Requires Epic 1 (approval workflow) for safety (human review gates)
- ⚠️ Research phase needed: Does Agent Framework add value over CLI approach?

**Decision Point:**
After 1-week PoC, decide if framework adds sufficient value vs. current CLI architecture.

**Deliverables:**
- [ ] PoC: Agent Framework + tool use demonstration
- [ ] Decision: Proceed or defer?
- [ ] If proceed: `AnthropicApiClient` (direct Messages API)
- [ ] If proceed: Tool plugins and kernel configuration
- [ ] If proceed: Update workflow graphs to use agents

---

## Epic Dependency Graph

```
┌─────────────────────────────────────────────────────────┐
│  PRIO 1 (Parallel - Start Immediately)                  │
│                                                           │
│  Epic 1: Team Review    Epic 2: Multi-LLM Support       │
│  └─────────┬─────────┘  └──────────┬──────────┘         │
└────────────┼────────────────────────┼────────────────────┘
             │                        │
             ▼                        ▼
┌─────────────────────────────────────────────────────────┐
│  PRIO 2 (Sequential - After Prio 1)                     │
│                                                           │
│  Epic 3: Deep Planning  Epic 4: Diff Viewer             │
│    (Requires Epic 1+2)    (Requires Epic 1)             │
│  └─────────┬─────────┘  └──────────┬──────────┘         │
└────────────┼────────────────────────┼────────────────────┘
             │                        │
             └────────┬───────────────┘
                      ▼
┌─────────────────────────────────────────────────────────┐
│  FUTURE (After Prio 2 + PoC Decision)                   │
│                                                           │
│  Epic 5: Agent Framework                                 │
│    (Research → PoC → Decide → Implement)                │
└─────────────────────────────────────────────────────────┘
```

---

## Quick Start Guide for AI Agents

**If you're an AI agent starting work, follow this process:**

1. **Check Prio 1 Epics**
   - Read [EPIC_01_TEAM_REVIEW.md](./EPIC_01_TEAM_REVIEW.md)
   - Read [EPIC_02_MULTI_LLM.md](./EPIC_02_MULTI_LLM.md)
   - These can be worked on in parallel by different agents

2. **Choose Your Epic**
   - Pick based on your assignment or team priority
   - If no assignment, default to Epic 1 (Team Review)

3. **Read the Epic Details**
   - Each epic file has full implementation plan
   - Actionable tasks broken down
   - Files to create/modify listed

4. **Implement**
   - Follow the tasks in order
   - Update the epic status as you go
   - Mark deliverables complete

5. **Update This Backlog**
   - Change epic status (🔴 Not Started → 🟡 In Progress → 🟢 Complete)
   - Update "Last Updated" date
   - Note any blockers or changes

---

## Status Legend

- 🔴 **Not Started** - Epic not yet begun
- 🟡 **In Progress** - Work has started but not complete
- 🟢 **Complete** - Epic fully implemented and merged
- ⏸️ **Blocked** - Waiting on dependencies or decision
- ❌ **Cancelled** - Epic deprioritized or no longer needed

---

## Related Documentation

- [Implementation Status](../IMPLEMENTATION_STATUS.md) - Overall project status
- [Roadmap](../ROADMAP.md) - High-level product roadmap
- [Architecture](../ARCHITECTURE.md) - System architecture overview
- [CLAUDE.md](../../CLAUDE.md) - Guidelines for AI agents working on codebase

---

## Contact

Questions about the backlog? Check these resources first:
- Read the individual epic files for detailed plans
- Review [Implementation Status](../IMPLEMENTATION_STATUS.md) for current state
- Check [CLAUDE.md](../../CLAUDE.md) for architectural decisions (what NOT to change)

**Remember:** This backlog is written by AI for AI. Keep it clear, actionable, and up-to-date.
