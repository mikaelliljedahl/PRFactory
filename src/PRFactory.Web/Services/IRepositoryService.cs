using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing repositories.
/// This is a facade service that converts between DTOs and domain entities.
/// </summary>
public interface IRepositoryService
{
    /// <summary>
    /// Get all repositories
    /// </summary>
    Task<List<RepositoryDto>> GetAllRepositoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific repository by ID
    /// </summary>
    Task<RepositoryDto?> GetRepositoryByIdAsync(Guid repositoryId, CancellationToken ct = default);

    /// <summary>
    /// Create a new repository
    /// </summary>
    Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing repository
    /// </summary>
    Task UpdateRepositoryAsync(Guid repositoryId, UpdateRepositoryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete a repository
    /// </summary>
    Task DeleteRepositoryAsync(Guid repositoryId, CancellationToken ct = default);

    /// <summary>
    /// Test connection to a repository
    /// </summary>
    Task<RepositoryConnectionTestResult> TestConnectionAsync(string cloneUrl, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Get available branches from a repository
    /// </summary>
    Task<List<string>> GetBranchesAsync(Guid repositoryId, CancellationToken ct = default);

    /// <summary>
    /// Get all tenants for dropdown selection
    /// </summary>
    Task<List<TenantDto>> GetAllTenantsAsync(CancellationToken ct = default);
}
