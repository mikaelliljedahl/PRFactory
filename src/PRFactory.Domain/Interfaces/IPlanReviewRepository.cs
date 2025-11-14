using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for managing plan reviews
/// </summary>
public interface IPlanReviewRepository
{
    /// <summary>
    /// Gets a plan review by its unique ID
    /// </summary>
    Task<PlanReview?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a plan review by its unique ID including checklist items
    /// </summary>
    Task<PlanReview?> GetByIdWithChecklistAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews for a specific ticket
    /// </summary>
    Task<List<PlanReview>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a review for a specific ticket and reviewer
    /// </summary>
    Task<PlanReview?> GetByTicketAndReviewerAsync(Guid ticketId, Guid reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending reviews assigned to a specific reviewer
    /// </summary>
    Task<List<PlanReview>> GetPendingByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new plan review
    /// </summary>
    Task<PlanReview> CreateAsync(PlanReview review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing plan review
    /// </summary>
    Task UpdateAsync(PlanReview review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all reviews for a specific ticket (used when reassigning reviewers)
    /// </summary>
    Task DeleteByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
