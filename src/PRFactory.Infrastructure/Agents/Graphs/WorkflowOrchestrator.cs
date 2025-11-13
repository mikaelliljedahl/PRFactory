namespace PRFactory.Infrastructure.Agents.Graphs;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Configuration;

/// <summary>
/// WorkflowOrchestrator - Main orchestrator for multi-graph workflows
///
/// Chains graphs: Refinement → Planning → [Implementation] → [CodeReview]
/// - Event-driven transitions between graphs
/// - Handles webhook resume after HumanWait
/// - Maintains overall workflow state
/// - Supports code review loop (Implementation ↔ CodeReview)
/// </summary>
[SuppressMessage("Design", "CA1062", Justification = "Workflow orchestrator requires multiple graph dependencies")]
[SuppressMessage("Design", "S107", Justification = "WorkflowOrchestrator requires multiple graph dependencies for orchestration")]
public class WorkflowOrchestrator(
    ILogger<WorkflowOrchestrator> logger,
    RefinementGraph refinementGraph,
    PlanningGraph planningGraph,
    ImplementationGraph implementationGraph,
    CodeReviewGraph codeReviewGraph,
    IWorkflowStateStore workflowStateStore,
    IEventPublisher eventPublisher,
    ITenantConfigurationService tenantConfigService) : IWorkflowOrchestrator
{
    private const string ImplementationGraphName = "ImplementationGraph";
    private const string RefinementGraphName = "RefinementGraph";
    private const string PlanningGraphName = "PlanningGraph";
    private const string CodeReviewGraphName = "CodeReviewGraph";
    private const string WorkflowForTicketLogTemplate = "workflow for ticket {TicketId}";
    private const string NoWorkflowFoundErrorMessage = "No workflow found for ticket {TicketId}";

    /// <summary>
    /// Start a new workflow from a trigger message
    /// </summary>
    public async Task<Guid> StartWorkflowAsync(
        TriggerTicketMessage triggerMessage,
        CancellationToken cancellationToken = default)
    {
        var workflowId = Guid.NewGuid();
        var ticketId = triggerMessage.TicketId;

        logger.LogInformation(
            "Starting workflow {WorkflowId} for ticket {TicketId}",
            workflowId, ticketId);

        try
        {
            // Create workflow state
            var workflowState = new WorkflowState
            {
                WorkflowId = workflowId,
                TicketId = ticketId,
                CurrentGraph = RefinementGraphName,
                Status = WorkflowStatus.Running,
                StartedAt = DateTime.UtcNow
            };
            await workflowStateStore.SaveStateAsync(workflowState);

            // Execute RefinementGraph
            var result = await refinementGraph.ExecuteAsync(triggerMessage, cancellationToken);

            // Update workflow state based on result
            await HandleGraphResultAsync(workflowState, result, cancellationToken);

            logger.LogInformation(
                "Workflow {WorkflowId} for ticket {TicketId} started successfully, current state: {State}",
                workflowId, ticketId, result.State);

            return workflowId;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to start workflow for ticket {TicketId}",
                ticketId);

            await workflowStateStore.UpdateStatusAsync(
                workflowId,
                WorkflowStatus.Failed,
                error: ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Resume a suspended workflow with new input
    /// </summary>
    public async Task ResumeWorkflowAsync(
        Guid ticketId,
        IAgentMessage resumeMessage,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Resuming workflow for ticket {TicketId} with message type {MessageType}",
            ticketId, resumeMessage.GetType().Name);

        try
        {
            // Load workflow state
            var workflowState = await workflowStateStore.GetByTicketIdAsync(ticketId);
            if (workflowState == null)
            {
                throw new InvalidOperationException(NoWorkflowFoundErrorMessage.Replace("{TicketId}", ticketId.ToString()));
            }

            if (workflowState.Status != WorkflowStatus.Suspended)
            {
                throw new InvalidOperationException(
                    $"Workflow {workflowState.WorkflowId} is not in suspended state (current: {workflowState.Status})");
            }

            // Update status to running
            workflowState.Status = WorkflowStatus.Running;
            await workflowStateStore.SaveStateAsync(workflowState);

            // Resume the appropriate graph based on current state
            GraphExecutionResult result;
            switch (workflowState.CurrentGraph)
            {
                case RefinementGraphName:
                    result = await refinementGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                    break;

                case PlanningGraphName:
                    result = await planningGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                    break;

                case ImplementationGraphName:
                    result = await implementationGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                    break;

                case CodeReviewGraphName:
                    result = await codeReviewGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown graph: {workflowState.CurrentGraph}");
            }

            // Handle the result and potentially transition to next graph
            await HandleGraphResultAsync(workflowState, result, cancellationToken);

            logger.LogInformation(
                "Workflow resumed for ticket {TicketId}, current state: {State}",
                ticketId, result.State);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to resume workflow for ticket {TicketId}",
                ticketId);

            var workflowState = await workflowStateStore.GetByTicketIdAsync(ticketId);
            if (workflowState != null)
            {
                await workflowStateStore.UpdateStatusAsync(
                    workflowState.WorkflowId,
                    WorkflowStatus.Failed,
                    error: ex.Message);
            }

            throw;
        }
    }

    /// <summary>
    /// Get the current status of a workflow
    /// </summary>
    public async Task<WorkflowStatus> GetWorkflowStatusAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var workflowState = await workflowStateStore.GetByTicketIdAsync(ticketId);
        return workflowState?.Status ?? WorkflowStatus.NotFound;
    }

    /// <summary>
    /// Cancel a running workflow
    /// </summary>
    public async Task CancelWorkflowAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Cancelling {WorkflowForTicketLogTemplate}", ticketId);

        var workflowState = await workflowStateStore.GetByTicketIdAsync(ticketId);
        if (workflowState == null)
        {
            throw new InvalidOperationException(NoWorkflowFoundErrorMessage.Replace("{TicketId}", ticketId.ToString()));
        }

        await workflowStateStore.UpdateStatusAsync(
            workflowState.WorkflowId,
            WorkflowStatus.Cancelled);

        await eventPublisher.PublishAsync(new WorkflowCancelledEvent
        {
            TicketId = ticketId,
            WorkflowId = workflowState.WorkflowId,
            CancelledAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handle the result of a graph execution and potentially transition to next graph
    /// </summary>
    private async Task HandleGraphResultAsync(
        WorkflowState workflowState,
        GraphExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            // Graph failed
            logger.LogError(
                "Graph {GraphId} failed for ticket {TicketId}: {Error}",
                workflowState.CurrentGraph, workflowState.TicketId, result.Error?.Message);

            workflowState.Status = WorkflowStatus.Failed;
            workflowState.ErrorMessage = result.Error?.Message;
            workflowState.CompletedAt = DateTime.UtcNow;
            await workflowStateStore.SaveStateAsync(workflowState);

            await eventPublisher.PublishAsync(new WorkflowFailedEvent
            {
                TicketId = workflowState.TicketId,
                WorkflowId = workflowState.WorkflowId,
                GraphId = workflowState.CurrentGraph,
                Error = result.Error?.Message,
                FailedAt = DateTime.UtcNow
            });

            return;
        }

        // Check if graph is suspended (awaiting human input)
        if (result.State.Contains("awaiting") || result.State.Contains("suspended"))
        {
            logger.LogInformation(
                "Graph {GraphId} suspended for ticket {TicketId} at state {State}",
                workflowState.CurrentGraph, workflowState.TicketId, result.State);

            workflowState.Status = WorkflowStatus.Suspended;
            workflowState.CurrentState = result.State;
            await workflowStateStore.SaveStateAsync(workflowState);

            await eventPublisher.PublishAsync(new WorkflowSuspendedEvent
            {
                TicketId = workflowState.TicketId,
                WorkflowId = workflowState.WorkflowId,
                GraphId = workflowState.CurrentGraph,
                State = result.State,
                SuspendedAt = DateTime.UtcNow
            });

            return;
        }

        // Graph completed successfully - check for transitions
        await HandleGraphTransitionAsync(workflowState, result, cancellationToken);
    }

    /// <summary>
    /// Handle transitions between graphs based on completion events
    /// </summary>
    private async Task HandleGraphTransitionAsync(
        WorkflowState workflowState,
        GraphExecutionResult result,
        CancellationToken cancellationToken)
    {
        switch (workflowState.CurrentGraph)
        {
            case RefinementGraphName:
                if (result.OutputMessage is RefinementCompleteEvent)
                {
                    logger.LogInformation(
                        $"{RefinementGraphName} completed {WorkflowForTicketLogTemplate}, transitioning to {PlanningGraphName}",
                        workflowState.TicketId);

                    // Transition to PlanningGraph
                    workflowState.CurrentGraph = PlanningGraphName;
                    workflowState.Status = WorkflowStatus.Running;
                    await workflowStateStore.SaveStateAsync(workflowState);

                    // Execute PlanningGraph with answers from RefinementGraph
                    var planningResult = await planningGraph.ExecuteAsync(
                        result.OutputMessage, cancellationToken);
                    await HandleGraphResultAsync(workflowState, planningResult, cancellationToken);
                }
                break;

            case PlanningGraphName:
                if (result.OutputMessage is PlanApprovedEvent planApproved)
                {
                    logger.LogInformation(
                        $"{PlanningGraphName} completed with approval {WorkflowForTicketLogTemplate}, checking for implementation",
                        workflowState.TicketId);

                    // Create plan approved message for ImplementationGraph
                    var planApprovedMessage = new PlanApprovedMessage(
                        workflowState.TicketId,
                        planApproved.ApprovedAt,
                        "system"
                    );

                    // Transition to ImplementationGraph
                    workflowState.CurrentGraph = ImplementationGraphName;
                    workflowState.Status = WorkflowStatus.Running;
                    await workflowStateStore.SaveStateAsync(workflowState);

                    // Execute ImplementationGraph (it will check if auto-implementation is enabled)
                    var implementationResult = await implementationGraph.ExecuteAsync(
                        planApprovedMessage, cancellationToken);
                    await HandleGraphResultAsync(workflowState, implementationResult, cancellationToken);
                }
                break;

            case ImplementationGraphName:
                // Check if we should transition to CodeReviewGraph or complete
                await HandleImplementationCompletionAsync(workflowState, result, cancellationToken);
                break;

            case CodeReviewGraphName:
                // Check if we should loop back to ImplementationGraph or complete
                await HandleCodeReviewCompletionAsync(workflowState, result, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Handle completion of ImplementationGraph - check if code review should be triggered
    /// </summary>
    private async Task HandleImplementationCompletionAsync(
        WorkflowState workflowState,
        GraphExecutionResult result,
        CancellationToken cancellationToken)
    {
        // Check if PR was created
        if (result.OutputMessage is not PRCreatedMessage prCreated)
        {
            // No PR created, complete workflow
            logger.LogInformation(
                $"{ImplementationGraphName} completed without PR {WorkflowForTicketLogTemplate}, workflow finished",
                workflowState.TicketId);

            await CompleteWorkflowAsync(workflowState);
            return;
        }

        // Check if auto code review is enabled
        var tenantConfig = await tenantConfigService.GetConfigurationForTicketAsync(
            workflowState.TicketId, cancellationToken);

        if (tenantConfig?.EnableAutoCodeReview != true)
        {
            logger.LogInformation(
                $"Auto code review disabled {WorkflowForTicketLogTemplate}, workflow finished",
                workflowState.TicketId);

            await CompleteWorkflowAsync(workflowState);
            return;
        }

        // Transition to CodeReviewGraph
        logger.LogInformation(
            $"{ImplementationGraphName} completed with PR #{{PrNumber}} {WorkflowForTicketLogTemplate}, transitioning to {CodeReviewGraphName}",
            prCreated.PullRequestNumber, workflowState.TicketId);

        workflowState.CurrentGraph = CodeReviewGraphName;
        workflowState.Status = WorkflowStatus.Running;
        await workflowStateStore.SaveStateAsync(workflowState);

        // Create ReviewCodeMessage and execute CodeReviewGraph
        // Note: BranchName and PlanPath are populated from PR metadata during review execution
        var reviewMessage = new ReviewCodeMessage(
            workflowState.TicketId,
            prCreated.PullRequestNumber,
            prCreated.PullRequestUrl,
            BranchName: string.Empty,
            PlanPath: null
        );

        var reviewResult = await codeReviewGraph.ExecuteAsync(reviewMessage, cancellationToken);
        await HandleGraphResultAsync(workflowState, reviewResult, cancellationToken);
    }

    /// <summary>
    /// Handle completion of CodeReviewGraph - check if fixes needed or workflow complete
    /// </summary>
    private async Task HandleCodeReviewCompletionAsync(
        WorkflowState workflowState,
        GraphExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (result.OutputMessage is not CodeReviewCompleteMessage reviewComplete)
        {
            logger.LogWarning(
                $"{CodeReviewGraphName} completed with unexpected message type {WorkflowForTicketLogTemplate}",
                workflowState.TicketId);

            await CompleteWorkflowAsync(workflowState);
            return;
        }

        // Check if there are critical issues
        if (!reviewComplete.HasCriticalIssues || reviewComplete.CriticalIssues.Count == 0)
        {
            logger.LogInformation(
                $"{CodeReviewGraphName} completed with no critical issues {WorkflowForTicketLogTemplate}, workflow finished",
                workflowState.TicketId);

            // Check if auto-approval should be posted
            var tenantConfig = await tenantConfigService.GetConfigurationForTicketAsync(
                workflowState.TicketId, cancellationToken);

            if (tenantConfig?.AutoApproveIfNoIssues == true)
            {
                logger.LogInformation(
                    $"Auto-approval enabled {WorkflowForTicketLogTemplate}",
                    workflowState.TicketId);

                // Note: Auto-approval posting will be implemented in a future enhancement
                // to support direct approval comments on PR/ticket systems
            }

            await CompleteWorkflowAsync(workflowState);
            return;
        }

        // Get retry count from workflow state
        var retryCount = workflowState.CurrentState.Contains("retry_count:")
            ? int.Parse(workflowState.CurrentState.Split("retry_count:")[1].Split(',')[0])
            : 0;

        var tenantConfig2 = await tenantConfigService.GetConfigurationForTicketAsync(
            workflowState.TicketId, cancellationToken);
        var maxIterations = tenantConfig2?.MaxCodeReviewIterations ?? 3;

        if (retryCount >= maxIterations)
        {
            logger.LogWarning(
                $"{CodeReviewGraphName} max iterations ({{MaxIterations}}) reached {WorkflowForTicketLogTemplate}, completing with warnings",
                maxIterations, workflowState.TicketId);

            await CompleteWorkflowAsync(workflowState, hasWarnings: true);
            return;
        }

        // Loop back to ImplementationGraph to fix issues
        logger.LogInformation(
            $"{CodeReviewGraphName} found {{IssueCount}} issues {WorkflowForTicketLogTemplate}, looping back to {ImplementationGraphName} (attempt {{Attempt}}/{{MaxIterations}})",
            reviewComplete.CriticalIssues.Count, workflowState.TicketId, retryCount + 1, maxIterations);

        workflowState.CurrentGraph = ImplementationGraphName;
        workflowState.CurrentState = $"retry_count:{retryCount + 1},issues_found";
        workflowState.Status = WorkflowStatus.Running;
        await workflowStateStore.SaveStateAsync(workflowState);

        // Create FixCodeIssuesMessage and execute ImplementationGraph
        var fixMessage = new FixCodeIssuesMessage(
            workflowState.TicketId,
            reviewComplete.CriticalIssues,
            string.Join("\n", reviewComplete.CriticalIssues.Concat(reviewComplete.Suggestions))
        );

        var implementationResult = await implementationGraph.ExecuteAsync(fixMessage, cancellationToken);
        await HandleGraphResultAsync(workflowState, implementationResult, cancellationToken);
    }

    /// <summary>
    /// Complete the workflow successfully
    /// </summary>
    private async Task CompleteWorkflowAsync(WorkflowState workflowState, bool hasWarnings = false)
    {
        workflowState.Status = WorkflowStatus.Completed;
        workflowState.CompletedAt = DateTime.UtcNow;
        if (hasWarnings)
        {
            workflowState.CurrentState = "completed_with_warnings";
        }
        await workflowStateStore.SaveStateAsync(workflowState);

        await eventPublisher.PublishAsync(new WorkflowCompletedEvent
        {
            TicketId = workflowState.TicketId,
            WorkflowId = workflowState.WorkflowId,
            CompletedAt = DateTime.UtcNow,
            Duration = DateTime.UtcNow - workflowState.StartedAt
        });
    }
}

/// <summary>
/// Interface for workflow orchestration
/// </summary>
public interface IWorkflowOrchestrator
{
    Task<Guid> StartWorkflowAsync(TriggerTicketMessage triggerMessage, CancellationToken cancellationToken = default);
    Task ResumeWorkflowAsync(Guid ticketId, IAgentMessage resumeMessage, CancellationToken cancellationToken = default);
    Task<WorkflowStatus> GetWorkflowStatusAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task CancelWorkflowAsync(Guid ticketId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Workflow state model
/// </summary>
public class WorkflowState
{
    public Guid WorkflowId { get; set; }
    public Guid TicketId { get; set; }
    public string CurrentGraph { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Workflow status enum
/// </summary>
public enum WorkflowStatus
{
    NotFound,
    Running,
    Suspended,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Interface for workflow state persistence
/// </summary>
public interface IWorkflowStateStore
{
    Task SaveStateAsync(WorkflowState state);
    Task<WorkflowState?> GetByTicketIdAsync(Guid ticketId);
    Task<WorkflowState?> GetByWorkflowIdAsync(Guid workflowId);
    Task UpdateStatusAsync(Guid workflowId, WorkflowStatus status, string? error = null);
}

/// <summary>
/// Interface for event publishing
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
}

// Event types for workflow transitions
public class WorkflowSuspendedEvent
{
    public Guid TicketId { get; set; }
    public Guid WorkflowId { get; set; }
    public string GraphId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime SuspendedAt { get; set; }
}

public class WorkflowCompletedEvent
{
    public Guid TicketId { get; set; }
    public Guid WorkflowId { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
}

public class WorkflowFailedEvent
{
    public Guid TicketId { get; set; }
    public Guid WorkflowId { get; set; }
    public string GraphId { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime FailedAt { get; set; }
}

public class WorkflowCancelledEvent
{
    public Guid TicketId { get; set; }
    public Guid WorkflowId { get; set; }
    public DateTime CancelledAt { get; set; }
}
