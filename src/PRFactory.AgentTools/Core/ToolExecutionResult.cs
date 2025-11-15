namespace PRFactory.AgentTools.Core;

/// <summary>
/// Result of tool execution. Includes output, success status, and metadata.
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// Tool output (result of execution)
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Additional metadata about execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Create a successful result
    /// </summary>
    /// <param name="output">Tool output</param>
    /// <param name="duration">Execution duration</param>
    /// <returns>Success result</returns>
    public static ToolExecutionResult CreateSuccess(string output, TimeSpan duration)
    {
        return new ToolExecutionResult
        {
            Output = output,
            Success = true,
            Duration = duration
        };
    }

    /// <summary>
    /// Create a failure result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="duration">Execution duration</param>
    /// <returns>Failure result</returns>
    public static ToolExecutionResult CreateFailure(string errorMessage, TimeSpan duration)
    {
        return new ToolExecutionResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Duration = duration
        };
    }
}
