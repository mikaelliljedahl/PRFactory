using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that provides structured logging for agent execution.
/// Logs entry, exit, duration, and contextual information for each agent.
/// </summary>
public class LoggingMiddleware : IAgentMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        Func<AgentContext, CancellationToken, Task<AgentResult>> next,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var agentName = context.Metadata.CurrentPhase ?? "Unknown";

        // Log entry with structured data
        _logger.LogInformation(
            "Agent execution started. Agent: {AgentName}, TicketId: {TicketId}, TenantId: {TenantId}, ExecutionId: {ExecutionId}",
            agentName,
            context.TicketId,
            context.TenantId,
            context.Metadata.ExecutionId);

        // Log state keys for debugging
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var stateKeys = string.Join(", ", context.State.Keys);
            _logger.LogDebug(
                "Agent {AgentName} state contains keys: [{StateKeys}]",
                agentName,
                stateKeys);
        }

        AgentResult? result = null;
        Exception? exception = null;

        try
        {
            // Execute the next middleware or agent
            result = await next(context, cancellationToken);

            stopwatch.Stop();

            // Log successful completion with metrics
            _logger.LogInformation(
                "Agent execution completed successfully. Agent: {AgentName}, TicketId: {TicketId}, Status: {Status}, Duration: {DurationMs}ms, ExecutionId: {ExecutionId}",
                agentName,
                context.TicketId,
                result.Status,
                stopwatch.ElapsedMilliseconds,
                context.Metadata.ExecutionId);

            // Log output keys if debug is enabled
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var outputKeys = string.Join(", ", result.Output.Keys);
                _logger.LogDebug(
                    "Agent {AgentName} output contains keys: [{OutputKeys}]",
                    agentName,
                    outputKeys);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Agent execution cancelled. Agent: {AgentName}, TicketId: {TicketId}, Duration: {DurationMs}ms, ExecutionId: {ExecutionId}",
                agentName,
                context.TicketId,
                stopwatch.ElapsedMilliseconds,
                context.Metadata.ExecutionId);
            throw;
        }
        catch (Exception ex)
        {
            exception = ex;
            stopwatch.Stop();

            // Log error with full exception details
            _logger.LogError(
                ex,
                "Agent execution failed. Agent: {AgentName}, TicketId: {TicketId}, ErrorType: {ErrorType}, Duration: {DurationMs}ms, ExecutionId: {ExecutionId}",
                agentName,
                context.TicketId,
                ex.GetType().Name,
                stopwatch.ElapsedMilliseconds,
                context.Metadata.ExecutionId);

            throw;
        }
        finally
        {
            // Log performance metrics
            if (stopwatch.ElapsedMilliseconds > 10000) // Log as warning if > 10 seconds
            {
                _logger.LogWarning(
                    "Agent execution took longer than expected. Agent: {AgentName}, TicketId: {TicketId}, Duration: {DurationMs}ms",
                    agentName,
                    context.TicketId,
                    stopwatch.ElapsedMilliseconds);
            }

            // Log structured metrics for APM/monitoring systems
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Agent metrics: {{\"agent\": \"{AgentName}\", \"ticketId\": \"{TicketId}\", \"duration\": {Duration}, \"success\": {Success}, \"status\": \"{Status}\"}}",
                    agentName,
                    context.TicketId,
                    stopwatch.ElapsedMilliseconds,
                    exception == null,
                    result?.Status.ToString() ?? "Unknown");
            }
        }
    }
}

/// <summary>
/// Interface for agent middleware components.
/// Middleware can wrap agent execution to provide cross-cutting concerns like logging, telemetry, etc.
/// </summary>
public interface IAgentMiddleware
{
    /// <summary>
    /// Executes the middleware logic, calling the next middleware or agent in the pipeline.
    /// </summary>
    /// <param name="context">The agent execution context.</param>
    /// <param name="next">The next middleware or agent to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of agent execution.</returns>
    Task<AgentResult> ExecuteAsync(
        AgentContext context,
        Func<AgentContext, CancellationToken, Task<AgentResult>> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Builder for constructing middleware pipelines.
/// </summary>
public class AgentPipelineBuilder
{
    private readonly List<IAgentMiddleware> _middlewares = new();

    /// <summary>
    /// Adds a middleware to the pipeline.
    /// </summary>
    public AgentPipelineBuilder Use(IAgentMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Builds the middleware pipeline.
    /// </summary>
    public Func<AgentContext, CancellationToken, Task<AgentResult>> Build(
        Func<AgentContext, CancellationToken, Task<AgentResult>> finalHandler)
    {
        Func<AgentContext, CancellationToken, Task<AgentResult>> pipeline = finalHandler;

        // Build the pipeline in reverse order
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = pipeline;
            pipeline = (context, ct) => middleware.ExecuteAsync(context, next, ct);
        }

        return pipeline;
    }
}
