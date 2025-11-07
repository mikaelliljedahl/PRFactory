using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Configuration;

/// <summary>
/// Service for retrieving tenant configuration with caching support
/// </summary>
public interface ITenantConfigurationService
{
    /// <summary>
    /// Gets tenant configuration for a specific ticket by querying the ticket's tenant
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant configuration, or null if not found</returns>
    Task<TenantConfiguration?> GetConfigurationForTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant configuration by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant configuration, or null if not found</returns>
    Task<TenantConfiguration?> GetConfigurationAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether auto-implementation is enabled for a specific ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if auto-implementation is enabled, false otherwise (defaults to false if configuration not found)</returns>
    Task<bool> GetAutoImplementationEnabledAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
