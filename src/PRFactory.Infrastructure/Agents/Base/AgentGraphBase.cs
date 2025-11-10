using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Base
{
    /// <summary>
    /// Base class for agent graphs providing common functionality
    /// </summary>
    public abstract class AgentGraphBase : IAgentGraph
    {
        protected readonly ILogger Logger;
        protected readonly ICheckpointStore CheckpointStore;
        protected readonly ActivitySource ActivitySource;

        public abstract string GraphId { get; }

        protected AgentGraphBase(
            ILogger logger,
            ICheckpointStore checkpointStore)
        {
            Logger = logger;
            CheckpointStore = checkpointStore;
            ActivitySource = new ActivitySource($"PRFactory.{GraphId}");
        }

        public virtual async Task<GraphExecutionResult> ExecuteAsync(IAgentMessage inputMessage, CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity($"{GraphId}.Execute");
            activity?.SetTag("ticket_id", inputMessage.TicketId);
            activity?.SetTag("graph_id", GraphId);

            var startTime = DateTime.UtcNow;
            var context = new GraphContext
            {
                TicketId = inputMessage.TicketId,
                GraphId = GraphId,
                StartedAt = startTime
            };

            try
            {
                Logger.LogInformation("Starting graph {GraphId} for ticket {TicketId}", GraphId, inputMessage.TicketId);

                var result = await ExecuteCoreAsync(inputMessage, context, cancellationToken);

                var duration = DateTime.UtcNow - startTime;
                Logger.LogInformation(
                    "Graph {GraphId} completed for ticket {TicketId} in {Duration}ms with state {State}",
                    GraphId, inputMessage.TicketId, duration.TotalMilliseconds, result.State);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Graph {GraphId} failed for ticket {TicketId}", GraphId, inputMessage.TicketId);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return GraphExecutionResult.Failure("failed", ex);
            }
        }

        public virtual async Task<GraphExecutionResult> ResumeAsync(Guid ticketId, IAgentMessage resumeMessage, CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity($"{GraphId}.Resume");
            activity?.SetTag("ticket_id", ticketId);
            activity?.SetTag("graph_id", GraphId);

            try
            {
                Logger.LogInformation("Resuming graph {GraphId} for ticket {TicketId}", GraphId, ticketId);

                // Load checkpoint
                var checkpoint = await CheckpointStore.LoadCheckpointAsync(ticketId, GraphId);
                if (checkpoint == null)
                {
                    throw new InvalidOperationException($"No checkpoint found for ticket {ticketId} in graph {GraphId}");
                }

                var context = new GraphContext
                {
                    TicketId = ticketId,
                    GraphId = GraphId,
                    StartedAt = checkpoint.CreatedAt,
                    State = checkpoint.State,
                    CurrentCheckpoint = checkpoint.CheckpointId
                };

                return await ResumeCoreAsync(resumeMessage, context, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to resume graph {GraphId} for ticket {TicketId}", GraphId, ticketId);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return GraphExecutionResult.Failure("resume_failed", ex);
            }
        }

        public async Task<GraphStatus> GetStatusAsync(Guid ticketId, CancellationToken cancellationToken = default)
        {
            var checkpoint = await CheckpointStore.LoadCheckpointAsync(ticketId, GraphId);
            if (checkpoint == null)
            {
                return new GraphStatus
                {
                    TicketId = ticketId,
                    GraphId = GraphId,
                    CurrentState = "not_started",
                    IsRunning = false
                };
            }

            return new GraphStatus
            {
                TicketId = ticketId,
                GraphId = GraphId,
                CurrentState = checkpoint.State.TryGetValue("current_state", out var state) ? state?.ToString() ?? "unknown" : "unknown",
                CurrentAgent = checkpoint.State.TryGetValue("current_agent", out var agent) ? agent?.ToString() ?? "unknown" : "unknown",
                IsRunning = checkpoint.State.TryGetValue("is_running", out var running) && (bool)running,
                IsSuspended = checkpoint.State.TryGetValue("is_suspended", out var suspended) && (bool)suspended,
                IsCompleted = checkpoint.State.TryGetValue("is_completed", out var completed) && (bool)completed,
                IsFailed = checkpoint.State.TryGetValue("is_failed", out var failed) && (bool)failed,
                StartedAt = checkpoint.CreatedAt,
                RetryCount = checkpoint.State.TryGetValue("retry_count", out var retryCount) ? Convert.ToInt32(retryCount) : 0
            };
        }

        protected async Task SaveCheckpointAsync(GraphContext context, string checkpointName, string currentAgent)
        {
            context.State["current_state"] = checkpointName;
            context.State["current_agent"] = currentAgent;
            context.State["last_checkpoint"] = DateTime.UtcNow;

            await CheckpointStore.SaveCheckpointAsync(
                context.TicketId,
                GraphId,
                checkpointName,
                context.State);

            Logger.LogDebug(
                "Checkpoint saved for ticket {TicketId} at {CheckpointName}",
                context.TicketId, checkpointName);
        }

        protected abstract Task<GraphExecutionResult> ExecuteCoreAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken);

        protected abstract Task<GraphExecutionResult> ResumeCoreAsync(
            IAgentMessage resumeMessage,
            GraphContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface for checkpoint storage
    /// </summary>
    public interface ICheckpointStore
    {
        Task SaveCheckpointAsync(Guid ticketId, string graphId, string checkpointId, Dictionary<string, object> state);
        Task<Checkpoint> LoadCheckpointAsync(Guid ticketId, string graphId);
        Task<List<Checkpoint>> GetCheckpointHistoryAsync(Guid ticketId, string graphId);
    }

    /// <summary>
    /// Checkpoint data structure
    /// </summary>
    public class Checkpoint
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public string GraphId { get; set; } = string.Empty;
        public string CheckpointId { get; set; } = string.Empty;
        public Dictionary<string, object> State { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
