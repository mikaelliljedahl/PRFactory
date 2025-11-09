namespace PRFactory.Infrastructure.Persistence.DemoData;

/// <summary>
/// Constants and configuration for demo tenant data
/// </summary>
public static class DemoTenantData
{
    /// <summary>
    /// Hardcoded demo tenant ID for offline development
    /// </summary>
    public static readonly Guid DemoTenantId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Demo tenant name
    /// </summary>
    public const string TenantName = "Demo Tenant";

    /// <summary>
    /// Demo Jira URL
    /// </summary>
    public const string JiraUrl = "https://demo.atlassian.net";

    /// <summary>
    /// Demo Jira API token (will be encrypted)
    /// </summary>
    public const string JiraApiToken = "demo-jira-token-12345";

    /// <summary>
    /// Demo Claude API key (will be encrypted)
    /// </summary>
    public const string ClaudeApiKey = "demo-claude-api-key-67890";
}
