using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of tenant web service.
/// Facade that converts between domain entities and DTOs for Blazor components.
/// Uses direct application service injection (Blazor Server architecture).
/// </summary>
public class TenantService : ITenantService
{
    private readonly ILogger<TenantService> _logger;
    private readonly ITenantApplicationService _tenantApplicationService;

    public TenantService(
        ILogger<TenantService> logger,
        ITenantApplicationService tenantApplicationService)
    {
        _logger = logger;
        _tenantApplicationService = tenantApplicationService;
    }

    public async Task<List<TenantDto>> GetAllTenantsAsync(CancellationToken ct = default)
    {
        try
        {
            var tenants = await _tenantApplicationService.GetAllTenantsAsync(ct);
            return tenants.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all tenants");
            throw;
        }
    }

    public async Task<List<TenantDto>> GetActiveTenantsAsync(CancellationToken ct = default)
    {
        try
        {
            var tenants = await _tenantApplicationService.GetActiveTenantsAsync(ct);
            return tenants.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active tenants");
            throw;
        }
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantApplicationService.GetTenantByIdAsync(id, ct);
            return tenant != null ? MapToDto(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant {TenantId}", id);
            throw;
        }
    }

    public async Task<TenantDto?> GetTenantWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantApplicationService.GetTenantWithDetailsAsync(id, ct);
            return tenant != null ? MapToDtoWithDetails(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant details {TenantId}", id);
            throw;
        }
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        try
        {
            var configuration = new TenantConfiguration
            {
                AutoImplementAfterPlanApproval = request.AutoImplementAfterPlanApproval,
                MaxRetries = request.MaxRetries,
                ClaudeModel = request.ClaudeModel,
                MaxTokensPerRequest = request.MaxTokensPerRequest,
                ApiTimeoutSeconds = request.ApiTimeoutSeconds,
                EnableVerboseLogging = request.EnableVerboseLogging,
                EnableCodeReview = request.EnableCodeReview
            };

            var tenant = await _tenantApplicationService.CreateTenantAsync(
                request.Name,
                request.TicketPlatformUrl,
                request.TicketPlatformApiToken!,  // Validated by [Required] attribute
                request.ClaudeApiKey!,  // Validated by [Required] attribute
                request.TicketPlatform,
                configuration,
                ct);

            _logger.LogInformation("Created tenant {TenantId} with name {TenantName}", tenant.Id, tenant.Name);

            return MapToDto(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant {TenantName}", request.Name);
            throw;
        }
    }

    public async Task<TenantDto> UpdateTenantAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        try
        {
            var configuration = new TenantConfiguration
            {
                AutoImplementAfterPlanApproval = request.AutoImplementAfterPlanApproval,
                MaxRetries = request.MaxRetries,
                ClaudeModel = request.ClaudeModel,
                MaxTokensPerRequest = request.MaxTokensPerRequest,
                ApiTimeoutSeconds = request.ApiTimeoutSeconds,
                EnableVerboseLogging = request.EnableVerboseLogging,
                EnableCodeReview = request.EnableCodeReview,
                AllowedRepositories = request.AllowedRepositories
            };

            var tenant = await _tenantApplicationService.UpdateTenantAsync(
                request.Id,
                request.Name,
                request.TicketPlatformUrl,
                request.TicketPlatformApiToken,
                request.ClaudeApiKey,
                request.TicketPlatform,
                configuration,
                ct);

            _logger.LogInformation("Updated tenant {TenantId}", request.Id);

            return MapToDto(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", request.Id);
            throw;
        }
    }

    public async Task ActivateTenantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _tenantApplicationService.ActivateTenantAsync(id, ct);
            _logger.LogInformation("Activated tenant {TenantId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant {TenantId}", id);
            throw;
        }
    }

    public async Task DeactivateTenantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _tenantApplicationService.DeactivateTenantAsync(id, ct);
            _logger.LogInformation("Deactivated tenant {TenantId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", id);
            throw;
        }
    }

    public async Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _tenantApplicationService.DeleteTenantAsync(id, ct);
            _logger.LogInformation("Deleted tenant {TenantId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
            throw;
        }
    }

    public async Task<bool> IsTenantNameAvailableAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        try
        {
            return await _tenantApplicationService.IsTenantNameAvailableAsync(name, excludeId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking tenant name availability for {TenantName}", name);
            throw;
        }
    }

    public async Task<(int Active, int Inactive)> GetTenantStatsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _tenantApplicationService.GetTenantStatsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant statistics");
            throw;
        }
    }

    /// <summary>
    /// Maps a Tenant entity to a TenantDto
    /// </summary>
    private TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            TicketPlatformUrl = tenant.JiraUrl,  // Map from domain entity's JiraUrl to DTO's TicketPlatformUrl
            TicketPlatform = tenant.TicketPlatform ?? "Jira",  // Default to "Jira" if not set
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            AutoImplementAfterPlanApproval = tenant.Configuration.AutoImplementAfterPlanApproval,
            MaxRetries = tenant.Configuration.MaxRetries,
            ClaudeModel = tenant.Configuration.ClaudeModel,
            MaxTokensPerRequest = tenant.Configuration.MaxTokensPerRequest,
            EnableCodeReview = tenant.Configuration.EnableCodeReview,
            HasTicketPlatformApiToken = !string.IsNullOrEmpty(tenant.JiraApiToken),  // Map from domain entity's JiraApiToken
            HasClaudeApiKey = !string.IsNullOrEmpty(tenant.ClaudeApiKey),
            RepositoryCount = 0,
            TicketCount = 0
        };
    }

    /// <summary>
    /// Maps a Tenant entity with loaded relationships to a TenantDto
    /// </summary>
    private TenantDto MapToDtoWithDetails(Tenant tenant)
    {
        var dto = MapToDto(tenant);
        dto.RepositoryCount = tenant.Repositories?.Count ?? 0;
        dto.TicketCount = tenant.Tickets?.Count ?? 0;
        return dto;
    }
}
