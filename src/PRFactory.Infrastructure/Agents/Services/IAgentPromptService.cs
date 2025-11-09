using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Agents.Services;

/// <summary>
/// Service interface for retrieving agent prompts for use in workflows
/// </summary>
public interface IAgentPromptService
{
    /// <summary>
    /// Gets a prompt template by name for a specific tenant (with fallback to system template)
    /// </summary>
    Task<AgentPromptTemplate?> GetPromptTemplateAsync(string name, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets all prompt templates for a specific category and tenant
    /// </summary>
    Task<List<AgentPromptTemplate>> GetPromptTemplatesByCategoryAsync(string category, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the prompt content for a template by name
    /// </summary>
    Task<string?> GetPromptContentAsync(string name, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets all available templates for a tenant
    /// </summary>
    Task<List<AgentPromptTemplate>> GetAvailableTemplatesAsync(Guid tenantId, CancellationToken ct = default);
}
