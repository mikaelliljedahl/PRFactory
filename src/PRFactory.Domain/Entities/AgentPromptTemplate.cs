namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a reusable AI agent prompt template that can be used in different workflow stages.
/// Templates can be loaded from the .claude/agents folder and customized per tenant.
/// </summary>
public class AgentPromptTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Name/identifier of the agent (e.g., "code-implementation-specialist")
    /// </summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this agent does
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The full prompt content (including POML markup if present)
    /// </summary>
    public string PromptContent { get; private set; } = string.Empty;

    /// <summary>
    /// Recommended Claude model for this agent (e.g., "sonnet", "opus", "haiku")
    /// </summary>
    public string? RecommendedModel { get; private set; }

    /// <summary>
    /// UI color for this agent (for visual identification)
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Category/stage this prompt is designed for (e.g., "Implementation", "Planning", "Analysis", "Testing")
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this is a system-provided template (read-only) or user-created
    /// </summary>
    public bool IsSystemTemplate { get; private init; }

    /// <summary>
    /// The tenant this template belongs to (null for system templates available to all)
    /// </summary>
    public Guid? TenantId { get; private init; }

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; private init; }

    /// <summary>
    /// When the template was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    public Tenant? Tenant { get; private init; }

    private AgentPromptTemplate() { }

    /// <summary>
    /// Creates a new system template (available to all tenants)
    /// </summary>
    public static AgentPromptTemplate CreateSystemTemplate(
        string name,
        string description,
        string promptContent,
        string category,
        string? recommendedModel = null,
        string? color = null)
    {
        ValidateInputs(name, description, promptContent, category);

        return new AgentPromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            PromptContent = promptContent,
            Category = category,
            RecommendedModel = recommendedModel,
            Color = color,
            IsSystemTemplate = true,
            TenantId = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new tenant-specific template
    /// </summary>
    public static AgentPromptTemplate CreateTenantTemplate(
        Guid tenantId,
        string name,
        string description,
        string promptContent,
        string category,
        string? recommendedModel = null,
        string? color = null)
    {
        ValidateInputs(name, description, promptContent, category);

        return new AgentPromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            PromptContent = promptContent,
            Category = category,
            RecommendedModel = recommendedModel,
            Color = color,
            IsSystemTemplate = false,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the template content and metadata
    /// </summary>
    public void Update(
        string? description = null,
        string? promptContent = null,
        string? category = null,
        string? recommendedModel = null,
        string? color = null)
    {
        if (IsSystemTemplate)
        {
            throw new InvalidOperationException("System templates cannot be modified");
        }

        if (!string.IsNullOrWhiteSpace(description))
            Description = description;

        if (!string.IsNullOrWhiteSpace(promptContent))
            PromptContent = promptContent;

        if (!string.IsNullOrWhiteSpace(category))
            Category = category;

        if (recommendedModel != null)
            RecommendedModel = recommendedModel;

        if (color != null)
            Color = color;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a tenant-specific copy of a system template
    /// </summary>
    public AgentPromptTemplate CloneForTenant(Guid tenantId)
    {
        return new AgentPromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Description = Description,
            PromptContent = PromptContent,
            Category = Category,
            RecommendedModel = RecommendedModel,
            Color = Color,
            IsSystemTemplate = false,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void ValidateInputs(string name, string description, string promptContent, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Template description cannot be empty", nameof(description));

        if (string.IsNullOrWhiteSpace(promptContent))
            throw new ArgumentException("Prompt content cannot be empty", nameof(promptContent));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));
    }
}
