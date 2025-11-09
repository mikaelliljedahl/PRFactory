using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Application service for plan review management operations
/// </summary>
public interface IPlanReviewService
{
    /// <summary>
    /// Assigns reviewers to a ticket's plan
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="requiredReviewerIds">List of user IDs who must approve the plan</param>
    /// <param name="optionalReviewerIds">Optional list of user IDs who can optionally review</param>
    Task AssignReviewersAsync(
        Guid ticketId,
        List<Guid> requiredReviewerIds,
        List<Guid>? optionalReviewerIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews for a specific ticket
    /// </summary>
    Task<List<PlanReview>> GetReviewsByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending reviews assigned to a specific reviewer
    /// </summary>
    Task<List<PlanReview>> GetPendingReviewsForReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a plan review
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="decision">Optional note explaining the approval</param>
    Task ApproveReviewAsync(Guid reviewId, string? decision = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a plan review
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="reason">Explanation of why the plan was rejected</param>
    /// <param name="regenerateCompletely">If true, requests complete regeneration. If false, requests refinement.</param>
    Task RejectReviewAsync(Guid reviewId, string reason, bool regenerateCompletely, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a comment to a ticket review discussion
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="authorId">The user ID of the comment author</param>
    /// <param name="content">The comment text (supports markdown)</param>
    /// <param name="mentionedUserIds">Optional list of user IDs mentioned in the comment</param>
    Task<ReviewComment> AddCommentAsync(
        Guid ticketId,
        Guid authorId,
        string content,
        List<Guid>? mentionedUserIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all comments for a specific ticket
    /// </summary>
    Task<List<ReviewComment>> GetCommentsByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing comment
    /// </summary>
    Task UpdateCommentAsync(Guid commentId, string content, List<Guid>? mentionedUserIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a comment
    /// </summary>
    Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a ticket has sufficient approvals to proceed
    /// </summary>
    Task<bool> HasSufficientApprovalsAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all reviews for a ticket when a new plan is generated
    /// </summary>
    Task ResetReviewsForNewPlanAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
