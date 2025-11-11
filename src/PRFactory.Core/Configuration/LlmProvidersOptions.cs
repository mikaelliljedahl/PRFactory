using System.Collections.Generic;

namespace PRFactory.Core.Configuration;

/// <summary>
/// Configuration options for LLM providers
/// </summary>
public class LlmProvidersOptions
{
    /// <summary>
    /// Default provider to use when none is specified
    /// </summary>
    public string DefaultProvider { get; set; } = "anthropic";

    /// <summary>
    /// Fallback provider to use if primary provider fails
    /// </summary>
    public string? FallbackProvider { get; set; }

    /// <summary>
    /// Whether automatic failover to fallback provider is enabled
    /// </summary>
    public bool FailoverEnabled { get; set; }

    /// <summary>
    /// Provider-specific configuration options
    /// </summary>
    public Dictionary<string, ProviderOptions> Providers { get; set; } = new();
}

/// <summary>
/// Configuration options for a specific LLM provider
/// </summary>
public class ProviderOptions
{
    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Path to the CLI executable
    /// </summary>
    public string CliPath { get; set; } = string.Empty;

    /// <summary>
    /// Default model to use for this provider
    /// </summary>
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Default timeout in seconds for requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Default maximum tokens for responses
    /// </summary>
    public int MaxTokens { get; set; } = 8000;
}
