# Saturn Tools & Agent Architecture - Quick Reference

## Overview

Saturn (https://github.com/mikaelliljedahl/Saturn) is a C#/.NET 8.0 system implementing:
- **Plugin-based Tool System** - Discoverable, composable capabilities
- **Multi-Agent Orchestration** - Parallel agent execution with capacity management
- **Agent Framework** - Built on OpenRouter API for Claude/LLM access

---

## Key Architectural Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    Agents (AgentBase)                       │
│  - Execution (sync & streaming)                             │
│  - Tool calling with auto-invoke                            │
│  - Chat history management                                  │
│  - Persistence                                              │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 Tool Registry (Singleton)                   │
│  - Auto-discovery via reflection                            │
│  - Export to agent-consumable format                        │
│  - Runtime lookup by name                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│         Individual Tools (ITool implementations)            │
│  - File operations (read, write, delete, patch)             │
│  - Search operations (grep, glob, search/replace)           │
│  - Command execution (with approval gates)                  │
│  - Web access (fetch & parse)                               │
│  - Multi-agent coordination (create, handoff, wait)         │
└─────────────────────────────────────────────────────────────┘
```

---

## Tool Interface Pattern

All tools implement `ITool`:

```csharp
interface ITool
{
    string Name { get; }                                    // Unique identifier
    string Description { get; }                             // Purpose explanation
    Dictionary<string, object> GetParameters();             // Parameter schema
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
    string GetDisplaySummary(Dictionary<string, object> parameters);
}
```

All tools extend `ToolBase`:

```csharp
public abstract class ToolBase : ITool
{
    // Derived classes override:
    public abstract string Name { get; }
    public abstract string Description { get; }
    protected abstract Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
    protected abstract Dictionary<string, object>? GetParameterProperties();
    protected abstract List<string> GetRequiredParameters();
    
    // Base class provides:
    protected T GetParameter<T>(dict, name, defaultValue)      // Safe extraction
    protected ToolResult CreateSuccessResult(object data)      // JSON formatted
    protected ToolResult CreateErrorResult(string message)     // Error wrapper
    protected string FormatPath(string path)                   // Truncate intelligently
    protected string FormatByteSize(long bytes)                // B/KB/MB/GB/TB
    protected string TruncateString(string s, int len)         // With ellipsis
}
```

---

## Tool Categories

### 1. File System Tools

| Tool | Purpose | Key Features |
|------|---------|--------------|
| **ReadFileTool** | Read file contents | Line ranges, encoding, metadata, streaming |
| **WriteFileTool** | Write files atomically | Temp file pattern, overwrite protection, 10MB limit |
| **DeleteFileTool** | Delete files/dirs | Dry-run, force flag, recursive, locking |
| **ListFilesTool** | List directory tree | Pattern filtering, depth limits, sorting, metadata |
| **ApplyDiffTool** | Apply patches | Add/Update/Delete operations, dry-run, line-ending preservation |

### 2. Search Tools

| Tool | Purpose | Key Features |
|------|---------|--------------|
| **GrepTool** | Regex text search | Recursive, case-sensitivity, max results |
| **GlobTool** | File pattern matching | Wildcards (*, **, ?), negation (!), sorting |
| **SearchAndReplaceTool** | Regex find/replace | Dry-run, preserve encoding/line endings |

### 3. Command Execution

| Tool | Purpose | Key Features |
|------|---------|--------------|
| **ExecuteCommandTool** | Run shell commands | Cross-platform shell, timeout, output capture, approval gate |

### 4. Web Operations

| Tool | Purpose | Key Features |
|------|---------|--------------|
| **WebFetchTool** | Fetch & parse web | SSRF protection, caching (5 min), multiple output formats |

### 5. Multi-Agent Tools

| Tool | Purpose | Key Features |
|------|---------|--------------|
| **CreateAgentTool** | Spawn sub-agents | Capacity checking (max 25), preference-based config |
| **HandOffToAgentTool** | Assign work | Async distribution, returns task ID immediately |
| **WaitForAgentTool** | Wait for completion | Polling-based (100ms intervals), configurable timeout |
| **GetTaskResultTool** | Retrieve result | Non-blocking, works with cached results |
| **GetAgentStatusTool** | Query agent state | Single agent or all agents |
| **TerminateAgentTool** | Stop sub-agent | Cleanup and capacity release |

---

## Tool Registry Pattern

```csharp
public class ToolRegistry
{
    private static Lazy<ToolRegistry> _instance = new(() => new ToolRegistry());
    public static ToolRegistry Instance => _instance.Value;
    
    // AUTO-DISCOVERY: Reflection scan on initialization
    public ToolRegistry()
    {
        var toolTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ITool).IsAssignableFrom(t) && 
                        !t.IsAbstract && t.IsClass);
        
        foreach (var type in toolTypes)
            _tools[CreateInstance(type).Name] = CreateInstance(type);
    }
    
    // Usage
    public ITool? Get(string name);
    public List<ITool> GetAll();
    public List<string> GetAllNames();
    public List<ToolDefinition> GetOpenRouterToolDefinitions();  // For agents
}
```

**Key Pattern**: Zero registration boilerplate - tools automatically discovered.

---

## Agent Execution Model

```csharp
public abstract class AgentBase
{
    public string Id { get; }                              // Unique identifier
    public string Name { get; }
    public string SystemPrompt { get; }
    public List<ChatMessage> ChatHistory { get; }          // Conversation context
    
    // EXECUTION
    public T Execute<T>(string prompt) where T : class
    {
        // 1. Add user message to history
        // 2. Call LLM with tools available
        // 3. If tool call -> execute and recurse
        // 4. Parse JSON result to type T
        return result;
    }
    
    public async IAsyncEnumerable<string> ExecuteStreamAsync(string prompt)
    {
        // Streaming execution with tool calling support
    }
    
    // TOOL EXECUTION
    private List<ChatMessage> HandleToolCalls(List<ToolCall> toolCalls)
    {
        foreach (var call in toolCalls)
        {
            var tool = ToolRegistry.Instance.Get(call.Name);
            var result = await tool.ExecuteAsync(call.Input);
            // Return result back to LLM
        }
    }
    
    // HISTORY
    public List<ChatMessage> GetHistory();
    public void ClearHistory();
    public void TrimHistory();  // Keep only N most recent
    
    // PERSISTENCE
    public async Task PersistMessageAsync(ChatMessage message);
    public async Task FlushPendingMessagesAsync();
    public async Task PersistToolCallAsync(string id, string name, object input);
}
```

---

## Multi-Agent Orchestration

### AgentManager - Central Coordinator

```csharp
public class AgentManager
{
    private static Lazy<AgentManager> _instance;
    public static AgentManager Instance => _instance.Value;
    
    // Capacity management (max 25 concurrent agents)
    private SemaphoreSlim _agentSemaphore = new(25);
    
    // STATE TRACKING
    private ConcurrentDictionary<string, AgentInstance> _runningAgents;
    private ConcurrentDictionary<string, AgentTaskResult> _completedTasks;
    
    // EVENTS
    public event EventHandler<AgentStatusChangedEventArgs> AgentStatusChanged;
    public event EventHandler<TaskCompletedEventArgs> TaskCompleted;
    
    // OPERATIONS
    public bool TryCreateSubAgent(string name, string purpose, SubAgentPreferences prefs);
    public string HandOffTask(string agentId, string taskDescription, string? context);
    public async Task<AgentTaskResult> WaitForTaskAsync(string taskId, int timeoutMs = 30000);
    public async Task<List<AgentTaskResult>> WaitForAllTasksAsync(List<string> taskIds, int timeoutMs);
    public AgentStatusInfo GetAgentStatus(string agentId);
    public List<AgentStatusInfo> GetAllAgentStatuses();
    public AgentTaskResult? GetTaskResult(string taskId);
    public void TerminateAgent(string agentId);
}
```

### Optional Review Phase

After task completion, optional reviewer agent validates output:

```csharp
// Reviewer iterates on original agent until:
// - Approved (ReviewStatus.Approved)
// - Rejected (ReviewStatus.Rejected)
// - Max iterations reached
// 
// Supports iterative refinement of agent outputs
```

---

## Key Design Patterns

| Pattern | Use | Example |
|---------|-----|---------|
| **Singleton** | Global access | ToolRegistry, AgentManager |
| **Template Method** | Enforce structure | ToolBase, AgentBase |
| **Strategy** | Pluggable behavior | ITool implementations |
| **Factory** | Object creation | AgentConfiguration.FromMode |
| **Registry** | Runtime discovery | ToolRegistry auto-discovery |
| **Observer** | Event notification | AgentStatusChanged events |
| **Concurrent Collections** | Thread-safe state | ConcurrentDictionary for agents/tasks |

---

## Security Patterns

### Path Validation
```csharp
private void ValidatePathSecurity(string path)
{
    if (!Path.GetFullPath(path).StartsWith(WorkingDirectory))
        throw new InvalidOperationException("Path traversal detected");
    
    if (path.Contains("..") || path.StartsWith("~"))
        throw new InvalidOperationException("Invalid path");
}
```

### Size Limits
```csharp
const int MaxFileSize = 100_000_000;  // 100 MB
const int MaxOutputSize = 1_000_000;  // 1 MB
if (fileSize > MaxFileSize)
    throw new InvalidOperationException("File too large");
```

### Timeout Enforcement
```csharp
using var cts = new CancellationTokenSource(timeoutMs);
try
{
    await LongRunningOperation(cts.Token);
}
catch (OperationCanceledException)
{
    process?.Kill();
    throw new TimeoutException();
}
```

### SSRF Protection
```csharp
// Block dangerous schemes
if (url.StartsWith("file://") || url.StartsWith("ftp://"))
    throw new InvalidOperationException("Dangerous scheme");

// Block private IPs
if (ipAddress?.IsPrivate() == true)
    throw new InvalidOperationException("Access denied");
```

### Approval Gates
```csharp
if (AgentContext.RequireCommandApproval)
{
    var approved = await _approvalService.RequestApprovalAsync(command);
    if (!approved)
        return CreateErrorResult("Denied");
}
```

---

## PRFactory Integration Strategy

### What to Adopt

| Component | Status | Reason |
|-----------|--------|--------|
| **ITool + ToolBase** | ADOPT | Production-proven pattern |
| **ToolRegistry** | ADOPT | Zero-registration auto-discovery |
| **File tools** | PORT | Security-hardened implementations |
| **AgentBase pattern** | REFERENCE | Adapt for Microsoft Agent Framework |
| **MultiAgent coordination** | EVALUATE | Use for future phase 3+ if needed |

### What to Do Differently

1. **Use Microsoft Agent Framework** instead of direct OpenRouter
2. **Keep agents within graphs**, don't make agents the top-level construct
3. **Tool selection per graph**, not global availability
4. **Dependency injection** instead of static context
5. **Event-driven** instead of polling for task coordination
6. **Policy-based approval** at workflow level, not per-command

### Implementation Phases

**Phase 1**: Port core tool interface, implement file tools
**Phase 2**: Integrate Microsoft Agent Framework, agents within graphs
**Phase 3**: Multi-agent coordination for parallel implementation (future roadmap)

---

## Common Implementation Patterns

### Creating a Tool

```csharp
public class MyTool : ToolBase
{
    public override string Name => "tool_name";
    public override string Description => "What it does";
    
    protected override List<string> GetRequiredParameters() => new() { "param1" };
    
    protected override Dictionary<string, object>? GetParameterProperties() => new()
    {
        { "param1", "Description" },
        { "param2", "Optional description" }
    };
    
    protected override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var param1 = GetParameter<string>(parameters, "param1", "");
        var param2 = GetParameter<int>(parameters, "param2", 0);
        
        // Validation
        if (string.IsNullOrEmpty(param1))
            return CreateErrorResult("param1 required");
        
        try
        {
            // Implementation
            var result = await DoWork(param1, param2);
            return CreateSuccessResult(new { Result = result });
        }
        catch (Exception ex)
        {
            return CreateErrorResult(ex.Message);
        }
    }
}
```

### Error Handling Pattern

```csharp
try
{
    // Operation
}
catch (InvalidOperationException ex) when (ex.Message.Contains("capacity"))
{
    // Specific handling with guidance
    var running = GetRunningTasks();
    return CreateErrorResult(
        $"Capacity exceeded. " +
        $"Current: {running.Count}. " +
        $"Running tasks: {string.Join(", ", running)}");
}
catch (Exception ex)
{
    // Generic error handling
    return CreateErrorResult(ex.Message);
}
```

---

## Performance Considerations

### Caching
- WebFetchTool: 5-minute TTL on fetched content

### Timeouts
- ExecuteCommandTool: 30 seconds default
- WaitForAgentTool: 30 seconds default
- HttpClient: 30 seconds

### Capacity Limits
- Max concurrent agents: 25
- Max file size: 100 MB
- Max output capture: 1 MB per stream
- Max command history entries: configurable
- Max glob results: 1000
- Max grep results: 1000

### Resource Management
- File locking prevents concurrent modifications
- Semaphore throttling limits resource exhaustion
- Temp file cleanup in finally blocks
- Process disposal after command execution

---

## Full Documentation

**See `/home/user/PRFactory/docs/SATURN_TOOLS_ANALYSIS.md` for:**
- Detailed code examples
- Pattern explanations
- Integration considerations
- Technical debt analysis
- Step-by-step implementation roadmap

---

## Repository

Saturn: https://github.com/mikaelliljedahl/Saturn
- Written in: C# (.NET 8.0)
- LLM Backend: OpenRouter API (Claude support)
- Key Directories:
  - `/Tools` - Tool system
  - `/Agents` - Agent system
  - `/Agents/MultiAgent` - Orchestration
  - `/OpenRouter` - API client
