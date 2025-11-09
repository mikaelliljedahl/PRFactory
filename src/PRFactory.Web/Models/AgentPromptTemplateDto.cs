namespace PRFactory.Web.Models;

/// <summary>
/// Data transfer object for displaying agent prompt template information in the UI
/// </summary>
public class AgentPromptTemplateDto
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name/identifier of the agent (e.g., "code-implementation-specialist")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this agent does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The full prompt content (including POML markup if present)
    /// </summary>
    public string PromptContent { get; set; } = string.Empty;

    /// <summary>
    /// Recommended Claude model for this agent (e.g., "sonnet", "opus", "haiku")
    /// </summary>
    public string? RecommendedModel { get; set; }

    /// <summary>
    /// UI color for this agent (for visual identification)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Category/stage this prompt is designed for (e.g., "Implementation", "Planning", "Analysis", "Testing")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a system-provided template (read-only) or user-created
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// The tenant this template belongs to (null for system templates available to all)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the template was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Badge class for displaying template type
    /// </summary>
    public string TypeBadgeClass => IsSystemTemplate ? "badge bg-info" : "badge bg-success";

    /// <summary>
    /// Display text for template type
    /// </summary>
    public string TypeDisplay => IsSystemTemplate ? "System Template" : "Custom Template";

    /// <summary>
    /// Badge class for category
    /// </summary>
    public string CategoryBadgeClass => Category switch
    {
        "Implementation" => "badge bg-primary",
        "Planning" => "badge bg-info",
        "Analysis" => "badge bg-warning",
        "Testing" => "badge bg-success",
        "Review" => "badge bg-secondary",
        _ => "badge bg-light text-dark"
    };
}
