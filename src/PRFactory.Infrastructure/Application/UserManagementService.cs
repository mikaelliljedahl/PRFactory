using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Service for managing users within a tenant (role changes, activation, statistics)
/// </summary>
public class UserManagementService(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IPlanReviewRepository planReviewRepository,
    IReviewCommentRepository reviewCommentRepository,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    /// <summary>
    /// Gets all users for the current tenant
    /// </summary>
    public async Task<List<UserManagementDto>> GetUsersForTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID is not available");
        }

        logger.LogInformation("Getting all users for tenant {TenantId}", tenantId);

        var users = await userRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);

        return users.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets a specific user by ID (must belong to current tenant)
    /// </summary>
    public async Task<UserManagementDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID is not available");
        }

        logger.LogInformation("Getting user {UserId} for tenant {TenantId}", id, tenantId);

        var user = await userRepository.GetByIdAsync(id, cancellationToken);

        // Verify user belongs to current tenant (tenant isolation)
        if (user != null && user.TenantId != tenantId.Value)
        {
            logger.LogWarning("User {UserId} does not belong to tenant {TenantId}", id, tenantId);
            return null;
        }

        return user != null ? MapToDto(user) : null;
    }

    /// <summary>
    /// Updates a user's role within the tenant.
    /// Only Owner and Admin can change roles.
    /// Cannot remove the last Owner from a tenant.
    /// </summary>
    public async Task UpdateUserRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default)
    {
        var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
        if (currentUser == null)
        {
            throw new InvalidOperationException("Current user is not available");
        }

        // Only Owner and Admin can change roles
        if (currentUser.Role != UserRole.Owner && currentUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Owners and Admins can change user roles");
        }

        logger.LogInformation("User {CurrentUserId} ({CurrentUserRole}) attempting to change role of user {UserId} to {NewRole}",
            currentUser.Id, currentUser.Role, id, role);

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        // Verify user belongs to current tenant (tenant isolation)
        if (user.TenantId != currentUser.TenantId)
        {
            throw new UnauthorizedAccessException("Cannot modify users from other tenants");
        }

        // If changing from Owner role, ensure there's at least one other Owner
        if (user.Role == UserRole.Owner && role != UserRole.Owner)
        {
            var allUsers = await userRepository.GetByTenantIdAsync(currentUser.TenantId, cancellationToken);
            var ownerCount = allUsers.Count(u => u.Role == UserRole.Owner && u.IsActive);

            if (ownerCount <= 1)
            {
                throw new InvalidOperationException("Cannot remove the last Owner from the tenant");
            }

            logger.LogWarning("Removing Owner role from user {UserId}. Remaining owners: {RemainingOwners}",
                user.Id, ownerCount - 1);
        }

        user.SetRole(role);
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Successfully updated role for user {UserId} to {NewRole}", id, role);
    }

    /// <summary>
    /// Activates a user (allows access to the system)
    /// </summary>
    public async Task ActivateUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID is not available");
        }

        logger.LogInformation("Activating user {UserId}", id);

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        // Verify user belongs to current tenant (tenant isolation)
        if (user.TenantId != tenantId.Value)
        {
            throw new UnauthorizedAccessException("Cannot modify users from other tenants");
        }

        user.Activate();
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Successfully activated user {UserId}", id);
    }

    /// <summary>
    /// Deactivates a user (prevents access to the system)
    /// </summary>
    public async Task DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID is not available");
        }

        logger.LogInformation("Deactivating user {UserId}", id);

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        // Verify user belongs to current tenant (tenant isolation)
        if (user.TenantId != tenantId.Value)
        {
            throw new UnauthorizedAccessException("Cannot modify users from other tenants");
        }

        user.Deactivate();
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Successfully deactivated user {UserId}", id);
    }

    /// <summary>
    /// Gets statistics for a specific user (plan reviews, comments, last activity)
    /// </summary>
    public async Task<UserStatisticsDto> GetUserStatisticsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);
        if (tenantId == null)
        {
            throw new InvalidOperationException("Current tenant ID is not available");
        }

        logger.LogInformation("Getting statistics for user {UserId}", id);

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        // Verify user belongs to current tenant (tenant isolation)
        if (user.TenantId != tenantId.Value)
        {
            throw new UnauthorizedAccessException("Cannot access statistics for users from other tenants");
        }

        // Get plan reviews (count pending reviews for now, can be enhanced to count all reviews)
        var planReviews = await planReviewRepository.GetPendingByReviewerIdAsync(id, cancellationToken);
        var planReviewCount = planReviews.Count;

        // Get comments authored by user
        var comments = await reviewCommentRepository.GetByAuthorIdAsync(id, cancellationToken);
        var commentCount = comments.Count;

        logger.LogInformation("User {UserId} statistics: {PlanReviews} plan reviews, {Comments} comments",
            id, planReviewCount, commentCount);

        return new UserStatisticsDto
        {
            UserId = id,
            TotalPlanReviews = planReviewCount,
            TotalComments = commentCount,
            LastSeenAt = user.LastSeenAt
        };
    }

    /// <summary>
    /// Maps a User entity to UserManagementDto
    /// </summary>
    private static UserManagementDto MapToDto(User user)
    {
        return new UserManagementDto
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastSeenAt = user.LastSeenAt
        };
    }
}
