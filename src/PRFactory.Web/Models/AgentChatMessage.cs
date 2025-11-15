namespace PRFactory.Web.Models;

/// <summary>
/// Represents a single message in the agent chat interface.
/// </summary>
public class AgentChatMessage
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of message.
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Defines the type of chat message.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    UserMessage,

    /// <summary>
    /// Message from the AI assistant.
    /// </summary>
    AssistantMessage,

    /// <summary>
    /// Tool invocation notification.
    /// </summary>
    ToolInvocation,

    /// <summary>
    /// Agent reasoning/thinking step.
    /// </summary>
    Reasoning
}
