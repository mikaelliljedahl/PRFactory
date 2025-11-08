using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for Checkpoint operations.
/// Manages checkpoint storage and retrieval for workflow graph execution.
/// </summary>
public interface ICheckpointRepository
{
    /// <summary>
    /// Saves a checkpoint to the database
    /// </summary>
    /// <param name="checkpoint">The checkpoint to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved checkpoint</returns>
    Task<Checkpoint> SaveCheckpointAsync(Checkpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest active checkpoint for a specific ticket and graph
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="graphId">The graph ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest checkpoint or null if none exists</returns>
    Task<Checkpoint?> GetLatestCheckpointAsync(Guid ticketId, string graphId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a checkpoint by its unique ID
    /// </summary>
    /// <param name="id">The checkpoint ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The checkpoint or null if not found</returns>
    Task<Checkpoint?> GetCheckpointByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all checkpoints for a specific ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of checkpoints ordered by creation date descending</returns>
    Task<List<Checkpoint>> GetCheckpointsByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets checkpoint history for a specific ticket and graph
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="graphId">The graph ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of checkpoints ordered by creation date descending</returns>
    Task<List<Checkpoint>> GetCheckpointHistoryAsync(Guid ticketId, string graphId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a checkpoint as resumed
    /// </summary>
    /// <param name="id">The checkpoint ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsResumedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a checkpoint (marks as deleted, doesn't physically remove)
    /// </summary>
    /// <param name="id">The checkpoint ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteCheckpointAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires old checkpoints that are older than the specified age
    /// </summary>
    /// <param name="olderThan">Expire checkpoints older than this timespan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of checkpoints expired</returns>
    Task<int> ExpireOldCheckpointsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active checkpoints for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active checkpoints</returns>
    Task<List<Checkpoint>> GetActiveCheckpointsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
