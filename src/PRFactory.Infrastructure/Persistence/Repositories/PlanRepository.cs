using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Plan entity operations.
/// </summary>
public class PlanRepository : IPlanRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanRepository> _logger;

    public PlanRepository(
        ApplicationDbContext context,
        ILogger<PlanRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Include(p => p.Versions.OrderByDescending(v => v.Version))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Plan?> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Include(p => p.Versions.OrderByDescending(v => v.Version))
            .FirstOrDefaultAsync(p => p.TicketId == ticketId, cancellationToken);
    }

    public async Task<List<PlanVersion>> GetVersionHistoryAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanVersions
            .Where(v => v.PlanId == planId)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanVersion?> GetVersionAsync(Guid planId, int version, CancellationToken cancellationToken = default)
    {
        return await _context.PlanVersions
            .FirstOrDefaultAsync(
                v => v.PlanId == planId && v.Version == version,
                cancellationToken);
    }

    public async Task<Plan> AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        _logger.LogInformation("Adding new plan {PlanId} for ticket {TicketId}", plan.Id, plan.TicketId);

        await _context.Plans.AddAsync(plan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return plan;
    }

    public async Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        _logger.LogInformation("Updating plan {PlanId} to version {Version}", plan.Id, plan.Version);

        // Check if entity is already tracked
        var entry = _context.Entry(plan);
        if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            _context.Plans.Update(plan);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        _logger.LogInformation("Deleting plan {PlanId}", plan.Id);

        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
