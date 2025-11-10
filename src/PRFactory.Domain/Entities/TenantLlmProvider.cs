namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an LLM provider configuration for a tenant.
/// Supports multiple providers (Anthropic Claude, Z.ai/GLM, Minimax M2, etc.)
/// via Claude Code CLI settings.json configuration.
/// </summary>
public class TenantLlmProvider
{
    /// <summary>
    /// Unique identifier for this provider configuration
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The tenant this configuration belongs to
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Display name for this provider configuration (e.g., "Production Claude", "Dev Minimax")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Provider type identifier
    /// </summary>
    public LlmProviderType ProviderType { get; private set; }

    /// <summary>
    /// Whether this provider uses OAuth (Anthropic native) or API key
    /// </summary>
    public bool UsesOAuth { get; private set; }

    /// <summary>
    /// Encrypted API token/key for this provider
    /// For OAuth providers, this stores the access token after OAuth flow completes
    /// </summary>
    public string? EncryptedApiToken { get; private set; }

    /// <summary>
    /// Base URL override for the provider API
    /// Null for native Anthropic (uses default https://api.anthropic.com)
    /// </summary>
    public string? ApiBaseUrl { get; private set; }

    /// <summary>
    /// Timeout in milliseconds for API requests
    /// Default: 300000 (5 minutes)
    /// </summary>
    public int TimeoutMs { get; private set; } = 300000;

    /// <summary>
    /// Default model to use for this provider
    /// Examples: "claude-sonnet-4-5-20250929", "MiniMax-M2", "gpt-4o"
    /// </summary>
    public string DefaultModel { get; private set; } = string.Empty;

    /// <summary>
    /// Whether to disable non-essential traffic (useful for proxy providers)
    /// </summary>
    public bool DisableNonEssentialTraffic { get; private set; }

    /// <summary>
    /// Model overrides for different model tiers (Minimax M2 specific)
    /// Stored as JSON: { "small_fast_model": "MiniMax-M2", "default_sonnet_model": "MiniMax-M2", ... }
    /// </summary>
    public Dictionary<string, string>? ModelOverrides { get; private set; }

    /// <summary>
    /// Whether this provider configuration is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Whether this is the default provider for this tenant
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// When OAuth token was last refreshed (for OAuth providers)
    /// </summary>
    public DateTime? OAuthTokenRefreshedAt { get; private set; }

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    public Tenant? Tenant { get; private set; }

    private TenantLlmProvider() { }

    /// <summary>
    /// Creates a new API key-based provider configuration (Z.ai, Minimax, OpenRouter, etc.)
    /// </summary>
    public static TenantLlmProvider CreateApiKeyProvider(
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

        return new TenantLlmProvider
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ProviderType = providerType,
            UsesOAuth = false,
            EncryptedApiToken = encryptedApiToken,
            ApiBaseUrl = apiBaseUrl,
            TimeoutMs = timeoutMs,
            DefaultModel = defaultModel,
            DisableNonEssentialTraffic = disableNonEssentialTraffic,
            ModelOverrides = modelOverrides,
            IsActive = true,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new OAuth-based provider configuration (Native Anthropic Claude)
    /// </summary>
    public static TenantLlmProvider CreateOAuthProvider(
        Guid tenantId,
        string name,
        string defaultModel = "claude-sonnet-4-5-20250929")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new TenantLlmProvider
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ProviderType = LlmProviderType.AnthropicNative,
            UsesOAuth = true,
            EncryptedApiToken = null, // Will be set after OAuth flow
            ApiBaseUrl = null, // Uses default Anthropic URL
            TimeoutMs = 300000,
            DefaultModel = defaultModel,
            DisableNonEssentialTraffic = false,
            IsActive = true,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the API token (for API key providers or after OAuth completes)
    /// </summary>
    public void UpdateApiToken(string encryptedApiToken, bool isOAuthToken = false)
    {
        if (string.IsNullOrWhiteSpace(encryptedApiToken))
            throw new ArgumentException("API token cannot be empty", nameof(encryptedApiToken));

        EncryptedApiToken = encryptedApiToken;
        UpdatedAt = DateTime.UtcNow;

        if (isOAuthToken)
        {
            OAuthTokenRefreshedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Updates provider configuration
    /// </summary>
    public void UpdateConfiguration(
        string? name = null,
        string? apiBaseUrl = null,
        string? defaultModel = null,
        int? timeoutMs = null,
        bool? disableNonEssentialTraffic = null,
        Dictionary<string, string>? modelOverrides = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        if (apiBaseUrl != null)
            ApiBaseUrl = apiBaseUrl;

        if (!string.IsNullOrWhiteSpace(defaultModel))
            DefaultModel = defaultModel;

        if (timeoutMs.HasValue)
            TimeoutMs = timeoutMs.Value;

        if (disableNonEssentialTraffic.HasValue)
            DisableNonEssentialTraffic = disableNonEssentialTraffic.Value;

        if (modelOverrides != null)
            ModelOverrides = modelOverrides;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this provider as the default for the tenant
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes default status from this provider
    /// </summary>
    public void RemoveAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates this provider
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this provider (tasks cannot use this provider)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates the Claude Code CLI settings.json content for this provider
    /// </summary>
    /// <param name="decryptedToken">The decrypted API token</param>
    /// <returns>Dictionary representing the settings.json env section</returns>
    public Dictionary<string, object> GenerateClaudeSettingsEnv(string decryptedToken)
    {
        var env = new Dictionary<string, object>();

        // Always set auth token
        env["ANTHROPIC_AUTH_TOKEN"] = decryptedToken;

        // Set base URL if overridden
        if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            env["ANTHROPIC_BASE_URL"] = ApiBaseUrl;
        }

        // Set timeout
        env["API_TIMEOUT_MS"] = TimeoutMs.ToString();

        // Disable non-essential traffic if requested
        if (DisableNonEssentialTraffic)
        {
            env["CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC"] = "1";
        }

        // Add model overrides if present (Minimax M2)
        if (ModelOverrides != null)
        {
            foreach (var (key, value) in ModelOverrides)
            {
                env[key] = value;
            }
        }

        return env;
    }
}

/// <summary>
/// Supported LLM provider types
/// </summary>
public enum LlmProviderType
{
    /// <summary>
    /// Native Anthropic Claude API with OAuth authentication
    /// </summary>
    AnthropicNative = 1,

    /// <summary>
    /// Z.ai unified API (supports Claude, GPT-4, Gemini via single API)
    /// </summary>
    ZAi = 2,

    /// <summary>
    /// Minimax M2 model via Anthropic-compatible API
    /// </summary>
    MinimaxM2 = 3,

    /// <summary>
    /// OpenRouter (100+ models via single API)
    /// </summary>
    OpenRouter = 4,

    /// <summary>
    /// Together AI (fast inference for open models)
    /// </summary>
    TogetherAI = 5,

    /// <summary>
    /// Custom provider with user-specified base URL
    /// </summary>
    Custom = 99
}
