using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Git;
using PRFactory.Infrastructure.Persistence.Encryption;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Service for repository operations
/// </summary>
public class RepositoryService(
    IRepositoryRepository repositoryRepository,
    ITicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IEncryptionService encryptionService,
    ILocalGitService localGitService,
    ILogger<RepositoryService> logger) : IRepositoryService
{
    public async Task<List<RepositoryDto>> GetRepositoriesForTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation("Getting repositories for tenant {TenantId}", tenantId);

        var repositories = await repositoryRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        logger.LogInformation("Found {Count} repositories for tenant {TenantId}", repositories.Count, tenantId);

        return repositories.Select(MapToDto).ToList();
    }

    public async Task<RepositoryDto?> GetRepositoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation("Getting repository {RepositoryId} for tenant {TenantId}", id, tenantId);

        var repository = await repositoryRepository.GetByIdAsync(id, cancellationToken);

        if (repository == null)
        {
            logger.LogWarning("Repository {RepositoryId} not found", id);
            return null;
        }

        // Enforce tenant isolation
        if (repository.TenantId != tenantId)
        {
            logger.LogWarning(
                "Attempted to access repository {RepositoryId} from tenant {RequestingTenantId}, but it belongs to tenant {OwnerTenantId}",
                id, tenantId, repository.TenantId);
            return null;
        }

        return MapToDto(repository);
    }

    public async Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation(
            "Creating repository {Name} for tenant {TenantId} on platform {GitPlatform}",
            dto.Name, tenantId, dto.GitPlatform);

        // Check if repository with same clone URL already exists
        var existingRepository = await repositoryRepository.GetByCloneUrlAsync(dto.CloneUrl, cancellationToken);
        if (existingRepository != null)
        {
            logger.LogError("Repository with clone URL {CloneUrl} already exists", dto.CloneUrl);
            throw new InvalidOperationException($"Repository with clone URL '{dto.CloneUrl}' already exists");
        }

        // Encrypt access token before storing
        var encryptedAccessToken = encryptionService.Encrypt(dto.AccessToken);

        var repository = Repository.Create(
            tenantId,
            dto.Name,
            dto.GitPlatform,
            dto.CloneUrl,
            encryptedAccessToken,
            dto.DefaultBranch);

        var created = await repositoryRepository.AddAsync(repository, cancellationToken);

        logger.LogInformation("Created repository {RepositoryId} for tenant {TenantId}", created.Id, tenantId);

        return MapToDto(created);
    }

    public async Task<RepositoryDto> UpdateRepositoryAsync(Guid id, UpdateRepositoryDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation("Updating repository {RepositoryId} for tenant {TenantId}", id, tenantId);

        var repository = await repositoryRepository.GetByIdAsync(id, cancellationToken);

        if (repository == null)
        {
            logger.LogError("Repository {RepositoryId} not found", id);
            throw new InvalidOperationException($"Repository with ID '{id}' not found");
        }

        // Enforce tenant isolation
        if (repository.TenantId != tenantId)
        {
            logger.LogError(
                "Attempted to update repository {RepositoryId} from tenant {RequestingTenantId}, but it belongs to tenant {OwnerTenantId}",
                id, tenantId, repository.TenantId);
            throw new UnauthorizedAccessException($"Cannot access repository '{id}' - it belongs to a different tenant");
        }

        // Create new repository with updated values (since entity is immutable via private setters)
        var updatedRepository = Repository.Create(
            repository.TenantId,
            dto.Name,
            dto.GitPlatform,
            dto.CloneUrl,
            dto.AccessToken != null ? encryptionService.Encrypt(dto.AccessToken) : repository.AccessToken,
            dto.DefaultBranch);

        // Preserve original ID, CreatedAt, and other properties
        var repositoryToUpdate = repository;

        // Update properties through reflection since they have private setters
        var idProperty = typeof(Repository).GetProperty(nameof(Repository.Id));
        idProperty?.SetValue(updatedRepository, repository.Id);

        var createdAtProperty = typeof(Repository).GetProperty(nameof(Repository.CreatedAt));
        createdAtProperty?.SetValue(updatedRepository, repository.CreatedAt);

        // Update using domain methods where available
        if (dto.AccessToken != null && dto.AccessToken != repository.AccessToken)
        {
            var encryptedToken = encryptionService.Encrypt(dto.AccessToken);
            repository.UpdateAccessToken(encryptedToken);
        }

        if (dto.DefaultBranch != repository.DefaultBranch)
        {
            repository.UpdateDefaultBranch(dto.DefaultBranch);
        }

        if (dto.IsActive && !repository.IsActive)
        {
            repository.Activate();
        }
        else if (!dto.IsActive && repository.IsActive)
        {
            repository.Deactivate();
        }

        await repositoryRepository.UpdateAsync(repository, cancellationToken);

        logger.LogInformation("Updated repository {RepositoryId} for tenant {TenantId}", id, tenantId);

        return MapToDto(repository);
    }

    public async Task DeleteRepositoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation("Deleting repository {RepositoryId} for tenant {TenantId}", id, tenantId);

        var repository = await repositoryRepository.GetByIdAsync(id, cancellationToken);

        if (repository == null)
        {
            logger.LogError("Repository {RepositoryId} not found", id);
            throw new InvalidOperationException($"Repository with ID '{id}' not found");
        }

        // Enforce tenant isolation
        if (repository.TenantId != tenantId)
        {
            logger.LogError(
                "Attempted to delete repository {RepositoryId} from tenant {RequestingTenantId}, but it belongs to tenant {OwnerTenantId}",
                id, tenantId, repository.TenantId);
            throw new UnauthorizedAccessException($"Cannot access repository '{id}' - it belongs to a different tenant");
        }

        // Soft delete - deactivate the repository instead of hard delete
        repository.Deactivate();
        await repositoryRepository.UpdateAsync(repository, cancellationToken);

        logger.LogInformation("Deactivated (soft deleted) repository {RepositoryId} for tenant {TenantId}", id, tenantId);
    }

    public async Task<ConnectionTestResult> TestRepositoryConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation("Testing connection for repository {RepositoryId} for tenant {TenantId}", id, tenantId);

        var repository = await repositoryRepository.GetByIdAsync(id, cancellationToken);

        if (repository == null)
        {
            logger.LogError("Repository {RepositoryId} not found", id);
            throw new InvalidOperationException($"Repository with ID '{id}' not found");
        }

        // Enforce tenant isolation
        if (repository.TenantId != tenantId)
        {
            logger.LogError(
                "Attempted to test connection for repository {RepositoryId} from tenant {RequestingTenantId}, but it belongs to tenant {OwnerTenantId}",
                id, tenantId, repository.TenantId);
            throw new UnauthorizedAccessException($"Cannot access repository '{id}' - it belongs to a different tenant");
        }

        // Decrypt access token
        var accessToken = encryptionService.Decrypt(repository.AccessToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Test connection by attempting to clone (shallow clone to minimize data transfer)
            var localPath = await localGitService.CloneAsync(repository.CloneUrl, accessToken, cancellationToken);

            stopwatch.Stop();

            // Clean up test clone
            if (Directory.Exists(localPath))
            {
                Directory.Delete(localPath, true);
            }

            logger.LogInformation(
                "Connection test successful for repository {RepositoryId}, took {ElapsedMs}ms",
                id, stopwatch.ElapsedMilliseconds);

            return new ConnectionTestResult
            {
                Success = true,
                Message = "Connection successful",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorDetails = null
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Connection test failed for repository {RepositoryId}, took {ElapsedMs}ms",
                id, stopwatch.ElapsedMilliseconds);

            return new ConnectionTestResult
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorDetails = ex.ToString()
            };
        }
    }

    public async Task<RepositoryStatisticsDto> GetRepositoryStatisticsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = await GetCurrentTenantIdAsync(cancellationToken);

        logger.LogInformation("Getting statistics for repository {RepositoryId} for tenant {TenantId}", id, tenantId);

        var repository = await repositoryRepository.GetByIdAsync(id, cancellationToken);

        if (repository == null)
        {
            logger.LogError("Repository {RepositoryId} not found", id);
            throw new InvalidOperationException($"Repository with ID '{id}' not found");
        }

        // Enforce tenant isolation
        if (repository.TenantId != tenantId)
        {
            logger.LogError(
                "Attempted to get statistics for repository {RepositoryId} from tenant {RequestingTenantId}, but it belongs to tenant {OwnerTenantId}",
                id, tenantId, repository.TenantId);
            throw new UnauthorizedAccessException($"Cannot access repository '{id}' - it belongs to a different tenant");
        }

        // Get ticket count
        var tickets = await ticketRepository.GetByRepositoryIdAsync(id, cancellationToken);
        var totalTickets = tickets.Count;

        // Count tickets with pull requests (those that have reached PRCreated state or beyond)
        var totalPullRequests = tickets.Count(t =>
            t.State >= Domain.ValueObjects.WorkflowState.PRCreated);

        logger.LogInformation(
            "Repository {RepositoryId} has {TotalTickets} tickets and {TotalPullRequests} pull requests",
            id, totalTickets, totalPullRequests);

        return new RepositoryStatisticsDto
        {
            RepositoryId = id,
            TotalTickets = totalTickets,
            TotalPullRequests = totalPullRequests,
            LastAccessedAt = repository.LastAccessedAt
        };
    }

    private async Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenantId = await currentUserService.GetCurrentTenantIdAsync(cancellationToken);

        if (tenantId == null)
        {
            logger.LogError("No current tenant ID found - user may not be authenticated");
            throw new UnauthorizedAccessException("No current tenant found - please authenticate");
        }

        return tenantId.Value;
    }

    private static RepositoryDto MapToDto(Repository repository) => new()
    {
        Id = repository.Id,
        TenantId = repository.TenantId,
        Name = repository.Name,
        GitPlatform = repository.GitPlatform,
        CloneUrl = repository.CloneUrl,
        DefaultBranch = repository.DefaultBranch,
        IsActive = repository.IsActive,
        CreatedAt = repository.CreatedAt,
        UpdatedAt = repository.UpdatedAt,
        LastAccessedAt = repository.LastAccessedAt
    };
}
