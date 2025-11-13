# Epic 05: Agent Framework Integration - Implementation Plans

**Epic Status:** 🟡 Refined & Ready for Implementation
**Last Updated:** 2025-11-13

---

## Overview

This directory contains detailed implementation plans for integrating Microsoft Agent Framework into PRFactory. The integration preserves PRFactory's proven graph-based orchestration while adding intelligent agent capabilities with tool use.

**Key Innovation:** Agents as specialized **graph node executors** rather than replacements for graphs.

---

## Document Index

### 1. **[EPIC_05_AGENT_FRAMEWORK.md](../EPIC_05_AGENT_FRAMEWORK.md)**
   - **Purpose:** Refined epic document with strategic goals, architecture vision, and decision rationale
   - **Audience:** Product managers, engineering leadership, stakeholders
   - **Key Sections:**
     - Executive summary
     - Architecture vision (graphs + agents + tools)
     - Database-driven configuration
     - AG-UI integration strategy
     - Migration and rollback plans
     - Success criteria and risks

### 2. **[01_ARCHITECTURE.md](./01_ARCHITECTURE.md)** ⭐
   - **Purpose:** Detailed architecture and design decisions
   - **Audience:** Architects, senior engineers
   - **Key Sections:**
     - High-level system architecture
     - Design principles (preserve PRFactory patterns)
     - Component architecture (AgentFactory, ToolRegistry, etc.)
     - Integration patterns (adapter, checkpoint, multi-tenancy)
     - Data flow diagrams
     - Security architecture
   - **Size:** 47 KB, comprehensive reference

### 3. **[02_TOOLS_LIBRARY.md](./02_TOOLS_LIBRARY.md)** ⭐
   - **Purpose:** Complete specification for PRFactory.AgentTools class library
   - **Audience:** Engineers implementing tools
   - **Key Sections:**
     - Project structure
     - Core interfaces (ITool, ToolBase from Saturn)
     - Tool implementations (File, Git, Jira, Analysis, Search)
     - Security patterns (path validation, SSRF protection)
     - Testing strategy (unit + integration)
     - Registration and DI
   - **Size:** 41 KB, complete implementation guide

### 4. **[03_AGENT_ROLES.md](./03_AGENT_ROLES.md)**
   - **Purpose:** Specialized agent role definitions for PRFactory workflows
   - **Audience:** Engineers implementing agent adapters
   - **Key Sections:**
     - AnalyzerAgent (RefinementGraph)
     - PlannerAgent (PlanningGraph)
     - CodeExecutorAgent (ImplementationGraph)
     - ReviewerAgent (CodeReviewGraph)
     - Adapter implementation patterns
     - Prompt engineering for each role

### 5. **[04_UI_INTEGRATION.md](./04_UI_INTEGRATION.md)**
   - **Purpose:** AG-UI protocol and Blazor integration
   - **Audience:** Frontend engineers, full-stack developers
   - **Key Sections:**
     - AG-UI protocol (HTTP + SSE)
     - Blazor component design
     - Real-time streaming UI
     - Follow-up question flows
     - Approval gate UI patterns

### 6. **[05_CONFIGURATION.md](./05_CONFIGURATION.md)**
   - **Purpose:** Database-driven agent configuration system
   - **Audience:** Backend engineers, DBAs
   - **Key Sections:**
     - Database schema (AgentConfiguration, AgentExecutionLog)
     - Admin UI for configuration
     - Runtime agent creation from DB
     - Multi-tenant isolation
     - Migration scripts

### 7. **[06_IMPLEMENTATION_ROADMAP.md](./06_IMPLEMENTATION_ROADMAP.md)** ⭐
   - **Purpose:** Week-by-week implementation timeline
   - **Audience:** Project managers, engineering leadership
   - **Key Sections:**
     - Phase 1: Foundation (Weeks 1-3)
     - Phase 2: Agent Roles (Weeks 4-6)
     - Phase 3: Tool Ecosystem (Weeks 7-8)
     - Phase 4: UI Integration (Weeks 9-10)
     - Phase 5: Production Readiness (Weeks 11-12)
     - Milestones, deliverables, and approval gates

---

## Research Documents

Comprehensive research was conducted before creating these implementation plans:

### Agent Framework Research
- **[AGENT_FRAMEWORK_RESEARCH.md](./AGENT_FRAMEWORK_RESEARCH.md)** (58 KB, 1,960 lines)
  - Technical deep-dive into Microsoft Agent Framework
  - 14 major topics covered
  - Production-ready patterns and best practices

- **[AGENT_FRAMEWORK_RECOMMENDATIONS.md](./AGENT_FRAMEWORK_RECOMMENDATIONS.md)** (17 KB, 503 lines)
  - Executive summary and decision rationale
  - Risk assessment and cost-benefit analysis
  - Success criteria and approval gates

- **[AGENT_FRAMEWORK_INTEGRATION_MAP.md](./AGENT_FRAMEWORK_INTEGRATION_MAP.md)** (71 KB, 1,732 lines)
  - Detailed mapping to PRFactory architecture
  - Integration points for each workflow phase
  - Migration strategies

### Saturn Fork Research
- **[SATURN_TOOLS_ANALYSIS.md](./SATURN_TOOLS_ANALYSIS.md)** (61 KB, 1,848 lines)
  - Complete analysis of Saturn's tool architecture
  - 16 production-ready tool patterns
  - Multi-agent coordination patterns
  - What to adopt vs. avoid

- **[SATURN_QUICK_REFERENCE.md](./SATURN_QUICK_REFERENCE.md)** (16 KB, 470 lines)
  - Quick reference for Saturn patterns
  - Tool categories and examples
  - Security patterns to port

- **[SATURN_EXPLORATION_INDEX.md](./SATURN_EXPLORATION_INDEX.md)** (11 KB, 325 lines)
  - Navigation guide to all Saturn documentation

---

## Implementation Approach

### Guiding Principles

1. **✅ PRESERVE: Graph-based orchestration**
   - Graphs remain the workflow backbone
   - Agent Framework enhances graph nodes, not replaces graphs

2. **✅ PRESERVE: Checkpoint-based resumption**
   - Full pause/resume capability maintained
   - Agent state serialized into existing Checkpoint entity

3. **✅ PRESERVE: Multi-tenancy**
   - Agent configuration per tenant
   - Tool permissions tenant-isolated
   - Workspace isolation enforced

4. **✅ NEW: Agent-driven task execution**
   - Replace simple prompt wrappers with intelligent agents
   - Agents have tool access and multi-turn reasoning

5. **✅ NEW: Separate tools library**
   - `PRFactory.AgentTools` class library
   - Reusable, testable, tenant-aware tools

6. **✅ NEW: Database-driven configuration**
   - AgentConfiguration entity (NOT appsettings)
   - Runtime configurability per tenant
   - Admin UI for agent management

7. **✅ NEW: AG-UI integration**
   - Real-time streaming responses
   - Follow-up question flows
   - Interactive agent reasoning visibility

---

## Phased Rollout

### Phase 1: Foundation (Weeks 1-3)
**Goal:** Create infrastructure for Agent Framework integration

**Deliverables:**
- `PRFactory.AgentTools` class library
- Core tool interfaces (ITool, ToolBase, ToolRegistry)
- Database schema (AgentConfiguration, AgentExecutionLog)
- Agent Framework SDK integration
- OpenTelemetry observability

**Approval Gate:** Architecture review, schema approval

---

### Phase 2: Agent Roles (Weeks 4-6)
**Goal:** Implement first agent adapters

**Deliverables:**
- AnalyzerAgent + adapter for RefinementGraph
- PlannerAgent + adapter for PlanningGraph
- AgentConfigurationService
- Middleware for multi-tenant isolation
- Unit and integration tests

**Approval Gate:** End-to-end RefinementGraph test with AF agents

---

### Phase 3: Tool Ecosystem (Weeks 7-8)
**Goal:** Complete tool implementations

**Deliverables:**
- 15+ tools (File, Git, Jira, Analysis, Search)
- Security hardening (path validation, SSRF protection)
- Tool execution audit logs
- Performance benchmarks

**Approval Gate:** Security review, penetration testing

---

### Phase 4: UI Integration (Weeks 9-10)
**Goal:** AG-UI and Blazor integration

**Deliverables:**
- AG-UI protocol implementation
- Blazor components for agent chat
- Real-time streaming response UI
- Follow-up question flows
- Approval gate UI improvements

**Approval Gate:** UX review, user acceptance testing

---

### Phase 5: Production Readiness (Weeks 11-12)
**Goal:** Production deployment

**Deliverables:**
- CodeExecutorAgent + ReviewerAgent
- End-to-end workflow testing
- Performance optimization
- Documentation and training
- Feature flags for gradual rollout

**Approval Gate:** Production readiness review, go/no-go decision

---

## Key Architectural Decisions

### 1. Adapter Pattern (Not Replacement)
**Decision:** Wrap AF agents with adapter classes that implement BaseAgent.

**Rationale:**
- Preserves existing graph node interface
- Enables hybrid approach (some agents AF, some traditional)
- Simplifies testing and rollback
- No graph rewrite needed

**Code Pattern:**
```csharp
public class AnalysisAgentAdapter : BaseAgent
{
    private readonly IAgentFactory _agentFactory;

    public override async Task<IAgentMessage> ExecuteAsync(IAgentMessage input)
    {
        var agent = await _agentFactory.CreateAgentAsync(
            Context.TenantId, "AnalyzerAgent");

        var result = await agent.RunAsync(BuildPrompt(input));

        return new AnalysisCompleteMessage(result.Output);
    }
}
```

---

### 2. Database Configuration (Not appsettings)
**Decision:** All agent config stored in AgentConfiguration table.

**Rationale:**
- Multi-tenant isolation (each tenant configures their agents)
- Runtime configurability (no deployment for config changes)
- Audit trail (who changed what when)
- Admin UI for non-technical users

**Schema:**
```sql
CREATE TABLE AgentConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    AgentName NVARCHAR(100) NOT NULL,
    Instructions NVARCHAR(MAX) NOT NULL,    -- System prompt
    EnabledTools NVARCHAR(MAX) NOT NULL,    -- JSON array
    MaxTokens INT NOT NULL DEFAULT 8000,
    Temperature FLOAT NOT NULL DEFAULT 0.3,
    CONSTRAINT UQ_AgentConfig UNIQUE (TenantId, AgentName)
);
```

---

### 3. Tools as Separate Library
**Decision:** Create `PRFactory.AgentTools` class library.

**Rationale:**
- Tools are reusable across agents
- Clear security boundary (tool permissions)
- Can be versioned independently
- Simplifies testing

**Project Structure:**
```
/PRFactory.AgentTools/
├── Core/          # ITool, ToolBase, ToolRegistry
├── FileSystem/    # ReadFile, WriteFile, Grep, Glob
├── Git/           # Commit, Branch, PR, Diff
├── Jira/          # GetTicket, AddComment, Transition
├── Analysis/      # CodeSearch, DependencyMap
├── Command/       # ExecuteShell, RunTests, Build
└── Web/           # WebFetch, ApiCall
```

---

### 4. Security-First Design
**Decision:** Every tool implements multiple security layers.

**Rationale:**
- Agents are autonomous - can't trust blindly
- Defense in depth (validation + whitelist + limits)
- Production-hardened patterns from Saturn

**Security Layers:**
1. **Tool Whitelisting** - AgentConfiguration.EnabledTools
2. **Input Validation** - Path traversal, SSRF, injection prevention
3. **Resource Limits** - File size, timeout, rate limiting
4. **Audit Logging** - All tool executions logged

---

## Success Metrics

### Phase 1 (Foundation)
- [ ] `PRFactory.AgentTools` class library created
- [ ] 5+ core tools implemented
- [ ] Database schema deployed
- [ ] 80%+ unit test coverage

### Phase 2 (Agents)
- [ ] AnalyzerAgent produces better analysis than current
- [ ] Token usage < 2x current approach
- [ ] Latency < 5s added per stage
- [ ] Multi-tenant isolation verified

### Phase 3 (Tools)
- [ ] 15+ tools implemented
- [ ] Security validation passes
- [ ] Performance benchmarks acceptable
- [ ] Tool audit logs working

### Phase 4 (UI)
- [ ] AG-UI streaming works in Blazor
- [ ] Follow-up questions functional
- [ ] User feedback positive
- [ ] Approval gates integrated

### Phase 5 (Production)
- [ ] End-to-end workflow tested
- [ ] Feature flags enabled for pilot
- [ ] Documentation complete
- [ ] Training materials ready

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Token costs too high | High | Token budgets, smaller models, caching |
| Latency degrades UX | Medium | Streaming UI, parallel execution |
| Agents make mistakes | High | Approval gates, audit logs, fallback |
| Complex integration | Medium | Phased rollout, feature flags, hybrid approach |
| Security vulnerabilities | High | Tool whitelisting, resource limits, audit trail |

---

## Next Steps

1. ✅ **Research complete** - Comprehensive research documents created
2. ✅ **Epic refined** - Updated with database config, AG-UI, Saturn learnings
3. ✅ **Implementation plans created** - Detailed specifications ready
4. ⏳ **Stakeholder review** - Schedule review with engineering/product leadership
5. ⏳ **Approval** - Get go/no-go decision and resource allocation
6. ⏳ **Phase 1 kickoff** - Begin foundation work (tools library, DB schema)

---

## Questions?

**For technical details:** See individual implementation plan documents
**For architecture decisions:** See `01_ARCHITECTURE.md`
**For tool specifications:** See `02_TOOLS_LIBRARY.md`
**For timeline:** See `06_IMPLEMENTATION_ROADMAP.md`

**Status:** ✅ **Ready for stakeholder review and approval**
