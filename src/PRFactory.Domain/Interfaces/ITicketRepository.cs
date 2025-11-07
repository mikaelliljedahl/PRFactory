using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for Ticket aggregate root operations
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// Gets a ticket by its unique identifier
    /// </summary>
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a ticket by its ticket key (e.g., "PROJ-123")
    /// </summary>
    Task<Ticket?> GetByTicketKeyAsync(string ticketKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets for a specific tenant
    /// </summary>
    Task<List<Ticket>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets for a specific repository
    /// </summary>
    Task<List<Ticket>> GetByRepositoryIdAsync(Guid repositoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets in a specific workflow state
    /// </summary>
    Task<List<Ticket>> GetByStateAsync(WorkflowState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets that are in a specific set of workflow states
    /// </summary>
    Task<List<Ticket>> GetByStatesAsync(IEnumerable<WorkflowState> states, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets awaiting answers for more than the specified duration
    /// </summary>
    Task<List<Ticket>> GetStaleAwaitingAnswersAsync(TimeSpan threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tickets in failed state that can be retried
    /// </summary>
    Task<List<Ticket>> GetRetryableFailedTicketsAsync(int maxRetries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-terminal) tickets for a tenant
    /// </summary>
    Task<List<Ticket>> GetActiveTicketsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tickets with their events eagerly loaded
    /// </summary>
    Task<Ticket?> GetByIdWithEventsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tickets created within a date range
    /// </summary>
    Task<List<Ticket>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new ticket
    /// </summary>
    Task<Ticket> AddAsync(Ticket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing ticket
    /// </summary>
    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a ticket (should rarely be used)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of tickets by state for a tenant
    /// </summary>
    Task<Dictionary<WorkflowState, int>> GetStateCountsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a ticket with the given key already exists
    /// </summary>
    Task<bool> ExistsAsync(string ticketKey, CancellationToken cancellationToken = default);
}
