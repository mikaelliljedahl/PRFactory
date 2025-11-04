using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Suspends workflow execution until human response is received.
/// Saves checkpoint, transitions ticket to appropriate waiting state,
/// and returns suspended status to halt the workflow.
/// </summary>
public class HumanWaitAgent : BaseAgent
{
    private readonly ITicketRepository _ticketRepository;

    public override string Name => "HumanWaitAgent";
    public override string Description => "Suspend workflow execution until human input is received";

    public HumanWaitAgent(
        ILogger<HumanWaitAgent> logger,
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
                Error = "Ticket entity is required"
            };
        }

        // Determine which state to transition to based on metadata
        var waitType = context.Metadata.ContainsKey("WaitType")
            ? context.Metadata["WaitType"].ToString()
            : "answers";

        WorkflowState targetState;
        string reason;

        switch (waitType?.ToLowerInvariant())
        {
            case "answers":
                targetState = WorkflowState.AwaitingAnswers;
                reason = "Waiting for developer to answer clarifying questions";
                break;

            case "planapproval":
                targetState = WorkflowState.PlanUnderReview;
                reason = "Waiting for developer to approve implementation plan";
                break;

            default:
                Logger.LogError("Unknown wait type: {WaitType}", waitType);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = $"Unknown wait type: {waitType}"
                };
        }

        Logger.LogInformation("Suspending workflow for ticket {JiraKey}, transitioning to {State}",
            context.Ticket.TicketKey, targetState);

        try
        {
            // Transition to waiting state
            var transitionResult = context.Ticket.TransitionTo(targetState, reason);
            if (!transitionResult.IsSuccess)
            {
                Logger.LogError("Failed to transition to {State}: {Error}", targetState, transitionResult.ErrorMessage);
                return new AgentResult
                {
                    Status = AgentStatus.Failed,
                    Error = transitionResult.ErrorMessage
                };
            }

            // Update ticket in database
            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            // Create checkpoint for resuming later
            var checkpoint = new CheckpointData
            {
                CheckpointId = Guid.NewGuid(),
                NextAgentType = DetermineNextAgent(waitType),
                SavedAt = DateTime.UtcNow,
                State = new Dictionary<string, object>(context.State)
            };

            context.Checkpoint = checkpoint;
            context.State["Checkpoint"] = checkpoint;

            // Update status
            context.Status = AgentStatus.Suspended;

            Logger.LogInformation("Workflow suspended for ticket {JiraKey}, checkpoint {CheckpointId} saved",
                context.Ticket.TicketKey, checkpoint.CheckpointId);

            // Return suspended result
            return new AgentResult
            {
                Status = AgentStatus.Pending,
                Output = new Dictionary<string, object>
                {
                    ["Suspended"] = true,
                    ["WaitType"] = waitType!,
                    ["CheckpointId"] = checkpoint.CheckpointId.ToString(),
                    ["State"] = targetState.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to suspend workflow for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to suspend workflow: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private string DetermineNextAgent(string? waitType)
    {
        return waitType?.ToLowerInvariant() switch
        {
            "answers" => "AnswerProcessingAgent",
            "planapproval" => "ImplementationAgent",
            _ => "UnknownAgent"
        };
    }
}
