using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Agents.Stubs;

/// <summary>
/// Stub implementation of IAgentExecutionQueue for build purposes.
/// This should be replaced with a real implementation that uses a database or message queue.
/// </summary>
public class AgentExecutionQueue : IAgentExecutionQueue
{
    private readonly ILogger<AgentExecutionQueue> _logger;

    public AgentExecutionQueue(ILogger<AgentExecutionQueue> logger)
    {
        _logger = logger;
    }

    public Task<List<AgentExecutionRequest>> GetPendingExecutionsAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Using stub implementation of IAgentExecutionQueue");
        return Task.FromResult(new List<AgentExecutionRequest>());
    }

    public Task<List<SuspendedWorkflow>> GetSuspendedWorkflowsWithEventsAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<SuspendedWorkflow>());
    }

    public Task MarkExecutionCompletedAsync(
        Guid executionId,
        WorkflowExecutionResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Execution {ExecutionId} completed: {Result}", executionId, result.IsSuccess);
        return Task.CompletedTask;
    }

    public Task MarkExecutionFailedAsync(
        Guid executionId,
        string error,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Execution {ExecutionId} failed: {Error}", executionId, error);
        return Task.CompletedTask;
    }

    public Task ScheduleRetryAsync(
        AgentExecutionRequest execution,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling retry for execution {ExecutionId}", execution.ExecutionId);
        return Task.CompletedTask;
    }

    public Task MarkWorkflowResumedAsync(
        Guid ticketId,
        WorkflowExecutionResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Workflow {TicketId} resumed: {Result}", ticketId, result.IsSuccess);
        return Task.CompletedTask;
    }

    public Task MarkWorkflowResumeFailedAsync(
        Guid ticketId,
        string error,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Workflow resume {TicketId} failed: {Error}", ticketId, error);
        return Task.CompletedTask;
    }

    public Task ScheduleResumeRetryAsync(
        SuspendedWorkflow workflow,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling resume retry for workflow {TicketId}", workflow.TicketId);
        return Task.CompletedTask;
    }
}
