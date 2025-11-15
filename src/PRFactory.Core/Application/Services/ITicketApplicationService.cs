using PRFactory.Domain.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Results;
using PRFactory.Domain.ValueObjects;

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
    /// Gets tickets with pagination, filtering, and sorting support
    /// </summary>
    /// <param name="paginationParams">Pagination and filtering parameters</param>
    /// <param name="stateFilter">Optional state filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of tickets</returns>
    Task<PagedResult<Ticket>> GetTicketsPagedAsync(
        PaginationParams paginationParams,
        WorkflowState? stateFilter = null,
        CancellationToken cancellationToken = default);

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
    /// <param name="regenerateCompletely">If true, regenerate the plan from scratch; if false, refine the existing plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or plan cannot be rejected</exception>
    Task RejectPlanAsync(Guid ticketId, string rejectionReason, bool regenerateCompletely = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines a plan for a ticket with specific instructions
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="refinementInstructions">Specific instructions for refining the plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or plan cannot be refined</exception>
    Task RefinePlanAsync(Guid ticketId, string refinementInstructions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits answers to refinement questions for a ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="answers">Dictionary of question-answer pairs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if ticket not found or answers cannot be submitted</exception>
    Task SubmitAnswersAsync(Guid ticketId, Dictionary<string, string> answers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the diff content for a ticket if available.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Diff content, or null if not available</returns>
    Task<string?> GetDiffContentAsync(Guid ticketId);

    /// <summary>
    /// Creates a pull request for a ticket with approved code changes.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="approvedBy">User who approved the changes</param>
    /// <returns>PR creation result with URL and number</returns>
    Task<PullRequestCreationResult> CreatePullRequestAsync(Guid ticketId, string? approvedBy = null);
}
