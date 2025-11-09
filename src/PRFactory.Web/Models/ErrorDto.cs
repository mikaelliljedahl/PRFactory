using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO for error log display in the Web UI
/// </summary>
public class ErrorDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public ErrorSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? ContextData { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties for UI
    public string SeverityClass => Severity switch
    {
        ErrorSeverity.Critical => "danger",
        ErrorSeverity.High => "warning",
        ErrorSeverity.Medium => "info",
        ErrorSeverity.Low => "secondary",
        _ => "secondary"
    };

    public string SeverityIcon => Severity switch
    {
        ErrorSeverity.Critical => "exclamation-triangle-fill",
        ErrorSeverity.High => "exclamation-circle-fill",
        ErrorSeverity.Medium => "info-circle-fill",
        ErrorSeverity.Low => "info-circle",
        _ => "info-circle"
    };

    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public string FormattedResolvedAt => ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
}
