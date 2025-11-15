using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Git;
using System.Text.RegularExpressions;

namespace PRFactory.AgentTools.Git;

/// <summary>
/// Create or switch git branches.
/// </summary>
public class GitBranchTool : ToolBase
{
    private readonly ILocalGitService _gitService;
    private static readonly Regex ValidBranchNameRegex = new(@"^[a-zA-Z0-9/_-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "GitBranch";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Create or switch git branches";

    /// <summary>
    /// Create a new GitBranchTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="gitService">Local git service</param>
    public GitBranchTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        ILocalGitService gitService)
        : base(logger, tenantContext)
    {
        _gitService = gitService;
    }

    /// <summary>
    /// Execute the tool to create or switch branches
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Success message</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var branchName = context.GetParameter<string>("branchName");
        var createNew = context.GetOptionalParameter<bool>("createNew", false);
        var fromBranch = context.GetOptionalParameter<string>("fromBranch", null);

        // 1. Validate repository path exists
        var fullRepoPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullRepoPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. Sanitize and validate branch name
        ValidateBranchName(branchName);

        // 3. Create or switch branch
        if (createNew)
        {
            // Get base branch (default to main/master if not specified)
            var baseBranch = fromBranch ?? _gitService.GetDefaultBranch(fullRepoPath);

            // Check if branch already exists
            var exists = await _gitService.BranchExistsAsync(fullRepoPath, branchName);
            if (exists)
            {
                throw new InvalidOperationException($"Branch '{branchName}' already exists");
            }

            // Create new branch
            await _gitService.CreateBranchAsync(fullRepoPath, branchName, baseBranch);

            _logger.LogInformation(
                "Created branch {BranchName} from {BaseBranch} in repository {RepositoryPath} for tenant {TenantId}",
                branchName, baseBranch, repositoryPath, context.TenantId);

            return $"Successfully created branch '{branchName}' from '{baseBranch}'";
        }
        else
        {
            // Switch to existing branch
            var exists = await _gitService.BranchExistsAsync(fullRepoPath, branchName);
            if (!exists)
            {
                throw new InvalidOperationException($"Branch '{branchName}' does not exist. Use createNew=true to create it.");
            }

            // Use CreateBranchAsync which also checks out the branch
            // This is safe even if branch exists because we'll catch the error
            await _gitService.CreateBranchAsync(fullRepoPath, branchName, _gitService.GetDefaultBranch(fullRepoPath));

            _logger.LogInformation(
                "Switched to branch {BranchName} in repository {RepositoryPath} for tenant {TenantId}",
                branchName, repositoryPath, context.TenantId);

            return $"Successfully switched to branch '{branchName}'";
        }
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

        if (!context.Parameters.ContainsKey("branchName"))
            throw new ArgumentException("Parameter 'branchName' is required");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate branch name format and security
    /// </summary>
    /// <param name="branchName">Branch name to validate</param>
    /// <exception cref="ArgumentException">Thrown when branch name is invalid</exception>
    private static void ValidateBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentException("Branch name cannot be empty");
        }

        if (branchName.Length > 100)
        {
            throw new ArgumentException("Branch name cannot exceed 100 characters");
        }

        if (!ValidBranchNameRegex.IsMatch(branchName))
        {
            throw new ArgumentException(
                "Branch name contains invalid characters. Only alphanumeric, hyphen, underscore, and forward slash are allowed.");
        }

        // Prevent command injection attempts
        if (branchName.Contains("..") || branchName.Contains("~") || branchName.Contains("^"))
        {
            throw new ArgumentException("Branch name contains forbidden patterns");
        }
    }
}
