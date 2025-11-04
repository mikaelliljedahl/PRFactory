using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for Repository entity operations
/// </summary>
public interface IRepositoryRepository
{
    /// <summary>
    /// Gets a repository by its unique identifier
    /// </summary>
    Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all repositories for a specific tenant
    /// </summary>
    Task<List<Repository>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository by its clone URL
    /// </summary>
    Task<Repository?> GetByCloneUrlAsync(string cloneUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository by name and tenant
    /// </summary>
    Task<Repository?> GetByNameAndTenantAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all repositories for a specific git platform
    /// </summary>
    Task<List<Repository>> GetByGitPlatformAsync(string gitPlatform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active repositories
    /// </summary>
    Task<List<Repository>> GetActiveRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets repositories that haven't been accessed since the specified date
    /// </summary>
    Task<List<Repository>> GetStaleRepositoriesAsync(DateTime thresholdDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository with its tickets eagerly loaded
    /// </summary>
    Task<Repository?> GetByIdWithTicketsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new repository
    /// </summary>
    Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing repository
    /// </summary>
    Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a repository
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a repository with the given clone URL already exists
    /// </summary>
    Task<bool> ExistsAsync(string cloneUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of repositories by git platform
    /// </summary>
    Task<Dictionary<string, int>> GetPlatformCountsAsync(CancellationToken cancellationToken = default);
}
