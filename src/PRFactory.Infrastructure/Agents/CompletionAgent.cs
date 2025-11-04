using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Finalizes the workflow execution.
/// Cleans up workspace, transitions ticket to final state, and deletes checkpoint.
/// </summary>
public class CompletionAgent : BaseAgent
{
    private readonly ITicketRepository _ticketRepository;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public override string Name => "CompletionAgent";
    public override string Description => "Finalize workflow, cleanup workspace, and transition to completed state";

    public CompletionAgent(
        ILogger<CompletionAgent> logger,
        ITicketRepository ticketRepository,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
        : base(logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

        Logger.LogInformation("Completing workflow for ticket {JiraKey}", context.Ticket.TicketKey);

        try
        {
            // Determine final state based on PR creation
            WorkflowState finalState;
            if (!string.IsNullOrEmpty(context.PullRequestUrl))
            {
                finalState = WorkflowState.InReview;
            }
            else if (!string.IsNullOrEmpty(context.PlanBranchName))
            {
                finalState = WorkflowState.PlanPosted;
            }
            else
            {
                finalState = WorkflowState.Completed;
            }

            // Transition to final state
            var transitionResult = context.Ticket.TransitionTo(finalState);
            if (!transitionResult.IsSuccess)
            {
                Logger.LogWarning("Failed to transition to {State}: {Error}. Current state: {CurrentState}",
                    finalState, transitionResult.ErrorMessage, context.Ticket.State);
                // Continue anyway - this is not critical
            }

            // Update ticket
            await _ticketRepository.UpdateAsync(context.Ticket, cancellationToken);

            // Cleanup workspace (optional - based on configuration)
            var shouldCleanup = _configuration.GetValue<bool>("Workspace:CleanupAfterCompletion", false);
            if (shouldCleanup && !string.IsNullOrEmpty(context.RepositoryPath))
            {
                await CleanupWorkspaceAsync(context.RepositoryPath, cancellationToken);
            }

            // Delete checkpoint if exists
            if (context.Checkpoint != null)
            {
                Logger.LogInformation("Deleting checkpoint {CheckpointId}", context.Checkpoint.CheckpointId);
                // TODO: Implement checkpoint deletion
                context.Checkpoint = null;
            }

            // Update context status
            context.Status = AgentStatus.Completed;

            Logger.LogInformation("Workflow completed for ticket {JiraKey}, final state: {State}",
                context.Ticket.TicketKey, finalState);

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["FinalState"] = finalState.ToString(),
                    ["TicketKey"] = context.Ticket.TicketKey,
                    ["PullRequestUrl"] = context.PullRequestUrl ?? "N/A",
                    ["CompletedAt"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to complete workflow for ticket {JiraKey}", context.Ticket.TicketKey);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Failed to complete workflow: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private async Task CleanupWorkspaceAsync(string workspacePath, CancellationToken cancellationToken)
    {
        try
        {
            if (Directory.Exists(workspacePath))
            {
                Logger.LogInformation("Cleaning up workspace at {Path}", workspacePath);
                
                // Delete directory recursively
                await Task.Run(() =>
                {
                    Directory.Delete(workspacePath, recursive: true);
                }, cancellationToken);

                Logger.LogInformation("Workspace cleanup completed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to cleanup workspace at {Path}", workspacePath);
            // Don't throw - cleanup failure is not critical
        }
    }
}
