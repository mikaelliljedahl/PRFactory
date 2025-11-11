namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Query parameters for filtering and paginating workflow events
/// </summary>
public class WorkflowEventQueryParameters
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Filter by ticket ID (optional)
    /// </summary>
    public Guid? TicketId { get; }

    /// <summary>
    /// Filter by event type (optional)
    /// </summary>
    public string? EventType { get; }

    /// <summary>
    /// Filter by events created from this date (optional)
    /// </summary>
    public DateTime? StartDate { get; }

    /// <summary>
    /// Filter by events created to this date (optional)
    /// </summary>
    public DateTime? EndDate { get; }

    /// <summary>
    /// Search term for filtering events (optional)
    /// </summary>
    public string? SearchText { get; }

    public WorkflowEventQueryParameters(
        int pageNumber,
        int pageSize,
        Guid? ticketId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchText = null)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

        PageNumber = pageNumber;
        PageSize = pageSize;
        TicketId = ticketId;
        EventType = eventType;
        StartDate = startDate;
        EndDate = endDate;
        SearchText = searchText;
    }
}
