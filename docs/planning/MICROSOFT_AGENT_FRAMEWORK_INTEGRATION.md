# Microsoft Agent Framework Integration Plan

**Status**: Future Work
**Priority**: Medium
**Estimated Effort**: 2-3 weeks

---

## Overview

Add agentic layer around AnthropicApiClient using Microsoft Agent Framework to enable autonomous agent workflows.

## Why Microsoft Agent Framework?

**Microsoft Agent Framework** (formerly Semantic Kernel) provides:
- ✅ Multi-step agent orchestration
- ✅ Function calling / tool use
- ✅ Memory and context management
- ✅ Built-in LLM provider abstraction
- ✅ Prompt templates and chaining
- ✅ State management
- ✅ Production-ready patterns

**Benefits over direct API calls**:
- Agents can use tools (file operations, git, Jira API)
- Multi-turn conversations with memory
- Autonomous task planning and execution
- Standardized patterns for agent workflows

---

## Current State

### What We Have
- ✅ OAuth infrastructure (per-user tokens)
- ✅ CLI integrations (Claude Code CLI, Gemini CLI, Codex CLI)
- ✅ Multi-graph workflow orchestration (RefinementGraph, PlanningGraph, ImplementationGraph)

### What's Missing
- ❌ AnthropicApiClient (direct Messages API calls)
- ❌ Agentic layer for autonomous tool use
- ❌ Integration with Microsoft Agent Framework

---

## Architecture Plan

### Option A: Direct API + Agent Framework

```
┌─────────────────────────────────────────────────────┐
│          Microsoft Agent Framework                   │
│                                                       │
│  ┌────────────────────────────────────────────┐    │
│  │  Agent Orchestration                        │    │
│  │  - Planning                                 │    │
│  │  - Tool Selection                           │    │
│  │  - Multi-turn Conversations                │    │
│  └────────────────────────────────────────────┘    │
│                       │                              │
│                       ▼                              │
│  ┌────────────────────────────────────────────┐    │
│  │  Tool Plugins                               │    │
│  │  - FileOperations (Read, Edit, Write)      │    │
│  │  - GitOperations (Commit, Branch, PR)      │    │
│  │  - JiraOperations (Comment, Transition)    │    │
│  │  - AnalysisTools (CodeSearch, AST)         │    │
│  └────────────────────────────────────────────┘    │
│                       │                              │
└───────────────────────┼──────────────────────────────┘
                        │
                        ▼
              ┌──────────────────┐
              │ AnthropicApiClient │
              │  (OAuth Tokens)    │
              └──────────────────┘
                        │
                        ▼
         https://api.anthropic.com/v1/messages
```

### Option B: CLI + Agent Framework

```
┌─────────────────────────────────────────────────────┐
│          Microsoft Agent Framework                   │
│  (Higher-level orchestration)                        │
└───────────────────────┬─────────────────────────────┘
                        │
                        ▼
              ┌──────────────────────┐
              │ ClaudeCodeCliAdapter  │
              │  (OAuth via auth.json)│
              └──────────────────────┘
                        │
                        ▼
                  claude --print
            (Has built-in tool support)
```

**Recommendation**: Start with **Option B** (CLI) since Claude Code CLI already has tool support. Add Option A (API + Framework) later for more control.

---

## Implementation Plan

### Phase 1: Research & Proof of Concept (1 week)

**Tasks**:
1. Install Microsoft Agent Framework NuGet packages
   - `Microsoft.SemanticKernel`
   - `Microsoft.SemanticKernel.Connectors.Anthropic` (if available)
2. Create PoC: Simple agent with file tool
3. Test with Claude Code CLI integration
4. Evaluate vs direct CLI approach

**Deliverables**:
- PoC code demonstrating agent + tool use
- Performance comparison (agent framework vs direct CLI)
- Recommendation: Use framework or stick with direct CLI?

### Phase 2: AnthropicApiClient Implementation (3-5 days)

**Tasks**:
1. Create `AnthropicApiClient` interface
   ```csharp
   public interface IAnthropicApiClient
   {
       Task<CompletionResponse> SendMessageAsync(
           Guid userId,
           string prompt,
           string? systemPrompt = null,
           CancellationToken ct = default);

       Task<StreamingCompletionResponse> SendMessageStreamAsync(
           Guid userId,
           string prompt,
           string? systemPrompt = null,
           CancellationToken ct = default);
   }
   ```

2. Implement Messages API calls
   - POST https://api.anthropic.com/v1/messages
   - Use OAuth tokens from User entity
   - Handle token refresh
   - Streaming support

3. Add to configuration system
   ```json
   {
     "LlmProviders": {
       "AnthropicApi": {
         "BaseUrl": "https://api.anthropic.com/v1",
         "Model": "claude-sonnet-4-5-20250929",
         "MaxTokens": 8000
       }
     }
   }
   ```

### Phase 3: Agent Framework Integration (1-2 weeks)

**Tasks**:
1. Create `SemanticKernelAgent` wrapper
2. Implement tool plugins:
   - `FileToolPlugin` (Read, Write, Edit, Search)
   - `GitToolPlugin` (Commit, Branch, Push, CreatePR)
   - `JiraToolPlugin` (GetTicket, Comment, Transition)
   - `AnalysisToolPlugin` (SearchCode, GetDefinition, FindReferences)

3. Update workflow graphs to use agents
   - RefinementGraph → Use agent for analysis
   - PlanningGraph → Use agent for planning
   - ImplementationGraph → Use agent for code generation

4. Add agent memory/context management
   - Conversation history
   - Codebase context
   - Ticket context

**Example**:
```csharp
public class AnalysisAgent
{
    private readonly Kernel _kernel;
    private readonly IAnthropicApiClient _apiClient;

    public async Task<AnalysisResult> AnalyzeTicketAsync(Guid userId, Guid ticketId)
    {
        // Load OAuth tokens for user
        var tokens = await _tokenStore.LoadTokensAsync(userId);

        // Create kernel with tools
        var kernel = Kernel.CreateBuilder()
            .AddAnthropicChatCompletion(
                model: "claude-sonnet-4-5-20250929",
                apiKey: tokens.AccessToken)
            .Build();

        // Add tool plugins
        kernel.ImportPluginFromType<FileToolPlugin>();
        kernel.ImportPluginFromType<JiraToolPlugin>();

        // Execute agent
        var result = await kernel.InvokePromptAsync(
            "Analyze ticket {{$ticketId}} and suggest implementation approach",
            new KernelArguments { ["ticketId"] = ticketId });

        return ParseAnalysisResult(result);
    }
}
```

### Phase 4: Testing & Validation (3-5 days)

**Tasks**:
1. End-to-end workflow testing
2. Performance comparison (API vs CLI)
3. Token usage optimization
4. Error handling and retry logic
5. Security audit (token handling, tool permissions)

---

## Technical Details

### Microsoft Agent Framework Resources

**Documentation**:
- https://learn.microsoft.com/en-us/semantic-kernel/overview/
- https://github.com/microsoft/semantic-kernel

**Key Concepts**:
- **Kernel**: Main orchestration engine
- **Plugins**: Tools/functions agents can use
- **Prompts**: Template-based prompt management
- **Memory**: Context and conversation history
- **Planners**: Autonomous task planning (experimental)

**NuGet Packages**:
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
<PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" Version="1.0.0" />
```

### Alternative: Anthropic SDK

Anthropic may release official .NET SDK with agent/tool support:
- Monitor: https://github.com/anthropics
- If released, evaluate vs Microsoft Agent Framework

---

## Risks & Considerations

### Risks
1. **Token Costs**: Agentic workflows may use more tokens (multi-turn conversations, tool calls)
2. **Latency**: Multiple API roundtrips for tool use
3. **Complexity**: Agent framework adds abstraction layer
4. **Reliability**: Autonomous agents may make unexpected tool calls

### Mitigations
1. **Cost Controls**: Set token budgets per workflow, monitor usage
2. **Latency**: Use streaming responses, parallel tool calls where possible
3. **Complexity**: Keep agents focused (one task per agent type)
4. **Reliability**: Extensive testing, human approval gates for critical operations

---

## Success Criteria

**Must Have**:
- ✅ Agents can autonomously use file/git/Jira tools
- ✅ Multi-turn conversations with context retention
- ✅ Token usage within acceptable limits (< 2x current usage)
- ✅ End-to-end workflows complete successfully

**Nice to Have**:
- ✅ Streaming responses for better UX
- ✅ Parallel tool execution for performance
- ✅ Agent learning from feedback (approval/rejection)

---

## Next Steps

1. **Research Phase**: Allocate 1 week for PoC
2. **Decision Point**: After PoC, decide if framework adds value
3. **If YES**: Proceed with Phase 2 (AnthropicApiClient)
4. **If NO**: Stick with CLI approach, revisit later

---

## References

- Microsoft Agent Framework: https://learn.microsoft.com/en-us/semantic-kernel/
- Anthropic Messages API: https://docs.anthropic.com/en/api/messages
- Claude Code CLI: https://code.claude.com/docs
- PRFactory Architecture: `/docs/ARCHITECTURE.md`

**Last Updated**: 2025-11-09
