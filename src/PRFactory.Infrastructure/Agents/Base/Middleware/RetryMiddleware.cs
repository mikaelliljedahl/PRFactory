using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that provides retry logic for agent execution using Polly.
/// Automatically retries transient failures with exponential backoff.
/// </summary>
public class RetryMiddleware : IAgentMiddleware
{
    private readonly ILogger<RetryMiddleware> _logger;
    private readonly RetryOptions _options;
    private readonly ResiliencePipeline<AgentResult> _retryPipeline;

    public RetryMiddleware(
        ILogger<RetryMiddleware> logger,
        RetryOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new RetryOptions();

        // Build Polly v8 retry pipeline
        _retryPipeline = BuildRetryPipeline();
    }

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        Func<AgentContext, CancellationToken, Task<AgentResult>> next,
        CancellationToken cancellationToken = default)
    {
        var agentName = context.Metadata.ContainsKey("CurrentPhase")
            ? context.Metadata["CurrentPhase"]?.ToString() ?? "Unknown"
            : "Unknown";

        // Execute with retry policy
        var result = await _retryPipeline.ExecuteAsync(
            async ct => await next(context, ct),
            cancellationToken);

        return result;
    }

    private ResiliencePipeline<AgentResult> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<AgentResult>()
            .AddRetry(new RetryStrategyOptions<AgentResult>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = _options.InitialDelay,
                BackoffType = _options.BackoffType,
                UseJitter = _options.UseJitter,
                MaxDelay = _options.MaxDelay,

                // Determine if we should retry based on the result
                ShouldHandle = new PredicateBuilder<AgentResult>()
                    .HandleResult(result => result.ShouldRetry)
                    .Handle<TransientException>()
                    .Handle<HttpRequestException>(ex => IsTransientHttpError(ex))
                    .Handle<TimeoutException>(),

                OnRetry = args =>
                {
                    var attemptNumber = args.AttemptNumber;
                    var delay = args.RetryDelay;
                    var outcome = args.Outcome;

                    if (outcome.Exception != null)
                    {
                        _logger.LogWarning(
                            outcome.Exception,
                            "Retry attempt {AttemptNumber} after {DelayMs}ms due to exception: {ErrorType}",
                            attemptNumber,
                            delay.TotalMilliseconds,
                            outcome.Exception.GetType().Name);
                    }
                    else if (outcome.Result != null)
                    {
                        _logger.LogWarning(
                            "Retry attempt {AttemptNumber} after {DelayMs}ms due to failed result. Status: {Status}, Error: {Error}",
                            attemptNumber,
                            delay.TotalMilliseconds,
                            outcome.Result.Status,
                            outcome.Result.Error);
                    }

                    // Custom retry callback if configured
                    _options.OnRetry?.Invoke(attemptNumber, delay);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private bool IsTransientHttpError(HttpRequestException ex)
    {
        // Reuse the same logic from ErrorHandlingMiddleware
        var transientStatusCodes = new[]
        {
            System.Net.HttpStatusCode.RequestTimeout,
            System.Net.HttpStatusCode.TooManyRequests,
            System.Net.HttpStatusCode.InternalServerError,
            System.Net.HttpStatusCode.BadGateway,
            System.Net.HttpStatusCode.ServiceUnavailable,
            System.Net.HttpStatusCode.GatewayTimeout
        };

        if (ex.StatusCode.HasValue)
        {
            return transientStatusCodes.Contains(ex.StatusCode.Value);
        }

        return ex.InnerException is System.Net.Sockets.SocketException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Configuration options for retry middleware.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay before first retry.
    /// Default: 1 second
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff strategy for retry delays.
    /// Default: Exponential
    /// </summary>
    public DelayBackoffType BackoffType { get; set; } = DelayBackoffType.Exponential;

    /// <summary>
    /// Whether to add jitter to retry delays to avoid thundering herd.
    /// Default: true
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Custom callback invoked on each retry attempt.
    /// Parameters: (attemptNumber, delay)
    /// </summary>
    public Action<int, TimeSpan>? OnRetry { get; set; }

    /// <summary>
    /// Custom predicate to determine if a specific exception should be retried.
    /// </summary>
    public Func<Exception, bool>? ShouldRetryException { get; set; }

    /// <summary>
    /// Custom predicate to determine if a specific result should be retried.
    /// </summary>
    public Func<AgentResult, bool>? ShouldRetryResult { get; set; }
}

/// <summary>
/// Fluent builder for configuring retry options.
/// </summary>
public class RetryOptionsBuilder
{
    private readonly RetryOptions _options = new();

    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    public RetryOptionsBuilder WithMaxRetries(int maxRetries)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");

        _options.MaxRetryAttempts = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the initial delay before first retry.
    /// </summary>
    public RetryOptionsBuilder WithInitialDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be non-negative");

        _options.InitialDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retries.
    /// </summary>
    public RetryOptionsBuilder WithMaxDelay(TimeSpan maxDelay)
    {
        if (maxDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay must be non-negative");

        _options.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Uses exponential backoff strategy.
    /// </summary>
    public RetryOptionsBuilder WithExponentialBackoff()
    {
        _options.BackoffType = DelayBackoffType.Exponential;
        return this;
    }

    /// <summary>
    /// Uses linear backoff strategy.
    /// </summary>
    public RetryOptionsBuilder WithLinearBackoff()
    {
        _options.BackoffType = DelayBackoffType.Linear;
        return this;
    }

    /// <summary>
    /// Uses constant delay between retries.
    /// </summary>
    public RetryOptionsBuilder WithConstantDelay()
    {
        _options.BackoffType = DelayBackoffType.Constant;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter in retry delays.
    /// </summary>
    public RetryOptionsBuilder WithJitter(bool useJitter = true)
    {
        _options.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Adds a callback to be invoked on each retry.
    /// </summary>
    public RetryOptionsBuilder OnRetry(Action<int, TimeSpan> callback)
    {
        _options.OnRetry = callback;
        return this;
    }

    /// <summary>
    /// Builds the configured RetryOptions.
    /// </summary>
    public RetryOptions Build()
    {
        return _options;
    }
}

/// <summary>
/// Extension methods for creating common retry configurations.
/// </summary>
public static class RetryOptionsExtensions
{
    /// <summary>
    /// Creates retry options optimized for API calls.
    /// 5 retries with exponential backoff, jitter enabled.
    /// </summary>
    public static RetryOptions ForApiCalls()
    {
        return new RetryOptionsBuilder()
            .WithMaxRetries(5)
            .WithInitialDelay(TimeSpan.FromSeconds(1))
            .WithMaxDelay(TimeSpan.FromSeconds(60))
            .WithExponentialBackoff()
            .WithJitter()
            .Build();
    }

    /// <summary>
    /// Creates retry options for database operations.
    /// 3 retries with shorter delays.
    /// </summary>
    public static RetryOptions ForDatabaseOperations()
    {
        return new RetryOptionsBuilder()
            .WithMaxRetries(3)
            .WithInitialDelay(TimeSpan.FromMilliseconds(500))
            .WithMaxDelay(TimeSpan.FromSeconds(5))
            .WithExponentialBackoff()
            .WithJitter()
            .Build();
    }

    /// <summary>
    /// Creates retry options for file I/O operations.
    /// 3 retries with minimal delays.
    /// </summary>
    public static RetryOptions ForFileOperations()
    {
        return new RetryOptionsBuilder()
            .WithMaxRetries(3)
            .WithInitialDelay(TimeSpan.FromMilliseconds(100))
            .WithMaxDelay(TimeSpan.FromSeconds(2))
            .WithLinearBackoff()
            .Build();
    }
}
