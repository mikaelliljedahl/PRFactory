using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using System.Security.Claims;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Service for accessing the current authenticated user context.
/// Replaces StubCurrentUserService with real implementation that reads from HTTP context claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CurrentUserService> _logger;

    // Claim types
    private const string PrFactoryUserIdClaim = "prfactory_user_id";
    private const string PrFactoryTenantIdClaim = "prfactory_tenant_id";

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("No authenticated user in HTTP context");
            return null;
        }

        // Try to get prfactory_user_id claim
        var userIdClaim = httpContext.User.FindFirst(PrFactoryUserIdClaim);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        // Fallback: Try to get user from external auth ID and return their ID
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Id;
    }

    /// <inheritdoc/>
    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("No authenticated user in HTTP context");
            return null;
        }

        // Try to get user by prfactory_user_id claim first (fastest)
        var userIdClaim = httpContext.User.FindFirst(PrFactoryUserIdClaim);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null)
                return user;
        }

        // Fallback: Try to get user by external auth ID (from identity provider)
        var externalAuthId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(externalAuthId))
        {
            var user = await _userRepository.GetByExternalAuthIdAsync(externalAuthId, cancellationToken);
            if (user != null)
            {
                _logger.LogDebug(
                    "Found user {UserId} by external auth ID {ExternalAuthId}",
                    user.Id,
                    externalAuthId);
                return user;
            }
        }

        _logger.LogWarning(
            "No PRFactory user found for authenticated identity. ExternalAuthId={ExternalAuthId}",
            externalAuthId);

        return null;
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("No authenticated user in HTTP context");
            return null;
        }

        // Try to get prfactory_tenant_id claim first (fastest)
        var tenantIdClaim = httpContext.User.FindFirst(PrFactoryTenantIdClaim);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }

        // Fallback: Get user and return their tenant ID
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.TenantId;
    }

    /// <inheritdoc/>
    public Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var isAuthenticated = httpContext?.User?.Identity?.IsAuthenticated == true;

        _logger.LogDebug("IsAuthenticated check: {IsAuthenticated}", isAuthenticated);

        return Task.FromResult(isAuthenticated);
    }
}
