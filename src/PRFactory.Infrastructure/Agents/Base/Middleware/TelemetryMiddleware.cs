using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that provides OpenTelemetry tracing and metrics for agent execution.
/// Creates spans for each agent execution with relevant tags and metrics.
/// </summary>
public class TelemetryMiddleware : IAgentMiddleware
{
    private readonly ILogger<TelemetryMiddleware> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _executionCounter;
    private readonly Histogram<double> _executionDuration;
    private readonly Counter<long> _errorCounter;

    public TelemetryMiddleware(ILogger<TelemetryMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = new ActivitySource("PRFactory.Agents");
        _meter = new Meter("PRFactory.Agents");

        // Initialize metrics
        _executionCounter = _meter.CreateCounter<long>(
            "agent.executions",
            unit: "executions",
            description: "Total number of agent executions");

        _executionDuration = _meter.CreateHistogram<double>(
            "agent.execution.duration",
            unit: "ms",
            description: "Duration of agent executions");

        _errorCounter = _meter.CreateCounter<long>(
            "agent.errors",
            unit: "errors",
            description: "Total number of agent execution errors");
    }

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        Func<AgentContext, CancellationToken, Task<AgentResult>> next,
        CancellationToken cancellationToken = default)
    {
        var agentName = context.Metadata.CurrentPhase ?? "Unknown";
        var stopwatch = Stopwatch.StartNew();

        // Create OpenTelemetry activity (span)
        using var activity = _activitySource.StartActivity(
            $"Agent.{agentName}",
            ActivityKind.Internal);

        // Add standard tags to the span
        activity?.SetTag("agent.name", agentName);
        activity?.SetTag("ticket.id", context.TicketId);
        activity?.SetTag("tenant.id", context.TenantId);
        activity?.SetTag("repository.id", context.RepositoryId);
        activity?.SetTag("execution.id", context.Metadata.ExecutionId);
        activity?.SetTag("phase", context.Metadata.CurrentPhase);

        // Add custom tags if available
        foreach (var tag in context.Metadata.Tags)
        {
            activity?.SetTag($"custom.{tag.Key}", tag.Value);
        }

        AgentResult? result = null;
        var success = false;
        var errorType = string.Empty;

        try
        {
            // Execute the next middleware or agent
            result = await next(context, cancellationToken);
            success = result.Status == AgentStatus.Completed;

            // Add result information to span
            activity?.SetTag("result.status", result.Status.ToString());
            activity?.SetTag("result.has_error", !string.IsNullOrEmpty(result.Error));
            activity?.SetTag("result.next_agent", result.NextAgent ?? "none");

            if (result.Status == AgentStatus.Failed)
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.Error ?? "Unknown error");
                errorType = "execution_failure";
            }
            else if (result.Status == AgentStatus.Pending)
            {
                activity?.SetTag("waiting_for", "human_input");
            }

            return result;
        }
        catch (OperationCanceledException ex)
        {
            errorType = "cancellation";
            activity?.SetStatus(ActivityStatusCode.Error, "Operation cancelled");
            activity?.RecordException(ex);
            throw;
        }
        catch (Exception ex)
        {
            errorType = ex.GetType().Name;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            // Add duration to span
            activity?.SetTag("duration.ms", durationMs);

            // Record metrics with appropriate tags
            var tags = new TagList
            {
                { "agent.name", agentName },
                { "tenant.id", context.TenantId },
                { "status", result?.Status.ToString() ?? "unknown" },
                { "success", success }
            };

            _executionCounter.Add(1, tags);
            _executionDuration.Record(durationMs, tags);

            // Record error metrics if applicable
            if (!success || !string.IsNullOrEmpty(errorType))
            {
                var errorTags = new TagList
                {
                    { "agent.name", agentName },
                    { "tenant.id", context.TenantId },
                    { "error.type", errorType }
                };
                _errorCounter.Add(1, errorTags);
            }

            // Log telemetry for debugging
            _logger.LogDebug(
                "Telemetry recorded for agent {AgentName}: Duration={Duration}ms, Success={Success}, Status={Status}",
                agentName,
                durationMs,
                success,
                result?.Status.ToString() ?? "unknown");
        }
    }
}

/// <summary>
/// Extension methods for adding telemetry events to Activities.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    public static void RecordException(this Activity? activity, Exception exception)
    {
        if (activity == null || exception == null)
            return;

        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        };

        if (exception.InnerException != null)
        {
            tags.Add("exception.inner_type", exception.InnerException.GetType().FullName);
            tags.Add("exception.inner_message", exception.InnerException.Message);
        }

        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }

    /// <summary>
    /// Adds a custom event to the activity.
    /// </summary>
    public static void AddCustomEvent(this Activity? activity, string eventName, Dictionary<string, object>? attributes = null)
    {
        if (activity == null)
            return;

        var tags = new ActivityTagsCollection();
        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                tags.Add(attr.Key, attr.Value);
            }
        }

        activity.AddEvent(new ActivityEvent(eventName, tags: tags));
    }
}

/// <summary>
/// Telemetry configuration options.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Enable or disable telemetry collection.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Sample rate for traces (0.0 to 1.0). 1.0 means sample all traces.
    /// </summary>
    public double TraceSampleRate { get; set; } = 1.0;

    /// <summary>
    /// Enable detailed metrics collection.
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;

    /// <summary>
    /// Custom tags to add to all telemetry.
    /// </summary>
    public Dictionary<string, string> GlobalTags { get; set; } = new();
}
