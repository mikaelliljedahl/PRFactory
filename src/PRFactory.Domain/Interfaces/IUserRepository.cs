using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address within a tenant
    /// </summary>
    Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their external authentication ID
    /// </summary>
    Task<User?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a specific tenant
    /// </summary>
    Task<List<User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple users by their IDs
    /// </summary>
    Task<List<User>> GetByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users
    /// </summary>
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email already exists within a tenant
    /// </summary>
    Task<bool> ExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with OAuth tokens configured
    /// </summary>
    Task<List<User>> GetUsersWithOAuthTokensAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
}
