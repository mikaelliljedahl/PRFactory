using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Jira;
using PRFactory.Infrastructure.Jira.Models;
using System.Text;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Agent for posting approved ticket updates to Jira.
/// Formats the refined ticket information (title, description, success criteria, acceptance criteria)
/// and posts it as a comment to the Jira ticket.
/// </summary>
public class TicketUpdatePostAgent : BaseAgent
{
    private readonly IJiraClient _jiraClient;
    private readonly ITicketUpdateRepository _ticketUpdateRepository;
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "TicketUpdatePostAgent";
    public override string Description => "Post approved ticket updates to Jira ticket";

    public TicketUpdatePostAgent(
        ILogger<TicketUpdatePostAgent> logger,
        IJiraClient jiraClient,
        ITicketUpdateRepository ticketUpdateRepository,
        ITicketRepository ticketRepository)
        : base(logger)
    {
        _jiraClient = jiraClient ?? throw new ArgumentNullException(nameof(jiraClient));
        _ticketUpdateRepository = ticketUpdateRepository ?? throw new ArgumentNullException(nameof(ticketUpdateRepository));
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

        // Get TicketUpdateId from context
        if (!context.State.TryGetValue("TicketUpdateId", out var ticketUpdateIdObj) || ticketUpdateIdObj is not Guid ticketUpdateId)
        {
            Logger.LogError("TicketUpdateId is missing from context state");
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "TicketUpdateId is required in context state"
            };
        }

        Logger.LogInformation(
            "Posting ticket update {TicketUpdateId} to Jira ticket {JiraKey}",
            ticketUpdateId, context.Ticket.TicketKey);

        try
        {
            // Retrieve the ticket update
            var ticketUpdate = await _ticketUpdateRepository.GetByIdAsync(ticketUpdateId, cancellationToken);
            if (ticketUpdate == null)
            {
                Logger.LogError("TicketUpdate {TicketUpdateId} not found", ticketUpdateId);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"TicketUpdate {ticketUpdateId} not found"
                };
            }

            // Verify the ticket update is approved
            if (!ticketUpdate.IsApproved)
            {
                Logger.LogError("TicketUpdate {TicketUpdateId} is not approved", ticketUpdateId);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = "Cannot post unapproved ticket update"
                };
            }

            // Check if already posted
            if (ticketUpdate.PostedAt.HasValue)
            {
                Logger.LogWarning(
                    "TicketUpdate {TicketUpdateId} has already been posted at {PostedAt}",
                    ticketUpdateId, ticketUpdate.PostedAt);
                return new AgentResult
                {
                    Status = AgentStatus.Completed,
                    Output = new Dictionary<string, object>
                    {
                        ["TicketUpdateId"] = ticketUpdateId,
                        ["AlreadyPosted"] = true,
                        ["PostedAt"] = ticketUpdate.PostedAt.Value
                    }
                };
            }

            // Format the ticket update for Jira
            var commentContent = FormatTicketUpdateForJira(ticketUpdate);

            // Post to Jira
            var commentRequest = AddCommentRequest.FromPlainText(commentContent);
            await _jiraClient.AddCommentAsync(context.Ticket.TicketKey, commentRequest);

            // Mark as posted
            ticketUpdate.MarkAsPosted();
            await _ticketUpdateRepository.UpdateAsync(ticketUpdate, cancellationToken);

            // Update ticket workflow state
            context.Ticket.UpdateWorkflowState(WorkflowState.TicketUpdatePosted);
            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            // Store posted info in context
            context.State["TicketUpdatePostedAt"] = ticketUpdate.PostedAt!.Value;

            Logger.LogInformation(
                "Successfully posted ticket update {TicketUpdateId} (version {Version}) to Jira ticket {JiraKey}",
                ticketUpdateId, ticketUpdate.Version, context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["TicketUpdateId"] = ticketUpdateId,
                    ["Version"] = ticketUpdate.Version,
                    ["PostedAt"] = ticketUpdate.PostedAt!.Value,
                    ["JiraKey"] = context.Ticket.TicketKey
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to post ticket update {TicketUpdateId} to Jira ticket {JiraKey}",
                ticketUpdateId, context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to post ticket update to Jira: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Formats the ticket update as a Jira comment using markdown-style formatting.
    /// Jira will convert this to ADF format via the FromPlainText helper.
    /// </summary>
    private string FormatTicketUpdateForJira(Domain.Entities.TicketUpdate ticketUpdate)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# 🎯 Refined Ticket Update");
        sb.AppendLine();
        sb.AppendLine($"*Version {ticketUpdate.Version} • Generated: {ticketUpdate.GeneratedAt:yyyy-MM-dd HH:mm} UTC*");
        if (ticketUpdate.ApprovedAt.HasValue)
        {
            sb.AppendLine($"*Approved: {ticketUpdate.ApprovedAt.Value:yyyy-MM-dd HH:mm} UTC*");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Updated Title
        sb.AppendLine("## 📝 Updated Title");
        sb.AppendLine();
        sb.AppendLine($"**{ticketUpdate.UpdatedTitle}**");
        sb.AppendLine();

        // Updated Description
        sb.AppendLine("## 📖 Description");
        sb.AppendLine();
        sb.AppendLine(ticketUpdate.UpdatedDescription);
        sb.AppendLine();

        // Success Criteria
        sb.AppendLine("## ✅ Success Criteria");
        sb.AppendLine();

        // Group by category
        var criteriaByCategory = ticketUpdate.SuccessCriteria
            .GroupBy(sc => sc.Category)
            .OrderBy(g => g.Key);

        foreach (var group in criteriaByCategory)
        {
            sb.AppendLine($"### {FormatCategory(group.Key)}");
            sb.AppendLine();

            // Group by priority within category
            var mustHave = group.Where(sc => sc.Priority == 0).ToList();
            var shouldHave = group.Where(sc => sc.Priority == 1).ToList();
            var niceToHave = group.Where(sc => sc.Priority == 2).ToList();

            if (mustHave.Any())
            {
                sb.AppendLine("**Must Have (P0):**");
                foreach (var criterion in mustHave)
                {
                    var testable = criterion.IsTestable ? " ✓ Testable" : "";
                    sb.AppendLine($"- {criterion.Description}{testable}");
                }
                sb.AppendLine();
            }

            if (shouldHave.Any())
            {
                sb.AppendLine("**Should Have (P1):**");
                foreach (var criterion in shouldHave)
                {
                    var testable = criterion.IsTestable ? " ✓ Testable" : "";
                    sb.AppendLine($"- {criterion.Description}{testable}");
                }
                sb.AppendLine();
            }

            if (niceToHave.Any())
            {
                sb.AppendLine("**Nice to Have (P2):**");
                foreach (var criterion in niceToHave)
                {
                    var testable = criterion.IsTestable ? " ✓ Testable" : "";
                    sb.AppendLine($"- {criterion.Description}{testable}");
                }
                sb.AppendLine();
            }
        }

        // Acceptance Criteria
        sb.AppendLine("## 📋 Acceptance Criteria");
        sb.AppendLine();
        sb.AppendLine(ticketUpdate.AcceptanceCriteria);
        sb.AppendLine();

        // Summary Statistics
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("### Summary");
        sb.AppendLine($"- Total Success Criteria: {ticketUpdate.SuccessCriteria.Count}");
        sb.AppendLine($"  - Must Have: {ticketUpdate.GetMustHaveCriteria().Count}");
        sb.AppendLine($"  - Should Have: {ticketUpdate.GetShouldHaveCriteria().Count}");
        sb.AppendLine($"  - Nice to Have: {ticketUpdate.GetNiceToHaveCriteria().Count}");
        sb.AppendLine($"- Testable Criteria: {ticketUpdate.GetTestableCriteria().Count}");

        return sb.ToString();
    }

    private string FormatCategory(SuccessCriterionCategory category)
    {
        return category switch
        {
            SuccessCriterionCategory.Functional => "🔧 Functional",
            SuccessCriterionCategory.Technical => "⚙️ Technical",
            SuccessCriterionCategory.Testing => "🧪 Testing",
            SuccessCriterionCategory.UX => "🎨 User Experience",
            SuccessCriterionCategory.Security => "🔒 Security",
            SuccessCriterionCategory.Performance => "⚡ Performance",
            _ => category.ToString()
        };
    }
}
