using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// ImplementationGraph - Code implementation workflow (Optional)
    /// Flow: Implementation → GitCommit → PullRequest + JiraPost (parallel) → Completion
    ///
    /// Features:
    /// - Conditional: Only runs if auto-implementation enabled in TenantConfiguration
    /// - Parallel: PullRequest + JiraPost can run concurrently
    /// - Checkpoint after implementation and PR creation
    /// </summary>
    public class ImplementationGraph : AgentGraphBase
    {
        private readonly IAgentExecutor _agentExecutor;
        private readonly ITenantConfigurationService _tenantConfigService;

        public override string GraphId => "ImplementationGraph";

        public ImplementationGraph(
            ILogger<ImplementationGraph> logger,
            ICheckpointStore checkpointStore,
            IAgentExecutor agentExecutor,
            ITenantConfigurationService tenantConfigService)
            : base(logger, checkpointStore)
        {
            _agentExecutor = agentExecutor;
            _tenantConfigService = tenantConfigService;
        }

        protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Input should be PlanApprovedMessage
                if (inputMessage is not PlanApprovedMessage planApproved)
                {
                    throw new InvalidOperationException(
                        $"Expected PlanApprovedMessage, got {inputMessage?.GetType().Name}");
                }

                // Check if auto-implementation is enabled for this tenant
                var isAutoImplementEnabled = await CheckAutoImplementationEnabledAsync(
                    context.TicketId, cancellationToken);

                if (!isAutoImplementEnabled)
                {
                    Logger.LogInformation(
                        "Auto-implementation disabled for ticket {TicketId}, skipping ImplementationGraph",
                        context.TicketId);

                    context.State["is_completed"] = true;
                    context.State["skipped"] = true;
                    context.State["skip_reason"] = "auto_implementation_disabled";
                    await SaveCheckpointAsync(context, "skipped", "ConfigurationCheck");

                    var duration = DateTime.UtcNow - startTime;
                    return GraphExecutionResult.Success("skipped", planApproved, duration);
                }

                // Stage 1: Implementation Agent
                Logger.LogInformation("Stage 1: Implementation Agent for ticket {TicketId}", context.TicketId);
                var implementedMessage = await ExecuteAgentAsync<ImplementationAgent>(
                    planApproved, context, "implementation", cancellationToken);
                await SaveCheckpointAsync(context, "code_implemented", "ImplementationAgent");

                if (implementedMessage is not CodeImplementedMessage codeImplemented)
                {
                    throw new InvalidOperationException(
                        $"Expected CodeImplementedMessage from ImplementationAgent, got {implementedMessage?.GetType().Name}");
                }

                // Stage 2: Git Commit Agent
                Logger.LogInformation("Stage 2: Git Commit Agent for ticket {TicketId}", context.TicketId);
                var committedMessage = await ExecuteAgentAsync<GitCommitAgent>(
                    codeImplemented, context, "git_commit", cancellationToken);
                await SaveCheckpointAsync(context, "code_committed", "GitCommitAgent");

                // Stage 3 & 4: Parallel execution of PullRequest and JiraPost
                Logger.LogInformation(
                    "Stage 3/4: Creating Pull Request and posting to Jira in parallel for ticket {TicketId}",
                    context.TicketId);

                var prTask = ExecuteAgentAsync<PullRequestAgent>(
                    committedMessage, context, "pull_request", cancellationToken);
                var jiraTask = ExecuteAgentAsync<JiraPostAgent>(
                    committedMessage, context, "jira_post", cancellationToken);

                // Wait for both to complete
                var results = await Task.WhenAll(prTask, jiraTask);
                var prResult = results[0];
                var jiraResult = results[1];

                // Save checkpoint after parallel execution
                if (prResult is PRCreatedMessage prCreated)
                {
                    context.State["pr_number"] = prCreated.PullRequestNumber;
                    context.State["pr_url"] = prCreated.PullRequestUrl;
                }
                context.State["jira_posted"] = jiraResult is MessagePostedMessage;
                await SaveCheckpointAsync(context, "pr_created", "PullRequestAgent,JiraPostAgent");

                // Stage 5: Completion Agent
                Logger.LogInformation("Stage 5: Completion Agent for ticket {TicketId}", context.TicketId);
                var completionMessage = await ExecuteAgentAsync<CompletionAgent>(
                    prResult, context, "completion", cancellationToken);
                await SaveCheckpointAsync(context, "completed", "CompletionAgent");

                // Mark as completed
                context.State["is_completed"] = true;
                var totalDuration = DateTime.UtcNow - startTime;

                Logger.LogInformation(
                    "ImplementationGraph completed for ticket {TicketId} in {Duration}ms",
                    context.TicketId, totalDuration.TotalMilliseconds);

                return GraphExecutionResult.Success("completed", completionMessage, totalDuration);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ImplementationGraph failed for ticket {TicketId}", context.TicketId);
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
            // ImplementationGraph doesn't have human-in-the-loop, so resume is not typical
            // However, it could be resumed if the graph crashed mid-execution

            var currentState = context.State.TryGetValue("current_state", out var state)
                ? state?.ToString()
                : "unknown";

            Logger.LogWarning(
                "Attempting to resume ImplementationGraph for ticket {TicketId} from state {State}. " +
                "This graph typically runs to completion without suspension.",
                context.TicketId, currentState);

            // For now, restart from the beginning with the original plan approved message
            // In a production system, you might want to implement more granular resume logic
            if (resumeMessage is PlanApprovedMessage planApproved)
            {
                return await ExecuteCoreAsync(planApproved, context, cancellationToken);
            }

            throw new InvalidOperationException(
                $"Cannot resume ImplementationGraph from state {currentState} with message type {resumeMessage.GetType().Name}");
        }

        /// <summary>
        /// Check if auto-implementation is enabled for the tenant
        /// </summary>
        private async Task<bool> CheckAutoImplementationEnabledAsync(
            Guid ticketId,
            CancellationToken cancellationToken)
        {
            try
            {
                var config = await _tenantConfigService.GetConfigurationForTicketAsync(
                    ticketId, cancellationToken);

                return config?.AutoImplementAfterPlanApproval ?? false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    ex,
                    "Failed to check auto-implementation configuration for ticket {TicketId}, defaulting to false",
                    ticketId);
                return false;
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
    /// Interface for tenant configuration service
    /// </summary>
    public interface ITenantConfigurationService
    {
        Task<TenantConfiguration> GetConfigurationForTicketAsync(Guid ticketId, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Tenant configuration model
    /// </summary>
    public class TenantConfiguration
    {
        public Guid TenantId { get; set; }
        public bool AutoImplementAfterPlanApproval { get; set; }
        public int MaxTokensPerRequest { get; set; }
        public bool EnableCodeReview { get; set; }
        public string[] AllowedRepositories { get; set; }
    }

    // Placeholder agent types
    public class ImplementationAgent { }
    public class GitCommitAgent { }
    public class PullRequestAgent { }
    public class CompletionAgent { }
}
