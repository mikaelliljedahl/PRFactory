namespace PRFactory.Domain.Results;

/// <summary>
/// Result object for pull request creation operations.
/// </summary>
public class PullRequestCreationResult
{
    /// <summary>
    /// Indicates if the pull request was created successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// URL to the created pull request
    /// </summary>
    public string? PullRequestUrl { get; init; }

    /// <summary>
    /// Pull request number (e.g., 123)
    /// </summary>
    public int? PullRequestNumber { get; init; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <param name="url">The pull request URL</param>
    /// <param name="number">The pull request number</param>
    /// <returns>A successful result</returns>
    public static PullRequestCreationResult Successful(string url, int number) => new()
    {
        Success = true,
        PullRequestUrl = url,
        PullRequestNumber = number
    };

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static PullRequestCreationResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
