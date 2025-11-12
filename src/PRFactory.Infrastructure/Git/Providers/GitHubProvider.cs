using Microsoft.Extensions.Logging;
using Octokit;
using Polly;
using Polly.Extensions.Http;

namespace PRFactory.Infrastructure.Git.Providers;

/// <summary>
/// GitHub implementation using Octokit
/// </summary>
public class GitHubProvider : IGitPlatformProvider
{
    private readonly ILogger<GitHubProvider> _logger;
    private readonly IAsyncPolicy<object> _retryPolicy;

    // This will be injected from the calling service
    // In a real implementation, we'd have a repository repository to fetch repository details
    private Func<Guid, CancellationToken, Task<RepositoryEntity>>? _repositoryGetter;

    public string PlatformName => "GitHub";

    public GitHubProvider(ILogger<GitHubProvider> logger)
    {
        _logger = logger;

        // Polly retry policy for transient errors
        _retryPolicy = Policy<object>
            .Handle<ApiException>(ex => IsTransientError(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("GitHub API retry attempt {RetryAttempt} after {Delay}s due to: {Exception}",
                        retryAttempt, timespan.TotalSeconds, outcome.Exception?.Message);
                }
            );
    }

    /// <summary>
    /// Set the repository getter function (dependency injection alternative)
    /// In production, this would be a proper repository interface
    /// </summary>
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
        var (owner, repoName) = ParseGitHubUrl(repo.CloneUrl);

        return (PullRequestInfo)await _retryPolicy.ExecuteAsync(async () =>
        {
            var client = CreateGitHubClient(repo.AccessToken);

            var newPr = new NewPullRequest(request.Title, request.SourceBranch, request.TargetBranch)
            {
                Body = request.Description
            };

            _logger.LogInformation("Creating GitHub PR in {Owner}/{Repo}: {Title}",
                owner, repoName, request.Title);

            var pr = await client.PullRequest.Create(owner, repoName, newPr);

            _logger.LogInformation("Created GitHub PR #{Number}: {Title}", pr.Number, pr.Title);

            return new PullRequestInfo(pr.Number, pr.Url, pr.HtmlUrl);
        });
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (owner, repoName) = ParseGitHubUrl(repo.CloneUrl);

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var client = CreateGitHubClient(repo.AccessToken);

            _logger.LogInformation("Adding comment to GitHub PR #{Number} in {Owner}/{Repo}",
                pullRequestNumber, owner, repoName);

            await client.Issue.Comment.Create(owner, repoName, pullRequestNumber, comment);

            _logger.LogInformation("Added comment to GitHub PR #{Number}", pullRequestNumber);

            return Task.FromResult<object>(null!);
        });
    }

    public async Task<RepositoryInfo> GetRepositoryInfoAsync(
        Guid repositoryId,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (owner, repoName) = ParseGitHubUrl(repo.CloneUrl);

        return (RepositoryInfo)await _retryPolicy.ExecuteAsync(async () =>
        {
            var client = CreateGitHubClient(repo.AccessToken);

            var ghRepo = await client.Repository.Get(owner, repoName);

            return new RepositoryInfo(
                ghRepo.Name,
                ghRepo.DefaultBranch,
                ghRepo.CloneUrl
            );
        });
    }

    public async Task<PullRequestDetails> GetPullRequestDetailsAsync(
        Guid repositoryId,
        int pullRequestNumber,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (owner, repoName) = ParseGitHubUrl(repo.CloneUrl);

        return (PullRequestDetails)await _retryPolicy.ExecuteAsync(async () =>
        {
            var client = CreateGitHubClient(repo.AccessToken);

            _logger.LogInformation("Fetching GitHub PR #{Number} details from {Owner}/{Repo}",
                pullRequestNumber, owner, repoName);

            // Get PR basic info
            var pr = await client.PullRequest.Get(owner, repoName, pullRequestNumber);

            // Get PR files
            var files = await client.PullRequest.Files(owner, repoName, pullRequestNumber);

            // Get PR commits
            var commits = await client.PullRequest.Commits(owner, repoName, pullRequestNumber);

            // Map files to FileChange records
            var fileChanges = files.Select(f => new FileChange(
                Path: f.FileName,
                Status: f.Status,
                Additions: f.Additions,
                Deletions: f.Deletions,
                Changes: f.Changes
            )).ToList();

            // Calculate statistics
            var totalAdditions = files.Sum(f => f.Additions);
            var totalDeletions = files.Sum(f => f.Deletions);

            _logger.LogInformation(
                "GitHub PR #{Number} has {FileCount} files, {Additions} additions, {Deletions} deletions, {CommitCount} commits",
                pullRequestNumber, files.Count, totalAdditions, totalDeletions, commits.Count);

            return new PullRequestDetails(
                Number: pr.Number,
                Url: pr.Url,
                HtmlUrl: pr.HtmlUrl,
                Title: pr.Title,
                Description: pr.Body ?? string.Empty,
                FilesChangedCount: files.Count,
                LinesAdded: totalAdditions,
                LinesDeleted: totalDeletions,
                CommitsCount: commits.Count,
                FilesChanged: fileChanges
            );
        });
    }

    private GitHubClient CreateGitHubClient(string accessToken)
    {
        var client = new GitHubClient(new ProductHeaderValue("PRFactory"));
        client.Credentials = new Credentials(accessToken);
        return client;
    }

    private (string owner, string repo) ParseGitHubUrl(string url)
    {
        // Parse: https://github.com/owner/repo.git
        var uri = new Uri(url.Replace(".git", ""));
        var segments = uri.AbsolutePath.Trim('/').Split('/');

        if (segments.Length < 2)
        {
            throw new ArgumentException($"Invalid GitHub URL format: {url}");
        }

        return (segments[0], segments[1]);
    }

    private bool IsTransientError(ApiException ex)
    {
        // Retry on rate limits, server errors, timeouts
        return ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
               ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               ex.StatusCode == System.Net.HttpStatusCode.GatewayTimeout;
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

/// <summary>
/// Temporary entity class - in production this would be in PRFactory.Core/Domain/Entities
/// </summary>
public class RepositoryEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GitPlatform { get; set; } = string.Empty;
    public string CloneUrl { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public string AccessToken { get; set; } = string.Empty;
}
