using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing errors.
/// This service encapsulates business logic for error logging, resolution, and retry operations.
/// </summary>
public interface IErrorApplicationService
{
    /// <summary>
    /// Gets errors for a tenant with pagination and filtering
    /// </summary>
    Task<(List<ErrorLog> Items, int TotalCount)> GetErrorsAsync(
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
    /// Gets an error by ID
    /// </summary>
    Task<ErrorLog?> GetErrorByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets errors related to a specific entity
    /// </summary>
    Task<List<ErrorLog>> GetErrorsByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unresolved error count for a tenant
    /// </summary>
    Task<int> GetUnresolvedCountAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets error statistics for a tenant
    /// </summary>
    Task<ErrorStatistics> GetStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a new error
    /// </summary>
    Task<ErrorLog> LogErrorAsync(
        Guid tenantId,
        ErrorSeverity severity,
        string message,
        string? stackTrace = null,
        string? entityType = null,
        Guid? entityId = null,
        string? contextData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an error as resolved
    /// </summary>
    Task MarkErrorResolvedAsync(
        Guid errorId,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk marks errors as resolved
    /// </summary>
    Task BulkMarkErrorsResolvedAsync(
        List<Guid> errorIds,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed operation associated with an error.
    /// This will attempt to trigger the workflow for the related entity.
    /// </summary>
    Task<bool> RetryFailedOperationAsync(Guid errorId, CancellationToken cancellationToken = default);
}
