# Git Platform Integration Architecture

## Overview

The Git integration provides an **abstraction layer** that enables PRFactory to work seamlessly with multiple git platforms (GitHub, Bitbucket, Azure DevOps) while keeping the core workflow engine platform-agnostic.

## Design Principles

1. **Platform Abstraction**: Core logic doesn't know about specific git platforms
2. **Extensibility**: New platforms can be added without modifying existing code
3. **Local-First**: Use LibGit2Sharp for local git operations (clone, branch, commit)
4. **API for PRs**: Use platform-specific APIs for pull request creation
5. **Unified Interface**: Consistent API regardless of underlying platform

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Core Workflow Engine                       │
│           (Platform Agnostic)                               │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          IGitPlatformService (Interface)                    │
│  • CloneRepositoryAsync()                                   │
│  • CreateBranchAsync()                                      │
│  • CommitFilesAsync()                                       │
│  • PushBranchAsync()                                        │
│  • CreatePullRequestAsync()                                 │
│  • AddPullRequestCommentAsync()                             │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
┌─────────────┐ ┌─────────┐ ┌──────────────┐
│   GitHub    │ │Bitbucket│ │ Azure DevOps │
│  Provider   │ │Provider │ │  Provider    │
└──────┬──────┘ └────┬────┘ └──────┬───────┘
       │             │             │
       │  All use    │             │
       └─────────────┼─────────────┘
                     ▼
         ┌───────────────────────┐
         │  LocalGitService      │
         │  (LibGit2Sharp)       │
         │  • Local git ops      │
         └───────────────────────┘
```

## Core Abstraction

### IGitPlatformService Interface

```csharp
// PRFactory.Core/Domain/Interfaces/IGitPlatformService.cs
public interface IGitPlatformService
{
    /// <summary>
    /// Clone repository to local workspace
    /// </summary>
    Task<string> CloneRepositoryAsync(
        Guid repositoryId,
        CancellationToken ct = default
    );

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
}

// DTOs
public record CreatePullRequestRequest(
    string SourceBranch,
    string TargetBranch,
    string Title,
    string Description
);

public record PullRequestInfo(
    int Number,
    string Url,
    string HtmlUrl
);

public record RepositoryInfo(
    string Name,
    string DefaultBranch,
    string CloneUrl
);
```

## Local Git Service (LibGit2Sharp)

Handles all local git operations (clone, checkout, commit, push). This is shared by all platform providers.

```csharp
// PRFactory.Infrastructure/Git/LocalGitService.cs
public interface ILocalGitService
{
    Task<string> CloneAsync(string repoUrl, string accessToken, CancellationToken ct = default);
    Task<string> CreateBranchAsync(string repoPath, string branchName, string fromBranch);
    Task CommitAsync(string repoPath, Dictionary<string, string> files, string message, string author);
    Task PushAsync(string repoPath, string branchName, string accessToken);
    Task<bool> BranchExistsAsync(string repoPath, string branchName);
    string GetDefaultBranch(string repoPath);
}

public class LocalGitService : ILocalGitService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalGitService> _logger;
    private readonly string _workspaceBasePath;

    public LocalGitService(IConfiguration configuration, ILogger<LocalGitService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _workspaceBasePath = configuration["Workspace:BasePath"] ?? "/var/prfactory/workspace";
    }

    public async Task<string> CloneAsync(string repoUrl, string accessToken, CancellationToken ct = default)
    {
        var repoName = ExtractRepoName(repoUrl);
        var localPath = Path.Combine(_workspaceBasePath, Guid.NewGuid().ToString(), repoName);

        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        _logger.LogInformation("Cloning repository {RepoUrl} to {LocalPath}", repoUrl, localPath);

        var cloneOptions = new CloneOptions
        {
            CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
            {
                Username = "oauth2",
                Password = accessToken
            },
            IsBare = false,
            Checkout = true
        };

        await Task.Run(() => LibGit2Sharp.Repository.Clone(repoUrl, localPath, cloneOptions), ct);

        _logger.LogInformation("Repository cloned successfully to {LocalPath}", localPath);

        return localPath;
    }

    public async Task<string> CreateBranchAsync(string repoPath, string branchName, string fromBranch)
    {
        using var repo = new LibGit2Sharp.Repository(repoPath);

        // Find base branch
        var baseBranch = repo.Branches[fromBranch] ?? repo.Branches[$"origin/{fromBranch}"];
        if (baseBranch == null)
        {
            throw new InvalidOperationException($"Base branch '{fromBranch}' not found");
        }

        // Create new branch
        var newBranch = repo.CreateBranch(branchName, baseBranch.Tip);

        // Checkout new branch
        Commands.Checkout(repo, newBranch);

        _logger.LogInformation("Created and checked out branch {BranchName} from {FromBranch}",
            branchName, fromBranch);

        return branchName;
    }

    public async Task CommitAsync(
        string repoPath,
        Dictionary<string, string> files,
        string message,
        string author)
    {
        using var repo = new LibGit2Sharp.Repository(repoPath);

        // Write files to disk
        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(repoPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);

            // Stage file
            Commands.Stage(repo, relativePath);
        }

        // Commit
        var signature = new Signature(author, "claude@prfactory.ai", DateTimeOffset.Now);
        var commit = repo.Commit(message, signature, signature);

        _logger.LogInformation("Committed {FileCount} files with message: {Message}",
            files.Count, message);
    }

    public async Task PushAsync(string repoPath, string branchName, string accessToken)
    {
        using var repo = new LibGit2Sharp.Repository(repoPath);

        var branch = repo.Branches[branchName];
        if (branch == null)
        {
            throw new InvalidOperationException($"Branch '{branchName}' not found");
        }

        var pushOptions = new PushOptions
        {
            CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
            {
                Username = "oauth2",
                Password = accessToken
            }
        };

        var remote = repo.Network.Remotes["origin"];
        var pushRefSpec = $"refs/heads/{branchName}:refs/heads/{branchName}";

        _logger.LogInformation("Pushing branch {BranchName} to remote", branchName);

        repo.Network.Push(remote, pushRefSpec, pushOptions);

        _logger.LogInformation("Branch {BranchName} pushed successfully", branchName);
    }

    public async Task<bool> BranchExistsAsync(string repoPath, string branchName)
    {
        using var repo = new LibGit2Sharp.Repository(repoPath);
        return repo.Branches[branchName] != null;
    }

    public string GetDefaultBranch(string repoPath)
    {
        using var repo = new LibGit2Sharp.Repository(repoPath);
        return repo.Head.FriendlyName;
    }

    private string ExtractRepoName(string repoUrl)
    {
        var uri = new Uri(repoUrl.Replace(".git", ""));
        return uri.Segments.Last();
    }
}
```

## Platform Providers

### GitHub Provider

```csharp
// PRFactory.Infrastructure/Git/Providers/GitHubProvider.cs
public class GitHubProvider : IGitPlatformProvider
{
    private readonly ILocalGitService _localGitService;
    private readonly IRepositoryRepository _repoRepository;
    private readonly ILogger<GitHubProvider> _logger;

    public string PlatformName => "GitHub";

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var (owner, repoName) = ParseGitHubUrl(repo.CloneUrl);

        var client = new GitHubClient(new ProductHeaderValue("PRFactory"));
        client.Credentials = new Credentials(repo.AccessToken);

        var newPr = new NewPullRequest(request.Title, request.SourceBranch, request.TargetBranch)
        {
            Body = request.Description
        };

        var pr = await client.PullRequest.Create(owner, repoName, newPr);

        _logger.LogInformation("Created GitHub PR #{Number}: {Title}", pr.Number, pr.Title);

        return new PullRequestInfo(pr.Number, pr.Url, pr.HtmlUrl);
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var (owner, repoName) = ParseGitHubUrl(repo.CloneUrl);

        var client = new GitHubClient(new ProductHeaderValue("PRFactory"));
        client.Credentials = new Credentials(repo.AccessToken);

        await client.Issue.Comment.Create(owner, repoName, pullRequestNumber, comment);

        _logger.LogInformation("Added comment to GitHub PR #{Number}", pullRequestNumber);
    }

    private (string owner, string repo) ParseGitHubUrl(string url)
    {
        // Parse: https://github.com/owner/repo.git
        var uri = new Uri(url.Replace(".git", ""));
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        return (segments[0], segments[1]);
    }
}
```

### Bitbucket Provider

```csharp
// PRFactory.Infrastructure/Git/Providers/BitbucketProvider.cs
public class BitbucketProvider : IGitPlatformProvider
{
    private readonly ILocalGitService _localGitService;
    private readonly IRepositoryRepository _repoRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitbucketProvider> _logger;

    public string PlatformName => "Bitbucket";

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
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

        var response = await _httpClient.PostAsJsonAsync(
            $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests",
            prRequest,
            ct
        );

        response.EnsureSuccessStatusCode();

        var pr = await response.Content.ReadFromJsonAsync<BitbucketPullRequest>(cancellationToken: ct);

        _logger.LogInformation("Created Bitbucket PR #{Id}: {Title}", pr!.Id, pr.Title);

        return new PullRequestInfo(pr.Id, pr.Links.Self.Href, pr.Links.Html.Href);
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var (workspace, repoSlug) = ParseBitbucketUrl(repo.CloneUrl);

        var commentRequest = new { content = new { raw = comment } };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", repo.AccessToken);

        await _httpClient.PostAsJsonAsync(
            $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestNumber}/comments",
            commentRequest,
            ct
        );

        _logger.LogInformation("Added comment to Bitbucket PR #{Number}", pullRequestNumber);
    }

    private (string workspace, string repoSlug) ParseBitbucketUrl(string url)
    {
        // Parse: https://bitbucket.org/workspace/repo.git
        var uri = new Uri(url.Replace(".git", ""));
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        return (segments[0], segments[1]);
    }
}
```

### Azure DevOps Provider

```csharp
// PRFactory.Infrastructure/Git/Providers/AzureDevOpsProvider.cs
public class AzureDevOpsProvider : IGitPlatformProvider
{
    private readonly ILocalGitService _localGitService;
    private readonly IRepositoryRepository _repoRepository;
    private readonly ILogger<AzureDevOpsProvider> _logger;

    public string PlatformName => "AzureDevOps";

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var (organization, project, repoName) = ParseAzureDevOpsUrl(repo.CloneUrl);

        var connection = new VssConnection(
            new Uri($"https://dev.azure.com/{organization}"),
            new VssBasicCredential(string.Empty, repo.AccessToken)
        );

        var gitClient = connection.GetClient<GitHttpClient>();

        var prToCreate = new GitPullRequest
        {
            SourceRefName = $"refs/heads/{request.SourceBranch}",
            TargetRefName = $"refs/heads/{request.TargetBranch}",
            Title = request.Title,
            Description = request.Description
        };

        var repositories = await gitClient.GetRepositoriesAsync(project);
        var repository = repositories.First(r => r.Name == repoName);

        var pr = await gitClient.CreatePullRequestAsync(prToCreate, repository.Id);

        _logger.LogInformation("Created Azure DevOps PR #{Id}: {Title}", pr.PullRequestId, pr.Title);

        return new PullRequestInfo(
            pr.PullRequestId,
            pr.Url,
            $"https://dev.azure.com/{organization}/{project}/_git/{repoName}/pullrequest/{pr.PullRequestId}"
        );
    }

    public async Task AddPullRequestCommentAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var (organization, project, repoName) = ParseAzureDevOpsUrl(repo.CloneUrl);

        var connection = new VssConnection(
            new Uri($"https://dev.azure.com/{organization}"),
            new VssBasicCredential(string.Empty, repo.AccessToken)
        );

        var gitClient = connection.GetClient<GitHttpClient>();

        var repositories = await gitClient.GetRepositoriesAsync(project);
        var repository = repositories.First(r => r.Name == repoName);

        var thread = new GitPullRequestCommentThread
        {
            Comments = new List<Comment>
            {
                new Comment { Content = comment }
            },
            Status = CommentThreadStatus.Active
        };

        await gitClient.CreateThreadAsync(thread, repository.Id, pullRequestNumber);

        _logger.LogInformation("Added comment to Azure DevOps PR #{Number}", pullRequestNumber);
    }

    private (string org, string project, string repo) ParseAzureDevOpsUrl(string url)
    {
        // Parse: https://dev.azure.com/org/project/_git/repo
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        return (segments[0], segments[1], segments[3]);
    }
}
```

## Git Platform Service (Facade)

Coordinates between local git operations and platform-specific APIs.

```csharp
// PRFactory.Infrastructure/Git/GitPlatformService.cs
public class GitPlatformService : IGitPlatformService
{
    private readonly ILocalGitService _localGitService;
    private readonly IRepositoryRepository _repoRepository;
    private readonly IEnumerable<IGitPlatformProvider> _providers;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<GitPlatformService> _logger;

    public async Task<string> CloneRepositoryAsync(Guid repositoryId, CancellationToken ct = default)
    {
        // Check cache first
        var cachedPath = await _cache.GetAsync<string>($"repo:path:{repositoryId}");
        if (!string.IsNullOrEmpty(cachedPath) && Directory.Exists(cachedPath))
        {
            _logger.LogInformation("Using cached repository at {Path}", cachedPath);
            return cachedPath;
        }

        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var localPath = await _localGitService.CloneAsync(repo.CloneUrl, repo.AccessToken, ct);

        // Cache path for 1 hour
        await _cache.SetAsync($"repo:path:{repositoryId}", localPath, TimeSpan.FromHours(1));

        return localPath;
    }

    public async Task<string> CreateBranchAsync(
        Guid repositoryId,
        string branchName,
        string? fromBranch = null,
        CancellationToken ct = default)
    {
        var repoPath = await CloneRepositoryAsync(repositoryId, ct);
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);

        fromBranch ??= repo.DefaultBranch;

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
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);

        await _localGitService.PushAsync(repoPath, branchName, repo.AccessToken);
    }

    public async Task<PullRequestInfo> CreatePullRequestAsync(
        Guid repositoryId,
        CreatePullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = await _repoRepository.GetByIdAsync(repositoryId, ct);
        var provider = GetProvider(repo.GitPlatform);

        return await provider.CreatePullRequestAsync(repositoryId, request, ct);
    }

    private IGitPlatformProvider GetProvider(string platformName)
    {
        var provider = _providers.FirstOrDefault(p => p.PlatformName == platformName);
        if (provider == null)
        {
            throw new NotSupportedException($"Git platform '{platformName}' is not supported");
        }
        return provider;
    }
}
```

## Configuration & DI Registration

```csharp
// Program.cs
services.AddScoped<ILocalGitService, LocalGitService>();

// Register all platform providers
services.AddScoped<IGitPlatformProvider, GitHubProvider>();
services.AddScoped<IGitPlatformProvider, BitbucketProvider>();
services.AddScoped<IGitPlatformProvider, AzureDevOpsProvider>();

// Register facade
services.AddScoped<IGitPlatformService, GitPlatformService>();

// HTTP client for Bitbucket
services.AddHttpClient<BitbucketProvider>()
    .AddPolicyHandler(ResiliencePolicies.GetHttpRetryPolicy());
```

## Workspace Management

### Cleanup Strategy

```csharp
// Background job to cleanup old workspaces
public class WorkspaceCleanupJob
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WorkspaceCleanupJob> _logger;

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync()
    {
        var workspacePath = _configuration["Workspace:BasePath"];
        var maxAge = TimeSpan.FromDays(7);

        var directories = Directory.GetDirectories(workspacePath);

        foreach (var dir in directories)
        {
            var dirInfo = new DirectoryInfo(dir);
            if (DateTime.UtcNow - dirInfo.LastAccessTime > maxAge)
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    _logger.LogInformation("Deleted old workspace: {Path}", dir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete workspace: {Path}", dir);
                }
            }
        }
    }
}

// Schedule daily
RecurringJob.AddOrUpdate<WorkspaceCleanupJob>(
    "workspace-cleanup",
    job => job.ExecuteAsync(),
    Cron.Daily
);
```

## Security Considerations

### Access Tokens
- Store encrypted in database
- Use minimal scopes (read for clone, write for push, PR creation)
- Support token rotation

### Workspace Isolation
- Each ticket gets isolated workspace
- Prevent path traversal attacks
- Clean up after processing

### Git Operations
- Never execute arbitrary git commands from user input
- Sanitize branch names
- Validate commit messages

## Testing

### Unit Tests
```csharp
[Fact]
public async Task CreateBranch_ShouldCreateFromDefaultBranch()
{
    // Arrange
    var mockLocalGit = new Mock<ILocalGitService>();
    var service = new GitPlatformService(mockLocalGit.Object, ...);

    // Act
    await service.CreateBranchAsync(repoId, "feature/test");

    // Assert
    mockLocalGit.Verify(x => x.CreateBranchAsync(It.IsAny<string>(), "feature/test", "main"), Times.Once);
}
```

### Integration Tests
- Use test repositories on each platform
- Verify end-to-end: clone → branch → commit → push → PR

## Performance Optimizations

1. **Repository Caching**: Keep cloned repos for 1 hour
2. **Shallow Clones**: Clone with `--depth 1` for analysis
3. **Incremental Fetch**: Only fetch new commits if repo cached
4. **Parallel Operations**: Clone multiple repos in parallel

## Monitoring

### Key Metrics
- Clone duration
- Push success/failure rate
- PR creation success rate
- Workspace disk usage

### Alerts
- Failed git operations
- Workspace disk usage > 80%
- Token expiration warnings

## Next Steps

Review the Claude AI integration:
- [Claude AI Integration](./claude-integration.md)
