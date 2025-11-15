using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Git;

/// <summary>
/// Interface for local git operations using LibGit2Sharp
/// </summary>
public interface ILocalGitService
{
    /// <summary>
    /// Clone a repository to local workspace
    /// </summary>
    Task<string> CloneAsync(string repoUrl, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Create a new branch from a base branch
    /// </summary>
    Task<string> CreateBranchAsync(string repoPath, string branchName, string fromBranch);

    /// <summary>
    /// Commit files to the repository
    /// </summary>
    Task CommitAsync(string repoPath, Dictionary<string, string> files, string message, string author);

    /// <summary>
    /// Push a branch to remote
    /// </summary>
    Task PushAsync(string repoPath, string branchName, string accessToken);

    /// <summary>
    /// Check if a branch exists
    /// </summary>
    Task<bool> BranchExistsAsync(string repoPath, string branchName);

    /// <summary>
    /// Get the default branch name
    /// </summary>
    string GetDefaultBranch(string repoPath);

    /// <summary>
    /// Get diff for a file between two branches or commits
    /// </summary>
    Task<string> GetDiffAsync(string repoPath, string? filePath = null, string? baseBranch = null, string? compareBranch = null);
}

/// <summary>
/// LibGit2Sharp wrapper for local git operations
/// </summary>
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
            IsBare = false,
            Checkout = true
        };

        cloneOptions.FetchOptions.CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
        {
            Username = "oauth2",
            Password = accessToken
        };

        await Task.Run(() => Repository.Clone(repoUrl, localPath, cloneOptions), ct);

        _logger.LogInformation("Repository cloned successfully to {LocalPath}", localPath);

        return localPath;
    }

    public async Task<string> CreateBranchAsync(string repoPath, string branchName, string fromBranch)
    {
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);

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
        });
    }

    public async Task CommitAsync(
        string repoPath,
        Dictionary<string, string> files,
        string message,
        string author)
    {
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);

            // Write files to disk
            foreach (var (relativePath, content) in files)
            {
                var fullPath = Path.Combine(repoPath, relativePath);
                var directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fullPath, content);

                // Stage file
                Commands.Stage(repo, relativePath);
            }

            // Commit
            var signature = new Signature(author, "claude@prfactory.ai", DateTimeOffset.Now);
            var commit = repo.Commit(message, signature, signature);

            _logger.LogInformation("Committed {FileCount} files with message: {Message}",
                files.Count, message);
        });
    }

    public async Task PushAsync(string repoPath, string branchName, string accessToken)
    {
        await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);

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
        });
    }

    public async Task<bool> BranchExistsAsync(string repoPath, string branchName)
    {
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);
            return repo.Branches[branchName] != null;
        });
    }

    public string GetDefaultBranch(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return repo.Head.FriendlyName;
    }

    public async Task<string> GetDiffAsync(string repoPath, string? filePath = null, string? baseBranch = null, string? compareBranch = null)
    {
        return await Task.Run(() =>
        {
            using var repo = new Repository(repoPath);

            // Default to HEAD for compare branch
            var compareCommit = repo.Head.Tip;
            if (!string.IsNullOrEmpty(compareBranch))
            {
                var compareBranchObj = repo.Branches[compareBranch] ?? repo.Branches[$"origin/{compareBranch}"];
                if (compareBranchObj == null)
                {
                    throw new InvalidOperationException($"Compare branch '{compareBranch}' not found");
                }
                compareCommit = compareBranchObj.Tip;
            }

            // Default to parent commit or empty tree for base
            Tree? baseTree = null;
            if (!string.IsNullOrEmpty(baseBranch))
            {
                var baseBranchObj = repo.Branches[baseBranch] ?? repo.Branches[$"origin/{baseBranch}"];
                if (baseBranchObj == null)
                {
                    throw new InvalidOperationException($"Base branch '{baseBranch}' not found");
                }
                baseTree = baseBranchObj.Tip.Tree;
            }
            else if (compareCommit.Parents.Any())
            {
                baseTree = compareCommit.Parents.First().Tree;
            }

            var compareTree = compareCommit.Tree;

            // Get diff
            var diff = baseTree != null
                ? repo.Diff.Compare<TreeChanges>(baseTree, compareTree)
                : repo.Diff.Compare<TreeChanges>(null, compareTree);

            // Filter by file path if specified
            IEnumerable<TreeEntryChanges> changes = diff;
            if (!string.IsNullOrEmpty(filePath))
            {
                changes = changes.Where(c => c.Path == filePath || c.OldPath == filePath);
            }

            // Generate patch
            var patch = repo.Diff.Compare<Patch>(baseTree, compareTree);
            var result = new System.Text.StringBuilder();

            foreach (var change in changes)
            {
                var patchEntry = patch[change.Path];
                if (patchEntry != null)
                {
                    result.AppendLine(patchEntry.Patch);
                }
            }

            return result.ToString();
        });
    }

    private string ExtractRepoName(string repoUrl)
    {
        // Validate that the URL is absolute and well-formed
        if (!Uri.TryCreate(repoUrl.Replace(".git", ""), UriKind.Absolute, out var uri))
        {
            throw new UriFormatException($"The repository URL '{repoUrl}' is not a valid absolute URI.");
        }

        return uri.Segments.Last();
    }
}
