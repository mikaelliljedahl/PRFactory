using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Stubs;

/// <summary>
/// Stub implementation of ICheckpointStore for build purposes.
/// This should be replaced with a real implementation that persists checkpoints to a database.
/// </summary>
public class CheckpointStore : ICheckpointStore
{
    private readonly ILogger<CheckpointStore> _logger;

    public CheckpointStore(ILogger<CheckpointStore> logger)
    {
        _logger = logger;
    }

    public Task<CheckpointData?> LoadCheckpointAsync(
        Guid checkpointId,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Using stub implementation of ICheckpointStore");
        return Task.FromResult<CheckpointData?>(null);
    }

    public Task<CheckpointData?> LoadLatestCheckpointAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<CheckpointData?>(null);
    }

    public Task SaveCheckpointAsync(
        CheckpointData checkpoint,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checkpoint saved at {SavedAt}", checkpoint.SavedAt);
        return Task.CompletedTask;
    }
}
