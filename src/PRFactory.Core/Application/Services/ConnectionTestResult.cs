namespace PRFactory.Core.Application.Services;

/// <summary>
/// Result of a connection test (used by repository and provider services)
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Whether the connection test was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; init; }

    /// <summary>
    /// Error details if the connection test failed
    /// </summary>
    public string? ErrorDetails { get; init; }
}
