using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Git;

namespace PRFactory.AgentTools.Git;

/// <summary>
/// Create pull request via platform provider.
/// </summary>
public class GitPullRequestTool : ToolBase
{
    private readonly IGitPlatformService _gitPlatformService;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "GitPullRequest";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Create pull request via git platform provider";

    /// <summary>
    /// Create a new GitPullRequestTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="gitPlatformService">Git platform service</param>
    public GitPullRequestTool(
        ILogger<ToolBase> logger,
        ITenantContext tenantContext,
        IGitPlatformService gitPlatformService)
        : base(logger, tenantContext)
    {
        _gitPlatformService = gitPlatformService;
    }

    /// <summary>
    /// Execute the tool to create a pull request
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Pull request URL and details</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryId = context.GetParameter<Guid>("repositoryId");
        var title = context.GetParameter<string>("title");
        var description = context.GetParameter<string>("description");
        var sourceBranch = context.GetParameter<string>("sourceBranch");
        var targetBranch = context.GetParameter<string>("targetBranch");

        // 1. Validate title and description
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("PR title cannot be empty");
        }

        if (title.Length > 256)
        {
            throw new ArgumentException("PR title cannot exceed 256 characters");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("PR description cannot be empty");
        }

        if (description.Length > 65536) // 64KB limit
        {
            throw new ArgumentException("PR description cannot exceed 64KB");
        }

        // 2. Validate branch names
        if (string.IsNullOrWhiteSpace(sourceBranch))
        {
            throw new ArgumentException("Source branch cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(targetBranch))
        {
            throw new ArgumentException("Target branch cannot be empty");
        }

        if (sourceBranch == targetBranch)
        {
            throw new ArgumentException("Source and target branches must be different");
        }

        // 3. Create pull request via platform provider
        var request = new CreatePullRequestRequest(
            sourceBranch,
            targetBranch,
            title,
            description
        );

        var prInfo = await ExecuteWithTimeoutAsync(
            () => _gitPlatformService.CreatePullRequestAsync(repositoryId, request, CancellationToken.None),
            TimeSpan.FromMinutes(2)
        );

        _logger.LogInformation(
            "Created pull request #{PrNumber} for repository {RepositoryId} for tenant {TenantId}: {PrUrl}",
            prInfo.Number, repositoryId, context.TenantId, prInfo.HtmlUrl);

        return $"Successfully created pull request #{prInfo.Number}\nURL: {prInfo.HtmlUrl}\nTitle: {title}";
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("repositoryId"))
            throw new ArgumentException("Parameter 'repositoryId' is required");

        if (!context.Parameters.ContainsKey("title"))
            throw new ArgumentException("Parameter 'title' is required");

        if (!context.Parameters.ContainsKey("description"))
            throw new ArgumentException("Parameter 'description' is required");

        if (!context.Parameters.ContainsKey("sourceBranch"))
            throw new ArgumentException("Parameter 'sourceBranch' is required");

        if (!context.Parameters.ContainsKey("targetBranch"))
            throw new ArgumentException("Parameter 'targetBranch' is required");

        return Task.CompletedTask;
    }
}
