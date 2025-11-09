using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for user management operations
/// </summary>
public interface IUserService
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
    /// <param name="tenantId">The tenant ID the user belongs to</param>
    /// <param name="email">User's email address</param>
    /// <param name="displayName">User's display name</param>
    /// <param name="avatarUrl">Optional URL to user's avatar image</param>
    /// <param name="externalAuthId">Optional external authentication ID</param>
    Task<User> CreateUserAsync(
        Guid tenantId,
        string email,
        string displayName,
        string? avatarUrl = null,
        string? externalAuthId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile information
    /// </summary>
    Task UpdateProfileAsync(Guid userId, string displayName, string? avatarUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's last seen timestamp
    /// </summary>
    Task UpdateLastSeenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user by ID
    /// </summary>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email exists in the tenant
    /// </summary>
    Task<bool> ExistsAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
}
