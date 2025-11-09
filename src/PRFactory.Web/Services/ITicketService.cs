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
    /// Get a specific ticket by ID as DTO
    /// </summary>
    Task<TicketDto?> GetTicketDtoByIdAsync(Guid ticketId, CancellationToken ct = default);

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
    /// Reject a plan and optionally regenerate it completely
    /// </summary>
    Task RejectPlanAsync(Guid ticketId, string rejectionReason, bool regenerateCompletely = false, CancellationToken ct = default);

    /// <summary>
    /// Refine a plan with specific instructions
    /// </summary>
    Task RefinePlanAsync(Guid ticketId, string refinementInstructions, CancellationToken ct = default);

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

    /// <summary>
    /// Get questions with their answers for a ticket
    /// </summary>
    Task<List<QuestionDto>> GetQuestionsAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>
    /// Get workflow events for a ticket
    /// </summary>
    Task<List<WorkflowEventDto>> GetEventsAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>
    /// Get the implementation plan for a ticket
    /// </summary>
    Task<PlanDto?> GetPlanAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>
    /// Create a new ticket
    /// </summary>
    Task<Ticket> CreateTicketAsync(string ticketKey, string title, string description, Guid repositoryId, CancellationToken ct = default);
}
