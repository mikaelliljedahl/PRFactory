using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for accessing the current authenticated user context.
/// This is a stub implementation until proper authentication is implemented.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    /// <returns>User entity if authenticated, null otherwise</returns>
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's tenant ID
    /// </summary>
    /// <returns>Tenant ID if authenticated, null otherwise</returns>
    Task<Guid?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is currently authenticated
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
}
