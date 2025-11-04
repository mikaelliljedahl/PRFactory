using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace PRFactory.Infrastructure.Agents.Base;

/// <summary>
/// Abstract base class for all agents in the PRFactory system.
/// Provides common functionality for checkpointing, error handling, retry logic, and telemetry.
/// </summary>
public abstract class BaseAgent
{
    private readonly ILogger _logger;
    private readonly ActivitySource _activitySource;
    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>
    /// Unique identifier for this agent instance.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Description of what this agent does.
    /// </summary>
    public abstract string Description { get; }

    protected BaseAgent(ILogger logger, string? agentId = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AgentId = agentId ?? Guid.NewGuid().ToString();
        _activitySource = new ActivitySource($"PRFactory.Agent.{Name}");

        // Configure default retry policy using Polly v8
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Agent {AgentName} retry attempt {AttemptNumber} after {Delay}ms",
                        Name,
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Executes the agent with the given context.
    /// Includes automatic checkpointing, error handling, and telemetry.
    /// </summary>
    /// <param name="context">The execution context containing ticket and state information.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>The result of agent execution.</returns>
    public async Task<AgentResult> ExecuteWithMiddlewareAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            $"{Name}.Execute",
            ActivityKind.Internal);

        activity?.SetTag("agent.id", AgentId);
        activity?.SetTag("agent.name", Name);
        activity?.SetTag("ticket.id", context.TicketId);
        activity?.SetTag("tenant.id", context.TenantId);

        _logger.LogInformation(
            "Starting agent {AgentName} (ID: {AgentId}) for ticket {TicketId}",
            Name, AgentId, context.TicketId);

        try
        {
            // Try to restore checkpoint if exists
            var checkpoint = await RestoreCheckpointAsync(context, cancellationToken);
            if (checkpoint != null)
            {
                _logger.LogInformation(
                    "Restored checkpoint {CheckpointId} for agent {AgentName}",
                    checkpoint.CheckpointId, Name);

                context.RestoreFromCheckpoint(checkpoint);
            }

            // Execute with retry policy
            var result = await _retryPipeline.ExecuteAsync(
                async ct => await ExecuteAsync(context, ct),
                cancellationToken);

            // Save checkpoint after successful execution
            await SaveCheckpointAsync(context, cancellationToken);

            _logger.LogInformation(
                "Agent {AgentName} completed successfully for ticket {TicketId}",
                Name, context.TicketId);

            activity?.SetTag("result.status", result.Status.ToString());
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Agent {AgentName} was cancelled for ticket {TicketId}",
                Name, context.TicketId);

            activity?.SetStatus(ActivityStatusCode.Error, "Operation cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Agent {AgentName} failed for ticket {TicketId}",
                Name, context.TicketId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = ex.Message,
                ErrorDetails = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Core execution logic to be implemented by derived agents.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of execution.</returns>
    protected abstract Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the current execution state as a checkpoint.
    /// Can be overridden to customize checkpoint behavior.
    /// </summary>
    protected virtual async Task SaveCheckpointAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var checkpoint = new AgentCheckpoint
        {
            CheckpointId = Guid.NewGuid().ToString(),
            TicketId = context.TicketId,
            AgentName = Name,
            State = context.State,
            Timestamp = DateTime.UtcNow
        };

        await checkpoint.SaveAsync(cancellationToken);

        _logger.LogDebug(
            "Saved checkpoint {CheckpointId} for agent {AgentName}",
            checkpoint.CheckpointId, Name);
    }

    /// <summary>
    /// Restores a previous checkpoint if one exists.
    /// Can be overridden to customize restoration behavior.
    /// </summary>
    protected virtual async Task<AgentCheckpoint?> RestoreCheckpointAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var checkpoint = await AgentCheckpoint.LoadLatestAsync(
            context.TicketId,
            Name,
            cancellationToken);

        return checkpoint;
    }

    /// <summary>
    /// Validates the context before execution.
    /// Can be overridden to add custom validation logic.
    /// </summary>
    protected virtual Task<bool> ValidateContextAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.TicketId))
        {
            throw new ArgumentException("TicketId is required", nameof(context));
        }

        if (string.IsNullOrEmpty(context.TenantId))
        {
            throw new ArgumentException("TenantId is required", nameof(context));
        }

        return Task.FromResult(true);
    }

    protected ILogger Logger => _logger;
}

/// <summary>
/// Represents the result of an agent execution.
/// </summary>
public class AgentResult
{
    /// <summary>
    /// The execution status.
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Completed;

    /// <summary>
    /// Output data from the agent execution.
    /// </summary>
    public Dictionary<string, object> Output { get; set; } = new();

    /// <summary>
    /// Error message if the agent failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Detailed error information for debugging.
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// The next agent to execute in the graph (if any).
    /// </summary>
    public string? NextAgent { get; set; }

    /// <summary>
    /// Indicates if the agent execution should be retried.
    /// </summary>
    public bool ShouldRetry { get; set; }
}

/// <summary>
/// Agent execution status.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent execution completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Agent execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Agent execution is pending (waiting for external input).
    /// </summary>
    Pending,

    /// <summary>
    /// Agent execution was skipped.
    /// </summary>
    Skipped
}
