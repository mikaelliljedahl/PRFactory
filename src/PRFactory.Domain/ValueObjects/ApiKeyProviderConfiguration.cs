using PRFactory.Domain.Entities;

namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Configuration parameters for creating an API key-based LLM provider
/// </summary>
public class ApiKeyProviderConfiguration
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Display name for the provider
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Provider type identifier
    /// </summary>
    public LlmProviderType ProviderType { get; }

    /// <summary>
    /// API base URL for the provider
    /// </summary>
    public string ApiBaseUrl { get; }

    /// <summary>
    /// Encrypted API token/key
    /// </summary>
    public string EncryptedApiToken { get; }

    /// <summary>
    /// Default model to use
    /// </summary>
    public string DefaultModel { get; }

    /// <summary>
    /// Timeout in milliseconds (default: 300000)
    /// </summary>
    public int TimeoutMs { get; }

    /// <summary>
    /// Whether to disable non-essential traffic
    /// </summary>
    public bool DisableNonEssentialTraffic { get; }

    /// <summary>
    /// Model overrides for different tiers
    /// </summary>
    public Dictionary<string, string>? ModelOverrides { get; }

    public ApiKeyProviderConfiguration(
        Guid tenantId,
        string name,
        LlmProviderType providerType,
        string apiBaseUrl,
        string encryptedApiToken,
        string defaultModel,
        int timeoutMs = 300000,
        bool disableNonEssentialTraffic = false,
        Dictionary<string, string>? modelOverrides = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new ArgumentException("API base URL cannot be empty", nameof(apiBaseUrl));

        if (string.IsNullOrWhiteSpace(encryptedApiToken))
            throw new ArgumentException("API token cannot be empty", nameof(encryptedApiToken));

        if (string.IsNullOrWhiteSpace(defaultModel))
            throw new ArgumentException("Default model cannot be empty", nameof(defaultModel));

        if (timeoutMs <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(timeoutMs));

        TenantId = tenantId;
        Name = name;
        ProviderType = providerType;
        ApiBaseUrl = apiBaseUrl;
        EncryptedApiToken = encryptedApiToken;
        DefaultModel = defaultModel;
        TimeoutMs = timeoutMs;
        DisableNonEssentialTraffic = disableNonEssentialTraffic;
        ModelOverrides = modelOverrides;
    }
}
