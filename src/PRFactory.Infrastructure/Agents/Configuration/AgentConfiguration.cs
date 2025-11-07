using PRFactory.Infrastructure.Agents.Base.Middleware;

namespace PRFactory.Infrastructure.Agents.Configuration;

/// <summary>
/// Configuration options for the agent framework.
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AgentFramework";

    /// <summary>
    /// Enable or disable the agent framework.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default timeout for agent execution in seconds.
    /// Default: 300 (5 minutes)
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum concurrent agent executions.
    /// Default: 10
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 10;

    /// <summary>
    /// Enable checkpoint persistence for resumable workflows.
    /// Default: true
    /// </summary>
    public bool EnableCheckpoints { get; set; } = true;

    /// <summary>
    /// Checkpoint retention period in days.
    /// Checkpoints older than this will be cleaned up.
    /// Default: 30 days
    /// </summary>
    public int CheckpointRetentionDays { get; set; } = 30;

    /// <summary>
    /// Middleware configuration.
    /// </summary>
    public MiddlewareConfiguration Middleware { get; set; } = new();

    /// <summary>
    /// Retry configuration.
    /// </summary>
    public RetryConfiguration Retry { get; set; } = new();

    /// <summary>
    /// Error handling configuration.
    /// </summary>
    public ErrorHandlingConfiguration ErrorHandling { get; set; } = new();
}

/// <summary>
/// Configuration for agent middleware.
/// </summary>
public class MiddlewareConfiguration
{
    /// <summary>
    /// Enable logging middleware.
    /// Default: true
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Enable telemetry middleware.
    /// Default: true
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Enable error handling middleware.
    /// Default: true
    /// </summary>
    public bool EnableErrorHandling { get; set; } = true;

    /// <summary>
    /// Enable retry middleware.
    /// Default: true
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Custom middleware order (if you want to override default).
    /// Middleware will be executed in the order specified.
    /// </summary>
    public List<string>? CustomOrder { get; set; }
}

/// <summary>
/// Configuration for retry behavior.
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay before first retry in milliseconds.
    /// Default: 1000 (1 second)
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay between retries in milliseconds.
    /// Default: 30000 (30 seconds)
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Backoff strategy: "Exponential", "Linear", or "Constant".
    /// Default: "Exponential"
    /// </summary>
    public string BackoffType { get; set; } = "Exponential";

    /// <summary>
    /// Enable jitter in retry delays.
    /// Default: true
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Converts to RetryOptions for middleware.
    /// </summary>
    public RetryOptions ToRetryOptions()
    {
        return new RetryOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            InitialDelay = TimeSpan.FromMilliseconds(InitialDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(MaxDelayMs),
            BackoffType = BackoffType.ToLowerInvariant() switch
            {
                "linear" => Polly.DelayBackoffType.Linear,
                "constant" => Polly.DelayBackoffType.Constant,
                _ => Polly.DelayBackoffType.Exponential
            },
            UseJitter = UseJitter
        };
    }
}

/// <summary>
/// Configuration for error handling.
/// </summary>
public class ErrorHandlingConfiguration
{
    /// <summary>
    /// Include stack traces in error responses.
    /// Default: false (should be false in production).
    /// </summary>
    public bool IncludeStackTrace { get; set; } = false;

    /// <summary>
    /// Sanitize error messages to avoid leaking sensitive information.
    /// Default: true (should be true in production).
    /// </summary>
    public bool SanitizeErrorMessages { get; set; } = true;

    /// <summary>
    /// Rethrow cancellation exceptions.
    /// Default: true
    /// </summary>
    public bool RethrowCancellation { get; set; } = true;

    /// <summary>
    /// Rethrow unhandled exceptions.
    /// Default: false
    /// </summary>
    public bool RethrowUnhandled { get; set; } = false;

    /// <summary>
    /// Converts to ErrorHandlingOptions for middleware.
    /// </summary>
    public ErrorHandlingOptions ToErrorHandlingOptions()
    {
        return new ErrorHandlingOptions
        {
            IncludeStackTrace = IncludeStackTrace,
            SanitizeErrorMessages = SanitizeErrorMessages,
            RethrowCancellation = RethrowCancellation,
            RethrowUnhandled = RethrowUnhandled
        };
    }
}

/// <summary>
/// Configuration for checkpoint storage.
/// </summary>
public class CheckpointConfiguration
{
    /// <summary>
    /// Storage provider: "InMemory", "Database", "CosmosDB", etc.
    /// Default: "InMemory"
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Connection string for checkpoint storage (if applicable).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Container/table name for checkpoint storage.
    /// Default: "Checkpoints"
    /// </summary>
    public string ContainerName { get; set; } = "Checkpoints";

    /// <summary>
    /// Enable automatic checkpoint cleanup.
    /// Default: true
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Cleanup interval in hours.
    /// Default: 24 (daily cleanup)
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}

/// <summary>
/// Validation extension for AgentConfiguration.
/// </summary>
public static class AgentConfigurationExtensions
{
    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    public static void Validate(this AgentConfiguration config)
    {
        if (config.DefaultTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "DefaultTimeoutSeconds must be greater than 0");
        }

        if (config.MaxConcurrentExecutions <= 0)
        {
            throw new InvalidOperationException(
                "MaxConcurrentExecutions must be greater than 0");
        }

        if (config.CheckpointRetentionDays <= 0)
        {
            throw new InvalidOperationException(
                "CheckpointRetentionDays must be greater than 0");
        }

        config.Retry.Validate();
    }

    private static void Validate(this RetryConfiguration config)
    {
        if (config.MaxRetryAttempts < 0)
        {
            throw new InvalidOperationException(
                "MaxRetryAttempts must be non-negative");
        }

        if (config.InitialDelayMs < 0)
        {
            throw new InvalidOperationException(
                "InitialDelayMs must be non-negative");
        }

        if (config.MaxDelayMs < config.InitialDelayMs)
        {
            throw new InvalidOperationException(
                "MaxDelayMs must be greater than or equal to InitialDelayMs");
        }
    }
}
