using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for Tenant entity operations
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Gets a tenant by its unique identifier
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its name
    /// </summary>
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants
    /// </summary>
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active tenants
    /// </summary>
    Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant with its repositories eagerly loaded
    /// </summary>
    Task<Tenant?> GetByIdWithRepositoriesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant with its repositories and tickets eagerly loaded
    /// </summary>
    Task<Tenant?> GetByIdWithRepositoriesAndTicketsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenants by Jira URL
    /// </summary>
    Task<List<Tenant>> GetByJiraUrlAsync(string jiraUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new tenant
    /// </summary>
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant (should rarely be used, consider deactivating instead)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant with the given name already exists
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active vs inactive tenants
    /// </summary>
    Task<(int Active, int Inactive)> GetActiveInactiveCountsAsync(CancellationToken cancellationToken = default);
}
