using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Tenant entity operations.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantRepository> _logger;

    public TenantRepository(
        ApplicationDbContext context,
        ILogger<TenantRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetByIdWithRepositoriesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.Repositories)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetByIdWithRepositoriesAndTicketsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.Repositories)
            .Include(t => t.Tickets)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<Tenant>> GetByJiraUrlAsync(string jiraUrl, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Where(t => t.JiraUrl == jiraUrl)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);

        return tenant;
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Attempted to delete non-existent tenant {TenantId}", id);
            return;
        }

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<(int Active, int Inactive)> GetActiveInactiveCountsAsync(CancellationToken cancellationToken = default)
    {
        var active = await _context.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var inactive = await _context.Tenants.CountAsync(t => !t.IsActive, cancellationToken);

        return (active, inactive);
    }
}
