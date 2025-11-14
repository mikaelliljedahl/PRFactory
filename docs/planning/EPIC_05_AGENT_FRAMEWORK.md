# Epic 5: Microsoft Agent Framework Integration

**Status:** 🟡 Refined & Ready for Implementation
**Priority:** High (Post-P1, foundational for future AI capabilities)
**Effort:** 10-12 weeks (phased implementation)
**Dependencies:** Epic 2 (Multi-LLM), Epic 1 (Team Review for safety gates)

---

## Executive Summary

Enable **autonomous agentic workflows** with specialized tool use (file operations, git commands, Jira API calls, code analysis) while maintaining human oversight gates. Move from graph-based orchestration to **agent-driven orchestration within graphs**, combining PRFactory's proven workflow architecture with Microsoft Agent Framework's powerful agent capabilities.

**Key Innovation:** Agents as specialized **graph node executors** rather than replacements for graphs. Graphs provide workflow orchestration, agents provide intelligent task execution.

---

## Strategic Goals

### 1. **Intelligent Task Execution**
Current agents are prompt-based wrappers. New Agent Framework agents have:
- **Tool use** - Direct API calls to file system, git, Jira, codebase analysis
- **Multi-turn reasoning** - Agents can plan, execute, validate, retry autonomously
- **Conversation memory** - Context retention across workflow stages
- **Structured outputs** - Type-safe responses instead of parsing markdown

### 2. **Specialized Agent Roles**
Align agents with PRFactory's proven workflows:
- **AnalyzerAgent** - Codebase analysis, impact assessment (RefinementGraph)
- **PlannerAgent** - Implementation planning, task decomposition (PlanningGraph)
- **CodeExecutorAgent** - Code generation, testing, validation (ImplementationGraph)
- **ReviewerAgent** - Code quality review, security analysis (CodeReviewGraph)

### 3. **Interactive User Experience**
Via AG-UI integration:
- Real-time agent reasoning visibility (streaming responses)
- Follow-up question flows ("Did you mean this file?")
- Human approval gates before destructive operations
- Conversation history and context management

### 4. **Database-Driven Configuration**
All agent configuration stored in database (multi-tenant isolated):
- Agent instructions and personas per tenant
- Tool permissions and safety policies
- Token budgets and cost controls
- Feature flags for gradual rollout

---

## Architecture Vision

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     PRFactory Workflow Graphs                    │
│  (RefinementGraph, PlanningGraph, ImplementationGraph, etc.)    │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Agent Adapters (BaseAgent Wrappers)                │
│     AnalysisAgentAdapter, PlannerAgentAdapter, etc.             │
│  (Preserve checkpoint resumption, multi-tenancy, messages)      │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│           Microsoft Agent Framework (AF.Agents)                 │
│                                                                  │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────┐ │
│  │ AnalyzerAgent  │  │ PlannerAgent   │  │ CodeExecutorAgent│ │
│  │ - Code search  │  │ - Task decomp  │  │ - Code gen       │ │
│  │ - Impact       │  │ - Risk assess  │  │ - Testing        │ │
│  │ - Dependencies │  │ - Estimation   │  │ - Validation     │ │
│  └────────┬───────┘  └────────┬───────┘  └────────┬─────────┘ │
│           │                    │                    │           │
│           └────────────────────┼────────────────────┘           │
│                                ▼                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Agent Tool Registry (DI)                     │  │
│  │         PRFactory.AgentTools Class Library                │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                   PRFactory.AgentTools                          │
│                  (Separate Class Library)                        │
│                                                                  │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────┐│
│  │ File Tools      │  │ Git Tools        │  │ Jira Tools     ││
│  │ - ReadFile      │  │ - Commit         │  │ - GetTicket    ││
│  │ - WriteFile     │  │ - CreateBranch   │  │ - AddComment   ││
│  │ - Grep          │  │ - CreatePR       │  │ - Transition   ││
│  │ - Glob          │  │ - GetDiff        │  │                ││
│  └─────────────────┘  └──────────────────┘  └────────────────┘│
│                                                                  │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────┐│
│  │ Analysis Tools  │  │ Command Tools    │  │ Web Tools      ││
│  │ - CodeSearch    │  │ - ExecuteShell   │  │ - WebFetch     ││
│  │ - ParseAST      │  │ - RunTests       │  │ - ApiCall      ││
│  │ - DependencyMap │  │ - BuildProject   │  │                ││
│  └─────────────────┘  └──────────────────┘  └────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Infrastructure Services                       │
│  LocalGitService, JiraService, FileSystem, HttpClient, etc.     │
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Decisions

**✅ PRESERVE: Graph-based orchestration**
Graphs remain the workflow backbone. Agent Framework enhances individual graph nodes, not replaces graphs.

**✅ PRESERVE: Checkpoint-based resumption**
Agent state serialized into existing Checkpoint entity. Full pause/resume capability maintained.

**✅ PRESERVE: Multi-tenancy**
Agent configuration, tool permissions, and execution all tenant-isolated via ITenantContext.

**✅ NEW: Agent-driven task execution**
Replace simple prompt wrappers with intelligent agents that can reason, plan, and use tools.

**✅ NEW: Separate tools library**
`PRFactory.AgentTools` class library - reusable, testable, tenant-aware tools.

**✅ NEW: Database-driven configuration**
New `AgentConfiguration` entity stores all agent settings (instructions, tools, limits, policies).

**✅ NEW: AG-UI integration**
Real-time streaming UI for agent interactions, follow-up questions, and approval gates.

---

## Why Microsoft Agent Framework?

### Research Summary

**Three comprehensive research documents created:**
1. **AGENT_FRAMEWORK_RESEARCH.md** - Deep technical dive (1,960 lines, 14 topics)
2. **AGENT_FRAMEWORK_RECOMMENDATIONS.md** - Decision rationale and roadmap (503 lines)
3. **AGENT_FRAMEWORK_INTEGRATION_MAP.md** - PRFactory integration points (1,732 lines)

### Perfect Alignment with PRFactory

| PRFactory Requirement | Agent Framework Capability | Fit |
|----------------------|----------------------------|-----|
| Graph-based workflows | Graph-based workflow support | ✅ Perfect |
| Checkpoint resumption | Native checkpointing & AgentThread | ✅ Perfect |
| Multi-tenant isolation | Middleware + DI support | ✅ Perfect |
| Tool/function calling | AIFunctionFactory | ✅ Perfect |
| OpenTelemetry | Built-in observability | ✅ Perfect |
| Multi-LLM support | IChatClient abstraction | ✅ Perfect |
| Real-time UI | AG-UI protocol (SSE) | ✅ Perfect |

### Benefits Over Current Approach

**Current:** Simple prompt wrappers calling LLM CLI/API
**Agent Framework:** Intelligent agents with reasoning, planning, tool use, memory

| Capability | Current | Agent Framework |
|-----------|---------|-----------------|
| **Autonomy** | Single prompt/response | Multi-turn reasoning with tool use |
| **Tools** | None (all manual) | 20+ tools (file, git, Jira, analysis) |
| **Memory** | Stateless per graph node | Conversation history + context |
| **Planning** | Fixed graph logic | Dynamic agent planning |
| **Validation** | Manual or none | Agents validate their own work |
| **Error Recovery** | Fixed retry logic | Intelligent retry with context |
| **Observability** | Custom logging | Built-in OpenTelemetry |
| **UI Interaction** | Approval gates only | Streaming, follow-ups, reasoning |

### Cost & Performance Considerations

**Token Usage:**
- ✅ **Mitigation**: Token budgets per tenant in `AgentConfiguration`
- ✅ **Mitigation**: Smaller models for simple tasks (Haiku vs Sonnet)
- ✅ **Mitigation**: Tool results cached where possible
- ⚠️ **Expected increase**: 1.5-2x tokens vs current (multi-turn conversations)

**Latency:**
- ✅ **Mitigation**: Streaming responses via AG-UI (perceived performance)
- ✅ **Mitigation**: Parallel tool execution where possible
- ⚠️ **Expected increase**: +2-5s per workflow stage (tool roundtrips)

**Value Proposition:**
Higher costs justified by significantly better outcomes (better analysis, planning, code generation, review).

---

## Saturn Fork Learnings

### What Saturn Got Right (Adopt)

**Comprehensive research document created:**
**SATURN_TOOLS_ANALYSIS.md** - 1,848 lines, complete tool architecture analysis

**Key Patterns to Port:**
1. **ITool Interface** - Simple, elegant tool contract
2. **ToolRegistry with Auto-Discovery** - Reflection-based tool registration
3. **Template Method Pattern** - Consistent tool execution flow
4. **Production-Hardened Security** - Path validation, size limits, SSRF protection, timeouts
5. **Atomic File Operations** - Temp file + rename pattern for corruption prevention
6. **Streaming Support** - Real-time output for long operations
7. **Message Persistence** - Full audit trail

**16 Production-Ready Tools Identified:**
- File System: Read, Write, Delete, List, Patch
- Search: Grep, Glob, SearchReplace
- Command: ExecuteShell with timeout
- Web: WebFetch with SSRF protection
- Multi-Agent: Create, Handoff, Wait, GetResult, Status, Terminate

### What Saturn Got Wrong (Avoid)

1. ❌ **Global Singleton State** - Use DI instead
2. ❌ **Polling-Based Coordination** - Use event-driven patterns
3. ❌ **No Security Boundaries** - Tools must be per-agent/per-tenant controlled
4. ❌ **Direct LLM Provider Coupling** - Use Agent Framework's abstraction
5. ❌ **Per-Command Approval** - Use policy-based approval at workflow level
6. ❌ **Complex Review Phase Loops** - Keep approval gates simple

### Saturn Integration Strategy

**Phase 1:** Port ITool + ToolBase + ToolRegistry patterns
**Phase 2:** Implement 10 core tools (file, git, Jira)
**Phase 3:** Add advanced tools (analysis, AST parsing)
**Phase 4:** Multi-agent coordination (future)

---

## Implementation Plan Overview

**Detailed implementation plans in:** `/docs/planning/epic_05_agent_framework/`

### Phase 1: Foundation (Weeks 1-3)
- Create `PRFactory.AgentTools` class library
- Port Saturn's ITool pattern and core tools
- Create `AgentConfiguration` entity and database schema
- Set up Agent Framework SDK integration
- Implement observability (OpenTelemetry)

### Phase 2: Agent Roles (Weeks 4-6)
- Implement AnalyzerAgent + adapter for RefinementGraph
- Implement PlannerAgent + adapter for PlanningGraph
- Create AgentConfigurationService for database-driven config
- Add middleware for multi-tenant isolation
- Unit and integration testing

### Phase 3: Tool Ecosystem (Weeks 7-8)
- Complete file system tools (Read, Write, Grep, Glob)
- Complete git tools (Commit, Branch, PR, Diff)
- Complete Jira tools (GetTicket, AddComment, Transition)
- Add analysis tools (CodeSearch, DependencyMap)
- Security hardening and validation

### Phase 4: UI Integration (Weeks 9-10)
- AG-UI protocol implementation
- Blazor components for agent interaction
- Real-time streaming response UI
- Follow-up question flows
- Approval gate UI improvements

### Phase 5: Production Readiness (Weeks 11-12)
- CodeExecutorAgent + ReviewerAgent implementation
- End-to-end workflow testing
- Performance optimization
- Documentation and training
- Gradual rollout with feature flags

---

## Database Schema Changes

### New Entities

**AgentConfiguration** (Multi-tenant isolated)
```csharp
public class AgentConfiguration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AgentName { get; set; }  // "AnalyzerAgent", "PlannerAgent", etc.
    public string Instructions { get; set; }  // System prompt / persona
    public string[] EnabledTools { get; set; }  // Tool whitelist
    public int MaxTokens { get; set; }  // Token budget
    public float Temperature { get; set; }  // LLM temperature
    public bool StreamingEnabled { get; set; }
    public bool RequiresApproval { get; set; }  // Approval gate config
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Tenant Tenant { get; set; }
}
```

**AgentExecutionLog** (Audit trail)
```csharp
public class AgentExecutionLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid CheckpointId { get; set; }
    public string AgentName { get; set; }
    public string ToolName { get; set; }  // Null if no tool used
    public string Input { get; set; }
    public string Output { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
}
```

### Checkpoint Schema Extension

**Add AgentThread state to Checkpoint:**
```csharp
public class Checkpoint
{
    // ... existing fields ...

    // New fields for Agent Framework integration
    public string? AgentThreadId { get; set; }  // AF AgentThread ID
    public string? ConversationHistory { get; set; }  // Serialized messages (JSON)
    public string? AgentState { get; set; }  // Serialized agent-specific state
}
```

---

## Configuration Example (Database, NOT appsettings)

### UI for Agent Configuration

**Admin UI: `/admin/agent-configuration`**

Tenant admins can configure agents:
- Agent name and role
- Custom instructions/persona
- Tool permissions (checkboxes for each tool)
- Token budget and temperature
- Streaming and approval settings

**Example stored in database:**
```json
{
  "tenantId": "abc123",
  "agentName": "AnalyzerAgent",
  "instructions": "You are a senior software architect. Analyze codebases for impact, dependencies, and risks. Be thorough but concise.",
  "enabledTools": [
    "ReadFile",
    "Grep",
    "Glob",
    "CodeSearch",
    "GetJiraTicket"
  ],
  "maxTokens": 8000,
  "temperature": 0.3,
  "streamingEnabled": true,
  "requiresApproval": false
}
```

### Runtime Agent Creation (from Database)

```csharp
public class AgentFactory : IAgentFactory
{
    private readonly IAgentConfigurationService _configService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IChatClient _chatClient;

    public async Task<AIAgent> CreateAgentAsync(
        Guid tenantId,
        string agentName,
        CancellationToken ct = default)
    {
        // Load from database
        var config = await _configService.GetConfigurationAsync(
            tenantId, agentName, ct);

        // Get enabled tools (filtered by tenant permissions)
        var tools = _toolRegistry.GetTools(
            tenantId, config.EnabledTools);

        // Create agent with config
        var agent = _chatClient.CreateAIAgent(
            instructions: config.Instructions,
            tools: tools,
            maxTokens: config.MaxTokens,
            temperature: config.Temperature);

        // Apply middleware
        return agent
            .WithMiddleware(LoggingMiddleware)
            .WithMiddleware(TenantIsolationMiddleware)
            .WithMiddleware(TokenBudgetMiddleware);
    }
}
```

---

## AG-UI Integration

### Real-Time Streaming Protocol

**AG-UI uses HTTP + Server-Sent Events (SSE):**

```
Client (Blazor)
    ↓ POST /api/agent/chat
Server (AG-UI endpoint)
    ↓ SSE stream
Client receives:
  - Agent reasoning steps
  - Tool invocations
  - Partial responses
  - Final result
```

### Blazor Component Example

```razor
<!-- /PRFactory.Web/Components/Agents/AgentChat.razor -->
<div class="agent-chat">
    <div class="messages">
        @foreach (var msg in messages)
        {
            <AgentMessage Message="@msg" />
        }

        @if (isStreaming)
        {
            <div class="streaming-indicator">
                <LoadingSpinner />
                <span>@currentAgentAction</span>
            </div>
        }
    </div>

    @if (requiresUserInput)
    {
        <AgentFollowUpQuestion
            Question="@currentQuestion"
            OnAnswer="HandleUserAnswer" />
    }

    @if (requiresApproval)
    {
        <AgentApprovalGate
            ProposedAction="@proposedAction"
            OnApprove="HandleApproval"
            OnReject="HandleRejection" />
    }
</div>

@code {
    [Parameter] public Guid TicketId { get; set; }
    [Inject] private IAgentChatService AgentChatService { get; set; }

    private async Task SendMessageAsync(string userMessage)
    {
        isStreaming = true;

        await foreach (var chunk in AgentChatService.StreamResponseAsync(
            TicketId, userMessage))
        {
            if (chunk.Type == "reasoning")
                currentAgentAction = chunk.Content;
            else if (chunk.Type == "tool_use")
                messages.Add(new ToolUseMessage(chunk));
            else if (chunk.Type == "response")
                messages.Add(new AssistantMessage(chunk));

            StateHasChanged();
        }

        isStreaming = false;
    }
}
```

### Follow-Up Question Flow

**Agent can ask clarifying questions mid-workflow:**

1. Agent analyzes ticket: "Fix authentication bug"
2. Agent searches codebase, finds 3 auth-related files
3. **Agent asks:** "I found 3 authentication modules. Which one is affected?"
   - `src/Auth/JwtAuth.cs`
   - `src/Auth/OAuth2Auth.cs`
   - `src/Auth/ApiKeyAuth.cs`
4. User clicks: `JwtAuth.cs`
5. Agent continues with correct context

This is **only possible with AG-UI + Agent Framework**, not current approach.

---

## Security & Safety

### Tool Permission Model

**Database-driven tool whitelisting:**
- Each AgentConfiguration specifies `EnabledTools`
- ToolRegistry filters tools by tenant permissions
- Agents cannot access tools not in their whitelist

**Example:**
```csharp
// AnalyzerAgent config (read-only)
EnabledTools = ["ReadFile", "Grep", "Glob", "CodeSearch"]

// CodeExecutorAgent config (read-write)
EnabledTools = ["ReadFile", "WriteFile", "Grep", "Glob", "ExecuteShell", "RunTests"]

// ReviewerAgent config (read-only)
EnabledTools = ["ReadFile", "Grep", "GetGitDiff"]
```

### Approval Gates

**Workflow-level approval (not per-command):**
- RefinementGraph → Human approves refined requirements
- PlanningGraph → Human approves implementation plan
- ImplementationGraph → Human approves code changes
- CodeReviewGraph → Human approves review feedback

**No per-tool approval** (too granular, slows down agents).

### Resource Limits

**Enforced via middleware:**
- Token budgets per agent per ticket
- File size limits (10MB max read, 1MB max write)
- Execution timeouts (30s per tool, 5min per agent)
- Rate limiting (100 tool calls per workflow)

### Audit Trail

**All agent actions logged in `AgentExecutionLog`:**
- Which agent executed
- Which tool was invoked
- Input and output
- Token usage and duration
- Success or failure

Enables full forensic analysis of agent behavior.

---

## Migration Strategy

### Hybrid Approach (Gradual Rollout)

**Not a "big bang" rewrite - gradual enhancement:**

**Week 1-3:** AnalyzerAgent (RefinementGraph only)
- Feature flag: `EnableAgentFrameworkAnalyzer`
- Fallback to current AnalysisAgent if flag disabled

**Week 4-6:** PlannerAgent (PlanningGraph only)
- Feature flag: `EnableAgentFrameworkPlanner`
- Fallback to current PlanningAgent if flag disabled

**Week 7-10:** Full integration (all graphs)
- Feature flag: `EnableAgentFrameworkFull`
- Remove old agents once validated

### Rollback Plan

**If Agent Framework underperforms:**
1. Disable feature flags per tenant
2. Fallback to current graph agents
3. Continue using existing workflow graphs (unchanged)
4. Re-evaluate and iterate

**No risk to core workflows** - graphs and checkpoints preserved.

---

## Success Criteria

### Phase 1 (Foundation - Week 3)
- [ ] `PRFactory.AgentTools` class library created
- [ ] ITool interface and 5 core tools implemented
- [ ] `AgentConfiguration` entity in database
- [ ] Agent Framework SDK integrated
- [ ] Unit tests for all tools (80%+ coverage)

### Phase 2 (Agent Roles - Week 6)
- [ ] AnalyzerAgent produces better codebase analysis than current
- [ ] PlannerAgent generates structured plans with risk assessment
- [ ] Multi-tenant isolation verified (no cross-tenant data leaks)
- [ ] Token usage < 2x current approach
- [ ] Latency < 5s added per workflow stage

### Phase 3 (Tool Ecosystem - Week 8)
- [ ] 15+ tools implemented and tested
- [ ] Security validation passes (no directory traversal, SSRF, etc.)
- [ ] Tool execution audit logs working
- [ ] Performance benchmarks acceptable

### Phase 4 (UI Integration - Week 10)
- [ ] AG-UI streaming works in Blazor
- [ ] Follow-up questions functional
- [ ] Approval gates integrated
- [ ] User feedback positive

### Phase 5 (Production - Week 12)
- [ ] CodeExecutorAgent and ReviewerAgent implemented
- [ ] End-to-end workflow tested (Jira → Code → PR)
- [ ] Feature flags enabled for pilot tenants
- [ ] Documentation complete
- [ ] Training materials created

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Token costs too high | High | Medium | Token budgets, smaller models, caching |
| Latency degrades UX | Medium | Low | Streaming UI, parallel execution |
| Agents make mistakes | High | Medium | Approval gates, audit logs, fallback to human |
| Framework not mature | Medium | Low | Public preview (Oct 2025) is production-ready |
| Complex integration | Medium | Medium | Phased rollout, hybrid approach, feature flags |
| Security vulnerabilities | High | Low | Tool whitelisting, resource limits, audit trail |

---

## Related Documentation

### Research Documents
- **AGENT_FRAMEWORK_RESEARCH.md** - Technical deep-dive (58 KB, 1,960 lines)
- **AGENT_FRAMEWORK_RECOMMENDATIONS.md** - Decision rationale (17 KB, 503 lines)
- **AGENT_FRAMEWORK_INTEGRATION_MAP.md** - PRFactory integration (71 KB, 1,732 lines)
- **SATURN_TOOLS_ANALYSIS.md** - Saturn fork analysis (61 KB, 1,848 lines)
- **SATURN_QUICK_REFERENCE.md** - Saturn patterns (16 KB, 470 lines)

### Implementation Plans
- **01_ARCHITECTURE.md** - Detailed architecture and design decisions
- **02_TOOLS_LIBRARY.md** - PRFactory.AgentTools class library spec
- **03_AGENT_ROLES.md** - Specialized agent role definitions
- **04_UI_INTEGRATION.md** - AG-UI and Blazor integration
- **05_CONFIGURATION.md** - Database-driven configuration spec
- **06_IMPLEMENTATION_ROADMAP.md** - Week-by-week implementation plan

### Related Epics
- **Epic 1 (Team Review)** - Approval gates for agent actions
- **Epic 2 (Multi-LLM)** - Agent Framework supports all LLM providers
- **Epic 3 (OAuth)** - Secure credential management for agents

---

## Next Steps

1. **Review this refined EPIC** with engineering and product leadership
2. **Review detailed implementation plans** in `/docs/planning/epic_05_agent_framework/`
3. **Approve/adjust scope** and timeline
4. **Allocate engineering resources** (2 senior engineers, 10-12 weeks)
5. **Begin Phase 1: Foundation** (create tools library, database schema, SDK integration)
6. **Weekly progress reviews** with stakeholders
7. **Gradual rollout** with feature flags and pilot tenants

---

**Status:** ✅ **Refined and ready for stakeholder review**
**Next Review Date:** [To be scheduled]
**Owner:** [To be assigned]
