using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for WorkflowEvent operations
/// </summary>
public interface IWorkflowEventRepository
{
    /// <summary>
    /// Gets a workflow event by its unique identifier
    /// </summary>
    Task<WorkflowEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workflow events for a specific ticket
    /// </summary>
    Task<List<WorkflowEvent>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflow events with pagination and filtering
    /// </summary>
    Task<(List<WorkflowEvent> Events, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchText = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflow events within a date range
    /// </summary>
    Task<List<WorkflowEvent>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflow events by type
    /// </summary>
    Task<List<WorkflowEvent>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of events by type
    /// </summary>
    Task<Dictionary<string, int>> GetEventTypeCountsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of workflow events
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new workflow event
    /// </summary>
    Task<WorkflowEvent> AddAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple workflow events
    /// </summary>
    Task AddRangeAsync(IEnumerable<WorkflowEvent> workflowEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old workflow events (for maintenance/cleanup)
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
