using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for plan revision snapshots
/// </summary>
public class PlanRevisionRepository : IPlanRevisionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanRevisionRepository> _logger;

    public PlanRevisionRepository(
        ApplicationDbContext context,
        ILogger<PlanRevisionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlanRevision?> GetByIdAsync(Guid revisionId)
    {
        _logger.LogDebug("Getting plan revision {RevisionId}", revisionId);

        return await _context.PlanRevisions
            .Include(r => r.Ticket)
            .Include(r => r.CreatedBy)
            .FirstOrDefaultAsync(r => r.Id == revisionId);
    }

    public async Task<List<PlanRevision>> GetByTicketIdAsync(Guid ticketId)
    {
        _logger.LogDebug("Getting all plan revisions for ticket {TicketId}", ticketId);

        return await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .OrderBy(r => r.RevisionNumber)
            .ToListAsync();
    }

    public async Task<PlanRevision?> GetLatestByTicketIdAsync(Guid ticketId)
    {
        _logger.LogDebug("Getting latest plan revision for ticket {TicketId}", ticketId);

        return await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .OrderByDescending(r => r.RevisionNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<PlanRevision?> GetByRevisionNumberAsync(Guid ticketId, int revisionNumber)
    {
        _logger.LogDebug("Getting plan revision #{RevisionNumber} for ticket {TicketId}",
            revisionNumber, ticketId);

        return await _context.PlanRevisions
            .FirstOrDefaultAsync(r => r.TicketId == ticketId && r.RevisionNumber == revisionNumber);
    }

    public async Task<int> GetNextRevisionNumberAsync(Guid ticketId)
    {
        _logger.LogDebug("Getting next revision number for ticket {TicketId}", ticketId);

        var maxRevision = await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .MaxAsync(r => (int?)r.RevisionNumber);

        var nextNumber = (maxRevision ?? 0) + 1;

        _logger.LogDebug("Next revision number for ticket {TicketId} is {RevisionNumber}",
            ticketId, nextNumber);

        return nextNumber;
    }

    public async Task CreateAsync(PlanRevision revision)
    {
        if (revision == null)
            throw new ArgumentNullException(nameof(revision));

        _logger.LogInformation(
            "Creating plan revision {RevisionId} (rev #{RevisionNumber}) for ticket {TicketId} with reason {Reason}",
            revision.Id, revision.RevisionNumber, revision.TicketId, revision.Reason);

        _context.PlanRevisions.Add(revision);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Successfully created plan revision {RevisionId} for ticket {TicketId}",
            revision.Id, revision.TicketId);
    }

    public async Task DeleteByTicketIdAsync(Guid ticketId)
    {
        _logger.LogInformation("Deleting all plan revisions for ticket {TicketId}", ticketId);

        var revisions = await _context.PlanRevisions
            .Where(r => r.TicketId == ticketId)
            .ToListAsync();

        if (revisions.Count == 0)
        {
            _logger.LogDebug("No plan revisions found for ticket {TicketId}", ticketId);
            return;
        }

        _context.PlanRevisions.RemoveRange(revisions);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted {Count} plan revisions for ticket {TicketId}",
            revisions.Count, ticketId);
    }
}
