using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// PlanningGraph - Plan generation workflow
    /// Flow: Planning → GitPlan + JiraPost (parallel) → HumanWait (approval)
    ///
    /// Features:
    /// - Conditional: If approved → emit "plan_approved", else → back to Planning
    /// - Parallel: GitPlan + JiraPost can run concurrently
    /// - Checkpoint after plan generation
    /// </summary>
    public class PlanningGraph : AgentGraphBase
    {
        private readonly IAgentExecutor _agentExecutor;

        public override string GraphId => "PlanningGraph";

        public PlanningGraph(
            ILogger<PlanningGraph> logger,
            ICheckpointStore checkpointStore,
            IAgentExecutor agentExecutor)
            : base(logger, checkpointStore)
        {
            _agentExecutor = agentExecutor;
        }

        protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Input should be AnswersReceivedMessage or PlanRejectedMessage (for retry)
                return await GenerateAndPostPlanAsync(inputMessage, context, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PlanningGraph failed for ticket {TicketId}", context.TicketId);
                context.State["is_failed"] = true;
                context.State["error"] = ex.Message;
                await SaveCheckpointAsync(context, "failed", "unknown");
                return GraphExecutionResult.Failure("failed", ex);
            }
        }

        protected override async Task<GraphExecutionResult> ResumeCoreAsync(
            IAgentMessage resumeMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var currentState = context.State.TryGetValue("current_state", out var state)
                    ? state?.ToString()
                    : "unknown";

                Logger.LogInformation(
                    "Resuming PlanningGraph for ticket {TicketId} from state {State}",
                    context.TicketId, currentState);

                if (currentState == "awaiting_approval")
                {
                    // Check if approved or rejected
                    if (resumeMessage is PlanApprovedMessage approvedMessage)
                    {
                        return await HandlePlanApprovalAsync(approvedMessage, context, cancellationToken, startTime);
                    }
                    else if (resumeMessage is PlanRejectedMessage rejectedMessage)
                    {
                        return await HandlePlanRejectionAsync(rejectedMessage, context, cancellationToken);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Expected PlanApprovedMessage or PlanRejectedMessage, got {resumeMessage.GetType().Name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot resume from state {currentState}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to resume PlanningGraph for ticket {TicketId}", context.TicketId);
                context.State["is_failed"] = true;
                context.State["error"] = ex.Message;
                await SaveCheckpointAsync(context, "resume_failed", "unknown");
                return GraphExecutionResult.Failure("resume_failed", ex);
            }
        }

        /// <summary>
        /// Generate plan and post to Jira (with parallel execution)
        /// </summary>
        private async Task<GraphExecutionResult> GenerateAndPostPlanAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            // Track if this is a retry
            var retryCount = context.State.TryGetValue("plan_retry_count", out var count)
                ? Convert.ToInt32(count)
                : 0;

            if (retryCount > 0)
            {
                Logger.LogInformation(
                    "Regenerating plan for ticket {TicketId}, attempt {Attempt}",
                    context.TicketId, retryCount + 1);
            }

            // Stage 1: Planning Agent
            Logger.LogInformation("Stage 1: Planning Agent for ticket {TicketId}", context.TicketId);
            var planMessage = await ExecuteAgentAsync<PlanningAgent>(
                inputMessage, context, "planning", cancellationToken);
            await SaveCheckpointAsync(context, "plan_generated", "PlanningAgent");

            if (planMessage is not PlanGeneratedMessage plan)
            {
                throw new InvalidOperationException(
                    $"Expected PlanGeneratedMessage from PlanningAgent, got {planMessage?.GetType().Name}");
            }

            // Stage 2 & 3: Parallel execution of GitPlan and JiraPost
            Logger.LogInformation(
                "Stage 2/3: Executing GitPlan and JiraPost in parallel for ticket {TicketId}",
                context.TicketId);

            var gitTask = ExecuteAgentAsync<GitPlanAgent>(plan, context, "git_plan", cancellationToken);
            var jiraTask = ExecuteAgentAsync<JiraPostAgent>(plan, context, "jira_post", cancellationToken);

            // Wait for both to complete
            var results = await Task.WhenAll(gitTask, jiraTask);
            var gitResult = results[0];
            var jiraResult = results[1];

            // Save checkpoint after parallel execution
            context.State["git_commit_sha"] = gitResult is PlanCommittedMessage committed ? committed.CommitSha : null;
            context.State["jira_posted"] = jiraResult is MessagePostedMessage;
            await SaveCheckpointAsync(context, "plan_posted", "GitPlanAgent,JiraPostAgent");

            // Stage 4: Enter suspended state awaiting human approval
            Logger.LogInformation(
                "Stage 4: Awaiting plan approval for ticket {TicketId}",
                context.TicketId);

            context.State["is_suspended"] = true;
            context.State["waiting_for"] = "plan_approval";
            context.State["plan_retry_count"] = retryCount;
            await SaveCheckpointAsync(context, "awaiting_approval", "HumanWaitAgent");

            return GraphExecutionResult.Suspended("awaiting_approval", jiraResult);
        }

        /// <summary>
        /// Handle plan approval - emit plan_approved event
        /// </summary>
        private async Task<GraphExecutionResult> HandlePlanApprovalAsync(
            PlanApprovedMessage approvedMessage,
            GraphContext context,
            CancellationToken cancellationToken,
            DateTime startTime)
        {
            Logger.LogInformation(
                "Plan approved for ticket {TicketId} by {ApprovedBy}",
                context.TicketId, approvedMessage.ApprovedBy);

            // Mark as completed
            context.State["is_completed"] = true;
            context.State["is_suspended"] = false;
            context.State["approved_by"] = approvedMessage.ApprovedBy;
            context.State["approved_at"] = approvedMessage.ApprovedAt;
            await SaveCheckpointAsync(context, "plan_approved", "CompletedAgent");

            var duration = DateTime.UtcNow - startTime;

            // Emit plan_approved event for WorkflowOrchestrator
            var approvedEvent = new PlanApprovedEvent(
                context.TicketId,
                approvedMessage.ApprovedAt
            );

            Logger.LogInformation(
                "PlanningGraph completed with approval for ticket {TicketId} in {Duration}ms",
                context.TicketId, duration.TotalMilliseconds);

            return GraphExecutionResult.Success("plan_approved", approvedEvent, duration);
        }

        /// <summary>
        /// Handle plan rejection - loop back to Planning
        /// </summary>
        private async Task<GraphExecutionResult> HandlePlanRejectionAsync(
            PlanRejectedMessage rejectedMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation(
                "Plan rejected for ticket {TicketId}, reason: {Reason}",
                context.TicketId, rejectedMessage.Reason);

            // Increment retry count
            var retryCount = context.State.TryGetValue("plan_retry_count", out var count)
                ? Convert.ToInt32(count)
                : 0;
            retryCount++;

            context.State["plan_retry_count"] = retryCount;
            context.State["rejection_reason"] = rejectedMessage.Reason;
            context.State["is_suspended"] = false;
            await SaveCheckpointAsync(context, "plan_rejected", "PlanningAgent");

            // Check if we should limit retries
            const int maxRetries = 5;
            if (retryCount > maxRetries)
            {
                Logger.LogError(
                    "Plan rejected too many times ({RetryCount}) for ticket {TicketId}, failing workflow",
                    retryCount, context.TicketId);

                context.State["is_failed"] = true;
                await SaveCheckpointAsync(context, "too_many_rejections", "FailedAgent");
                return GraphExecutionResult.Failure(
                    "too_many_rejections",
                    new InvalidOperationException($"Plan rejected {retryCount} times, exceeding maximum retries"));
            }

            // Loop back to planning - execute the full cycle again
            Logger.LogInformation(
                "Looping back to Planning Agent for ticket {TicketId}, attempt {Attempt}",
                context.TicketId, retryCount + 1);

            // Create a new input message with the rejection context
            var retryMessage = new AnswersReceivedMessage(
                context.TicketId,
                new Dictionary<string, string>
                {
                    ["rejection_reason"] = rejectedMessage.Reason,
                    ["retry_attempt"] = retryCount.ToString()
                }
            );

            return await GenerateAndPostPlanAsync(retryMessage, context, cancellationToken);
        }

        /// <summary>
        /// Execute a specific agent and return the result message
        /// </summary>
        private async Task<IAgentMessage> ExecuteAgentAsync<TAgent>(
            IAgentMessage inputMessage,
            GraphContext context,
            string stage,
            CancellationToken cancellationToken)
        {
            context.State["current_stage"] = stage;
            return await _agentExecutor.ExecuteAsync<TAgent>(inputMessage, context, cancellationToken);
        }
    }

    // Placeholder agent types
    public class PlanningAgent { }
    public class GitPlanAgent { }
}
