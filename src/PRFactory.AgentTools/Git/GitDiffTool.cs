using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Git;
using System.Text;

namespace PRFactory.AgentTools.Git;

/// <summary>
/// Get diff for files in git repository.
/// </summary>
public class GitDiffTool : ToolBase
{
    private readonly ILocalGitService _gitService;
    private const long MaxDiffSize = 1024 * 1024; // 1MB max diff size

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "GitDiff";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Get diff for files between branches or commits";

    /// <summary>
    /// Create a new GitDiffTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="gitService">Local git service</param>
    public GitDiffTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        ILocalGitService gitService)
        : base(logger, tenantContext)
    {
        _gitService = gitService;
    }

    /// <summary>
    /// Execute the tool to get git diff
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Diff output</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var filePath = context.GetOptionalParameter<string>("filePath", null);
        var baseBranch = context.GetOptionalParameter<string>("baseBranch", null);
        var compareBranch = context.GetOptionalParameter<string>("compareBranch", null);

        // 1. Validate repository path exists
        var fullRepoPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullRepoPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. If file path specified, validate it
        if (!string.IsNullOrEmpty(filePath))
        {
            // Normalize path separators for cross-platform compatibility
            filePath = filePath.Replace("\\", "/");

            // Validate file path (don't resolve to full path, just sanitize)
            if (filePath.Contains("..") || Path.IsPathRooted(filePath))
            {
                throw new ArgumentException("Invalid file path. Use relative paths within repository.");
            }
        }

        // 3. Get diff with timeout
        var diff = await ExecuteWithTimeoutAsync(
            () => _gitService.GetDiffAsync(fullRepoPath, filePath, baseBranch, compareBranch),
            TimeSpan.FromSeconds(30)
        );

        // 4. Validate diff size
        var diffSize = Encoding.UTF8.GetByteCount(diff);
        if (diffSize > MaxDiffSize)
        {
            throw new InvalidOperationException(
                $"Diff size {diffSize} bytes exceeds limit {MaxDiffSize} bytes. " +
                "Try specifying a specific file path to reduce diff size.");
        }

        if (string.IsNullOrEmpty(diff))
        {
            var fileInfo = !string.IsNullOrEmpty(filePath) ? $" for file '{filePath}'" : "";
            var branchInfo = !string.IsNullOrEmpty(baseBranch) && !string.IsNullOrEmpty(compareBranch)
                ? $" between '{baseBranch}' and '{compareBranch}'"
                : "";

            return $"No changes found{fileInfo}{branchInfo}";
        }

        _logger.LogInformation(
            "Retrieved diff for repository {RepositoryPath} ({DiffSize} bytes) for tenant {TenantId}",
            repositoryPath, diffSize, context.TenantId);

        return diff;
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("repositoryPath"))
            throw new ArgumentException("Parameter 'repositoryPath' is required");

        return Task.CompletedTask;
    }
}
