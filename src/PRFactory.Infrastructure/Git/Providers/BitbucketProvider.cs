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

public class BitbucketPullRequestDetails
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BitbucketLinks Links { get; set; } = new();
}

public class BitbucketDiffstatResponse
{
    public List<BitbucketDiffstat>? Values { get; set; }
}

public class BitbucketDiffstat
{
    public string Status { get; set; } = string.Empty;
    public int LinesAdded { get; set; }
    public int LinesRemoved { get; set; }
    public BitbucketFile? Old { get; set; }
    public BitbucketFile? New { get; set; }
}

public class BitbucketFile
{
    public string Path { get; set; } = string.Empty;
}

public class BitbucketCommitsResponse
{
    public int Size { get; set; }
}

/// <summary>
/// Bitbucket implementation using HttpClient and REST API
/// </summary>
public class BitbucketProvider : IGitPlatformProvider
{
    private const string BearerPrefix = "Bearer";
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
            new AuthenticationHeaderValue(BearerPrefix, repo.AccessToken);

        _logger.LogInformation("Creating Bitbucket PR in {Workspace}/{Repo}: {Title}",
            workspace, repoSlug, request.Title);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var content = JsonContent.Create(prRequest);
            return await _httpClient.PostAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests",
                content,
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
            new AuthenticationHeaderValue(BearerPrefix, repo.AccessToken);

        _logger.LogInformation("Adding comment to Bitbucket PR #{Number} in {Workspace}/{Repo}",
            pullRequestNumber, workspace, repoSlug);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var content = JsonContent.Create(commentRequest);
            return await _httpClient.PostAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestNumber}/comments",
                content,
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
            new AuthenticationHeaderValue(BearerPrefix, repo.AccessToken);

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

    public async Task<PullRequestDetails> GetPullRequestDetailsAsync(
        Guid repositoryId,
        int pullRequestNumber,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (workspace, repoSlug) = ParseBitbucketUrl(repo.CloneUrl);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(BearerPrefix, repo.AccessToken);

        _logger.LogInformation("Fetching Bitbucket PR #{Number} details from {Workspace}/{Repo}",
            pullRequestNumber, workspace, repoSlug);

        // Get PR basic info
        var prResponse = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.GetAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestNumber}",
                ct
            );
        });

        prResponse.EnsureSuccessStatusCode();
        var pr = await prResponse.Content.ReadFromJsonAsync<BitbucketPullRequestDetails>(cancellationToken: ct);

        if (pr == null)
        {
            throw new InvalidOperationException("Failed to deserialize Bitbucket PR response");
        }

        // Get PR diffstat for file changes
        var diffstatResponse = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.GetAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestNumber}/diffstat",
                ct
            );
        });

        diffstatResponse.EnsureSuccessStatusCode();
        var diffstat = await diffstatResponse.Content.ReadFromJsonAsync<BitbucketDiffstatResponse>(cancellationToken: ct);

        // Get PR commits
        var commitsResponse = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _httpClient.GetAsync(
                $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestNumber}/commits",
                ct
            );
        });

        commitsResponse.EnsureSuccessStatusCode();
        var commits = await commitsResponse.Content.ReadFromJsonAsync<BitbucketCommitsResponse>(cancellationToken: ct);

        // Map files to FileChange records
        var fileChanges = diffstat?.Values?.Select(f => new FileChange(
            Path: f.Old?.Path ?? f.New?.Path ?? "unknown",
            Status: f.Status,
            Additions: f.LinesAdded,
            Deletions: f.LinesRemoved,
            Changes: f.LinesAdded + f.LinesRemoved
        )).ToList() ?? new List<FileChange>();

        // Calculate statistics
        var totalAdditions = fileChanges.Sum(f => f.Additions);
        var totalDeletions = fileChanges.Sum(f => f.Deletions);
        var commitCount = commits?.Size ?? 0;

        _logger.LogInformation(
            "Bitbucket PR #{Number} has {FileCount} files, {Additions} additions, {Deletions} deletions, {CommitCount} commits",
            pullRequestNumber, fileChanges.Count, totalAdditions, totalDeletions, commitCount);

        return new PullRequestDetails(
            Number: pr.Id,
            Url: pr.Links.Self.Href,
            HtmlUrl: pr.Links.Html.Href,
            Title: pr.Title,
            Description: pr.Description ?? string.Empty,
            FilesChangedCount: fileChanges.Count,
            LinesAdded: totalAdditions,
            LinesDeleted: totalDeletions,
            CommitsCount: commitCount,
            FilesChanged: fileChanges
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
