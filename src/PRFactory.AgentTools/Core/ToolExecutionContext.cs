namespace PRFactory.AgentTools.Core;

/// <summary>
/// Context for tool execution. Contains tenant info, workspace path, and parameters.
/// </summary>
public class ToolExecutionContext
{
    /// <summary>
    /// Tenant identifier for multi-tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Ticket identifier for tracking
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Workspace root path for file operations
    /// </summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// Tool-specific parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Get a required parameter with type conversion
    /// </summary>
    /// <typeparam name="T">Expected parameter type</typeparam>
    /// <param name="name">Parameter name</param>
    /// <returns>Parameter value</returns>
    /// <exception cref="ArgumentException">Thrown when parameter is missing</exception>
    public T GetParameter<T>(string name)
    {
        if (!Parameters.TryGetValue(name, out var value))
            throw new ArgumentException($"Parameter '{name}' is required", nameof(name));

        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    /// Get an optional parameter with default value
    /// </summary>
    /// <typeparam name="T">Expected parameter type</typeparam>
    /// <param name="name">Parameter name</param>
    /// <param name="defaultValue">Default value if parameter is missing</param>
    /// <returns>Parameter value or default</returns>
    public T? GetOptionalParameter<T>(string name, T? defaultValue = default)
    {
        return Parameters.TryGetValue(name, out var value)
            ? (T)Convert.ChangeType(value, typeof(T))
            : defaultValue;
    }
}
