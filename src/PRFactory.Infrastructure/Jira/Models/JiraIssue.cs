using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Represents a Jira issue retrieved from the REST API.
/// </summary>
public class JiraIssue
{
    /// <summary>
    /// Gets or sets the unique identifier of the issue.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue key (e.g., "PROJ-123").
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to view the issue in Jira.
    /// </summary>
    [JsonPropertyName("self")]
    public string Self { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue fields (summary, description, status, etc.).
    /// </summary>
    [JsonPropertyName("fields")]
    public JiraIssueFields Fields { get; set; } = new();
}

/// <summary>
/// Represents the fields of a Jira issue.
/// </summary>
public class JiraIssueFields
{
    /// <summary>
    /// Gets or sets the issue summary (title).
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the issue description in Atlassian Document Format (ADF).
    /// </summary>
    [JsonPropertyName("description")]
    public JiraContent? Description { get; set; }

    /// <summary>
    /// Gets or sets the current status of the issue.
    /// </summary>
    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the issue type (e.g., Bug, Story, Task).
    /// </summary>
    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }

    /// <summary>
    /// Gets or sets the labels associated with the issue.
    /// </summary>
    [JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }

    /// <summary>
    /// Gets or sets the reporter (user who created the issue).
    /// </summary>
    [JsonPropertyName("reporter")]
    public JiraUser? Reporter { get; set; }

    /// <summary>
    /// Gets or sets the assignee (user assigned to the issue).
    /// </summary>
    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    /// <summary>
    /// Gets or sets custom fields (dynamic based on Jira configuration).
    /// Use JsonExtensionData to capture custom fields like customfield_10001.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Represents the status of a Jira issue.
/// </summary>
public class JiraStatus
{
    /// <summary>
    /// Gets or sets the status ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the status name (e.g., "To Do", "In Progress", "Done").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the status category (e.g., "To Do", "In Progress", "Done").
    /// </summary>
    [JsonPropertyName("statusCategory")]
    public JiraStatusCategory? StatusCategory { get; set; }
}

/// <summary>
/// Represents the category of a Jira status.
/// </summary>
public class JiraStatusCategory
{
    /// <summary>
    /// Gets or sets the category key (e.g., "new", "indeterminate", "done").
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Represents a Jira issue type.
/// </summary>
public class JiraIssueType
{
    /// <summary>
    /// Gets or sets the issue type ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the issue type name (e.g., "Bug", "Story", "Task").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Represents a Jira user.
/// </summary>
public class JiraUser
{
    /// <summary>
    /// Gets or sets the account ID of the user.
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}
