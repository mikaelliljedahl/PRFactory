using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Infrastructure.Agents.Specialized;

/// <summary>
/// Specialized agent that posts an approval message to pull requests
/// when code review passes with no critical issues.
/// </summary>
public class PostApprovalCommentAgent : BaseAgent
{
    private readonly IGitPlatformService _gitPlatformService;

    public PostApprovalCommentAgent(
        ILogger<PostApprovalCommentAgent> logger,
        IGitPlatformService gitPlatformService)
        : base(logger)
    {
        _gitPlatformService = gitPlatformService ?? throw new ArgumentNullException(nameof(gitPlatformService));
    }

    public override string Name => "post-approval-comment";

    public override string Description => "Posts approval message to pull requests that pass code review";

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Posting approval comment for ticket {TicketId}, PR #{PrNumber}",
            context.TicketId, context.PullRequestNumber);

        // Validate context
        if (context.PullRequestNumber == null)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "PullRequestNumber is required to post approval comment"
            };
        }

        if (context.Repository == null)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Repository information is required to post approval comment"
            };
        }

        try
        {
            // Get suggestions from context state (may be empty)
            var suggestions = GetSuggestionsFromState(context);
            var reviewId = context.State.TryGetValue("review_id", out var rid) ? rid?.ToString() : "unknown";

            Logger.LogInformation(
                "Formatting approval message with {SuggestionCount} optional suggestions",
                suggestions.Count);

            // Format the approval message
            var comment = FormatApprovalMessage(
                suggestions,
                reviewId ?? "unknown",
                context.Repository.GitPlatform);

            // Post approval to PR
            await PostApprovalToPrAsync(
                context.Repository.Id,
                context.PullRequestNumber.Value,
                comment,
                cancellationToken);

            Logger.LogInformation(
                "Successfully posted approval comment for ticket {TicketId}",
                context.TicketId);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["ApprovalPosted"] = true,
                    ["SuggestionsCount"] = suggestions.Count,
                    ["CommentLength"] = comment.Length
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to post approval comment for ticket {TicketId}", context.TicketId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = ex.Message,
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Gets suggestions from context state as a list of strings
    /// </summary>
    private static List<string> GetSuggestionsFromState(AgentContext context)
    {
        if (!context.State.TryGetValue("suggestions", out var value))
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
    /// Formats approval message as markdown
    /// </summary>
    private static string FormatApprovalMessage(
        List<string> suggestions,
        string reviewId,
        string platformName)
    {
        var sb = new StringBuilder();

        // Header with approval emoji
        sb.AppendLine("## ✅ Code Review Approved");
        sb.AppendLine();
        sb.AppendLine("This pull request has been reviewed by AI and **no critical issues were found**.");
        sb.AppendLine();

        // Suggestions section (if any)
        if (suggestions.Count > 0)
        {
            sb.AppendLine("### 💡 Optional Suggestions");
            sb.AppendLine();
            sb.AppendLine("While not required, you may want to consider:");
            sb.AppendLine();

            for (int i = 0; i < suggestions.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {suggestions[i]}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("**Great work!** The code looks good and follows best practices.");
            sb.AppendLine();
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Review ID: `{reviewId}` | Platform: {platformName} | Approved at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    /// <summary>
    /// Posts the approval message to the pull request
    /// </summary>
    private async Task PostApprovalToPrAsync(
        Guid repositoryId,
        int pullRequestNumber,
        string comment,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Posting approval to PR #{PrNumber} on repository {RepositoryId}",
            pullRequestNumber, repositoryId);

        try
        {
            await _gitPlatformService.AddPullRequestCommentAsync(
                repositoryId,
                pullRequestNumber,
                comment,
                cancellationToken);

            Logger.LogInformation(
                "Successfully posted approval to PR #{PrNumber}",
                pullRequestNumber);

            // Note: Actual PR approval (setting approved status) is platform-specific
            // and may require additional permissions. For now, we just post a comment.
            // Future enhancement: Call platform-specific approval API if supported.
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to post approval to PR #{PrNumber} on repository {RepositoryId}",
                pullRequestNumber, repositoryId);
            throw new InvalidOperationException(
                $"Failed to post approval to PR #{pullRequestNumber} on repository {repositoryId}",
                ex);
        }
    }
}
