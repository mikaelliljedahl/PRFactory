using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing tenant configuration settings.
/// This service encapsulates business logic and coordinates between repositories to manage tenant configuration.
/// </summary>
public class TenantConfigurationService : ITenantConfigurationService
{
    private readonly ILogger<TenantConfigurationService> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;

    public TenantConfigurationService(
        ILogger<TenantConfigurationService> logger,
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc/>
    public async Task<TenantConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting configuration for current tenant");

        var tenantId = await _currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID cannot be determined. User may not be authenticated.");
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {tenantId} not found");
        }

        _logger.LogDebug("Retrieved configuration for tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);

        return MapToDto(tenant.Configuration);
    }

    /// <inheritdoc/>
    public async Task UpdateConfigurationAsync(TenantConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        _logger.LogInformation("Updating configuration for current tenant");

        var tenantId = await _currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID cannot be determined. User may not be authenticated.");
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {tenantId} not found");
        }

        // Validate configuration values
        ValidateConfiguration(dto);

        // Create TenantConfiguration from DTO
        var configuration = MapFromDto(dto);

        // Update tenant configuration
        tenant.UpdateConfiguration(configuration);

        // Save changes
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        _logger.LogInformation(
            "Updated configuration for tenant {TenantId} ({TenantName})",
            tenant.Id,
            tenant.Name);
    }

    /// <summary>
    /// Validates the configuration DTO values
    /// </summary>
    private void ValidateConfiguration(TenantConfigurationDto dto)
    {
        var errors = new List<string>();

        // Validate MaxRetries
        if (dto.MaxRetries is < 1 or > 10)
        {
            errors.Add("MaxRetries must be between 1 and 10");
        }

        // Validate ApiTimeoutSeconds
        if (dto.ApiTimeoutSeconds is < 30 or > 600)
        {
            errors.Add("ApiTimeoutSeconds must be between 30 and 600");
        }

        // Validate MaxTokensPerRequest
        if (dto.MaxTokensPerRequest is < 1000 or > 200000)
        {
            errors.Add("MaxTokensPerRequest must be between 1,000 and 200,000");
        }

        // Validate MaxCodeReviewIterations
        if (dto.MaxCodeReviewIterations is < 1 or > 10)
        {
            errors.Add("MaxCodeReviewIterations must be between 1 and 10");
        }

        // Validate ClaudeModel
        if (string.IsNullOrWhiteSpace(dto.ClaudeModel))
        {
            errors.Add("ClaudeModel cannot be empty");
        }
        else if (dto.ClaudeModel.Length > 100)
        {
            errors.Add("ClaudeModel cannot exceed 100 characters");
        }

        if (errors.Count > 0)
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Configuration validation failed: {Errors}", errorMessage);
            throw new ArgumentException($"Configuration validation failed: {errorMessage}");
        }
    }

    /// <summary>
    /// Maps TenantConfiguration entity to DTO
    /// </summary>
    private static TenantConfigurationDto MapToDto(TenantConfiguration config)
    {
        return new TenantConfigurationDto
        {
            AutoImplementAfterPlanApproval = config.AutoImplementAfterPlanApproval,
            MaxRetries = config.MaxRetries,
            ClaudeModel = config.ClaudeModel,
            MaxTokensPerRequest = config.MaxTokensPerRequest,
            ApiTimeoutSeconds = config.ApiTimeoutSeconds,
            EnableVerboseLogging = config.EnableVerboseLogging,
            EnableCodeReview = config.EnableCodeReview,
            AllowedRepositories = config.AllowedRepositories,
            CustomPromptTemplates = config.CustomPromptTemplates,
            EnableAutoCodeReview = config.EnableAutoCodeReview,
            CodeReviewLlmProviderId = config.CodeReviewLlmProviderId,
            ImplementationLlmProviderId = config.ImplementationLlmProviderId,
            PlanningLlmProviderId = config.PlanningLlmProviderId,
            AnalysisLlmProviderId = config.AnalysisLlmProviderId,
            MaxCodeReviewIterations = config.MaxCodeReviewIterations,
            AutoApproveIfNoIssues = config.AutoApproveIfNoIssues,
            RequireHumanApprovalAfterReview = config.RequireHumanApprovalAfterReview
        };
    }

    /// <summary>
    /// Maps DTO to TenantConfiguration entity
    /// </summary>
    private static TenantConfiguration MapFromDto(TenantConfigurationDto dto)
    {
        return new TenantConfiguration
        {
            AutoImplementAfterPlanApproval = dto.AutoImplementAfterPlanApproval,
            MaxRetries = dto.MaxRetries,
            ClaudeModel = dto.ClaudeModel,
            MaxTokensPerRequest = dto.MaxTokensPerRequest,
            ApiTimeoutSeconds = dto.ApiTimeoutSeconds,
            EnableVerboseLogging = dto.EnableVerboseLogging,
            EnableCodeReview = dto.EnableCodeReview,
            AllowedRepositories = dto.AllowedRepositories,
            CustomPromptTemplates = dto.CustomPromptTemplates,
            EnableAutoCodeReview = dto.EnableAutoCodeReview,
            CodeReviewLlmProviderId = dto.CodeReviewLlmProviderId,
            ImplementationLlmProviderId = dto.ImplementationLlmProviderId,
            PlanningLlmProviderId = dto.PlanningLlmProviderId,
            AnalysisLlmProviderId = dto.AnalysisLlmProviderId,
            MaxCodeReviewIterations = dto.MaxCodeReviewIterations,
            AutoApproveIfNoIssues = dto.AutoApproveIfNoIssues,
            RequireHumanApprovalAfterReview = dto.RequireHumanApprovalAfterReview
        };
    }
}
