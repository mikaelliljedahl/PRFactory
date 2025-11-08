using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for managing ticket updates.
/// This service encapsulates business logic for ticket update operations.
/// </summary>
public interface ITicketUpdateService
{
    /// <summary>
    /// Gets the latest ticket update for a specific ticket
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest ticket update, or null if not found</returns>
    Task<TicketUpdate?> GetLatestTicketUpdateAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing ticket update (for manual edits)
    /// </summary>
    /// <param name="ticketUpdateId">The ticket update ID</param>
    /// <param name="updatedTitle">The updated title</param>
    /// <param name="updatedDescription">The updated description</param>
    /// <param name="acceptanceCriteria">The updated acceptance criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated ticket update</returns>
    /// <exception cref="InvalidOperationException">Thrown if ticket update not found or not editable</exception>
    Task<TicketUpdate> UpdateTicketUpdateAsync(
        Guid ticketUpdateId,
        string updatedTitle,
        string updatedDescription,
        string acceptanceCriteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a ticket update and triggers workflow continuation
    /// </summary>
    /// <param name="ticketUpdateId">The ticket update ID</param>
    /// <param name="approvedBy">Who approved the update (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The approved ticket update</returns>
    /// <exception cref="InvalidOperationException">Thrown if ticket update not found or not approvable</exception>
    Task<TicketUpdate> ApproveTicketUpdateAsync(
        Guid ticketUpdateId,
        string? approvedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a ticket update with optional regeneration
    /// </summary>
    /// <param name="ticketUpdateId">The ticket update ID</param>
    /// <param name="reason">Reason for rejection</param>
    /// <param name="rejectedBy">Who rejected the update (optional)</param>
    /// <param name="regenerate">Whether to regenerate a new version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rejected ticket update</returns>
    /// <exception cref="InvalidOperationException">Thrown if ticket update not found or not rejectable</exception>
    Task<TicketUpdate> RejectTicketUpdateAsync(
        Guid ticketUpdateId,
        string reason,
        string? rejectedBy = null,
        bool regenerate = true,
        CancellationToken cancellationToken = default);
}
