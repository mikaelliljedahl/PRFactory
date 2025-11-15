namespace PRFactory.Web.Models;

/// <summary>
/// Response DTO after pull request creation.
/// </summary>
public class CreatePRResponse
{
    /// <summary>
    /// URL to the created pull request
    /// </summary>
    public required string PullRequestUrl { get; init; }

    /// <summary>
    /// Pull request number (e.g., #123)
    /// </summary>
    public required int PullRequestNumber { get; init; }

    /// <summary>
    /// Indicates if PR was created successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
