using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for InlineCommentAnchor entity operations.
/// </summary>
public interface IInlineCommentAnchorRepository
{
    /// <summary>
    /// Gets an inline comment anchor by its ID
    /// </summary>
    Task<InlineCommentAnchor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the anchor associated with a specific review comment
    /// </summary>
    Task<InlineCommentAnchor?> GetByCommentIdAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all anchors for comments on a specific ticket
    /// </summary>
    Task<List<InlineCommentAnchor>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inline comment anchor
    /// </summary>
    Task<InlineCommentAnchor> CreateAsync(InlineCommentAnchor anchor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inline comment anchor
    /// </summary>
    Task UpdateAsync(InlineCommentAnchor anchor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an inline comment anchor by ID
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
