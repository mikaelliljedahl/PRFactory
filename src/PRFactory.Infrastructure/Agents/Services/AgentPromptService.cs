using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Agents.Services;

/// <summary>
/// Service implementation for retrieving agent prompts for use in workflows
/// </summary>
public class AgentPromptService : IAgentPromptService
{
    private readonly IAgentPromptTemplateRepository _repository;
    private readonly ILogger<AgentPromptService> _logger;

    public AgentPromptService(
        IAgentPromptTemplateRepository repository,
        ILogger<AgentPromptService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentPromptTemplate?> GetPromptTemplateAsync(
        string name,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var template = await _repository.GetByNameAsync(name, tenantId, ct);

        if (template == null)
        {
            _logger.LogWarning("Prompt template not found: {TemplateName} (TenantId: {TenantId})", name, tenantId);
        }

        return template;
    }

    public async Task<List<AgentPromptTemplate>> GetPromptTemplatesByCategoryAsync(
        string category,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        return await _repository.GetByCategoryAsync(category, tenantId, ct);
    }

    public async Task<string?> GetPromptContentAsync(
        string name,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var template = await GetPromptTemplateAsync(name, tenantId, ct);
        return template?.PromptContent;
    }

    public async Task<List<AgentPromptTemplate>> GetAvailableTemplatesAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _repository.GetAvailableForTenantAsync(tenantId, ct);
    }
}
