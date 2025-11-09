using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

/// <summary>
/// Request model for updating an existing tenant
/// </summary>
public class UpdateTenantRequest : ITenantRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tenant name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jira URL is required")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string JiraUrl { get; set; } = string.Empty;

    // Optional - only update if provided
    [StringLength(500, MinimumLength = 10, ErrorMessage = "API token appears to be invalid")]
    public string? JiraApiToken { get; set; }

    // Optional - only update if provided
    [StringLength(500, MinimumLength = 10, ErrorMessage = "API key appears to be invalid")]
    public string? ClaudeApiKey { get; set; }

    public bool IsActive { get; set; }

    // Configuration options
    public bool AutoImplementAfterPlanApproval { get; set; }

    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; }

    public string ClaudeModel { get; set; } = "claude-sonnet-4-5-20250929";

    [Range(1000, 100000, ErrorMessage = "Max tokens must be between 1,000 and 100,000")]
    public int MaxTokensPerRequest { get; set; }

    [Range(30, 600, ErrorMessage = "Timeout must be between 30 and 600 seconds")]
    public int ApiTimeoutSeconds { get; set; }

    public bool EnableVerboseLogging { get; set; }
    public bool EnableCodeReview { get; set; }

    public string[] AllowedRepositories { get; set; } = Array.Empty<string>();
}
