# PRFactory Documentation

Welcome to the PRFactory documentation! This guide will help you navigate all available documentation.

## Quick Links

- **[Main README](../README.md)** - Start here! Overview, quick start, and introduction
- **[Setup Guide](SETUP.md)** - Complete installation and configuration instructions
- **[Architecture](ARCHITECTURE.md)** - System design, components, and patterns
- **[Workflow](WORKFLOW.md)** - Detailed workflow with sequence diagrams
- **[Database Schema](database-schema.md)** - Database structure and entity relationships

## Documentation Structure

```
PRFactory/
├── README.md                          # Main project overview (START HERE)
├── docs/
│   ├── README.md                      # This file (documentation index)
│   ├── SETUP.md                       # Installation and configuration guide
│   ├── ARCHITECTURE.md                # System architecture and design
│   ├── WORKFLOW.md                    # Detailed workflow explanation
│   ├── database-schema.md             # Database schema documentation
│   ├── ORIGINAL_PROPOSAL.md           # Original project proposal (historical)
│   └── architecture/                  # Component-specific architecture docs
│       ├── core-engine.md             # Core workflow engine details
│       ├── jira-integration.md        # Jira integration details
│       ├── git-integration.md         # Git integration details
│       └── claude-integration.md      # Claude AI integration details
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

## Documentation by Role

### For New Users

Start with these documents in order:

1. **[Main README](../README.md)** - Understand what PRFactory does
2. **[Workflow](WORKFLOW.md)** - See how it works end-to-end
3. **[Setup Guide](SETUP.md)** - Get it running

### For Developers

Explore these for development:

1. **[Architecture](ARCHITECTURE.md)** - Understand system design
2. **[Database Schema](database-schema.md)** - Learn data model
3. **[Component READMEs](../src/)** - Deep dive into specific components
4. **[Architecture Details](architecture/)** - Component-specific architecture

### For DevOps/Operators

Focus on deployment and operations:

1. **[Setup Guide](SETUP.md)** - Installation options
2. **[Architecture - Deployment](ARCHITECTURE.md#deployment-architecture)** - Deployment strategies
3. **[Setup - Troubleshooting](SETUP.md#troubleshooting)** - Common issues

### For Architects

Review design decisions:

1. **[Architecture](ARCHITECTURE.md)** - Overall system design
2. **[Architecture - Patterns](ARCHITECTURE.md#architecture-patterns)** - Design patterns used
3. **[Original Proposal](ORIGINAL_PROPOSAL.md)** - Initial planning and decisions

## Component Documentation

### API Layer
**Location:** `src/PRFactory.Api/README.md`

Documentation for REST API endpoints, controllers, and webhooks.

### Domain Layer
**Location:** `src/PRFactory.Domain/README.md`

Documentation for business entities, value objects, and domain logic.

### Infrastructure Layer
**Location:** `src/PRFactory.Infrastructure/README.md`

Documentation for external integrations (Jira, Git, Claude, Database).

**Subsystems:**
- **[Agents](../src/PRFactory.Infrastructure/Agents/README.md)** - 14 specialized workflow agents
- **[Claude](../src/PRFactory.Infrastructure/Claude/README.md)** - Claude AI client and prompts
- **[Git](../src/PRFactory.Infrastructure/Git/README.md)** - Git operations and platform integrations

### Worker Service
**Location:** `src/PRFactory.Worker/README.md`

Documentation for background job processing and workflow orchestration.

## Architecture Deep Dives

### Core Workflow Engine
**Location:** `docs/architecture/core-engine.md`

Detailed documentation on:
- Workflow state machine
- State transitions
- Checkpoint system
- Agent orchestration

### Jira Integration
**Location:** `docs/architecture/jira-integration.md`

Detailed documentation on:
- Webhook handling
- HMAC validation
- Comment posting
- Event processing

### Git Integration
**Location:** `docs/architecture/git-integration.md`

Detailed documentation on:
- Multi-platform support (GitHub, GitLab, Azure DevOps)
- Local git operations (LibGit2Sharp)
- Branch management
- Pull request creation

### Claude AI Integration
**Location:** `docs/architecture/claude-integration.md`

Detailed documentation on:
- Claude API client
- Prompt templates
- Context building
- Response parsing

## Getting Help

### Common Questions

**Q: How do I get started?**
A: Read the [Main README](../README.md), then follow the [Setup Guide](SETUP.md).

**Q: How does the workflow work?**
A: See the [Workflow Documentation](WORKFLOW.md) with detailed diagrams.

**Q: What's the system architecture?**
A: Review the [Architecture Documentation](ARCHITECTURE.md).

**Q: How do I troubleshoot issues?**
A: Check the [Troubleshooting Section](SETUP.md#troubleshooting) in the setup guide.

**Q: Where are the API endpoints?**
A: See [src/PRFactory.Api/README.md](../src/PRFactory.Api/README.md).

**Q: How do I add a new agent?**
A: See [Agent Documentation](../src/PRFactory.Infrastructure/Agents/README.md).

### Still Need Help?

- Check the logs (Serilog output)
- Review relevant component README
- Open an issue on GitHub
- Check Jira webhook logs for webhook issues

## Contributing to Documentation

When updating documentation:

1. **Keep it current** - Update docs when code changes
2. **Link liberally** - Cross-reference related docs
3. **Use diagrams** - Mermaid diagrams for workflows and architecture
4. **Provide examples** - Code samples and walkthroughs
5. **Update this index** - Add new docs to this README

### Documentation Standards

- Use Markdown (.md files)
- Include table of contents for long documents
- Use Mermaid for diagrams (flowcharts, sequence, state)
- Link to code files with line numbers where relevant
- Keep language clear and concise

## Historical Documents

### Original Proposal
**Location:** `docs/ORIGINAL_PROPOSAL.md`

The original project proposal with initial planning, workflow diagrams, and requirements. Kept for historical reference.

---

**Last Updated:** 2025-11-07
**PRFactory Version:** 1.0
