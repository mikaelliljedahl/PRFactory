using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.Blazor.TestDataBuilders;

/// <summary>
/// Builder for creating AgentPromptTemplateDto instances for testing
/// </summary>
public class AgentPromptTemplateDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "TestPrompt";
    private string _description = "Test prompt description";
    private string _promptContent = "This is a test prompt with {{variable}}";
    private string _category = "General";
    private bool _isSystemTemplate = false;
    private Guid? _tenantId = null;
    private string? _recommendedModel = "sonnet";
    private string? _color = "#007bff";
    private readonly DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = DateTime.UtcNow;

    public AgentPromptTemplateDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithPromptContent(string promptContent)
    {
        _promptContent = promptContent;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public AgentPromptTemplateDtoBuilder AsSystemTemplate(bool isSystem = true)
    {
        _isSystemTemplate = isSystem;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithTenantId(Guid? tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithRecommendedModel(string? model)
    {
        _recommendedModel = model;
        return this;
    }

    public AgentPromptTemplateDtoBuilder WithColor(string? color)
    {
        _color = color;
        return this;
    }

    public AgentPromptTemplateDto Build()
    {
        return new AgentPromptTemplateDto
        {
            Id = _id,
            Name = _name,
            Description = _description,
            PromptContent = _promptContent,
            Category = _category,
            IsSystemTemplate = _isSystemTemplate,
            TenantId = _tenantId,
            RecommendedModel = _recommendedModel,
            Color = _color,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt
        };
    }
}
