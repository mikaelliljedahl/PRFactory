using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Stub implementation of ICurrentUserService that returns a hardcoded default user.
/// This is a temporary implementation until proper authentication is integrated.
///
/// TODO: Replace with actual authentication implementation (Auth0, Azure AD, etc.)
/// </summary>
public class StubCurrentUserService : ICurrentUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<StubCurrentUserService> _logger;

    // Hardcoded values for stub implementation
    private const string DefaultUserEmail = "dev@prfactory.local";
    private const string DefaultUserDisplayName = "Development User";

    public StubCurrentUserService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ILogger<StubCurrentUserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Id;
    }

    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        // Get the first active tenant (stub behavior)
        var tenant = (await _tenantRepository.GetActiveTenantsAsync(cancellationToken)).FirstOrDefault();
        if (tenant == null)
        {
            _logger.LogWarning("No active tenants found for stub user");
            return null;
        }

        // Try to get existing default user
        var user = await _userRepository.GetByEmailAsync(DefaultUserEmail, tenant.Id, cancellationToken);

        // Create default user if it doesn't exist
        if (user == null)
        {
            user = new User(tenant.Id, DefaultUserEmail, DefaultUserDisplayName);
            await _userRepository.CreateAsync(user, cancellationToken);
            _logger.LogInformation("Created stub user {UserId} for tenant {TenantId}", user.Id, tenant.Id);
        }

        return user;
    }

    public async Task<Guid?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.TenantId;
    }

    public Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        // Stub: always return true (pretend user is authenticated)
        return Task.FromResult(true);
    }
}
