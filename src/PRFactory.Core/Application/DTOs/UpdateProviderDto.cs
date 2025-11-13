using System.ComponentModel.DataAnnotations;

namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for updating an existing LLM provider
/// </summary>
public class UpdateProviderDto
{
    /// <summary>
    /// Display name for this provider configuration
    /// </summary>
    [Required(ErrorMessage = "Provider name is required")]
    [MaxLength(100, ErrorMessage = "Provider name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base URL override for the provider API (null for native Anthropic)
    /// </summary>
    [Url(ErrorMessage = "API base URL must be a valid URL")]
    [MaxLength(500, ErrorMessage = "API base URL cannot exceed 500 characters")]
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// Default model to use for this provider
    /// </summary>
    [Required(ErrorMessage = "Default model is required")]
    [MaxLength(200, ErrorMessage = "Default model cannot exceed 200 characters")]
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in milliseconds for API requests
    /// </summary>
    [Range(1000, 600000, ErrorMessage = "Timeout must be between 1 second and 10 minutes")]
    public int TimeoutMs { get; set; }

    /// <summary>
    /// Whether to disable non-essential traffic (useful for proxy providers)
    /// </summary>
    public bool DisableNonEssentialTraffic { get; set; }

    /// <summary>
    /// Model overrides for different model tiers (Minimax M2 specific)
    /// </summary>
    public Dictionary<string, string>? ModelOverrides { get; set; }

    /// <summary>
    /// Whether this provider configuration is active
    /// </summary>
    public bool IsActive { get; set; }
}
