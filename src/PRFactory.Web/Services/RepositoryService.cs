using PRFactory.Domain.Entities;
using System.Net.Http.Json;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of repository service using HttpClient to call PRFactory.Api
/// </summary>
public class RepositoryService : IRepositoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RepositoryService> _logger;

    public RepositoryService(IHttpClientFactory httpClientFactory, ILogger<RepositoryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("PRFactoryApi");
    }

    public async Task<List<Repository>> GetAllRepositoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var repositories = await client.GetFromJsonAsync<List<Repository>>("/api/repositories", ct);
            return repositories ?? new List<Repository>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all repositories");
            throw;
        }
    }

    public async Task<Repository?> GetRepositoryByIdAsync(Guid repositoryId, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            return await client.GetFromJsonAsync<Repository>($"/api/repositories/{repositoryId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Repository {RepositoryId} not found", repositoryId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<Repository> CreateRepositoryAsync(Repository repository, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("/api/repositories", repository, ct);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<Repository>(ct);
            _logger.LogInformation("Created repository {RepositoryName}", repository.Name);
            return created!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repository {RepositoryName}", repository.Name);
            throw;
        }
    }

    public async Task UpdateRepositoryAsync(Guid repositoryId, Repository repository, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"/api/repositories/{repositoryId}", repository, ct);
            response.EnsureSuccessStatusCode();
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
            var client = CreateClient();
            var response = await client.DeleteAsync($"/api/repositories/{repositoryId}", ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Deleted repository {RepositoryId}", repositoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repository {RepositoryId}", repositoryId);
            throw;
        }
    }
}
