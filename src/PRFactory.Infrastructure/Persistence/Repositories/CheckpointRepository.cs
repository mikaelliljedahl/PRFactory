using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Checkpoint entity operations.
/// </summary>
public class CheckpointRepository : ICheckpointRepository
{
    private const string CheckpointForTicketLogMessageTemplate = "for ticket {TicketId}";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckpointRepository> _logger;

    public CheckpointRepository(
        ApplicationDbContext context,
        ILogger<CheckpointRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Checkpoint> SaveCheckpointAsync(Checkpoint checkpoint, CancellationToken cancellationToken = default)
    {
        // Check if we're updating an existing checkpoint or creating a new one
        var existingCheckpoint = await _context.Checkpoints
            .FirstOrDefaultAsync(
                c => c.TicketId == checkpoint.TicketId
                    && c.GraphId == checkpoint.GraphId
                    && c.Status == CheckpointStatus.Active,
                cancellationToken);

        if (existingCheckpoint != null)
        {
            // Mark existing checkpoint as deleted and create new one
            existingCheckpoint.MarkAsDeleted();

            _logger.LogDebug($"Marked old checkpoint {{CheckpointId}} as deleted {CheckpointForTicketLogMessageTemplate} in graph {{GraphId}}",
                existingCheckpoint.CheckpointId, checkpoint.TicketId, checkpoint.GraphId);
        }

        // Add new checkpoint
        _context.Checkpoints.Add(checkpoint);
        _logger.LogDebug($"Created checkpoint {{CheckpointId}} {CheckpointForTicketLogMessageTemplate} in graph {{GraphId}}",
            checkpoint.CheckpointId, checkpoint.TicketId, checkpoint.GraphId);

        await _context.SaveChangesAsync(cancellationToken);
        return checkpoint;
    }

    public async Task<Checkpoint?> GetLatestCheckpointAsync(
        Guid ticketId,
        string graphId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Checkpoints
            .Where(c => c.TicketId == ticketId
                && c.GraphId == graphId
                && c.Status == CheckpointStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Checkpoint?> GetCheckpointByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Checkpoints
            .Include(c => c.Ticket)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Checkpoint>> GetCheckpointsByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Checkpoints
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Checkpoint>> GetCheckpointHistoryAsync(
        Guid ticketId,
        string graphId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Checkpoints
            .Where(c => c.TicketId == ticketId && c.GraphId == graphId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsResumedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var checkpoint = await GetCheckpointByIdAsync(id, cancellationToken);
        if (checkpoint == null)
        {
            _logger.LogWarning("Attempted to mark non-existent checkpoint {CheckpointId} as resumed", id);
            return;
        }

        checkpoint.MarkAsResumed();
        _context.Checkpoints.Update(checkpoint);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug($"Marked checkpoint {{CheckpointId}} as resumed {CheckpointForTicketLogMessageTemplate}",
            checkpoint.CheckpointId, checkpoint.TicketId);
    }

    public async Task DeleteCheckpointAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var checkpoint = await GetCheckpointByIdAsync(id, cancellationToken);
        if (checkpoint == null)
        {
            _logger.LogWarning("Attempted to delete non-existent checkpoint {CheckpointId}", id);
            return;
        }

        checkpoint.MarkAsDeleted();
        _context.Checkpoints.Update(checkpoint);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug($"Marked checkpoint {{CheckpointId}} as deleted {CheckpointForTicketLogMessageTemplate}",
            checkpoint.CheckpointId, checkpoint.TicketId);
    }

    public async Task<int> ExpireOldCheckpointsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var expirationDate = DateTime.UtcNow.Subtract(olderThan);

        var checkpointsToExpire = await _context.Checkpoints
            .Where(c => c.Status == CheckpointStatus.Active
                && c.CreatedAt < expirationDate)
            .ToListAsync(cancellationToken);

        foreach (var checkpoint in checkpointsToExpire)
        {
            checkpoint.MarkAsExpired();
        }

        if (checkpointsToExpire.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Expired {Count} checkpoints older than {OlderThan}",
                checkpointsToExpire.Count, olderThan);
        }

        return checkpointsToExpire.Count;
    }

    public async Task<List<Checkpoint>> GetActiveCheckpointsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Checkpoints
            .Include(c => c.Ticket)
            .Where(c => c.TenantId == tenantId && c.Status == CheckpointStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
