using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of agent configuration service.
/// Facade that manages which LLM provider each agent type uses.
/// Uses direct application service injection (Blazor Server architecture).
///
/// NOTE: This implementation uses TenantConfiguration to store agent-specific provider settings.
/// The domain model should be extended to include:
/// - AnalysisAgentProviderId
/// - PlanningAgentProviderId
/// - ImplementationAgentProviderId
/// - CodeReviewAgentProviderId
/// - MaxCodeReviewIterations
/// - AutoApproveIfNoIssues
/// - RequireHumanApprovalAfterReview
/// </summary>
public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly ILogger<AgentConfigurationService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantApplicationService _tenantApplicationService;
    private readonly ApplicationDbContext _dbContext;

    public AgentConfigurationService(
        ILogger<AgentConfigurationService> logger,
        ITenantContext tenantContext,
        ITenantApplicationService tenantApplicationService,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _tenantContext = tenantContext;
        _tenantApplicationService = tenantApplicationService;
        _dbContext = dbContext;
    }

    public async Task<AgentConfigurationDto> GetConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            var tenantId = await _tenantContext.GetCurrentTenantIdAsync(ct);
            return await GetConfigurationByTenantIdAsync(tenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching agent configuration for current tenant");
            throw;
        }
    }

    public async Task<AgentConfigurationDto> GetConfigurationByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantApplicationService.GetTenantByIdAsync(tenantId, ct);
            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant {tenantId} not found");
            }

            // Map from TenantConfiguration to AgentConfigurationDto
            // TODO: Once domain model is extended, map actual provider IDs
            return new AgentConfigurationDto
            {
                TenantId = tenantId,
                AnalysisAgentProviderId = null, // TODO: Map from TenantConfiguration
                PlanningAgentProviderId = null, // TODO: Map from TenantConfiguration
                ImplementationAgentProviderId = null, // TODO: Map from TenantConfiguration
                CodeReviewAgentProviderId = null, // TODO: Map from TenantConfiguration
                EnableCodeReview = tenant.Configuration.EnableCodeReview,
                MaxCodeReviewIterations = 3, // TODO: Map from TenantConfiguration
                AutoApproveIfNoIssues = false, // TODO: Map from TenantConfiguration
                RequireHumanApprovalAfterReview = true, // TODO: Map from TenantConfiguration
                UpdatedAt = tenant.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching agent configuration for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<LlmProviderSummaryDto>> GetAvailableProvidersAsync(CancellationToken ct = default)
    {
        try
        {
            var tenantId = await _tenantContext.GetCurrentTenantIdAsync(ct);
            return await GetAvailableProvidersByTenantIdAsync(tenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available providers for current tenant");
            throw;
        }
    }

    public async Task<List<LlmProviderSummaryDto>> GetAvailableProvidersByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var providers = await _dbContext.TenantLlmProviders
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .ToListAsync(ct);

            return providers.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available providers for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<AgentConfigurationDto> SaveConfigurationAsync(AgentConfigurationDto configuration, CancellationToken ct = default)
    {
        try
        {
            // Validate configuration first
            var (isValid, errors) = await ValidateConfigurationAsync(configuration, ct);
            if (!isValid)
            {
                throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
            }

            var tenant = await _tenantApplicationService.GetTenantByIdAsync(configuration.TenantId, ct);
            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant {configuration.TenantId} not found");
            }

            // Update TenantConfiguration
            // TODO: Once domain model is extended, save actual provider IDs
            var updatedConfiguration = tenant.Configuration;
            updatedConfiguration.EnableCodeReview = configuration.EnableCodeReview;
            // updatedConfiguration.AnalysisAgentProviderId = configuration.AnalysisAgentProviderId;
            // updatedConfiguration.PlanningAgentProviderId = configuration.PlanningAgentProviderId;
            // updatedConfiguration.ImplementationAgentProviderId = configuration.ImplementationAgentProviderId;
            // updatedConfiguration.CodeReviewAgentProviderId = configuration.CodeReviewAgentProviderId;
            // updatedConfiguration.MaxCodeReviewIterations = configuration.MaxCodeReviewIterations;
            // updatedConfiguration.AutoApproveIfNoIssues = configuration.AutoApproveIfNoIssues;
            // updatedConfiguration.RequireHumanApprovalAfterReview = configuration.RequireHumanApprovalAfterReview;

            await _tenantApplicationService.UpdateTenantConfigurationAsync(
                configuration.TenantId,
                updatedConfiguration,
                ct);

            _logger.LogInformation(
                "Updated agent configuration for tenant {TenantId}",
                configuration.TenantId);

            return await GetConfigurationByTenantIdAsync(configuration.TenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving agent configuration for tenant {TenantId}", configuration.TenantId);
            throw;
        }
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateConfigurationAsync(
        AgentConfigurationDto configuration,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        try
        {
            // Validate MaxCodeReviewIterations range
            if (configuration.MaxCodeReviewIterations < 1 || configuration.MaxCodeReviewIterations > 10)
            {
                errors.Add("Max code review iterations must be between 1 and 10");
            }

            // Get all active providers for the tenant
            var providers = await GetAvailableProvidersByTenantIdAsync(configuration.TenantId, ct);
            var activeProviderIds = providers.Where(p => p.IsActive).Select(p => p.Id).ToHashSet();

            // Validate each provider ID if specified
            if (configuration.AnalysisAgentProviderId.HasValue &&
                !activeProviderIds.Contains(configuration.AnalysisAgentProviderId.Value))
            {
                errors.Add("Analysis agent provider ID is invalid or inactive");
            }

            if (configuration.PlanningAgentProviderId.HasValue &&
                !activeProviderIds.Contains(configuration.PlanningAgentProviderId.Value))
            {
                errors.Add("Planning agent provider ID is invalid or inactive");
            }

            if (configuration.ImplementationAgentProviderId.HasValue &&
                !activeProviderIds.Contains(configuration.ImplementationAgentProviderId.Value))
            {
                errors.Add("Implementation agent provider ID is invalid or inactive");
            }

            if (configuration.CodeReviewAgentProviderId.HasValue &&
                !activeProviderIds.Contains(configuration.CodeReviewAgentProviderId.Value))
            {
                errors.Add("Code review agent provider ID is invalid or inactive");
            }

            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating agent configuration for tenant {TenantId}", configuration.TenantId);
            errors.Add("Validation error occurred");
            return (false, errors);
        }
    }

    /// <summary>
    /// Maps a TenantLlmProvider entity to a LlmProviderSummaryDto
    /// </summary>
    private static LlmProviderSummaryDto MapToSummaryDto(TenantLlmProvider provider)
    {
        return new LlmProviderSummaryDto
        {
            Id = provider.Id,
            Name = provider.Name,
            DefaultModel = provider.DefaultModel,
            ProviderType = provider.ProviderType.ToString(),
            IsActive = provider.IsActive,
            IsDefault = provider.IsDefault
        };
    }
}
