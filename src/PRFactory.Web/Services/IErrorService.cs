using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing errors via Blazor UI.
/// This is a facade service that converts between domain entities and DTOs.
/// </summary>
public interface IErrorService
{
    /// <summary>
    /// Get errors with pagination and filtering
    /// </summary>
    Task<(List<ErrorDto> Items, int TotalCount)> GetErrorsAsync(
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        ErrorSeverity? severity = null,
        string? entityType = null,
        bool? isResolved = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get an error by ID
    /// </summary>
    Task<ErrorDto?> GetErrorByIdAsync(Guid errorId, CancellationToken ct = default);

    /// <summary>
    /// Get errors related to a specific entity
    /// </summary>
    Task<List<ErrorDto>> GetErrorsByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Get unresolved error count
    /// </summary>
    Task<int> GetUnresolvedCountAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Get error statistics
    /// </summary>
    Task<ErrorStatisticsDto> GetStatisticsAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Mark an error as resolved
    /// </summary>
    Task MarkErrorResolvedAsync(
        Guid errorId,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Bulk mark errors as resolved
    /// </summary>
    Task BulkMarkErrorsResolvedAsync(
        List<Guid> errorIds,
        string? resolvedBy = null,
        string? resolutionNotes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retry a failed operation
    /// </summary>
    Task<bool> RetryFailedOperationAsync(Guid errorId, CancellationToken ct = default);
}
