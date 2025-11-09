using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing tickets.
/// This service encapsulates business logic for ticket operations.
/// </summary>
public interface ITicketApplicationService
{
    /// <summary>
    /// Gets all tickets for the current tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tickets</returns>
    Task<List<Ticket>> GetAllTicketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific ticket by ID
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ticket, or null if not found</returns>
    Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets for a specific repository
    /// </summary>
    /// <param name="repositoryId">The repository ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tickets for the repository</returns>
    Task<List<Ticket>> GetTicketsByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers the workflow for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or workflow cannot be started</exception>
    Task TriggerWorkflowAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a plan for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="comments">Optional approval comments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or plan cannot be approved</exception>
    Task ApprovePlanAsync(Guid ticketId, string? comments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a plan for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="rejectionReason">Reason for rejection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or plan cannot be rejected</exception>
    Task RejectPlanAsync(Guid ticketId, string rejectionReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits answers to refinement questions for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="answers">Dictionary of question-answer pairs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or answers cannot be submitted</exception>
    Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken cancellationToken = default);
}
