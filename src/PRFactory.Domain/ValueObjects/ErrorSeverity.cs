namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Represents the severity level of an error.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Low severity - Minor issues that don't impact functionality
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - Issues that impact non-critical functionality
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - Issues that impact critical functionality
    /// </summary>
    High,

    /// <summary>
    /// Critical severity - System-breaking errors that require immediate attention
    /// </summary>
    Critical
}
