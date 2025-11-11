using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an error that occurred during workflow processing.
/// Provides detailed error information for debugging and monitoring.
/// </summary>
public class ErrorLog
{
    /// <summary>
    /// Unique identifier for the error log entry
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The tenant this error belongs to
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Severity level of the error
    /// </summary>
    public ErrorSeverity Severity { get; private set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Stack trace of the error (optional)
    /// </summary>
    public string? StackTrace { get; private set; }

    /// <summary>
    /// Type of entity this error is related to (e.g., "Ticket", "Repository", "Checkpoint")
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// ID of the related entity (e.g., TicketId, RepositoryId)
    /// </summary>
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// Additional context data as JSON (e.g., request parameters, state information)
    /// </summary>
    public string? ContextData { get; private set; }

    /// <summary>
    /// Whether this error has been resolved/acknowledged
    /// </summary>
    public bool IsResolved { get; private set; }

    /// <summary>
    /// When the error was marked as resolved
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// Who resolved the error (username/email)
    /// </summary>
    public string? ResolvedBy { get; private set; }

    /// <summary>
    /// Notes about the resolution (e.g., root cause, fix applied)
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; }

    private ErrorLog() { }

    /// <summary>
    /// Creates a new error log entry
    /// </summary>
    public static ErrorLog Create(
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
            throw new ArgumentException("Error message cannot be empty", nameof(message));

        var errorLog = new ErrorLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Severity = severity,
            Message = message,
            StackTrace = stackTrace,
            EntityType = entityType,
            EntityId = entityId,
            ContextData = contextData,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        return errorLog;
    }

    /// <summary>
    /// Marks the error as resolved
    /// </summary>
    public void MarkAsResolved(string? resolvedBy = null, string? resolutionNotes = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        ResolutionNotes = resolutionNotes;
    }

    /// <summary>
    /// Reopens a resolved error
    /// </summary>
    public void Reopen()
    {
        IsResolved = false;
        ResolvedAt = null;
        ResolvedBy = null;
        ResolutionNotes = null;
    }

    /// <summary>
    /// Updates the resolution notes
    /// </summary>
    public void UpdateResolutionNotes(string notes)
    {
        if (!IsResolved)
            throw new InvalidOperationException("Cannot update resolution notes for an unresolved error");

        ResolutionNotes = notes;
    }
}
