using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Repository entity operations.
/// </summary>
public class RepositoryRepository : IRepositoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RepositoryRepository> _logger;

    public RepositoryRepository(
        ApplicationDbContext context,
        ILogger<RepositoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Repository>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Repository?> GetByCloneUrlAsync(string cloneUrl, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.CloneUrl == cloneUrl, cancellationToken);
    }

    public async Task<Repository?> GetByNameAndTenantAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.Name == name && r.TenantId == tenantId, cancellationToken);
    }

    public async Task<List<Repository>> GetByGitPlatformAsync(string gitPlatform, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Where(r => r.GitPlatform == gitPlatform)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Repository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Repository>> GetStaleRepositoriesAsync(DateTime thresholdDate, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Where(r => r.LastAccessedAt == null || r.LastAccessedAt < thresholdDate)
            .OrderBy(r => r.LastAccessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Repository?> GetByIdWithTicketsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.Tenant)
            .Include(r => r.Tickets)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _context.Repositories.Add(repository);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created repository {RepositoryId} ({RepositoryName})",
            repository.Id, repository.Name);

        return repository;
    }

    public async Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _context.Repositories.Update(repository);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated repository {RepositoryId} ({RepositoryName})",
            repository.Id, repository.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repository = await GetByIdAsync(id, cancellationToken);
        if (repository == null)
        {
            _logger.LogWarning("Attempted to delete non-existent repository {RepositoryId}", id);
            return;
        }

        _context.Repositories.Remove(repository);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted repository {RepositoryId} ({RepositoryName})",
            repository.Id, repository.Name);
    }

    public async Task<bool> ExistsAsync(string cloneUrl, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .AnyAsync(r => r.CloneUrl == cloneUrl, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetPlatformCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .GroupBy(r => r.GitPlatform)
            .Select(g => new { Platform = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Platform, x => x.Count, cancellationToken);
    }
}
