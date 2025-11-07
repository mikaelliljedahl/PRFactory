using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Creates a pull request on the git platform (GitHub, Bitbucket, Azure DevOps).
/// Links the PR to the Jira ticket and updates the ticket with PR information.
/// </summary>
public class PullRequestAgent : BaseAgent
{
    private readonly IGitPlatformProvider _gitPlatformProvider;
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "PullRequestAgent";
    public override string Description => "Create pull request on git platform and link to Jira ticket";

    public PullRequestAgent(
        ILogger<PullRequestAgent> logger,
        IGitPlatformProvider gitPlatformProvider,
        ITicketRepository ticketRepository)
        : base(logger)
    {
        _gitPlatformProvider = gitPlatformProvider ?? throw new ArgumentNullException(nameof(gitPlatformProvider));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (context.Ticket == null)
        {
            Logger.LogError("Ticket entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Ticket entity is required"
            };
        }

        if (context.Repository == null)
        {
            Logger.LogError("Repository entity is missing from context");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Repository entity is required"
            };
        }

        // Determine which branch to use for PR (implementation or plan)
        var sourceBranch = !string.IsNullOrEmpty(context.ImplementationBranchName)
            ? context.ImplementationBranchName
            : context.PlanBranchName;

        if (string.IsNullOrEmpty(sourceBranch))
        {
            Logger.LogError("No branch found for creating PR");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Source branch is required for creating PR"
            };
        }

        Logger.LogInformation("Creating pull request for ticket {JiraKey} from branch {Branch}",
            context.Ticket.TicketKey, sourceBranch);

        try
        {
            // Prepare PR details
            var targetBranch = context.Repository.DefaultBranch;
            var prTitle = $"[{context.Ticket.TicketKey}] {context.Ticket.Title}";
            var prDescription = BuildPRDescription(context);

            // Create PR using git platform provider
            var pullRequest = await _gitPlatformProvider.CreatePullRequestAsync(
                context.Repository.Id,
                new CreatePullRequestRequest
                {
                    Title = prTitle,
                    Description = prDescription,
                    SourceBranch = sourceBranch,
                    TargetBranch = targetBranch,
                    Labels = new List<string> { "prfactory", "ai-generated" }
                },
                cancellationToken
            );

            // Update context
            context.PullRequestUrl = pullRequest.Url;
            context.PullRequestNumber = pullRequest.Number;

            // Update ticket
            context.Ticket.SetPullRequest(pullRequest.Url, pullRequest.Number);

            // Transition to PRCreated state
            var transitionResult = context.Ticket.TransitionTo(WorkflowState.PRCreated);
            if (!transitionResult.IsSuccess)
            {
                Logger.LogError("Failed to transition to PRCreated: {Error}", transitionResult.ErrorMessage);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = transitionResult.ErrorMessage
                };
            }

            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            Logger.LogInformation("Pull request #{Number} created for ticket {JiraKey}: {Url}",
                pullRequest.Number, context.Ticket.TicketKey, pullRequest.Url);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["PullRequestUrl"] = pullRequest.Url,
                    ["PullRequestNumber"] = pullRequest.Number,
                    ["SourceBranch"] = sourceBranch,
                    ["TargetBranch"] = targetBranch
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create pull request for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to create pull request: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string BuildPRDescription(AgentContext context)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"## {context.Ticket.TicketKey}: {context.Ticket.Title}");
        sb.AppendLine();
        sb.AppendLine("### Description");
        sb.AppendLine(context.Ticket.Description);
        sb.AppendLine();

        if (context.Analysis != null)
        {
            sb.AppendLine("### Changes");
            sb.AppendLine($"{context.Analysis.Summary}");
            sb.AppendLine();

            if (context.Analysis.AffectedFiles.Any())
            {
                sb.AppendLine("### Affected Files");
                foreach (var file in context.Analysis.AffectedFiles)
                {
                    sb.AppendLine($"- `{file}`");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("### Testing");
        sb.AppendLine("- [ ] Unit tests added/updated");
        sb.AppendLine("- [ ] Integration tests added/updated");
        sb.AppendLine("- [ ] Manual testing completed");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine($"**Jira Ticket:** {context.Ticket.TicketKey}");
        sb.AppendLine("**Generated by:** PRFactory AI");

        return sb.ToString();
    }
}

/// <summary>
/// Request to create a pull request
/// </summary>
public class CreatePullRequestRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
}

/// <summary>
/// Pull request response
/// </summary>
public class PullRequestResponse
{
    public string Url { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Id { get; set; } = string.Empty;
}
