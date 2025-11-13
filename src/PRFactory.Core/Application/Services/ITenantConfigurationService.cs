using PRFactory.Core.Application.DTOs;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing tenant configuration settings.
/// This service encapsulates business logic for tenant configuration operations.
/// </summary>
public interface ITenantConfigurationService
{
    /// <summary>
    /// Gets the configuration for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant configuration DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown if current tenant cannot be determined or tenant not found</exception>
    Task<TenantConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration for the current tenant
    /// </summary>
    /// <param name="dto">The updated configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentNullException">Thrown if dto is null</exception>
    /// <exception cref="ArgumentException">Thrown if configuration values are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown if current tenant cannot be determined or tenant not found</exception>
    Task UpdateConfigurationAsync(TenantConfigurationDto dto, CancellationToken cancellationToken = default);
}
