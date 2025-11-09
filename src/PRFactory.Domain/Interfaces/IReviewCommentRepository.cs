using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for managing review comments
/// </summary>
public interface IReviewCommentRepository
{
    /// <summary>
    /// Gets a review comment by its unique ID
    /// </summary>
    Task<ReviewComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all comments for a specific ticket, ordered by creation time
    /// </summary>
    Task<List<ReviewComment>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all comments authored by a specific user
    /// </summary>
    Task<List<ReviewComment>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all comments where a specific user is mentioned
    /// </summary>
    Task<List<ReviewComment>> GetByMentionedUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new review comment
    /// </summary>
    Task<ReviewComment> CreateAsync(ReviewComment comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing review comment
    /// </summary>
    Task UpdateAsync(ReviewComment comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a review comment by ID
    /// </summary>
    Task DeleteAsync(Guid commentId, CancellationToken cancellationToken = default);
}
