using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Claude.Models;

namespace PRFactory.Infrastructure.Claude;

/// <summary>
/// In-memory implementation of conversation history repository
/// In production, this would be backed by a database
/// </summary>
public class ConversationHistoryRepository : IConversationHistoryRepository
{
    private readonly ILogger<ConversationHistoryRepository> _logger;

    // In-memory storage: ticketId -> list of conversation entries
    private static readonly Dictionary<string, List<ConversationEntry>> _conversations = new();
    private static readonly object _lock = new();

    public ConversationHistoryRepository(ILogger<ConversationHistoryRepository> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task AddMessageAsync(
        string ticketId,
        string phase,
        Message userMessage,
        string assistantResponse)
    {
        lock (_lock)
        {
            if (!_conversations.ContainsKey(ticketId))
            {
                _conversations[ticketId] = new List<ConversationEntry>();
            }

            _conversations[ticketId].Add(new ConversationEntry
            {
                Phase = phase,
                UserMessage = userMessage,
                AssistantResponse = assistantResponse,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation(
            "Added conversation entry for ticket {TicketId}, phase {Phase}",
            ticketId, phase);

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<List<Message>> GetConversationAsync(string ticketId)
    {
        List<Message> messages;
        lock (_lock)
        {
            if (!_conversations.ContainsKey(ticketId))
            {
                return new List<Message>();
            }

            messages = new List<Message>();
            foreach (var entry in _conversations[ticketId])
            {
                messages.Add(entry.UserMessage);
                messages.Add(new Message("assistant", entry.AssistantResponse));
            }
        }

        await Task.CompletedTask;
        return messages;
    }

    /// <inheritdoc/>
    public async Task<List<Message>> GetPhaseConversationAsync(string ticketId, string phase)
    {
        List<Message> messages;
        lock (_lock)
        {
            if (!_conversations.ContainsKey(ticketId))
            {
                return new List<Message>();
            }

            messages = new List<Message>();
            foreach (var entry in _conversations[ticketId].Where(e => e.Phase == phase))
            {
                messages.Add(entry.UserMessage);
                messages.Add(new Message("assistant", entry.AssistantResponse));
            }
        }

        await Task.CompletedTask;
        return messages;
    }

    /// <inheritdoc/>
    public async Task ClearConversationAsync(string ticketId)
    {
        lock (_lock)
        {
            _conversations.Remove(ticketId);
        }

        _logger.LogInformation("Cleared conversation history for ticket {TicketId}", ticketId);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<string?> GetLastResponseAsync(string ticketId, string phase)
    {
        string? response = null;
        lock (_lock)
        {
            if (_conversations.ContainsKey(ticketId))
            {
                var lastEntry = _conversations[ticketId]
                    .Where(e => e.Phase == phase)
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault();

                response = lastEntry?.AssistantResponse;
            }
        }

        await Task.CompletedTask;
        return response;
    }

    /// <summary>
    /// Internal class to store conversation entries
    /// </summary>
    private class ConversationEntry
    {
        public string Phase { get; set; } = string.Empty;
        public Message UserMessage { get; set; } = null!;
        public string AssistantResponse { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
