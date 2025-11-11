using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for tenant management.
/// Contains business logic for tenant operations.
/// </summary>
public interface ITenantApplicationService
{
    /// <summary>
    /// Gets all tenants
    /// </summary>
    Task<List<Tenant>> GetAllTenantsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets active tenants only
    /// </summary>
    Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant by ID
    /// </summary>
    Task<Tenant?> GetTenantByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant by ID with repositories loaded
    /// </summary>
    Task<Tenant?> GetTenantWithRepositoriesAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant by ID with repositories and tickets loaded
    /// </summary>
    Task<Tenant?> GetTenantWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant by name
    /// </summary>
    Task<Tenant?> GetTenantByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="name">Tenant name</param>
    /// <param name="ticketPlatformUrl">Ticket platform instance URL (e.g., Jira, Azure DevOps)</param>
    /// <param name="ticketPlatformApiToken">Ticket platform API token (will be encrypted)</param>
    /// <param name="claudeApiKey">Claude API key (will be encrypted)</param>
    /// <param name="ticketPlatform">Ticket platform type (e.g., "Jira", "AzureDevOps"). Defaults to "Jira"</param>
    /// <param name="configuration">Optional tenant configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created tenant</returns>
    Task<Tenant> CreateTenantAsync(
        string name,
        string ticketPlatformUrl,
        string ticketPlatformApiToken,
        string claudeApiKey,
        string ticketPlatform = "Jira",
        TenantConfiguration? configuration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a tenant
    /// </summary>
    Task<Tenant> UpdateTenantAsync(
        UpdateTenantRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates tenant configuration only
    /// </summary>
    Task UpdateTenantConfigurationAsync(
        Guid id,
        TenantConfiguration configuration,
        CancellationToken ct = default);

    /// <summary>
    /// Activates a tenant
    /// </summary>
    Task ActivateTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a tenant
    /// </summary>
    Task DeactivateTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes a tenant (use with caution)
    /// </summary>
    Task DeleteTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a tenant name is available
    /// </summary>
    Task<bool> IsTenantNameAvailableAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets tenant statistics
    /// </summary>
    Task<(int Active, int Inactive)> GetTenantStatsAsync(CancellationToken ct = default);
}
