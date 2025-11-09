using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing workflow events in the UI
/// </summary>
public interface IWorkflowEventService
{
    /// <summary>
    /// Gets workflow events with pagination and filtering
    /// </summary>
    Task<PagedResult<WorkflowEventDto>> GetEventsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchText = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific workflow event by ID
    /// </summary>
    Task<WorkflowEventDto?> GetEventByIdAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Gets workflow event statistics
    /// </summary>
    Task<EventStatisticsDto> GetStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all available event types for filtering
    /// </summary>
    Task<List<string>> GetEventTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Exports events to CSV format
    /// </summary>
    Task<byte[]> ExportToCsvAsync(
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports events to JSON format
    /// </summary>
    Task<byte[]> ExportToJsonAsync(
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);
}
