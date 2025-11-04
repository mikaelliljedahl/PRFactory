using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Clones the repository to local workspace using LocalGitService.
/// Implements caching to avoid re-cloning if repo already exists.
/// </summary>
public class RepositoryCloneAgent : BaseAgent
{
    private readonly ILocalGitService _localGitService;
    private readonly string _workspaceBasePath;

    public override string Name => "RepositoryCloneAgent";
    public override string Description => "Clone repository to local workspace for analysis";

    public RepositoryCloneAgent(
        ILogger<RepositoryCloneAgent> logger,
        ILocalGitService localGitService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
        : base(logger)
    {
        _localGitService = localGitService ?? throw new ArgumentNullException(nameof(localGitService));
        _workspaceBasePath = configuration["Workspace:BasePath"] 
            ?? throw new InvalidOperationException("Workspace:BasePath not configured");
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (context.Repository == null)
        {
            Logger.LogError("Repository entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Repository entity is required"
            };
        }

        var repositoryId = context.Repository.Id;
        var cloneUrl = context.Repository.CloneUrl;
        var defaultBranch = context.Repository.DefaultBranch;

        Logger.LogInformation("Cloning repository {RepositoryId} from {CloneUrl}", repositoryId, cloneUrl);

        try
        {
            // Determine local path for this repository
            var localPath = Path.Combine(_workspaceBasePath, repositoryId.ToString());

            // Check if repository already exists (caching)
            if (Directory.Exists(localPath) && Directory.Exists(Path.Combine(localPath, ".git")))
            {
                Logger.LogInformation("Repository already cloned at {LocalPath}, pulling latest changes", localPath);
                
                // Pull latest changes instead of re-cloning
                await _localGitService.PullAsync(localPath, cancellationToken);
            }
            else
            {
                Logger.LogInformation("Cloning repository to {LocalPath}", localPath);
                
                // Clone repository
                await _localGitService.CloneAsync(cloneUrl, localPath, cancellationToken);
                
                // Checkout default branch
                if (!string.IsNullOrEmpty(defaultBranch))
                {
                    await _localGitService.CheckoutAsync(localPath, defaultBranch, cancellationToken);
                }
            }

            // Update context with repository path
            context.RepositoryPath = localPath;
            context.State["RepositoryPath"] = localPath;

            Logger.LogInformation("Repository cloned successfully to {LocalPath}", localPath);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["RepositoryPath"] = localPath
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to clone repository {RepositoryId}", repositoryId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to clone repository: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }
}
