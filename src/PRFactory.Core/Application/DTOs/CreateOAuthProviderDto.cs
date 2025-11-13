using System.ComponentModel.DataAnnotations;

namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for creating an OAuth-based LLM provider (Native Anthropic Claude)
/// </summary>
public class CreateOAuthProviderDto
{
    /// <summary>
    /// Display name for this provider configuration
    /// </summary>
    [Required(ErrorMessage = "Provider name is required")]
    [MaxLength(100, ErrorMessage = "Provider name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default model to use for this provider (e.g., "claude-sonnet-4-5-20250929")
    /// </summary>
    [Required(ErrorMessage = "Default model is required")]
    [MaxLength(200, ErrorMessage = "Default model cannot exceed 200 characters")]
    public string DefaultModel { get; set; } = "claude-sonnet-4-5-20250929";
}
