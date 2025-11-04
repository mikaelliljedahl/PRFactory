using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Represents the payload received from a Jira webhook.
/// </summary>
/// <remarks>
/// Jira sends webhooks for various events like issue_created, issue_updated, comment_created, etc.
/// The structure varies slightly depending on the event type, but common fields are captured here.
/// </remarks>
public class JiraWebhookPayload
{
    /// <summary>
    /// Gets or sets the webhook event type (e.g., "jira:issue_created", "comment_created").
    /// </summary>
    [JsonPropertyName("webhookEvent")]
    public string WebhookEvent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user who triggered the event.
    /// </summary>
    [JsonPropertyName("user")]
    public JiraUser? User { get; set; }

    /// <summary>
    /// Gets or sets the issue associated with the event.
    /// </summary>
    [JsonPropertyName("issue")]
    public JiraIssue? Issue { get; set; }

    /// <summary>
    /// Gets or sets the changelog (for issue_updated events).
    /// </summary>
    [JsonPropertyName("changelog")]
    public JiraChangelog? Changelog { get; set; }

    /// <summary>
    /// Gets or sets the comment (for comment_created and comment_updated events).
    /// </summary>
    [JsonPropertyName("comment")]
    public JiraWebhookComment? Comment { get; set; }

    /// <summary>
    /// Gets or sets additional issue event type name.
    /// </summary>
    [JsonPropertyName("issue_event_type_name")]
    public string? IssueEventTypeName { get; set; }
}

/// <summary>
/// Represents a comment in a webhook payload.
/// </summary>
/// <remarks>
/// This is slightly different from JiraComment as webhook payloads may include
/// plain text body alongside ADF.
/// </remarks>
public class JiraWebhookComment
{
    /// <summary>
    /// Gets or sets the comment ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comment body in ADF format.
    /// </summary>
    [JsonPropertyName("body")]
    public JiraContent? Body { get; set; }

    /// <summary>
    /// Gets or sets the plain text representation of the comment.
    /// This is useful for parsing without having to traverse ADF structure.
    /// </summary>
    [JsonPropertyName("renderedBody")]
    public string? RenderedBody { get; set; }

    /// <summary>
    /// Gets or sets the author of the comment.
    /// </summary>
    [JsonPropertyName("author")]
    public JiraUser? Author { get; set; }

    /// <summary>
    /// Gets or sets when the comment was created.
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    /// <summary>
    /// Gets or sets when the comment was last updated.
    /// </summary>
    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }

    /// <summary>
    /// Gets the plain text body by extracting text from ADF or using renderedBody.
    /// </summary>
    /// <returns>The comment text as plain string.</returns>
    public string GetPlainTextBody()
    {
        if (!string.IsNullOrEmpty(RenderedBody))
            return RenderedBody;

        if (Body?.Content == null)
            return string.Empty;

        return string.Join("\n\n", Body.Content.Select(ExtractTextFromNode));
    }

    private static string ExtractTextFromNode(JiraContentNode node)
    {
        if (node.Text != null)
            return node.Text;

        if (node.Content != null)
            return string.Join(" ", node.Content.Select(ExtractTextFromNode));

        return string.Empty;
    }
}

/// <summary>
/// Represents the changelog in an issue update event.
/// </summary>
public class JiraChangelog
{
    /// <summary>
    /// Gets or sets the changelog ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the list of field changes.
    /// </summary>
    [JsonPropertyName("items")]
    public List<JiraChangelogItem>? Items { get; set; }
}

/// <summary>
/// Represents a single field change in the changelog.
/// </summary>
public class JiraChangelogItem
{
    /// <summary>
    /// Gets or sets the field name that changed (e.g., "status", "assignee").
    /// </summary>
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the field type.
    /// </summary>
    [JsonPropertyName("fieldtype")]
    public string? FieldType { get; set; }

    /// <summary>
    /// Gets or sets the old value (as string).
    /// </summary>
    [JsonPropertyName("fromString")]
    public string? FromString { get; set; }

    /// <summary>
    /// Gets or sets the new value (as string).
    /// </summary>
    [JsonPropertyName("toString")]
    public string? ToString { get; set; }

    /// <summary>
    /// Gets or sets the old value ID.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the new value ID.
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }
}
