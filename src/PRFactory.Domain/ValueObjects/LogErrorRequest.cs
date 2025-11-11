using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Request parameters for logging a new error
/// </summary>
public class LogErrorRequest
{
    /// <summary>
    /// Tenant ID for the error
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Error severity level
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Stack trace (optional)
    /// </summary>
    public string? StackTrace { get; }

    /// <summary>
    /// Entity type associated with the error (optional)
    /// </summary>
    public string? EntityType { get; }

    /// <summary>
    /// Entity ID associated with the error (optional)
    /// </summary>
    public Guid? EntityId { get; }

    /// <summary>
    /// Additional context data (optional, JSON format)
    /// </summary>
    public string? ContextData { get; }

    public LogErrorRequest(
        Guid tenantId,
        ErrorSeverity severity,
        string message,
        string? stackTrace = null,
        string? entityType = null,
        Guid? entityId = null,
        string? contextData = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        TenantId = tenantId;
        Severity = severity;
        Message = message;
        StackTrace = stackTrace;
        EntityType = entityType;
        EntityId = entityId;
        ContextData = contextData;
    }
}
