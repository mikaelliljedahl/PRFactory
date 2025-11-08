using PRFactory.Domain.Entities;

namespace PRFactory.Domain.Interfaces;

/// <summary>
/// Repository interface for managing AgentPromptTemplate entities.
/// </summary>
public interface IAgentPromptTemplateRepository
{
    /// <summary>
    /// Gets a template by its ID
    /// </summary>
    Task<AgentPromptTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a template by name (system templates or tenant-specific)
    /// </summary>
    Task<AgentPromptTemplate?> GetByNameAsync(string name, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets all templates available to a specific tenant (system templates + tenant-specific)
    /// </summary>
    Task<List<AgentPromptTemplate>> GetAvailableForTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets all templates in a specific category for a tenant
    /// </summary>
    Task<List<AgentPromptTemplate>> GetByCategoryAsync(string category, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets all system templates
    /// </summary>
    Task<List<AgentPromptTemplate>> GetSystemTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all templates for a specific tenant
    /// </summary>
    Task<List<AgentPromptTemplate>> GetTenantTemplatesAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new template
    /// </summary>
    Task AddAsync(AgentPromptTemplate template, CancellationToken ct = default);

    /// <summary>
    /// Adds multiple templates in bulk
    /// </summary>
    Task AddRangeAsync(IEnumerable<AgentPromptTemplate> templates, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    Task UpdateAsync(AgentPromptTemplate template, CancellationToken ct = default);

    /// <summary>
    /// Deletes a template (only allowed for non-system templates)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a template with the given name exists for a tenant
    /// </summary>
    Task<bool> ExistsAsync(string name, Guid? tenantId = null, CancellationToken ct = default);
}
