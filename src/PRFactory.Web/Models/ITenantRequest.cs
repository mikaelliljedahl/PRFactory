namespace PRFactory.Web.Models;

/// <summary>
/// Common interface for tenant request models
/// </summary>
public interface ITenantRequest
{
    string Name { get; set; }
    string JiraUrl { get; set; }

    // Nullable to support UpdateTenantRequest where these are optional
    // CreateTenantRequest marks these as [Required] via attributes
    string? JiraApiToken { get; set; }
    string? ClaudeApiKey { get; set; }

    bool IsActive { get; set; }
    bool AutoImplementAfterPlanApproval { get; set; }
    int MaxRetries { get; set; }
    string ClaudeModel { get; set; }
    int MaxTokensPerRequest { get; set; }
    int ApiTimeoutSeconds { get; set; }
    bool EnableVerboseLogging { get; set; }
    bool EnableCodeReview { get; set; }
}
