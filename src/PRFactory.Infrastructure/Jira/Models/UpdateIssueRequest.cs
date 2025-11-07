using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Request to update fields on a Jira issue.
/// </summary>
public class UpdateIssueRequest
{
    /// <summary>
    /// Gets or sets the fields to update.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, object> Fields { get; set; } = new();

    /// <summary>
    /// Creates a request to update a single field.
    /// </summary>
    /// <param name="fieldKey">The field key (e.g., "summary", "customfield_10001").</param>
    /// <param name="value">The new value for the field.</param>
    /// <returns>An update request with the specified field change.</returns>
    public static UpdateIssueRequest UpdateField(string fieldKey, object value)
    {
        return new UpdateIssueRequest
        {
            Fields = new Dictionary<string, object>
            {
                [fieldKey] = value
            }
        };
    }

    /// <summary>
    /// Creates a request to update the summary (title) of an issue.
    /// </summary>
    /// <param name="summary">The new summary text.</param>
    /// <returns>An update request to change the summary.</returns>
    public static UpdateIssueRequest UpdateSummary(string summary) =>
        UpdateField("summary", summary);

    /// <summary>
    /// Creates a request to update the description of an issue.
    /// </summary>
    /// <param name="description">The new description in ADF format.</param>
    /// <returns>An update request to change the description.</returns>
    public static UpdateIssueRequest UpdateDescription(JiraContent description) =>
        UpdateField("description", description);

    /// <summary>
    /// Creates a request to update labels on an issue.
    /// </summary>
    /// <param name="labels">The new set of labels.</param>
    /// <returns>An update request to change the labels.</returns>
    public static UpdateIssueRequest UpdateLabels(params string[] labels) =>
        UpdateField("labels", labels);

    /// <summary>
    /// Creates a request to update a custom field.
    /// </summary>
    /// <param name="customFieldId">The custom field ID (e.g., "customfield_10001").</param>
    /// <param name="value">The new value for the custom field.</param>
    /// <returns>An update request to change the custom field.</returns>
    public static UpdateIssueRequest UpdateCustomField(string customFieldId, object value) =>
        UpdateField(customFieldId, value);
}
