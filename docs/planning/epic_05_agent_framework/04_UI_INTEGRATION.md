# 04: UI Integration - AG-UI Protocol & Blazor Components

**Document Purpose:** Complete specification for AG-UI protocol integration and Blazor UI components for agent interaction.

**Last Updated:** 2025-11-13

---

## Table of Contents

- [AG-UI Protocol Overview](#ag-ui-protocol-overview)
- [Blazor Component Architecture](#blazor-component-architecture)
- [Streaming Response Implementation](#streaming-response-implementation)
- [Follow-Up Question Flow](#follow-up-question-flow)
- [Approval Gate UI Patterns](#approval-gate-ui-patterns)
- [Implementation Guide](#implementation-guide)

---

## AG-UI Protocol Overview

### What is AG-UI?

AG-UI is Microsoft's standard protocol for agent user interfaces, using **HTTP + Server-Sent Events (SSE)** for real-time streaming communication between agents and UIs.

**Reference:** https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/getting-started

### Protocol Architecture

```
┌─────────────────┐         HTTP POST          ┌──────────────────┐
│  Blazor Client  │ ────────────────────────▶  │   API Endpoint   │
│  (Browser)      │                             │ /api/agent/chat  │
└─────────────────┘                             └──────────────────┘
         │                                               │
         │                                               │
         │            SSE Stream (text/event-stream)     │
         │ ◀────────────────────────────────────────────┘
         │
         │  Events:
         │  - message.start
         │  - message.delta (streaming text)
         │  - tool.use (agent invoking tool)
         │  - tool.result (tool execution result)
         │  - message.complete
         │  - question.followup (agent asking for clarification)
         │  - approval.required (agent needs approval)
```

### Message Format

**Request (HTTP POST):**
```json
{
  "ticketId": "guid",
  "message": "User message or answer",
  "threadId": "existing-thread-id-or-null",
  "context": {
    "tenantId": "guid",
    "agentName": "AnalyzerAgent"
  }
}
```

**Response Stream (SSE Events):**
```
event: message.start
data: {"messageId": "msg-123", "role": "assistant"}

event: message.delta
data: {"delta": "Analyzing ticket "}

event: message.delta
data: {"delta": "PROJ-123..."}

event: tool.use
data: {"toolName": "GetJiraTicket", "parameters": {"ticketKey": "PROJ-123"}}

event: tool.result
data: {"toolName": "GetJiraTicket", "result": "{...ticket data...}"}

event: message.delta
data: {"delta": "Found 3 related files: "}

event: message.complete
data: {"messageId": "msg-123", "usage": {"inputTokens": 1200, "outputTokens": 450}}
```

---

## Blazor Component Architecture

### Component Hierarchy

```
AgentInteractionPage.razor (Route: /tickets/{id}/agent)
  ├── AgentChat.razor
  │     ├── AgentMessageList.razor
  │     │     └── AgentMessage.razor (multiple)
  │     │           ├── AgentReasoningStep.razor
  │     │           ├── AgentToolUse.razor
  │     │           └── AgentToolResult.razor
  │     ├── AgentStreamingIndicator.razor
  │     ├── AgentFollowUpQuestion.razor
  │     └── AgentApprovalGate.razor
  └── AgentInputBox.razor
```

### Component Specifications

#### AgentChat.razor

**Purpose:** Main container for agent conversation UI.

**Implementation:**
```razor
@* PRFactory.Web/Components/Agents/AgentChat.razor *@
@inject IAgentChatService AgentChatService
@inject NavigationManager Navigation

<div class="agent-chat-container">
    <AgentMessageList Messages="@messages" />
    
    @if (isStreaming)
    {
        <AgentStreamingIndicator 
            CurrentAction="@currentAction" 
            CurrentTool="@currentTool" />
    }
    
    @if (followUpQuestion != null)
    {
        <AgentFollowUpQuestion
            Question="@followUpQuestion"
            OnAnswer="HandleFollowUpAnswer" />
    }
    
    @if (approvalRequired != null)
    {
        <AgentApprovalGate
            ProposedAction="@approvalRequired"
            OnApprove="HandleApproval"
            OnReject="HandleRejection" />
    }
    
    @if (!isStreaming && followUpQuestion == null && approvalRequired == null)
    {
        <AgentInputBox 
            OnSendMessage="HandleUserMessage"
            Disabled="@isStreaming" />
    }
</div>

@code {
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }
    
    private List<AgentMessageDto> messages = new();
    private bool isStreaming;
    private string? currentAction;
    private string? currentTool;
    private AgentFollowUpQuestionDto? followUpQuestion;
    private AgentApprovalRequestDto? approvalRequired;
    private string? threadId;
}
```

**Code-Behind (AgentChat.razor.cs):**
```csharp
namespace PRFactory.Web.Components.Agents;

public partial class AgentChat
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }
    
    [Inject]
    private IAgentChatService AgentChatService { get; set; } = null!;
    
    private List<AgentMessageDto> messages = new();
    private bool isStreaming;
    private string? currentAction;
    private string? currentTool;
    private AgentFollowUpQuestionDto? followUpQuestion;
    private AgentApprovalRequestDto? approvalRequired;
    private string? threadId;
    
    protected override async Task OnInitializedAsync()
    {
        // Load existing conversation history for this ticket
        var history = await AgentChatService.GetConversationHistoryAsync(TicketId);
        messages = history.Messages;
        threadId = history.ThreadId;
    }
    
    private async Task HandleUserMessage(string message)
    {
        // Add user message to UI immediately
        messages.Add(new AgentMessageDto
        {
            Role = "user",
            Content = message,
            Timestamp = DateTime.UtcNow
        });
        
        isStreaming = true;
        StateHasChanged();
        
        // Stream agent response
        await foreach (var chunk in AgentChatService.StreamResponseAsync(
            TicketId, message, threadId))
        {
            await ProcessStreamChunk(chunk);
        }
        
        isStreaming = false;
        StateHasChanged();
    }
    
    private async Task ProcessStreamChunk(AgentStreamChunk chunk)
    {
        switch (chunk.EventType)
        {
            case "message.start":
                messages.Add(new AgentMessageDto
                {
                    Id = chunk.MessageId,
                    Role = "assistant",
                    Content = "",
                    Timestamp = DateTime.UtcNow
                });
                break;
                
            case "message.delta":
                var lastMessage = messages.LastOrDefault(m => m.Role == "assistant");
                if (lastMessage != null)
                {
                    lastMessage.Content += chunk.Delta;
                }
                break;
                
            case "tool.use":
                currentTool = chunk.ToolName;
                currentAction = $"Using {chunk.ToolName}...";
                break;
                
            case "tool.result":
                currentTool = null;
                var toolMessage = messages.LastOrDefault(m => m.Role == "assistant");
                toolMessage?.ToolUses.Add(new ToolUseDto
                {
                    ToolName = chunk.ToolName,
                    Result = chunk.ToolResult
                });
                break;
                
            case "question.followup":
                followUpQuestion = new AgentFollowUpQuestionDto
                {
                    Question = chunk.Question,
                    Options = chunk.Options
                };
                break;
                
            case "approval.required":
                approvalRequired = new AgentApprovalRequestDto
                {
                    Action = chunk.Action,
                    Reason = chunk.Reason,
                    RiskLevel = chunk.RiskLevel
                };
                break;
                
            case "message.complete":
                threadId = chunk.ThreadId;
                break;
        }
        
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task HandleFollowUpAnswer(string answer)
    {
        followUpQuestion = null;
        await HandleUserMessage(answer);
    }
    
    private async Task HandleApproval()
    {
        approvalRequired = null;
        await AgentChatService.SendApprovalAsync(TicketId, threadId!, approved: true);
        // Agent will continue after approval
    }
    
    private async Task HandleRejection(string reason)
    {
        approvalRequired = null;
        await AgentChatService.SendApprovalAsync(TicketId, threadId!, approved: false, reason);
        isStreaming = false;
    }
}
```

---

## Streaming Response Implementation

### Backend: AgentChatService

**Interface:**
```csharp
public interface IAgentChatService
{
    IAsyncEnumerable<AgentStreamChunk> StreamResponseAsync(
        Guid ticketId,
        string message,
        string? threadId = null,
        CancellationToken ct = default);
        
    Task<ConversationHistoryDto> GetConversationHistoryAsync(
        Guid ticketId,
        CancellationToken ct = default);
        
    Task SendApprovalAsync(
        Guid ticketId,
        string threadId,
        bool approved,
        string? reason = null,
        CancellationToken ct = default);
}
```

**Implementation:**
```csharp
public class AgentChatService : IAgentChatService
{
    private readonly IAgentFactory _agentFactory;
    private readonly ICheckpointService _checkpointService;
    private readonly ITenantContext _tenantContext;
    
    public async IAsyncEnumerable<AgentStreamChunk> StreamResponseAsync(
        Guid ticketId,
        string message,
        string? threadId = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Load checkpoint if resuming
        var checkpoint = await _checkpointService.LoadAsync(ticketId, "AgentChat");
        
        // Create or resume agent
        var agent = await _agentFactory.CreateAgentAsync(
            _tenantContext.TenantId,
            "AnalyzerAgent", // Or dynamically determined
            ct);
        
        // Build messages array
        var conversationMessages = checkpoint?.ConversationHistory != null
            ? JsonSerializer.Deserialize<List<ChatMessage>>(checkpoint.ConversationHistory)!
            : new List<ChatMessage>();
        
        conversationMessages.Add(new ChatMessage
        {
            Role = "user",
            Content = message
        });
        
        // Stream agent response
        yield return new AgentStreamChunk
        {
            EventType = "message.start",
            MessageId = Guid.NewGuid().ToString()
        };
        
        await foreach (var chunk in agent.RunStreamingAsync(conversationMessages, ct))
        {
            // Map Agent Framework events to AG-UI events
            yield return MapToAgentStreamChunk(chunk);
        }
        
        yield return new AgentStreamChunk
        {
            EventType = "message.complete",
            ThreadId = threadId ?? Guid.NewGuid().ToString()
        };
        
        // Save checkpoint
        await _checkpointService.SaveAsync(new Checkpoint
        {
            TicketId = ticketId,
            NodeName = "AgentChat",
            ConversationHistory = JsonSerializer.Serialize(conversationMessages),
            CreatedAt = DateTime.UtcNow
        });
    }
    
    private AgentStreamChunk MapToAgentStreamChunk(AgentFrameworkStreamChunk chunk)
    {
        return chunk.Type switch
        {
            "content_delta" => new AgentStreamChunk
            {
                EventType = "message.delta",
                Delta = chunk.Text
            },
            "tool_call" => new AgentStreamChunk
            {
                EventType = "tool.use",
                ToolName = chunk.ToolName,
                Parameters = chunk.Parameters
            },
            "tool_result" => new AgentStreamChunk
            {
                EventType = "tool.result",
                ToolName = chunk.ToolName,
                ToolResult = chunk.Result
            },
            _ => new AgentStreamChunk { EventType = "unknown" }
        };
    }
}
```

### API Endpoint (SSE)

**Controller:**
```csharp
[ApiController]
[Route("api/agent")]
public class AgentChatController : ControllerBase
{
    private readonly IAgentChatService _agentChatService;
    
    [HttpPost("chat")]
    public async Task ChatAsync([FromBody] AgentChatRequest request)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        
        await foreach (var chunk in _agentChatService.StreamResponseAsync(
            request.TicketId,
            request.Message,
            request.ThreadId,
            HttpContext.RequestAborted))
        {
            await Response.WriteAsync($"event: {chunk.EventType}\n");
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}
```

---

## Follow-Up Question Flow

### Component: AgentFollowUpQuestion.razor

```razor
<Card Title="Agent Question" Icon="question-circle" CssClass="border-warning">
    <div class="mb-3">
        <p class="lead">@Question.Question</p>
    </div>
    
    @if (Question.Options != null && Question.Options.Any())
    {
        <div class="btn-group-vertical w-100" role="group">
            @foreach (var option in Question.Options)
            {
                <button type="button" 
                        class="btn btn-outline-primary text-start" 
                        @onclick="() => SelectOption(option)">
                    @option
                </button>
            }
        </div>
    }
    else
    {
        <FormField Label="Your Answer">
            <InputText @bind-Value="freeformAnswer" class="form-control" 
                       placeholder="Type your answer..." />
        </FormField>
        <LoadingButton OnClick="SubmitFreeformAnswer" Icon="check">
            Submit Answer
        </LoadingButton>
    }
</Card>
```

**Code-Behind:**
```csharp
public partial class AgentFollowUpQuestion
{
    [Parameter, EditorRequired]
    public AgentFollowUpQuestionDto Question { get; set; } = null!;
    
    [Parameter, EditorRequired]
    public EventCallback<string> OnAnswer { get; set; }
    
    private string freeformAnswer = "";
    
    private async Task SelectOption(string option)
    {
        await OnAnswer.InvokeAsync(option);
    }
    
    private async Task SubmitFreeformAnswer()
    {
        if (!string.IsNullOrWhiteSpace(freeformAnswer))
        {
            await OnAnswer.InvokeAsync(freeformAnswer);
        }
    }
}
```

### Example Flow

```
Agent: "I found 3 authentication modules. Which one is affected?"
  → Options: ["JwtAuth.cs", "OAuth2Auth.cs", "ApiKeyAuth.cs"]
  
User clicks: "JwtAuth.cs"
  
Agent: "Analyzing JwtAuth.cs..."
  → Uses ReadFile tool
  → Continues analysis with correct file
```

---

## Approval Gate UI Patterns

### Component: AgentApprovalGate.razor

```razor
<Card Title="Approval Required" Icon="shield-exclamation" CssClass="border-danger">
    <div class="mb-3">
        <StatusBadge State="@ProposedAction.RiskLevel" />
        <h5 class="mt-2">@ProposedAction.Action</h5>
        <p class="text-muted">@ProposedAction.Reason</p>
    </div>
    
    @if (ProposedAction.Preview != null)
    {
        <div class="border rounded p-3 mb-3 bg-light">
            <h6>Preview:</h6>
            <pre><code>@ProposedAction.Preview</code></pre>
        </div>
    }
    
    <div class="d-flex gap-2">
        <LoadingButton OnClick="Approve" 
                       CssClass="btn-success" 
                       Icon="check-circle">
            Approve
        </LoadingButton>
        
        <button type="button" 
                class="btn btn-danger" 
                @onclick="ShowRejectModal">
            <i class="bi bi-x-circle"></i> Reject
        </button>
    </div>
</Card>

@if (showRejectModal)
{
    <Modal Title="Reject Action" OnClose="CancelReject">
        <FormField Label="Reason for Rejection">
            <InputTextArea @bind-Value="rejectionReason" 
                           class="form-control" 
                           rows="3" 
                           placeholder="Explain why you're rejecting..." />
        </FormField>
        <div class="d-flex gap-2 mt-3">
            <LoadingButton OnClick="ConfirmReject" 
                           CssClass="btn-danger" 
                           Icon="x-circle">
                Confirm Rejection
            </LoadingButton>
            <button type="button" class="btn btn-secondary" @onclick="CancelReject">
                Cancel
            </button>
        </div>
    </Modal>
}
```

**Code-Behind:**
```csharp
public partial class AgentApprovalGate
{
    [Parameter, EditorRequired]
    public AgentApprovalRequestDto ProposedAction { get; set; } = null!;
    
    [Parameter, EditorRequired]
    public EventCallback OnApprove { get; set; }
    
    [Parameter, EditorRequired]
    public EventCallback<string> OnReject { get; set; }
    
    private bool showRejectModal;
    private string rejectionReason = "";
    
    private async Task Approve()
    {
        await OnApprove.InvokeAsync();
    }
    
    private void ShowRejectModal()
    {
        showRejectModal = true;
    }
    
    private async Task ConfirmReject()
    {
        await OnReject.InvokeAsync(rejectionReason);
        showRejectModal = false;
        rejectionReason = "";
    }
    
    private void CancelReject()
    {
        showRejectModal = false;
        rejectionReason = "";
    }
}
```

### Example Scenarios

**Scenario 1: Write File Approval**
```
Action: "Write changes to UserService.cs"
RiskLevel: "Medium"
Reason: "Modifying authentication logic"
Preview: "public bool ValidateToken(string token) { ... }"

[Approve] [Reject]
```

**Scenario 2: Git Commit Approval**
```
Action: "Commit changes with message 'feat: Add token expiration check'"
RiskLevel: "Low"
Reason: "Ready to commit 1 file with 15 lines changed"
Preview: "
  M src/Services/UserService.cs
  +15 -3
"

[Approve] [Reject]
```

---

## Implementation Guide

### Phase 1: Backend Setup (Week 9, Days 1-2)

1. **Create AgentChatService interface and implementation**
   - File: `/PRFactory.Infrastructure/Application/AgentChatService.cs`
   - Implement streaming async enumerable

2. **Create API endpoint with SSE support**
   - File: `/PRFactory.Api/Controllers/AgentChatController.cs`
   - Configure SSE headers

3. **Test streaming with curl**
   ```bash
   curl -N -H "Content-Type: application/json" \
     -d '{"ticketId":"...","message":"Analyze this ticket"}' \
     http://localhost:5000/api/agent/chat
   ```

### Phase 2: Blazor Components (Week 9, Days 3-5)

4. **Create base AgentChat component**
   - File: `/PRFactory.Web/Components/Agents/AgentChat.razor`
   - Implement SSE client using JavaScript interop (minimal)

5. **Create supporting components**
   - AgentMessageList.razor
   - AgentMessage.razor
   - AgentStreamingIndicator.razor

6. **Test in isolation with mock data**

### Phase 3: Interactive Features (Week 10, Days 1-3)

7. **Implement AgentFollowUpQuestion component**
   - File: `/PRFactory.Web/Components/Agents/AgentFollowUpQuestion.razor`
   - Test with multiple choice and freeform answers

8. **Implement AgentApprovalGate component**
   - File: `/PRFactory.Web/Components/Agents/AgentApprovalGate.razor`
   - Test approve/reject flows

### Phase 4: Integration (Week 10, Days 4-5)

9. **Integrate into Ticket Detail page**
   - Add "Chat with Agent" tab
   - Wire up to existing ticket workflows

10. **End-to-end testing**
    - Test full conversation flow
    - Test follow-up questions
    - Test approval gates
    - Performance testing (latency, memory)

---

## Next Steps

1. **Review with UX team** - Approve component designs
2. **Implement Phase 1** - Backend streaming infrastructure
3. **Implement Phase 2** - Base Blazor components
4. **Implement Phase 3** - Interactive features
5. **Implement Phase 4** - Integration and testing

**See:** `05_CONFIGURATION.md` for agent configuration and `04_IMPLEMENTATION_ROADMAP.md` for timeline.
