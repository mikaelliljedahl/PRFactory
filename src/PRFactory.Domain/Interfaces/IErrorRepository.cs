using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for ErrorLog entity operations
/// </summary>
public interface IErrorRepository
{
    /// <summary>
    /// Gets an error log by its unique identifier
    /// </summary>
    Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all error logs for a specific tenant with pagination
    /// </summary>
    Task<(List<ErrorLog> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        ErrorSeverity? severity = null,
        string? entityType = null,
        bool? isResolved = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets error logs by entity
    /// </summary>
    Task<List<ErrorLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unresolved errors count for a tenant
    /// </summary>
    Task<int> GetUnresolvedCountAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets error statistics for a tenant
    /// </summary>
    Task<ErrorStatistics> GetStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new error log entry
    /// </summary>
    Task<ErrorLog> AddAsync(ErrorLog errorLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing error log entry
    /// </summary>
    Task UpdateAsync(ErrorLog errorLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an error log entry
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk marks errors as resolved
    /// </summary>
    Task BulkMarkAsResolvedAsync(
        List<Guid> errorIds,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Error statistics for a tenant
/// </summary>
public class ErrorStatistics
{
    public int TotalErrors { get; set; }
    public int UnresolvedErrors { get; set; }
    public int ResolvedErrors { get; set; }
    public int CriticalErrors { get; set; }
    public int HighErrors { get; set; }
    public int MediumErrors { get; set; }
    public int LowErrors { get; set; }
    public Dictionary<string, int> ErrorsByEntityType { get; set; } = new();
    public Dictionary<DateTime, int> ErrorsByDate { get; set; } = new();
}
