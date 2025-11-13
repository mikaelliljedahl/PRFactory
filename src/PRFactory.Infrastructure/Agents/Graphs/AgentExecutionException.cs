using System;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// Exception thrown when agent execution fails.
    /// Wraps agent-level errors for graph-level error handling.
    /// </summary>
    public class AgentExecutionException : Exception
    {
        /// <summary>
        /// Detailed error information for debugging
        /// </summary>
        public string? ErrorDetails { get; }

        /// <summary>
        /// The agent type that failed
        /// </summary>
        public string? AgentType { get; }

        /// <summary>
        /// The ticket ID being processed when the error occurred
        /// </summary>
        public Guid? TicketId { get; }

        public AgentExecutionException(string message)
            : base(message)
        {
        }

        public AgentExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AgentExecutionException(string message, string? errorDetails)
            : base(message)
        {
            ErrorDetails = errorDetails;
        }

        public AgentExecutionException(
            string message,
            string? errorDetails,
            string? agentType,
            Guid? ticketId = null)
            : base(message)
        {
            ErrorDetails = errorDetails;
            AgentType = agentType;
            TicketId = ticketId;
        }

        public AgentExecutionException(
            string message,
            Exception innerException,
            string? agentType,
            Guid? ticketId = null)
            : base(message, innerException)
        {
            AgentType = agentType;
            TicketId = ticketId;
        }

        /// <summary>
        /// Gets a formatted error message including all context
        /// </summary>
        public override string ToString()
        {
            List<string> parts = [base.ToString()];

            if (!string.IsNullOrEmpty(AgentType))
            {
                parts.Add($"Agent Type: {AgentType}");
            }

            if (TicketId.HasValue)
            {
                parts.Add($"Ticket ID: {TicketId}");
            }

            if (!string.IsNullOrEmpty(ErrorDetails))
            {
                parts.Add($"Error Details: {ErrorDetails}");
            }

            return string.Join(Environment.NewLine, parts);
        }
    }
}
