using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing agent-LLM provider configuration.
/// Facade for Blazor components to configure which LLM provider each agent type uses.
/// </summary>
public interface IAgentConfigurationService
{
    /// <summary>
    /// Gets the current agent configuration for the current tenant
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current agent configuration</returns>
    Task<AgentConfigurationDto> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current agent configuration for a specific tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current agent configuration</returns>
    Task<AgentConfigurationDto> GetConfigurationByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets all available LLM providers for the current tenant
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available LLM providers</returns>
    Task<List<LlmProviderSummaryDto>> GetAvailableProvidersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all available LLM providers for a specific tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available LLM providers</returns>
    Task<List<LlmProviderSummaryDto>> GetAvailableProvidersByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Saves agent configuration for the current tenant
    /// </summary>
    /// <param name="configuration">Configuration to save</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated configuration</returns>
    Task<AgentConfigurationDto> SaveConfigurationAsync(AgentConfigurationDto configuration, CancellationToken ct = default);

    /// <summary>
    /// Validates that all provider IDs exist and are active
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with error messages if invalid</returns>
    Task<(bool IsValid, List<string> Errors)> ValidateConfigurationAsync(AgentConfigurationDto configuration, CancellationToken ct = default);
}
