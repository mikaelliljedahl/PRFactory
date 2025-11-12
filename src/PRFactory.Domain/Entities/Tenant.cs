namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a tenant (customer) in the multi-tenant PRFactory system.
/// Each tenant has their own Jira instance, repositories, and Claude API key.
/// </summary>
public class Tenant
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Display name for the tenant
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Identity provider used for authentication (AzureAD, GoogleWorkspace, Personal)
    /// </summary>
    public string IdentityProvider { get; private set; } = string.Empty;

    /// <summary>
    /// External tenant identifier from the identity provider (Azure AD tenant ID or Google Workspace domain)
    /// </summary>
    public string ExternalTenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Ticket platform being used (Jira, AzureDevOps, GitHub, GitLab)
    /// </summary>
    public string TicketPlatform { get; private set; } = "Jira"; // Default to Jira for backward compatibility

    /// <summary>
    /// Base URL for the tenant's ticket system (e.g., https://company.atlassian.net for Jira)
    /// </summary>
    public string TicketPlatformUrl { get; private set; } = string.Empty;

    /// <summary>
    /// API token for accessing the ticket platform (should be encrypted at rest)
    /// </summary>
    public string TicketPlatformApiToken { get; private set; } = string.Empty;

    /// <summary>
    /// Claude API key for this tenant (should be encrypted at rest)
    /// </summary>
    public string ClaudeApiKey { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this tenant is active and can process tickets
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// When the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the tenant was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Tenant-specific configuration settings
    /// </summary>
    public TenantConfiguration Configuration { get; private set; } = new();

    /// <summary>
    /// Repositories associated with this tenant
    /// </summary>
    public List<Repository> Repositories { get; private set; } = new();

    /// <summary>
    /// Tickets processed for this tenant
    /// </summary>
    public List<Ticket> Tickets { get; private set; } = new();

    /// <summary>
    /// LLM provider configurations for this tenant
    /// </summary>
    public List<TenantLlmProvider> LlmProviders { get; private set; } = new();

    private Tenant() { }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    public static Tenant Create(
        string name,
        string identityProvider,
        string externalTenantId,
        string ticketPlatformUrl,
        string ticketPlatformApiToken,
        string claudeApiKey,
        string ticketPlatform = "Jira")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(identityProvider))
            throw new ArgumentException("Identity provider cannot be empty", nameof(identityProvider));

        if (string.IsNullOrWhiteSpace(externalTenantId))
            throw new ArgumentException("External tenant ID cannot be empty", nameof(externalTenantId));

        if (string.IsNullOrWhiteSpace(ticketPlatformUrl))
            throw new ArgumentException("Ticket platform URL cannot be empty", nameof(ticketPlatformUrl));

        if (string.IsNullOrWhiteSpace(ticketPlatformApiToken))
            throw new ArgumentException("Ticket platform API token cannot be empty", nameof(ticketPlatformApiToken));

        if (string.IsNullOrWhiteSpace(claudeApiKey))
            throw new ArgumentException("Claude API key cannot be empty", nameof(claudeApiKey));

        if (string.IsNullOrWhiteSpace(ticketPlatform))
            throw new ArgumentException("Ticket platform cannot be empty", nameof(ticketPlatform));

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            IdentityProvider = identityProvider,
            ExternalTenantId = externalTenantId,
            TicketPlatform = ticketPlatform,
            TicketPlatformUrl = ticketPlatformUrl.TrimEnd('/'),
            TicketPlatformApiToken = ticketPlatformApiToken,
            ClaudeApiKey = claudeApiKey,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates tenant credentials
    /// </summary>
    public void UpdateCredentials(string? ticketPlatformApiToken = null, string? claudeApiKey = null)
    {
        if (!string.IsNullOrWhiteSpace(ticketPlatformApiToken))
            TicketPlatformApiToken = ticketPlatformApiToken;

        if (!string.IsNullOrWhiteSpace(claudeApiKey))
            ClaudeApiKey = claudeApiKey;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates tenant platform settings
    /// </summary>
    public void UpdatePlatformSettings(string? ticketPlatform = null, string? ticketPlatformUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(ticketPlatform))
            TicketPlatform = ticketPlatform;

        if (!string.IsNullOrWhiteSpace(ticketPlatformUrl))
            TicketPlatformUrl = ticketPlatformUrl.TrimEnd('/');

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates tenant configuration
    /// </summary>
    public void UpdateConfiguration(TenantConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the tenant
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the tenant (stops processing new tickets)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an LLM provider to this tenant
    /// </summary>
    public void AddLlmProvider(TenantLlmProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        if (provider.TenantId != Id)
            throw new ArgumentException($"Provider belongs to different tenant (Provider: {provider.TenantId}, Tenant: {Id})", nameof(provider));

        if (LlmProviders.Any(p => p.Id == provider.Id))
            throw new InvalidOperationException($"Provider {provider.Id} already exists for tenant {Id}");

        LlmProviders.Add(provider);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an LLM provider from this tenant
    /// </summary>
    public void RemoveLlmProvider(Guid providerId)
    {
        var provider = LlmProviders.FirstOrDefault(p => p.Id == providerId);
        if (provider == null)
            throw new InvalidOperationException($"Provider {providerId} not found for tenant {Id}");

        LlmProviders.Remove(provider);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the default LLM provider for this tenant
    /// </summary>
    public TenantLlmProvider? GetDefaultLlmProvider()
    {
        return LlmProviders.FirstOrDefault(p => p.IsDefault && p.IsActive);
    }

    /// <summary>
    /// Sets a provider as the default for this tenant (clears other defaults)
    /// </summary>
    public void SetDefaultLlmProvider(Guid providerId)
    {
        var provider = LlmProviders.FirstOrDefault(p => p.Id == providerId);
        if (provider == null)
            throw new InvalidOperationException($"Provider {providerId} not found for tenant {Id}");

        // Clear existing default
        foreach (var p in LlmProviders.Where(p => p.IsDefault))
        {
            p.RemoveAsDefault();
        }

        // Set new default
        provider.SetAsDefault();
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Configuration settings for a tenant
/// </summary>
public class TenantConfiguration
{
    /// <summary>
    /// Whether to automatically start implementation after plan approval
    /// </summary>
    public bool AutoImplementAfterPlanApproval { get; set; } = false;

    /// <summary>
    /// Maximum number of retries for failed operations
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Claude model to use for this tenant
    /// </summary>
    public string ClaudeModel { get; set; } = "claude-sonnet-4-5-20250929";

    /// <summary>
    /// Maximum tokens per Claude API request
    /// </summary>
    public int MaxTokensPerRequest { get; set; } = 8000;

    /// <summary>
    /// Timeout in seconds for Claude API requests
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to enable verbose logging for this tenant
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable code review workflow for this tenant (DEPRECATED - use EnableAutoCodeReview)
    /// </summary>
    public bool EnableCodeReview { get; set; } = false;

    /// <summary>
    /// List of allowed repository names for this tenant (empty means all allowed)
    /// </summary>
    public string[] AllowedRepositories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Custom prompt templates for this tenant (optional)
    /// </summary>
    public Dictionary<string, string> CustomPromptTemplates { get; set; } = new();

    // --- Multi-LLM Configuration (Epic 02) ---

    /// <summary>
    /// Whether to automatically trigger code review after PR creation
    /// </summary>
    public bool EnableAutoCodeReview { get; set; } = false;

    /// <summary>
    /// LLM provider ID to use for code review (null = use tenant default)
    /// </summary>
    public Guid? CodeReviewLlmProviderId { get; set; }

    /// <summary>
    /// LLM provider ID to use for code implementation (null = use tenant default)
    /// </summary>
    public Guid? ImplementationLlmProviderId { get; set; }

    /// <summary>
    /// LLM provider ID to use for planning (null = use tenant default)
    /// </summary>
    public Guid? PlanningLlmProviderId { get; set; }

    /// <summary>
    /// LLM provider ID to use for ticket analysis and refinement (null = use tenant default)
    /// </summary>
    public Guid? AnalysisLlmProviderId { get; set; }

    /// <summary>
    /// Maximum number of code review iterations before completing with warnings (default: 3)
    /// </summary>
    public int MaxCodeReviewIterations { get; set; } = 3;

    /// <summary>
    /// Whether to automatically post approval comment if code review finds no issues
    /// </summary>
    public bool AutoApproveIfNoIssues { get; set; } = false;

    /// <summary>
    /// Whether to require human approval after code review before merging (future use)
    /// </summary>
    public bool RequireHumanApprovalAfterReview { get; set; } = true;
}
