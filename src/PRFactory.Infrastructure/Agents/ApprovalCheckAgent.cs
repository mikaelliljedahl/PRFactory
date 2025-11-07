using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Checks if a plan has been approved in the database.
/// This agent verifies the approval status by checking the ticket state.
/// The ticket state should be updated by other agents (e.g., HumanWaitAgent)
/// when they process Jira webhook comments containing approval/rejection keywords.
/// </summary>
public class ApprovalCheckAgent : BaseAgent
{
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "ApprovalCheckAgent";
    public override string Description => "Check if implementation plan has been approved or rejected";

    public ApprovalCheckAgent(
        ILogger<ApprovalCheckAgent> logger,
        ITicketRepository ticketRepository)
        : base(logger)
    {
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
                Error = "Ticket entity is required for approval check"
            };
        }

        Logger.LogInformation(
            "Checking approval status for ticket {JiraKey} (current state: {State})",
            context.Ticket.TicketKey,
            context.Ticket.State);

        try
        {
            // Get the latest ticket state from database
            var ticket = await _ticketRepository.GetByIdAsync(context.Ticket.Id, cancellationToken);
            if (ticket == null)
            {
                Logger.LogError("Ticket {TicketId} not found in database", context.Ticket.Id);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"Ticket {context.Ticket.Id} not found"
                };
            }

            // Update context with latest ticket state
            context.Ticket = ticket;

            // Determine approval status based on ticket state
            var approvalStatus = DetermineApprovalStatus(ticket.State);
            var isApproved = approvalStatus == "approved";
            var isRejected = approvalStatus == "rejected";
            var isPending = approvalStatus == "pending";

            // Note: This agent relies on the ticket state being updated by other agents
            // (e.g., HumanWaitAgent or AnswerProcessingAgent) when they process Jira webhook
            // comments containing approval/rejection keywords. We don't directly query Jira
            // comments here to avoid complexity with the Jira API expand parameter.

            Logger.LogInformation(
                "Approval check completed for ticket {JiraKey}: Status={Status}, Approved={IsApproved}, Rejected={IsRejected}",
                ticket.TicketKey,
                approvalStatus,
                isApproved,
                isRejected);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["ApprovalStatus"] = approvalStatus,
                    ["IsApproved"] = isApproved,
                    ["IsRejected"] = isRejected,
                    ["IsPending"] = isPending,
                    ["CurrentState"] = ticket.State.ToString(),
                    ["TicketId"] = ticket.Id.ToString(),
                    ["JiraKey"] = ticket.TicketKey
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to check approval status for ticket {JiraKey}",
                context.Ticket.TicketKey);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to check approval status: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Determines approval status based on ticket workflow state
    /// </summary>
    private string DetermineApprovalStatus(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.PlanApproved => "approved",
            WorkflowState.PlanRejected => "rejected",
            WorkflowState.PlanUnderReview => "pending",
            WorkflowState.PlanPosted => "pending",
            _ => "unknown"
        };
    }
}
