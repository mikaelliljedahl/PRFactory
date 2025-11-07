using PRFactory.Domain.Entities;

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
}
