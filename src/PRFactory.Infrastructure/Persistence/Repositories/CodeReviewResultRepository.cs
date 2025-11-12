using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CodeReviewResult entity operations.
/// </summary>
public class CodeReviewResultRepository : ICodeReviewResultRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CodeReviewResultRepository> _logger;

    public CodeReviewResultRepository(
        ApplicationDbContext context,
        ILogger<CodeReviewResultRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CodeReviewResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Include(r => r.Ticket)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<CodeReviewResult>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Where(r => r.TicketId == ticketId)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CodeReviewResult?> GetLatestByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Where(r => r.TicketId == ticketId)
            .OrderByDescending(r => r.ReviewedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<CodeReviewResult>> GetByPullRequestAsync(int pullRequestNumber, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Include(r => r.Ticket)
            .Where(r => r.PullRequestNumber == pullRequestNumber)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CodeReviewResult>> GetPassedReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Include(r => r.Ticket)
            .Where(r => r.CriticalIssues.Count == 0)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CodeReviewResult>> GetFailedReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Include(r => r.Ticket)
            .Where(r => r.CriticalIssues.Count > 0)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CodeReviewResult>> GetByLlmProviderAsync(string llmProviderName, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Include(r => r.Ticket)
            .Where(r => r.LlmProviderName == llmProviderName)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CodeReviewResult>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .Include(r => r.Ticket)
            .Where(r => r.ReviewedAt >= startDate && r.ReviewedAt <= endDate)
            .OrderBy(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Passed, int Failed)> GetReviewCountsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.CodeReviewResults
            .Where(r => r.TicketId == ticketId)
            .Select(r => new { HasIssues = r.CriticalIssues.Count > 0 })
            .ToListAsync(cancellationToken);

        var passed = reviews.Count(r => !r.HasIssues);
        var failed = reviews.Count(r => r.HasIssues);

        return (passed, failed);
    }

    public async Task<CodeReviewResult> AddAsync(CodeReviewResult reviewResult, CancellationToken cancellationToken = default)
    {
        _context.CodeReviewResults.Add(reviewResult);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created code review result {ReviewId} for ticket {TicketId} - Passed: {Passed}, Issues: {IssueCount}",
            reviewResult.Id, reviewResult.TicketId, reviewResult.Passed, reviewResult.TotalIssueCount);

        return reviewResult;
    }

    public async Task UpdateAsync(CodeReviewResult reviewResult, CancellationToken cancellationToken = default)
    {
        _context.CodeReviewResults.Update(reviewResult);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated code review result {ReviewId} for ticket {TicketId}",
            reviewResult.Id, reviewResult.TicketId);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reviewResult = await GetByIdAsync(id, cancellationToken);
        if (reviewResult == null)
        {
            _logger.LogWarning("Attempted to delete non-existent code review result {ReviewId}", id);
            return;
        }

        _context.CodeReviewResults.Remove(reviewResult);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted code review result {ReviewId} for ticket {TicketId}", id, reviewResult.TicketId);
    }

    public async Task<bool> ExistsForRetryAttemptAsync(Guid ticketId, int retryAttempt, CancellationToken cancellationToken = default)
    {
        return await _context.CodeReviewResults
            .AnyAsync(r => r.TicketId == ticketId && r.RetryAttempt == retryAttempt, cancellationToken);
    }

    public async Task<int> GetRetryCountAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var maxRetry = await _context.CodeReviewResults
            .Where(r => r.TicketId == ticketId)
            .MaxAsync(r => (int?)r.RetryAttempt, cancellationToken);

        return maxRetry ?? 0;
    }
}
