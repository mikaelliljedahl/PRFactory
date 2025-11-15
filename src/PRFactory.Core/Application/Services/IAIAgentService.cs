using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.AI;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for creating and executing AI agents with Microsoft Agent Framework SDK integration.
/// Note: Tools parameter uses object type to avoid circular dependency with AgentTools project.
/// </summary>
public interface IAIAgentService
{
    /// <summary>
    /// Creates an AI agent from configuration with tool support.
    /// </summary>
    /// <param name="config">Agent configuration</param>
    /// <param name="tools">Available tools for the agent (IEnumerable of ITool instances)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent instance (type depends on AF SDK availability)</returns>
    Task<object> CreateAgentAsync(
        AIAgentConfiguration config,
        IEnumerable<object> tools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes agent with multi-turn conversation support.
    /// Returns streaming chunks for real-time UI updates.
    /// </summary>
    /// <param name="agent">Agent instance created by CreateAgentAsync</param>
    /// <param name="userMessage">User message to process</param>
    /// <param name="conversationHistory">Previous conversation messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Streaming chunks for real-time updates</returns>
    IAsyncEnumerable<AgentStreamChunk> ExecuteAgentAsync(
        object agent,
        string userMessage,
        List<AgentChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);
}
