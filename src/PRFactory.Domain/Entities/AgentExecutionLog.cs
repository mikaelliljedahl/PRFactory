namespace PRFactory.Domain.Entities;

/// <summary>
/// Audit log for agent and tool executions.
/// </summary>
public class AgentExecutionLog
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }

    /// <summary>
    /// Agent name (e.g., "AnalyzerAgent")
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Tool name if tool execution (e.g., "ReadFile"), null for agent-level logs
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Input to agent/tool (JSON serialized)
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Output from agent/tool (JSON serialized)
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Execution succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Tokens consumed (input + output)
    /// </summary>
    public int? TokensUsed { get; set; }

    public DateTime ExecutedAt { get; set; }

    // Navigation properties
    public Tenant? Tenant { get; set; }
    public Ticket? Ticket { get; set; }
}
