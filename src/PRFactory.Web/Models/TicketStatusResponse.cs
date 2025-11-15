using System.Text.Json.Serialization;

namespace PRFactory.Web.Models;

/// <summary>
/// Response for ticket status queries
/// </summary>
public class TicketStatusResponse
{
    /// <summary>
    /// Ticket ID (Jira issue key)
    /// </summary>
    [JsonPropertyName("ticketId")]
    public string TicketId { get; set; } = string.Empty;

    /// <summary>
    /// Current workflow state
    /// </summary>
    [JsonPropertyName("currentState")]
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    [JsonPropertyName("repositoryName")]
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Feature branch name
    /// </summary>
    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    /// <summary>
    /// Pull request URL
    /// </summary>
    [JsonPropertyName("pullRequestUrl")]
    public string? PullRequestUrl { get; set; }

    /// <summary>
    /// Implementation plan (if available)
    /// </summary>
    [JsonPropertyName("implementationPlan")]
    public string? ImplementationPlan { get; set; }

    /// <summary>
    /// Questions asked to the user (if any)
    /// </summary>
    [JsonPropertyName("questions")]
    public List<string>? Questions { get; set; }

    /// <summary>
    /// Answers provided by the user (if any)
    /// </summary>
    [JsonPropertyName("answers")]
    public Dictionary<string, string>? Answers { get; set; }

    /// <summary>
    /// Workflow events history
    /// </summary>
    [JsonPropertyName("events")]
    public List<WorkflowEventDto> Events { get; set; } = new();

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the workflow is waiting for human input
    /// </summary>
    [JsonPropertyName("awaitingHumanInput")]
    public bool AwaitingHumanInput { get; set; }
}
