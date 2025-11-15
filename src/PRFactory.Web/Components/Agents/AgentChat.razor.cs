using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using System.Text.Json;

namespace PRFactory.Web.Components.Agents;

public partial class AgentChat
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Inject]
    private HttpClient HttpClient { get; set; } = null!;

    [Inject]
    private ILogger<AgentChat> Logger { get; set; } = null!;

    private List<PRFactory.Web.Models.AgentChatMessage> messages = new();
    private string userInput = string.Empty;
    private bool isStreaming;
    private string currentAgentAction = string.Empty;
    private ElementReference messagesContainer;
    private WorkflowState agentStatus = WorkflowState.Triggered;
    private bool requiresUserInput = false;
    private string currentQuestion = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadChatHistoryAsync();
    }

    private async Task LoadChatHistoryAsync()
    {
        try
        {
            var history = await HttpClient.GetFromJsonAsync<List<PRFactory.Web.Models.AgentChatMessage>>(
                $"/api/agent/chat/history?ticketId={TicketId}");
            messages = history ?? new();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load chat history for ticket {TicketId}", TicketId);
            messages = new();
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(userInput)) return;

        var userMessage = new PRFactory.Web.Models.AgentChatMessage
        {
            Type = PRFactory.Web.Models.MessageType.UserMessage,
            Content = userInput
        };
        messages.Add(userMessage);
        var messageToSend = userInput;
        userInput = string.Empty;
        isStreaming = true;

        try
        {
            await StreamAgentResponseAsync(messageToSend);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send message for ticket {TicketId}", TicketId);
            messages.Add(new PRFactory.Web.Models.AgentChatMessage
            {
                Type = PRFactory.Web.Models.MessageType.AssistantMessage,
                Content = "Sorry, I encountered an error processing your request. Please try again."
            });
        }
        finally
        {
            isStreaming = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task StreamAgentResponseAsync(string message)
    {
        var url = $"/api/agent/chat/stream?ticketId={TicketId}&message={Uri.EscapeDataString(message)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var currentResponse = new PRFactory.Web.Models.AgentChatMessage
        {
            Type = PRFactory.Web.Models.MessageType.AssistantMessage,
            Content = string.Empty
        };

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var json = line.Substring(6); // Remove "data: " prefix
            var chunk = JsonSerializer.Deserialize<AgentStreamChunk>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (chunk == null) continue;

            switch (chunk.Type)
            {
                case ChunkType.Reasoning:
                    currentAgentAction = chunk.Content;
                    await InvokeAsync(StateHasChanged);
                    break;

                case ChunkType.ToolUse:
                    messages.Add(new PRFactory.Web.Models.AgentChatMessage
                    {
                        Type = PRFactory.Web.Models.MessageType.ToolInvocation,
                        Content = chunk.Content
                    });
                    await InvokeAsync(StateHasChanged);
                    break;

                case ChunkType.Response:
                    currentResponse.Content += chunk.Content;
                    await InvokeAsync(StateHasChanged);
                    break;

                case ChunkType.Complete:
                    if (!string.IsNullOrEmpty(currentResponse.Content))
                    {
                        messages.Add(currentResponse);
                    }
                    currentAgentAction = string.Empty;
                    await InvokeAsync(StateHasChanged);
                    break;

                case ChunkType.Error:
                    messages.Add(new PRFactory.Web.Models.AgentChatMessage
                    {
                        Type = PRFactory.Web.Models.MessageType.AssistantMessage,
                        Content = $"Error: {chunk.Content}"
                    });
                    currentAgentAction = string.Empty;
                    await InvokeAsync(StateHasChanged);
                    break;
            }
        }
    }

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !isStreaming)
        {
            await SendMessageAsync();
        }
    }

    private async Task HandleUserAnswer(string answer)
    {
        requiresUserInput = false;
        currentQuestion = string.Empty;
        userInput = answer;
        await SendMessageAsync();
    }
}
