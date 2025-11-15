# AG-UI Integration in PRFactory

## Overview

PRFactory implements the **Microsoft AG-UI (Agent-User Interface) protocol** for real-time agent-to-UI communication. This document explains our implementation approach and why we use a custom SSE implementation instead of the official `MapAGUI()` extension method.

## What is AG-UI?

AG-UI is an open, lightweight, event-based protocol designed to standardize how AI agents connect to user-facing applications. It provides:

- **Remote Agent Hosting**: Deploy AI agents as web services accessible by multiple clients
- **Real-time Streaming**: Stream agent responses using Server-Sent Events (SSE) for immediate feedback
- **Tool Rendering**: Display agent tool invocations in the UI (e.g., "Searching codebase...")
- **Multi-turn Conversations**: Maintain conversation history and context across interactions

**Official Documentation**: https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/

## Package Reference

```xml
<PackageReference Include="Microsoft.Agents.AI.Hosting.AGUI.AspNetCore" Version="1.0.0-preview.251113.1" />
```

**Status**: Preview/Prerelease (as of November 2025)

## Official MapAGUI() Pattern

The official AG-UI integration uses the `MapAGUI()` extension method:

```csharp
// Official pattern (simple scenarios)
builder.Services.AddAGUI();
var app = builder.Build();

app.MapAGUI("/", agent); // Maps endpoint to single agent instance
await app.RunAsync();
```

**Limitations of MapAGUI():**
1. **Single Agent**: Maps one endpoint to one agent instance
2. **No Multi-Tenancy**: Doesn't support tenant-specific agent configuration
3. **No Custom Services**: Limited dependency injection support
4. **Minimal State Management**: No built-in chat history or follow-up questions

## PRFactory's Custom AG-UI Implementation

### Why Custom Implementation?

PRFactory has architectural requirements that the official `MapAGUI()` doesn't support:

1. **Multi-Agent Routing**: Dynamically select agents based on:
   - Tenant configuration (different customers use different agent types)
   - Workflow state (AnalyzerAgent, PlannerAgent, ImplementerAgent, ReviewerAgent)
   - Ticket context (some tickets require specialized agents)

2. **Chat History Persistence**:
   - Store conversation history in database (not just in-memory)
   - Support conversation resume after server restart
   - Enable audit trail and analytics

3. **Tenant Isolation**:
   - Each tenant has isolated agent configuration
   - Tenant-specific token limits and model selection
   - Per-tenant feature flags (some tenants get experimental agents)

4. **Advanced Features**:
   - Follow-up question handling with entity tracking
   - Real-time workflow state updates via SignalR
   - Agent status broadcasting to multiple UI clients
   - Integration with ticket lifecycle and approval workflows

5. **Service Integration**:
   - Inject `IAgentFactory` for dynamic agent creation
   - Inject `ITenantContext` for tenant resolution
   - Inject `ITicketService`, `IWorkflowOrchestrator`, etc.
   - Complex dependency graph not supported by MapAGUI()

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Blazor Component (AgentChat.razor)         │
│       Connects to /api/agent/chat/stream via SSE        │
└───────────────────────────┬─────────────────────────────┘
                            │ SSE (Server-Sent Events)
                            ▼
┌─────────────────────────────────────────────────────────┐
│          AgentChatController (API Controller)           │
│     GET /api/agent/chat/stream?ticketId=X&message=Y     │
│                                                          │
│  - Validates request parameters                         │
│  - Sets SSE headers (text/event-stream)                 │
│  - Delegates to IAgentChatService                       │
│  - Streams chunks in AG-UI format                       │
└───────────────────────────┬─────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│      IAgentChatService (Application Service)            │
│        AgentChatService implementation                  │
│                                                          │
│  - Loads conversation history from database             │
│  - Resolves tenant context                              │
│  - Creates agent via IAgentFactory                      │
│  - Executes agent with conversation history             │
│  - Yields AG-UI protocol chunks                         │
│  - Persists messages to database                        │
└───────────────────────────┬─────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│       IAIAgentService (Agent Framework Wrapper)         │
│                                                          │
│  - Creates agent from configuration                     │
│  - Registers tools (ITool implementations)              │
│  - Executes agent with streaming                        │
│  - Converts agent events to AG-UI chunks                │
└─────────────────────────────────────────────────────────┘
```

### Protocol Specification

PRFactory's SSE implementation follows the AG-UI protocol specification:

**Endpoint:**
```
GET /api/agent/chat/stream?ticketId={guid}&message={string}
```

**Response Headers:**
```
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
X-Accel-Buffering: no
```

**Chunk Format:**
```
data: {"type":"Reasoning","content":"Analyzing ticket...","chunkId":"1","isFinal":false}

data: {"type":"ToolUse","content":"Searching codebase for references...","chunkId":"2","isFinal":false}

data: {"type":"Response","content":"I found 3 relevant files...","chunkId":"3","isFinal":false}

data: {"type":"Complete","content":"","chunkId":"4","isFinal":true}

```

**Chunk Types:**

| Type | Description | Example |
|------|-------------|---------|
| `Reasoning` | Agent's thinking process | "Analyzing ticket requirements to understand scope..." |
| `ToolUse` | Agent invoking a tool | "Searching codebase with pattern '*.cs'..." |
| `Response` | Agent's text response to user | "I found 3 files that need modification..." |
| `Complete` | Final chunk indicating completion | Empty content, `isFinal: true` |
| `Error` | Error during execution | "Stream error occurred: {message}" |

**Chunk Schema:**
```csharp
public class AgentStreamChunk
{
    public ChunkType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ChunkId { get; set; } = Guid.NewGuid().ToString();
    public bool IsFinal { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### Implementation Files

**Core Files:**
- `src/PRFactory.Web/Controllers/AgentChatController.cs` - SSE endpoint implementation
- `src/PRFactory.Infrastructure/Application/AgentChatService.cs` - Business logic
- `src/PRFactory.Core/Application/Services/IAgentChatService.cs` - Service interface
- `src/PRFactory.Core/Application/AgentUI/AgentStreamChunk.cs` - AG-UI protocol models
- `src/PRFactory.Web/Configuration/AGUIConfiguration.cs` - AG-UI config and documentation

**Blazor Client:**
- `src/PRFactory.Web/Components/Agents/AgentChat.razor` - UI component
- `src/PRFactory.Web/Components/Agents/AgentChat.razor.cs` - SSE client logic

### Code Examples

**Server-Side (AgentChatService):**
```csharp
public async IAsyncEnumerable<AgentStreamChunk> StreamResponseAsync(
    Guid ticketId,
    string userMessage,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // Load conversation history
    var history = await GetChatHistoryAsync(ticketId, cancellationToken);

    // Resolve tenant context
    var ticket = await _ticketRepo.GetByIdAsync(ticketId);
    var tenant = await _tenantRepo.GetByIdAsync(ticket.TenantId);

    // Create agent dynamically
    var agent = await _agentFactory.CreateAgentAsync(
        tenant.Id,
        DetermineAgentType(ticket.State),
        cancellationToken);

    // Execute agent with streaming
    await foreach (var chunk in _aiAgentService.ExecuteAgentAsync(
        agent,
        userMessage,
        history,
        cancellationToken))
    {
        // Persist to database
        await PersistChunkAsync(ticketId, chunk);

        // Yield AG-UI protocol chunk
        yield return chunk;
    }
}
```

**Client-Side (AgentChat.razor.cs):**
```csharp
private async Task StreamAgentResponseAsync(string message)
{
    var url = $"/api/agent/chat/stream?ticketId={TicketId}&message={Uri.EscapeDataString(message)}";

    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

        var json = line.Substring(6); // Remove "data: " prefix
        var chunk = JsonSerializer.Deserialize<AgentStreamChunk>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Handle chunk based on type
        switch (chunk.Type)
        {
            case ChunkType.Reasoning:
                currentAgentAction = chunk.Content;
                await InvokeAsync(StateHasChanged);
                break;

            case ChunkType.ToolUse:
                messages.Add(new AgentChatMessage
                {
                    Type = MessageType.ToolInvocation,
                    Content = chunk.Content
                });
                await InvokeAsync(StateHasChanged);
                break;

            case ChunkType.Response:
                currentResponse.Content += chunk.Content;
                await InvokeAsync(StateHasChanged);
                break;

            case ChunkType.Complete:
                messages.Add(currentResponse);
                currentAgentAction = string.Empty;
                await InvokeAsync(StateHasChanged);
                break;
        }
    }
}
```

## Migration Path to Official MapAGUI()

**When to Migrate:**

1. **AG-UI Package Reaches GA**: Preview status removed, stable API guaranteed
2. **Multi-Agent Support**: MapAGUI supports dynamic agent selection
3. **Dependency Injection**: MapAGUI allows injecting custom services
4. **State Management**: Built-in chat history and conversation persistence

**Migration Checklist:**

- [ ] AG-UI package version >= 1.0.0 (GA)
- [ ] MapAGUI supports agent factory pattern
- [ ] MapAGUI supports tenant context injection
- [ ] MapAGUI supports custom state persistence
- [ ] MapAGUI integrates with ASP.NET Core DI
- [ ] Performance benchmarks show no regression
- [ ] All existing features supported (follow-up questions, workflow sync)

**Until then**, our custom SSE implementation provides:
- Full AG-UI protocol compliance
- All advanced features PRFactory requires
- Production-ready stability
- Clear migration path when official support is ready

## Testing AG-UI Compliance

**Manual Testing:**

1. Start PRFactory: `dotnet run --project src/PRFactory.Web`
2. Navigate to a ticket detail page
3. Open browser DevTools → Network tab
4. Send a message in the agent chat
5. Verify SSE stream:
   - Content-Type: `text/event-stream`
   - Chunks in format: `data: {json}\n\n`
   - Chunk types: Reasoning, ToolUse, Response, Complete

**Automated Testing:**

```csharp
[Fact]
public async Task StreamResponseAsync_ReturnsAGUICompliantChunks()
{
    // Arrange
    var ticketId = Guid.NewGuid();
    var message = "Analyze this ticket";

    // Act
    var chunks = new List<AgentStreamChunk>();
    await foreach (var chunk in _chatService.StreamResponseAsync(ticketId, message))
    {
        chunks.Add(chunk);
    }

    // Assert
    Assert.Contains(chunks, c => c.Type == ChunkType.Reasoning);
    Assert.Contains(chunks, c => c.Type == ChunkType.Response);
    Assert.Contains(chunks, c => c.Type == ChunkType.Complete && c.IsFinal);

    // Verify AG-UI protocol compliance
    foreach (var chunk in chunks)
    {
        Assert.NotNull(chunk.ChunkId);
        Assert.NotNull(chunk.Type);
        Assert.NotNull(chunk.Content);
    }
}
```

## References

- **Official AG-UI Documentation**: https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/
- **Backend Tool Rendering**: https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/backend-tool-rendering
- **NuGet Package**: https://www.nuget.org/packages/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore
- **GitHub Issue (AG-UI .NET Support)**: https://github.com/microsoft/agent-framework/issues/1774
- **PRFactory Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **PRFactory Implementation Status**: [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)

## Summary

PRFactory implements the **AG-UI protocol specification** using a custom SSE endpoint that provides:

✅ **Protocol Compliance**: Full adherence to AG-UI chunk types and SSE format
✅ **Multi-Agent Routing**: Dynamic agent selection based on tenant and workflow state
✅ **Advanced Features**: Chat history, follow-up questions, workflow synchronization
✅ **Production Ready**: Stable, tested, and proven in production workloads
✅ **Future Proof**: Clear migration path to MapAGUI() when multi-agent support is added

The official `MapAGUI()` extension method is included in project dependencies for future use, but our custom implementation provides the flexibility and features PRFactory requires today.
