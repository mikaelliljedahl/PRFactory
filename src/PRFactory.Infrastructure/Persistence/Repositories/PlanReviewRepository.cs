using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PlanReview entity operations.
/// </summary>
public class PlanReviewRepository : IPlanReviewRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanReviewRepository> _logger;

    public PlanReviewRepository(
        ApplicationDbContext context,
        ILogger<PlanReviewRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlanReview?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanReviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task<PlanReview?> GetByIdWithChecklistAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanReviews
            .Include(r => r.Reviewer)
            .Include(r => r.Checklist!)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task<List<PlanReview>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanReviews
            .Include(r => r.Reviewer)
            .Where(r => r.TicketId == ticketId)
            .OrderBy(r => r.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanReview?> GetByTicketAndReviewerAsync(Guid ticketId, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanReviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.TicketId == ticketId && r.ReviewerId == reviewerId, cancellationToken);
    }

    public async Task<List<PlanReview>> GetPendingByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _context.PlanReviews
            .Include(r => r.Ticket)
            .Where(r => r.ReviewerId == reviewerId && r.Status == ReviewStatus.Pending)
            .OrderByDescending(r => r.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanReview> CreateAsync(PlanReview review, CancellationToken cancellationToken = default)
    {
        _context.PlanReviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created plan review {ReviewId} for ticket {TicketId}, reviewer {ReviewerId}",
            review.Id, review.TicketId, review.ReviewerId);

        return review;
    }

    public async Task UpdateAsync(PlanReview review, CancellationToken cancellationToken = default)
    {
        _context.PlanReviews.Update(review);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated plan review {ReviewId}, status: {Status}", review.Id, review.Status);
    }

    public async Task DeleteByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.PlanReviews
            .Where(r => r.TicketId == ticketId)
            .ToListAsync(cancellationToken);

        if (reviews.Any())
        {
            _context.PlanReviews.RemoveRange(reviews);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} plan reviews for ticket {TicketId}", reviews.Count, ticketId);
        }
    }
}
