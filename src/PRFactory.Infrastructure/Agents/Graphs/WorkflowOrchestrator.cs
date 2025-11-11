using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// WorkflowOrchestrator - Main orchestrator for multi-graph workflows
    ///
    /// Chains graphs: Refinement → Planning → [Implementation] → [CodeReview]
    /// - Event-driven transitions between graphs
    /// - Handles webhook resume after HumanWait
    /// - Maintains overall workflow state
    /// - Supports code review loop (Implementation ↔ CodeReview)
    /// </summary>
    public class WorkflowOrchestrator : IWorkflowOrchestrator
    {
        private readonly ILogger<WorkflowOrchestrator> _logger;
        private readonly RefinementGraph _refinementGraph;
        private readonly PlanningGraph _planningGraph;
        private readonly ImplementationGraph _implementationGraph;
        private readonly CodeReviewGraph _codeReviewGraph;
        private readonly IWorkflowStateStore _workflowStateStore;
        private readonly IEventPublisher _eventPublisher;
        private readonly Configuration.ITenantConfigurationService _tenantConfigService;

        public WorkflowOrchestrator(
            ILogger<WorkflowOrchestrator> logger,
            RefinementGraph refinementGraph,
            PlanningGraph planningGraph,
            ImplementationGraph implementationGraph,
            CodeReviewGraph codeReviewGraph,
            IWorkflowStateStore workflowStateStore,
            IEventPublisher eventPublisher,
            Configuration.ITenantConfigurationService tenantConfigService)
        {
            _logger = logger;
            _refinementGraph = refinementGraph;
            _planningGraph = planningGraph;
            _implementationGraph = implementationGraph;
            _codeReviewGraph = codeReviewGraph;
            _workflowStateStore = workflowStateStore;
            _eventPublisher = eventPublisher;
            _tenantConfigService = tenantConfigService;
        }

        /// <summary>
        /// Start a new workflow from a trigger message
        /// </summary>
        public async Task<Guid> StartWorkflowAsync(
            TriggerTicketMessage triggerMessage,
            CancellationToken cancellationToken = default)
        {
            var workflowId = Guid.NewGuid();
            var ticketId = triggerMessage.TicketId;

            _logger.LogInformation(
                "Starting workflow {WorkflowId} for ticket {TicketId}",
                workflowId, ticketId);

            try
            {
                // Create workflow state
                var workflowState = new WorkflowState
                {
                    WorkflowId = workflowId,
                    TicketId = ticketId,
                    CurrentGraph = "RefinementGraph",
                    Status = WorkflowStatus.Running,
                    StartedAt = DateTime.UtcNow
                };
                await _workflowStateStore.SaveStateAsync(workflowState);

                // Execute RefinementGraph
                var result = await _refinementGraph.ExecuteAsync(triggerMessage, cancellationToken);

                // Update workflow state based on result
                await HandleGraphResultAsync(workflowState, result, cancellationToken);

                _logger.LogInformation(
                    "Workflow {WorkflowId} for ticket {TicketId} started successfully, current state: {State}",
                    workflowId, ticketId, result.State);

                return workflowId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to start workflow for ticket {TicketId}",
                    ticketId);

                await _workflowStateStore.UpdateStatusAsync(
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
            _logger.LogInformation(
                "Resuming workflow for ticket {TicketId} with message type {MessageType}",
                ticketId, resumeMessage.GetType().Name);

            try
            {
                // Load workflow state
                var workflowState = await _workflowStateStore.GetByTicketIdAsync(ticketId);
                if (workflowState == null)
                {
                    throw new InvalidOperationException($"No workflow found for ticket {ticketId}");
                }

                if (workflowState.Status != WorkflowStatus.Suspended)
                {
                    throw new InvalidOperationException(
                        $"Workflow {workflowState.WorkflowId} is not in suspended state (current: {workflowState.Status})");
                }

                // Update status to running
                workflowState.Status = WorkflowStatus.Running;
                await _workflowStateStore.SaveStateAsync(workflowState);

                // Resume the appropriate graph based on current state
                GraphExecutionResult result;
                switch (workflowState.CurrentGraph)
                {
                    case "RefinementGraph":
                        result = await _refinementGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                        break;

                    case "PlanningGraph":
                        result = await _planningGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                        break;

                    case "ImplementationGraph":
                        result = await _implementationGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                        break;

                    case "CodeReviewGraph":
                        result = await _codeReviewGraph.ResumeAsync(ticketId, resumeMessage, cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unknown graph: {workflowState.CurrentGraph}");
                }

                // Handle the result and potentially transition to next graph
                await HandleGraphResultAsync(workflowState, result, cancellationToken);

                _logger.LogInformation(
                    "Workflow resumed for ticket {TicketId}, current state: {State}",
                    ticketId, result.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resume workflow for ticket {TicketId}",
                    ticketId);

                var workflowState = await _workflowStateStore.GetByTicketIdAsync(ticketId);
                if (workflowState != null)
                {
                    await _workflowStateStore.UpdateStatusAsync(
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
            var workflowState = await _workflowStateStore.GetByTicketIdAsync(ticketId);
            return workflowState?.Status ?? WorkflowStatus.NotFound;
        }

        /// <summary>
        /// Cancel a running workflow
        /// </summary>
        public async Task CancelWorkflowAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cancelling workflow for ticket {TicketId}", ticketId);

            var workflowState = await _workflowStateStore.GetByTicketIdAsync(ticketId);
            if (workflowState == null)
            {
                throw new InvalidOperationException($"No workflow found for ticket {ticketId}");
            }

            await _workflowStateStore.UpdateStatusAsync(
                workflowState.WorkflowId,
                WorkflowStatus.Cancelled);

            await _eventPublisher.PublishAsync(new WorkflowCancelledEvent
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
                _logger.LogError(
                    "Graph {GraphId} failed for ticket {TicketId}: {Error}",
                    workflowState.CurrentGraph, workflowState.TicketId, result.Error?.Message);

                workflowState.Status = WorkflowStatus.Failed;
                workflowState.ErrorMessage = result.Error?.Message;
                workflowState.CompletedAt = DateTime.UtcNow;
                await _workflowStateStore.SaveStateAsync(workflowState);

                await _eventPublisher.PublishAsync(new WorkflowFailedEvent
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
                _logger.LogInformation(
                    "Graph {GraphId} suspended for ticket {TicketId} at state {State}",
                    workflowState.CurrentGraph, workflowState.TicketId, result.State);

                workflowState.Status = WorkflowStatus.Suspended;
                workflowState.CurrentState = result.State;
                await _workflowStateStore.SaveStateAsync(workflowState);

                await _eventPublisher.PublishAsync(new WorkflowSuspendedEvent
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
                case "RefinementGraph":
                    if (result.OutputMessage is RefinementCompleteEvent)
                    {
                        _logger.LogInformation(
                            "RefinementGraph completed for ticket {TicketId}, transitioning to PlanningGraph",
                            workflowState.TicketId);

                        // Transition to PlanningGraph
                        workflowState.CurrentGraph = "PlanningGraph";
                        workflowState.Status = WorkflowStatus.Running;
                        await _workflowStateStore.SaveStateAsync(workflowState);

                        // Execute PlanningGraph with answers from RefinementGraph
                        var planningResult = await _planningGraph.ExecuteAsync(
                            result.OutputMessage, cancellationToken);
                        await HandleGraphResultAsync(workflowState, planningResult, cancellationToken);
                    }
                    break;

                case "PlanningGraph":
                    if (result.OutputMessage is PlanApprovedEvent planApproved)
                    {
                        _logger.LogInformation(
                            "PlanningGraph completed with approval for ticket {TicketId}, checking for implementation",
                            workflowState.TicketId);

                        // Create plan approved message for ImplementationGraph
                        var planApprovedMessage = new PlanApprovedMessage(
                            workflowState.TicketId,
                            planApproved.ApprovedAt,
                            "system"
                        );

                        // Transition to ImplementationGraph
                        workflowState.CurrentGraph = "ImplementationGraph";
                        workflowState.Status = WorkflowStatus.Running;
                        await _workflowStateStore.SaveStateAsync(workflowState);

                        // Execute ImplementationGraph (it will check if auto-implementation is enabled)
                        var implementationResult = await _implementationGraph.ExecuteAsync(
                            planApprovedMessage, cancellationToken);
                        await HandleGraphResultAsync(workflowState, implementationResult, cancellationToken);
                    }
                    break;

                case "ImplementationGraph":
                    // Check if we should transition to CodeReviewGraph or complete
                    await HandleImplementationCompletionAsync(workflowState, result, cancellationToken);
                    break;

                case "CodeReviewGraph":
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
                _logger.LogInformation(
                    "ImplementationGraph completed without PR for ticket {TicketId}, workflow finished",
                    workflowState.TicketId);

                await CompleteWorkflowAsync(workflowState);
                return;
            }

            // Check if auto code review is enabled
            var tenantConfig = await _tenantConfigService.GetConfigurationForTicketAsync(
                workflowState.TicketId, cancellationToken);

            if (tenantConfig?.EnableAutoCodeReview != true)
            {
                _logger.LogInformation(
                    "Auto code review disabled for ticket {TicketId}, workflow finished",
                    workflowState.TicketId);

                await CompleteWorkflowAsync(workflowState);
                return;
            }

            // Transition to CodeReviewGraph
            _logger.LogInformation(
                "ImplementationGraph completed with PR #{PrNumber} for ticket {TicketId}, transitioning to CodeReviewGraph",
                prCreated.PullRequestNumber, workflowState.TicketId);

            workflowState.CurrentGraph = "CodeReviewGraph";
            workflowState.Status = WorkflowStatus.Running;
            await _workflowStateStore.SaveStateAsync(workflowState);

            // Create ReviewCodeMessage and execute CodeReviewGraph
            var reviewMessage = new ReviewCodeMessage(
                workflowState.TicketId,
                prCreated.PullRequestNumber,
                prCreated.PullRequestUrl,
                BranchName: string.Empty, // TODO: Get from context if needed
                PlanPath: null
            );

            var reviewResult = await _codeReviewGraph.ExecuteAsync(reviewMessage, cancellationToken);
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
                _logger.LogWarning(
                    "CodeReviewGraph completed with unexpected message type for ticket {TicketId}",
                    workflowState.TicketId);

                await CompleteWorkflowAsync(workflowState);
                return;
            }

            // Check if there are critical issues
            if (!reviewComplete.HasCriticalIssues || reviewComplete.CriticalIssues.Count == 0)
            {
                _logger.LogInformation(
                    "CodeReviewGraph completed with no critical issues for ticket {TicketId}, workflow finished",
                    workflowState.TicketId);

                // Check if auto-approval should be posted
                var tenantConfig = await _tenantConfigService.GetConfigurationForTicketAsync(
                    workflowState.TicketId, cancellationToken);

                if (tenantConfig?.AutoApproveIfNoIssues == true)
                {
                    _logger.LogInformation(
                        "Posting auto-approval comment for ticket {TicketId}",
                        workflowState.TicketId);
                    // TODO: Post approval comment to PR
                }

                await CompleteWorkflowAsync(workflowState);
                return;
            }

            // Get retry count from workflow state
            var retryCount = workflowState.CurrentState.Contains("retry_count:")
                ? int.Parse(workflowState.CurrentState.Split("retry_count:")[1].Split(',')[0])
                : 0;

            var tenantConfig2 = await _tenantConfigService.GetConfigurationForTicketAsync(
                workflowState.TicketId, cancellationToken);
            var maxIterations = tenantConfig2?.MaxCodeReviewIterations ?? 3;

            if (retryCount >= maxIterations)
            {
                _logger.LogWarning(
                    "CodeReviewGraph max iterations ({MaxIterations}) reached for ticket {TicketId}, completing with warnings",
                    maxIterations, workflowState.TicketId);

                await CompleteWorkflowAsync(workflowState, hasWarnings: true);
                return;
            }

            // Loop back to ImplementationGraph to fix issues
            _logger.LogInformation(
                "CodeReviewGraph found {IssueCount} issues for ticket {TicketId}, looping back to ImplementationGraph (attempt {Attempt}/{MaxIterations})",
                reviewComplete.CriticalIssues.Count, workflowState.TicketId, retryCount + 1, maxIterations);

            workflowState.CurrentGraph = "ImplementationGraph";
            workflowState.CurrentState = $"retry_count:{retryCount + 1},issues_found";
            workflowState.Status = WorkflowStatus.Running;
            await _workflowStateStore.SaveStateAsync(workflowState);

            // Create FixCodeIssuesMessage and execute ImplementationGraph
            var fixMessage = new FixCodeIssuesMessage(
                workflowState.TicketId,
                reviewComplete.CriticalIssues,
                string.Join("\n", reviewComplete.CriticalIssues.Concat(reviewComplete.Suggestions))
            );

            var implementationResult = await _implementationGraph.ExecuteAsync(fixMessage, cancellationToken);
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
            await _workflowStateStore.SaveStateAsync(workflowState);

            await _eventPublisher.PublishAsync(new WorkflowCompletedEvent
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
}
