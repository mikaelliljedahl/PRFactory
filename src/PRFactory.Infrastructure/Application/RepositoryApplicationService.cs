using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Git;
using PRFactory.Infrastructure.Persistence.Encryption;

// Type alias to resolve ambiguity between PRFactory.Domain.Entities.Repository and LibGit2Sharp.Repository
using GitRepository = LibGit2Sharp.Repository;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing repositories.
/// This service encapsulates business logic and coordinates repository operations.
/// </summary>
public class RepositoryApplicationService : IRepositoryApplicationService
{
    private readonly ILogger<RepositoryApplicationService> _logger;
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ILocalGitService _localGitService;
    private readonly IEncryptionService _encryptionService;

    public RepositoryApplicationService(
        ILogger<RepositoryApplicationService> logger,
        IRepositoryRepository repositoryRepository,
        ITicketRepository ticketRepository,
        ITenantRepository tenantRepository,
        ILocalGitService localGitService,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _repositoryRepository = repositoryRepository;
        _ticketRepository = ticketRepository;
        _tenantRepository = tenantRepository;
        _localGitService = localGitService;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc/>
    public async Task<List<Repository>> GetAllRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all repositories");

        // Get all repositories (for multi-tenant apps, this would be filtered by tenant from context)
        var repositories = await _repositoryRepository.GetAllAsync(cancellationToken);

        _logger.LogDebug("Found {Count} repositories", repositories.Count);

        return repositories;
    }

    /// <inheritdoc/>
    public async Task<Repository?> GetRepositoryByIdAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting repository {RepositoryId}", repositoryId);
        return await _repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Repository> CreateRepositoryAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating repository {RepositoryName}", repository.Name);

        // Validate repository doesn't already exist
        var exists = await _repositoryRepository.ExistsAsync(repository.CloneUrl, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Repository with clone URL '{repository.CloneUrl}' already exists");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(repository.Name))
        {
            throw new InvalidOperationException("Repository name is required");
        }

        if (string.IsNullOrWhiteSpace(repository.CloneUrl))
        {
            throw new InvalidOperationException("Repository clone URL is required");
        }

        if (string.IsNullOrWhiteSpace(repository.GitPlatform))
        {
            throw new InvalidOperationException("Repository git platform is required");
        }

        // Create repository
        var created = await _repositoryRepository.AddAsync(repository, cancellationToken);

        _logger.LogInformation("Created repository {RepositoryId}: {RepositoryName}", created.Id, created.Name);

        return created;
    }

    /// <inheritdoc/>
    public async Task UpdateRepositoryAsync(Guid repositoryId, Repository repository, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating repository {RepositoryId}", repositoryId);

        // Verify repository exists
        var existing = await _repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
        if (existing == null)
        {
            throw new InvalidOperationException($"Repository {repositoryId} not found");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(repository.Name))
        {
            throw new InvalidOperationException("Repository name is required");
        }

        if (string.IsNullOrWhiteSpace(repository.CloneUrl))
        {
            throw new InvalidOperationException("Repository clone URL is required");
        }

        if (string.IsNullOrWhiteSpace(repository.GitPlatform))
        {
            throw new InvalidOperationException("Repository git platform is required");
        }

        // Update the repository using entity methods to maintain encapsulation
        if (existing.DefaultBranch != repository.DefaultBranch)
        {
            existing.UpdateDefaultBranch(repository.DefaultBranch);
        }

        if (existing.AccessToken != repository.AccessToken)
        {
            existing.UpdateAccessToken(repository.AccessToken);
        }

        // For fields without specific update methods, we need to update via repository
        // Since the entity uses private setters, we'll update through the repository pattern
        await _repositoryRepository.UpdateAsync(repository, cancellationToken);

        _logger.LogInformation("Updated repository {RepositoryId}: {RepositoryName}", repositoryId, existing.Name);
    }

    /// <inheritdoc/>
    public async Task DeleteRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting repository {RepositoryId}", repositoryId);

        // Verify repository exists
        var repository = await _repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
        if (repository == null)
        {
            throw new InvalidOperationException($"Repository {repositoryId} not found");
        }

        // Check if repository has any tickets
        var tickets = await _ticketRepository.GetByRepositoryIdAsync(repositoryId, cancellationToken);
        if (tickets.Any())
        {
            throw new InvalidOperationException(
                $"Cannot delete repository '{repository.Name}' because it has {tickets.Count} associated tickets. " +
                "Please delete or reassign the tickets first.");
        }

        // Delete repository
        await _repositoryRepository.DeleteAsync(repositoryId, cancellationToken);

        _logger.LogInformation("Deleted repository {RepositoryId}: {RepositoryName}", repositoryId, repository.Name);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message, List<string> Branches, string? ErrorDetails)> TestRepositoryConnectionAsync(
        string cloneUrl,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing connection to repository {CloneUrl}", cloneUrl);

        try
        {
            var localPath = await _localGitService.CloneAsync(cloneUrl, accessToken, cancellationToken);

            _logger.LogInformation("Successfully cloned repository to {LocalPath}", localPath);

            var branches = new List<string>();
            try
            {
                using var repo = new GitRepository(localPath);
                branches = repo.Branches
                    .Where(b => !b.IsRemote)
                    .Select(b => b.FriendlyName)
                    .ToList();

                if (branches.Count == 0)
                {
                    branches = repo.Branches
                        .Where(b => b.IsRemote && !b.FriendlyName.Contains("HEAD"))
                        .Select(b => b.FriendlyName.Replace("origin/", ""))
                        .Distinct()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve branches, but connection succeeded");
            }

            try
            {
                if (Directory.Exists(localPath))
                {
                    Directory.Delete(localPath, true);
                    _logger.LogDebug("Cleaned up test repository at {LocalPath}", localPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not clean up test repository at {LocalPath}", localPath);
            }

            return (true, "Successfully connected to repository", branches, null);
        }
        catch (LibGit2SharpException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to repository {CloneUrl}", cloneUrl);

            var errorMessage = ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase)
                ? "Authentication failed. Please check your access token."
                : ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? "Repository not found. Please check the clone URL."
                : "Failed to connect to repository.";

            return (false, errorMessage, new List<string>(), ex.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing connection to repository {CloneUrl}", cloneUrl);
            return (false, "An unexpected error occurred while testing the connection.", new List<string>(), ex.ToString());
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetRepositoryBranchesAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting branches for repository {RepositoryId}", repositoryId);

        var repository = await _repositoryRepository.GetByIdAsync(repositoryId, cancellationToken);
        if (repository == null)
        {
            throw new InvalidOperationException($"Repository {repositoryId} not found");
        }

        var decryptedToken = _encryptionService.Decrypt(repository.AccessToken);

        var testResult = await TestRepositoryConnectionAsync(repository.CloneUrl, decryptedToken, cancellationToken);

        if (!testResult.Success)
        {
            throw new InvalidOperationException($"Failed to retrieve branches: {testResult.Message}");
        }

        return testResult.Branches;
    }
}
