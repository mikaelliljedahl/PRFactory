using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// RefinementGraph - Ticket refinement workflow
    /// Flow: Trigger → RepositoryClone → Analysis → QuestionGeneration → JiraPost → HumanWait → AnswerProcessing →
    ///       TicketUpdateGeneration → TicketUpdateReview → [if approved] → TicketUpdatePosted → Complete
    ///                                                   → [if rejected] → Retry TicketUpdateGeneration
    ///
    /// Features:
    /// - On success: Emit "refinement_complete" event
    /// - On failure: Retry analysis up to 3 times
    /// - Retry ticket update generation up to 3 times on rejection
    /// - Checkpoint after each agent
    /// </summary>
    public class RefinementGraph : AgentGraphBase
    {
        private const int MaxAnalysisRetries = 3;
        private const int MaxTicketUpdateRetries = 3;
        private readonly IAgentExecutor _agentExecutor;

        public override string GraphId => "RefinementGraph";

        public RefinementGraph(
            ILogger<RefinementGraph> logger,
            Base.ICheckpointStore checkpointStore,
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
            IAgentMessage currentMessage = inputMessage;

            try
            {
                // Stage 1: Trigger Agent
                Logger.LogInformation("Stage 1: Trigger Agent for ticket {TicketId}", context.TicketId);
                currentMessage = await ExecuteAgentAsync<TriggerAgent>(
                    currentMessage, context, "trigger", cancellationToken);
                await SaveCheckpointAsync(context, "trigger_complete", "TriggerAgent");

                // Stage 2: Repository Clone Agent (parallel with initial setup)
                Logger.LogInformation("Stage 2: Repository Clone Agent for ticket {TicketId}", context.TicketId);
                currentMessage = await ExecuteAgentAsync<RepositoryCloneAgent>(
                    currentMessage, context, "repository_clone", cancellationToken);
                await SaveCheckpointAsync(context, "clone_complete", "RepositoryCloneAgent");

                // Stage 3: Analysis Agent (with retry logic)
                Logger.LogInformation("Stage 3: Analysis Agent for ticket {TicketId}", context.TicketId);
                currentMessage = await ExecuteAnalysisWithRetryAsync(
                    currentMessage, context, cancellationToken);
                await SaveCheckpointAsync(context, "analysis_complete", "AnalysisAgent");

                // Stage 4: Question Generation Agent
                Logger.LogInformation("Stage 4: Question Generation Agent for ticket {TicketId}", context.TicketId);
                currentMessage = await ExecuteAgentAsync<QuestionGenerationAgent>(
                    currentMessage, context, "question_generation", cancellationToken);
                await SaveCheckpointAsync(context, "questions_generated", "QuestionGenerationAgent");

                // Stage 5: Jira Post Agent
                Logger.LogInformation("Stage 5: Jira Post Agent for ticket {TicketId}", context.TicketId);
                currentMessage = await ExecuteAgentAsync<JiraPostAgent>(
                    currentMessage, context, "jira_post", cancellationToken);
                await SaveCheckpointAsync(context, "questions_posted", "JiraPostAgent");

                // Stage 6: Human Wait Agent (suspended state)
                Logger.LogInformation("Stage 6: Human Wait Agent for ticket {TicketId} - entering suspended state", context.TicketId);
                context.State["is_suspended"] = true;
                context.State["waiting_for"] = "human_answers";
                await SaveCheckpointAsync(context, "awaiting_answers", "HumanWaitAgent");

                // Return suspended result - workflow will resume when webhook received
                return GraphExecutionResult.Suspended("awaiting_answers", currentMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RefinementGraph failed for ticket {TicketId}", context.TicketId);
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
                // Determine where to resume based on checkpoint
                var currentState = context.State.TryGetValue("current_state", out var state)
                    ? state?.ToString()
                    : "unknown";

                Logger.LogInformation(
                    "Resuming RefinementGraph for ticket {TicketId} from state {State}",
                    context.TicketId, currentState);

                // Resume from awaiting answers
                if (currentState == "awaiting_answers" && resumeMessage is AnswersReceivedMessage)
                {
                    // Stage 7: Answer Processing Agent
                    Logger.LogInformation("Stage 7: Processing received answers for ticket {TicketId}", context.TicketId);
                    var processedMessage = await ExecuteAgentAsync<AnswerProcessingAgent>(
                        resumeMessage, context, "answer_processing", cancellationToken);
                    await SaveCheckpointAsync(context, "answers_processed", "AnswerProcessingAgent");

                    // Stage 8: Ticket Update Generation Agent (with retry logic)
                    Logger.LogInformation("Stage 8: Ticket Update Generation for ticket {TicketId}", context.TicketId);
                    var ticketUpdateMessage = await ExecuteTicketUpdateGenerationWithRetryAsync(
                        processedMessage, context, cancellationToken);
                    await SaveCheckpointAsync(context, "ticket_update_generated", "TicketUpdateGenerationAgent");

                    // Stage 9: Suspend and wait for ticket update approval
                    Logger.LogInformation("Stage 9: Awaiting ticket update approval for ticket {TicketId}", context.TicketId);
                    context.State["is_suspended"] = true;
                    context.State["waiting_for"] = "ticket_update_approval";
                    await SaveCheckpointAsync(context, "awaiting_ticket_update_approval", "HumanWaitAgent");

                    return GraphExecutionResult.Suspended("awaiting_ticket_update_approval", ticketUpdateMessage);
                }
                // Resume from awaiting ticket update approval - APPROVED
                else if (currentState == "awaiting_ticket_update_approval" && resumeMessage is TicketUpdateApprovedMessage approvedMessage)
                {
                    // Stage 10: Post ticket update to Jira
                    Logger.LogInformation("Stage 10: Posting approved ticket update for ticket {TicketId}", context.TicketId);
                    var postedMessage = await ExecuteAgentAsync<TicketUpdatePostAgent>(
                        approvedMessage, context, "ticket_update_post", cancellationToken);
                    await SaveCheckpointAsync(context, "ticket_update_posted", "TicketUpdatePostAgent");

                    // Mark as completed and emit refinement_complete event
                    context.State["is_completed"] = true;
                    context.State["is_suspended"] = false;
                    await SaveCheckpointAsync(context, "refinement_complete", "CompletedAgent");

                    var duration = DateTime.UtcNow - startTime;
                    var completionEvent = new RefinementCompleteEvent(
                        context.TicketId,
                        DateTime.UtcNow
                    );

                    Logger.LogInformation(
                        "RefinementGraph completed for ticket {TicketId} in {Duration}ms",
                        context.TicketId, duration.TotalMilliseconds);

                    return GraphExecutionResult.Success("refinement_complete", completionEvent, duration);
                }
                // Resume from awaiting ticket update approval - REJECTED
                else if (currentState == "awaiting_ticket_update_approval" && resumeMessage is TicketUpdateRejectedMessage rejectedMessage)
                {
                    // Check retry count
                    var retryCount = context.State.TryGetValue("ticket_update_retry_count", out var count)
                        ? Convert.ToInt32(count)
                        : 0;

                    if (retryCount >= MaxTicketUpdateRetries)
                    {
                        Logger.LogError(
                            "Ticket update rejected {RetryCount} times for ticket {TicketId}, max retries exceeded",
                            retryCount, context.TicketId);

                        context.State["is_failed"] = true;
                        context.State["error"] = $"Ticket update rejected {retryCount} times, max retries exceeded";
                        await SaveCheckpointAsync(context, "ticket_update_failed", "unknown");

                        return GraphExecutionResult.Failure("ticket_update_failed",
                            new InvalidOperationException($"Ticket update rejected {retryCount} times"));
                    }

                    // Retry ticket update generation with rejection feedback
                    Logger.LogInformation(
                        "Ticket update rejected for ticket {TicketId}, regenerating (attempt {Attempt} of {MaxAttempts})",
                        context.TicketId, retryCount + 1, MaxTicketUpdateRetries);

                    context.State["ticket_update_retry_count"] = retryCount + 1;
                    context.State["last_rejection_reason"] = rejectedMessage.Reason;

                    // Regenerate ticket update
                    var ticketUpdateMessage = await ExecuteTicketUpdateGenerationWithRetryAsync(
                        rejectedMessage, context, cancellationToken);
                    await SaveCheckpointAsync(context, "ticket_update_regenerated", "TicketUpdateGenerationAgent");

                    // Suspend again and wait for approval
                    context.State["is_suspended"] = true;
                    context.State["waiting_for"] = "ticket_update_approval";
                    await SaveCheckpointAsync(context, "awaiting_ticket_update_approval", "HumanWaitAgent");

                    return GraphExecutionResult.Suspended("awaiting_ticket_update_approval", ticketUpdateMessage);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot resume from state {currentState} with message type {resumeMessage.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to resume RefinementGraph for ticket {TicketId}", context.TicketId);
                context.State["is_failed"] = true;
                context.State["error"] = ex.Message;
                await SaveCheckpointAsync(context, "resume_failed", "unknown");
                return GraphExecutionResult.Failure("resume_failed", ex);
            }
        }

        /// <summary>
        /// Execute Analysis Agent with retry logic (up to 3 attempts)
        /// </summary>
        private async Task<IAgentMessage> ExecuteAnalysisWithRetryAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var retryCount = context.State.TryGetValue("analysis_retry_count", out var count)
                ? Convert.ToInt32(count)
                : 0;

            for (int attempt = retryCount; attempt < MaxAnalysisRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        Logger.LogWarning(
                            "Retrying Analysis Agent for ticket {TicketId}, attempt {Attempt} of {MaxAttempts}",
                            context.TicketId, attempt + 1, MaxAnalysisRetries);

                        // Exponential backoff
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay, cancellationToken);
                    }

                    context.State["analysis_retry_count"] = attempt;
                    var result = await ExecuteAgentAsync<AnalysisAgent>(
                        inputMessage, context, "analysis", cancellationToken);

                    // Success - reset retry count
                    context.State["analysis_retry_count"] = 0;
                    return result;
                }
                catch (Exception ex) when (attempt < MaxAnalysisRetries - 1)
                {
                    Logger.LogWarning(
                        ex,
                        "Analysis Agent failed for ticket {TicketId}, attempt {Attempt} of {MaxAttempts}",
                        context.TicketId, attempt + 1, MaxAnalysisRetries);

                    context.State["last_analysis_error"] = ex.Message;
                    await SaveCheckpointAsync(context, $"analysis_retry_{attempt}", "AnalysisAgent");
                }
            }

            // All retries exhausted
            throw new InvalidOperationException(
                $"Analysis Agent failed after {MaxAnalysisRetries} attempts for ticket {context.TicketId}");
        }

        /// <summary>
        /// Execute Ticket Update Generation Agent (no automatic retries, retries happen on rejection)
        /// </summary>
        private async Task<IAgentMessage> ExecuteTicketUpdateGenerationWithRetryAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await ExecuteAgentAsync<TicketUpdateGenerationAgent>(
                    inputMessage, context, "ticket_update_generation", cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Ticket Update Generation Agent failed for ticket {TicketId}",
                    context.TicketId);
                throw;
            }
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

    /// <summary>
    /// Interface for executing agents
    /// </summary>
    public interface IAgentExecutor
    {
        Task<IAgentMessage> ExecuteAsync<TAgent>(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken);
    }

    // Agent type markers for ExecuteAgentAsync<TAgent> generic resolution
    public class TriggerAgent { }
    public class RepositoryCloneAgent { }
    public class AnalysisAgent { }
    public class QuestionGenerationAgent { }
    public class JiraPostAgent { }
    public class HumanWaitAgent { }
    public class AnswerProcessingAgent { }
    public class TicketUpdateGenerationAgent { }
    public class TicketUpdatePostAgent { }

}
