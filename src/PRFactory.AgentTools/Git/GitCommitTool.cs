using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Git;

namespace PRFactory.AgentTools.Git;

/// <summary>
/// Commit changes with message using LocalGitService.
/// </summary>
public class GitCommitTool : ToolBase
{
    private readonly ILocalGitService _gitService;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "GitCommit";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Commit changes to git repository with message";

    /// <summary>
    /// Create a new GitCommitTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="gitService">Local git service</param>
    public GitCommitTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        ILocalGitService gitService)
        : base(logger, tenantContext)
    {
        _gitService = gitService;
    }

    /// <summary>
    /// Execute the tool to commit files
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Success message</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var files = context.GetParameter<Dictionary<string, string>>("files");
        var commitMessage = context.GetParameter<string>("commitMessage");
        var author = context.GetOptionalParameter<string>("author", "PRFactory Agent") ?? "PRFactory Agent";

        // 1. Validate repository path exists
        var fullRepoPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullRepoPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. Validate commit message
        if (string.IsNullOrWhiteSpace(commitMessage))
        {
            throw new ArgumentException("Commit message cannot be empty");
        }

        // 3. Validate file count limit (max 100 files)
        if (files.Count == 0)
        {
            throw new ArgumentException("At least one file must be specified");
        }

        if (files.Count > 100)
        {
            throw new InvalidOperationException($"Cannot commit more than 100 files at once (got {files.Count})");
        }

        // 4. Validate each file exists and is within workspace
        foreach (var filePath in files.Keys)
        {
            var fullPath = PathValidator.ValidateAndResolve(
                Path.Combine(repositoryPath, filePath),
                context.WorkspacePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File '{filePath}' does not exist");
            }
        }

        // 5. Commit files
        await _gitService.CommitAsync(fullRepoPath, files, commitMessage, author);

        _logger.LogInformation(
            "Committed {FileCount} files to repository {RepositoryPath} for tenant {TenantId}",
            files.Count, repositoryPath, context.TenantId);

        return $"Successfully committed {files.Count} file(s) with message: {commitMessage}";
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

        if (!context.Parameters.ContainsKey("files"))
            throw new ArgumentException("Parameter 'files' is required");

        if (!context.Parameters.ContainsKey("commitMessage"))
            throw new ArgumentException("Parameter 'commitMessage' is required");

        return Task.CompletedTask;
    }
}
