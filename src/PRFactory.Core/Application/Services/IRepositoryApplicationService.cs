// Type alias to avoid ambiguity with LibGit2Sharp.Repository
using Repository = PRFactory.Domain.Entities.Repository;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing repositories.
/// This service encapsulates business logic for repository operations.
/// </summary>
public interface IRepositoryApplicationService
{
    /// <summary>
    /// Gets all repositories for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of repositories</returns>
    Task<List<Repository>> GetAllRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific repository by ID
    /// </summary>
    /// <param name="repositoryId">The repository ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The repository, or null if not found</returns>
    Task<Repository?> GetRepositoryByIdAsync(Guid repositoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new repository
    /// </summary>
    /// <param name="repository">The repository to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created repository</returns>
    /// <exception cref="InvalidOperationException">Thrown if repository with same clone URL already exists</exception>
    Task<Repository> CreateRepositoryAsync(Repository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing repository
    /// </summary>
    /// <param name="repositoryId">The repository ID</param>
    /// <param name="repository">The updated repository data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if repository not found</exception>
    Task UpdateRepositoryAsync(Guid repositoryId, Repository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a repository
    /// </summary>
    /// <param name="repositoryId">The repository ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if repository not found or has associated tickets</exception>
    Task DeleteRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connection to a repository by attempting to clone it
    /// </summary>
    /// <param name="cloneUrl">Repository clone URL</param>
    /// <param name="accessToken">Access token for authentication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result with success status, message, and available branches if successful</returns>
    Task<(bool Success, string Message, List<string> Branches, string? ErrorDetails)> TestRepositoryConnectionAsync(
        string cloneUrl,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available branches from a repository
    /// </summary>
    /// <param name="repositoryId">The repository ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of branch names</returns>
    Task<List<string>> GetRepositoryBranchesAsync(Guid repositoryId, CancellationToken cancellationToken = default);
}
