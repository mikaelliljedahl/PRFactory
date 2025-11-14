using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository for managing plan revision snapshots
/// </summary>
public interface IPlanRevisionRepository
{
    /// <summary>
    /// Gets a revision by its ID
    /// </summary>
    Task<PlanRevision?> GetByIdAsync(Guid revisionId);

    /// <summary>
    /// Gets all revisions for a ticket, ordered by revision number
    /// </summary>
    Task<List<PlanRevision>> GetByTicketIdAsync(Guid ticketId);

    /// <summary>
    /// Gets the latest revision for a ticket
    /// </summary>
    Task<PlanRevision?> GetLatestByTicketIdAsync(Guid ticketId);

    /// <summary>
    /// Gets a specific revision by ticket and revision number
    /// </summary>
    Task<PlanRevision?> GetByRevisionNumberAsync(Guid ticketId, int revisionNumber);

    /// <summary>
    /// Gets the next revision number for a ticket
    /// </summary>
    Task<int> GetNextRevisionNumberAsync(Guid ticketId);

    /// <summary>
    /// Creates a new revision
    /// </summary>
    Task CreateAsync(PlanRevision revision);

    /// <summary>
    /// Deletes all revisions for a ticket (when ticket is deleted)
    /// </summary>
    Task DeleteByTicketIdAsync(Guid ticketId);
}
