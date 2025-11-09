using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

/// <summary>
/// Request model for updating an existing agent prompt template
/// </summary>
public class UpdatePromptTemplateRequest
{
    /// <summary>
    /// Human-readable description of what this agent does
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// The full prompt content (including POML markup if present)
    /// </summary>
    public string? PromptContent { get; set; }

    /// <summary>
    /// Category/stage this prompt is designed for
    /// </summary>
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

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
}
