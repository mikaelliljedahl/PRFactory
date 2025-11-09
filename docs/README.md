# PRFactory Documentation

Welcome to the PRFactory documentation! This guide will help you navigate all available documentation.

---

## 🚀 Quick Start

### Essential Documents (Read These First)

| Document | Purpose | Audience |
|----------|---------|----------|
| **[Main README](../README.md)** | Project overview and quick start | Everyone |
| **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** | What's built vs. planned | Everyone |
| **[SETUP.md](SETUP.md)** | Installation and configuration | Developers, DevOps |
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | System design and patterns | Developers, Architects |
| **[WORKFLOW.md](WORKFLOW.md)** | How workflows execute | Developers, Users |

### Key References

- **[CLAUDE.md](../CLAUDE.md)** - Architecture vision for AI agents (essential for contributors)
- **[ROADMAP.md](ROADMAP.md)** - Future enhancements and planned features
- **[Database Schema](database-schema.md)** - Entity relationships and data model

---

## 📁 Documentation Structure

```
PRFactory/
├── README.md                          # Main project overview (START HERE)
├── CLAUDE.md                          # Architecture vision for AI agents ⭐
│
├── docs/
│   ├── README.md                      # This file (documentation index)
│   │
│   ├── IMPLEMENTATION_STATUS.md       # ⭐ Single source of truth: What's built vs. planned
│   ├── ROADMAP.md                     # Future enhancements and vision
│   │
│   ├── SETUP.md                       # Installation and configuration guide
│   ├── ARCHITECTURE.md                # System architecture and design patterns
│   ├── WORKFLOW.md                    # Detailed workflow explanation
│   ├── database-schema.md             # Database schema documentation
│   │
│   ├── IMPLEMENTATION_SUMMARY.md      # Recent implementation summary
│   ├── REFINEMENT_ENHANCEMENT_PLAN.md # Refinement workflow enhancements
│   ├── DOCUMENTATION_RESTRUCTURE_PLAN.md # This restructuring plan
│   │
│   ├── architecture/                  # Component-specific architecture docs
│   │   ├── core-engine.md             # Core workflow engine details
│   │   ├── jira-integration.md        # Jira integration details
│   │   ├── git-integration.md         # Git integration details
│   │   ├── claude-integration.md      # Claude AI integration details
│   │   ├── cli-oauth-integration-analysis.md # CLI/OAuth integration analysis
│   │   ├── WORKFLOW_EXECUTION_ARCHITECTURE.md # Workflow execution deep-dive
│   │   ├── WORKFLOW_EXECUTION_CRITICAL_GAPS.md # Blocking implementation gaps
│   │   ├── WORKFLOW_EXECUTION_SUMMARY.md # Workflow execution summary
│   │   └── OAUTH_INTEGRATION_SOLUTION.md # OAuth integration solution (OrchestratorChat port)
│   │
│   ├── design/                        # Design documents
│   │   ├── team-review-design.md      # Team review feature design
│   │   ├── implementation-quality-loop.md # Quality loop design
│   │   └── implementation-quality-loop-addendum.md # Quality loop addendum
│   │
│   ├── reviews/                       # Architecture and code reviews
│   │   └── ARCHITECTURE_REVIEW.md     # 2025-11-09 Comprehensive architecture review ⚠️
│   │
│   ├── security/                      # Security documentation
│   │   ├── SECURITY_REVIEW.md         # Security vulnerability analysis
│   │   └── SECURITY_CHECKLIST.md      # Actionable security fixes
│   │
│   └── archive/                       # Historical/outdated documents
│       ├── ORIGINAL_PROPOSAL.md       # Initial Jira-first proposal (archived)
│       ├── IMPLEMENTATION_PLAN_WEB_UI.md # WebUI planning doc (archived)
│       ├── ARCHITECTURE_REVIEW_2025-11-07.md # Point-in-time review (archived)
│       └── AGENT_PROMPTS_INTEGRATION.md # Agent prompts impl guide (archived)
│
└── src/
    ├── PRFactory.Api/README.md        # API component documentation
    ├── PRFactory.Domain/README.md     # Domain layer documentation
    ├── PRFactory.Infrastructure/
    │   ├── README.md                  # Infrastructure overview
    │   ├── Agents/README.md           # Agent system documentation
    │   ├── Agents/Graphs/README.md    # Agent workflow graphs
    │   ├── Claude/README.md           # Claude client documentation
    │   └── Git/README.md              # Git service documentation
    └── PRFactory.Worker/README.md     # Background worker documentation
```

---

## 📖 Documentation by Type

### Current State (What Exists Today)

| Document | Status | Description |
|----------|--------|-------------|
| **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** | ⭐ **PRIMARY** | Single source of truth for what's built vs. planned |
| [ARCHITECTURE.md](ARCHITECTURE.md) | ✅ Current | System architecture and design patterns |
| [WORKFLOW.md](WORKFLOW.md) | ✅ Current | How workflows execute end-to-end |
| [SETUP.md](SETUP.md) | ✅ Current | Installation and configuration |
| [database-schema.md](database-schema.md) | ✅ Current | Database structure and entities |
| [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) | ✅ Current | Recent implementation summary |

### Future Vision & Planning

| Document | Status | Description |
|----------|--------|-------------|
| **[ROADMAP.md](ROADMAP.md)** | 📋 Planning | Future enhancements (3, 6, 12 month vision) |
| [REFINEMENT_ENHANCEMENT_PLAN.md](REFINEMENT_ENHANCEMENT_PLAN.md) | 📋 Planning | Planned refinement workflow improvements |

### Architecture Reviews & Analysis

| Document | Status | Description |
|----------|--------|-------------|
| **[reviews/ARCHITECTURE_REVIEW.md](reviews/ARCHITECTURE_REVIEW.md)** | ⚠️ **CRITICAL** | 2025-11-09 Comprehensive architecture review |
| [architecture/WORKFLOW_EXECUTION_ARCHITECTURE.md](architecture/WORKFLOW_EXECUTION_ARCHITECTURE.md) | 📋 Analysis | Workflow execution deep-dive |
| [architecture/WORKFLOW_EXECUTION_CRITICAL_GAPS.md](architecture/WORKFLOW_EXECUTION_CRITICAL_GAPS.md) | ⚠️ Gaps | Blocking implementation gaps |
| [architecture/OAUTH_INTEGRATION_SOLUTION.md](architecture/OAUTH_INTEGRATION_SOLUTION.md) | ✅ Solution | OAuth integration via OrchestratorChat port |
| [architecture/cli-oauth-integration-analysis.md](architecture/cli-oauth-integration-analysis.md) | 📋 Analysis | CLI/OAuth integration analysis |

### Security Documentation

| Document | Status | Description |
|----------|--------|-------------|
| **[security/SECURITY_REVIEW.md](security/SECURITY_REVIEW.md)** | 🔴 **CRITICAL** | Security vulnerability analysis (16 vulnerabilities) |
| [security/SECURITY_CHECKLIST.md](security/SECURITY_CHECKLIST.md) | ✅ Actionable | Security fixes with time estimates |

### AI Agent Guidance

| Document | Status | Description |
|----------|--------|-------------|
| **[CLAUDE.md](../CLAUDE.md)** | ⭐ **ESSENTIAL** | Architecture vision, what to preserve vs. simplify |
| [DOCUMENTATION_RESTRUCTURE_PLAN.md](DOCUMENTATION_RESTRUCTURE_PLAN.md) | 📋 Plan | This documentation reorganization plan |

### Historical (Archived)

| Document | Archive Date | Description |
|----------|--------------|-------------|
| [ORIGINAL_PROPOSAL.md](archive/ORIGINAL_PROPOSAL.md) | 2025-11-08 | Initial Jira-first proposal (now WebUI-first) |
| [IMPLEMENTATION_PLAN_WEB_UI.md](archive/IMPLEMENTATION_PLAN_WEB_UI.md) | 2025-11-08 | WebUI transition plan (now implemented) |
| [ARCHITECTURE_REVIEW_2025-11-07.md](archive/ARCHITECTURE_REVIEW_2025-11-07.md) | 2025-11-08 | Point-in-time architecture review |
| [AGENT_PROMPTS_INTEGRATION.md](archive/AGENT_PROMPTS_INTEGRATION.md) | 2025-11-08 | Agent prompts system guide (now complete) |

---

## 👥 Documentation by Role

### For New Users

**Goal**: Understand what PRFactory does and how to use it

1. **[Main README](../README.md)** - What is PRFactory?
2. **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** - What works today?
3. **[WORKFLOW.md](WORKFLOW.md)** - How does it work end-to-end?
4. **[SETUP.md](SETUP.md)** - How do I run it?

### For Developers

**Goal**: Understand codebase and contribute features

1. **[CLAUDE.md](../CLAUDE.md)** - Architecture vision (READ THIS FIRST!)
2. **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** - What's built vs. planned
3. **[ARCHITECTURE.md](ARCHITECTURE.md)** - System design and patterns
4. **[Database Schema](database-schema.md)** - Data model
5. **[Component READMEs](../src/)** - Deep dive into specific components
6. **[Architecture Details](architecture/)** - Component-specific docs

**Key Resources**:
- [Workflow Graphs](../src/PRFactory.Infrastructure/Agents/Graphs/README.md)
- [Agent System](../src/PRFactory.Infrastructure/Agents/README.md)
- [Git Integration](../src/PRFactory.Infrastructure/Git/README.md)

### For DevOps/Operators

**Goal**: Deploy and operate PRFactory

1. **[SETUP.md](SETUP.md)** - Installation options (Docker, local)
2. **[ARCHITECTURE - Deployment](ARCHITECTURE.md#deployment-architecture)** - Deployment strategies
3. **[SETUP - Troubleshooting](SETUP.md#troubleshooting)** - Common issues
4. **[Database Schema](database-schema.md)** - Database setup

### For Architects

**Goal**: Review design decisions and patterns

1. **[CLAUDE.md](../CLAUDE.md)** - Architecture philosophy and vision
2. **[ARCHITECTURE.md](ARCHITECTURE.md)** - System design
3. **[ARCHITECTURE - Patterns](ARCHITECTURE.md#architecture-patterns)** - Design patterns
4. **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** - Current vs. planned
5. **[ROADMAP.md](ROADMAP.md)** - Future vision

### For Product Managers

**Goal**: Understand features and roadmap

1. **[Main README](../README.md)** - Product overview
2. **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** - Features status
3. **[ROADMAP.md](ROADMAP.md)** - Future enhancements
4. **[WORKFLOW.md](WORKFLOW.md)** - User workflows

---

## 🏗️ Architecture Deep Dives

### Core Components

| Component | Document | Description |
|-----------|----------|-------------|
| **Workflow Engine** | [core-engine.md](architecture/core-engine.md) | State machine, transitions, checkpoints |
| **Jira Integration** | [jira-integration.md](architecture/jira-integration.md) | Webhooks, HMAC validation, comments |
| **Git Integration** | [git-integration.md](architecture/git-integration.md) | Multi-platform, LibGit2Sharp, PRs |
| **Claude AI** | [claude-integration.md](architecture/claude-integration.md) | API client, prompts, context building |

### Component Documentation

| Layer | Location | Description |
|-------|----------|-------------|
| **API Layer** | [src/PRFactory.Api/README.md](../src/PRFactory.Api/README.md) | REST endpoints, controllers, webhooks |
| **Domain Layer** | [src/PRFactory.Domain/README.md](../src/PRFactory.Domain/README.md) | Entities, value objects, domain logic |
| **Infrastructure** | [src/PRFactory.Infrastructure/README.md](../src/PRFactory.Infrastructure/README.md) | External integrations (Jira, Git, Claude, DB) |
| **Worker Service** | [src/PRFactory.Worker/README.md](../src/PRFactory.Worker/README.md) | Background jobs, workflow orchestration |

**Infrastructure Subsystems**:
- **[Agents](../src/PRFactory.Infrastructure/Agents/README.md)** - 15+ specialized workflow agents
- **[Workflow Graphs](../src/PRFactory.Infrastructure/Agents/Graphs/README.md)** - RefinementGraph, PlanningGraph, ImplementationGraph
- **[Claude](../src/PRFactory.Infrastructure/Claude/README.md)** - Claude AI client and prompts
- **[Git](../src/PRFactory.Infrastructure/Git/README.md)** - Git operations and platform integrations

---

## ❓ Getting Help

### Quick Answers

**Q: What's actually implemented vs. planned?**
A: See **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** - the single source of truth.

**Q: How do I get started?**
A: Read the [Main README](../README.md), then follow the [Setup Guide](SETUP.md).

**Q: How does the workflow work?**
A: See the [Workflow Documentation](WORKFLOW.md) with detailed diagrams.

**Q: What's the system architecture?**
A: Review the [Architecture Documentation](ARCHITECTURE.md).

**Q: What's planned for the future?**
A: Check the [Roadmap](ROADMAP.md) for short, medium, and long-term vision.

**Q: I'm a developer - where do I start?**
A: Read [CLAUDE.md](../CLAUDE.md) first, then [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md).

**Q: How do I troubleshoot issues?**
A: Check the [Troubleshooting Section](SETUP.md#troubleshooting) in the setup guide.

**Q: Where are the API endpoints?**
A: See [src/PRFactory.Api/README.md](../src/PRFactory.Api/README.md).

**Q: How do I add a new agent?**
A: See [Agent Documentation](../src/PRFactory.Infrastructure/Agents/README.md).

**Q: Why is this architecture so complex?**
A: Read [CLAUDE.md](../CLAUDE.md) - it explains what's INTENTIONAL vs. overengineered.

### Still Need Help?

- Check the logs (Serilog output)
- Review relevant component README
- Open an issue on GitHub
- Check Jira webhook logs for webhook issues

---

## 🤝 Contributing to Documentation

### When to Update Docs

**Update immediately**:
- ✅ When adding new features
- ✅ When changing architecture
- ✅ When fixing bugs that affect documented behavior
- ✅ When adding new components

**Update weekly**:
- ✅ **[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)** - Keep status current
- ✅ Component READMEs - Reflect code changes

**Update monthly**:
- ✅ **[ROADMAP.md](ROADMAP.md)** - Review priorities and timelines

### Documentation Standards

**General Guidelines**:
1. **Keep it current** - Update docs when code changes
2. **Link liberally** - Cross-reference related docs
3. **Use diagrams** - Mermaid diagrams for workflows and architecture
4. **Provide examples** - Code samples and walkthroughs
5. **Update this index** - Add new docs to this README

**Formatting**:
- Use Markdown (.md files)
- Include table of contents for long documents (>100 lines)
- Use Mermaid for diagrams (flowcharts, sequence, state)
- Link to code files with line numbers where relevant
- Keep language clear and concise

**Status Indicators**:
- ✅ **COMPLETE** - Fully implemented and tested
- ⚠️ **PARTIAL** - Implemented but incomplete
- 🚧 **IN PROGRESS** - Currently being worked on
- 📋 **PLANNED** - Designed but not started
- ❌ **NOT PLANNED** - Not in roadmap

### Archiving Documents

When a document becomes outdated:

1. Move to `/docs/archive/`
2. Add archive header with:
   - Archive date
   - Reason for archiving
   - Link to current documentation
3. Update references in other docs
4. Update this README

---

## 📊 Documentation Health

**Last Major Update**: 2025-11-08

### Coverage Status

| Area | Status | Notes |
|------|--------|-------|
| Architecture | ✅ Complete | Well documented with diagrams |
| Setup/Installation | ✅ Complete | Docker and local setup |
| Workflow | ✅ Complete | Detailed with sequence diagrams |
| API | ⚠️ Partial | Needs OpenAPI/Swagger docs |
| Testing | ❌ Missing | No testing guide yet |
| Troubleshooting | ⚠️ Partial | Basic troubleshooting only |

### Documentation Metrics

- **Total Documents**: 20+ markdown files
- **Core Documentation**: 10 files (current)
- **Archived Documents**: 4 files (historical)
- **Component READMEs**: 8+ files

---

**Maintained By**: PRFactory Development Team
**Review Frequency**: Weekly
**Last Reviewed**: 2025-11-08
**Next Review**: 2025-11-15

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ⭐ | Essential/Primary document |
| ✅ | Current and accurate |
| ⚠️ | Partial or needs updates |
| 🚧 | In progress |
| 📋 | Planning/Future |
| ❌ | Missing or not planned |
