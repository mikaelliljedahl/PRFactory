using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for InlineCommentAnchor entity operations.
/// </summary>
public class InlineCommentAnchorRepository : IInlineCommentAnchorRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InlineCommentAnchorRepository> _logger;

    public InlineCommentAnchorRepository(
        ApplicationDbContext context,
        ILogger<InlineCommentAnchorRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InlineCommentAnchor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InlineCommentAnchors
            .Include(a => a.ReviewComment)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<InlineCommentAnchor?> GetByCommentIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await _context.InlineCommentAnchors
            .Include(a => a.ReviewComment)
            .FirstOrDefaultAsync(a => a.ReviewCommentId == commentId, cancellationToken);
    }

    public async Task<List<InlineCommentAnchor>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.InlineCommentAnchors
            .Include(a => a.ReviewComment)
                .ThenInclude(c => c.Author)
            .Where(a => a.ReviewComment.TicketId == ticketId)
            .OrderBy(a => a.StartLine)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<InlineCommentAnchor> CreateAsync(InlineCommentAnchor anchor, CancellationToken cancellationToken = default)
    {
        _context.InlineCommentAnchors.Add(anchor);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created inline comment anchor {AnchorId} for comment {CommentId} at lines {StartLine}-{EndLine}",
            anchor.Id, anchor.ReviewCommentId, anchor.StartLine, anchor.EndLine);

        return anchor;
    }

    public async Task UpdateAsync(InlineCommentAnchor anchor, CancellationToken cancellationToken = default)
    {
        _context.InlineCommentAnchors.Update(anchor);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated inline comment anchor {AnchorId}", anchor.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var anchor = await GetByIdAsync(id, cancellationToken);
        if (anchor == null)
        {
            _logger.LogWarning("Attempted to delete non-existent inline comment anchor {AnchorId}", id);
            return;
        }

        _context.InlineCommentAnchors.Remove(anchor);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted inline comment anchor {AnchorId}", id);
    }
}
