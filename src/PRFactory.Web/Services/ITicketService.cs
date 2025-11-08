using PRFactory.Domain.Entities;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing tickets via API
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Get all tickets
    /// </summary>
    Task<List<Ticket>> GetAllTicketsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific ticket by ID
    /// </summary>
    Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>
    /// Get tickets by repository
    /// </summary>
    Task<List<Ticket>> GetTicketsByRepositoryAsync(Guid repositoryId, CancellationToken ct = default);

    /// <summary>
    /// Trigger workflow for a ticket
    /// </summary>
    Task TriggerWorkflowAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>
    /// Approve a plan
    /// </summary>
    Task ApprovePlanAsync(Guid ticketId, string? comments = null, CancellationToken ct = default);

    /// <summary>
    /// Reject a plan
    /// </summary>
    Task RejectPlanAsync(Guid ticketId, string rejectionReason, CancellationToken ct = default);

    /// <summary>
    /// Submit answers to refinement questions
    /// </summary>
    Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken ct = default);

    /// <summary>
    /// Get the latest ticket update for a ticket
    /// </summary>
    Task<TicketUpdateDto?> GetLatestTicketUpdateAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>
    /// Update a ticket update
    /// </summary>
    Task UpdateTicketUpdateAsync(Guid ticketUpdateId, TicketUpdateDto ticketUpdate, CancellationToken ct = default);

    /// <summary>
    /// Approve a ticket update
    /// </summary>
    Task ApproveTicketUpdateAsync(Guid ticketUpdateId, CancellationToken ct = default);

    /// <summary>
    /// Reject a ticket update
    /// </summary>
    Task RejectTicketUpdateAsync(Guid ticketUpdateId, string rejectionReason, CancellationToken ct = default);
}
