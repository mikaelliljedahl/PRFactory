using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for TicketUpdate entity operations
/// </summary>
public interface ITicketUpdateRepository
{
    /// <summary>
    /// Gets a ticket update by its unique identifier
    /// </summary>
    Task<TicketUpdate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ticket updates for a specific ticket
    /// </summary>
    Task<List<TicketUpdate>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest draft ticket update for a specific ticket
    /// Returns null if no draft exists
    /// </summary>
    Task<TicketUpdate?> GetLatestDraftByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest approved ticket update for a specific ticket
    /// Returns null if no approved update exists
    /// </summary>
    Task<TicketUpdate?> GetLatestApprovedByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ticket updates for a specific ticket ordered by version (ascending)
    /// Useful for viewing the version history
    /// </summary>
    Task<List<TicketUpdate>> GetVersionHistoryAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ticket updates that are approved but not yet posted
    /// </summary>
    Task<List<TicketUpdate>> GetPendingPostsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ticket updates in draft state
    /// </summary>
    Task<List<TicketUpdate>> GetDraftsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rejected ticket updates
    /// </summary>
    Task<List<TicketUpdate>> GetRejectedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ticket updates created within a date range
    /// </summary>
    Task<List<TicketUpdate>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new ticket update
    /// </summary>
    Task<TicketUpdate> CreateAsync(TicketUpdate ticketUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing ticket update
    /// </summary>
    Task UpdateAsync(TicketUpdate ticketUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a ticket update
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all ticket updates for a specific ticket
    /// </summary>
    Task DeleteByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a ticket has any approved updates
    /// </summary>
    Task<bool> HasApprovedUpdateAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of ticket updates by approval status for a ticket
    /// </summary>
    Task<Dictionary<string, int>> GetStatusCountsAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the highest version number for a specific ticket
    /// Returns 0 if no updates exist
    /// </summary>
    Task<int> GetLatestVersionNumberAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
