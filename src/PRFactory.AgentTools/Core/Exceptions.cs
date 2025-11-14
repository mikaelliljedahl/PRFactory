namespace PRFactory.AgentTools.Core;

/// <summary>
/// Exception thrown when a tool execution times out
/// </summary>
public class ToolTimeoutException : Exception
{
    /// <summary>
    /// Tool name that timed out
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Timeout duration
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Create a new ToolTimeoutException
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <param name="timeout">Timeout duration</param>
    public ToolTimeoutException(string toolName, TimeSpan timeout)
        : base($"Tool '{toolName}' timed out after {timeout.TotalSeconds:F2} seconds")
    {
        ToolName = toolName;
        Timeout = timeout;
    }
}

/// <summary>
/// Exception thrown when a file is too large for tool processing
/// </summary>
public class FileTooLargeException : Exception
{
    /// <summary>
    /// Create a new FileTooLargeException
    /// </summary>
    /// <param name="message">Error message</param>
    public FileTooLargeException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when a tool is not found in the registry
/// </summary>
public class ToolNotFoundException : Exception
{
    /// <summary>
    /// Tool name that was not found
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Create a new ToolNotFoundException
    /// </summary>
    /// <param name="toolName">Tool name</param>
    public ToolNotFoundException(string toolName)
        : base($"Tool '{toolName}' not found in registry")
    {
        ToolName = toolName;
    }
}
