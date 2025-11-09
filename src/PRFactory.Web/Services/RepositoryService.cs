using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of repository service.
/// Uses direct application service injection (Blazor Server architecture).
/// This is a facade service that converts between DTOs and domain entities.
/// </summary>
public class RepositoryService : IRepositoryService
{
    private readonly ILogger<RepositoryService> _logger;
    private readonly IRepositoryApplicationService _repositoryApplicationService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IEncryptionService _encryptionService;

    public RepositoryService(
        ILogger<RepositoryService> logger,
        IRepositoryApplicationService repositoryApplicationService,
        ITenantRepository tenantRepository,
        ITicketRepository ticketRepository,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _repositoryApplicationService = repositoryApplicationService;
        _tenantRepository = tenantRepository;
        _ticketRepository = ticketRepository;
        _encryptionService = encryptionService;
    }

    public async Task<List<RepositoryDto>> GetAllRepositoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var repositories = await _repositoryApplicationService.GetAllRepositoriesAsync(ct);
            var dtos = new List<RepositoryDto>();

            foreach (var repo in repositories)
            {
                var ticketCount = (await _ticketRepository.GetByRepositoryIdAsync(repo.Id, ct)).Count;
                var tenant = await _tenantRepository.GetByIdAsync(repo.TenantId, ct);

                dtos.Add(MapToDto(repo, tenant?.Name ?? "Unknown", ticketCount));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all repositories");
            throw;
        }
    }

    public async Task<RepositoryDto?> GetRepositoryByIdAsync(Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            var repository = await _repositoryApplicationService.GetRepositoryByIdAsync(repositoryId, ct);
            if (repository == null)
            {
                return null;
            }

            var ticketCount = (await _ticketRepository.GetByRepositoryIdAsync(repositoryId, ct)).Count;
            var tenant = await _tenantRepository.GetByIdAsync(repository.TenantId, ct);

            return MapToDto(repository, tenant?.Name ?? "Unknown", ticketCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryRequest request, CancellationToken ct = default)
    {
        try
        {
            var encryptedToken = _encryptionService.Encrypt(request.AccessToken);

            var repository = Repository.Create(
                request.TenantId,
                request.Name,
                request.GitPlatform,
                request.CloneUrl,
                encryptedToken,
                request.DefaultBranch
            );

            var created = await _repositoryApplicationService.CreateRepositoryAsync(repository, ct);
            var tenant = await _tenantRepository.GetByIdAsync(created.TenantId, ct);

            _logger.LogInformation("Created repository {RepositoryName}", request.Name);

            return MapToDto(created, tenant?.Name ?? "Unknown", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repository {RepositoryName}", request.Name);
            throw;
        }
    }

    public async Task UpdateRepositoryAsync(Guid repositoryId, UpdateRepositoryRequest request, CancellationToken ct = default)
    {
        try
        {
            var existing = await _repositoryApplicationService.GetRepositoryByIdAsync(repositoryId, ct);
            if (existing == null)
            {
                throw new InvalidOperationException($"Repository {repositoryId} not found");
            }

            var accessToken = string.IsNullOrWhiteSpace(request.AccessToken)
                ? existing.AccessToken
                : _encryptionService.Encrypt(request.AccessToken);

            var updated = Repository.Create(
                existing.TenantId,
                request.Name,
                request.GitPlatform,
                request.CloneUrl,
                accessToken,
                request.DefaultBranch
            );

            await _repositoryApplicationService.UpdateRepositoryAsync(repositoryId, updated, ct);

            if (request.IsActive != existing.IsActive)
            {
                if (request.IsActive)
                {
                    existing.Activate();
                }
                else
                {
                    existing.Deactivate();
                }
            }

            _logger.LogInformation("Updated repository {RepositoryId}", repositoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task DeleteRepositoryAsync(Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            await _repositoryApplicationService.DeleteRepositoryAsync(repositoryId, ct);
            _logger.LogInformation("Deleted repository {RepositoryId}", repositoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<RepositoryConnectionTestResult> TestConnectionAsync(string cloneUrl, string accessToken, CancellationToken ct = default)
    {
        try
        {
            var result = await _repositoryApplicationService.TestRepositoryConnectionAsync(cloneUrl, accessToken, ct);

            return new RepositoryConnectionTestResult
            {
                Success = result.Success,
                Message = result.Message,
                AvailableBranches = result.Branches,
                ErrorDetails = result.ErrorDetails,
                TestedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing repository connection");
            return new RepositoryConnectionTestResult
            {
                Success = false,
                Message = "An unexpected error occurred while testing the connection.",
                ErrorDetails = ex.ToString(),
                TestedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<List<string>> GetBranchesAsync(Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            return await _repositoryApplicationService.GetRepositoryBranchesAsync(repositoryId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches for repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<List<TenantDto>> GetAllTenantsAsync(CancellationToken ct = default)
    {
        try
        {
            var tenants = await _tenantRepository.GetAllAsync(ct);

            return tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all tenants");
            throw;
        }
    }

    private RepositoryDto MapToDto(Repository repository, string tenantName, int ticketCount)
    {
        return new RepositoryDto
        {
            Id = repository.Id,
            TenantId = repository.TenantId,
            TenantName = tenantName,
            Name = repository.Name,
            GitPlatform = repository.GitPlatform,
            CloneUrl = repository.CloneUrl,
            DefaultBranch = repository.DefaultBranch,
            IsActive = repository.IsActive,
            CreatedAt = repository.CreatedAt,
            UpdatedAt = repository.UpdatedAt,
            LastAccessedAt = repository.LastAccessedAt,
            TicketCount = ticketCount
        };
    }
}
