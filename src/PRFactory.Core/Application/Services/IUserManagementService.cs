using PRFactory.Core.Application.DTOs;
using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for managing users within a tenant (role changes, activation, statistics)
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Gets all users for the current tenant
    /// </summary>
    Task<List<UserManagementDto>> GetUsersForTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user by ID (must belong to current tenant)
    /// </summary>
    Task<UserManagementDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's role within the tenant.
    /// Only Owner and Admin can change roles.
    /// Cannot remove the last Owner from a tenant.
    /// </summary>
    Task UpdateUserRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a user (allows access to the system)
    /// </summary>
    Task ActivateUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user (prevents access to the system)
    /// </summary>
    Task DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for a specific user (plan reviews, comments, last activity)
    /// </summary>
    Task<UserStatisticsDto> GetUserStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// User activity statistics
/// </summary>
public class UserStatisticsDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Total number of plan reviews completed by the user
    /// </summary>
    public int TotalPlanReviews { get; set; }

    /// <summary>
    /// Total number of comments made by the user
    /// </summary>
    public int TotalComments { get; set; }

    /// <summary>
    /// When the user was last seen (last activity timestamp)
    /// </summary>
    public DateTime? LastSeenAt { get; set; }
}
