using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Query parameters for filtering and paginating error logs
/// </summary>
public class ErrorQueryParameters
{
    /// <summary>
    /// Tenant ID to filter errors
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Filter by severity level
    /// </summary>
    public ErrorSeverity? Severity { get; }

    /// <summary>
    /// Filter by entity type
    /// </summary>
    public string? EntityType { get; }

    /// <summary>
    /// Filter by resolution status
    /// </summary>
    public bool? IsResolved { get; }

    /// <summary>
    /// Filter by errors created from this date
    /// </summary>
    public DateTime? FromDate { get; }

    /// <summary>
    /// Filter by errors created to this date
    /// </summary>
    public DateTime? ToDate { get; }

    /// <summary>
    /// Search term for filtering errors
    /// </summary>
    public string? SearchTerm { get; }

    public ErrorQueryParameters(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        ErrorSeverity? severity = null,
        string? entityType = null,
        bool? isResolved = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (page < 1)
            throw new ArgumentException("Page must be greater than 0", nameof(page));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

        TenantId = tenantId;
        Page = page;
        PageSize = pageSize;
        Severity = severity;
        EntityType = entityType;
        IsResolved = isResolved;
        FromDate = fromDate;
        ToDate = toDate;
        SearchTerm = searchTerm;
    }
}
