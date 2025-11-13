using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for TenantLlmProvider entity operations
/// </summary>
public interface ITenantLlmProviderRepository
{
    /// <summary>
    /// Gets a provider by its unique identifier
    /// </summary>
    Task<TenantLlmProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all providers for a specific tenant
    /// </summary>
    Task<List<TenantLlmProvider>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default provider for a tenant
    /// </summary>
    Task<TenantLlmProvider?> GetDefaultProviderAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active providers for a tenant
    /// </summary>
    Task<List<TenantLlmProvider>> GetActiveProvidersAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new provider configuration
    /// </summary>
    Task AddAsync(TenantLlmProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing provider configuration
    /// </summary>
    Task UpdateAsync(TenantLlmProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a provider configuration
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
