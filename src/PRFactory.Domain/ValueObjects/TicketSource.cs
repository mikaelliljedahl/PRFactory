namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Indicates where a ticket originated
/// </summary>
public enum TicketSource
{
    /// <summary>
    /// Ticket created directly in PRFactory Web UI
    /// </summary>
    WebUI,

    /// <summary>
    /// Ticket synced from Jira
    /// </summary>
    Jira,

    /// <summary>
    /// Ticket synced from Azure DevOps
    /// </summary>
    AzureDevOps,

    /// <summary>
    /// Ticket synced from GitHub Issues
    /// </summary>
    GitHubIssues
}
