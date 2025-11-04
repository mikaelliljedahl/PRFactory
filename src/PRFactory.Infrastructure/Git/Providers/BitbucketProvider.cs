using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly;

namespace PRFactory.Infrastructure.Git.Providers;

/// <summary>
/// Bitbucket DTOs for API responses
/// </summary>
public class BitbucketPullRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public BitbucketLinks Links { get; set; } = new();
}

public class BitbucketLinks
{
    public BitbucketLink Self { get; set; } = new();
    public BitbucketLink Html { get; set; } = new();
}

public class BitbucketLink
{
    public string Href { get; set; } = string.Empty;
}

public class BitbucketRepository
{
    public string Name { get; set; } = string.Empty;
    public BitbucketMainBranch? Mainbranch { get; set; }
    public BitbucketLinks Links { get; set; } = new();
}

public class BitbucketMainBranch
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Bitbucket implementation using HttpClient and REST API
/// </summary>
public class BitbucketProvider : IGitPlatformProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitbucketProvider> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    private Func<Guid, CancellationToken, Task<RepositoryEntity>>? _repositoryGetter;

    public string PlatformName => "Bitbucket";

    public BitbucketProvider(HttpClient httpClient, ILogger<BitbucketProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Polly retry policy for transient HTTP errors
        _retryPolicy = Policy<HttpResponseMessage>
            .HandleResult(r => !r.IsSuccessStatusCode && IsTransientStatusCode(r.StatusCode))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("Bitbucket API retry attempt {RetryAttempt} after {Delay}s. Status: {StatusCode}",
                        retryAttempt, timespan.TotalSeconds, outcome.Result?.StatusCode);
                }
            );
    }

    public void SetRepositoryGetter(Func<Guid, CancellationToken, Task<RepositoryEntity>> getter)
    {
        _repositoryGetter = getter;
    }

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (workspace, repoSlug) = ParseBitbucketUrl(repo.CloneUrl);

        var prRequest = new
        {
            title = request.Title,
            description = request.Description,
            source = new { branch = new { name = request.SourceBranch } },
            destination = new { branch = new { name = request.TargetBranch } },
            close_source_branch = false
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", repo.AccessToken);

        _logger.LogInformation("Creating Bitbucket PR in {Workspace}/{Repo}: {Title}",
            workspace, repoSlug, request.Title);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.PostAsJsonAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests",
                prRequest,
                ct
            );
        });

        response.EnsureSuccessStatusCode();

        var pr = await response.Content.ReadFromJsonAsync<BitbucketPullRequest>(cancellationToken: ct);

        if (pr == null)
        {
            throw new InvalidOperationException("Failed to deserialize Bitbucket PR response");
        }

        _logger.LogInformation("Created Bitbucket PR #{Id}: {Title}", pr.Id, pr.Title);

        return new PullRequestInfo(pr.Id, pr.Links.Self.Href, pr.Links.Html.Href);
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (workspace, repoSlug) = ParseBitbucketUrl(repo.CloneUrl);

        var commentRequest = new
        {
            content = new { raw = comment }
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", repo.AccessToken);

        _logger.LogInformation("Adding comment to Bitbucket PR #{Number} in {Workspace}/{Repo}",
            pullRequestNumber, workspace, repoSlug);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.PostAsJsonAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestNumber}/comments",
                commentRequest,
                ct
            );
        });

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Added comment to Bitbucket PR #{Number}", pullRequestNumber);
    }

    public async Task<RepositoryInfo> GetRepositoryInfoAsync(
        Guid repositoryId,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (workspace, repoSlug) = ParseBitbucketUrl(repo.CloneUrl);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", repo.AccessToken);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.GetAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}",
                ct
            );
        });

        response.EnsureSuccessStatusCode();

        var bbRepo = await response.Content.ReadFromJsonAsync<BitbucketRepository>(cancellationToken: ct);

        if (bbRepo == null)
        {
            throw new InvalidOperationException("Failed to deserialize Bitbucket repository response");
        }

        return new RepositoryInfo(
            bbRepo.Name,
            bbRepo.Mainbranch?.Name ?? "main",
            repo.CloneUrl
        );
    }

    private (string workspace, string repoSlug) ParseBitbucketUrl(string url)
    {
        // Parse: https://bitbucket.org/workspace/repo.git
        var uri = new Uri(url.Replace(".git", ""));
        var segments = uri.AbsolutePath.Trim('/').Split('/');

        if (segments.Length < 2)
        {
            throw new ArgumentException($"Invalid Bitbucket URL format: {url}");
        }

        return (segments[0], segments[1]);
    }

    private bool IsTransientStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.TooManyRequests ||
               statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               statusCode == System.Net.HttpStatusCode.GatewayTimeout ||
               statusCode == System.Net.HttpStatusCode.RequestTimeout;
    }

    private async Task<RepositoryEntity> GetRepositoryAsync(Guid repositoryId, CancellationToken ct)
    {
        if (_repositoryGetter == null)
        {
            throw new InvalidOperationException(
                "Repository getter not configured. Call SetRepositoryGetter() first.");
        }

        return await _repositoryGetter(repositoryId, ct);
    }
}
