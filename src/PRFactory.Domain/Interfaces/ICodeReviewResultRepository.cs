using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for CodeReviewResult entity operations
/// </summary>
public interface ICodeReviewResultRepository
{
    /// <summary>
    /// Gets a code review result by its unique identifier
    /// </summary>
    Task<CodeReviewResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all code review results for a specific ticket
    /// </summary>
    Task<List<CodeReviewResult>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest code review result for a specific ticket
    /// </summary>
    Task<CodeReviewResult?> GetLatestByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets code review results for a specific pull request
    /// </summary>
    Task<List<CodeReviewResult>> GetByPullRequestAsync(int pullRequestNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all code review results that passed (no critical issues)
    /// </summary>
    Task<List<CodeReviewResult>> GetPassedReviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all code review results that failed (has critical issues)
    /// </summary>
    Task<List<CodeReviewResult>> GetFailedReviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets code review results by LLM provider
    /// </summary>
    Task<List<CodeReviewResult>> GetByLlmProviderAsync(string llmProviderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets code review results within a date range
    /// </summary>
    Task<List<CodeReviewResult>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of reviews by outcome (passed vs failed) for a ticket
    /// </summary>
    Task<(int Passed, int Failed)> GetReviewCountsAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new code review result
    /// </summary>
    Task<CodeReviewResult> AddAsync(CodeReviewResult reviewResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing code review result
    /// </summary>
    Task UpdateAsync(CodeReviewResult reviewResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a code review result
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a code review result exists for a specific ticket and retry attempt
    /// </summary>
    Task<bool> ExistsForRetryAttemptAsync(Guid ticketId, int retryAttempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the retry count for a specific ticket
    /// </summary>
    Task<int> GetRetryCountAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
