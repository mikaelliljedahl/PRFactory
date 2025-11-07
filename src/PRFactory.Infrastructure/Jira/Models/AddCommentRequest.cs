using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Request to add a comment to a Jira issue.
/// </summary>
public class AddCommentRequest
{
    /// <summary>
    /// Gets or sets the comment body in Atlassian Document Format (ADF).
    /// </summary>
    [JsonPropertyName("body")]
    public JiraContent Body { get; set; } = new();

    /// <summary>
    /// Gets or sets the visibility restrictions for the comment.
    /// If null, the comment is visible to everyone with access to the issue.
    /// </summary>
    [JsonPropertyName("visibility")]
    public CommentVisibility? Visibility { get; set; }

    /// <summary>
    /// Creates a comment request with plain text content.
    /// </summary>
    /// <param name="text">The comment text.</param>
    /// <returns>A comment request with ADF-formatted content.</returns>
    public static AddCommentRequest FromPlainText(string text) => new()
    {
        Body = JiraContent.FromPlainText(text)
    };
}

/// <summary>
/// Represents visibility restrictions for a comment.
/// </summary>
public class CommentVisibility
{
    /// <summary>
    /// Gets or sets the visibility type ("group" or "role").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value (group name or role name).
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Creates visibility restriction for a specific group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    public static CommentVisibility ForGroup(string groupName) => new()
    {
        Type = "group",
        Value = groupName
    };

    /// <summary>
    /// Creates visibility restriction for a specific role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    public static CommentVisibility ForRole(string roleName) => new()
    {
        Type = "role",
        Value = roleName
    };
}
