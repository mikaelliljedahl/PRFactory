namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for displaying LLM provider information
/// </summary>
public class TenantLlmProviderDto
{
    /// <summary>
    /// Unique identifier for this provider configuration
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant this configuration belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Display name for this provider configuration (e.g., "Production Claude", "Dev Minimax")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider type identifier (AnthropicNative, ZAi, MinimaxM2, etc.)
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider uses OAuth (Anthropic native) or API key
    /// </summary>
    public bool UsesOAuth { get; set; }

    /// <summary>
    /// Base URL override for the provider API (null for native Anthropic)
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// Default model to use for this provider (e.g., "claude-sonnet-4-5-20250929")
    /// </summary>
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in milliseconds for API requests
    /// </summary>
    public int TimeoutMs { get; set; }

    /// <summary>
    /// Whether this is the default provider for this tenant
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this provider configuration is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
