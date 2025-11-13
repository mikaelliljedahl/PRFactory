using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Web.Models;

namespace PRFactory.Web.Services;

/// <summary>
/// Implementation of agent prompt template service.
/// Uses direct repository injection (Blazor Server architecture).
/// This is a facade service that converts between domain entities and DTOs.
/// </summary>
public class AgentPromptService : IAgentPromptService
{
    private readonly ILogger<AgentPromptService> _logger;
    private readonly IAgentPromptTemplateRepository _repository;

    public AgentPromptService(
        ILogger<AgentPromptService> logger,
        IAgentPromptTemplateRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<List<AgentPromptTemplateDto>> GetAllTemplatesAsync(CancellationToken ct = default)
    {
        try
        {
            var systemTemplates = await _repository.GetSystemTemplatesAsync(ct);
            return systemTemplates.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all templates");
            throw;
        }
    }

    public async Task<List<AgentPromptTemplateDto>> GetTemplatesByAgentAsync(string agentName, CancellationToken ct = default)
    {
        try
        {
            var template = await _repository.GetByNameAsync(agentName, tenantId: null, ct);
            List<AgentPromptTemplateDto> result = template != null ? [MapToDto(template)] : [];
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates for agent {AgentName}", agentName);
            throw;
        }
    }

    public async Task<List<AgentPromptTemplateDto>> GetTemplatesByCategoryAsync(string category, Guid? tenantId = null, CancellationToken ct = default)
    {
        try
        {
            var templates = await _repository.GetByCategoryAsync(category, tenantId, ct);
            return templates.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates for category {Category}", category);
            throw;
        }
    }

    public async Task<AgentPromptTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var template = await _repository.GetByIdAsync(id, ct);
            return template != null ? MapToDto(template) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching template {TemplateId}", id);
            throw;
        }
    }

    public async Task<List<AgentPromptTemplateDto>> GetSystemTemplatesAsync(CancellationToken ct = default)
    {
        try
        {
            var templates = await _repository.GetSystemTemplatesAsync(ct);
            return templates.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system templates");
            throw;
        }
    }

    public async Task<List<AgentPromptTemplateDto>> GetTenantTemplatesAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var templates = await _repository.GetTenantTemplatesAsync(tenantId, ct);
            return templates.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<AgentPromptTemplateDto>> GetAvailableTemplatesForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var templates = await _repository.GetAvailableForTenantAsync(tenantId, ct);
            return templates.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<AgentPromptTemplateDto> CreateTemplateAsync(CreatePromptTemplateRequest request, CancellationToken ct = default)
    {
        try
        {
            AgentPromptTemplate template;

            if (request.TenantId.HasValue)
            {
                template = AgentPromptTemplate.CreateTenantTemplate(
                    request.TenantId.Value,
                    request.Name,
                    request.Description,
                    request.PromptContent,
                    request.Category,
                    request.RecommendedModel,
                    request.Color);
            }
            else
            {
                template = AgentPromptTemplate.CreateSystemTemplate(
                    request.Name,
                    request.Description,
                    request.PromptContent,
                    request.Category,
                    request.RecommendedModel,
                    request.Color);
            }

            await _repository.AddAsync(template, ct);
            _logger.LogInformation("Created template {TemplateName} ({TemplateId})", template.Name, template.Id);

            return MapToDto(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template {TemplateName}", request.Name);
            throw;
        }
    }

    public async Task UpdateTemplateAsync(Guid id, UpdatePromptTemplateRequest request, CancellationToken ct = default)
    {
        try
        {
            var template = await _repository.GetByIdAsync(id, ct);
            if (template == null)
            {
                throw new InvalidOperationException($"Template {id} not found");
            }

            template.Update(
                description: request.Description,
                promptContent: request.PromptContent,
                category: request.Category,
                recommendedModel: request.RecommendedModel,
                color: request.Color);

            await _repository.UpdateAsync(template, ct);
            _logger.LogInformation("Updated template {TemplateId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            throw;
        }
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _repository.DeleteAsync(id, ct);
            _logger.LogInformation("Deleted template {TemplateId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            throw;
        }
    }

    public async Task<string> PreviewTemplateAsync(Guid id, Dictionary<string, string>? sampleData = null, CancellationToken ct = default)
    {
        try
        {
            var template = await _repository.GetByIdAsync(id, ct);
            if (template == null)
            {
                throw new InvalidOperationException($"Template {id} not found");
            }

            var content = template.PromptContent;

            // Apply sample data substitution if provided
            if (sampleData != null && sampleData.Any())
            {
                foreach (var kvp in sampleData)
                {
                    content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
                }
            }
            else
            {
                // Use default sample data
                var defaultSampleData = GetDefaultSampleData();
                foreach (var kvp in defaultSampleData)
                {
                    content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
                }
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing template {TemplateId}", id);
            throw;
        }
    }

    public async Task<AgentPromptTemplateDto> CloneTemplateForTenantAsync(Guid templateId, Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var template = await _repository.GetByIdAsync(templateId, ct);
            if (template == null)
            {
                throw new InvalidOperationException($"Template {templateId} not found");
            }

            if (!template.IsSystemTemplate)
            {
                throw new InvalidOperationException("Only system templates can be cloned");
            }

            var clonedTemplate = template.CloneForTenant(tenantId);
            await _repository.AddAsync(clonedTemplate, ct);

            _logger.LogInformation("Cloned template {TemplateId} for tenant {TenantId}", templateId, tenantId);

            return MapToDto(clonedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning template {TemplateId} for tenant {TenantId}", templateId, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Maps an AgentPromptTemplate entity to a DTO
    /// </summary>
    private AgentPromptTemplateDto MapToDto(AgentPromptTemplate template)
    {
        return new AgentPromptTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            PromptContent = template.PromptContent,
            RecommendedModel = template.RecommendedModel,
            Color = template.Color,
            Category = template.Category,
            IsSystemTemplate = template.IsSystemTemplate,
            TenantId = template.TenantId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    /// <summary>
    /// Returns default sample data for template preview
    /// </summary>
    private Dictionary<string, string> GetDefaultSampleData()
    {
        return new Dictionary<string, string>
        {
            { "TicketTitle", "Add user authentication feature" },
            { "TicketDescription", "Implement OAuth2 authentication with support for Google and GitHub providers" },
            { "TicketKey", "PROJ-123" },
            { "RepositoryName", "my-awesome-app" },
            { "RepositoryUrl", "https://github.com/myorg/my-awesome-app" },
            { "BranchName", "feature/oauth-authentication" },
            { "UserName", "John Doe" },
            { "UpdatedTitle", "Implement OAuth2 Authentication" },
            { "UpdatedDescription", "Add OAuth2 authentication with Google and GitHub providers, including secure token storage" },
            { "AcceptanceCriteria", "- Users can sign in with Google\n- Users can sign in with GitHub\n- Tokens are securely stored" },
            { "PlanContent", "1. Set up OAuth2 provider configuration\n2. Implement authentication endpoints\n3. Add secure token storage" }
        };
    }
}
