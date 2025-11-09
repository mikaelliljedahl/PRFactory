using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing workflow events.
/// Retrieves events from tickets and provides them for timeline display.
/// </summary>
public interface IWorkflowEventApplicationService
{
    /// <summary>
    /// Gets all workflow events for a ticket, ordered by occurrence time
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of workflow events ordered by time (most recent first)</returns>
    Task<List<WorkflowEvent>> GetEventsAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
