using System;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO for displaying tenant information
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TicketPlatformUrl { get; set; } = string.Empty;
    public string TicketPlatform { get; set; } = "Jira";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Configuration summary
    public bool AutoImplementAfterPlanApproval { get; set; }
    public int MaxRetries { get; set; }
    public string ClaudeModel { get; set; } = string.Empty;
    public int MaxTokensPerRequest { get; set; }
    public bool EnableCodeReview { get; set; }

    // Related counts
    public int RepositoryCount { get; set; }
    public int TicketCount { get; set; }

    // Credentials - masked for display
    public bool HasTicketPlatformApiToken { get; set; }
    public bool HasClaudeApiKey { get; set; }

    // Backward compatibility - obsolete aliases
    [Obsolete("Use TicketPlatformUrl instead. This property will be removed in a future version.")]
    public string JiraUrl
    {
        get => TicketPlatformUrl;
        set => TicketPlatformUrl = value;
    }

    [Obsolete("Use HasTicketPlatformApiToken instead. This property will be removed in a future version.")]
    public bool HasJiraApiToken
    {
        get => HasTicketPlatformApiToken;
        set => HasTicketPlatformApiToken = value;
    }
}
