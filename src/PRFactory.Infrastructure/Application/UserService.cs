using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service implementation for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByEmailAsync(tenantId, email, cancellationToken);
    }

    public async Task<List<User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken);
    }

    public async Task<List<User>> GetByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdsAsync(userIds, cancellationToken);
    }

    public async Task<User> CreateUserAsync(
        Guid tenantId,
        string email,
        string displayName,
        string? avatarUrl = null,
        string? externalAuthId = null,
        CancellationToken cancellationToken = default)
    {
        // Validate that email is unique within tenant
        if (await _userRepository.ExistsAsync(tenantId, email, cancellationToken))
        {
            throw new InvalidOperationException($"A user with email '{email}' already exists in this tenant.");
        }

        var user = new User(tenantId, email, displayName, avatarUrl, externalAuthId);
        await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("Created user {UserId} ({Email}) for tenant {TenantId}", user.Id, user.Email, tenantId);

        return user;
    }

    public async Task UpdateProfileAsync(Guid userId, string displayName, string? avatarUrl = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.UpdateProfile(displayName, avatarUrl);
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated profile for user {UserId}", userId);
    }

    public async Task UpdateLastSeenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Cannot update last seen for non-existent user {UserId}", userId);
            return;
        }

        user.UpdateLastSeen();
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _userRepository.DeleteAsync(userId, cancellationToken);
        _logger.LogInformation("Deleted user {UserId}", userId);
    }

    public async Task<bool> ExistsAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.ExistsAsync(tenantId, email, cancellationToken);
    }
}
