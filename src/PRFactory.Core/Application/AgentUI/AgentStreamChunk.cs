namespace PRFactory.Core.Application.AgentUI;

/// <summary>
/// Represents a chunk of data in an agent's streaming response.
/// Used for Server-Sent Events (SSE) to provide real-time updates to clients.
/// </summary>
public class AgentStreamChunk
{
    /// <summary>
    /// Unique identifier for this chunk.
    /// </summary>
    public string ChunkId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of chunk being streamed.
    /// </summary>
    public ChunkType Type { get; set; }

    /// <summary>
    /// Content of the chunk.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is the final chunk in the stream.
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Additional metadata for the chunk.
    /// Can include tool names, parameters, or other context-specific data.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Defines the type of streaming chunk.
/// </summary>
public enum ChunkType
{
    /// <summary>
    /// Agent thinking or reasoning step.
    /// </summary>
    Reasoning,

    /// <summary>
    /// Agent is invoking a tool.
    /// </summary>
    ToolUse,

    /// <summary>
    /// Output from a tool invocation.
    /// </summary>
    ToolResult,

    /// <summary>
    /// Partial response from the agent (may be multiple chunks).
    /// </summary>
    Response,

    /// <summary>
    /// Stream has finished successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// An error occurred during streaming.
    /// </summary>
    Error
}
