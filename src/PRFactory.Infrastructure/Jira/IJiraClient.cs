using PRFactory.Infrastructure.Jira.Models;
using Refit;

namespace PRFactory.Infrastructure.Jira;

/// <summary>
/// Refit-based HTTP client for interacting with the Jira REST API v3.
/// Provides low-level operations for issue management, comments, transitions, and remote links.
/// </summary>
/// <remarks>
/// This interface is implemented automatically by Refit at runtime.
/// Configure the HttpClient with base URL and authentication credentials during service registration.
/// </remarks>
public interface IJiraClient
{
    /// <summary>
    /// Retrieves a Jira issue by its key.
    /// </summary>
    /// <param name="issueKey">The issue key (e.g., "PROJ-123").</param>
    /// <returns>The Jira issue details including fields and metadata.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails (e.g., 404 if issue not found).</exception>
    [Get("/rest/api/3/issue/{issueKey}")]
    Task<JiraIssue> GetIssueAsync(string issueKey);

    /// <summary>
    /// Adds a comment to a Jira issue.
    /// </summary>
    /// <param name="issueKey">The issue key to add the comment to.</param>
    /// <param name="request">The comment request containing the body in Atlassian Document Format (ADF).</param>
    /// <returns>The created comment with its ID and metadata.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails.</exception>
    [Post("/rest/api/3/issue/{issueKey}/comment")]
    Task<JiraComment> AddCommentAsync(
        string issueKey,
        [Body] AddCommentRequest request);

    /// <summary>
    /// Updates fields on a Jira issue.
    /// </summary>
    /// <param name="issueKey">The issue key to update.</param>
    /// <param name="request">The update request containing field changes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails.</exception>
    [Put("/rest/api/3/issue/{issueKey}")]
    Task UpdateIssueAsync(
        string issueKey,
        [Body] UpdateIssueRequest request);

    /// <summary>
    /// Transitions a Jira issue to a different status.
    /// </summary>
    /// <param name="issueKey">The issue key to transition.</param>
    /// <param name="request">The transition request containing the transition ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails.</exception>
    /// <remarks>
    /// To get available transitions for an issue, use GET /rest/api/3/issue/{issueKey}/transitions.
    /// </remarks>
    [Post("/rest/api/3/issue/{issueKey}/transitions")]
    Task TransitionIssueAsync(
        string issueKey,
        [Body] TransitionRequest request);

    /// <summary>
    /// Adds a remote link (e.g., pull request URL) to a Jira issue.
    /// </summary>
    /// <param name="issueKey">The issue key to add the link to.</param>
    /// <param name="request">The remote link request containing URL, title, and icon.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails.</exception>
    [Post("/rest/api/3/issue/{issueKey}/remotelink")]
    Task AddRemoteLinkAsync(
        string issueKey,
        [Body] RemoteLinkRequest request);
}
