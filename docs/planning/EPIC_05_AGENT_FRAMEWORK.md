# Epic 5: Microsoft Agent Framework Integration

**Status:** 🔴 Not Started (Research Phase Required)
**Priority:** Future (After P1 & P2)
**Effort:** 2-3 weeks (after PoC decision)
**Dependencies:** Epic 2 (Multi-LLM), Epic 1 (Team Review for safety gates)

---

## Strategic Goal

Enable autonomous agentic workflows with tool use (file operations, git commands, Jira API calls). Move beyond simple CLI execution to true multi-turn agent conversations with memory and context.

**Current Approach:** CLI-based (simple, works today)

**Proposed Approach:** Microsoft Agent Framework (formerly Semantic Kernel) with direct API calls and tool plugins

**Decision Point:** After 1-week PoC, decide if framework adds sufficient value vs current CLI architecture.

---

## Why Microsoft Agent Framework?

**✅ Benefits:**
- **Multi-step Orchestration:** Agents can plan and execute complex workflows autonomously
- **Function Calling / Tool Use:** Agents can invoke tools (file I/O, git, Jira) directly
- **Memory & Context:** Multi-turn conversations with conversation history
- **Built-in LLM Abstraction:** Works with Anthropic, OpenAI, Google out-of-the-box
- **Prompt Templates & Chaining:** Reusable, composable prompt patterns
- **Production-Ready:** Microsoft-supported, used in Copilot products

**❌ Tradeoffs:**
- **Complexity:** Adds abstraction layer vs simple CLI subprocess calls
- **Token Costs:** Agentic workflows may use more tokens (multi-turn, tool calls)
- **Latency:** Multiple API roundtrips for tool execution
- **Reliability:** Autonomous agents may make unexpected decisions

---

## Architecture Comparison

### Current: CLI-Based

```
Workflow Graphs
  ↓
Agents (Analysis/Planning/Implementation)
  ↓
ILlmProvider (ClaudeCodeCliAdapter, etc.)
  ↓
ProcessExecutor
  ↓
claude --headless "prompt"
```

**Pros:** Simple, works today, authentication handled by CLI
**Cons:** Limited to single-turn prompts, no tool use, no memory

---

### Proposed: Agent Framework + Direct API

```
Workflow Graphs
  ↓
Microsoft Agent Framework (Kernel)
  ↓
┌────────────────────────────────────────────┐
│  Agent Orchestration                        │
│  - Planning                                 │
│  - Tool Selection                           │
│  - Multi-turn Conversations                │
└────────────────────────────────────────────┘
  ↓
┌────────────────────────────────────────────┐
│  Tool Plugins                               │
│  - FileOperations (Read, Edit, Write)      │
│  - GitOperations (Commit, Branch, PR)      │
│  - JiraOperations (Comment, Transition)    │
│  - AnalysisTools (CodeSearch, AST)         │
└────────────────────────────────────────────┘
  ↓
AnthropicApiClient (Direct Messages API)
  ↓
https://api.anthropic.com/v1/messages
```

**Pros:** True agentic workflows, tool use, multi-turn, memory
**Cons:** More complex, higher token costs, requires API key management

---

## PoC Phase (Week 1 - Research & Decision)

### Objectives

**Must Answer:**
1. Does Agent Framework provide enough value over CLI approach?
2. What is the token cost increase for agentic workflows vs CLI?
3. How reliable are autonomous tool calls?
4. Does framework support all required LLM providers (Anthropic, OpenAI, Google)?

### Tasks

1. **Install Microsoft Agent Framework**
   ```bash
   dotnet add package Microsoft.SemanticKernel
   dotnet add package Microsoft.SemanticKernel.Plugins.Core
   ```

2. **Create Simple PoC Agent**
   ```csharp
   var kernel = Kernel.CreateBuilder()
       .AddAnthropicChatCompletion(model: "claude-sonnet-4-5", apiKey: apiKey)
       .Build();

   // Add file tool plugin
   kernel.ImportPluginFromType<FileToolPlugin>();

   var result = await kernel.InvokePromptAsync(@"
       Analyze the file UserService.cs and suggest improvements.
       Use the ReadFile tool to access the file content.
   ");
   ```

3. **Test Tool Use**
   - Create simple plugins: `FileToolPlugin`, `GitToolPlugin`
   - Verify agent can invoke tools autonomously
   - Measure token usage vs CLI approach

4. **Performance Comparison**
   - CLI: Single prompt, single response
   - Agent Framework: Prompt → tool call → tool result → final response
   - Measure latency and token costs

5. **Decision Matrix**

   | Criteria | CLI Approach | Agent Framework |
   |----------|--------------|-----------------|
   | Simplicity | ✅ Very simple | ❌ More complex |
   | Token Costs | ✅ Lower | ❌ Higher (multi-turn) |
   | Autonomy | ❌ Limited | ✅ High (tool use) |
   | Latency | ✅ Lower | ❌ Higher (roundtrips) |
   | Flexibility | ❌ Single-turn only | ✅ Multi-turn, memory |
   | Maintainability | ✅ Simple code | ❌ More abstraction |

### Deliverables

- [ ] PoC code demonstrating agent + tool use
- [ ] Performance benchmark (latency, token usage)
- [ ] Decision document: Proceed or defer?

---

## Implementation Plan (If PoC Decision is "Proceed")

### Phase 1: AnthropicApiClient (Direct Messages API)

**Create:** `/PRFactory.Infrastructure/LLM/AnthropicApiClient.cs`

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

public class AnthropicApiClient : IAnthropicApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOAuthTokenStore _tokenStore;

    public async Task<CompletionResponse> SendMessageAsync(
        Guid userId,
        string prompt,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        // Load user's OAuth tokens
        var tokens = await _tokenStore.LoadTokensAsync(userId);

        // POST https://api.anthropic.com/v1/messages
        var request = new
        {
            model = "claude-sonnet-4-5-20250929",
            max_tokens = 8000,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.anthropic.com/v1/messages",
            request,
            ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(ct);

        return new CompletionResponse
        {
            Content = result.Content[0].Text,
            Usage = new UsageMetrics
            {
                InputTokens = result.Usage.InputTokens,
                OutputTokens = result.Usage.OutputTokens
            }
        };
    }
}
```

**Configuration:**

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

---

### Phase 2: Tool Plugins

**Create:** `/PRFactory.Infrastructure/Agents/Plugins/FileToolPlugin.cs`

```csharp
public class FileToolPlugin
{
    [KernelFunction("ReadFile")]
    [Description("Read the contents of a file")]
    public async Task<string> ReadFileAsync(
        [Description("File path to read")] string filePath)
    {
        if (!File.Exists(filePath))
            return $"Error: File not found: {filePath}";

        return await File.ReadAllTextAsync(filePath);
    }

    [KernelFunction("WriteFile")]
    [Description("Write content to a file")]
    public async Task<string> WriteFileAsync(
        [Description("File path to write")] string filePath,
        [Description("Content to write")] string content)
    {
        await File.WriteAllTextAsync(filePath, content);
        return $"Successfully wrote to {filePath}";
    }

    [KernelFunction("SearchFiles")]
    [Description("Search for files matching a pattern")]
    public string[] SearchFilesAsync(
        [Description("Directory to search")] string directory,
        [Description("Search pattern (e.g., *.cs)")] string pattern)
    {
        return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
    }
}
```

**Create:** `/PRFactory.Infrastructure/Agents/Plugins/GitToolPlugin.cs`

```csharp
public class GitToolPlugin
{
    private readonly ILocalGitService _gitService;

    [KernelFunction("GitCommit")]
    [Description("Commit changes to git repository")]
    public async Task<string> CommitAsync(
        [Description("Repository path")] string repositoryPath,
        [Description("Commit message")] string message)
    {
        await _gitService.CommitAsync(repositoryPath, message);
        return $"Committed: {message}";
    }

    [KernelFunction("GitCreateBranch")]
    [Description("Create a new git branch")]
    public async Task<string> CreateBranchAsync(
        [Description("Repository path")] string repositoryPath,
        [Description("Branch name")] string branchName)
    {
        await _gitService.CreateBranchAsync(repositoryPath, branchName);
        return $"Created branch: {branchName}";
    }
}
```

**Create:** `/PRFactory.Infrastructure/Agents/Plugins/JiraToolPlugin.cs`

```csharp
public class JiraToolPlugin
{
    private readonly IJiraService _jiraService;

    [KernelFunction("GetJiraTicket")]
    [Description("Get details of a Jira ticket")]
    public async Task<string> GetTicketAsync(
        [Description("Ticket key (e.g., PROJ-123)")] string ticketKey)
    {
        var ticket = await _jiraService.GetTicketAsync(ticketKey);
        return JsonSerializer.Serialize(ticket);
    }

    [KernelFunction("AddJiraComment")]
    [Description("Add a comment to a Jira ticket")]
    public async Task<string> AddCommentAsync(
        [Description("Ticket key")] string ticketKey,
        [Description("Comment text")] string comment)
    {
        await _jiraService.AddCommentAsync(ticketKey, comment);
        return $"Comment added to {ticketKey}";
    }
}
```

---

### Phase 3: Agent Orchestration

**Create:** `/PRFactory.Infrastructure/Agents/AnalysisAgent.cs`

```csharp
public class AnalysisAgent
{
    private readonly IKernel _kernel;

    public async Task<AnalysisResult> AnalyzeTicketAsync(Guid userId, Guid ticketId)
    {
        // Load OAuth tokens for user
        var tokens = await _tokenStore.LoadTokensAsync(userId);

        // Create kernel with Anthropic
        var kernel = Kernel.CreateBuilder()
            .AddAnthropicChatCompletion(
                model: "claude-sonnet-4-5-20250929",
                apiKey: tokens.AccessToken)
            .Build();

        // Import tool plugins
        kernel.ImportPluginFromType<FileToolPlugin>();
        kernel.ImportPluginFromType<JiraToolPlugin>();

        // Execute agent with autonomy
        var systemPrompt = @"
You are a software analyst. Your task is to analyze a Jira ticket and the related codebase.

You have access to these tools:
- ReadFile: Read file contents
- SearchFiles: Find files matching a pattern
- GetJiraTicket: Get ticket details

Use these tools to gather context and generate a comprehensive analysis.
";

        var result = await kernel.InvokePromptAsync($@"
{systemPrompt}

Analyze ticket ID: {ticketId}

Steps:
1. Use GetJiraTicket to fetch ticket details
2. Use SearchFiles to find related code files
3. Use ReadFile to read relevant files
4. Generate analysis report
");

        return ParseAnalysisResult(result.GetValue<string>());
    }
}
```

---

## Acceptance Criteria (If Implemented)

### PoC Phase
- [ ] PoC code demonstrates agent + tool use
- [ ] Token usage measured and acceptable (< 2x CLI approach)
- [ ] Latency measured and acceptable (< 5s total for tool workflow)
- [ ] Decision made: Proceed or defer

### Implementation (If "Proceed")
- [ ] `AnthropicApiClient` implements direct Messages API calls
- [ ] OAuth tokens loaded from User entity
- [ ] `FileToolPlugin`, `GitToolPlugin`, `JiraToolPlugin` created
- [ ] Agent can autonomously invoke tools
- [ ] Multi-turn conversations with memory
- [ ] Workflow graphs updated to use Agent Framework

### Safety & Reliability
- [ ] Human approval gates for critical operations (git push, Jira transitions)
- [ ] Tool execution limits (max file reads, max tool calls per conversation)
- [ ] Audit logging for all tool invocations
- [ ] Error handling and retry logic

---

## Risks & Mitigations

**Risk:** Token costs significantly higher
**Mitigation:** Set token budgets, monitor usage, use smaller models for simple tasks

**Risk:** Agents make unexpected tool calls
**Mitigation:** Extensive testing, human approval gates, audit logs

**Risk:** Added complexity for minimal gain
**Mitigation:** PoC phase validates value before full implementation

---

## Decision Criteria

**Proceed if:**
- Token cost increase < 2x CLI approach
- Latency acceptable for user workflows
- Tool use reliability > 95%
- Framework provides clear value (autonomous multi-step workflows)

**Defer if:**
- Token costs prohibitive
- Latency too high (> 10s for typical workflow)
- Tool use unreliable
- CLI approach sufficient for current needs

---

## Related Epics

- **Epic 2 (Multi-LLM):** Agent Framework should work with all LLM providers
- **Epic 1 (Team Review):** Human approval gates critical for agentic workflows

---

**Next Steps:**
1. Allocate 1 week for PoC phase
2. Build simple agent with tool use
3. Measure performance and costs
4. Make decision: Proceed or defer
5. If defer: Revisit in 6 months after P1/P2 epics complete
