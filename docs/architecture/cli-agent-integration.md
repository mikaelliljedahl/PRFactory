# CLI Agent Integration Architecture

## Overview

PRFactory uses an **LLM-agnostic CLI agent architecture** that abstracts AI operations behind a clean interface. This allows PRFactory to work with any CLI-based AI agent (Claude Desktop, OpenAI Codex, etc.) without coupling the core workflow logic to a specific LLM provider.

**Key Principle**: Agents execute prompts through a CLI adapter layer, making it trivial to swap LLM providers by changing dependency injection configuration.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│               Workflow Agents                           │
│  (TicketUpdateGenerationAgent, PlanningAgent, etc.)    │
└────────────────────┬────────────────────────────────────┘
                     │ Inject ICliAgent via DI
                     ▼
┌─────────────────────────────────────────────────────────┐
│          ICliAgent (Abstraction Layer)                  │
│  • ExecutePromptAsync()                                 │
│  • ExecuteWithProjectContextAsync()                     │
│  • ExecuteStreamingAsync()                              │
│  • GetCapabilities()                                    │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
┌──────────────┐ ┌──────────┐ ┌──────────────┐
│   Claude     │ │  Codex   │ │   Future     │
│   Desktop    │ │   CLI    │ │  Providers   │
│   Adapter    │ │ Adapter  │ │  (GitLab,    │
│              │ │ (Stub)   │ │   Gemini)    │
└──────┬───────┘ └──────────┘ └──────────────┘
       │
       ▼
┌──────────────────────────────┐
│   IProcessExecutor           │
│   (Safe CLI Execution)       │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│   Claude Desktop CLI         │
│   (claude --headless ...)    │
└──────────────────────────────┘
```

---

## 1. ICliAgent Interface

**Location**: `/home/user/PRFactory/src/PRFactory.Core/Application/Services/ICliAgent.cs`

**Purpose**: LLM-agnostic abstraction for AI operations

### Interface Definition

```csharp
public interface ICliAgent
{
    /// <summary>
    /// Name of the CLI agent (e.g., "Claude Desktop CLI", "Codex CLI")
    /// </summary>
    string AgentName { get; }

    /// <summary>
    /// Indicates whether the agent supports streaming output
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Gets the capabilities of this CLI agent
    /// </summary>
    CliAgentCapabilities GetCapabilities();

    /// <summary>
    /// Executes a prompt and returns the response
    /// </summary>
    /// <param name="prompt">The prompt to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the CLI agent</returns>
    Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with full project context (codebase awareness)
    /// </summary>
    /// <param name="prompt">The prompt to execute</param>
    /// <param name="projectPath">Path to project directory for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the CLI agent</returns>
    Task<CliAgentResponse> ExecuteWithProjectContextAsync(
        string prompt,
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with streaming output
    /// </summary>
    /// <param name="prompt">The prompt to execute</param>
    /// <param name="onOutputReceived">Callback for each output chunk</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final aggregated response</returns>
    Task<CliAgentResponse> ExecuteStreamingAsync(
        string prompt,
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default);
}
```

### Response Object

```csharp
public class CliAgentResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<FileOperation> FileOperations { get; set; } = new();
    public int ExitCode { get; set; }
}

public class FileOperation
{
    public string Path { get; set; } = string.Empty;
    public FileOperationType Type { get; set; } // Create, Update, Delete
    public string Content { get; set; } = string.Empty;
}
```

### Capabilities Object

```csharp
public class CliAgentCapabilities
{
    public bool SupportsCodeGeneration { get; set; }
    public bool SupportsFileOperations { get; set; }
    public bool SupportsProjectContext { get; set; }
    public bool SupportsStreaming { get; set; }
    public int MaxTokens { get; set; }
    public List<string> SupportedFormats { get; set; } // JSON, Markdown, Text
    public string ModelName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

---

## 2. ClaudeDesktopCliAdapter (Production Implementation)

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Adapters/ClaudeDesktopCliAdapter.cs`

**Status**: ✅ **Fully Implemented**

### Configuration

```csharp
public class ClaudeDesktopCliOptions
{
    public string ExecutablePath { get; set; } = "claude";
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes
    public int ProjectContextTimeoutSeconds { get; set; } = 600; // 10 minutes
    public int StreamingTimeoutSeconds { get; set; } = 300;
    public bool EnableVerboseLogging { get; set; } = false;
}
```

### Implementation Highlights

**Capabilities**:
- Code generation: ✅
- File operations: ✅
- Project context: ✅
- Streaming: ✅
- Max tokens: 200,000
- Supported formats: JSON, Markdown, Text
- Model: claude-sonnet-4-5-20250929

**CLI Command Examples**:
```bash
# Simple prompt
claude --headless --prompt "Generate a function to calculate Fibonacci numbers"

# With project context (full codebase awareness)
claude --headless --project-path "/path/to/project" --prompt "Add logging to UserService.cs"

# Streaming mode (implied by real-time processing)
claude --headless --prompt "Generate 1000 lines of code" # Streams output as generated
```

**Safe Argument Passing**:
```csharp
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = _options.ExecutablePath,
        ArgumentList = // Safe: no shell injection risk
        {
            "--headless",
            "--prompt",
            prompt
        },
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    }
};
```

**Response Processing**:
1. Execute CLI process via `IProcessExecutor`
2. Capture stdout and stderr
3. Parse JSON from markdown code blocks: `` ```json ... ``` ``
4. Extract metadata (tokens, model name)
5. Parse file operations from structured output
6. Return `CliAgentResponse`

**Error Handling**:
- Timeout detection (kills process tree)
- Cancellation support (CancellationToken)
- Non-zero exit codes → `Success = false`
- stderr captured in `ErrorMessage`
- Detailed logging with execution duration

---

## 3. CodexCliAdapter (Stub - Future)

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Adapters/CodexCliAdapter.cs`

**Status**: ❌ **Placeholder Only**

All methods throw `NotImplementedException` with guidance:
```csharp
public Task<CliAgentResponse> ExecutePromptAsync(string prompt, CancellationToken ct)
{
    throw new NotImplementedException(
        "Codex CLI adapter not yet implemented. " +
        "Implement using OpenAI Codex CLI interface.");
}
```

**Planned Capabilities**:
- Code generation: ✅
- File operations: ✅
- Project context: ✅
- Streaming: ❌ (not supported by Codex)
- Max tokens: 8,000
- Supported formats: JSON, Text

---

## 4. How Workflow Agents Use ICliAgent

### Example 1: TicketUpdateGenerationAgent

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/TicketUpdateGenerationAgent.cs`

**Purpose**: Generate refined ticket descriptions with SMART success criteria

**Usage Pattern**:
```csharp
public class TicketUpdateGenerationAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    public TicketUpdateGenerationAgent(
        ILogger<TicketUpdateGenerationAgent> logger,
        ICliAgent cliAgent, // Injected via DI
        ITicketUpdateRepository ticketUpdateRepo,
        ITicketRepository ticketRepo)
        : base(logger)
    {
        _cliAgent = cliAgent;
        // ...
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken ct)
    {
        // Build structured prompt
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(context, rejectionContext);
        var prompt = $"{systemPrompt}\n\n{userPrompt}";

        // Execute via CLI agent (LLM-agnostic)
        var response = await _cliAgent.ExecutePromptAsync(prompt, ct);

        if (!response.Success)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"CLI agent failed: {response.ErrorMessage}"
            };
        }

        // Parse JSON response
        var json = ExtractJsonFromResponse(response.Content);
        var dto = JsonSerializer.Deserialize<TicketUpdateDto>(json);

        // Process results...
        var ticketUpdate = TicketUpdate.Create(...);
        await _ticketUpdateRepo.CreateAsync(ticketUpdate, ct);

        return new AgentResult { Status = AgentStatus.Completed };
    }
}
```

### Example 2: PlanningAgent with Project Context

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/PlanningAgent.cs`

**Purpose**: Generate implementation plans with full codebase awareness

**Usage Pattern**:
```csharp
public class PlanningAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken ct)
    {
        // Build planning prompt (includes ticket, answers, analysis)
        var prompt = BuildPlanningPrompt(context);

        // Execute WITH PROJECT CONTEXT for codebase awareness
        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath, // Full codebase access
            ct
        );

        // Store implementation plan
        context.State["ImplementationPlan"] = response.Content;

        return new AgentResult { Status = AgentStatus.Completed };
    }
}
```

### Agents Using ICliAgent

1. **TicketUpdateGenerationAgent** - Refined ticket descriptions
2. **PlanningAgent** - Implementation plans with project context
3. **ImplementationAgent** - Code generation (optional)
4. **QuestionGenerationAgent** - Clarifying questions
5. **AnalysisAgent** - Codebase architecture analysis

---

## 5. Agent Prompt Management

### IAgentPromptService

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Services/IAgentPromptService.cs`

**Purpose**: Retrieve reusable prompt templates for agents

**Methods**:
```csharp
public interface IAgentPromptService
{
    Task<AgentPromptTemplate?> GetPromptTemplateAsync(
        string name,
        Guid? tenantId = null,
        CancellationToken ct = default);

    Task<List<AgentPromptTemplate>> GetPromptTemplatesByCategoryAsync(
        string category,
        Guid? tenantId = null,
        CancellationToken ct = default);

    Task<string?> GetPromptContentAsync(
        string name,
        Guid? tenantId = null,
        CancellationToken ct = default);

    Task<List<AgentPromptTemplate>> GetAvailableTemplatesAsync(
        Guid tenantId,
        CancellationToken ct = default);
}
```

### AgentPromptTemplate Entity

**Properties**:
- `Id` - Unique identifier
- `Name` - Template name (e.g., "code-implementation-specialist")
- `Description` - What the agent does
- `PromptContent` - Full prompt with POML markup
- `RecommendedModel` - "sonnet", "opus", "haiku"
- `Color` - UI visual identifier
- `Category` - "Implementation", "Planning", "Analysis", "Testing", "Evaluation"
- `IsSystemTemplate` - Read-only system vs. user-created
- `TenantId` - Tenant ownership (null for system)

**Template Storage**:
- **System Templates**: Loaded from `.claude/agents/*.md` files
- **Tenant Templates**: Database, editable per tenant
- **Fallback Logic**: Tenant-specific → System template

### Prompt Template Files

**Location**: `/home/user/PRFactory/.claude/agents/`

**Available Templates**:
1. `code-implementation.md` - code-implementation-specialist
2. `evaluation-specialist.md` - evaluation-specialist
3. `test-analysis-specialist.md` - test-analysis-specialist
4. `test-fix-specialist.md` - test-fix-specialist
5. `test-runner-specialist.md` - test-runner-specialist
6. `simple-code-implementation-agent.md` - simple-code-implementation-agent

**YAML Frontmatter Format**:
```yaml
---
name: code-implementation-specialist
description: Use this agent when you need to implement coding tasks from detailed specifications
model: sonnet
color: blue
---
```

**Prompt Content**: POML (Prompt Markup Language)
```xml
<poml>
<role>You are a precise software implementation specialist...</role>
<task>Complete the exact requirements - nothing more, nothing less.</task>
<cp caption="Core Implementation Principles">
- Maintain consistency across files
- Ensure code compiles
- Follow existing patterns
</cp>
</poml>
```

### AgentPromptLoaderService

**Purpose**: Load `.claude/agents/*.md` files into database

**Process**:
1. Scan `.claude/agents/` directory
2. Parse YAML frontmatter
3. Extract prompt content after `---`
4. Auto-detect category from description keywords
5. Create `AgentPromptTemplate.CreateSystemTemplate(...)`
6. Bulk insert to database (skip duplicates)

**Invoked**: Application startup via hosted service

---

## 6. Process Execution Layer

### IProcessExecutor

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Execution/ProcessExecutor.cs`

**Purpose**: Safe CLI process execution with timeout and cancellation

**Methods**:
```csharp
public interface IProcessExecutor
{
    Task<ProcessExecutionResult> ExecuteAsync(
        string fileName,
        IEnumerable<string> argumentList, // Safe: no shell injection
        string? workingDirectory = null,
        int timeoutSeconds = 300,
        CancellationToken ct = default);

    Task<ProcessExecutionResult> ExecuteStreamingAsync(
        string fileName,
        IEnumerable<string> argumentList,
        Action<string> onOutputReceived,
        Action<string> onErrorReceived,
        string? workingDirectory = null,
        int timeoutSeconds = 300,
        CancellationToken ct = default);
}
```

**Features**:
- **Safe Arguments**: Uses `ProcessStartInfo.ArgumentList` (no manual escaping)
- **Timeout Support**: Configurable timeouts with process tree termination
- **Cancellation**: Respects `CancellationToken`
- **Streaming Callbacks**: Real-time output processing
- **Process Tree Killing**: Kills child processes on timeout/cancellation
- **Detailed Logging**: Process ID, exit codes, duration

**Example Usage**:
```csharp
var result = await _processExecutor.ExecuteAsync(
    fileName: "claude",
    argumentList: new[] { "--headless", "--prompt", userPrompt },
    workingDirectory: "/path/to/project",
    timeoutSeconds: 300,
    ct: cancellationToken
);

if (result.Success)
{
    Console.WriteLine($"Output: {result.Output}");
}
else
{
    Console.WriteLine($"Error: {result.Error} (Exit Code: {result.ExitCode})");
}
```

---

## 7. Dependency Injection Configuration

### Registration

**Location**: `/home/user/PRFactory/src/PRFactory.Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Process executor
    services.AddScoped<IProcessExecutor, ProcessExecutor>();

    // Configuration
    services.Configure<ClaudeDesktopCliOptions>(
        configuration.GetSection("ClaudeDesktopCli"));

    // CLI Adapters
    services.AddScoped<ClaudeDesktopCliAdapter>();
    services.AddScoped<CodexCliAdapter>();

    // Default CLI agent (Claude Desktop)
    services.AddScoped<ICliAgent>(sp =>
        sp.GetRequiredService<ClaudeDesktopCliAdapter>());

    // Agent prompt service
    services.AddScoped<IAgentPromptService, AgentPromptService>();
    services.AddScoped<IAgentPromptLoaderService, AgentPromptLoaderService>();

    return services;
}
```

### Configuration (appsettings.json)

```json
{
  "ClaudeDesktopCli": {
    "ExecutablePath": "claude",
    "DefaultTimeoutSeconds": 300,
    "ProjectContextTimeoutSeconds": 600,
    "StreamingTimeoutSeconds": 300,
    "EnableVerboseLogging": false
  }
}
```

---

## 8. Switching LLM Providers

### Simple Provider Swap

To switch from Claude to Codex (once implemented):

```csharp
// Change ONE LINE in DependencyInjection.cs:
services.AddScoped<ICliAgent>(sp =>
    sp.GetRequiredService<CodexCliAdapter>()); // Changed from ClaudeDesktopCliAdapter
```

**No other code changes needed** - all workflow agents use `ICliAgent` abstraction.

### Multi-Tenant Provider Selection

For different providers per tenant:

```csharp
services.AddScoped<ICliAgent>(sp =>
{
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    var tenant = tenantContext.GetCurrentTenant();

    return tenant.PreferredLlmProvider switch
    {
        "Claude" => sp.GetRequiredService<ClaudeDesktopCliAdapter>(),
        "Codex" => sp.GetRequiredService<CodexCliAdapter>(),
        _ => sp.GetRequiredService<ClaudeDesktopCliAdapter>()
    };
});
```

---

## 9. Testing Strategy

### Mocking ICliAgent

```csharp
[Fact]
public async Task TicketUpdateGenerationAgent_Should_Generate_Update()
{
    // Arrange
    var mockCliAgent = new Mock<ICliAgent>();
    mockCliAgent
        .Setup(x => x.ExecutePromptAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CliAgentResponse
        {
            Success = true,
            Content = GetMockTicketUpdateJson()
        });

    var agent = new TicketUpdateGenerationAgent(
        logger,
        mockCliAgent.Object,
        ticketUpdateRepo,
        ticketRepo
    );

    // Act
    var result = await agent.ExecuteAsync(context, CancellationToken.None);

    // Assert
    Assert.Equal(AgentStatus.Completed, result.Status);
    mockCliAgent.Verify(x => x.ExecutePromptAsync(
        It.Is<string>(p => p.Contains("Generate a refined ticket")),
        It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Integration Testing with Real CLI

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task ClaudeDesktopCliAdapter_Should_Execute_Real_Prompt()
{
    // Arrange
    var options = new ClaudeDesktopCliOptions
    {
        ExecutablePath = "claude",
        DefaultTimeoutSeconds = 60
    };
    var processExecutor = new ProcessExecutor(logger);
    var adapter = new ClaudeDesktopCliAdapter(options, processExecutor, logger);

    // Act
    var response = await adapter.ExecutePromptAsync(
        "What is 2+2?",
        CancellationToken.None
    );

    // Assert
    Assert.True(response.Success);
    Assert.Contains("4", response.Content);
}
```

---

## 10. Error Handling and Resilience

### Timeout Handling

```csharp
try
{
    var response = await _cliAgent.ExecutePromptAsync(prompt, ct);
}
catch (TimeoutException ex)
{
    Logger.LogError("CLI agent timed out after {Timeout}s", timeout);
    return new AgentResult
    {
        Status = AgentStatus.Failed,
        Error = "LLM operation timed out"
    };
}
```

### Cancellation Handling

```csharp
try
{
    var response = await _cliAgent.ExecutePromptAsync(prompt, ct);
}
catch (OperationCanceledException)
{
    Logger.LogWarning("CLI agent operation cancelled");
    return new AgentResult { Status = AgentStatus.Cancelled };
}
```

### Retry Logic (Agent-Level)

```csharp
for (int attempt = 1; attempt <= MaxRetries; attempt++)
{
    var response = await _cliAgent.ExecutePromptAsync(prompt, ct);

    if (response.Success)
    {
        return ParseResponse(response.Content);
    }

    if (attempt < MaxRetries)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        await Task.Delay(delay, ct);
    }
}

throw new AgentExecutionException("Max retries exceeded");
```

---

## 11. Performance Considerations

### Token Usage Tracking

```csharp
var response = await _cliAgent.ExecutePromptAsync(prompt, ct);

if (response.Metadata.TryGetValue("InputTokens", out var inputTokens))
{
    Logger.LogInformation("Tokens used: {Input} input", inputTokens);
}
```

### Project Context Optimization

**Problem**: Full project context is expensive (high token count)

**Solution**: Selective context loading
```csharp
// Only use project context when necessary
if (requiresCodebaseAwareness)
{
    response = await _cliAgent.ExecuteWithProjectContextAsync(
        prompt,
        repositoryPath,
        ct
    );
}
else
{
    response = await _cliAgent.ExecutePromptAsync(prompt, ct);
}
```

### Caching Strategies

**Future Enhancement**: Cache LLM responses for identical prompts
```csharp
var cacheKey = $"prompt:{HashPrompt(prompt)}";
if (_cache.TryGet(cacheKey, out CliAgentResponse cached))
{
    return cached;
}

var response = await _cliAgent.ExecutePromptAsync(prompt, ct);
_cache.Set(cacheKey, response, TimeSpan.FromMinutes(30));
return response;
```

---

## 12. Security Considerations

### Input Validation

```csharp
if (string.IsNullOrWhiteSpace(prompt))
{
    throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
}

if (prompt.Length > MaxPromptLength)
{
    throw new ArgumentException(
        $"Prompt exceeds max length of {MaxPromptLength}",
        nameof(prompt)
    );
}
```

### Safe Argument Passing

**Never** concatenate arguments into a shell command:
```csharp
// ❌ WRONG - Shell injection risk
var command = $"claude --prompt \"{userPrompt}\"";
Process.Start("sh", $"-c \"{command}\"");

// ✅ CORRECT - Safe argument passing
process.StartInfo.ArgumentList.Add("--prompt");
process.StartInfo.ArgumentList.Add(userPrompt); // Automatically escaped
```

### Credential Management

- API keys stored encrypted in database
- Environment variables for development
- Azure Key Vault for production
- Never log full prompts (may contain sensitive data)

---

## 13. Future Enhancements

### Planned Improvements

1. **Additional Adapters**:
   - GitLab CodeSuggestions CLI
   - Google Gemini CLI
   - Azure OpenAI Service CLI
   - Self-hosted LLM CLI (Ollama, LM Studio)

2. **Enhanced Features**:
   - Response caching
   - Token usage analytics dashboard
   - Cost tracking per tenant
   - Prompt versioning
   - A/B testing different prompts

3. **Performance**:
   - Parallel LLM calls for independent operations
   - Streaming UI updates
   - Progressive enhancement (show partial results)

4. **Quality**:
   - LLM response validation
   - Automated quality scoring
   - Fallback to alternative providers on failure

---

## Summary

PRFactory's CLI agent integration is **production-ready and fully LLM-agnostic**:

✅ **Clean Abstraction**: `ICliAgent` interface decouples workflows from LLM providers
✅ **Production Adapter**: `ClaudeDesktopCliAdapter` fully implemented and tested
✅ **Safe Execution**: No shell injection, timeout/cancellation support
✅ **Extensible**: Add new providers by implementing `ICliAgent`
✅ **Testable**: Mock `ICliAgent` for unit tests, integration tests with real CLI
✅ **Project Context**: Full codebase awareness for planning and implementation
✅ **Prompt Management**: Reusable templates with tenant customization
✅ **Error Resilient**: Timeout, cancellation, and retry support

The architecture allows PRFactory to:
- **Switch LLM providers** with one line of DI configuration
- **Support multiple providers** simultaneously (multi-tenant)
- **Test in isolation** with mock adapters
- **Scale** to new LLM providers without core code changes
- **Customize** prompts per tenant without redeployment

**Status**: ✅ CURRENT PRODUCTION ARCHITECTURE
