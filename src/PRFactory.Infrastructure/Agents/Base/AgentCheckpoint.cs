using System.Text.Json;

namespace PRFactory.Infrastructure.Agents.Base;

/// <summary>
/// Represents a checkpoint in agent execution for recovery and resumption.
/// Checkpoints enable human-in-the-loop workflows where execution pauses for approval or input.
/// </summary>
public class AgentCheckpoint
{
    /// <summary>
    /// Unique identifier for this checkpoint.
    /// </summary>
    public string CheckpointId { get; set; } = string.Empty;

    /// <summary>
    /// The ticket ID this checkpoint belongs to.
    /// </summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the agent that created this checkpoint.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// The execution state at the time of checkpoint.
    /// Serialized as JSON for persistence.
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// When this checkpoint was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional reason for the checkpoint (e.g., "waiting_for_approval", "manual_intervention").
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Metadata about the checkpoint for debugging and tracking.
    /// </summary>
    public CheckpointMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Saves the checkpoint to persistent storage.
    /// This is a placeholder - actual implementation would use a repository/database.
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual persistence logic
        // This would typically:
        // 1. Serialize the State dictionary to JSON
        // 2. Store in database (e.g., CosmosDB, SQL Server, etc.)
        // 3. Include proper error handling and retry logic

        await Task.CompletedTask;
    }

    /// <summary>
    /// Loads the latest checkpoint for a specific ticket and agent.
    /// </summary>
    public static async Task<AgentCheckpoint?> LoadLatestAsync(
        string ticketId,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual retrieval logic
        // This would typically:
        // 1. Query database for latest checkpoint matching ticketId and agentName
        // 2. Deserialize JSON state back to dictionary
        // 3. Return null if no checkpoint found

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Loads all checkpoints for a specific ticket.
    /// </summary>
    public static async Task<List<AgentCheckpoint>> LoadAllAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual retrieval logic
        // This would return all checkpoints for a ticket, ordered by timestamp

        await Task.CompletedTask;
        return new List<AgentCheckpoint>();
    }

    /// <summary>
    /// Deletes a checkpoint from storage.
    /// </summary>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual deletion logic
        // This would remove the checkpoint from the database

        await Task.CompletedTask;
    }

    /// <summary>
    /// Serializes the checkpoint to JSON string.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Deserializes a checkpoint from JSON string.
    /// </summary>
    public static AgentCheckpoint? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<AgentCheckpoint>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Creates a new checkpoint with the given data.
    /// </summary>
    public static AgentCheckpoint Create(
        string ticketId,
        string agentName,
        Dictionary<string, object> state,
        string? reason = null)
    {
        return new AgentCheckpoint
        {
            CheckpointId = Guid.NewGuid().ToString(),
            TicketId = ticketId,
            AgentName = agentName,
            State = state,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Metadata associated with a checkpoint for tracking and debugging.
/// </summary>
public class CheckpointMetadata
{
    /// <summary>
    /// The execution ID when the checkpoint was created.
    /// </summary>
    public string? ExecutionId { get; set; }

    /// <summary>
    /// The current phase of execution (e.g., "Analysis", "Planning", "Implementation").
    /// </summary>
    public string? Phase { get; set; }

    /// <summary>
    /// Number of retries that occurred before this checkpoint.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Total execution time before checkpoint (in milliseconds).
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Additional custom tags for filtering and searching.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Interface for checkpoint persistence operations.
/// Implement this to integrate with your specific database/storage solution.
/// </summary>
public interface ICheckpointRepository
{
    /// <summary>
    /// Saves a checkpoint to storage.
    /// </summary>
    Task SaveCheckpointAsync(AgentCheckpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the latest checkpoint for a ticket and agent.
    /// </summary>
    Task<AgentCheckpoint?> GetLatestCheckpointAsync(
        string ticketId,
        string agentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all checkpoints for a specific ticket.
    /// </summary>
    Task<List<AgentCheckpoint>> GetCheckpointsAsync(
        string ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific checkpoint by ID.
    /// </summary>
    Task<AgentCheckpoint?> GetCheckpointByIdAsync(
        string checkpointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a checkpoint from storage.
    /// </summary>
    Task DeleteCheckpointAsync(
        string checkpointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all checkpoints for a specific ticket.
    /// </summary>
    Task DeleteTicketCheckpointsAsync(
        string ticketId,
        CancellationToken cancellationToken = default);
}
