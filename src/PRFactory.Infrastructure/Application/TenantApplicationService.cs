using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Implementation of tenant application service.
/// Contains business logic for tenant operations.
/// </summary>
public class TenantApplicationService : ITenantApplicationService
{
    private readonly ILogger<TenantApplicationService> _logger;
    private readonly ITenantRepository _tenantRepository;

    public TenantApplicationService(
        ILogger<TenantApplicationService> logger,
        ITenantRepository tenantRepository)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
    }

    public async Task<List<Tenant>> GetAllTenantsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching all tenants");
        return await _tenantRepository.GetAllAsync(ct);
    }

    public async Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching active tenants");
        return await _tenantRepository.GetActiveTenantsAsync(ct);
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching tenant {TenantId}", id);
        return await _tenantRepository.GetByIdAsync(id, ct);
    }

    public async Task<Tenant?> GetTenantWithRepositoriesAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching tenant {TenantId} with repositories", id);
        return await _tenantRepository.GetByIdWithRepositoriesAsync(id, ct);
    }

    public async Task<Tenant?> GetTenantWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching tenant {TenantId} with full details", id);
        return await _tenantRepository.GetByIdWithRepositoriesAndTicketsAsync(id, ct);
    }

    public async Task<Tenant?> GetTenantByNameAsync(string name, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching tenant by name: {TenantName}", name);
        return await _tenantRepository.GetByNameAsync(name, ct);
    }

    public async Task<Tenant> CreateTenantAsync(
        string name,
        string ticketPlatformUrl,
        string ticketPlatformApiToken,
        string claudeApiKey,
        string ticketPlatform = "Jira",
        TenantConfiguration? configuration = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new tenant: {TenantName} with platform: {Platform}", name, ticketPlatform);

        // Check if name is already taken
        var existingTenant = await _tenantRepository.GetByNameAsync(name, ct);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"A tenant with the name '{name}' already exists");
        }

        // Create tenant entity
        var tenant = Tenant.Create(name, ticketPlatformUrl, ticketPlatformApiToken, claudeApiKey, ticketPlatform);

        // Update configuration if provided
        if (configuration != null)
        {
            tenant.UpdateConfiguration(configuration);
        }

        // Save to database
        var createdTenant = await _tenantRepository.AddAsync(tenant, ct);

        _logger.LogInformation("Successfully created tenant {TenantId} with name {TenantName} and platform {Platform}",
            createdTenant.Id, createdTenant.Name, createdTenant.TicketPlatform);

        return createdTenant;
    }

    public async Task<Tenant> UpdateTenantAsync(
        UpdateTenantRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Updating tenant {TenantId}", request.Id);

        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{request.Id}' not found");
        }

        // Check if name is changing and if new name is available
        if (tenant.Name != request.Name)
        {
            var existingTenant = await _tenantRepository.GetByNameAsync(request.Name, ct);
            if (existingTenant != null && existingTenant.Id != request.Id)
            {
                throw new InvalidOperationException($"A tenant with the name '{request.Name}' already exists");
            }
        }

        // Update platform settings if provided
        if (!string.IsNullOrEmpty(request.TicketPlatform) || !string.IsNullOrEmpty(request.TicketPlatformUrl))
        {
            tenant.UpdatePlatformSettings(request.TicketPlatform, request.TicketPlatformUrl);
        }

        // Update credentials if provided
        if (!string.IsNullOrEmpty(request.TicketPlatformApiToken) || !string.IsNullOrEmpty(request.ClaudeApiKey))
        {
            tenant.UpdateCredentials(request.TicketPlatformApiToken, request.ClaudeApiKey);
        }

        // Update configuration if provided
        if (request.Configuration != null)
        {
            tenant.UpdateConfiguration(request.Configuration);
        }

        // Save changes
        await _tenantRepository.UpdateAsync(tenant, ct);

        _logger.LogInformation("Successfully updated tenant {TenantId}", request.Id);

        return tenant;
    }

    public async Task UpdateTenantConfigurationAsync(
        Guid id,
        TenantConfiguration configuration,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Updating configuration for tenant {TenantId}", id);

        var tenant = await _tenantRepository.GetByIdAsync(id, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");
        }

        tenant.UpdateConfiguration(configuration);
        await _tenantRepository.UpdateAsync(tenant, ct);

        _logger.LogInformation("Successfully updated configuration for tenant {TenantId}", id);
    }

    public async Task ActivateTenantAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Activating tenant {TenantId}", id);

        var tenant = await _tenantRepository.GetByIdAsync(id, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");
        }

        tenant.Activate();
        await _tenantRepository.UpdateAsync(tenant, ct);

        _logger.LogInformation("Successfully activated tenant {TenantId}", id);
    }

    public async Task DeactivateTenantAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deactivating tenant {TenantId}", id);

        var tenant = await _tenantRepository.GetByIdAsync(id, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");
        }

        tenant.Deactivate();
        await _tenantRepository.UpdateAsync(tenant, ct);

        _logger.LogInformation("Successfully deactivated tenant {TenantId}", id);
    }

    public async Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogWarning("Deleting tenant {TenantId}", id);

        var tenant = await _tenantRepository.GetByIdAsync(id, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");
        }

        await _tenantRepository.DeleteAsync(id, ct);

        _logger.LogWarning("Successfully deleted tenant {TenantId}", id);
    }

    public async Task<bool> IsTenantNameAvailableAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var existingTenant = await _tenantRepository.GetByNameAsync(name, ct);

        if (existingTenant == null)
        {
            return true;
        }

        // If we're excluding a specific ID (for updates), check if it's the same tenant
        if (excludeId.HasValue && existingTenant.Id == excludeId.Value)
        {
            return true;
        }

        return false;
    }

    public async Task<(int Active, int Inactive)> GetTenantStatsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching tenant statistics");
        return await _tenantRepository.GetActiveInactiveCountsAsync(ct);
    }
}
