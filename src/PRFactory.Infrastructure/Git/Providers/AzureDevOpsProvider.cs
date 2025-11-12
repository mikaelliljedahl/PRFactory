using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Polly;

namespace PRFactory.Infrastructure.Git.Providers;

/// <summary>
/// Azure DevOps implementation using Azure DevOps SDK
/// </summary>
public class AzureDevOpsProvider : IGitPlatformProvider
{
    private readonly ILogger<AzureDevOpsProvider> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    private Func<Guid, CancellationToken, Task<RepositoryEntity>>? _repositoryGetter;

    public string PlatformName => "AzureDevOps";

    public AzureDevOpsProvider(ILogger<AzureDevOpsProvider> logger)
    {
        _logger = logger;

        // Polly retry policy for transient errors
        _retryPolicy = Policy
            .Handle<VssServiceException>(ex => IsTransientError(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("Azure DevOps API retry attempt {RetryAttempt} after {Delay}s due to: {Exception}",
                        retryAttempt, timespan.TotalSeconds, exception.Message);
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
        var (organization, project, repoName) = ParseAzureDevOpsUrl(repo.CloneUrl);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var connection = CreateConnection(organization, repo.AccessToken);
            var gitClient = connection.GetClient<GitHttpClient>();

            var prToCreate = new GitPullRequest
            {
                SourceRefName = $"refs/heads/{request.SourceBranch}",
                TargetRefName = $"refs/heads/{request.TargetBranch}",
                Title = request.Title,
                Description = request.Description
            };

            _logger.LogInformation("Creating Azure DevOps PR in {Organization}/{Project}/{Repo}: {Title}",
                organization, project, repoName, request.Title);

            // Get repository by name
            var repositories = await gitClient.GetRepositoriesAsync(project, cancellationToken: ct);
            var repository = repositories.FirstOrDefault(r => r.Name == repoName);

            if (repository == null)
            {
                throw new InvalidOperationException($"Repository '{repoName}' not found in project '{project}'");
            }

            var pr = await gitClient.CreatePullRequestAsync(prToCreate, repository.Id, cancellationToken: ct);

            _logger.LogInformation("Created Azure DevOps PR #{Id}: {Title}", pr.PullRequestId, pr.Title);

            var htmlUrl = $"https://dev.azure.com/{organization}/{project}/_git/{repoName}/pullrequest/{pr.PullRequestId}";

            return new PullRequestInfo(
                pr.PullRequestId,
                pr.Url,
                htmlUrl
            );
        });
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (organization, project, repoName) = ParseAzureDevOpsUrl(repo.CloneUrl);

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var connection = CreateConnection(organization, repo.AccessToken);
            var gitClient = connection.GetClient<GitHttpClient>();

            _logger.LogInformation("Adding comment to Azure DevOps PR #{Number} in {Organization}/{Project}/{Repo}",
                pullRequestNumber, organization, project, repoName);

            // Get repository by name
            var repositories = await gitClient.GetRepositoriesAsync(project, cancellationToken: ct);
            var repository = repositories.FirstOrDefault(r => r.Name == repoName);

            if (repository == null)
            {
                throw new InvalidOperationException($"Repository '{repoName}' not found in project '{project}'");
            }

            var thread = new GitPullRequestCommentThread
            {
                Comments = new List<Comment>
                {
                    new Comment { Content = comment }
                },
                Status = CommentThreadStatus.Active
            };

            await gitClient.CreateThreadAsync(thread, repository.Id, pullRequestNumber, cancellationToken: ct);

            _logger.LogInformation("Added comment to Azure DevOps PR #{Number}", pullRequestNumber);
        });
    }

    public async Task<RepositoryInfo> GetRepositoryInfoAsync(
        Guid repositoryId,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (organization, project, repoName) = ParseAzureDevOpsUrl(repo.CloneUrl);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var connection = CreateConnection(organization, repo.AccessToken);
            var gitClient = connection.GetClient<GitHttpClient>();

            var repositories = await gitClient.GetRepositoriesAsync(project, cancellationToken: ct);
            var repository = repositories.FirstOrDefault(r => r.Name == repoName);

            if (repository == null)
            {
                throw new InvalidOperationException($"Repository '{repoName}' not found in project '{project}'");
            }

            return new RepositoryInfo(
                repository.Name,
                repository.DefaultBranch?.Replace("refs/heads/", "") ?? "main",
                repository.RemoteUrl
            );
        });
    }

    public async Task<PullRequestDetails> GetPullRequestDetailsAsync(
        Guid repositoryId,
        int pullRequestNumber,
        CancellationToken ct = default)
    {
        var repo = await GetRepositoryAsync(repositoryId, ct);
        var (organization, project, repoName) = ParseAzureDevOpsUrl(repo.CloneUrl);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var connection = CreateConnection(organization, repo.AccessToken);
            var gitClient = connection.GetClient<GitHttpClient>();

            _logger.LogInformation("Fetching Azure DevOps PR #{Number} details from {Organization}/{Project}/{Repo}",
                pullRequestNumber, organization, project, repoName);

            // Get repository by name
            var repositories = await gitClient.GetRepositoriesAsync(project, cancellationToken: ct);
            var repository = repositories.FirstOrDefault(r => r.Name == repoName);

            if (repository == null)
            {
                throw new InvalidOperationException($"Repository '{repoName}' not found in project '{project}'");
            }

            // Get PR details
            var pr = await gitClient.GetPullRequestAsync(project, repository.Id, pullRequestNumber, cancellationToken: ct);

            // Get PR commits
            var commits = await gitClient.GetPullRequestCommitsAsync(project, repository.Id, pullRequestNumber, cancellationToken: ct);

            // Get PR iteration changes (files changed)
            // Azure DevOps uses iterations to track changes
            var iterations = await gitClient.GetPullRequestIterationsAsync(project, repository.Id, pullRequestNumber, cancellationToken: ct);

            var fileChanges = new List<FileChange>();
            var totalAdditions = 0;
            var totalDeletions = 0;

            // Get changes from the latest iteration
            if (iterations.Any())
            {
                var latestIteration = iterations.OrderByDescending(i => i.Id).First();
                var changes = await gitClient.GetPullRequestIterationChangesAsync(
                    project,
                    repository.Id,
                    pullRequestNumber,
                    latestIteration.Id ?? 0,
                    cancellationToken: ct);

                if (changes?.ChangeEntries != null)
                {
                    foreach (var change in changes.ChangeEntries)
                    {
                        // Azure DevOps doesn't provide line-by-line stats in the same way
                        // We'll use 0 for additions/deletions per file since we'd need to fetch diffs
                        var status = change.ChangeType.ToString();
                        fileChanges.Add(new FileChange(
                            Path: change.Item?.Path ?? "unknown",
                            Status: status,
                            Additions: 0,  // Would need additional API call to get diff stats
                            Deletions: 0,  // Would need additional API call to get diff stats
                            Changes: 0
                        ));
                    }
                }
            }

            // Note: Azure DevOps SDK doesn't provide easy access to total line changes
            // We could calculate this by fetching diffs, but that's expensive
            // For now, we'll report 0 for line stats

            var htmlUrl = $"https://dev.azure.com/{organization}/{project}/_git/{repoName}/pullrequest/{pr.PullRequestId}";

            _logger.LogInformation(
                "Azure DevOps PR #{Number} has {FileCount} files, {CommitCount} commits",
                pullRequestNumber, fileChanges.Count, commits.Count);

            return new PullRequestDetails(
                Number: pr.PullRequestId,
                Url: pr.Url,
                HtmlUrl: htmlUrl,
                Title: pr.Title ?? string.Empty,
                Description: pr.Description ?? string.Empty,
                FilesChangedCount: fileChanges.Count,
                LinesAdded: totalAdditions,
                LinesDeleted: totalDeletions,
                CommitsCount: commits.Count,
                FilesChanged: fileChanges
            );
        });
    }

    private VssConnection CreateConnection(string organization, string accessToken)
    {
        var credentials = new VssBasicCredential(string.Empty, accessToken);
        var uri = new Uri($"https://dev.azure.com/{organization}");
        return new VssConnection(uri, credentials);
    }

    private (string org, string project, string repo) ParseAzureDevOpsUrl(string url)
    {
        // Parse: https://dev.azure.com/org/project/_git/repo
        // or: https://org@dev.azure.com/org/project/_git/repo
        var uri = new Uri(url.Replace(".git", ""));
        var segments = uri.AbsolutePath.Trim('/').Split('/');

        if (segments.Length < 4)
        {
            throw new ArgumentException($"Invalid Azure DevOps URL format: {url}. Expected format: https://dev.azure.com/org/project/_git/repo");
        }

        // segments[0] = org
        // segments[1] = project
        // segments[2] = _git
        // segments[3] = repo
        return (segments[0], segments[1], segments[3]);
    }

    private bool IsTransientError(VssServiceException ex)
    {
        // Retry on server errors and timeouts
        // Note: VssServiceException doesn't expose HttpStatusCode directly
        // Check exception message or InnerException for specific status codes
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("503") || message.Contains("service unavailable") ||
               message.Contains("504") || message.Contains("gateway timeout") ||
               message.Contains("408") || message.Contains("request timeout") ||
               message.Contains("429") || message.Contains("too many requests");
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
