using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Web service facade for tenant management.
/// Converts between domain entities and DTOs for Blazor components.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets all tenants as DTOs
    /// </summary>
    Task<List<TenantDto>> GetAllTenantsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets active tenants only
    /// </summary>
    Task<List<TenantDto>> GetActiveTenantsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant by ID as DTO
    /// </summary>
    Task<TenantDto?> GetTenantByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant with full details (repositories, tickets, etc.)
    /// </summary>
    Task<TenantDto?> GetTenantWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    Task<TenantDto> UpdateTenantAsync(UpdateTenantRequest request, CancellationToken ct = default);

    /// <summary>
    /// Activates a tenant
    /// </summary>
    Task ActivateTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a tenant
    /// </summary>
    Task DeactivateTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes a tenant
    /// </summary>
    Task DeleteTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if tenant name is available
    /// </summary>
    Task<bool> IsTenantNameAvailableAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets tenant statistics
    /// </summary>
    Task<(int Active, int Inactive)> GetTenantStatsAsync(CancellationToken ct = default);
}
