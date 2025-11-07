using PRFactory.Domain.Entities;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing repositories via API
/// </summary>
public interface IRepositoryService
{
    /// <summary>
    /// Get all repositories
    /// </summary>
    Task<List<Repository>> GetAllRepositoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific repository by ID
    /// </summary>
    Task<Repository?> GetRepositoryByIdAsync(Guid repositoryId, CancellationToken ct = default);

    /// <summary>
    /// Create a new repository
    /// </summary>
    Task<Repository> CreateRepositoryAsync(Repository repository, CancellationToken ct = default);

    /// <summary>
    /// Update an existing repository
    /// </summary>
    Task UpdateRepositoryAsync(Guid repositoryId, Repository repository, CancellationToken ct = default);

    /// <summary>
    /// Delete a repository
    /// </summary>
    Task DeleteRepositoryAsync(Guid repositoryId, CancellationToken ct = default);
}
