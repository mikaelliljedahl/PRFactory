using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Request to add a remote link (external URL) to a Jira issue.
/// Commonly used to link pull requests, builds, or other external resources.
/// </summary>
public class RemoteLinkRequest
{
    /// <summary>
    /// Gets or sets the global ID for the link (optional).
    /// Used to prevent duplicate links.
    /// </summary>
    [JsonPropertyName("globalId")]
    public string? GlobalId { get; set; }

    /// <summary>
    /// Gets or sets the application information (optional).
    /// </summary>
    [JsonPropertyName("application")]
    public RemoteLinkApplication? Application { get; set; }

    /// <summary>
    /// Gets or sets the relationship type (optional).
    /// Common values: "mentions", "blocked by", "depends on".
    /// </summary>
    [JsonPropertyName("relationship")]
    public string? Relationship { get; set; }

    /// <summary>
    /// Gets or sets the object being linked (URL, title, icon, etc.).
    /// </summary>
    [JsonPropertyName("object")]
    public RemoteLinkObject Object { get; set; } = new();

    /// <summary>
    /// Creates a remote link request for a pull request.
    /// </summary>
    /// <param name="prUrl">The pull request URL.</param>
    /// <param name="prTitle">The pull request title.</param>
    /// <param name="repositoryName">The repository name (optional).</param>
    /// <returns>A remote link request configured for a GitHub pull request.</returns>
    public static RemoteLinkRequest ForPullRequest(string prUrl, string prTitle, string? repositoryName = null)
    {
        var globalId = $"prfactory-pr-{prUrl.GetHashCode():X}";

        return new RemoteLinkRequest
        {
            GlobalId = globalId,
            Application = new RemoteLinkApplication
            {
                Type = "com.github.pullrequest",
                Name = "GitHub"
            },
            Relationship = "implements",
            Object = new RemoteLinkObject
            {
                Url = prUrl,
                Title = prTitle,
                Summary = repositoryName != null ? $"Pull Request in {repositoryName}" : "Pull Request",
                Icon = new RemoteLinkIcon
                {
                    Url16x16 = "https://github.com/favicon.ico",
                    Title = "GitHub"
                },
                Status = new RemoteLinkStatus
                {
                    Resolved = false,
                    Icon = new RemoteLinkIcon
                    {
                        Url16x16 = "https://github.com/favicon.ico",
                        Title = "Open"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Creates a remote link request for a generic URL.
    /// </summary>
    /// <param name="url">The URL to link.</param>
    /// <param name="title">The link title.</param>
    /// <param name="summary">Optional summary description.</param>
    /// <returns>A remote link request.</returns>
    public static RemoteLinkRequest ForUrl(string url, string title, string? summary = null)
    {
        return new RemoteLinkRequest
        {
            Object = new RemoteLinkObject
            {
                Url = url,
                Title = title,
                Summary = summary
            }
        };
    }
}

/// <summary>
/// Represents application information for a remote link.
/// </summary>
public class RemoteLinkApplication
{
    /// <summary>
    /// Gets or sets the application type identifier.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Represents the object being linked via remote link.
/// </summary>
public class RemoteLinkObject
{
    /// <summary>
    /// Gets or sets the URL of the linked resource.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the linked resource.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary/description of the linked resource.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the icon for the linked resource.
    /// </summary>
    [JsonPropertyName("icon")]
    public RemoteLinkIcon? Icon { get; set; }

    /// <summary>
    /// Gets or sets the status of the linked resource.
    /// </summary>
    [JsonPropertyName("status")]
    public RemoteLinkStatus? Status { get; set; }
}

/// <summary>
/// Represents an icon for a remote link.
/// </summary>
public class RemoteLinkIcon
{
    /// <summary>
    /// Gets or sets the URL to a 16x16 icon.
    /// </summary>
    [JsonPropertyName("url16x16")]
    public string? Url16x16 { get; set; }

    /// <summary>
    /// Gets or sets the icon title/tooltip.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Represents the status of a remote linked resource.
/// </summary>
public class RemoteLinkStatus
{
    /// <summary>
    /// Gets or sets whether the linked item is resolved/completed.
    /// </summary>
    [JsonPropertyName("resolved")]
    public bool Resolved { get; set; }

    /// <summary>
    /// Gets or sets the icon representing the status.
    /// </summary>
    [JsonPropertyName("icon")]
    public RemoteLinkIcon? Icon { get; set; }
}
