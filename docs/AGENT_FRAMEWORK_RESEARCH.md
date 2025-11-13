# Microsoft Agent Framework - Comprehensive Research Report

**Date**: November 2025  
**Framework Status**: Public Preview  
**Version Compatibility**: .NET 8.0+ and Python 3.10+  
**Repository**: https://github.com/microsoft/agent-framework

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Core Framework Capabilities](#core-framework-capabilities)
3. [Agent Abstraction Model](#agent-abstraction-model)
4. [Tool Integration Architecture](#tool-integration-architecture)
5. [AG-UI Integration](#ag-ui-integration)
6. [Configuration & Lifecycle Management](#configuration--lifecycle-management)
7. [Workflow Architecture](#workflow-architecture)
8. [Middleware System](#middleware-system)
9. [Multi-Tenant Considerations](#multi-tenant-considerations)
10. [Observability & Monitoring](#observability--monitoring)
11. [Project Structure & Separation of Concerns](#project-structure--separation-of-concerns)
12. [Error Handling & Resilience](#error-handling--resilience)
13. [Testing Strategies](#testing-strategies)
14. [PRFactory Integration Recommendations](#prfactory-integration-recommendations)

---

## Executive Summary

The **Microsoft Agent Framework** is a unified, production-ready framework for building AI agents and multi-agent workflows. It combines the simplicity of AutoGen's abstractions with Semantic Kernel's enterprise features (thread-based state management, type safety, filters, telemetry, extensive model support).

### Key Strengths for PRFactory

- **Graph-Based Workflow Architecture**: Aligns perfectly with PRFactory's multi-graph design (RefinementGraph, PlanningGraph, ImplementationGraph)
- **Multi-Platform Support**: Native provider abstractions for Azure OpenAI, OpenAI, Azure AI Foundry
- **AG-UI Integration**: Modern web UI protocol with real-time streaming and state management
- **Checkpoint-Based Resume**: Built-in checkpointing for fault tolerance and long-running workflows
- **OpenTelemetry Observability**: Standards-based tracing, metrics, and logging
- **Thread-Based State Management**: Perfect for multi-turn conversations and workflow persistence
- **Middleware System**: Clean cross-cutting concerns (logging, validation, security)
- **Function Tool Integration**: Type-safe function calling with minimal boilerplate

### Compatibility with PRFactory Architecture

PRFactory's existing multi-graph architecture maps directly to Agent Framework concepts:

| PRFactory Component | Agent Framework Equivalent | Notes |
|-------------------|-------------------------|-------|
| RefinementGraph | Custom Workflow Executor | Can extend AIAgent base class |
| PlanningGraph | Custom Workflow Executor | Full customization support |
| ImplementationGraph | Custom Workflow Executor | Integrates with tool execution |
| Checkpoint System | Built-in WorkflowState Checkpoints | Seamless suspension/resumption |
| Git Platform Providers | IChatClient + Tool Registry | Agents use tools for platform ops |
| Thread Management | AgentThread | Native state persistence |
| Workflow Orchestrator | Graph Workflow + Executors | Full composition support |

---

## Core Framework Capabilities

### 1. AI Agents

Agents leverage Large Language Models (LLMs) to process inputs, make decisions, invoke tools/MCP servers, and generate responses.

#### Core Agent Flow

```
User Input → Agent (LLM Decision) → Tool Selection → Tool Execution → Response Generation
```

#### Supported LLM Providers

- **Azure OpenAI** (ChatCompletion, Responses API)
- **OpenAI** (ChatCompletion, Responses API, Assistants)
- **Azure AI Foundry** (multiple SDK options)
- **Custom Implementations** (via `IChatClient` interface)

#### Agent Capabilities

- ✅ Function calling (automatic tool selection)
- ✅ Multi-turn conversations with history
- ✅ Custom tools (AIFunction, MCP servers)
- ✅ Structured output via JSON schema
- ✅ Streaming responses
- ✅ Context providers for memory
- ✅ Middleware for cross-cutting concerns

### 2. Workflows (Graph-Based)

Graph-based systems connecting multiple agents and functions for complex, multi-step tasks.

#### Workflow Components

- **Executors**: Individual processing units (agents, functions, subprocess calls)
- **Edges**: Message flows between executors with conditional routing
- **Checkpoints**: Save/resume capability for long-running operations
- **Execution Patterns**: Sequential, concurrent, handoff, group chat

#### Workflow Features

- ✅ Type-safe message routing
- ✅ Conditional branching and loops
- ✅ Parallel execution
- ✅ Dynamic routing based on LLM decisions
- ✅ State snapshots (checkpoints)
- ✅ Human-in-the-loop pauses
- ✅ Time-travel debugging

### 3. Framework Inheritance & Composition

```
IBatch/IChatClient (LLM inference interfaces)
         ↓
    AIAgent (base class)
         ↓
    ├─ ChatClientAgent (chat-based LLM agents)
    ├─ AzureAIFoundryAgent (service-managed history)
    ├─ OpenAIAssistantAgent (OpenAI Assistants)
    └─ CustomAgent (extend AIAgent for complete control)
         ↓
    Composed into Workflows via Executors
```

---

## Agent Abstraction Model

### Base Class: `AIAgent`

All agents inherit from the abstract `AIAgent` base class, providing:

```csharp
// Core abstraction in C#
public abstract class AIAgent
{
    public string Name { get; }
    public string? Description { get; }
    
    // Primary execution method
    public abstract Task<AgentRunResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);
    
    // Streaming execution
    public abstract IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);
    
    // Conversation state
    public abstract AgentThread GetNewThread();
}
```

### Agent Types

#### 1. Simple Chat Client-Based Agents (Most Common)

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

// Create chat client
var chatClient = new AzureOpenAIClient(
    new Uri("https://<resource>.openai.azure.com"),
    new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini");

// Convert to Agent Framework
var agent = chatClient.AsIChatClient()
    .CreateAIAgent(
        name: "RefinementAgent",
        instructions: "You are an expert at refining software requirements...");
```

#### 2. Azure AI Foundry Agent

```csharp
using Azure.AI.Foundry;

var client = new AzureAIFoundryClient(credential, project_endpoint);
var agent = await client.CreateAgentAsync(
    name: "PlanningAgent",
    model: "gpt-4o",
    instructions: "You are an expert implementation planner...");
```

#### 3. Custom Agent (Full Control)

```csharp
public class CustomRefinementAgent : AIAgent
{
    private readonly ITicketUpdateService _ticketService;
    
    public override string Name => "CustomRefinement";
    
    public override async Task<AgentRunResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Complete control over execution logic
        var userMessage = messages.Last().Content;
        
        // Custom orchestration
        var refinement = await _ticketService.RefineRequirementsAsync(
            userMessage, cancellationToken);
        
        return new AgentRunResponse(
            messages: [new(ChatRole.Assistant, refinement)],
            thread: thread ?? GetNewThread());
    }
    
    public override IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Implement streaming version
        throw new NotImplementedException();
    }
}
```

---

## Tool Integration Architecture

### Function Tools (Type-Safe)

Function tools are C# methods that agents can invoke. The framework automatically handles:
- Parameter type validation
- Description generation from attributes
- Serialization/deserialization
- Error handling and retries

#### Implementation Pattern

```csharp
// Define tool function
[Description("Fetches the latest ticket state and requirements")]
public static async Task<TicketState> GetTicketStateAsync(
    [Description("The unique ticket identifier")] string ticketId,
    [Description("Include extended metadata")] bool includeMetadata = false)
{
    // Your implementation
    return await _ticketService.GetStateAsync(ticketId, includeMetadata);
}

// Register with agent
var agent = chatClient
    .AsIChatClient()
    .CreateAIAgent(
        name: "RefinementAgent",
        instructions: "You refine ticket requirements",
        tools: [
            AIFunctionFactory.Create(GetTicketStateAsync),
            AIFunctionFactory.Create(UpdateTicketStateAsync),
            AIFunctionFactory.Create(GetAvailableStatesAsync)
        ]);

// Agent automatically selects and invokes tools
var response = await agent.RunAsync(messages, thread);
```

#### Function Tool Best Practices

1. **Atomic Operations**: Each function should perform a single, well-defined action
2. **Clear Descriptions**: Use `[Description]` attributes for all parameters
3. **Type Safety**: Strongly-typed return values (not strings)
4. **Error Handling**: Throw meaningful exceptions; let agent handle recovery
5. **Idempotency**: Functions should be safe to call multiple times
6. **Timeouts**: Set reasonable execution timeouts

#### Example Tool Library

```csharp
// PRFactory.Infrastructure/Agents/Tools/TicketTools.cs
namespace PRFactory.Infrastructure.Agents.Tools;

public class TicketTools
{
    private readonly ITicketUpdateService _ticketUpdateService;
    private readonly ITicketRepository _ticketRepository;
    
    public TicketTools(ITicketUpdateService ticketUpdateService, ITicketRepository ticketRepository)
    {
        _ticketUpdateService = ticketUpdateService;
        _ticketRepository = ticketRepository;
    }
    
    [Description("Retrieve the latest ticket update draft for review")]
    public async Task<TicketUpdateDto> GetLatestDraftAsync(
        [Description("Ticket identifier")] Guid ticketId)
    {
        var update = await _ticketUpdateService.GetLatestUpdateAsync(ticketId);
        return MapToDto(update);
    }
    
    [Description("Save ticket requirement refinement")]
    public async Task<RefinementResultDto> SaveRefinementAsync(
        [Description("Ticket identifier")] Guid ticketId,
        [Description("Refined requirements")] string refinedText,
        [Description("Clarity score (1-10)")] int clarityScore)
    {
        var result = await _ticketUpdateService.SaveRefinementAsync(
            ticketId, refinedText, clarityScore);
        return MapToDto(result);
    }
}
```

#### Tool Registration in Dependency Injection

```csharp
// Program.cs
services.AddScoped<TicketTools>();
services.AddScoped<GitPlatformTools>();
services.AddScoped<WorkflowTools>();

services.AddScoped<IAgentFactory, AgentFactory>();

// AgentFactory.cs
public class AgentFactory : IAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly TicketTools _ticketTools;
    private readonly GitPlatformTools _gitTools;
    private readonly WorkflowTools _workflowTools;
    
    public IAgent CreateRefinementAgent()
    {
        var tools = new AIFunction[]
        {
            AIFunctionFactory.Create(_ticketTools.GetLatestDraftAsync),
            AIFunctionFactory.Create(_ticketTools.SaveRefinementAsync),
            AIFunctionFactory.Create(_gitTools.GetRepositoryInfoAsync),
            AIFunctionFactory.Create(_workflowTools.LogEventAsync)
        };
        
        return _chatClient
            .AsIChatClient()
            .CreateAIAgent(
                name: "RefinementAgent",
                instructions: RefinementPrompts.Instructions,
                tools: tools.ToList());
    }
}
```

### MCP (Model Context Protocol) Integration

MCP allows integration with external tools and services through a standardized protocol.

#### MCP Server Integration

```csharp
using Microsoft.Agents.AI;

// Create MCP client
using var mcpClient = await McpClientFactory.CreateAsync(
    new StdioServerParameters { Command = "node", Arguments = "mcp-server.js" });

// List available tools
var toolList = await mcpClient.ListToolsAsync();

// Cast MCP tools to AITool and use with agent
var mcpTools = toolList.Tools
    .Select(t => (AITool)new McpTool(t, mcpClient))
    .ToList();

var agent = chatClient
    .AsIChatClient()
    .CreateAIAgent(
        instructions: "Use MCP tools for GitHub access",
        tools: mcpTools);
```

#### Popular MCP Servers

- **GitHub**: Repository access, PR operations
- **File System**: File read/write operations
- **SQLite**: Database queries
- **Calculator**: Numeric computations

---

## AG-UI Integration

### Overview

AG-UI is a modern protocol for building web-based AI agent applications with:
- Real-time streaming responses
- Server-Sent Events (SSE) communication
- Persistent conversation threads
- Rich, interactive UI components

### Architecture

```
┌─────────────────────────────────────────────────────┐
│         Web Browser (AG-UI Client)                   │
│    (React/Vue/Blazor-based rich UI)                  │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP POST (message)
                       │ SSE (streaming response)
                       ▼
┌──────────────────────────────────────────────────────┐
│     AG-UI Server (ASP.NET Core)                      │
│     MapAGUI("/", agent)                              │
│     - Handles HTTP endpoints                          │
│     - Manages SSE streaming                           │
│     - Coordinates agent execution                     │
└──────────────────────┬───────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────┐
│     Agent Framework                                  │
│     - ChatClientAgent                                │
│     - Tool execution                                 │
│     - Response streaming                             │
└──────────────────────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────┐
│     LLM Provider (Azure OpenAI, OpenAI, etc.)        │
└──────────────────────────────────────────────────────┘
```

### Server-Side Setup

#### Required Packages

```bash
dotnet add package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore --prerelease
dotnet add package Azure.AI.OpenAI --prerelease
dotnet add package Azure.Identity
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease
```

#### Complete Server Implementation

```csharp
// Program.cs
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHttpClient();
builder.Services.AddLogging();
builder.Services.AddAGUI();  // CRITICAL: Enables AG-UI support

// Configure Azure OpenAI
string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set");
string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("Deployment name not set");

// Create LLM client
ChatClient chatClient = new AzureOpenAIClient(
    new Uri(endpoint),
    new DefaultAzureCredential())
    .GetChatClient(deploymentName);

// Create agent
AIAgent agent = chatClient
    .AsIChatClient()
    .CreateAIAgent(
        name: "RefinementAgent",
        instructions: "You are an expert software requirements analyst. " +
                      "Help refine and clarify ticket requirements.");

// Build app and map AG-UI endpoint
WebApplication app = builder.Build();

// This registers:
// - POST /api/messages (send user message)
// - GET /api/thread (create/manage threads)
// - SSE streaming for responses
app.MapAGUI("/", agent);

await app.RunAsync();
```

#### Environment Configuration

```bash
# .env or Azure Key Vault
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
ASPNETCORE_ENVIRONMENT=Development
```

### Client-Side Setup

#### Required Packages

```bash
dotnet add package Microsoft.Agents.AI.AGUI --prerelease
dotnet add package Microsoft.Agents.AI --prerelease
```

#### Console Client Example

```csharp
// Program.cs - AG-UI Client
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;

string serverUrl = Environment.GetEnvironmentVariable("AGUI_SERVER_URL")
    ?? "http://localhost:8888";

using HttpClient httpClient = new()
{
    Timeout = TimeSpan.FromSeconds(60)
};

// Create AG-UI client
AGUIChatClient agUIChatClient = new(httpClient, serverUrl);

// Create agent from remote server
AIAgent agent = agUIChatClient.CreateAIAgent(
    name: "remote-agent",
    description: "Remote AG-UI agent");

// Get new conversation thread
AgentThread thread = agent.GetNewThread();

// Conversation loop
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are helpful.")
};

while (true)
{
    Console.Write("You: ");
    string? input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(input) || input is ":q" or "quit")
        break;
    
    messages.Add(new(ChatRole.User, input));
    
    // Stream responses
    bool isFirst = true;
    await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(messages, thread))
    {
        if (isFirst)
        {
            Console.WriteLine("\nAgent:");
            isFirst = false;
        }
        
        // Display streamed content
        foreach (AIContent content in update.Contents)
        {
            if (content is TextContent text)
                Console.Write(text.Text);
        }
    }
    
    Console.WriteLine("\n");
}
```

### AG-UI Protocol Features

#### Request/Response Model

**Client → Server:**
```json
{
  "messages": [
    {"role": "user", "content": "Refine this requirement..."},
    {"role": "assistant", "content": "..."}
  ],
  "thread_id": "conv-123",
  "conversation_context": {...}
}
```

**Server → Client (SSE Stream):**
```
event: start
data: {"run_id": "run-456", "thread_id": "conv-123"}

event: content
data: {"type": "text", "content": "The requirement can be"}

event: content
data: {"type": "text", "content": " clarified as..."}

event: end
data: {"status": "completed"}
```

#### Key Interaction Patterns

1. **Thread Management**: Each conversation maintains a thread ID for state persistence
2. **Streaming**: Content arrives as incremental chunks, enabling real-time UI updates
3. **Tool Calls**: Agent function invocations are streamed as separate events
4. **Error Handling**: Exceptions converted to user-friendly error events

---

## Configuration & Lifecycle Management

### Configuration Sources

The Agent Framework supports multiple configuration approaches:

#### 1. Environment Variables

```bash
# LLM Provider
AZURE_OPENAI_ENDPOINT=https://resource.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
AZURE_OPENAI_API_KEY=...

# Observability
ENABLE_OTEL=true
ENABLE_SENSITIVE_DATA=false
OTLP_ENDPOINT=http://localhost:4317
APPLICATION_INSIGHTS_CONNECTION_STRING=...

# AG-UI
AGUI_SERVER_URL=http://localhost:8888
```

#### 2. Dependency Injection Configuration

For PRFactory's multi-tenant model, use DI configuration:

```csharp
// Program.cs
public static IServiceCollection AddAgentFramework(
    this IServiceCollection services,
    IConfiguration config)
{
    // Register core services
    services.AddHttpClient();
    services.AddLogging();
    services.AddAGUI();
    
    // Register agent factory
    services.AddScoped<IAgentFactory>(provider =>
        new AgentFactory(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<ILogger<AgentFactory>>()));
    
    // Register tool libraries
    services.AddScoped<TicketTools>();
    services.AddScoped<GitPlatformTools>();
    services.AddScoped<WorkflowTools>();
    
    return services;
}
```

#### 3. Database-Driven Configuration (Recommended for PRFactory)

```csharp
// PRFactory.Infrastructure/Agents/Configuration/AgentConfiguration.cs
public class AgentConfiguration
{
    public Guid TenantId { get; set; }
    public string AgentName { get; set; }
    public string AgentType { get; set; }  // e.g., "Refinement", "Planning"
    public string Instructions { get; set; }
    public string ModelProvider { get; set; }  // "AzureOpenAI", "OpenAI"
    public string DeploymentName { get; set; }
    public int MaxTokens { get; set; }
    public float Temperature { get; set; }
    public bool StreamingEnabled { get; set; }
    public string[] EnabledTools { get; set; }
    public Dictionary<string, string> ProviderSettings { get; set; }
}

// Repository for DB access
public interface IAgentConfigurationRepository
{
    Task<AgentConfiguration> GetAsync(Guid tenantId, string agentName);
    Task UpdateAsync(AgentConfiguration config);
    Task DeleteAsync(Guid tenantId, string agentName);
}

// Factory that reads from DB
public class TenantAwareAgentFactory : IAgentFactory
{
    private readonly IAgentConfigurationRepository _configRepo;
    private readonly Guid _tenantId;
    
    public async Task<AIAgent> CreateAgentAsync(string agentName)
    {
        var config = await _configRepo.GetAsync(_tenantId, agentName);
        
        // Create based on configuration
        var chatClient = CreateChatClient(config);
        var tools = CreateTools(config);
        
        return chatClient
            .AsIChatClient()
            .CreateAIAgent(
                name: config.AgentName,
                instructions: config.Instructions,
                tools: tools);
    }
}
```

### Agent Lifecycle

#### Initialization

```csharp
// 1. Create chat client
var chatClient = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName);

// 2. Convert to Agent Framework IChatClient
var agentChatClient = chatClient.AsIChatClient();

// 3. Create agent (no persistent resources)
var agent = agentChatClient.CreateAIAgent(
    name: "RefinementAgent",
    instructions: "...",
    tools: toolsList);

// 4. Get thread for conversation state
var thread = agent.GetNewThread();
```

#### Execution

```csharp
// Single execution
var response = await agent.RunAsync(messages, thread);

// Streaming execution
await foreach (var update in agent.RunStreamingAsync(messages, thread))
{
    // Handle streamed content
    foreach (var content in update.Contents)
    {
        if (content is TextContent text)
            Console.WriteLine(text.Text);
    }
}
```

#### State Persistence

```csharp
// Store thread state (in PRFactory's Checkpoint table)
public class ThreadCheckpoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public string AgentName { get; set; }
    public AgentThread Thread { get; set; }  // Serialized
    public List<ChatMessage> ConversationHistory { get; set; }
    public WorkflowState WorkflowState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResumedAt { get; set; }
}

// Resume from checkpoint
var checkpoint = await _checkpointRepo.GetAsync(ticketId, "RefinementAgent");
var thread = checkpoint.Thread;
var messages = checkpoint.ConversationHistory;

var response = await agent.RunAsync(messages, thread);
```

---

## Workflow Architecture

### Graph-Based Execution Model

Workflows define explicit execution paths (unlike agents which follow dynamic LLM-guided paths).

#### Workflow Components

```csharp
// Executor: Individual processing unit
public interface IWorkflowExecutor
{
    Task<ExecutorOutput> ExecuteAsync(ExecutorInput input, CancellationToken cancellationToken);
}

// Edge: Message flow and routing
public class WorkflowEdge
{
    public string FromExecutor { get; set; }
    public string ToExecutor { get; set; }
    public Func<ExecutorOutput, bool> Condition { get; set; }  // Conditional routing
}

// Workflow: Directed graph
public class WorkflowGraph
{
    public List<IWorkflowExecutor> Executors { get; set; }
    public List<WorkflowEdge> Edges { get; set; }
    public string StartExecutor { get; set; }
    public string EndExecutor { get; set; }
}
```

#### Workflow Patterns

**1. Sequential Execution**
```csharp
RefinementAgent → PlanningAgent → ImplementationAgent → Approval
```

**2. Parallel Execution**
```
         ┌─ GitPlan ─┐
Start ───┤           ├─ Merge → Approval
         └─ JiraPost ─┘
```

**3. Conditional Routing**
```csharp
Agent → Condition Check → {
    if (qualityScore > 0.8) → Approve
    else → RefinementAgent
}
```

**4. Handoff Pattern**
```csharp
// First agent does work, second agent reviews
RefinementAgent (input) → Decision → {
    if approved: Proceed
    else: PlanningAgent (revision)
}
```

### Checkpoint-Based Resume

```csharp
public class WorkflowCheckpoint
{
    public Guid WorkflowId { get; set; }
    public string CurrentExecutor { get; set; }
    public ExecutorState State { get; set; }
    public object ExecutorOutput { get; set; }
    public WorkflowStatus Status { get; set; }  // Running, Suspended, Completed
    public DateTime CreatedAt { get; set; }
}

// Save checkpoint
await _checkpointService.SaveCheckpointAsync(
    workflowId,
    currentExecutor: "RefinementAgent",
    state: new { messages, thread },
    status: WorkflowStatus.Suspended);

// Resume from checkpoint
var checkpoint = await _checkpointService.GetAsync(workflowId);
var nextOutput = await ExecuteNextAsync(checkpoint);
```

---

## Middleware System

### Middleware Types

The Agent Framework provides three middleware categories:

#### 1. Agent Run Middleware

Intercepts all agent executions to inspect/modify input and output.

```csharp
async Task<AgentRunResponse> LoggingAgentRunMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentThread? thread,
    AgentRunOptions? options,
    AIAgent agent,
    CancellationToken cancellationToken)
{
    _logger.LogInformation($"Agent {agent.Name} executing with {messages.Count()} messages");
    
    var response = await agent.RunAsync(messages, thread, options, cancellationToken);
    
    _logger.LogInformation($"Agent {agent.Name} completed with {response.Messages.Count} responses");
    
    return response;
}
```

#### 2. Function Calling Middleware

Intercepts tool/function invocations.

```csharp
async ValueTask<object?> ValidatingFunctionMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    _logger.LogInformation($"Invoking {context.Function.Name} with {context.Arguments}");
    
    // Validate arguments
    if (!ValidateArguments(context))
    {
        return new ErrorResult("Invalid arguments");
    }
    
    // Execute function
    var result = await next(context, cancellationToken);
    
    // Transform result
    return TransformResult(result);
}
```

#### 3. IChatClient Middleware

Intercepts calls to the underlying chat client (LLM inference).

```csharp
// Wraps IChatClient for cross-cutting concerns
public class ObservableChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly ILogger _logger;
    
    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatCompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"LLM call with {chatMessages.Count} messages");
        
        await foreach (var update in _inner.CompleteStreamingAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }
}
```

### Middleware Registration

```csharp
var agent = chatClient
    .AsIChatClient()
    .CreateAIAgent(
        name: "RefinementAgent",
        instructions: "...",
        tools: toolsList)
    .WithMiddleware(
        runMiddleware: LoggingAgentRunMiddleware,
        functionMiddleware: ValidatingFunctionMiddleware);
```

### Common Middleware Patterns

| Use Case | Middleware Type | Example |
|----------|-----------------|---------|
| **Logging** | Agent Run | Log inputs, outputs, execution time |
| **Rate Limiting** | Agent Run | Limit agents per tenant |
| **Cost Tracking** | IChatClient | Count tokens, track spend |
| **Security Validation** | Function | Verify tool access permissions |
| **Audit Trail** | All three | Record all agent actions |
| **Caching** | IChatClient | Cache LLM responses |
| **Content Moderation** | Agent Run | Filter inappropriate content |

---

## Multi-Tenant Considerations

### PRFactory Multi-Tenant Architecture

PRFactory uses logical multi-tenancy with these isolation boundaries:

1. **Tenant Context**: Request-scoped tenant ID
2. **Data Isolation**: Per-tenant database partitions
3. **Configuration Isolation**: Per-tenant agent configurations
4. **Tool Isolation**: Per-tenant tool access
5. **Checkpoint Isolation**: Per-tenant workflow state

### Agent Framework Integration

```csharp
// Tenant context accessor
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantName { get; }
}

// Scoped per request
services.AddScoped<ITenantContext>(provider =>
{
    var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var tenantId = Guid.Parse(httpContext.User.FindFirst("tenant_id").Value);
    return new TenantContext(tenantId);
});

// Tenant-aware agent factory
public class TenantAwareAgentFactory : IAgentFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly IAgentConfigurationRepository _configRepo;
    
    public async Task<AIAgent> CreateRefinementAgentAsync()
    {
        var config = await _configRepo.GetAsync(
            _tenantContext.TenantId,
            "RefinementAgent");
        
        var tools = new[]
        {
            AIFunctionFactory.Create(
                async (Guid ticketId) =>
                {
                    // _tenantContext.TenantId automatically applied
                    return await _ticketService.GetAsync(_tenantContext.TenantId, ticketId);
                })
        };
        
        return CreateAgent(config, tools);
    }
}

// Tenant-aware tool execution
public class TicketTools
{
    private readonly ITenantContext _tenantContext;
    
    public async Task<TicketDto> GetLatestDraftAsync(Guid ticketId)
    {
        // Automatic tenant isolation
        return await _ticketService.GetAsync(_tenantContext.TenantId, ticketId);
    }
}
```

### Multi-Tenant Configuration

```csharp
// Database schema
public class AgentConfigurationEntity
{
    public Guid TenantId { get; set; }  // Partition key
    public string AgentName { get; set; }
    
    // Tenant-specific settings
    public string? CustomInstructions { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; }
    public int? TokenLimit { get; set; }  // Per-tenant limits
    public bool Enabled { get; set; }
}

// Per-tenant checkpoint storage
public class WorkflowCheckpointEntity
{
    public Guid TenantId { get; set; }  // Partition key
    public Guid TicketId { get; set; }
    public string CheckpointData { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Tenant Isolation Patterns

**Pattern 1: Tenant-Scoped Tools**
```csharp
// Tools automatically work within tenant context
public class TenantAwareTicketTools
{
    public TenantAwareTicketTools(ITenantContext tenantContext, ITicketService ticketService)
    {
        _tenantContext = tenantContext;
        _ticketService = ticketService;
    }
    
    public Task<TicketDto> GetAsync(Guid ticketId)
    {
        // Tenant automatically applied
        return _ticketService.GetAsync(_tenantContext.TenantId, ticketId);
    }
}
```

**Pattern 2: Per-Tenant Agent Instances**
```csharp
// Create separate agents per tenant request
var agentFactory = serviceScopeFactory.CreateScope()
    .ServiceProvider.GetRequiredService<IAgentFactory>();
    
var agent = await agentFactory.CreateRefinementAgentAsync();
// Agent automatically works within tenant context
```

**Pattern 3: Tenant-Specific Instructions**
```csharp
var config = await _configRepo.GetAsync(_tenantContext.TenantId, "RefinementAgent");

var instructions = config.CustomInstructions 
    ?? DefaultInstructions;

var agent = chatClient
    .AsIChatClient()
    .CreateAIAgent(
        name: "RefinementAgent",
        instructions: instructions);
```

---

## Observability & Monitoring

### OpenTelemetry Integration

The Agent Framework emits traces, metrics, and logs following OpenTelemetry GenAI semantic conventions.

#### Automatic Instrumentation

Enable with minimal configuration:

```csharp
// Program.cs
var chatClient = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(
        sourceName: "PRFactory.Agents",
        configure: cfg => cfg.EnableSensitiveData = false)  // Don't log prompts in prod
    .Build();

var agent = chatClient
    .CreateAIAgent(
        name: "RefinementAgent",
        instructions: "...")
    .WithOpenTelemetry(
        sourceName: "PRFactory.Agents",
        enableSensitiveData: false);
```

#### Exporting Traces

**Azure Monitor Integration (Recommended for Production)**

```csharp
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var applicationInsightsConnectionString = 
    Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string not set");

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("PRFactory.Agents", serviceVersion: "1.0.0");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("PRFactory.Agents")
    .AddSource("*Microsoft.Extensions.AI")
    .AddSource("*Microsoft.Extensions.Agents*")
    .AddAzureMonitorTraceExporter(options =>
        options.ConnectionString = applicationInsightsConnectionString)
    .Build();
```

**Local Development (OTLP Exporter)**

```csharp
using OpenTelemetry.Exporter;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("PRFactory.Agents")
    .AddOtlpExporter(options =>
        options.Endpoint = new Uri("http://localhost:4317"))
    .Build();

// View with: docker run -p 4317:4317 -p 6831:6831/udp otel/opentelemetry-collector
```

### Automatic Trace Spans

The framework generates these spans automatically:

```
invoke_agent <agent_name>
├── chat <model>
│   └── (token_usage, latency, cost)
├── execute_tool <function_name>
│   └── (function_input, function_output, duration)
├── execute_tool <function_name>
│   └── ...
└── response_generation
    └── (output_tokens, total_cost)
```

### Custom Metrics

```csharp
using System.Diagnostics.Metrics;

public class AgentMetrics
{
    private readonly Meter _meter;
    private readonly Counter<int> _executionCounter;
    private readonly Histogram<double> _executionDuration;
    
    public AgentMetrics()
    {
        _meter = new Meter("PRFactory.Agents", "1.0.0");
        
        _executionCounter = _meter.CreateCounter<int>(
            "agent.executions",
            unit: "{execution}",
            description: "Number of agent executions");
        
        _executionDuration = _meter.CreateHistogram<double>(
            "agent.execution_duration",
            unit: "ms",
            description: "Agent execution duration in milliseconds");
    }
    
    public void RecordExecution(string agentName, double duration)
    {
        _executionCounter.Add(1, new KeyValuePair<string, object?>("agent.name", agentName));
        _executionDuration.Record(duration, new KeyValuePair<string, object?>("agent.name", agentName));
    }
}
```

### Logging Configuration

```csharp
services.AddLogging(builder =>
{
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(ResourceBuilder
            .CreateDefault()
            .AddService("PRFactory.Agents"));
        
        options.AddAzureMonitorLogExporter(opts =>
            opts.ConnectionString = applicationInsightsConnectionString);
        
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    })
    .SetMinimumLevel(LogLevel.Information);
});
```

### Key Metrics to Track

| Metric | Purpose | Alert Threshold |
|--------|---------|-----------------|
| Agent Execution Count | Usage tracking | Alert on unusual spikes |
| Agent Execution Duration | Performance | Alert if > 30s |
| Tool Execution Count | Tool usage | Track which tools are used |
| Token Usage | Cost tracking | Alert on > 100k tokens/day |
| Error Rate | Quality | Alert if > 5% |
| Checkpoint Save Duration | State management | Alert if > 5s |

---

## Project Structure & Separation of Concerns

### Recommended Architecture for PRFactory

```
/PRFactory
├── /src
│   ├── /PRFactory.Core
│   │   ├── /Agents
│   │   │   ├── IAgentFactory.cs
│   │   │   ├── IAgentService.cs
│   │   │   └── AgentDefinitions.cs
│   │   │
│   │   └── /Workflows
│   │       ├── IWorkflowExecutor.cs
│   │       └── WorkflowDefinitions.cs
│   │
│   ├── /PRFactory.Infrastructure
│   │   ├── /Agents
│   │   │   ├── /Tools               # Tool library (separate concern)
│   │   │   │   ├── TicketTools.cs
│   │   │   │   ├── GitPlatformTools.cs
│   │   │   │   └── WorkflowTools.cs
│   │   │   │
│   │   │   ├── /Configuration       # Tenant-aware config
│   │   │   │   ├── AgentConfigurationRepository.cs
│   │   │   │   └── AgentConfigurationEntity.cs
│   │   │   │
│   │   │   ├── /Middleware          # Cross-cutting concerns
│   │   │   │   ├── LoggingMiddleware.cs
│   │   │   │   ├── ValidationMiddleware.cs
│   │   │   │   ├── AuditMiddleware.cs
│   │   │   │   └── TelemetryMiddleware.cs
│   │   │   │
│   │   │   ├── /Execution
│   │   │   │   ├── AgentFactory.cs
│   │   │   │   ├── AgentService.cs
│   │   │   │   └── ThreadCheckpointService.cs
│   │   │   │
│   │   │   └── /Observability
│   │   │       ├── AgentMetrics.cs
│   │   │       └── AgentTelemetryService.cs
│   │   │
│   │   └── /Workflows
│   │       ├── WorkflowExecutor.cs
│   │       ├── WorkflowCheckpointService.cs
│   │       └── WorkflowGraph.cs
│   │
│   ├── /PRFactory.Web
│   │   └── /Services
│   │       ├── AgentApiService.cs    # Facade for Blazor
│   │       └── WorkflowUIService.cs
│   │
│   ├── /PRFactory.Api
│   │   └── /Controllers
│   │       ├── AgentsController.cs   # For external clients
│   │       └── WorkflowsController.cs
│   │
│   └── /PRFactory.Tests
│       ├── /Infrastructure
│       │   └── /Agents
│       │       ├── AgentFactoryTests.cs
│       │       ├── ToolTests.cs
│       │       └── MiddlewareTests.cs
│       │
│       └── /Integration
│           ├── AgentExecutionTests.cs
│           └── WorkflowTests.cs
```

### Key Separation Principles

#### 1. Tools in Separate Library

```csharp
// PRFactory.Infrastructure/Agents/Tools/TicketTools.cs
// NOT in UI/Pages or API/Controllers
namespace PRFactory.Infrastructure.Agents.Tools;

public class TicketTools
{
    private readonly ITicketUpdateService _service;
    
    [Description("Get latest ticket update")]
    public async Task<TicketUpdateDto> GetLatestAsync(Guid ticketId)
    {
        return await _service.GetLatestUpdateAsync(ticketId);
    }
}
```

#### 2. Configuration in Domain

```csharp
// PRFactory.Core/Agents/AgentDefinitions.cs
namespace PRFactory.Core.Agents;

public static class AgentDefinitions
{
    public const string RefinementAgent = "RefinementAgent";
    public const string PlanningAgent = "PlanningAgent";
    
    public static class RefinementInstructions
    {
        public const string Default = @"You are an expert software requirements analyst...";
        public const string Detailed = @"You are a detailed requirements engineer...";
    }
}
```

#### 3. Factory Patterns

```csharp
// PRFactory.Infrastructure/Agents/Execution/AgentFactory.cs
public interface IAgentFactory
{
    Task<AIAgent> CreateRefinementAgentAsync();
    Task<AIAgent> CreatePlanningAgentAsync();
    Task<AIAgent> CreateImplementationAgentAsync();
}

public class AgentFactory : IAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly IAgentConfigurationRepository _configRepo;
    private readonly TicketTools _ticketTools;
    
    public async Task<AIAgent> CreateRefinementAgentAsync()
    {
        var config = await _configRepo.GetAsync(_tenantId, "RefinementAgent");
        
        var tools = new[]
        {
            AIFunctionFactory.Create(_ticketTools.GetLatestAsync),
            AIFunctionFactory.Create(_ticketTools.SaveRefinementAsync)
        };
        
        return _chatClient
            .AsIChatClient()
            .CreateAIAgent(
                name: config.AgentName,
                instructions: config.Instructions,
                tools: tools.ToList())
            .WithMiddleware(
                runMiddleware: _loggingMiddleware,
                functionMiddleware: _validationMiddleware);
    }
}
```

#### 4. Service-Oriented Design

```csharp
// PRFactory.Infrastructure/Agents/Execution/AgentService.cs
public interface IAgentService
{
    Task<RefinementResultDto> RefineRequirementsAsync(Guid ticketId, Guid tenantId);
    Task<PlanResultDto> PlanImplementationAsync(Guid ticketId, Guid tenantId);
    IAsyncEnumerable<AgentStreamUpdate> StreamRefineAsync(Guid ticketId, Guid tenantId);
}

public class AgentService : IAgentService
{
    private readonly IAgentFactory _agentFactory;
    private readonly ICheckpointService _checkpointService;
    
    public async Task<RefinementResultDto> RefineRequirementsAsync(Guid ticketId, Guid tenantId)
    {
        var agent = await _agentFactory.CreateRefinementAgentAsync();
        var checkpoint = await _checkpointService.GetAsync(ticketId, "RefinementAgent");
        var messages = checkpoint?.Messages ?? new List<ChatMessage>();
        
        var response = await agent.RunAsync(messages, checkpoint?.Thread);
        
        // Save checkpoint
        await _checkpointService.SaveAsync(
            ticketId, "RefinementAgent", messages, response);
        
        return MapToDto(response);
    }
}
```

---

## Error Handling & Resilience

### Built-In Error Handling

```csharp
// Agent Framework throws on LLM errors
try
{
    var response = await agent.RunAsync(messages, thread);
}
catch (HttpRequestException ex)
{
    _logger.LogError($"LLM service unavailable: {ex.Message}");
    // Retry logic
    return await RetryWithBackoffAsync(() => agent.RunAsync(messages, thread));
}
catch (InvalidOperationException ex)
{
    _logger.LogError($"Invalid agent configuration: {ex.Message}");
    // Alert operations team
}
```

### Resilience Patterns (Using Polly)

```csharp
// Install: dotnet add package Polly

public static class ResiliencePolicies
{
    public static IAsyncPolicy<T> CreateAgentExecutionPolicy<T>()
    {
        return Policy<T>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult(r => r == null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),  // Exponential backoff
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} after {timespan.TotalSeconds}s");
                });
    }
}

// Usage
var policy = ResiliencePolicies.CreateAgentExecutionPolicy<AgentRunResponse>();

var response = await policy.ExecuteAsync(
    () => agent.RunAsync(messages, thread));
```

### Tool Execution Error Handling

```csharp
// Tools throw exceptions; let agent handle recovery
[Description("Save ticket refinement")]
public async Task<RefinementResultDto> SaveRefinementAsync(
    Guid ticketId,
    string refinedText)
{
    try
    {
        return await _ticketService.SaveAsync(ticketId, refinedText);
    }
    catch (ConcurrencyException)
    {
        throw new InvalidOperationException("Another user modified this ticket. Please refresh.");
    }
    catch (ValidationException ex)
    {
        throw new InvalidOperationException($"Validation failed: {ex.Message}");
    }
    // Don't catch all exceptions - let agent handle unexpected errors
}
```

### Checkpoint Recovery

```csharp
// Resume from last checkpoint on failure
public async Task<RefinementResultDto> RefineWithRecoveryAsync(Guid ticketId)
{
    try
    {
        var agent = await _agentFactory.CreateRefinementAgentAsync();
        var checkpoint = await _checkpointService.GetAsync(ticketId, "RefinementAgent");
        
        var response = await agent.RunAsync(
            checkpoint?.Messages ?? new List<ChatMessage>(),
            checkpoint?.Thread);
        
        return MapToDto(response);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Agent execution failed: {ex}");
        
        // Mark as failed but keep checkpoint for retry
        await _checkpointService.MarkFailedAsync(ticketId, "RefinementAgent", ex.Message);
        
        // User can retry later
        throw;
    }
}
```

---

## Testing Strategies

### Unit Testing Tools

```csharp
using Xunit;  // PRFactory uses xUnit (NO FluentAssertions)
using Moq;

[Fact]
public async Task TicketTools_GetLatestAsync_ReturnsDraftUpdate()
{
    // Arrange
    var mockTicketService = new Mock<ITicketUpdateService>();
    var expectedUpdate = new TicketUpdateDto { Id = Guid.NewGuid() };
    
    mockTicketService
        .Setup(s => s.GetLatestUpdateAsync(It.IsAny<Guid>()))
        .ReturnsAsync(expectedUpdate);
    
    var tools = new TicketTools(mockTicketService.Object, null);
    
    // Act
    var result = await tools.GetLatestAsync(Guid.NewGuid());
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedUpdate.Id, result.Id);
    mockTicketService.Verify(s => s.GetLatestUpdateAsync(It.IsAny<Guid>()), Times.Once);
}
```

### Mocking IChatClient

```csharp
[Fact]
public async Task RefinementAgent_CanExecuteWithMockLLM()
{
    // Arrange
    var mockChatClient = new Mock<IChatClient>();
    
    mockChatClient
        .Setup(c => c.CompleteAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatCompletionOptions>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ChatCompletion(
            "Refinement: The requirement should be clearer",
            new List<ChatMessage>()));
    
    var agent = mockChatClient.Object
        .AsIChatClient()
        .CreateAIAgent(
            name: "TestAgent",
            instructions: "Refine requirements");
    
    // Act
    var response = await agent.RunAsync(
        new[] { new ChatMessage(ChatRole.User, "Test requirement") });
    
    // Assert
    Assert.NotEmpty(response.Messages);
}
```

### Integration Testing

```csharp
[Fact]
public async Task RefinementWorkflow_ExecutesWithRealAgent()
{
    // Use test Azure OpenAI credentials
    var chatClient = new AzureOpenAIClient(
        new Uri(TestEnvironment.AzureOpenAIEndpoint),
        new DefaultAzureCredential())
        .GetChatClient(TestEnvironment.DeploymentName);
    
    var agent = chatClient
        .AsIChatClient()
        .CreateAIAgent(
            name: "TestAgent",
            instructions: "Refine: {{userInput}}");
    
    var messages = new[]
    {
        new ChatMessage(ChatRole.User, "Unclear requirement about API performance")
    };
    
    var response = await agent.RunAsync(messages);
    
    Assert.NotEmpty(response.Messages);
    Assert.Contains("refine", response.Messages.Last().Content, StringComparison.OrdinalIgnoreCase);
}
```

### Testing Observability

```csharp
[Fact]
public async Task Agent_GeneratesOpenTelemetryTraces()
{
    // Create test trace collector
    var traces = new List<Activity>();
    
    var chatClient = new AzureOpenAIClient(endpoint, credential)
        .GetChatClient(deploymentName)
        .AsIChatClient()
        .AsBuilder()
        .UseOpenTelemetry(sourceName: "TestAgent")
        .Build();
    
    var agent = chatClient.CreateAIAgent(name: "TestAgent", instructions: "...");
    
    // Execute agent
    await agent.RunAsync(messages);
    
    // Verify spans generated
    var spans = ActivitySource.AllInstances
        .Where(s => s.Name == "TestAgent")
        .SelectMany(s => s.EnumerateTrackedSpans());
    
    Assert.NotEmpty(spans);
    Assert.Single(spans, s => s.Name == "invoke_agent");
    Assert.Single(spans, s => s.Name.StartsWith("chat"));
}
```

---

## PRFactory Integration Recommendations

### Strategic Alignment

The Microsoft Agent Framework is **highly aligned** with PRFactory's architecture:

| PRFactory Pattern | Framework Support | Integration Level |
|-------------------|-------------------|-------------------|
| Multi-Graph Architecture | Graph-based Workflows | ✅ Direct mapping |
| Multi-Platform Support | Provider abstraction | ✅ IChatClient interface |
| Checkpoint-Based Resume | Built-in Checkpoints | ✅ Native support |
| State Management | AgentThread | ✅ Direct use |
| Tool Integration | AIFunctionFactory | ✅ Clean integration |
| Observability | OpenTelemetry | ✅ Standards-based |
| Multi-Tenant | Middleware system | ✅ Via context injection |
| UI Integration | AG-UI Protocol | ✅ Modern web UI |

### Implementation Roadmap

#### Phase 1: Foundation (Weeks 1-2)

1. **Create Agent Infrastructure**
   ```csharp
   // PRFactory.Infrastructure/Agents/
   ├── /Tools
   ├── /Configuration
   ├── /Middleware
   ├── /Execution
   └── /Observability
   ```

2. **Setup Dependencies**
   - Add NuGet packages: `Microsoft.Agents.AI`, `Microsoft.Extensions.AI`
   - Configure Azure OpenAI credentials
   - Setup OpenTelemetry exporters

3. **Build Tool Library**
   - Extract business logic into `TicketTools`, `GitPlatformTools`, `WorkflowTools`
   - Test tool execution in isolation
   - Implement tenant isolation

#### Phase 2: Agent Integration (Weeks 3-4)

1. **Implement Agent Factory**
   - Create `IAgentFactory` interface
   - Implement tenant-aware factory
   - Support agent configuration from database

2. **Integrate with Existing Graphs**
   - Map RefinementGraph → RefinementAgent
   - Map PlanningGraph → PlanningAgent
   - Keep existing orchestration logic

3. **AG-UI Server Setup**
   - MapAGUI endpoint in ASP.NET Core
   - Configure WebSocket/SSE streaming
   - Test with web client

#### Phase 3: State Management (Weeks 5-6)

1. **Checkpoint Integration**
   - Extend existing Checkpoint entity
   - Implement `ICheckpointService` using Agent Framework's format
   - Support pause/resume workflows

2. **Thread Management**
   - Store AgentThread in database
   - Implement thread recovery on server restart
   - Support multi-turn conversations

3. **Configuration Management**
   - Create `AgentConfigurationEntity` in database
   - Implement per-tenant customization
   - Support A/B testing different agents

#### Phase 4: Observability (Week 7)

1. **Enable Tracing**
   - Configure Azure Monitor exporter
   - Setup OpenTelemetry collectors
   - Create custom spans for workflow events

2. **Implement Metrics**
   - Track agent execution counts and duration
   - Monitor token usage and costs
   - Alert on error rates

3. **Logging**
   - Structured logging with tenant context
   - Audit trails for agent actions
   - Debug logging for development

#### Phase 5: Testing & Hardening (Week 8)

1. **Unit Tests**
   - 80% coverage on tools and factories
   - Mock agents for logic testing
   - Test middleware chains

2. **Integration Tests**
   - Test with real Azure OpenAI
   - Multi-tenant scenario testing
   - Failure recovery testing

3. **Performance & Load Testing**
   - Concurrent agent executions
   - Token usage under load
   - Checkpoint recovery performance

### Key Configuration Files

#### appsettings.json

```json
{
  "AgentFramework": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "DeploymentName": "gpt-4o-mini",
      "ApiVersion": "2024-08-01-preview"
    },
    "Observability": {
      "Enabled": true,
      "ExportToAzureMonitor": true,
      "EnableSensitiveData": false,
      "SamplingRate": 0.1
    },
    "Agents": {
      "RefinementAgent": {
        "Instructions": "You are an expert requirements analyst...",
        "MaxTokens": 4000,
        "Temperature": 0.7,
        "StreamingEnabled": true
      },
      "PlanningAgent": {
        "Instructions": "You are an expert implementation planner...",
        "MaxTokens": 8000,
        "Temperature": 0.5,
        "StreamingEnabled": true
      }
    }
  }
}
```

#### Program.cs Extension

```csharp
public static IServiceCollection AddAgentFrameworkIntegration(
    this IServiceCollection services,
    IConfiguration config)
{
    // Register core Agent Framework services
    services.AddHttpClient();
    services.AddLogging();
    services.AddAGUI();
    
    // Register Azure OpenAI
    var azureOpenAISection = config.GetSection("AgentFramework:AzureOpenAI");
    services.AddSingleton(_ =>
    {
        var endpoint = azureOpenAISection["Endpoint"]
            ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        var deploymentName = azureOpenAISection["DeploymentName"]
            ?? throw new InvalidOperationException("Deployment name not configured");
        
        var chatClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new DefaultAzureCredential())
            .GetChatClient(deploymentName);
        
        return chatClient.AsIChatClient();
    });
    
    // Register observability
    var obsSection = config.GetSection("AgentFramework:Observability");
    if (obsSection.GetValue<bool>("Enabled"))
    {
        services.AddOpenTelemetry(obsSection);
    }
    
    // Register Agent Framework services
    services.AddScoped<IAgentFactory, AgentFactory>();
    services.AddScoped<IAgentService, AgentService>();
    services.AddScoped<ICheckpointService, CheckpointService>();
    
    // Register tool libraries
    services.AddScoped<TicketTools>();
    services.AddScoped<GitPlatformTools>();
    services.AddScoped<WorkflowTools>();
    
    // Register middleware
    services.AddScoped<LoggingAgentMiddleware>();
    services.AddScoped<ValidationAgentMiddleware>();
    services.AddScoped<AuditAgentMiddleware>();
    
    return services;
}
```

### Breaking Changes & Migration Path

**No breaking changes to existing architecture:**

- Existing `RefinementGraph`, `PlanningGraph`, `ImplementationGraph` continue to work
- Agent Framework agents wrap around existing business logic
- Gradual migration: add one graph at a time
- Checkpoint format stays the same (Agent Framework uses same serialization)

**Migration Example:**

```csharp
// BEFORE: Direct graph execution
var refinement = await _refinementGraph.ExecuteAsync(ticket);

// AFTER: Using Agent Framework (behind the scenes)
var agent = await _agentFactory.CreateRefinementAgentAsync();
var refinement = await agent.RunAsync(messages, thread);
// Same result, but with LLM-guided logic and better observability
```

---

## Conclusion

The Microsoft Agent Framework provides a **production-ready, enterprise-grade foundation** for PRFactory's AI-powered workflow automation. Its alignment with PRFactory's multi-graph architecture, multi-tenant requirements, and observability needs makes it an ideal choice for:

1. **Replacing hand-rolled agent orchestration** with battle-tested framework
2. **Enabling AG-UI integration** for modern web-based workflows
3. **Providing built-in observability** without custom instrumentation
4. **Supporting multi-tenant deployments** through middleware and DI
5. **Enabling checkpoint-based resume** for fault-tolerant workflows

The 8-week implementation roadmap above provides a clear path to production integration while maintaining backward compatibility with existing PRFactory components.

---

## References

- **Official Documentation**: https://learn.microsoft.com/en-us/agent-framework/
- **GitHub Repository**: https://github.com/microsoft/agent-framework
- **AG-UI Integration**: https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/getting-started
- **Quick Start**: https://learn.microsoft.com/en-us/agent-framework/tutorials/quick-start
- **Sample Repository**: https://github.com/microsoft/Agent-Framework-Samples

