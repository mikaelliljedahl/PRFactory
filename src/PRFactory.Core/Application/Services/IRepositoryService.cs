using PRFactory.Core.Application.DTOs;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service interface for repository operations
/// </summary>
public interface IRepositoryService
{
    /// <summary>
    /// Gets all repositories for the current tenant
    /// </summary>
    Task<List<RepositoryDto>> GetRepositoriesForTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository by its ID
    /// </summary>
    Task<RepositoryDto?> GetRepositoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new repository
    /// </summary>
    Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing repository
    /// </summary>
    Task<RepositoryDto> UpdateRepositoryAsync(Guid id, UpdateRepositoryDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a repository
    /// </summary>
    Task DeleteRepositoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to a repository
    /// </summary>
    Task<ConnectionTestResult> TestRepositoryConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for a repository
    /// </summary>
    Task<RepositoryStatisticsDto> GetRepositoryStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
}
