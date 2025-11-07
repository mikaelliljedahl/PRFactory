using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Ticket entity operations.
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(
        ApplicationDbContext context,
        ILogger<TicketRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Ticket?> GetByTicketKeyAsync(string ticketKey, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.TicketKey == ticketKey, cancellationToken);
    }

    public async Task<List<Ticket>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetByRepositoryIdAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Tenant)
            .Where(t => t.RepositoryId == repositoryId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetByStateAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .Where(t => t.State == state)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetByStatesAsync(IEnumerable<WorkflowState> states, CancellationToken cancellationToken = default)
    {
        var statesList = states.ToList();
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .Where(t => statesList.Contains(t.State))
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetStaleAwaitingAnswersAsync(TimeSpan threshold, CancellationToken cancellationToken = default)
    {
        var thresholdDate = DateTime.UtcNow.Subtract(threshold);

        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .Where(t => t.State == WorkflowState.AwaitingAnswers
                && t.UpdatedAt != null
                && t.UpdatedAt < thresholdDate)
            .OrderBy(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetRetryableFailedTicketsAsync(int maxRetries, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .Where(t => (t.State == WorkflowState.Failed || t.State == WorkflowState.ImplementationFailed)
                && t.RetryCount < maxRetries)
            .OrderBy(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetActiveTicketsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var terminalStates = new[]
        {
            WorkflowState.Completed,
            WorkflowState.Cancelled,
            WorkflowState.Failed
        };

        return await _context.Tickets
            .Include(t => t.Repository)
            .Where(t => t.TenantId == tenantId && !terminalStates.Contains(t.State))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetByIdWithEventsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .Include(t => t.Events)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<Ticket>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(t => t.Repository)
            .Include(t => t.Tenant)
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket> AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created ticket {TicketId} ({TicketKey}) in state {State}",
            ticket.Id, ticket.TicketKey, ticket.State);

        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated ticket {TicketId} ({TicketKey}) - State: {State}",
            ticket.Id, ticket.TicketKey, ticket.State);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await GetByIdAsync(id, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Attempted to delete non-existent ticket {TicketId}", id);
            return;
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted ticket {TicketId} ({TicketKey})", ticket.Id, ticket.TicketKey);
    }

    public async Task<Dictionary<WorkflowState, int>> GetStateCountsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.Tickets
            .Where(t => t.TenantId == tenantId)
            .GroupBy(t => t.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.State, x => x.Count);
    }

    public async Task<bool> ExistsAsync(string ticketKey, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .AnyAsync(t => t.TicketKey == ticketKey, cancellationToken);
    }
}
