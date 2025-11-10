# Epic 2: AI Agnosticism (Multi-LLM Support)

**Status:** 🟡 Partially Complete (Claude Code CLI only)
**Priority:** P1 (Critical)
**Effort:** 2-3 weeks
**Dependencies:** None

---

## Strategic Goal

Remove vendor lock-in to Claude AI. Support multiple LLM providers (Anthropic, OpenAI, Google) to give customers choice and flexibility.

**Current Pain:** PRFactory is locked to Claude AI. Customers want GPT-4, Gemini, or other models. We're accumulating technical debt.

**Solution:** Build provider-agnostic architecture with `ILlmProvider` interface, factory pattern, and externalized prompts.

---

## Success Criteria

✅ **Must Have:**
- Global CLI parameters: `--provider anthropic|openai|google` and `--model <model-name>`
- `ILlmProvider` interface abstracts LLM API calls
- `LlmProviderFactory` instantiates correct provider based on configuration
- Externalized prompt templates (different prompts for different providers)
- Support for Claude Code CLI (already exists)
- Support for at least one additional provider (Gemini or OpenAI)

✅ **Nice to Have:**
- Support for all three providers (Claude, Gemini, OpenAI)
- Automatic fallback to secondary provider if primary fails
- Per-tenant provider selection (different customers use different providers)
- Usage metrics and cost tracking per provider

---

## Why CLI-Based Integration?

We're using CLI-based LLM providers (not direct API calls) for pragmatic reasons:

**✅ Benefits:**
- **Authentication Handled:** CLIs manage their own OAuth flows and token refresh
- **Feature Parity:** CLIs expose the same capabilities as web interfaces
- **Headless Support:** All CLIs support non-interactive execution
- **No Token Management:** No need to build OAuth infrastructure for each provider
- **Consistent Interface:** All CLIs follow similar command patterns

**❌ Tradeoffs:**
- **External Dependency:** Requires CLI installation on host system
- **Process Overhead:** Subprocess execution adds latency vs direct API calls
- **Less Control:** Can't customize every aspect of API interaction

**Decision:** Start with CLI approach (simpler, faster). Optionally add direct API support later (see Epic 5: Agent Framework).

---

## Architecture

### Current Architecture (Claude Code CLI Only)

```
Workflow Graphs
  ↓
Agents (Analysis/Planning/Implementation)
  ↓
ClaudeCodeCliAdapter (hardcoded!)
  ↓
ProcessExecutor
  ↓
claude --headless --model <model> "prompt"
```

### Target Architecture (Multi-Provider)

```
Workflow Graphs
  ↓
Agents (Analysis/Planning/Implementation)
  ↓
ILlmProvider Interface (abstraction)
  ↓
LlmProviderFactory (provider selection)
  ↓
┌───────────────┬──────────────────┬──────────────────┐
│               │                  │                  │
▼               ▼                  ▼                  ▼
ClaudeCodeCli   GeminiCli          OpenAiCli          [Future providers]
Adapter         Adapter            Adapter
  ↓               ↓                  ↓
ProcessExecutor ProcessExecutor    ProcessExecutor
  ↓               ↓                  ↓
claude          gemini             openai
--headless      --headless         codex
```

---

## Implementation Plan

### 1. Core Abstraction: `ILlmProvider` Interface

**Create:** `/PRFactory.Core/Application/LLM/ILlmProvider.cs`

```csharp
namespace PRFactory.Core.Application.LLM;

public interface ILlmProvider
{
    /// <summary>
    /// Provider name (e.g., "Anthropic", "OpenAI", "Google")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Supported models for this provider
    /// </summary>
    List<string> SupportedModels { get; }

    /// <summary>
    /// Execute a prompt and return response
    /// </summary>
    Task<LlmResponse> SendMessageAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Execute with streaming response
    /// </summary>
    Task<LlmStreamingResponse> SendMessageStreamAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Execute with project context (provide codebase path)
    /// </summary>
    Task<LlmResponse> SendMessageWithContextAsync(
        string prompt,
        string projectPath,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Check if provider is healthy (CLI installed, authenticated)
    /// </summary>
    Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken ct = default);
}

public class LlmOptions
{
    public string? Model { get; set; }
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class LlmResponse
{
    public bool Success { get; set; }
    public string Content { get; set; }
    public string? ErrorMessage { get; set; }
    public LlmUsageMetrics Usage { get; set; }
}

public class LlmUsageMetrics
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public TimeSpan Latency { get; set; }
}

public class ProviderHealthStatus
{
    public bool IsHealthy { get; set; }
    public string StatusMessage { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsAuthenticated { get; set; }
}
```

---

### 2. Provider Factory Pattern

**Create:** `/PRFactory.Core/Application/LLM/ILlmProviderFactory.cs`

```csharp
public interface ILlmProviderFactory
{
    /// <summary>
    /// Create provider by name
    /// </summary>
    ILlmProvider CreateProvider(string providerName);

    /// <summary>
    /// Get default provider from configuration
    /// </summary>
    ILlmProvider GetDefaultProvider();

    /// <summary>
    /// Get provider with fallback logic
    /// </summary>
    ILlmProvider GetProviderWithFallback(string primaryProvider, string? fallbackProvider = null);

    /// <summary>
    /// List all available providers
    /// </summary>
    List<string> GetAvailableProviders();
}
```

**Implement:** `/PRFactory.Infrastructure/Application/LlmProviderFactory.cs`

```csharp
public class LlmProviderFactory : ILlmProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LlmProvidersOptions _options;

    public LlmProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<LlmProvidersOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public ILlmProvider CreateProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "anthropic" or "claude" =>
                _serviceProvider.GetRequiredService<ClaudeCodeCliAdapter>(),

            "google" or "gemini" =>
                _serviceProvider.GetRequiredService<GeminiCliAdapter>(),

            "openai" or "gpt" =>
                _serviceProvider.GetRequiredService<OpenAiCliAdapter>(),

            _ => throw new NotSupportedException($"Provider '{providerName}' not supported")
        };
    }

    public ILlmProvider GetDefaultProvider()
    {
        var defaultProvider = _options.DefaultProvider ?? "anthropic";
        return CreateProvider(defaultProvider);
    }

    public ILlmProvider GetProviderWithFallback(string primaryProvider, string? fallbackProvider = null)
    {
        try
        {
            var provider = CreateProvider(primaryProvider);

            // Check health
            var health = provider.CheckHealthAsync().Result;
            if (health.IsHealthy)
                return provider;
        }
        catch
        {
            // Primary failed, try fallback
        }

        // Use fallback or configured fallback
        var fallback = fallbackProvider ?? _options.FallbackProvider ?? "anthropic";
        return CreateProvider(fallback);
    }

    public List<string> GetAvailableProviders()
    {
        return new List<string> { "anthropic", "google", "openai" };
    }
}
```

---

### 3. Global CLI Parameters

**Create:** `/CLI/Options/BaseAgentOptions.cs` (exact path TBD based on CLI structure)

```csharp
public class BaseAgentOptions
{
    [Option("--provider", Required = false, Default = "anthropic",
        HelpText = "LLM provider to use (anthropic, openai, google)")]
    public string Provider { get; set; } = "anthropic";

    [Option("--model", Required = false,
        HelpText = "Model name (e.g., claude-sonnet-4-5, gpt-4o, gemini-pro)")]
    public string? Model { get; set; }

    [Option("--max-tokens", Required = false, Default = 8000,
        HelpText = "Maximum tokens in response")]
    public int MaxTokens { get; set; } = 8000;

    [Option("--temperature", Required = false, Default = 0.7,
        HelpText = "Temperature (0.0-1.0)")]
    public double Temperature { get; set; } = 0.7;

    [Option("--timeout", Required = false, Default = 300,
        HelpText = "Timeout in seconds")]
    public int TimeoutSeconds { get; set; } = 300;
}
```

**Usage in CLI Commands:**

```csharp
public class PlanCommand : BaseAgentOptions  // Inherits provider options
{
    [Option("--issue", Required = true)]
    public string IssueKey { get; set; }

    public async Task<int> ExecuteAsync()
    {
        // Get provider from factory
        var provider = _factory.CreateProvider(this.Provider);

        // Load prompt template
        var promptTemplate = _promptLoader.LoadPrompt("plan", this.Provider, "system");

        // Execute
        var response = await provider.SendMessageAsync(
            prompt: GeneratePrompt(IssueKey),
            systemPrompt: promptTemplate,
            options: new LlmOptions
            {
                Model = this.Model,
                MaxTokens = this.MaxTokens,
                Temperature = this.Temperature,
                TimeoutSeconds = this.TimeoutSeconds
            });

        // Output
        Console.WriteLine(response.Content);
        return response.Success ? 0 : 1;
    }
}
```

**Example CLI Usage:**

```bash
# Use Claude (default)
cli plan --issue PROJ-123

# Use GPT-4
cli plan --issue PROJ-123 --provider openai --model gpt-4o

# Use Gemini
cli plan --issue PROJ-123 --provider google --model gemini-pro

# Override all parameters
cli plan --issue PROJ-123 \
  --provider openai \
  --model gpt-4-turbo \
  --max-tokens 4000 \
  --temperature 0.5
```

---

### 4. Externalized Prompt Templates

**Problem:** Different models have different prompting requirements:
- Claude: Prefers detailed system prompts with examples
- GPT-4: Shorter system prompts, more structured user prompts
- Gemini: Different XML tag formats

**Solution:** Load prompts from external files based on agent and provider.

**Directory Structure:**

```
/prompts/
├── plan/
│   ├── anthropic/
│   │   ├── system.txt
│   │   └── user_template.md
│   ├── openai/
│   │   ├── system.txt
│   │   └── user_template.md
│   └── google/
│       ├── system.txt
│       └── user_template.md
├── code/
│   ├── anthropic/
│   ├── openai/
│   └── google/
├── test/
│   ├── anthropic/
│   ├── openai/
│   └── google/
└── review/
    ├── anthropic/
    ├── openai/
    └── google/
```

**Example Prompts:**

`/prompts/plan/anthropic/system.txt`:
```
You are an expert software architect working on a complex enterprise system.

Your task is to analyze Jira tickets and generate comprehensive implementation plans.

Follow these principles:
1. Be thorough - consider edge cases, error handling, testing
2. Be specific - provide concrete file paths, method names, data structures
3. Be actionable - engineers should be able to implement directly from your plan

Output Format:
- Clear markdown structure
- Step-by-step implementation guide
- Database schema changes (SQL DDL)
- API endpoint specifications
- Test case descriptions
```

`/prompts/plan/openai/system.txt`:
```
You are a software architect. Generate detailed implementation plans from Jira tickets.

Output: Markdown with implementation steps, database changes, API specs, and test cases.
```

`/prompts/plan/anthropic/user_template.md`:
```
<ticket>
{ticket_description}
</ticket>

<codebase_context>
{codebase_files}
</codebase_context>

Generate a comprehensive implementation plan for this ticket.
```

**Prompt Loader Service:**

**Create:** `/PRFactory.Infrastructure/Application/PromptLoaderService.cs`

```csharp
public interface IPromptLoaderService
{
    string LoadPrompt(string agentName, string providerName, string promptType);
}

public class PromptLoaderService : IPromptLoaderService
{
    private readonly string _promptsBasePath;

    public PromptLoaderService(IConfiguration config)
    {
        _promptsBasePath = config["Prompts:BasePath"] ?? "/prompts";
    }

    public string LoadPrompt(string agentName, string providerName, string promptType)
    {
        var path = Path.Combine(
            _promptsBasePath,
            agentName.ToLowerInvariant(),
            providerName.ToLowerInvariant(),
            $"{promptType}.txt");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Prompt template not found: {path}");

        return File.ReadAllText(path);
    }
}
```

**Templating Engine:**

Use simple string replacement or Scriban for complex templates:

```csharp
// Simple approach (string.Replace)
var userPrompt = template
    .Replace("{ticket_description}", ticket.Description)
    .Replace("{codebase_files}", codebaseContext);

// Advanced approach (Scriban)
var template = Template.Parse(File.ReadAllText(templatePath));
var userPrompt = template.Render(new
{
    ticket_description = ticket.Description,
    codebase_files = codebaseContext
});
```

---

### 5. Provider Implementations

#### 5.1 Claude Code CLI Adapter (Already Exists - Enhance)

**File:** `/PRFactory.Infrastructure/Agents/Adapters/ClaudeCodeCliAdapter.cs`

**Enhancements Needed:**
- Implement `ILlmProvider` interface
- Add health check method
- Add usage metrics extraction
- Add retry logic

```csharp
public class ClaudeCodeCliAdapter : ILlmProvider
{
    public string ProviderName => "Anthropic (Claude Code CLI)";

    public List<string> SupportedModels => new()
    {
        "claude-sonnet-4-5-20250929",
        "claude-opus-4-20250514",
        "claude-3-5-sonnet-20241022"
    };

    public async Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken ct)
    {
        try
        {
            // Check if CLI installed
            var versionResult = await _processExecutor.ExecuteAsync(
                "claude", new[] { "--version" }, timeout: TimeSpan.FromSeconds(5), ct);

            if (versionResult.ExitCode != 0)
                return new ProviderHealthStatus
                {
                    IsHealthy = false,
                    IsInstalled = false,
                    StatusMessage = "Claude Code CLI not installed"
                };

            // Check if authenticated
            var authResult = await _processExecutor.ExecuteAsync(
                "claude", new[] { "auth", "status" }, timeout: TimeSpan.FromSeconds(5), ct);

            if (authResult.ExitCode != 0)
                return new ProviderHealthStatus
                {
                    IsHealthy = false,
                    IsInstalled = true,
                    IsAuthenticated = false,
                    StatusMessage = "Claude Code CLI not authenticated. Run: claude auth login"
                };

            return new ProviderHealthStatus
            {
                IsHealthy = true,
                IsInstalled = true,
                IsAuthenticated = true,
                StatusMessage = "Claude Code CLI ready"
            };
        }
        catch (Exception ex)
        {
            return new ProviderHealthStatus
            {
                IsHealthy = false,
                StatusMessage = $"Error checking health: {ex.Message}"
            };
        }
    }

    // ... Implement other ILlmProvider methods
}
```

#### 5.2 Gemini CLI Adapter (New)

**Create:** `/PRFactory.Infrastructure/Agents/Adapters/GeminiCliAdapter.cs`

```csharp
public class GeminiCliAdapter : ILlmProvider
{
    public string ProviderName => "Google (Gemini CLI)";

    public List<string> SupportedModels => new()
    {
        "gemini-pro",
        "gemini-pro-vision",
        "gemini-ultra"
    };

    // TODO: Research actual Gemini CLI commands
    // This is a placeholder based on expected patterns

    public async Task<LlmResponse> SendMessageAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        var args = new List<string>
        {
            "--headless",
            "--model", options?.Model ?? "gemini-pro"
        };

        if (systemPrompt != null)
            args.AddRange(new[] { "--system", systemPrompt });

        args.Add(prompt);

        var result = await _processExecutor.ExecuteAsync(
            "gemini",
            args,
            timeout: TimeSpan.FromSeconds(options?.TimeoutSeconds ?? 300),
            ct);

        return new LlmResponse
        {
            Success = result.ExitCode == 0,
            Content = result.StandardOutput,
            ErrorMessage = result.StandardError
        };
    }

    // ... Implement other methods
}
```

**Installation Instructions:**

```bash
# Research needed - check Google AI documentation
# Possible installation methods:

# Via npm
npm install -g @google/gemini-cli

# Via gcloud SDK
gcloud components install gemini-cli

# Verify
gemini --version
```

#### 5.3 OpenAI CLI Adapter (New)

**Create:** `/PRFactory.Infrastructure/Agents/Adapters/OpenAiCliAdapter.cs`

```csharp
public class OpenAiCliAdapter : ILlmProvider
{
    public string ProviderName => "OpenAI (GPT CLI)";

    public List<string> SupportedModels => new()
    {
        "gpt-4o",
        "gpt-4-turbo",
        "gpt-4",
        "gpt-3.5-turbo"
    };

    // TODO: Research actual OpenAI CLI
    // Note: OpenAI may not have official CLI - might need custom wrapper around API

    public async Task<LlmResponse> SendMessageAsync(
        string prompt,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        // Placeholder implementation
        // May need to create custom CLI wrapper or use direct API calls
        throw new NotImplementedException("OpenAI CLI adapter needs research");
    }

    // ... Implement other methods
}
```

**Note:** OpenAI might not have an official CLI. Options:
1. Create custom CLI wrapper around OpenAI API
2. Use direct API calls (see Epic 5: Agent Framework)
3. Use third-party OpenAI CLI if available

---

### 6. Configuration System

**appsettings.json:**

```json
{
  "LlmProviders": {
    "DefaultProvider": "anthropic",
    "FallbackProvider": "google",
    "FailoverEnabled": true,

    "Anthropic": {
      "Enabled": true,
      "CliPath": "claude",
      "DefaultModel": "claude-sonnet-4-5-20250929",
      "TimeoutSeconds": 300,
      "MaxTokens": 8000
    },

    "Google": {
      "Enabled": true,
      "CliPath": "gemini",
      "DefaultModel": "gemini-pro",
      "TimeoutSeconds": 300,
      "MaxTokens": 8000
    },

    "OpenAI": {
      "Enabled": false,
      "CliPath": "openai",
      "DefaultModel": "gpt-4o",
      "TimeoutSeconds": 300,
      "MaxTokens": 4000
    }
  },

  "Prompts": {
    "BasePath": "/prompts"
  }
}
```

**Configuration Classes:**

```csharp
public class LlmProvidersOptions
{
    public string DefaultProvider { get; set; } = "anthropic";
    public string? FallbackProvider { get; set; }
    public bool FailoverEnabled { get; set; }
    public Dictionary<string, ProviderOptions> Providers { get; set; }
}

public class ProviderOptions
{
    public bool Enabled { get; set; }
    public string CliPath { get; set; }
    public string DefaultModel { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MaxTokens { get; set; }
}
```

---

## Acceptance Criteria

### Core Abstraction
- [ ] `ILlmProvider` interface created
- [ ] `ILlmProviderFactory` interface created
- [ ] `LlmProviderFactory` implementation created
- [ ] Registered in DI container

### CLI Parameters
- [ ] `BaseAgentOptions` class with `--provider`, `--model`, `--max-tokens`, `--temperature`
- [ ] All CLI commands inherit from `BaseAgentOptions`
- [ ] CLI commands use factory to get provider

### Prompt Templates
- [ ] `/prompts/{agent}/{provider}` directory structure created
- [ ] System and user prompts for each agent × provider combination
- [ ] `IPromptLoaderService` interface created
- [ ] `PromptLoaderService` implementation created
- [ ] Templating engine integrated (Scriban or string.Replace)

### Provider Implementations
- [ ] `ClaudeCodeCliAdapter` implements `ILlmProvider` interface
- [ ] `GeminiCliAdapter` created (research + implementation)
- [ ] `OpenAiCliAdapter` created or alternative solution documented
- [ ] Health checks implemented for all providers
- [ ] Usage metrics extracted from CLI output

### Configuration
- [ ] `appsettings.json` updated with provider configuration
- [ ] `LlmProvidersOptions` configuration class created
- [ ] Provider selection logic implemented
- [ ] Fallback logic tested

### Testing
- [ ] Unit tests for `LlmProviderFactory`
- [ ] Unit tests for each provider adapter
- [ ] Integration tests with actual CLIs (CI environment)
- [ ] Health check tests

---

## Migration Path

### Week 1: Core Abstractions
- Create `ILlmProvider` interface
- Create `LlmProviderFactory`
- Add `BaseAgentOptions` to CLI framework
- Create prompt loader service
- Set up `/prompts/` directory structure

### Week 2: Enhance Claude Code CLI
- Update `ClaudeCodeCliAdapter` to implement `ILlmProvider`
- Add health checks
- Add usage metrics extraction
- Create prompt templates for Claude

### Week 3: Add Additional Provider(s)
- Research Gemini CLI or OpenAI CLI
- Implement `GeminiCliAdapter` and/or `OpenAiCliAdapter`
- Create prompt templates for new provider(s)
- Test end-to-end with multiple providers

---

## Related Epics

- **Epic 1 (Team Review):** Review prompts should work with all providers
- **Epic 3 (Deep Planning):** Multi-agent orchestration should support any provider
- **Epic 5 (Agent Framework):** May add direct API support (bypassing CLIs)

---

**Next Steps:**
1. Review this epic with team
2. Research Gemini CLI and OpenAI CLI availability
3. Decide: Implement Gemini, OpenAI, or both first?
4. Create tickets for Week 1, 2, 3 tasks
5. Start with core abstractions (interfaces, factory)
