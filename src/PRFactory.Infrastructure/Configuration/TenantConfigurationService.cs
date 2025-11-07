using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Infrastructure.Persistence;

namespace PRFactory.Infrastructure.Configuration;

/// <summary>
/// Service for retrieving tenant configuration with in-memory caching (5-minute TTL)
/// </summary>
public class TenantConfigurationService : ITenantConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantConfigurationService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public TenantConfigurationService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<TenantConfigurationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TenantConfiguration?> GetConfigurationForTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query ticket to get TenantId
            var ticket = await _context.Tickets
                .AsNoTracking()
                .Where(t => t.Id == ticketId)
                .Select(t => new { t.TenantId })
                .FirstOrDefaultAsync(cancellationToken);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found when fetching configuration", ticketId);
                return null;
            }

            // Get configuration for the tenant
            return await GetConfigurationAsync(ticket.TenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for ticket {TicketId}", ticketId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TenantConfiguration?> GetConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant_config_{tenantId}";

        // Try to get from cache first
        if (_cache.TryGetValue<TenantConfiguration>(cacheKey, out var cachedConfig))
        {
            _logger.LogDebug("Configuration for tenant {TenantId} retrieved from cache", tenantId);
            return cachedConfig;
        }

        try
        {
            // Query tenant configuration from database
            var tenant = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Select(t => t.Configuration)
                .FirstOrDefaultAsync(cancellationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found when fetching configuration", tenantId);
                return null;
            }

            // Cache the configuration for 5 minutes
            _cache.Set(cacheKey, tenant, CacheDuration);
            _logger.LogDebug("Configuration for tenant {TenantId} cached for {Duration} minutes",
                tenantId, CacheDuration.TotalMinutes);

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for tenant {TenantId}", tenantId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> GetAutoImplementationEnabledAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationForTicketAsync(ticketId, cancellationToken);

            // Safe default: return false if configuration is null
            return config?.AutoImplementAfterPlanApproval ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auto-implementation status for ticket {TicketId}", ticketId);
            return false; // Safe default on error
        }
    }
}
