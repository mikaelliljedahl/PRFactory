using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Git;

/// <summary>
/// Core interface for git platform operations used by the application layer
/// This is the main facade that coordinates local git operations and platform-specific API calls
/// </summary>
public interface IGitPlatformService
{
    /// <summary>
    /// Clone repository to local workspace (with caching)
    /// </summary>
    Task<string> CloneRepositoryAsync(Guid repositoryId, CancellationToken ct = default);

    /// <summary>
    /// Create a new branch from default branch
    /// </summary>
    Task<string> CreateBranchAsync(
        Guid repositoryId,
        string branchName,
        string? fromBranch = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Commit one or more files to a branch
    /// </summary>
    Task CommitFilesAsync(
        Guid repositoryId,
        string branchName,
        Dictionary<string, string> files,  // Path -> Content
        string commitMessage,
        CancellationToken ct = default
    );

    /// <summary>
    /// Push local branch to remote
    /// </summary>
    Task PushBranchAsync(
        Guid repositoryId,
        string branchName,
        CancellationToken ct = default
    );

    /// <summary>
    /// Create a pull request
    /// </summary>
    Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default
    );

    /// <summary>
    /// Add a comment to an existing pull request
    /// </summary>
    Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default
    );

    /// <summary>
    /// Get repository information
    /// </summary>
    Task<RepositoryInfo> GetRepositoryInfoAsync(
        Guid repositoryId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Get pull request details including files changed, commits, and statistics
    /// </summary>
    Task<PullRequestDetails> GetPullRequestDetailsAsync(
        Guid repositoryId,
        int pullRequestNumber,
        CancellationToken ct = default
    );
}

/// <summary>
/// Facade service that coordinates local git operations and platform-specific APIs
/// Implements repository caching and platform selection logic
/// </summary>
public class GitPlatformService : IGitPlatformService
{
    private readonly ILocalGitService _localGitService;
    private readonly IEnumerable<IGitPlatformProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GitPlatformService> _logger;

    // In production, this would be injected as IRepositoryRepository
    private readonly Func<Guid, CancellationToken, Task<Providers.RepositoryEntity>> _repositoryGetter;

    public GitPlatformService(
        ILocalGitService localGitService,
        IEnumerable<IGitPlatformProvider> providers,
        IMemoryCache cache,
        ILogger<GitPlatformService> logger,
        Func<Guid, CancellationToken, Task<Providers.RepositoryEntity>> repositoryGetter)
    {
        _localGitService = localGitService;
        _providers = providers;
        _cache = cache;
        _logger = logger;
        _repositoryGetter = repositoryGetter;

        // Configure repository getter for all providers
        foreach (var provider in _providers)
        {
            ConfigureProvider(provider);
        }
    }

    public async Task<string> CloneRepositoryAsync(Guid repositoryId, CancellationToken ct = default)
    {
        var cacheKey = $"repo:path:{repositoryId}";

        // Check cache first
        if (_cache.TryGetValue<string>(cacheKey, out var cachedPath) &&
            !string.IsNullOrEmpty(cachedPath) &&
            Directory.Exists(cachedPath))
        {
            _logger.LogInformation("Using cached repository at {Path}", cachedPath);
            return cachedPath;
        }

        var repo = await _repositoryGetter(repositoryId, ct);
        var localPath = await _localGitService.CloneAsync(repo.CloneUrl, repo.AccessToken, ct);

        // Cache path for 1 hour
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _cache.Set(cacheKey, localPath, cacheOptions);

        _logger.LogInformation("Repository {RepositoryId} cloned to {Path}", repositoryId, localPath);

        return localPath;
    }

    public async Task<string> CreateBranchAsync(
        Guid repositoryId,
        string branchName,
        string? fromBranch = null,
        CancellationToken ct = default)
    {
        var repoPath = await CloneRepositoryAsync(repositoryId, ct);
        var repo = await _repositoryGetter(repositoryId, ct);

        fromBranch ??= repo.DefaultBranch;

        _logger.LogInformation("Creating branch {BranchName} from {FromBranch} in repository {RepositoryId}",
            branchName, fromBranch, repositoryId);

        return await _localGitService.CreateBranchAsync(repoPath, branchName, fromBranch);
    }

    public async Task CommitFilesAsync(
        Guid repositoryId,
        string branchName,
        Dictionary<string, string> files,
        string commitMessage,
        CancellationToken ct = default)
    {
        var repoPath = await CloneRepositoryAsync(repositoryId, ct);

        _logger.LogInformation("Committing {FileCount} files to branch {BranchName} in repository {RepositoryId}",
            files.Count, branchName, repositoryId);

        await _localGitService.CommitAsync(
            repoPath,
            files,
            commitMessage,
            "Claude AI <claude@prfactory.ai>"
        );
    }

    public async Task PushBranchAsync(
        Guid repositoryId,
        string branchName,
        CancellationToken ct = default)
    {
        var repoPath = await CloneRepositoryAsync(repositoryId, ct);
        var repo = await _repositoryGetter(repositoryId, ct);

        _logger.LogInformation("Pushing branch {BranchName} for repository {RepositoryId}",
            branchName, repositoryId);

        await _localGitService.PushAsync(repoPath, branchName, repo.AccessToken);
    }

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = await _repositoryGetter(repositoryId, ct);
        var provider = GetProvider(repo.GitPlatform);

        _logger.LogInformation("Creating pull request for repository {RepositoryId} using {Platform} provider",
            repositoryId, provider.PlatformName);

        return await provider.CreatePullRequestAsync(repositoryId, request, ct);
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await _repositoryGetter(repositoryId, ct);
        var provider = GetProvider(repo.GitPlatform);

        _logger.LogInformation("Adding comment to PR #{PullRequestNumber} for repository {RepositoryId} using {Platform}",
            pullRequestNumber, repositoryId, provider.PlatformName);

        await provider.AddPullRequestCommentAsync(repositoryId, pullRequestNumber, comment, ct);
    }

    public async Task<RepositoryInfo> GetRepositoryInfoAsync(
        Guid repositoryId,
        CancellationToken ct = default)
    {
        var repo = await _repositoryGetter(repositoryId, ct);
        var provider = GetProvider(repo.GitPlatform);

        _logger.LogInformation("Getting repository info for {RepositoryId} using {Platform}",
            repositoryId, provider.PlatformName);

        return await provider.GetRepositoryInfoAsync(repositoryId, ct);
    }

    public async Task<PullRequestDetails> GetPullRequestDetailsAsync(
        Guid repositoryId,
        int pullRequestNumber,
        CancellationToken ct = default)
    {
        var repo = await _repositoryGetter(repositoryId, ct);
        var provider = GetProvider(repo.GitPlatform);

        _logger.LogInformation("Getting PR #{PullRequestNumber} details for {RepositoryId} using {Platform}",
            pullRequestNumber, repositoryId, provider.PlatformName);

        return await provider.GetPullRequestDetailsAsync(repositoryId, pullRequestNumber, ct);
    }

    private IGitPlatformProvider GetProvider(string platformName)
    {
        var provider = _providers.FirstOrDefault(p =>
            p.PlatformName.Equals(platformName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            throw new NotSupportedException(
                $"Git platform '{platformName}' is not supported. " +
                $"Available platforms: {string.Join(", ", _providers.Select(p => p.PlatformName))}");
        }

        return provider;
    }

    private void ConfigureProvider(IGitPlatformProvider provider)
    {
        // Use reflection to call SetRepositoryGetter if it exists
        var method = provider.GetType().GetMethod("SetRepositoryGetter");
        if (method != null)
        {
            method.Invoke(provider, new object[] { _repositoryGetter });
        }
    }
}
