using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Represents content in Atlassian Document Format (ADF).
/// ADF is a JSON-based format used by Jira Cloud for rich text content.
/// </summary>
/// <remarks>
/// ADF structure:
/// - Root is always a "doc" node with version 1
/// - Contains an array of content nodes (paragraphs, headings, lists, etc.)
/// - Text content is nested within text nodes
///
/// Example:
/// {
///   "type": "doc",
///   "version": 1,
///   "content": [
///     {
///       "type": "paragraph",
///       "content": [
///         { "type": "text", "text": "Hello world" }
///       ]
///     }
///   ]
/// }
/// </remarks>
public class JiraContent
{
    /// <summary>
    /// Gets or sets the node type. For root content, this is "doc".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "doc";

    /// <summary>
    /// Gets or sets the ADF version. Currently always 1.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the child content nodes.
    /// </summary>
    [JsonPropertyName("content")]
    public List<JiraContentNode> Content { get; set; } = new();

    /// <summary>
    /// Creates a simple ADF document with plain text paragraphs.
    /// </summary>
    /// <param name="text">The text to convert to ADF format.</param>
    /// <returns>A JiraContent instance with the text formatted as paragraphs.</returns>
    public static JiraContent FromPlainText(string text)
    {
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(para => JiraContentNode.Paragraph(para.Trim()))
            .ToList();

        return new JiraContent
        {
            Content = paragraphs.Any() ? paragraphs : new List<JiraContentNode>
            {
                JiraContentNode.Paragraph(text)
            }
        };
    }
}

/// <summary>
/// Represents a node in the Atlassian Document Format tree.
/// </summary>
public class JiraContentNode
{
    /// <summary>
    /// Gets or sets the type of the node (e.g., "paragraph", "text", "heading", "bulletList").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the child content nodes. Present for container nodes like paragraphs.
    /// </summary>
    [JsonPropertyName("content")]
    public List<JiraContentNode>? Content { get; set; }

    /// <summary>
    /// Gets or sets the text content. Present for leaf text nodes.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the attributes for the node (e.g., heading level).
    /// </summary>
    [JsonPropertyName("attrs")]
    public Dictionary<string, object>? Attrs { get; set; }

    /// <summary>
    /// Gets or sets text marks (formatting like bold, italic, code).
    /// </summary>
    [JsonPropertyName("marks")]
    public List<JiraContentMark>? Marks { get; set; }

    /// <summary>
    /// Creates a paragraph node containing text.
    /// </summary>
    /// <param name="text">The text content of the paragraph.</param>
    /// <returns>A paragraph node with the specified text.</returns>
    public static JiraContentNode Paragraph(string text) => new()
    {
        Type = "paragraph",
        Content = new List<JiraContentNode>
        {
            new() { Type = "text", Text = text }
        }
    };

    /// <summary>
    /// Creates a heading node.
    /// </summary>
    /// <param name="text">The heading text.</param>
    /// <param name="level">The heading level (1-6).</param>
    /// <returns>A heading node with the specified text and level.</returns>
    public static JiraContentNode Heading(string text, int level = 1) => new()
    {
        Type = "heading",
        Attrs = new Dictionary<string, object> { ["level"] = level },
        Content = new List<JiraContentNode>
        {
            new() { Type = "text", Text = text }
        }
    };

    /// <summary>
    /// Creates a code block node.
    /// </summary>
    /// <param name="code">The code content.</param>
    /// <param name="language">The programming language for syntax highlighting.</param>
    /// <returns>A code block node.</returns>
    public static JiraContentNode CodeBlock(string code, string? language = null) => new()
    {
        Type = "codeBlock",
        Attrs = language != null ? new Dictionary<string, object> { ["language"] = language } : null,
        Content = new List<JiraContentNode>
        {
            new() { Type = "text", Text = code }
        }
    };

    /// <summary>
    /// Creates a bullet list item.
    /// </summary>
    /// <param name="text">The list item text.</param>
    /// <returns>A list item node.</returns>
    public static JiraContentNode ListItem(string text) => new()
    {
        Type = "listItem",
        Content = new List<JiraContentNode>
        {
            Paragraph(text)
        }
    };

    /// <summary>
    /// Creates a bullet list with multiple items.
    /// </summary>
    /// <param name="items">The list item texts.</param>
    /// <returns>A bullet list node.</returns>
    public static JiraContentNode BulletList(params string[] items) => new()
    {
        Type = "bulletList",
        Content = items.Select(ListItem).ToList()
    };
}

/// <summary>
/// Represents text formatting marks in ADF (bold, italic, code, etc.).
/// </summary>
public class JiraContentMark
{
    /// <summary>
    /// Gets or sets the mark type (e.g., "strong", "em", "code", "link").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets attributes for the mark (e.g., href for links).
    /// </summary>
    [JsonPropertyName("attrs")]
    public Dictionary<string, object>? Attrs { get; set; }

    /// <summary>
    /// Creates a bold mark.
    /// </summary>
    public static JiraContentMark Bold() => new() { Type = "strong" };

    /// <summary>
    /// Creates an italic mark.
    /// </summary>
    public static JiraContentMark Italic() => new() { Type = "em" };

    /// <summary>
    /// Creates a code mark.
    /// </summary>
    public static JiraContentMark Code() => new() { Type = "code" };

    /// <summary>
    /// Creates a link mark.
    /// </summary>
    /// <param name="href">The URL to link to.</param>
    public static JiraContentMark Link(string href) => new()
    {
        Type = "link",
        Attrs = new Dictionary<string, object> { ["href"] = href }
    };
}
