using PRFactory.Infrastructure.Claude.Models;

namespace PRFactory.Infrastructure.Claude;

/// <summary>
/// Repository for storing and retrieving Claude conversation history per ticket
/// </summary>
public interface IConversationHistoryRepository
{
    /// <summary>
    /// Add a message exchange to the conversation history
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="phase">The phase (analysis, questions, planning, implementation)</param>
    /// <param name="userMessage">The user's message</param>
    /// <param name="assistantResponse">Claude's response</param>
    Task AddMessageAsync(string ticketId, string phase, Message userMessage, string assistantResponse);

    /// <summary>
    /// Get the full conversation history for a ticket
    /// </summary>
    Task<List<Message>> GetConversationAsync(string ticketId);

    /// <summary>
    /// Get conversation history for a specific phase
    /// </summary>
    Task<List<Message>> GetPhaseConversationAsync(string ticketId, string phase);

    /// <summary>
    /// Clear conversation history for a ticket
    /// </summary>
    Task ClearConversationAsync(string ticketId);

    /// <summary>
    /// Get the last response from a specific phase
    /// </summary>
    Task<string?> GetLastResponseAsync(string ticketId, string phase);
}
