using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Service for managing agent prompt templates
/// </summary>
public interface IAgentPromptService
{
    /// <summary>
    /// Gets all templates
    /// </summary>
    Task<List<AgentPromptTemplateDto>> GetAllTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets templates for a specific agent name
    /// </summary>
    Task<List<AgentPromptTemplateDto>> GetTemplatesByAgentAsync(string agentName, CancellationToken ct = default);

    /// <summary>
    /// Gets templates by category
    /// </summary>
    Task<List<AgentPromptTemplateDto>> GetTemplatesByCategoryAsync(string category, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    Task<AgentPromptTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all system templates
    /// </summary>
    Task<List<AgentPromptTemplateDto>> GetSystemTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all templates for a specific tenant
    /// </summary>
    Task<List<AgentPromptTemplateDto>> GetTenantTemplatesAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets all templates available to a tenant (system + tenant-specific)
    /// </summary>
    Task<List<AgentPromptTemplateDto>> GetAvailableTemplatesForTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new template
    /// </summary>
    Task<AgentPromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    Task UpdateTemplateAsync(Guid id, UpdatePromptTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a template
    /// </summary>
    Task DeleteTemplateAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Previews a template with sample data
    /// </summary>
    Task<string> PreviewTemplateAsync(Guid id, Dictionary<string, string>? sampleData = null, CancellationToken ct = default);

    /// <summary>
    /// Clones a system template for a specific tenant
    /// </summary>
    Task<AgentPromptTemplateDto> CloneTemplateForTenantAsync(Guid templateId, Guid tenantId, CancellationToken ct = default);
}
