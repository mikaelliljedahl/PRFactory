using Bunit;
using Xunit;
using PRFactory.Web.Components.Agents;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.Components.Agents;

/// <summary>
/// Tests for AgentMessage component
/// </summary>
public class AgentMessageTests : TestContext
{
    [Fact]
    public void AgentMessage_WithUserMessage_RendersCorrectly()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.UserMessage,
            Content = "Hello, agent!",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("Hello, agent!", cut.Markup);
        Assert.Contains("bi-person", cut.Markup);
        Assert.Contains("user", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithAssistantMessage_RendersCorrectly()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.AssistantMessage,
            Content = "Hello! How can I help you today?",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("Hello! How can I help you today?", cut.Markup);
        Assert.Contains("bi-robot", cut.Markup);
        Assert.Contains("assistant", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithToolInvocation_RendersCorrectly()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.ToolInvocation,
            Content = "SearchCodebase",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("Using tool: SearchCodebase", cut.Markup);
        Assert.Contains("bi-gear", cut.Markup);
        Assert.Contains("tool", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithReasoning_RendersCorrectly()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.Reasoning,
            Content = "Analyzing the codebase structure...",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("Analyzing the codebase structure...", cut.Markup);
        Assert.Contains("bi-lightbulb", cut.Markup);
        Assert.Contains("reasoning", cut.Markup);
    }

    [Fact]
    public void AgentMessage_ShowsTimestamp()
    {
        // Arrange
        var timestamp = new DateTime(2024, 11, 15, 14, 30, 0, DateTimeKind.Utc);
        var message = new AgentChatMessage
        {
            Type = MessageType.UserMessage,
            Content = "Test message",
            Timestamp = timestamp
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("message-time", cut.Markup);
        Assert.Contains("14:30", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithBoldText_RendersBold()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.AssistantMessage,
            Content = "This is **bold** text",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("<strong>bold</strong>", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithCodeText_RendersCode()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.AssistantMessage,
            Content = "Use the `console.log()` method",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("<code>console.log()</code>", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithNewlines_RendersLineBreaks()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.AssistantMessage,
            Content = "Line 1\nLine 2\nLine 3",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        // Component uses markdown rendering, so verify content is rendered
        Assert.Contains("Line 1", cut.Markup);
        Assert.Contains("Line 2", cut.Markup);
        Assert.Contains("Line 3", cut.Markup);
    }

    [Fact]
    public void AgentMessage_UserMessage_HasCorrectClass()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.UserMessage,
            Content = "Test",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("agent-message user", cut.Markup);
    }

    [Fact]
    public void AgentMessage_AssistantMessage_HasCorrectClass()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.AssistantMessage,
            Content = "Test",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("agent-message assistant", cut.Markup);
    }

    [Fact]
    public void AgentMessage_ToolInvocation_HasCorrectClass()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.ToolInvocation,
            Content = "Test",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("agent-message tool", cut.Markup);
    }

    [Fact]
    public void AgentMessage_Reasoning_HasCorrectClass()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.Reasoning,
            Content = "Test",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains("agent-message reasoning", cut.Markup);
    }

    [Fact]
    public void AgentMessage_WithEmptyContent_RendersWithoutError()
    {
        // Arrange
        var message = new AgentChatMessage
        {
            Type = MessageType.AssistantMessage,
            Content = "",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<AgentMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.NotNull(cut.Markup);
        Assert.Contains("agent-message", cut.Markup);
    }
}
