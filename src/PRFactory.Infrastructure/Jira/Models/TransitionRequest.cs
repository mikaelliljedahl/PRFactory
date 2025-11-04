using System.Text.Json.Serialization;

namespace PRFactory.Infrastructure.Jira.Models;

/// <summary>
/// Request to transition a Jira issue to a different status.
/// </summary>
public class TransitionRequest
{
    /// <summary>
    /// Gets or sets the transition to execute.
    /// </summary>
    [JsonPropertyName("transition")]
    public TransitionInfo Transition { get; set; } = new();

    /// <summary>
    /// Gets or sets fields to update during the transition (optional).
    /// Some transitions require field values to be set.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, object>? Fields { get; set; }

    /// <summary>
    /// Creates a transition request with a transition ID.
    /// </summary>
    /// <param name="transitionId">The ID of the transition to execute.</param>
    /// <returns>A transition request.</returns>
    /// <remarks>
    /// To get available transitions for an issue, use GET /rest/api/3/issue/{issueKey}/transitions.
    /// </remarks>
    public static TransitionRequest ToTransition(string transitionId) => new()
    {
        Transition = new TransitionInfo { Id = transitionId }
    };

    /// <summary>
    /// Creates a transition request with a transition name.
    /// </summary>
    /// <param name="transitionName">The name of the transition to execute.</param>
    /// <returns>A transition request.</returns>
    public static TransitionRequest ToTransitionByName(string transitionName) => new()
    {
        Transition = new TransitionInfo { Name = transitionName }
    };
}

/// <summary>
/// Represents transition information.
/// </summary>
public class TransitionInfo
{
    /// <summary>
    /// Gets or sets the transition ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the transition name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
