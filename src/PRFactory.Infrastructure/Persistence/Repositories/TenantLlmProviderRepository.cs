using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TenantLlmProvider entity operations.
/// Provides multi-tenant isolated access to LLM provider configurations.
/// </summary>
public class TenantLlmProviderRepository(
    ApplicationDbContext context,
    ILogger<TenantLlmProviderRepository> logger) : ITenantLlmProviderRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<TenantLlmProviderRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TenantLlmProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantLlmProviders
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<TenantLlmProvider>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantLlmProviders
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantLlmProvider?> GetDefaultProviderAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantLlmProviders
            .Where(p => p.TenantId == tenantId && p.IsDefault && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TenantLlmProvider>> GetActiveProvidersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantLlmProviders
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TenantLlmProvider provider, CancellationToken cancellationToken = default)
    {
        _context.TenantLlmProviders.Add(provider);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created LLM provider {ProviderId} ({ProviderName}) for tenant {TenantId} - Type: {ProviderType}, Default: {IsDefault}",
            provider.Id, provider.Name, provider.TenantId, provider.ProviderType, provider.IsDefault);
    }

    public async Task UpdateAsync(TenantLlmProvider provider, CancellationToken cancellationToken = default)
    {
        _context.TenantLlmProviders.Update(provider);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated LLM provider {ProviderId} ({ProviderName}) - Active: {IsActive}, Default: {IsDefault}",
            provider.Id, provider.Name, provider.IsActive, provider.IsDefault);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await GetByIdAsync(id, cancellationToken);
        if (provider == null)
        {
            _logger.LogWarning("Attempted to delete non-existent LLM provider {ProviderId}", id);
            return;
        }

        _context.TenantLlmProviders.Remove(provider);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Deleted LLM provider {ProviderId} ({ProviderName}) for tenant {TenantId}",
            provider.Id, provider.Name, provider.TenantId);
    }
}
