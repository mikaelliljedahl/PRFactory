using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

/// <summary>
/// Request model for creating a new tenant
/// </summary>
public class CreateTenantRequest : ITenantRequest
{
    [Required(ErrorMessage = "Tenant name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ticket platform URL is required")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string TicketPlatformUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ticket platform API Token is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "API token appears to be invalid")]
    public string? TicketPlatformApiToken { get; set; }

    public string TicketPlatform { get; set; } = "Jira";

    [Required(ErrorMessage = "Claude API Key is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "API key appears to be invalid")]
    public string? ClaudeApiKey { get; set; }

    public bool IsActive { get; set; } = true;

    // Configuration options
    public bool AutoImplementAfterPlanApproval { get; set; } = false;

    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;

    public string ClaudeModel { get; set; } = "claude-sonnet-4-5-20250929";

    [Range(1000, 100000, ErrorMessage = "Max tokens must be between 1,000 and 100,000")]
    public int MaxTokensPerRequest { get; set; } = 8000;

    [Range(30, 600, ErrorMessage = "Timeout must be between 30 and 600 seconds")]
    public int ApiTimeoutSeconds { get; set; } = 300;

    public bool EnableVerboseLogging { get; set; } = false;
    public bool EnableCodeReview { get; set; } = false;
}
