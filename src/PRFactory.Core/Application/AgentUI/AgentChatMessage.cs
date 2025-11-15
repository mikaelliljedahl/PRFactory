namespace PRFactory.Core.Application.AgentUI;

/// <summary>
/// Represents a message in an agent chat conversation.
/// Supports different message types for user messages, agent responses, tool invocations, and errors.
/// </summary>
public class AgentChatMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// UTC timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of message (user, assistant, tool invocation, etc.).
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata for the message.
    /// Can include tool parameters, results, or other context-specific data.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Defines the type of chat message.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Message from a human user.
    /// </summary>
    UserMessage,

    /// <summary>
    /// Response from the AI agent.
    /// </summary>
    AssistantMessage,

    /// <summary>
    /// Agent is invoking a tool (e.g., git, file operations).
    /// </summary>
    ToolInvocation,

    /// <summary>
    /// Output from a tool invocation.
    /// </summary>
    ToolResult,

    /// <summary>
    /// Agent internal reasoning or thinking process.
    /// </summary>
    Reasoning,

    /// <summary>
    /// Agent asking for clarification from the user.
    /// </summary>
    FollowUpQuestion,

    /// <summary>
    /// An error occurred during processing.
    /// </summary>
    Error
}
