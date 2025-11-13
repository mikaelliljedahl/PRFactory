using PRFactory.Core.Application.DTOs;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for managing tenant LLM provider configurations
/// </summary>
public interface ITenantLlmProviderService
{
    /// <summary>
    /// Gets all providers for the current tenant
    /// </summary>
    Task<List<TenantLlmProviderDto>> GetProvidersForTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific provider by ID (with tenant isolation)
    /// </summary>
    Task<TenantLlmProviderDto?> GetProviderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new API key-based provider
    /// </summary>
    Task<TenantLlmProviderDto> CreateApiKeyProviderAsync(CreateApiKeyProviderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new OAuth-based provider (Native Anthropic)
    /// </summary>
    Task<TenantLlmProviderDto> CreateOAuthProviderAsync(CreateOAuthProviderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing provider configuration
    /// </summary>
    Task<TenantLlmProviderDto> UpdateProviderAsync(Guid id, UpdateProviderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a provider configuration
    /// </summary>
    Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a provider as the default for the tenant (clears other defaults)
    /// </summary>
    Task<TenantLlmProviderDto> SetDefaultProviderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connection to an LLM provider
    /// </summary>
    Task<ConnectionTestResult> TestProviderConnectionAsync(Guid id, CancellationToken cancellationToken = default);
}
