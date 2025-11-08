using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TicketUpdate entity operations.
/// </summary>
public class TicketUpdateRepository : ITicketUpdateRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TicketUpdateRepository> _logger;

    public TicketUpdateRepository(
        ApplicationDbContext context,
        ILogger<TicketUpdateRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TicketUpdate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Include(tu => tu.Ticket)
            .FirstOrDefaultAsync(tu => tu.Id == id, cancellationToken);
    }

    public async Task<List<TicketUpdate>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId)
            .OrderByDescending(tu => tu.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketUpdate?> GetLatestDraftByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId && tu.IsDraft)
            .OrderByDescending(tu => tu.Version)
            .ThenByDescending(tu => tu.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TicketUpdate?> GetLatestApprovedByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId && tu.IsApproved)
            .OrderByDescending(tu => tu.Version)
            .ThenByDescending(tu => tu.ApprovedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TicketUpdate>> GetVersionHistoryAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId)
            .OrderBy(tu => tu.Version)
            .ThenBy(tu => tu.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TicketUpdate>> GetPendingPostsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Include(tu => tu.Ticket)
            .Where(tu => tu.IsApproved && tu.PostedAt == null)
            .OrderBy(tu => tu.ApprovedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TicketUpdate>> GetDraftsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Include(tu => tu.Ticket)
            .Where(tu => tu.IsDraft)
            .OrderByDescending(tu => tu.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TicketUpdate>> GetRejectedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Include(tu => tu.Ticket)
            .Where(tu => tu.RejectionReason != null)
            .OrderByDescending(tu => tu.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TicketUpdate>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .Include(tu => tu.Ticket)
            .Where(tu => tu.GeneratedAt >= startDate && tu.GeneratedAt <= endDate)
            .OrderBy(tu => tu.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketUpdate> CreateAsync(TicketUpdate ticketUpdate, CancellationToken cancellationToken = default)
    {
        _context.TicketUpdates.Add(ticketUpdate);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created ticket update {TicketUpdateId} for ticket {TicketId}, version {Version}, draft: {IsDraft}",
            ticketUpdate.Id, ticketUpdate.TicketId, ticketUpdate.Version, ticketUpdate.IsDraft);

        return ticketUpdate;
    }

    public async Task UpdateAsync(TicketUpdate ticketUpdate, CancellationToken cancellationToken = default)
    {
        _context.TicketUpdates.Update(ticketUpdate);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated ticket update {TicketUpdateId} for ticket {TicketId}, version {Version}, approved: {IsApproved}",
            ticketUpdate.Id, ticketUpdate.TicketId, ticketUpdate.Version, ticketUpdate.IsApproved);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticketUpdate = await GetByIdAsync(id, cancellationToken);
        if (ticketUpdate == null)
        {
            _logger.LogWarning("Attempted to delete non-existent ticket update {TicketUpdateId}", id);
            return;
        }

        _context.TicketUpdates.Remove(ticketUpdate);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Deleted ticket update {TicketUpdateId} for ticket {TicketId}",
            ticketUpdate.Id, ticketUpdate.TicketId);
    }

    public async Task DeleteByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticketUpdates = await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId)
            .ToListAsync(cancellationToken);

        if (ticketUpdates.Count == 0)
        {
            _logger.LogDebug("No ticket updates found for ticket {TicketId} to delete", ticketId);
            return;
        }

        _context.TicketUpdates.RemoveRange(ticketUpdates);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Deleted {Count} ticket updates for ticket {TicketId}",
            ticketUpdates.Count, ticketId);
    }

    public async Task<bool> HasApprovedUpdateAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.TicketUpdates
            .AnyAsync(tu => tu.TicketId == ticketId && tu.IsApproved, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var updates = await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId)
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<string, int>
        {
            ["Total"] = updates.Count,
            ["Draft"] = updates.Count(tu => tu.IsDraft),
            ["Approved"] = updates.Count(tu => tu.IsApproved),
            ["Posted"] = updates.Count(tu => tu.PostedAt.HasValue),
            ["Rejected"] = updates.Count(tu => tu.RejectionReason != null),
            ["PendingPost"] = updates.Count(tu => tu.IsApproved && !tu.PostedAt.HasValue)
        };

        return counts;
    }

    public async Task<int> GetLatestVersionNumberAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _context.TicketUpdates
            .Where(tu => tu.TicketId == ticketId)
            .MaxAsync(tu => (int?)tu.Version, cancellationToken);

        return maxVersion ?? 0;
    }
}
