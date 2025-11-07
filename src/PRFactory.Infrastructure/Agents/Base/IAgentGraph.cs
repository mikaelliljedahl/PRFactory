using System;
using System.Threading;
using System.Threading.Tasks;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Base
{
    /// <summary>
    /// Interface for agent graph execution
    /// </summary>
    public interface IAgentGraph
    {
        /// <summary>
        /// Unique identifier for the graph
        /// </summary>
        string GraphId { get; }

        /// <summary>
        /// Execute the graph with the given input message
        /// </summary>
        Task<GraphExecutionResult> ExecuteAsync(IAgentMessage inputMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume the graph from a checkpoint
        /// </summary>
        Task<GraphExecutionResult> ResumeAsync(Guid ticketId, IAgentMessage resumeMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the current status of a graph execution
        /// </summary>
        Task<GraphStatus> GetStatusAsync(Guid ticketId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of graph execution
    /// </summary>
    public class GraphExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string State { get; set; } = string.Empty;
        public IAgentMessage? OutputMessage { get; set; }
        public Exception? Error { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }

        public static GraphExecutionResult Success(string state, IAgentMessage outputMessage, TimeSpan duration)
        {
            return new GraphExecutionResult
            {
                IsSuccess = true,
                State = state,
                OutputMessage = outputMessage,
                CompletedAt = DateTime.UtcNow,
                Duration = duration
            };
        }

        public static GraphExecutionResult Failure(string state, Exception error)
        {
            return new GraphExecutionResult
            {
                IsSuccess = false,
                State = state,
                Error = error,
                CompletedAt = DateTime.UtcNow
            };
        }

        public static GraphExecutionResult Suspended(string state, IAgentMessage outputMessage)
        {
            return new GraphExecutionResult
            {
                IsSuccess = true,
                State = state,
                OutputMessage = outputMessage,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Status of a graph execution
    /// </summary>
    public class GraphStatus
    {
        public Guid TicketId { get; set; }
        public string GraphId { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string CurrentAgent { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFailed { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Context for graph execution
    /// </summary>
    public class GraphContext
    {
        public Guid TicketId { get; set; }
        public string GraphId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public Dictionary<string, object> State { get; set; } = new();
        public int RetryCount { get; set; }
        public string? CurrentCheckpoint { get; set; }
    }
}
