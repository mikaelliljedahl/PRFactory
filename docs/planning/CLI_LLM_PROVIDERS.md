# CLI LLM Provider Integration

**Status**: Partially Implemented (Claude Code CLI only)
**Priority**: High
**Estimated Effort**: 2-3 weeks for full multi-provider support

---

## Overview

PRFactory supports multiple CLI-based LLM providers to give users flexibility in choosing their preferred AI coding assistant. Each CLI handles its own OAuth authentication and token storage independently.

**Supported CLI Providers:**
1. ✅ **Claude Code CLI** (Anthropic) - Currently implemented
2. ⏳ **Gemini CLI** (Google) - Planned
3. ⏳ **Codex CLI** (OpenAI) - Planned

---

## Why CLI-Based Integration?

**Benefits over direct API integration:**
- ✅ **Authentication Handled**: CLIs manage their own OAuth flows and token refresh
- ✅ **Feature Parity**: CLIs expose the same capabilities as web interfaces
- ✅ **Headless Support**: All CLIs support non-interactive (headless) execution
- ✅ **No Token Management**: No need to build OAuth infrastructure for each provider
- ✅ **Consistent Interface**: All CLIs follow similar command patterns
- ✅ **Updates**: CLI updates automatically provide new LLM features

**Tradeoffs:**
- ❌ **External Dependency**: Requires CLI installation on host system
- ❌ **Process Overhead**: Subprocess execution adds latency vs direct API calls
- ❌ **Less Control**: Can't customize every aspect of API interaction

---

## Current Architecture

### ICliAgent Interface

All CLI providers implement the same interface:

```csharp
public interface ICliAgent
{
    Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    Task<CliAgentResponse> ExecuteWithProjectContextAsync(
        string prompt,
        string projectPath,
        CancellationToken cancellationToken = default);

    Task<CliAgentResponse> ExecuteStreamingAsync(
        string prompt,
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default);

    CliAgentCapabilities GetCapabilities();
}
```

### Execution Flow

```
Workflow Graphs
  ↓
Agents (Analysis/Planning/Implementation)
  ↓
ICliAgent Interface
  ↓
Provider-Specific Adapter (ClaudeDesktopCliAdapter, GeminiCliAdapter, CodexCliAdapter)
  ↓
ProcessExecutor (secure subprocess execution)
  ↓
CLI Subprocess (claude --headless, gemini, codex)
```

### Security: ProcessExecutor

All CLI invocations use `ProcessExecutor` for secure subprocess execution:

**Security Features:**
- ✅ No shell execution (prevents command injection)
- ✅ Proper argument escaping via `ProcessStartInfo.ArgumentList`
- ✅ Timeout enforcement (default: 5 minutes, configurable)
- ✅ Streaming support for long-running operations
- ✅ Graceful cancellation via `CancellationToken`

**File**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Execution/ProcessExecutor.cs`

---

## 1. Claude Code CLI (Anthropic)

### Status: ✅ Implemented

**Implementation**: `ClaudeDesktopCliAdapter`
**File**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Adapters/ClaudeDesktopCliAdapter.cs`

### Installation

```bash
# Install Claude Code CLI (https://code.claude.com)
npm install -g @anthropic-ai/claude-code

# Or via homebrew (macOS)
brew install claude-code

# Verify installation
claude --version
```

### Authentication

**OAuth Flow**: Handled automatically by CLI on first use

**Token Storage**: `~/.config/claude-code/auth.json`

**Scopes**:
- `org:create_api_key`
- `user:profile`
- `user:inference`

**Manual Authentication** (if needed):
```bash
claude auth login
# Opens browser for OAuth flow
# Tokens stored automatically in ~/.config/claude-code/auth.json
```

**Check Authentication Status**:
```bash
claude auth status
```

### CLI Usage

**Headless Mode** (non-interactive):
```bash
claude --headless "Analyze this codebase and suggest improvements"
```

**With Project Context**:
```bash
cd /path/to/project
claude --headless --project-path . "Generate unit tests for UserService.cs"
```

**Streaming Output**:
```bash
claude --headless --stream "Write a REST API controller for tickets"
```

### Configuration

**appsettings.json**:
```json
{
  "LlmProviders": {
    "ClaudeCodeCli": {
      "Enabled": true,
      "CliPath": "claude",
      "TimeoutSeconds": 300,
      "MaxTokens": 8000,
      "Model": "claude-sonnet-4-5-20250929",
      "Arguments": "--headless --model {model} --max-tokens {maxTokens}"
    }
  }
}
```

### Current Implementation

```csharp
public class ClaudeDesktopCliAdapter : ICliAgent
{
    private readonly ProcessExecutor _processExecutor;
    private readonly ClaudeDesktopCliOptions _options;

    public async Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken ct = default)
    {
        var arguments = new List<string>
        {
            "--headless",
            "--model", _options.Model,
            prompt
        };

        var result = await _processExecutor.ExecuteAsync(
            _options.CliPath,
            arguments,
            workingDirectory: null,
            timeout: TimeSpan.FromSeconds(_options.TimeoutSeconds),
            ct);

        return new CliAgentResponse
        {
            Success = result.ExitCode == 0,
            Content = result.StandardOutput,
            ErrorMessage = result.StandardError
        };
    }
}
```

### Documentation

- Official Docs: https://code.claude.com/docs
- Headless Mode: https://code.claude.com/docs/en/headless
- Authentication: https://code.claude.com/docs/en/authentication

---

## 2. Gemini CLI (Google)

### Status: ⏳ Planned

**Implementation**: `GeminiCliAdapter` (to be created)
**File**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Adapters/GeminiCliAdapter.cs` (does not exist yet)

### Installation

```bash
# Install Gemini CLI (hypothetical - verify actual installation method)
npm install -g @google/gemini-cli

# Or via gcloud SDK
gcloud components install gemini-cli

# Verify installation
gemini --version
```

### Authentication

**OAuth Flow**: Managed by Google Cloud SDK or standalone OAuth

**Token Storage**: Likely `~/.config/gcloud/` or `~/.gemini/credentials.json`

**Required Scopes**:
- `https://www.googleapis.com/auth/cloud-platform` (if using GCP)
- `https://www.googleapis.com/auth/generative-language` (Gemini API)

**Authentication Commands**:
```bash
# GCP-based auth
gcloud auth login
gcloud auth application-default login

# Or standalone Gemini CLI auth
gemini auth login
```

### CLI Usage (Expected)

```bash
# Headless mode
gemini --headless "Explain this codebase"

# With project context
gemini --project /path/to/project --headless "Add logging to UserService"

# Streaming
gemini --stream --headless "Write integration tests"
```

### Configuration (Proposed)

**appsettings.json**:
```json
{
  "LlmProviders": {
    "GeminiCli": {
      "Enabled": true,
      "CliPath": "gemini",
      "TimeoutSeconds": 300,
      "MaxTokens": 8000,
      "Model": "gemini-pro",
      "Arguments": "--headless --model {model}"
    }
  }
}
```

### Implementation Plan

**Tasks**:
1. Research actual Gemini CLI installation and authentication
2. Create `GeminiCliAdapter` implementing `ICliAgent`
3. Create `GeminiCliOptions` configuration class
4. Add configuration section to appsettings.json
5. Register adapter in DI container
6. Add integration tests
7. Update documentation

**Estimated Effort**: 3-5 days

### Documentation (Research Needed)

- Google AI Studio: https://ai.google.dev/
- Gemini API Docs: https://ai.google.dev/docs
- (Need to find official CLI docs)

---

## 3. Codex CLI (OpenAI)

### Status: ⏳ Planned (Placeholder exists)

**Implementation**: `CodexCliAdapter` (exists but throws `NotImplementedException`)
**File**: `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Adapters/CodexCliAdapter.cs`

### Installation

```bash
# Install OpenAI CLI (hypothetical - verify actual method)
npm install -g @openai/cli

# Or via pip
pip install openai-cli

# Verify installation
openai --version
```

### Authentication

**API Key Based**: OpenAI typically uses API keys, not OAuth

**Token Storage**: Likely `~/.openai/config` or environment variable `OPENAI_API_KEY`

**Authentication**:
```bash
# Set API key
openai api-key set <your-api-key>

# Or via environment variable
export OPENAI_API_KEY=sk-...
```

### CLI Usage (Expected)

```bash
# Generate code
openai codex "Write a Python function to parse JSON"

# With context
openai codex --context /path/to/project "Add error handling to UserController"

# Streaming
openai codex --stream "Implement user authentication"
```

### Configuration (Proposed)

**appsettings.json**:
```json
{
  "LlmProviders": {
    "CodexCli": {
      "Enabled": true,
      "CliPath": "openai",
      "TimeoutSeconds": 300,
      "MaxTokens": 4000,
      "Model": "code-davinci-002",
      "Arguments": "codex --model {model} --max-tokens {maxTokens}"
    }
  }
}
```

### Implementation Plan

**Tasks**:
1. Research OpenAI CLI for Codex (may not exist as standalone CLI)
2. Alternative: Consider using official OpenAI Python SDK via subprocess
3. Create `CodexCliAdapter` implementing `ICliAgent`
4. Create `CodexCliOptions` configuration class
5. Handle API key authentication (different from OAuth)
6. Add configuration section to appsettings.json
7. Register adapter in DI container
8. Add integration tests
9. Update documentation

**Estimated Effort**: 3-5 days

**Note**: OpenAI may have deprecated Codex in favor of GPT-4. Research needed to determine best approach.

### Documentation (Research Needed)

- OpenAI Platform: https://platform.openai.com/
- Codex Deprecation: https://help.openai.com/en/articles/6819671
- GPT-4 for Code: Alternative to Codex

---

## Provider Selection Strategy

### Configuration-Based Selection

Users can enable/disable providers via configuration:

```json
{
  "LlmProviders": {
    "DefaultProvider": "ClaudeCodeCli",
    "FallbackProvider": "GeminiCli",

    "ClaudeCodeCli": { "Enabled": true },
    "GeminiCli": { "Enabled": true },
    "CodexCli": { "Enabled": false }
  }
}
```

### Provider Factory Pattern

```csharp
public interface ICliAgentFactory
{
    ICliAgent CreateAgent(string providerName);
    ICliAgent GetDefaultAgent();
    List<string> GetAvailableProviders();
}

public class CliAgentFactory : ICliAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LlmProvidersOptions _options;

    public ICliAgent CreateAgent(string providerName)
    {
        return providerName switch
        {
            "ClaudeCodeCli" => _serviceProvider.GetRequiredService<ClaudeDesktopCliAdapter>(),
            "GeminiCli" => _serviceProvider.GetRequiredService<GeminiCliAdapter>(),
            "CodexCli" => _serviceProvider.GetRequiredService<CodexCliAdapter>(),
            _ => throw new NotSupportedException($"Provider '{providerName}' not supported")
        };
    }

    public ICliAgent GetDefaultAgent()
    {
        return CreateAgent(_options.DefaultProvider);
    }
}
```

### Tenant/Repository-Level Provider Selection

**Future Enhancement**: Allow per-tenant or per-repository provider selection:

```csharp
public class TenantConfiguration
{
    public string PreferredLlmProvider { get; set; } = "ClaudeCodeCli";
}

public class Repository
{
    public string? PreferredLlmProvider { get; set; } // Override tenant default
}
```

---

## Implementation Roadmap

### Phase 1: Complete Claude Code CLI Integration (1 week)

**Current Status**: Basic implementation exists

**Tasks**:
1. Add streaming support to ClaudeDesktopCliAdapter
2. Implement project context handling
3. Add retry logic for transient failures
4. Create comprehensive integration tests
5. Document configuration options
6. Add health checks (verify CLI is installed and authenticated)

### Phase 2: Add Gemini CLI Support (1 week)

**Tasks**:
1. Research Gemini CLI installation and authentication
2. Create `GeminiCliAdapter` implementing `ICliAgent`
3. Implement all interface methods
4. Add configuration support
5. Register in DI container
6. Create integration tests
7. Update documentation

### Phase 3: Add Codex CLI Support or GPT-4 Alternative (1 week)

**Tasks**:
1. Research OpenAI CLI options (may need custom solution)
2. Create `CodexCliAdapter` or `GPT4CliAdapter`
3. Implement API key authentication
4. Implement all interface methods
5. Add configuration support
6. Register in DI container
7. Create integration tests
8. Update documentation

### Phase 4: Provider Selection & Factory Pattern (2-3 days)

**Tasks**:
1. Implement `ICliAgentFactory` interface
2. Create `CliAgentFactory` implementation
3. Add provider selection configuration
4. Implement fallback logic
5. Add provider health checks
6. Update all agents to use factory
7. Test multi-provider scenarios

---

## Testing Strategy

### Unit Tests

**Test each adapter independently:**
- Successful prompt execution
- Error handling (CLI not found, authentication failure)
- Timeout enforcement
- Streaming support
- Project context handling

**Mock ProcessExecutor** to avoid actual CLI subprocess calls in unit tests.

### Integration Tests

**Test with actual CLIs installed:**
- End-to-end prompt execution
- Authentication verification
- Large prompt handling
- Long-running operations
- Concurrent requests

**Requires**:
- CLIs installed in CI environment
- Test accounts authenticated
- Test data / prompts

### Health Checks

**Verify CLI availability:**
```csharp
public class ClaudeCliHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        // Check if CLI is installed
        var result = await _processExecutor.ExecuteAsync("claude", new[] { "--version" }, ct);

        if (result.ExitCode != 0)
            return HealthCheckResult.Unhealthy("Claude CLI not found");

        // Check if authenticated
        var authResult = await _processExecutor.ExecuteAsync("claude", new[] { "auth", "status" }, ct);

        if (authResult.ExitCode != 0)
            return HealthCheckResult.Degraded("Claude CLI not authenticated");

        return HealthCheckResult.Healthy("Claude CLI ready");
    }
}
```

---

## Configuration Schema

### Full appsettings.json Example

```json
{
  "LlmProviders": {
    "DefaultProvider": "ClaudeCodeCli",
    "FallbackProvider": "GeminiCli",
    "FailoverEnabled": true,
    "HealthCheckIntervalSeconds": 60,

    "ClaudeCodeCli": {
      "Enabled": true,
      "CliPath": "claude",
      "TimeoutSeconds": 300,
      "MaxTokens": 8000,
      "Model": "claude-sonnet-4-5-20250929",
      "Arguments": "--headless --model {model} --max-tokens {maxTokens}",
      "RetryPolicy": {
        "MaxRetries": 3,
        "InitialDelaySeconds": 2,
        "MaxDelaySeconds": 30
      }
    },

    "GeminiCli": {
      "Enabled": true,
      "CliPath": "gemini",
      "TimeoutSeconds": 300,
      "MaxTokens": 8000,
      "Model": "gemini-pro",
      "Arguments": "--headless --model {model}",
      "RetryPolicy": {
        "MaxRetries": 3,
        "InitialDelaySeconds": 2,
        "MaxDelaySeconds": 30
      }
    },

    "CodexCli": {
      "Enabled": false,
      "CliPath": "openai",
      "TimeoutSeconds": 300,
      "MaxTokens": 4000,
      "Model": "code-davinci-002",
      "Arguments": "codex --model {model} --max-tokens {maxTokens}",
      "RetryPolicy": {
        "MaxRetries": 3,
        "InitialDelaySeconds": 2,
        "MaxDelaySeconds": 30
      }
    }
  }
}
```

---

## Security Considerations

### 1. Credential Storage

**Each CLI manages its own credentials:**
- ✅ Claude Code CLI: `~/.config/claude-code/auth.json` (encrypted by CLI)
- ✅ Gemini CLI: `~/.config/gcloud/` or similar (managed by gcloud SDK)
- ✅ Codex CLI: API keys in `~/.openai/config` or environment variables

**PRFactory does NOT store CLI credentials** - authentication is handled by each CLI independently.

### 2. Process Execution Security

**ProcessExecutor Security Features:**
- ✅ No shell execution (prevents command injection)
- ✅ Proper argument escaping
- ✅ Timeout enforcement
- ✅ Working directory isolation
- ✅ No environment variable leakage

### 3. Input Sanitization

**Prompts are sanitized before CLI execution:**
```csharp
private string SanitizePrompt(string prompt)
{
    // Remove dangerous characters that could break CLI parsing
    return prompt
        .Replace("\"", "\\\"")   // Escape quotes
        .Replace("\n", " ")      // Remove newlines
        .Replace("\r", "");      // Remove carriage returns
}
```

### 4. Output Validation

**CLI responses are validated:**
- Check exit codes (0 = success)
- Parse stderr for authentication errors
- Detect rate limiting
- Handle malformed responses gracefully

---

## Troubleshooting

### Claude Code CLI Issues

**Problem**: `claude: command not found`
```bash
# Solution: Install Claude Code CLI
npm install -g @anthropic-ai/claude-code
# Or via homebrew
brew install claude-code
```

**Problem**: `Authentication required`
```bash
# Solution: Run authentication flow
claude auth login
# Verify authentication
claude auth status
```

**Problem**: `Timeout after 5 minutes`
```json
// Solution: Increase timeout in appsettings.json
{
  "LlmProviders": {
    "ClaudeCodeCli": {
      "TimeoutSeconds": 600  // 10 minutes
    }
  }
}
```

### Gemini CLI Issues

**Problem**: `gcloud not authenticated`
```bash
# Solution: Authenticate with Google Cloud
gcloud auth login
gcloud auth application-default login
```

**Problem**: `Quota exceeded`
```bash
# Solution: Check quota limits in GCP Console
# Enable billing if needed
```

### Codex CLI Issues

**Problem**: `Invalid API key`
```bash
# Solution: Set valid OpenAI API key
openai api-key set sk-...
# Or via environment variable
export OPENAI_API_KEY=sk-...
```

**Problem**: `Codex model deprecated`
```bash
# Solution: Use GPT-4 instead
# Update configuration to use GPT-4 models
```

---

## Relationship to Microsoft Agent Framework

**Important**: The CLI-based approach is **separate and independent** from the Microsoft Agent Framework integration planned in `/docs/planning/MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md`.

**Two Parallel Tracks**:

1. **CLI Track (Current)**:
   - Uses existing CLIs (Claude Code CLI, Gemini CLI, Codex CLI)
   - CLIs handle OAuth and token management
   - Simple subprocess execution via `ProcessExecutor`
   - No additional authentication infrastructure needed
   - **Status**: Claude Code CLI implemented, others planned

2. **Agent Framework Track (Future)**:
   - Direct API calls to Anthropic Messages API
   - Per-user OAuth tokens stored in User entity
   - Agentic layer with tool use (file operations, git, Jira)
   - Microsoft Agent Framework for orchestration
   - **Status**: OAuth infrastructure ported, implementation not started

**Why Both?**:
- CLIs provide immediate functionality with minimal setup
- Agent Framework provides more control and advanced features
- Users can choose based on their needs
- CLIs work for simple workflows, Agent Framework for complex autonomous tasks

---

## Next Steps

### Immediate (1-2 weeks)
1. ✅ Document CLI providers (this document)
2. ⏳ Complete Claude Code CLI integration (streaming, context, tests)
3. ⏳ Research and implement Gemini CLI adapter
4. ⏳ Research OpenAI CLI options (Codex or GPT-4)

### Short-term (1 month)
1. Implement provider factory pattern
2. Add configuration-based provider selection
3. Implement health checks for all providers
4. Add comprehensive integration tests
5. Update documentation with real-world examples

### Long-term (2-3 months)
1. Add per-tenant provider selection
2. Implement failover logic (if primary CLI fails, use fallback)
3. Add usage metrics and cost tracking per provider
4. Implement prompt caching for repeated operations
5. Explore hybrid approach (CLI + Agent Framework)

---

## References

- **Claude Code CLI**: https://code.claude.com/docs
- **Google Gemini API**: https://ai.google.dev/docs
- **OpenAI Platform**: https://platform.openai.com/
- **PRFactory CLI Architecture**: Explored via codebase analysis (Nov 2024)
- **Microsoft Agent Framework Plan**: `/docs/planning/MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md`

**Last Updated**: 2025-11-09
