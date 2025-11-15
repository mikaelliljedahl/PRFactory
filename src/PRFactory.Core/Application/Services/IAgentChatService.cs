using PRFactory.Core.Application.AgentUI;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for managing real-time agent chat interactions using the AG-UI protocol.
/// Supports streaming agent responses via Server-Sent Events (SSE).
/// </summary>
public interface IAgentChatService
{
    /// <summary>
    /// Streams agent response for a user message.
    /// Yields chunks as the agent thinks, uses tools, and responds.
    /// </summary>
    /// <param name="ticketId">The ticket ID to associate the conversation with</param>
    /// <param name="userMessage">The user's message to the agent</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
    /// <returns>Async enumerable of stream chunks representing the agent's response</returns>
    IAsyncEnumerable<AgentStreamChunk> StreamResponseAsync(
        Guid ticketId,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets chat history for a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of chat messages ordered chronologically</returns>
    Task<List<AgentChatMessage>> GetChatHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a follow-up question from the agent.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="questionId">The question ID to answer</param>
    /// <param name="answer">The user's answer to the question</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation message</returns>
    Task<string> AnswerFollowUpQuestionAsync(
        Guid ticketId,
        string questionId,
        string answer,
        CancellationToken cancellationToken = default);
}
