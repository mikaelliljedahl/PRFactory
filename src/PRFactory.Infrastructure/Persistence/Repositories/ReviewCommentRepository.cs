using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ReviewComment entity operations.
/// </summary>
public class ReviewCommentRepository : IReviewCommentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewCommentRepository> _logger;

    public ReviewCommentRepository(
        ApplicationDbContext context,
        ILogger<ReviewCommentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReviewComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewComments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
    }

    public async Task<List<ReviewComment>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReviewComment>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewComments
            .Include(c => c.Ticket)
            .Where(c => c.AuthorId == authorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReviewComment>> GetByMentionedUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewComments
            .Include(c => c.Author)
            .Include(c => c.Ticket)
            .Where(c => c.MentionedUserIds.Contains(userId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewComment> CreateAsync(ReviewComment comment, CancellationToken cancellationToken = default)
    {
        _context.ReviewComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created review comment {CommentId} for ticket {TicketId} by author {AuthorId}",
            comment.Id, comment.TicketId, comment.AuthorId);

        return comment;
    }

    public async Task UpdateAsync(ReviewComment comment, CancellationToken cancellationToken = default)
    {
        _context.ReviewComments.Update(comment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated review comment {CommentId}", comment.Id);
    }

    public async Task DeleteAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
        {
            _logger.LogWarning("Attempted to delete non-existent comment {CommentId}", commentId);
            return;
        }

        _context.ReviewComments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted review comment {CommentId}", commentId);
    }
}
