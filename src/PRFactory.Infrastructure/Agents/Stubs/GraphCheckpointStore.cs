using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Stubs;

/// <summary>
/// Stub implementation of Base.ICheckpointStore for graph checkpointing.
/// This should be replaced with a real implementation that persists to database.
/// </summary>
public class GraphCheckpointStore : ICheckpointStore
{
    private readonly ILogger<GraphCheckpointStore> _logger;
    private readonly Dictionary<string, Checkpoint> _checkpoints = new();
    private readonly object _lock = new();

    public GraphCheckpointStore(ILogger<GraphCheckpointStore> logger)
    {
        _logger = logger;
    }

    public Task SaveCheckpointAsync(
        Guid ticketId,
        string graphId,
        string checkpointId,
        Dictionary<string, object> state)
    {
        lock (_lock)
        {
            var key = GetKey(ticketId, graphId);
            _checkpoints[key] = new Checkpoint
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                GraphId = graphId,
                CheckpointId = checkpointId,
                State = new Dictionary<string, object>(state),
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Saved checkpoint {CheckpointId} for ticket {TicketId}, graph {GraphId}",
                checkpointId, ticketId, graphId);
        }

        return Task.CompletedTask;
    }

    public Task<Checkpoint?> LoadCheckpointAsync(Guid ticketId, string graphId)
    {
        lock (_lock)
        {
            var key = GetKey(ticketId, graphId);
            _checkpoints.TryGetValue(key, out var checkpoint);
            return Task.FromResult(checkpoint);
        }
    }

    public Task<List<Checkpoint>> GetCheckpointHistoryAsync(Guid ticketId, string graphId)
    {
        // For stub implementation, just return the latest checkpoint if it exists
        lock (_lock)
        {
            var key = GetKey(ticketId, graphId);
            var result = new List<Checkpoint>();
            if (_checkpoints.TryGetValue(key, out var checkpoint))
            {
                result.Add(checkpoint);
            }
            return Task.FromResult(result);
        }
    }

    private static string GetKey(Guid ticketId, string graphId)
    {
        return $"{ticketId}:{graphId}";
    }
}
