using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a checkpoint in the workflow graph execution.
/// Checkpoints enable graphs to suspend and resume at specific points.
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// Unique identifier for this checkpoint record in the database
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The checkpoint identifier (e.g., "after_analysis", "awaiting_answers")
    /// </summary>
    public string CheckpointId { get; private set; } = string.Empty;

    /// <summary>
    /// The tenant this checkpoint belongs to
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The ticket being processed
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// The graph that created this checkpoint (e.g., "RefinementGraph", "PlanningGraph")
    /// </summary>
    public string GraphId { get; private set; } = string.Empty;

    /// <summary>
    /// Name of the agent that created this checkpoint (optional)
    /// </summary>
    public string? AgentName { get; private set; }

    /// <summary>
    /// Type of the next agent to execute when resuming (optional)
    /// </summary>
    public string? NextAgentType { get; private set; }

    /// <summary>
    /// Serialized state data as JSON (Dictionary&lt;string, object&gt;)
    /// Contains graph-specific state including current_state, current_agent, retry_count, etc.
    /// </summary>
    public string StateJson { get; private set; } = "{}";

    /// <summary>
    /// Current status of this checkpoint
    /// </summary>
    public CheckpointStatus Status { get; private set; }

    /// <summary>
    /// When the checkpoint was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the checkpoint was last updated (status change, etc.)
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// When the checkpoint was resumed (if applicable)
    /// </summary>
    public DateTime? ResumedAt { get; private set; }

    /// <summary>
    /// Navigation property to the ticket
    /// </summary>
    public Ticket? Ticket { get; private set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; private set; }

    private Checkpoint() { }

    /// <summary>
    /// Creates a new checkpoint
    /// </summary>
    public static Checkpoint Create(
        Guid tenantId,
        Guid ticketId,
        string graphId,
        string checkpointId,
        string stateJson,
        string? agentName = null,
        string? nextAgentType = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));

        if (string.IsNullOrWhiteSpace(graphId))
            throw new ArgumentException("Graph ID cannot be empty", nameof(graphId));

        if (string.IsNullOrWhiteSpace(checkpointId))
            throw new ArgumentException("Checkpoint ID cannot be empty", nameof(checkpointId));

        if (string.IsNullOrWhiteSpace(stateJson))
            throw new ArgumentException("State JSON cannot be empty", nameof(stateJson));

        return new Checkpoint
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TicketId = ticketId,
            GraphId = graphId,
            CheckpointId = checkpointId,
            StateJson = stateJson,
            AgentName = agentName,
            NextAgentType = nextAgentType,
            Status = CheckpointStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks the checkpoint as resumed
    /// </summary>
    public void MarkAsResumed()
    {
        Status = CheckpointStatus.Resumed;
        ResumedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the checkpoint as expired
    /// </summary>
    public void MarkAsExpired()
    {
        Status = CheckpointStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the checkpoint as deleted
    /// </summary>
    public void MarkAsDeleted()
    {
        Status = CheckpointStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the checkpoint state
    /// </summary>
    public void UpdateState(
        string stateJson,
        string? agentName = null,
        string? nextAgentType = null)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
            throw new ArgumentException("State JSON cannot be empty", nameof(stateJson));

        StateJson = stateJson;

        if (agentName != null)
            AgentName = agentName;

        if (nextAgentType != null)
            NextAgentType = nextAgentType;

        UpdatedAt = DateTime.UtcNow;
    }
}
