using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

/// <summary>
/// Request model for creating a new agent prompt template
/// </summary>
public class CreatePromptTemplateRequest
{
    /// <summary>
    /// Name/identifier of the agent (e.g., "code-implementation-specialist")
    /// </summary>
    [Required(ErrorMessage = "Agent name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this agent does
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The full prompt content (including POML markup if present)
    /// </summary>
    [Required(ErrorMessage = "Prompt content is required")]
    public string PromptContent { get; set; } = string.Empty;

    /// <summary>
    /// Category/stage this prompt is designed for
    /// </summary>
    [Required(ErrorMessage = "Category is required")]
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Recommended Claude model for this agent
    /// </summary>
    [StringLength(50, ErrorMessage = "Model name cannot exceed 50 characters")]
    public string? RecommendedModel { get; set; }

    /// <summary>
    /// UI color for this agent (hex color code)
    /// </summary>
    [StringLength(20, ErrorMessage = "Color cannot exceed 20 characters")]
    public string? Color { get; set; }

    /// <summary>
    /// The tenant ID (null for system templates)
    /// </summary>
    public Guid? TenantId { get; set; }
}
