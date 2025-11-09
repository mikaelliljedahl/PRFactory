using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Application service for managing workflow events.
/// Retrieves events from tickets and provides them for timeline display.
/// </summary>
public class WorkflowEventApplicationService : IWorkflowEventApplicationService
{
    private readonly ILogger<WorkflowEventApplicationService> _logger;
    private readonly ITicketRepository _ticketRepository;

    public WorkflowEventApplicationService(
        ILogger<WorkflowEventApplicationService> logger,
        ITicketRepository ticketRepository)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
    }

    /// <inheritdoc/>
    public async Task<List<WorkflowEvent>> GetEventsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting workflow events for ticket {TicketId}", ticketId);

        // Use GetByIdWithEventsAsync to eagerly load events
        var ticket = await _ticketRepository.GetByIdWithEventsAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", ticketId);
            return new List<WorkflowEvent>();
        }

        // Return events ordered by occurrence time (most recent first)
        var events = ticket.Events
            .OrderByDescending(e => e.OccurredAt)
            .ToList();

        _logger.LogDebug("Found {EventCount} events for ticket {TicketId}", events.Count, ticketId);

        return events;
    }
}
