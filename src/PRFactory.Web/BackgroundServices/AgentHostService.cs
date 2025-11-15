using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Web.BackgroundServices;

/// <summary>
/// Background service that hosts agent graph execution.
/// Polls for new agent executions and resumes suspended workflows when webhooks arrive.
/// </summary>
public class AgentHostService : BackgroundService
{
    private readonly ILogger<AgentHostService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentHostOptions _options;
    private readonly ActivitySource _activitySource;
    private readonly SemaphoreSlim _executionSemaphore;

    public AgentHostService(
        ILogger<AgentHostService> logger,
        IServiceProvider serviceProvider,
        IOptions<AgentHostOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _activitySource = new ActivitySource("PRFactory.Worker.AgentHost");
        _executionSemaphore = new SemaphoreSlim(_options.MaxConcurrentExecutions);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Agent Host Service starting. Max concurrent executions: {MaxConcurrent}, Poll interval: {PollInterval}s",
            _options.MaxConcurrentExecutions,
            _options.PollIntervalSeconds);

        // Wait a bit for the host to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndExecuteWorkflowsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Agent Host Service is stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Agent Host Service polling loop");
            }

            // Wait before next poll
            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("Agent Host Service stopped");
    }

    /// <summary>
    /// Polls for pending workflow executions and processes them.
    /// </summary>
    private async Task PollAndExecuteWorkflowsAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("PollAndExecuteWorkflows");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var executionQueue = scope.ServiceProvider
                .GetRequiredService<IAgentExecutionQueue>();

            // Get pending executions (newly triggered workflows)
            var pendingExecutions = await executionQueue
                .GetPendingExecutionsAsync(_options.BatchSize, cancellationToken);

            if (pendingExecutions.Any())
            {
                _logger.LogInformation(
                    "Found {Count} pending workflow execution(s) to process",
                    pendingExecutions.Count);

                // Process each execution
                var tasks = pendingExecutions.Select(execution =>
                    ProcessWorkflowExecutionAsync(execution, cancellationToken));

                await Task.WhenAll(tasks);
            }

            // Check for suspended workflows that need resumption
            var suspendedWorkflows = await executionQueue
                .GetSuspendedWorkflowsWithEventsAsync(_options.BatchSize, cancellationToken);

            if (suspendedWorkflows.Any())
            {
                _logger.LogInformation(
                    "Found {Count} suspended workflow(s) ready to resume",
                    suspendedWorkflows.Count);

                // Resume each workflow
                var resumeTasks = suspendedWorkflows.Select(workflow =>
                    ResumeWorkflowAsync(workflow, cancellationToken));

                await Task.WhenAll(resumeTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling for workflow executions");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Processes a single workflow execution.
    /// </summary>
    private async Task ProcessWorkflowExecutionAsync(
        AgentExecutionRequest execution,
        CancellationToken cancellationToken)
    {
        // Acquire semaphore to limit concurrent executions
        await _executionSemaphore.WaitAsync(cancellationToken);

        try
        {
            using var activity = _activitySource.StartActivity("ProcessWorkflow");
            activity?.SetTag("ticket.id", execution.TicketId);
            activity?.SetTag("workflow.type", execution.WorkflowType);

            _logger.LogInformation(
                "Starting workflow execution for ticket {TicketId}, type: {WorkflowType}",
                execution.TicketId,
                execution.WorkflowType);

            using var scope = _serviceProvider.CreateScope();
            var graphExecutor = scope.ServiceProvider
                .GetRequiredService<IAgentGraphExecutor>();

            try
            {
                // Execute the agent graph
                var result = await graphExecutor.ExecuteGraphAsync(
                    execution.WorkflowType,
                    execution.InitialMessage,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Workflow execution completed successfully for ticket {TicketId}",
                        execution.TicketId);

                    activity?.SetTag("result.status", "success");
                }
                else
                {
                    _logger.LogWarning(
                        "Workflow execution completed with warnings for ticket {TicketId}: {Message}",
                        execution.TicketId,
                        result.Message);

                    activity?.SetTag("result.status", "warning");
                }

                // Mark execution as completed
                var executionQueue = scope.ServiceProvider
                    .GetRequiredService<IAgentExecutionQueue>();
                await executionQueue.MarkExecutionCompletedAsync(
                    execution.ExecutionId,
                    result,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Workflow execution failed for ticket {TicketId}",
                    execution.TicketId);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                // Handle failure based on retry policy
                await HandleExecutionFailureAsync(execution, ex, scope, cancellationToken);
            }
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    /// <summary>
    /// Resumes a suspended workflow when a webhook event arrives.
    /// </summary>
    private async Task ResumeWorkflowAsync(
        SuspendedWorkflow workflow,
        CancellationToken cancellationToken)
    {
        await _executionSemaphore.WaitAsync(cancellationToken);

        try
        {
            using var activity = _activitySource.StartActivity("ResumeWorkflow");
            activity?.SetTag("ticket.id", workflow.TicketId);
            activity?.SetTag("suspended.agent", workflow.SuspendedAgentName);

            _logger.LogInformation(
                "Resuming workflow for ticket {TicketId} from agent {AgentName}",
                workflow.TicketId,
                workflow.SuspendedAgentName);

            using var scope = _serviceProvider.CreateScope();
            var resumeHandler = scope.ServiceProvider
                .GetRequiredService<IWorkflowResumeHandler>();

            try
            {
                // Resume workflow execution
                var result = await resumeHandler.ResumeWorkflowAsync(
                    workflow,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Workflow resumed successfully for ticket {TicketId}",
                        workflow.TicketId);

                    activity?.SetTag("result.status", "success");
                }
                else
                {
                    _logger.LogWarning(
                        "Workflow resume completed with warnings for ticket {TicketId}: {Message}",
                        workflow.TicketId,
                        result.Message);

                    activity?.SetTag("result.status", "warning");
                }

                // Mark as resumed
                var executionQueue = scope.ServiceProvider
                    .GetRequiredService<IAgentExecutionQueue>();
                await executionQueue.MarkWorkflowResumedAsync(
                    workflow.TicketId,
                    result,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resume workflow for ticket {TicketId}",
                    workflow.TicketId);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                // Handle resume failure
                await HandleResumeFailureAsync(workflow, ex, scope, cancellationToken);
            }
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    /// <summary>
    /// Handles execution failures with retry logic.
    /// </summary>
    private async Task HandleExecutionFailureAsync(
        AgentExecutionRequest execution,
        Exception exception,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var executionQueue = scope.ServiceProvider
            .GetRequiredService<IAgentExecutionQueue>();

        execution.RetryCount++;
        execution.LastError = exception.Message;
        execution.LastErrorDetails = exception.ToString();
        execution.LastAttemptAt = DateTime.UtcNow;

        if (execution.RetryCount < _options.MaxRetries)
        {
            // Calculate exponential backoff delay
            var delaySeconds = Math.Pow(2, execution.RetryCount) * _options.RetryDelayBaseSeconds;
            execution.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);

            _logger.LogWarning(
                "Scheduling retry {RetryCount}/{MaxRetries} for ticket {TicketId} in {DelaySeconds}s",
                execution.RetryCount,
                _options.MaxRetries,
                execution.TicketId,
                delaySeconds);

            await executionQueue.ScheduleRetryAsync(execution, cancellationToken);
        }
        else
        {
            _logger.LogError(
                "Workflow execution permanently failed for ticket {TicketId} after {RetryCount} retries",
                execution.TicketId,
                execution.RetryCount);

            await executionQueue.MarkExecutionFailedAsync(
                execution.ExecutionId,
                exception.Message,
                cancellationToken);
        }
    }

    /// <summary>
    /// Handles workflow resume failures.
    /// </summary>
    private async Task HandleResumeFailureAsync(
        SuspendedWorkflow workflow,
        Exception exception,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var executionQueue = scope.ServiceProvider
            .GetRequiredService<IAgentExecutionQueue>();

        workflow.ResumeAttempts++;
        workflow.LastResumeError = exception.Message;
        workflow.LastResumeAttemptAt = DateTime.UtcNow;

        if (workflow.ResumeAttempts < _options.MaxResumeRetries)
        {
            var delaySeconds = Math.Pow(2, workflow.ResumeAttempts) * _options.RetryDelayBaseSeconds;

            _logger.LogWarning(
                "Scheduling resume retry {RetryCount}/{MaxRetries} for ticket {TicketId} in {DelaySeconds}s",
                workflow.ResumeAttempts,
                _options.MaxResumeRetries,
                workflow.TicketId,
                delaySeconds);

            await executionQueue.ScheduleResumeRetryAsync(workflow, cancellationToken);
        }
        else
        {
            _logger.LogError(
                "Workflow resume permanently failed for ticket {TicketId} after {RetryCount} attempts",
                workflow.TicketId,
                workflow.ResumeAttempts);

            await executionQueue.MarkWorkflowResumeFailedAsync(
                workflow.TicketId,
                exception.Message,
                cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent Host Service is stopping...");

        // Wait for all current executions to complete
        var timeout = TimeSpan.FromSeconds(_options.GracefulShutdownTimeoutSeconds);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            // Wait for semaphore to have all slots available (all executions done)
            for (int i = 0; i < _options.MaxConcurrentExecutions; i++)
            {
                await _executionSemaphore.WaitAsync(cts.Token);
            }

            _logger.LogInformation("All agent executions completed gracefully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Graceful shutdown timeout exceeded after {Timeout}s, forcing shutdown",
                timeout.TotalSeconds);
        }

        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Configuration options for the Agent Host Service.
/// </summary>
public class AgentHostOptions
{
    /// <summary>
    /// Maximum number of concurrent workflow executions.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 10;

    /// <summary>
    /// Interval between polling for new work (in seconds).
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Number of executions to fetch per poll batch.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Maximum number of retry attempts for failed executions.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Maximum number of retry attempts for resuming suspended workflows.
    /// </summary>
    public int MaxResumeRetries { get; set; } = 5;

    /// <summary>
    /// Base delay for exponential backoff retry (in seconds).
    /// </summary>
    public int RetryDelayBaseSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout for graceful shutdown (in seconds).
    /// </summary>
    public int GracefulShutdownTimeoutSeconds { get; set; } = 300;
}
