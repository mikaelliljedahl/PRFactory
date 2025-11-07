using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Represents a request to execute an agent graph.
/// </summary>
public class AgentExecutionRequest
{
    public Guid ExecutionId { get; set; }
    public Guid TicketId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public IAgentMessage InitialMessage { get; set; } = default!;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public string? LastErrorDetails { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Represents a suspended workflow waiting for external input.
/// </summary>
public class SuspendedWorkflow
{
    public Guid TicketId { get; set; }
    public string SuspendedAgentName { get; set; } = string.Empty;
    public Guid CheckpointId { get; set; }
    public IAgentMessage? ResumeMessage { get; set; }
    public DateTime SuspendedAt { get; set; }
    public int ResumeAttempts { get; set; }
    public string? LastResumeError { get; set; }
    public DateTime? LastResumeAttemptAt { get; set; }
}

/// <summary>
/// Result of a workflow execution or resume operation.
/// </summary>
public class WorkflowExecutionResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Interface for managing agent execution queue.
/// </summary>
public interface IAgentExecutionQueue
{
    Task<List<AgentExecutionRequest>> GetPendingExecutionsAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task<List<SuspendedWorkflow>> GetSuspendedWorkflowsWithEventsAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkExecutionCompletedAsync(
        Guid executionId,
        WorkflowExecutionResult result,
        CancellationToken cancellationToken);

    Task MarkExecutionFailedAsync(
        Guid executionId,
        string error,
        CancellationToken cancellationToken);

    Task ScheduleRetryAsync(
        AgentExecutionRequest execution,
        CancellationToken cancellationToken);

    Task MarkWorkflowResumedAsync(
        Guid ticketId,
        WorkflowExecutionResult result,
        CancellationToken cancellationToken);

    Task MarkWorkflowResumeFailedAsync(
        Guid ticketId,
        string error,
        CancellationToken cancellationToken);

    Task ScheduleResumeRetryAsync(
        SuspendedWorkflow workflow,
        CancellationToken cancellationToken);
}

/// <summary>
/// Interface for executing agent graphs.
/// </summary>
public interface IAgentGraphExecutor
{
    Task<WorkflowExecutionResult> ExecuteGraphAsync(
        string workflowType,
        IAgentMessage initialMessage,
        CancellationToken cancellationToken);
}

/// <summary>
/// Interface for workflow resume handler.
/// </summary>
public interface IWorkflowResumeHandler
{
    Task<WorkflowExecutionResult> ResumeWorkflowAsync(
        SuspendedWorkflow workflow,
        CancellationToken cancellationToken);

    Task<(bool IsValid, IAgentMessage? ResumeMessage)> ValidateAndCreateResumeMessageAsync(
        Guid ticketId,
        string eventType,
        object eventData,
        CancellationToken cancellationToken);
}

/// <summary>
/// Interface for checkpoint storage.
/// </summary>
public interface ICheckpointStore
{
    Task<Base.CheckpointData?> LoadCheckpointAsync(
        Guid checkpointId,
        CancellationToken cancellationToken);

    Task<Base.CheckpointData?> LoadLatestCheckpointAsync(
        Guid ticketId,
        CancellationToken cancellationToken);

    Task SaveCheckpointAsync(
        Base.CheckpointData checkpoint,
        CancellationToken cancellationToken);
}
