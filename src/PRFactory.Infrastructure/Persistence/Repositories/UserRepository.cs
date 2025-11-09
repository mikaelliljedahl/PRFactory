using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User entity operations.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        ApplicationDbContext context,
        ILogger<UserRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.TenantId == tenantId, cancellationToken);
    }

    public async Task<User?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalAuthId == externalAuthId, cancellationToken);
    }

    public async Task<List<User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user {UserId} ({Email}) for tenant {TenantId}",
            user.Id, user.Email, user.TenantId);

        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user {UserId} ({Email})", user.Id, user.Email);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Attempted to delete non-existent user {UserId}", id);
            return;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted user {UserId} ({Email})", user.Id, user.Email);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail && u.TenantId == tenantId, cancellationToken);
    }

    public async Task<List<User>> GetUsersWithOAuthTokensAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query
            .Where(u => !string.IsNullOrEmpty(u.OAuthAccessToken))
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
