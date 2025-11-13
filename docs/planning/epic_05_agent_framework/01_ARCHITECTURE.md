# 01: Architecture & Design Decisions

**Document Purpose:** Define the architectural patterns, design decisions, and integration strategy for Microsoft Agent Framework within PRFactory.

**Last Updated:** 2025-11-13

---

## Table of Contents

- [High-Level Architecture](#high-level-architecture)
- [Design Principles](#design-principles)
- [Component Architecture](#component-architecture)
- [Integration Patterns](#integration-patterns)
- [Data Flow](#data-flow)
- [Multi-Tenancy Architecture](#multi-tenancy-architecture)
- [Security Architecture](#security-architecture)
- [Technology Stack](#technology-stack)

---

## High-Level Architecture

### System Context

```
┌─────────────────────────────────────────────────────────────────────┐
│                        External Systems                              │
│                                                                       │
│   ┌──────────────┐   ┌──────────────┐   ┌──────────────┐           │
│   │ Jira/Azure   │   │ GitHub/      │   │ Anthropic/   │           │
│   │ DevOps       │   │ Bitbucket    │   │ OpenAI APIs  │           │
│   └──────┬───────┘   └──────┬───────┘   └──────┬───────┘           │
└──────────┼──────────────────┼──────────────────┼───────────────────┘
           │                  │                  │
           ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      PRFactory System                                │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    Presentation Layer                           │ │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐      │ │
│  │  │ Blazor Web UI │  │ AG-UI         │  │ REST API      │      │ │
│  │  │ (Admin/User)  │  │ Components    │  │ (Webhooks)    │      │ │
│  │  └───────┬───────┘  └───────┬───────┘  └───────┬───────┘      │ │
│  └──────────┼──────────────────┼──────────────────┼──────────────┘ │
│             │                  │                  │                  │
│  ┌──────────┼──────────────────┼──────────────────┼──────────────┐ │
│  │          │    Application Service Layer        │               │ │
│  │          ▼                  ▼                  ▼               │ │
│  │  ┌─────────────────────────────────────────────────────────┐  │ │
│  │  │         Workflow Orchestration                           │  │ │
│  │  │  ┌──────────┐  ┌──────────┐  ┌────────────────────┐    │  │ │
│  │  │  │Refinement│  │ Planning │  │ Implementation     │    │  │ │
│  │  │  │  Graph   │  │  Graph   │  │    Graph           │    │  │ │
│  │  │  └────┬─────┘  └────┬─────┘  └────────┬───────────┘    │  │ │
│  │  └───────┼─────────────┼──────────────────┼────────────────┘  │ │
│  │          │             │                  │                    │ │
│  │  ┌───────┼─────────────┼──────────────────┼────────────────┐  │ │
│  │  │       ▼             ▼                  ▼                 │  │ │
│  │  │            Agent Adapter Layer                           │  │ │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │  │ │
│  │  │  │AnalysisAgent │  │PlannerAgent  │  │CodeExecutor  │  │  │ │
│  │  │  │   Adapter    │  │   Adapter    │  │   Adapter    │  │  │ │
│  │  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │  │ │
│  │  └─────────┼──────────────────┼──────────────────┼─────────┘  │ │
│  └────────────┼──────────────────┼──────────────────┼────────────┘ │
│               │                  │                  │                │
│  ┌────────────┼──────────────────┼──────────────────┼────────────┐ │
│  │            ▼                  ▼                  ▼             │ │
│  │           Microsoft Agent Framework Layer                     │ │
│  │  ┌───────────────────────────────────────────────────────┐   │ │
│  │  │            AgentFactory (Runtime Creation)             │   │ │
│  │  │  - Load config from DB                                 │   │ │
│  │  │  - Resolve tools per tenant                            │   │ │
│  │  │  - Apply middleware chain                              │   │ │
│  │  └─────────────────────┬─────────────────────────────────┘   │ │
│  │                        ▼                                      │ │
│  │  ┌─────────────────────────────────────────────────────────┐ │ │
│  │  │              AF Agents (IChatClient)                     │ │ │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │ │ │
│  │  │  │ Analyzer     │  │ Planner      │  │ CodeExecutor │  │ │ │
│  │  │  │ Agent        │  │ Agent        │  │ Agent        │  │ │ │
│  │  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │ │ │
│  │  └─────────┼──────────────────┼──────────────────┼─────────┘ │ │
│  │            │                  │                  │            │ │
│  │  ┌─────────┼──────────────────┼──────────────────┼─────────┐ │ │
│  │  │         ▼                  ▼                  ▼          │ │ │
│  │  │              Tool Registry (DI)                          │ │ │
│  │  │  - Filters tools by tenant/agent permissions            │ │ │
│  │  │  - Wraps tools with tenant context                      │ │ │
│  │  │  - Enforces security policies                           │ │ │
│  │  └─────────────────────┬────────────────────────────────────┘ │ │
│  └────────────────────────┼─────────────────────────────────────┘ │
│                           │                                        │
│  ┌────────────────────────┼─────────────────────────────────────┐ │
│  │                        ▼                                      │ │
│  │            PRFactory.AgentTools (Class Library)              │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │ │
│  │  │ File Tools  │  │ Git Tools   │  │ Jira Tools          │ │ │
│  │  │ Analysis    │  │ Command     │  │ Web Tools           │ │ │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘ │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │              Infrastructure Layer                             │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │ │
│  │  │ Database    │  │ Git Service │  │ External Service    │  │ │
│  │  │ (EF Core)   │  │(LibGit2Sharp│  │ Clients             │  │ │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘  │ │
│  └──────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────┘
```

---

## Design Principles

### 1. **Preserve PRFactory's Proven Architecture**

**Decision:** Agent Framework enhances, not replaces, existing workflow graphs.

**Rationale:**
- Graph-based orchestration is intentional, flexible, and working
- Checkpoint-based resumption is critical for human approval gates
- Multi-tenant architecture is core product feature
- Don't discard what works - enhance it

**Implementation:**
```csharp
// OLD (current)
public class AnalysisAgent : BaseAgent
{
    public override async Task<IAgentMessage> ExecuteAsync(IAgentMessage input)
    {
        // Simple prompt wrapper
        var prompt = BuildPrompt(input);
        var result = await _llmProvider.SendPromptAsync(prompt);
        return new AnalysisCompleteMessage(result);
    }
}

// NEW (Agent Framework)
public class AnalysisAgentAdapter : BaseAgent
{
    private readonly IAgentFactory _agentFactory;

    public override async Task<IAgentMessage> ExecuteAsync(IAgentMessage input)
    {
        // Create AF agent with tools
        var agent = await _agentFactory.CreateAgentAsync(
            tenantId: Context.TenantId,
            agentName: "AnalyzerAgent");

        // Run with tool access
        var result = await agent.RunAsync(BuildPrompt(input));

        // Return in existing message format
        return new AnalysisCompleteMessage(result.Output);
    }
}
```

**Benefits:**
- ✅ No graph rewrite needed
- ✅ Backward compatible
- ✅ Checkpoint resumption preserved
- ✅ Gradual rollout possible

---

### 2. **Separation of Concerns: Tools as Library**

**Decision:** Tools live in separate `PRFactory.AgentTools` class library.

**Rationale:**
- Tools are reusable across agents
- Tools can be tested independently
- Tools can be versioned separately
- Clear security boundary (tool permissions)

**Project Structure:**
```
/PRFactory.AgentTools/              # NEW class library
├── Core/
│   ├── ITool.cs                    # Tool interface (from Saturn)
│   ├── ToolBase.cs                 # Base implementation
│   ├── ToolRegistry.cs             # Auto-discovery + DI
│   └── ToolExecutionContext.cs     # Tenant context, limits
├── FileSystem/
│   ├── ReadFileTool.cs
│   ├── WriteFileTool.cs
│   ├── GrepTool.cs
│   └── GlobTool.cs
├── Git/
│   ├── CommitTool.cs
│   ├── CreateBranchTool.cs
│   └── GetDiffTool.cs
├── Jira/
│   ├── GetTicketTool.cs
│   ├── AddCommentTool.cs
│   └── TransitionTool.cs
├── Analysis/
│   ├── CodeSearchTool.cs
│   └── DependencyMapTool.cs
└── PRFactory.AgentTools.csproj
```

**Dependencies:**
- `PRFactory.AgentTools` → `PRFactory.Core` (ITenantContext, domain abstractions)
- `PRFactory.Infrastructure` → `PRFactory.AgentTools` (registers tools)

---

### 3. **Database-Driven Configuration**

**Decision:** All agent configuration stored in database, NOT appsettings.json.

**Rationale:**
- Multi-tenant isolation (each tenant configures their agents)
- Runtime configurability (no deployment for config changes)
- Audit trail (who changed what when)
- Feature flags per tenant

**Database Schema:**
```sql
CREATE TABLE AgentConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    AgentName NVARCHAR(100) NOT NULL,
    Instructions NVARCHAR(MAX) NOT NULL,    -- System prompt
    EnabledTools NVARCHAR(MAX) NOT NULL,    -- JSON array
    MaxTokens INT NOT NULL DEFAULT 8000,
    Temperature FLOAT NOT NULL DEFAULT 0.3,
    StreamingEnabled BIT NOT NULL DEFAULT 1,
    RequiresApproval BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_AgentConfig_Tenant FOREIGN KEY (TenantId)
        REFERENCES Tenants(Id),
    CONSTRAINT UQ_AgentConfig_TenantAgent
        UNIQUE (TenantId, AgentName)
);

CREATE INDEX IX_AgentConfig_TenantId ON AgentConfigurations(TenantId);
```

**Admin UI:**
- `/admin/agent-configuration` page
- CRUD operations for AgentConfiguration
- Tool permission checkboxes
- Real-time preview of agent behavior

---

### 4. **Adapter Pattern for Graph Integration**

**Decision:** Use adapter pattern to bridge BaseAgent and AF agents.

**Rationale:**
- Preserves existing graph node interface (IAgentNode, BaseAgent)
- Encapsulates AF-specific logic
- Enables hybrid approach (some agents AF, some traditional)
- Simplifies testing and rollback

**Pattern:**
```csharp
// Existing interface (unchanged)
public interface IAgentNode
{
    Task<IAgentMessage> ExecuteAsync(IAgentMessage input, CancellationToken ct);
}

// Adapter wraps AF agent
public class AnalyzerAgentAdapter : BaseAgent
{
    private readonly IAgentFactory _agentFactory;
    private readonly ICheckpointService _checkpointService;

    public override async Task<IAgentMessage> ExecuteAsync(
        IAgentMessage input, CancellationToken ct)
    {
        // 1. Load checkpoint (if exists)
        var checkpoint = await _checkpointService.LoadAsync(
            Context.TicketId, "AnalyzerAgent");

        // 2. Create AF agent with database config
        var agent = await _agentFactory.CreateAgentAsync(
            Context.TenantId, "AnalyzerAgent");

        // 3. Run with conversation history
        var result = await agent.RunAsync(
            messages: checkpoint?.ConversationHistory ?? [],
            prompt: BuildPrompt(input),
            ct: ct);

        // 4. Save checkpoint
        await _checkpointService.SaveAsync(
            Context.TicketId,
            "AnalyzerAgent",
            result.Thread,
            result.Messages);

        // 5. Return in graph message format
        return new AnalysisCompleteMessage(result.Output);
    }
}
```

---

### 5. **Multi-Tenancy via Middleware**

**Decision:** Tenant isolation enforced via middleware chain.

**Rationale:**
- Centralized security enforcement
- Cross-cutting concern (all agents need it)
- Easy to audit and test
- Consistent pattern across all agents

**Middleware Chain:**
```csharp
public class AgentFactory : IAgentFactory
{
    public async Task<AIAgent> CreateAgentAsync(
        Guid tenantId, string agentName, CancellationToken ct = default)
    {
        var config = await _configService.GetAsync(tenantId, agentName, ct);
        var tools = _toolRegistry.GetTools(tenantId, config.EnabledTools);

        var agent = _chatClient.CreateAIAgent(
            instructions: config.Instructions,
            tools: tools,
            maxTokens: config.MaxTokens);

        // Apply middleware chain
        return agent
            .WithMiddleware(TenantIsolationMiddleware)   // 1. Inject tenant context
            .WithMiddleware(TokenBudgetMiddleware)       // 2. Enforce token limits
            .WithMiddleware(AuditLoggingMiddleware)      // 3. Log all actions
            .WithMiddleware(SecurityValidationMiddleware)// 4. Validate inputs
            .WithMiddleware(ObservabilityMiddleware);    // 5. OpenTelemetry
    }
}
```

**Middleware Examples:**
```csharp
// Tenant isolation middleware
public class TenantIsolationMiddleware : IAgentMiddleware
{
    private readonly ITenantContext _tenantContext;

    public async Task<AgentResponse> RunAsync(
        AgentRequest request, Func<Task<AgentResponse>> next)
    {
        // Inject tenant context into all tool calls
        using var scope = _tenantContext.BeginScope(request.TenantId);

        // Validate no cross-tenant data access
        var response = await next();

        // Audit: Verify response contains no other tenant's data
        ValidateNoDataLeaks(response, request.TenantId);

        return response;
    }
}

// Token budget middleware
public class TokenBudgetMiddleware : IAgentMiddleware
{
    private readonly ITokenBudgetService _budgetService;

    public async Task<AgentResponse> RunAsync(
        AgentRequest request, Func<Task<AgentResponse>> next)
    {
        // Check budget before execution
        var budget = await _budgetService.GetRemainingBudgetAsync(
            request.TenantId, request.TicketId);

        if (budget.RemainingTokens < request.EstimatedTokens)
            throw new TokenBudgetExceededException();

        var response = await next();

        // Deduct from budget
        await _budgetService.DeductAsync(
            request.TenantId,
            request.TicketId,
            response.Usage.TotalTokens);

        return response;
    }
}
```

---

### 6. **Security-First Tool Design**

**Decision:** Every tool implements security validations from Saturn's proven patterns.

**Rationale:**
- Agents are autonomous - can't trust blindly
- Production-hardened patterns from Saturn
- Defense in depth (validation + whitelist + limits)

**Security Layers:**

**Layer 1: Tool Whitelisting**
```csharp
// AgentConfiguration table
EnabledTools = ["ReadFile", "Grep", "Glob"]  // No WriteFile

// ToolRegistry enforces
public class ToolRegistry
{
    public IEnumerable<ITool> GetTools(
        Guid tenantId, string[] enabledToolNames)
    {
        var allTools = _serviceProvider.GetServices<ITool>();

        return allTools
            .Where(t => enabledToolNames.Contains(t.Name))
            .Select(t => WrapWithTenantContext(t, tenantId));
    }
}
```

**Layer 2: Input Validation**
```csharp
public class ReadFileTool : ToolBase
{
    public override async Task<string> ExecuteAsync(
        ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");

        // 1. Path validation (prevent directory traversal)
        if (filePath.Contains("..") || Path.IsPathRooted(filePath))
            throw new SecurityException("Invalid path");

        // 2. Workspace boundary (can't read outside workspace)
        var workspacePath = context.GetWorkspacePath();
        var fullPath = Path.Combine(workspacePath, filePath);
        if (!fullPath.StartsWith(workspacePath))
            throw new SecurityException("Path outside workspace");

        // 3. Size limit (prevent reading huge files)
        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length > 10 * 1024 * 1024)  // 10MB
            throw new FileTooLargeException();

        return await File.ReadAllTextAsync(fullPath);
    }
}
```

**Layer 3: Resource Limits**
```csharp
public abstract class ToolBase : ITool
{
    protected async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<Task<TResult>> operation,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new ToolTimeoutException(Name, timeout);
        }
    }
}
```

**Layer 4: Audit Logging**
```csharp
// All tool executions logged to AgentExecutionLog
await _auditService.LogToolExecutionAsync(new ToolExecutionLog
{
    TenantId = context.TenantId,
    TicketId = context.TicketId,
    ToolName = tool.Name,
    Input = JsonSerializer.Serialize(context.Parameters),
    Output = result,
    Success = true,
    Duration = stopwatch.Elapsed
});
```

---

## Component Architecture

### AgentFactory

**Responsibility:** Create and configure AF agents at runtime from database config.

```csharp
public interface IAgentFactory
{
    Task<AIAgent> CreateAgentAsync(
        Guid tenantId,
        string agentName,
        CancellationToken ct = default);
}

public class AgentFactory : IAgentFactory
{
    private readonly IAgentConfigurationService _configService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IChatClient _chatClient;
    private readonly IEnumerable<IAgentMiddleware> _middleware;

    public async Task<AIAgent> CreateAgentAsync(
        Guid tenantId,
        string agentName,
        CancellationToken ct = default)
    {
        // 1. Load configuration from database
        var config = await _configService.GetConfigurationAsync(
            tenantId, agentName, ct);

        if (config == null)
            throw new AgentConfigurationNotFoundException(tenantId, agentName);

        // 2. Get allowed tools for this agent
        var tools = _toolRegistry.GetTools(tenantId, config.EnabledTools);

        // 3. Create base agent
        var agent = _chatClient.CreateAIAgent(
            instructions: config.Instructions,
            tools: tools.Select(t => t.ToAIFunction()).ToArray(),
            maxTokens: config.MaxTokens,
            temperature: config.Temperature);

        // 4. Apply middleware chain
        foreach (var middleware in _middleware.OrderBy(m => m.Order))
        {
            agent = agent.WithMiddleware(
                runMiddleware: middleware.RunAsync,
                functionMiddleware: middleware.FunctionAsync);
        }

        return agent;
    }
}
```

---

### ToolRegistry

**Responsibility:** Auto-discover tools, filter by permissions, wrap with tenant context.

```csharp
public interface IToolRegistry
{
    IEnumerable<ITool> GetAllTools();
    IEnumerable<ITool> GetTools(Guid tenantId, string[] enabledToolNames);
    ITool? GetTool(string toolName);
}

public class ToolRegistry : IToolRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Auto-discover all ITool implementations
        _allTools = serviceProvider.GetServices<ITool>().ToList();

        _logger.LogInformation(
            "Discovered {Count} tools: {Tools}",
            _allTools.Count,
            string.Join(", ", _allTools.Select(t => t.Name)));
    }

    public IEnumerable<ITool> GetTools(Guid tenantId, string[] enabledToolNames)
    {
        return _allTools
            .Where(t => enabledToolNames.Contains(t.Name))
            .Select(t => WrapWithTenantContext(t, tenantId))
            .ToList();
    }

    private ITool WrapWithTenantContext(ITool tool, Guid tenantId)
    {
        return new TenantAwareTool(tool, tenantId, _tenantContext);
    }
}

// Decorator pattern for tenant isolation
internal class TenantAwareTool : ITool
{
    private readonly ITool _inner;
    private readonly Guid _tenantId;
    private readonly ITenantContext _tenantContext;

    public async Task<string> ExecuteAsync(ToolExecutionContext context)
    {
        // Inject tenant context
        using var scope = _tenantContext.BeginScope(_tenantId);

        // Validate tenant isolation
        if (context.TenantId != _tenantId)
            throw new SecurityException("Tenant mismatch");

        return await _inner.ExecuteAsync(context);
    }
}
```

---

### AgentConfigurationService

**Responsibility:** CRUD operations for AgentConfiguration entity.

```csharp
public interface IAgentConfigurationService
{
    Task<AgentConfiguration?> GetConfigurationAsync(
        Guid tenantId, string agentName, CancellationToken ct = default);

    Task<List<AgentConfiguration>> GetAllConfigurationsAsync(
        Guid tenantId, CancellationToken ct = default);

    Task<AgentConfiguration> CreateConfigurationAsync(
        AgentConfiguration config, CancellationToken ct = default);

    Task UpdateConfigurationAsync(
        AgentConfiguration config, CancellationToken ct = default);

    Task DeleteConfigurationAsync(
        Guid configId, CancellationToken ct = default);
}

public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly IAgentConfigurationRepository _repo;
    private readonly ITenantContext _tenantContext;

    public async Task<AgentConfiguration?> GetConfigurationAsync(
        Guid tenantId, string agentName, CancellationToken ct = default)
    {
        // Multi-tenant query filter automatically applied
        return await _repo.GetByAgentNameAsync(tenantId, agentName, ct);
    }

    public async Task<AgentConfiguration> CreateConfigurationAsync(
        AgentConfiguration config, CancellationToken ct = default)
    {
        // Validate tenant context matches
        if (config.TenantId != _tenantContext.TenantId)
            throw new UnauthorizedAccessException("Tenant mismatch");

        // Validate tool names exist
        var validTools = _toolRegistry.GetAllTools().Select(t => t.Name).ToHashSet();
        var invalidTools = config.EnabledTools.Except(validTools).ToList();
        if (invalidTools.Any())
            throw new InvalidOperationException(
                $"Invalid tools: {string.Join(", ", invalidTools)}");

        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;

        return await _repo.CreateAsync(config, ct);
    }
}
```

---

## Integration Patterns

### Pattern 1: Graph Node Replacement (Adapter)

**Use Case:** Replace existing prompt-based agent with AF agent.

**Before (Current):**
```csharp
public class RefinementGraph : AgentGraphBase
{
    protected override void BuildGraph()
    {
        AddNode(new TriggerAgent());
        AddNode(new AnalysisAgent());         // ← Simple prompt wrapper
        AddNode(new QuestionGeneratorAgent());// ← Simple prompt wrapper
        AddNode(new HumanWaitAgent());
        AddNode(new AnswerProcessingAgent());
        AddNode(new CompletionAgent());

        AddEdge("Trigger", "Analysis");
        AddEdge("Analysis", "QuestionGenerator");
        // ...
    }
}
```

**After (AF Integration):**
```csharp
public class RefinementGraph : AgentGraphBase
{
    protected override void BuildGraph()
    {
        AddNode(new TriggerAgent());
        AddNode(new AnalysisAgentAdapter());         // ← Wraps AF agent
        AddNode(new QuestionGeneratorAgentAdapter());// ← Wraps AF agent
        AddNode(new HumanWaitAgent());
        AddNode(new AnswerProcessingAgent());
        AddNode(new CompletionAgent());

        AddEdge("Trigger", "Analysis");
        AddEdge("Analysis", "QuestionGenerator");
        // ...
    }
}
```

**Adapter Implementation:**
```csharp
public class AnalysisAgentAdapter : BaseAgent
{
    private readonly IAgentFactory _agentFactory;
    private readonly ICheckpointService _checkpointService;

    public override async Task<IAgentMessage> ExecuteAsync(
        IAgentMessage input, CancellationToken ct)
    {
        var analysisInput = (AnalysisRequestMessage)input;

        // Create AF agent
        var agent = await _agentFactory.CreateAgentAsync(
            Context.TenantId, "AnalyzerAgent", ct);

        // Build prompt with ticket context
        var prompt = $@"
Analyze Jira ticket: {analysisInput.TicketKey}

Title: {analysisInput.Title}
Description: {analysisInput.Description}

Use these tools:
- GetJiraTicket: Get full ticket details
- ReadFile: Read codebase files
- Grep: Search for code patterns
- CodeSearch: Find related code

Provide:
1. Impact assessment
2. Related code files
3. Dependencies
4. Risk analysis
";

        // Run agent with tools
        var result = await agent.RunAsync(prompt, ct);

        // Parse output and return in graph message format
        var analysis = ParseAnalysisResult(result.Output);

        return new AnalysisCompleteMessage(
            TicketId: analysisInput.TicketId,
            Impact: analysis.Impact,
            RelatedFiles: analysis.RelatedFiles,
            Dependencies: analysis.Dependencies,
            Risks: analysis.Risks);
    }
}
```

---

### Pattern 2: Checkpoint Integration

**Use Case:** Preserve graph checkpoint/resume capability with AF agents.

```csharp
public class AnalysisAgentAdapter : BaseAgent
{
    public override async Task<IAgentMessage> ExecuteAsync(
        IAgentMessage input, CancellationToken ct)
    {
        var ticketId = ((AnalysisRequestMessage)input).TicketId;

        // 1. Load existing checkpoint (if workflow resumed)
        var checkpoint = await _checkpointService.LoadAsync(
            ticketId, GraphName: "RefinementGraph", NodeName: "AnalysisAgent");

        // 2. Create AF agent
        var agent = await _agentFactory.CreateAgentAsync(
            Context.TenantId, "AnalyzerAgent", ct);

        // 3. Run with conversation history (enables multi-turn)
        var messages = checkpoint?.ConversationHistory != null
            ? JsonSerializer.Deserialize<List<ChatMessage>>(checkpoint.ConversationHistory)
            : new List<ChatMessage>();

        messages.Add(new ChatMessage { Role = "user", Content = BuildPrompt(input) });

        var result = await agent.RunAsync(messages, ct);

        // 4. Save updated checkpoint
        await _checkpointService.SaveAsync(new Checkpoint
        {
            TicketId = ticketId,
            GraphName = "RefinementGraph",
            NodeName = "AnalysisAgent",
            AgentThreadId = result.Thread.Id,
            ConversationHistory = JsonSerializer.Serialize(result.Thread.Messages),
            AgentState = JsonSerializer.Serialize(result.State),
            CreatedAt = DateTime.UtcNow
        });

        return ParseResult(result);
    }
}
```

---

### Pattern 3: Tool Permission Inheritance

**Use Case:** Different agents need different tool permissions.

**Database Configuration:**
```sql
-- AnalyzerAgent (read-only)
INSERT INTO AgentConfigurations (TenantId, AgentName, EnabledTools, ...)
VALUES (
    'tenant-123',
    'AnalyzerAgent',
    '["ReadFile", "Grep", "Glob", "CodeSearch", "GetJiraTicket"]',
    ...
);

-- CodeExecutorAgent (read-write)
INSERT INTO AgentConfigurations (TenantId, AgentName, EnabledTools, ...)
VALUES (
    'tenant-123',
    'CodeExecutorAgent',
    '["ReadFile", "WriteFile", "Grep", "Glob", "ExecuteShell", "RunTests", "GitCommit"]',
    ...
);

-- ReviewerAgent (read-only + git read)
INSERT INTO AgentConfigurations (TenantId, AgentName, EnabledTools, ...)
VALUES (
    'tenant-123',
    'ReviewerAgent',
    '["ReadFile", "Grep", "GetGitDiff", "GetJiraTicket"]',
    ...
);
```

**Runtime Enforcement:**
```csharp
// AgentFactory automatically filters tools
var analyzerAgent = await _agentFactory.CreateAgentAsync(tenantId, "AnalyzerAgent");
// Has: ReadFile, Grep, Glob, CodeSearch, GetJiraTicket

var codeExecutorAgent = await _agentFactory.CreateAgentAsync(tenantId, "CodeExecutorAgent");
// Has: ReadFile, WriteFile, Grep, Glob, ExecuteShell, RunTests, GitCommit

var reviewerAgent = await _agentFactory.CreateAgentAsync(tenantId, "ReviewerAgent");
// Has: ReadFile, Grep, GetGitDiff, GetJiraTicket
```

---

## Data Flow

### End-to-End Workflow: Ticket Analysis

```
1. Jira Webhook → PRFactory API
   POST /api/webhooks/jira
   {
     "ticketKey": "PROJ-123",
     "event": "comment_added",
     "comment": "@claude analyze this ticket"
   }

2. API → WorkflowOrchestrator
   orchestrator.StartAsync(ticketId, "RefinementGraph")

3. RefinementGraph → TriggerAgent
   message: StartWorkflowMessage

4. TriggerAgent → AnalysisAgentAdapter
   message: AnalysisRequestMessage {
     TicketId, TicketKey, Title, Description
   }

5. AnalysisAgentAdapter → AgentFactory
   agent = CreateAgentAsync(tenantId, "AnalyzerAgent")

6. AgentFactory → Database
   SELECT * FROM AgentConfigurations
   WHERE TenantId = ? AND AgentName = 'AnalyzerAgent'

7. AgentFactory → ToolRegistry
   tools = GetTools(tenantId, config.EnabledTools)

8. AgentFactory → IChatClient (AF)
   agent = CreateAIAgent(instructions, tools, maxTokens)

9. AnalysisAgentAdapter → AF Agent
   result = agent.RunAsync(prompt)

10. AF Agent → Tools (autonomous)
    - GetJiraTicket("PROJ-123")
    - Grep("Authentication", "src/**/*.cs")
    - ReadFile("src/Auth/JwtAuth.cs")
    - CodeSearch("JwtTokenValidator")

11. Each Tool → Infrastructure
    - GetJiraTicketTool → JiraService → Jira API
    - GrepTool → File System
    - ReadFileTool → File System
    - CodeSearchTool → Analysis Engine

12. Tools → AgentExecutionLog (audit)
    INSERT INTO AgentExecutionLogs (
      ToolName, Input, Output, Tokens, Duration, ...
    )

13. AF Agent → AnalysisAgentAdapter
    result: {
      Output: "Impact: Medium. Found 3 related files...",
      Usage: { InputTokens: 2500, OutputTokens: 800 }
    }

14. AnalysisAgentAdapter → CheckpointService
    SaveAsync(checkpoint with conversation history)

15. AnalysisAgentAdapter → Graph
    return AnalysisCompleteMessage {
      Impact, RelatedFiles, Dependencies, Risks
    }

16. RefinementGraph → QuestionGeneratorAgentAdapter
    (next node in graph)

17. ... (workflow continues) ...

18. Final Node → JiraPostAgent
    POST comment to Jira with refinement summary

19. WorkflowOrchestrator → Database
    UPDATE Tickets SET State = 'RefinementComplete'
```

---

## Multi-Tenancy Architecture

### Tenant Isolation Layers

**Layer 1: Database (Global Query Filters)**
```csharp
public class ApplicationDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filter for all tenant-scoped entities
        modelBuilder.Entity<AgentConfiguration>()
            .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<AgentExecutionLog>()
            .HasQueryFilter(l => l.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Ticket>()
            .HasQueryFilter(t => t.TenantId == _tenantContext.TenantId);
    }
}
```

**Layer 2: Tool Execution (Context Injection)**
```csharp
public class ToolExecutionContext
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public string WorkspacePath { get; set; }  // Isolated per tenant
    public Dictionary<string, object> Parameters { get; set; }
}

public abstract class ToolBase : ITool
{
    protected async Task<string> ExecuteAsync(ToolExecutionContext context)
    {
        // All file operations scoped to tenant workspace
        var workspacePath = GetTenantWorkspacePath(context.TenantId);

        // Validate no path traversal outside workspace
        ValidatePathWithinWorkspace(filePath, workspacePath);

        // Execute with tenant context
        using var scope = _tenantContext.BeginScope(context.TenantId);
        return await ExecuteToolAsync(context);
    }
}
```

**Layer 3: Middleware (Runtime Validation)**
```csharp
public class TenantIsolationMiddleware : IAgentMiddleware
{
    public async Task<AgentResponse> RunAsync(
        AgentRequest request, Func<Task<AgentResponse>> next)
    {
        // Set tenant context for this request
        using var scope = _tenantContext.BeginScope(request.TenantId);

        var response = await next();

        // Post-execution validation: No cross-tenant data leaks
        await ValidateResponseAsync(response, request.TenantId);

        return response;
    }

    private async Task ValidateResponseAsync(
        AgentResponse response, Guid expectedTenantId)
    {
        // Parse response for any entity IDs (ticket IDs, file paths, etc.)
        var entityIds = ExtractEntityIds(response.Output);

        foreach (var entityId in entityIds)
        {
            var actualTenantId = await _entityTenantResolver.GetTenantIdAsync(entityId);
            if (actualTenantId != expectedTenantId)
            {
                _logger.LogCritical(
                    "SECURITY VIOLATION: Cross-tenant data leak detected. " +
                    "Expected tenant {ExpectedTenantId}, found {ActualTenantId} " +
                    "in entity {EntityId}",
                    expectedTenantId, actualTenantId, entityId);

                throw new SecurityException("Cross-tenant data leak detected");
            }
        }
    }
}
```

**Layer 4: Workspace Isolation (File System)**
```
/var/prfactory/workspaces/
├── tenant-abc123/
│   ├── repos/
│   │   └── myproject/
│   │       └── src/
│   ├── temp/
│   └── .metadata
├── tenant-def456/
│   ├── repos/
│   │   └── anotherproject/
│   │       └── src/
│   ├── temp/
│   └── .metadata
```

Tools cannot access files outside their tenant workspace.

---

## Security Architecture

### Threat Model

**Threats:**
1. **Directory Traversal** - Agent reads files outside workspace
2. **SSRF** - Agent fetches malicious URLs
3. **Command Injection** - Agent executes arbitrary commands
4. **Cross-Tenant Data Leak** - Agent accesses another tenant's data
5. **Resource Exhaustion** - Agent consumes excessive tokens/time/disk
6. **Credential Theft** - Agent extracts API keys or credentials

### Mitigations

**1. Directory Traversal Prevention**
```csharp
public class ReadFileTool : ToolBase
{
    protected override async Task<string> ExecuteAsync(ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");
        var workspacePath = context.WorkspacePath;

        // Normalize paths
        var fullPath = Path.GetFullPath(Path.Combine(workspacePath, filePath));
        var normalizedWorkspace = Path.GetFullPath(workspacePath);

        // Validate within workspace
        if (!fullPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Path traversal attempt blocked: {FilePath} outside {Workspace}",
                filePath, workspacePath);

            throw new SecurityException(
                $"Access denied: Path '{filePath}' is outside workspace");
        }

        return await File.ReadAllTextAsync(fullPath);
    }
}
```

**2. SSRF Prevention**
```csharp
public class WebFetchTool : ToolBase
{
    private static readonly string[] BlockedHosts = {
        "localhost", "127.0.0.1", "::1",
        "169.254.169.254",  // AWS metadata
        "metadata.google.internal"  // GCP metadata
    };

    protected override async Task<string> ExecuteAsync(ToolExecutionContext context)
    {
        var url = context.GetParameter<string>("url");
        var uri = new Uri(url);

        // Block internal hosts
        if (BlockedHosts.Any(h =>
            uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)))
        {
            throw new SecurityException($"Access to {uri.Host} is blocked");
        }

        // Block private IP ranges
        var ip = Dns.GetHostAddresses(uri.Host).FirstOrDefault();
        if (IsPrivateIP(ip))
        {
            throw new SecurityException("Access to private IPs is blocked");
        }

        return await _httpClient.GetStringAsync(uri);
    }

    private static bool IsPrivateIP(IPAddress ip) =>
        ip.IsIPv4MappedToIPv6 && (
            ip.ToString().StartsWith("10.") ||
            ip.ToString().StartsWith("192.168.") ||
            ip.ToString().StartsWith("172."));
}
```

**3. Command Injection Prevention**
```csharp
public class ExecuteShellTool : ToolBase
{
    private static readonly string[] AllowedCommands = {
        "dotnet", "git", "npm", "docker"
    };

    protected override async Task<string> ExecuteAsync(ToolExecutionContext context)
    {
        var command = context.GetParameter<string>("command");
        var args = context.GetParameter<string[]>("args");

        // Whitelist commands
        if (!AllowedCommands.Contains(command))
        {
            throw new SecurityException(
                $"Command '{command}' is not allowed. " +
                $"Allowed: {string.Join(", ", AllowedCommands)}");
        }

        // Validate args (no shell metacharacters)
        foreach (var arg in args)
        {
            if (arg.Contains(";") || arg.Contains("|") || arg.Contains("&"))
            {
                throw new SecurityException(
                    $"Argument contains forbidden characters: {arg}");
            }
        }

        // Execute with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await ProcessExecutor.RunAsync(command, args, cts.Token);
    }
}
```

**4. Resource Limits**
```csharp
// Token budget (per tenant, per ticket)
public class TokenBudgetMiddleware : IAgentMiddleware
{
    public async Task<AgentResponse> RunAsync(
        AgentRequest request, Func<Task<AgentResponse>> next)
    {
        var budget = await _budgetService.GetBudgetAsync(
            request.TenantId, request.TicketId);

        if (budget.RemainingTokens < 1000)
            throw new TokenBudgetExceededException();

        var response = await next();

        await _budgetService.DeductAsync(
            request.TenantId,
            request.TicketId,
            response.Usage.TotalTokens);

        return response;
    }
}

// File size limits
public class ReadFileTool : ToolBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;  // 10MB

    protected override async Task<string> ExecuteAsync(ToolExecutionContext context)
    {
        var fileInfo = new FileInfo(fullPath);

        if (fileInfo.Length > MaxFileSize)
        {
            throw new FileTooLargeException(
                $"File size {fileInfo.Length} exceeds limit {MaxFileSize}");
        }

        return await File.ReadAllTextAsync(fullPath);
    }
}

// Execution timeout (per tool)
public abstract class ToolBase : ITool
{
    protected async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
        Func<Task<TResult>> operation,
        TimeSpan timeout = default)
    {
        timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;

        using var cts = new CancellationTokenSource(timeout);
        return await operation().WaitAsync(cts.Token);
    }
}
```

---

## Technology Stack

### Core Dependencies

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Agent Framework** | Microsoft.SemanticKernel | 1.0+ | AI agent orchestration |
| **LLM Client** | Anthropic.SDK / Azure.AI.OpenAI | Latest | LLM API clients |
| **Database** | EF Core + PostgreSQL | 8.0+ | Data persistence |
| **Observability** | OpenTelemetry + Azure Monitor | 1.6+ | Telemetry and monitoring |
| **Caching** | Microsoft.Extensions.Caching | 8.0+ | Response caching |
| **DI Container** | Microsoft.Extensions.DI | 8.0+ | Dependency injection |

### PRFactory.AgentTools Dependencies

| Package | Purpose |
|---------|---------|
| `PRFactory.Core` | Domain abstractions (ITenantContext, etc.) |
| `LibGit2Sharp` | Git operations (already used in PRFactory) |
| `System.Text.Json` | JSON parsing and serialization |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI support |

**No heavy dependencies** - keep tools library lightweight and reusable.

---

## Next Steps

1. **Review with team** - Validate architectural decisions
2. **Create database migration** - Add AgentConfiguration and AgentExecutionLog tables
3. **Scaffold PRFactory.AgentTools project** - Set up class library structure
4. **Port Saturn ITool pattern** - Implement core tool interfaces
5. **Implement first adapter** - AnalysisAgentAdapter for RefinementGraph
6. **Set up observability** - OpenTelemetry integration

**See:** `02_TOOLS_LIBRARY.md` for detailed tool implementation specs.
