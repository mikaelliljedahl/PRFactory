using System.Text.Json.Serialization;

namespace PRFactory.Api.Models;

/// <summary>
/// Represents a Jira webhook event payload
/// </summary>
public class JiraWebhookPayload
{
    /// <summary>
    /// Webhook event type (e.g., "jira:issue_created", "jira:issue_updated", "comment_created")
    /// </summary>
    [JsonPropertyName("webhookEvent")]
    public string WebhookEvent { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the event
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// The Jira issue involved in the event
    /// </summary>
    [JsonPropertyName("issue")]
    public JiraIssue? Issue { get; set; }

    /// <summary>
    /// The comment (if this is a comment event)
    /// </summary>
    [JsonPropertyName("comment")]
    public JiraComment? Comment { get; set; }

    /// <summary>
    /// User who triggered the event
    /// </summary>
    [JsonPropertyName("user")]
    public JiraUser? User { get; set; }

    /// <summary>
    /// Change log for the event
    /// </summary>
    [JsonPropertyName("changelog")]
    public JiraChangeLog? ChangeLog { get; set; }
}

/// <summary>
/// Represents a Jira issue
/// </summary>
public class JiraIssue
{
    /// <summary>
    /// Issue ID (numeric)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Issue key (e.g., "PROJ-123")
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Issue self URL
    /// </summary>
    [JsonPropertyName("self")]
    public string Self { get; set; } = string.Empty;

    /// <summary>
    /// Issue fields
    /// </summary>
    [JsonPropertyName("fields")]
    public JiraIssueFields Fields { get; set; } = new();
}

/// <summary>
/// Represents Jira issue fields
/// </summary>
public class JiraIssueFields
{
    /// <summary>
    /// Issue summary/title
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Issue description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Issue status
    /// </summary>
    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    /// <summary>
    /// Issue type
    /// </summary>
    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }

    /// <summary>
    /// Project
    /// </summary>
    [JsonPropertyName("project")]
    public JiraProject? Project { get; set; }

    /// <summary>
    /// Assignee
    /// </summary>
    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    /// <summary>
    /// Custom fields (key-value pairs)
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Represents a Jira status
/// </summary>
public class JiraStatus
{
    /// <summary>
    /// Status name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Status ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Jira issue type
/// </summary>
public class JiraIssueType
{
    /// <summary>
    /// Issue type name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Issue type ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Jira project
/// </summary>
public class JiraProject
{
    /// <summary>
    /// Project key
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Jira user
/// </summary>
public class JiraUser
{
    /// <summary>
    /// User account ID
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// User display name
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}

/// <summary>
/// Represents a Jira comment
/// </summary>
public class JiraComment
{
    /// <summary>
    /// Comment ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Comment body
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Comment author
    /// </summary>
    [JsonPropertyName("author")]
    public JiraUser? Author { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    /// <summary>
    /// Update timestamp
    /// </summary>
    [JsonPropertyName("updated")]
    public string Updated { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Jira change log
/// </summary>
public class JiraChangeLog
{
    /// <summary>
    /// List of items that changed
    /// </summary>
    [JsonPropertyName("items")]
    public List<JiraChangeLogItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single change log item
/// </summary>
public class JiraChangeLogItem
{
    /// <summary>
    /// Field that changed
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Field type
    /// </summary>
    [JsonPropertyName("fieldtype")]
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Old value
    /// </summary>
    [JsonPropertyName("fromString")]
    public string? FromString { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    [JsonPropertyName("toString")]
    public new string? ToString { get; set; }
}
