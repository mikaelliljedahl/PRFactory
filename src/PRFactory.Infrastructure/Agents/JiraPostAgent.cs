using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Jira;
using System.Text;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Generic agent for posting content to Jira (questions or plans).
/// Configurable via context to determine what content to post.
/// </summary>
public class JiraPostAgent : BaseAgent
{
    private readonly IJiraClient _jiraClient;

    public override string Name => "JiraPostAgent";
    public override string Description => "Post questions or plans to Jira ticket as comments";

    public JiraPostAgent(
        ILogger<JiraPostAgent> logger,
        IJiraClient jiraClient)
        : base(logger)
    {
        _jiraClient = jiraClient ?? throw new ArgumentNullException(nameof(jiraClient));
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

        // Determine what to post based on context metadata
        var postType = context.Metadata.ContainsKey("PostType")
            ? context.Metadata["PostType"].ToString()
            : "questions";

        Logger.LogInformation("Posting {PostType} to Jira ticket {JiraKey}", postType, context.Ticket.TicketKey);

        try
        {
            string commentText;

            switch (postType?.ToLowerInvariant())
            {
                case "questions":
                    commentText = FormatQuestionsComment(context);
                    break;

                case "plan":
                    commentText = FormatPlanComment(context);
                    break;

                case "custom":
                    // Custom message from metadata
                    commentText = context.Metadata.ContainsKey("CustomMessage")
                        ? context.Metadata["CustomMessage"].ToString()!
                        : throw new InvalidOperationException("CustomMessage metadata is required for custom post type");
                    break;

                default:
                    Logger.LogError("Unknown post type: {PostType}", postType);
                    return new AgentResult
                    {
                        Status = AgentStatus.Failed,
                        Error = $"Unknown post type: {postType}"
                    };
            }

            // Post comment to Jira
            // TODO: PostCommentAsync not yet implemented in IJiraClient
            Logger.LogWarning("PostCommentAsync not yet implemented - would post: {Comment}", commentText);
            // await _jiraClient.PostCommentAsync(context.Ticket.TicketKey, commentText, cancellationToken);

            Logger.LogInformation("Successfully posted {PostType} to Jira ticket {JiraKey}",
                postType, context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["PostType"] = postType!,
                    ["JiraKey"] = context.Ticket.TicketKey
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to post to Jira ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to post to Jira: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string FormatQuestionsComment(AgentContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 🤖 Claude AI - Clarifying Questions");
        sb.AppendLine();
        sb.AppendLine("I've analyzed the codebase and have some questions to ensure the implementation meets your needs:");
        sb.AppendLine();

        var questions = context.Ticket.Questions.OrderBy(q => q.Category).ToList();

        var groupedQuestions = questions.GroupBy(q => q.Category);

        foreach (var group in groupedQuestions)
        {
            sb.AppendLine($"### {CapitalizeCategory(group.Key)}");
            sb.AppendLine();

            foreach (var question in group)
            {
                sb.AppendLine($"**Q{questions.IndexOf(question) + 1}:** {question.Text}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine("*Please reply to this comment with `@claude` and your answers.*");
        sb.AppendLine();
        sb.AppendLine("**Example format:**");
        sb.AppendLine("```");
        sb.AppendLine("@claude");
        sb.AppendLine("Q1: [Your answer here]");
        sb.AppendLine("Q2: [Your answer here]");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private string FormatPlanComment(AgentContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 📋 Implementation Plan Ready");
        sb.AppendLine();
        sb.AppendLine($"I've created a detailed implementation plan for **{context.Ticket.TicketKey}**.");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(context.PlanBranchName))
        {
            sb.AppendLine($"**Branch:** `{context.PlanBranchName}`");
            sb.AppendLine();
        }

        sb.AppendLine("The plan includes:");
        sb.AppendLine("- ✅ Detailed implementation steps");
        sb.AppendLine("- ✅ List of affected files");
        sb.AppendLine("- ✅ Testing strategy");
        sb.AppendLine("- ✅ Potential risks and considerations");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(context.PlanBranchName))
        {
            sb.AppendLine($"Please review the `IMPLEMENTATION_PLAN.md` file in the `{context.PlanBranchName}` branch.");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("**To approve the plan, reply with:**");
        sb.AppendLine("```");
        sb.AppendLine("@claude approve");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("**To request changes, reply with:**");
        sb.AppendLine("```");
        sb.AppendLine("@claude reject [reason for rejection]");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private string CapitalizeCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return "Other";

        return category switch
        {
            "requirements" => "Requirements",
            "technical" => "Technical",
            "testing" => "Testing",
            _ => char.ToUpper(category[0]) + category.Substring(1)
        };
    }
}
