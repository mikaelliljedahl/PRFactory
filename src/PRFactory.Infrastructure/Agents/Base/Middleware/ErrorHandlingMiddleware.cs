using Microsoft.Extensions.Logging;
using System.Net;

namespace PRFactory.Infrastructure.Agents.Base.Middleware;

/// <summary>
/// Middleware that provides centralized error handling for agent execution.
/// Catches and logs exceptions, converts them to appropriate AgentResult objects.
/// </summary>
public class ErrorHandlingMiddleware : IAgentMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly ErrorHandlingOptions _options;

    public ErrorHandlingMiddleware(
        ILogger<ErrorHandlingMiddleware> logger,
        ErrorHandlingOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new ErrorHandlingOptions();
    }

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        Func<AgentContext, CancellationToken, Task<AgentResult>> next,
        CancellationToken cancellationToken = default)
    {
        var agentName = context.Metadata.CurrentPhase ?? "Unknown";

        try
        {
            return await next(context, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            // Handle cancellation separately
            _logger.LogWarning(
                ex,
                "Agent {AgentName} execution was cancelled for ticket {TicketId}",
                agentName,
                context.TicketId);

            // Store error in context for potential retry
            context.SetState(StateKeys.ErrorMessage, "Operation was cancelled");

            if (_options.RethrowCancellation)
            {
                throw;
            }

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Operation cancelled",
                ErrorDetails = ex.ToString()
            };
        }
        catch (AgentValidationException ex)
        {
            // Handle validation errors (user input errors, not system errors)
            _logger.LogWarning(
                ex,
                "Agent {AgentName} validation failed for ticket {TicketId}: {ValidationError}",
                agentName,
                context.TicketId,
                ex.Message);

            context.SetState(StateKeys.ErrorMessage, ex.Message);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = ex.Message,
                ErrorDetails = _options.IncludeStackTrace ? ex.ToString() : ex.Message,
                ShouldRetry = false // Don't retry validation errors
            };
        }
        catch (TransientException ex)
        {
            // Handle transient errors that should be retried
            _logger.LogWarning(
                ex,
                "Agent {AgentName} encountered transient error for ticket {TicketId}: {Error}",
                agentName,
                context.TicketId,
                ex.Message);

            context.SetState(StateKeys.ErrorMessage, ex.Message);
            IncrementRetryCount(context);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = ex.Message,
                ErrorDetails = _options.IncludeStackTrace ? ex.ToString() : ex.Message,
                ShouldRetry = true
            };
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP-related errors (API calls, webhooks, etc.)
            _logger.LogError(
                ex,
                "Agent {AgentName} HTTP request failed for ticket {TicketId}: {Error}",
                agentName,
                context.TicketId,
                ex.Message);

            context.SetState(StateKeys.ErrorMessage, $"HTTP request failed: {ex.Message}");
            IncrementRetryCount(context);

            // Determine if it's a transient HTTP error
            var isTransient = IsTransientHttpError(ex);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"HTTP request failed: {ex.Message}",
                ErrorDetails = _options.IncludeStackTrace ? ex.ToString() : ex.Message,
                ShouldRetry = isTransient
            };
        }
        catch (TimeoutException ex)
        {
            // Handle timeout errors
            _logger.LogError(
                ex,
                "Agent {AgentName} timed out for ticket {TicketId}",
                agentName,
                context.TicketId);

            context.SetState(StateKeys.ErrorMessage, "Operation timed out");
            IncrementRetryCount(context);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Operation timed out",
                ErrorDetails = _options.IncludeStackTrace ? ex.ToString() : ex.Message,
                ShouldRetry = true // Timeouts are usually transient
            };
        }
        catch (Exception ex)
        {
            // Handle all other unexpected errors
            _logger.LogError(
                ex,
                "Agent {AgentName} encountered unexpected error for ticket {TicketId}: {ErrorType} - {Error}",
                agentName,
                context.TicketId,
                ex.GetType().Name,
                ex.Message);

            context.SetState(StateKeys.ErrorMessage, ex.Message);
            IncrementRetryCount(context);

            // Determine if we should rethrow based on configuration
            if (_options.RethrowUnhandled)
            {
                throw;
            }

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = _options.SanitizeErrorMessages ? "An unexpected error occurred" : ex.Message,
                ErrorDetails = _options.IncludeStackTrace ? ex.ToString() : ex.Message,
                ShouldRetry = false
            };
        }
    }

    private void IncrementRetryCount(AgentContext context)
    {
        var currentCount = context.GetState<int>(StateKeys.RetryCount);
        context.SetState(StateKeys.RetryCount, currentCount + 1);
    }

    private bool IsTransientHttpError(HttpRequestException ex)
    {
        // Common transient HTTP status codes
        var transientStatusCodes = new[]
        {
            HttpStatusCode.RequestTimeout,           // 408
            HttpStatusCode.TooManyRequests,         // 429
            HttpStatusCode.InternalServerError,     // 500
            HttpStatusCode.BadGateway,              // 502
            HttpStatusCode.ServiceUnavailable,      // 503
            HttpStatusCode.GatewayTimeout           // 504
        };

        if (ex.StatusCode.HasValue)
        {
            return transientStatusCodes.Contains(ex.StatusCode.Value);
        }

        // If no status code, consider network errors as transient
        return ex.InnerException is System.Net.Sockets.SocketException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Configuration options for error handling middleware.
/// </summary>
public class ErrorHandlingOptions
{
    /// <summary>
    /// Whether to include stack traces in error details.
    /// Default: true (but should be false in production for security).
    /// </summary>
    public bool IncludeStackTrace { get; set; } = true;

    /// <summary>
    /// Whether to sanitize error messages to avoid leaking sensitive information.
    /// Default: false (but should be true in production).
    /// </summary>
    public bool SanitizeErrorMessages { get; set; } = false;

    /// <summary>
    /// Whether to rethrow OperationCanceledException.
    /// Default: true (allows cancellation to propagate properly).
    /// </summary>
    public bool RethrowCancellation { get; set; } = true;

    /// <summary>
    /// Whether to rethrow unhandled exceptions.
    /// Default: false (catch all errors and convert to AgentResult).
    /// </summary>
    public bool RethrowUnhandled { get; set; } = false;

    /// <summary>
    /// Custom error handlers for specific exception types.
    /// </summary>
    public Dictionary<Type, Func<Exception, AgentResult>> CustomHandlers { get; set; } = new();
}

/// <summary>
/// Exception thrown for validation errors in agent execution.
/// These are non-retryable errors caused by invalid input or configuration.
/// </summary>
public class AgentValidationException : Exception
{
    public AgentValidationException(string message) : base(message) { }

    public AgentValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    public string? PropertyName { get; set; }
    public object? InvalidValue { get; set; }
}

/// <summary>
/// Exception for transient errors that should be retried.
/// Examples: network timeouts, rate limiting, temporary service unavailability.
/// </summary>
public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }

    public TransientException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Suggested delay before retry (optional).
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }
}
