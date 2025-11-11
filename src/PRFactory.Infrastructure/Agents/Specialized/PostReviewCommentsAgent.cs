using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Infrastructure.Agents.Specialized;

/// <summary>
/// Specialized agent that posts code review feedback (critical issues and suggestions)
/// as comments on pull requests.
/// </summary>
public class PostReviewCommentsAgent : BaseAgent
{
    private readonly IGitPlatformService _gitPlatformService;

    public PostReviewCommentsAgent(
        ILogger<PostReviewCommentsAgent> logger,
        IGitPlatformService gitPlatformService)
        : base(logger)
    {
        _gitPlatformService = gitPlatformService ?? throw new ArgumentNullException(nameof(gitPlatformService));
    }

    public override string Name => "post-review-comments";

    public override string Description => "Posts code review feedback as comments on pull requests";

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Posting review comments for ticket {TicketId}, PR #{PrNumber}",
            context.TicketId, context.PullRequestNumber);

        // Validate context
        if (context.PullRequestNumber == null)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "PullRequestNumber is required to post review comments"
            };
        }

        if (context.Repository == null)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Repository information is required to post review comments"
            };
        }

        try
        {
            // Get issues from context state (set by CodeReviewGraph)
            var criticalIssues = GetIssuesFromState(context, "critical_issues");
            var suggestions = GetIssuesFromState(context, "suggestions");
            var reviewId = context.State.TryGetValue("review_id", out var rid) ? rid?.ToString() : "unknown";

            Logger.LogInformation(
                "Formatting {CriticalCount} critical issues and {SuggestionCount} suggestions",
                criticalIssues.Count, suggestions.Count);

            // Format the review feedback
            var comment = FormatReviewFeedback(
                criticalIssues,
                suggestions,
                reviewId,
                context.Repository.GitPlatform);

            // Post comment to PR
            await PostCommentToPrAsync(
                context.Repository.Id,
                context.PullRequestNumber.Value,
                comment,
                cancellationToken);

            Logger.LogInformation(
                "Successfully posted review comments for ticket {TicketId}",
                context.TicketId);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["CommentPosted"] = true,
                    ["CriticalIssuesCount"] = criticalIssues.Count,
                    ["SuggestionsCount"] = suggestions.Count,
                    ["CommentLength"] = comment.Length
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to post review comments for ticket {TicketId}", context.TicketId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = ex.Message,
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Gets issues from context state as a list of strings
    /// </summary>
    private List<string> GetIssuesFromState(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            return new List<string>();
        }

        // Handle both List<string> and List<object>
        if (value is List<string> stringList)
        {
            return stringList;
        }

        if (value is IEnumerable<object> objectList)
        {
            return objectList.Select(o => o?.ToString() ?? string.Empty).ToList();
        }

        return new List<string>();
    }

    /// <summary>
    /// Formats review feedback as markdown comment
    /// </summary>
    private string FormatReviewFeedback(
        List<string> criticalIssues,
        List<string> suggestions,
        string reviewId,
        string platformName)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("## 🤖 AI Code Review");
        sb.AppendLine();

        // Critical issues section
        if (criticalIssues.Count > 0)
        {
            sb.AppendLine("### ⚠️ Critical Issues");
            sb.AppendLine();
            sb.AppendLine("The following issues must be addressed before merging:");
            sb.AppendLine();

            for (int i = 0; i < criticalIssues.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {criticalIssues[i]}");
                sb.AppendLine();
            }
        }

        // Suggestions section
        if (suggestions.Count > 0)
        {
            sb.AppendLine("### 💡 Suggestions");
            sb.AppendLine();
            sb.AppendLine("Consider the following improvements:");
            sb.AppendLine();

            for (int i = 0; i < suggestions.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {suggestions[i]}");
                sb.AppendLine();
            }
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Review ID: `{reviewId}` | Platform: {platformName} | Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    /// <summary>
    /// Posts the formatted comment to the pull request
    /// </summary>
    private async Task PostCommentToPrAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Posting comment to PR #{PrNumber} on repository {RepositoryId}",
            pullRequestNumber, repositoryId);

        try
        {
            await _gitPlatformService.AddPullRequestCommentAsync(
                repositoryId,
                pullRequestNumber,
                comment,
                cancellationToken);

            Logger.LogInformation(
                "Successfully posted comment to PR #{PrNumber}",
                pullRequestNumber);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to post comment to PR #{PrNumber} on repository {RepositoryId}",
                pullRequestNumber, repositoryId);
            throw;
        }
    }
}
