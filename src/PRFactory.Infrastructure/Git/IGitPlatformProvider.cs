namespace PRFactory.Infrastructure.Git;

/// <summary>
/// DTOs for pull request operations
/// </summary>
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

public record FileChange(
    string Path,
    string Status,
    int Additions,
    int Deletions,
    int Changes
);

public record PullRequestDetails(
    int Number,
    string Url,
    string HtmlUrl,
    string Title,
    string Description,
    int FilesChangedCount,
    int LinesAdded,
    int LinesDeleted,
    int CommitsCount,
    List<FileChange> FilesChanged
);

/// <summary>
/// Strategy interface for git platform-specific operations (PR creation, comments)
/// Each platform (GitHub, Bitbucket, Azure DevOps) implements this interface
/// </summary>
public interface IGitPlatformProvider
{
    /// <summary>
    /// Platform name (GitHub, Bitbucket, AzureDevOps)
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Create a pull request on the platform
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
    /// Get repository information from the platform
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
