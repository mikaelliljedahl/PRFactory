using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Represents a comment on a Jira issue.
/// </summary>
public class JiraComment
{
    /// <summary>
    /// Gets or sets the unique identifier of the comment.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to access the comment.
    /// </summary>
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    /// <summary>
    /// Gets or sets the comment body in Atlassian Document Format (ADF).
    /// </summary>
    [JsonPropertyName("body")]
    public JiraContent? Body { get; set; }

    /// <summary>
    /// Gets or sets the author of the comment.
    /// </summary>
    [JsonPropertyName("author")]
    public JiraUser? Author { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp of the comment.
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp of the comment.
    /// </summary>
    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }
}
