using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for AgentConfiguration entity operations.
/// Provides CRUD operations and tenant-specific queries for agent configurations.
/// </summary>
public interface IAgentConfigurationRepository
{
    /// <summary>
    /// Gets an agent configuration by tenant ID and agent name.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="agentName">The agent name (e.g., "AnalysisAgent")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent configuration if found, null otherwise</returns>
    Task<AgentConfiguration?> GetByTenantAndNameAsync(
        Guid tenantId,
        string agentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agent configurations for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of agent configurations for the tenant</returns>
    Task<List<AgentConfiguration>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new agent configuration.
    /// </summary>
    /// <param name="configuration">The agent configuration to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created agent configuration with generated ID</returns>
    Task<AgentConfiguration> CreateAsync(
        AgentConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing agent configuration.
    /// </summary>
    /// <param name="configuration">The agent configuration to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(
        AgentConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent configuration.
    /// </summary>
    /// <param name="id">The agent configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
