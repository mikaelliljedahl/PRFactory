namespace PRFactory.Core.Application.AI;

/// <summary>
/// Configuration for creating AI agents with tool support.
/// </summary>
public class AIAgentConfiguration
{
    /// <summary>
    /// The name of the agent (e.g., "AnalyzerAgent", "ImplementerAgent").
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// System instructions/prompt for the agent.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// List of tool names enabled for this agent.
    /// </summary>
    public string[] EnabledTools { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Maximum tokens for agent responses.
    /// </summary>
    public int MaxTokens { get; set; } = 8000;

    /// <summary>
    /// Temperature for response generation (0.0-1.0).
    /// </summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// Whether streaming is enabled for real-time responses.
    /// </summary>
    public bool StreamingEnabled { get; set; } = true;
}
