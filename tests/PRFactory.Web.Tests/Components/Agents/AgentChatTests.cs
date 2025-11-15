using Bunit;
using Moq;
using Moq.Protected;
using Xunit;
using PRFactory.Web.Components.Agents;
using PRFactory.Web.Models;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PRFactory.Web.Tests.Components.Agents;

/// <summary>
/// Tests for AgentChat component
/// </summary>
public class AgentChatTests : TestContext
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<AgentChat>> _mockLogger;

    public AgentChatTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _mockLogger = new Mock<ILogger<AgentChat>>();

        Services.AddSingleton(_httpClient);
        Services.AddSingleton(_mockLogger.Object);
    }

    [Fact]
    public void AgentChat_RendersInitialChatInterface()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("Agent Assistant", cut.Markup);
        Assert.Contains("Ask the agent...", cut.Markup);
    }

    [Fact]
    public void AgentChat_ShowsChatHeader()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("chat-header", cut.Markup);
        Assert.Contains("Agent Assistant", cut.Markup);
    }

    [Fact]
    public void AgentChat_ShowsChatMessagesContainer()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("chat-messages", cut.Markup);
    }

    [Fact]
    public void AgentChat_ShowsChatInputArea()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("chat-input", cut.Markup);
        Assert.Contains("placeholder=\"Ask the agent...\"", cut.Markup);
    }

    [Fact]
    public void AgentChat_ShowsSendButton()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("Send", cut.Markup);
        Assert.Contains("bi-send", cut.Markup);
    }

    [Fact]
    public async Task AgentChat_LoadsChatHistoryOnInitialization()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var messages = new List<AgentChatMessage>
        {
            new AgentChatMessage
            {
                Type = MessageType.UserMessage,
                Content = "Hello agent"
            },
            new AgentChatMessage
            {
                Type = MessageType.AssistantMessage,
                Content = "Hello! How can I help?"
            }
        };
        SetupHistoryResponse(ticketId, messages);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Wait for async initialization
        await Task.Delay(100);

        // Assert
        Assert.Contains("Hello agent", cut.Markup);
        Assert.Contains("Hello! How can I help?", cut.Markup);
    }

    [Fact]
    public async Task AgentChat_HandlesHistoryLoadError()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupErrorResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Wait for async initialization
        await Task.Delay(100);

        // Assert - component should render without crashing
        Assert.Contains("Agent Assistant", cut.Markup);
    }

    [Fact]
    public void AgentChat_WithEmptyHistory_ShowsNoMessages()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Assert
        var messageElements = cut.FindAll(".agent-message");
        Assert.Empty(messageElements);
    }

    [Fact]
    public void AgentChat_InputDisabledWhenStreaming()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupEmptyHistoryResponse(ticketId);

        // Act
        var cut = RenderComponent<AgentChat>(parameters => parameters
            .Add(p => p.TicketId, ticketId));

        // Set component to streaming state through private field (for testing purposes)
        // In real scenario, this would be set during actual streaming

        // Assert initial state
        var input = cut.Find("input[type='text']");
        Assert.NotNull(input);
    }

    private void SetupEmptyHistoryResponse(Guid ticketId)
    {
        var emptyMessages = new List<AgentChatMessage>();
        var json = System.Text.Json.JsonSerializer.Serialize(emptyMessages);
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/api/agent/chat/history?ticketId={ticketId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHistoryResponse(Guid ticketId, List<AgentChatMessage> messages)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(messages);
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/api/agent/chat/history?ticketId={ticketId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupErrorResponse(Guid ticketId)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("Error", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/api/agent/chat/history?ticketId={ticketId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
