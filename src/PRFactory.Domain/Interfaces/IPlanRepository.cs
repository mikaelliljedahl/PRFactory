using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for Plan entity operations
/// </summary>
public interface IPlanRepository
{
    /// <summary>
    /// Gets a plan by its unique identifier
    /// </summary>
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a plan by ticket ID
    /// </summary>
    Task<Plan?> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets version history for a plan
    /// </summary>
    Task<List<PlanVersion>> GetVersionHistoryAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a plan
    /// </summary>
    Task<PlanVersion?> GetVersionAsync(Guid planId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new plan
    /// </summary>
    Task<Plan> AddAsync(Plan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing plan
    /// </summary>
    Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a plan
    /// </summary>
    Task DeleteAsync(Plan plan, CancellationToken cancellationToken = default);
}
