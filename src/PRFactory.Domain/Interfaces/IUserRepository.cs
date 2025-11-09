using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for managing users
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique ID
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address within a tenant
    /// </summary>
    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a specific tenant
    /// </summary>
    Task<List<User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple users by their IDs
    /// </summary>
    Task<List<User>> GetByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user
    /// </summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user by ID
    /// </summary>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email exists in the tenant
    /// </summary>
    Task<bool> ExistsAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
}
