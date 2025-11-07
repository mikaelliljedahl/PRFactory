using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using PRFactory.Infrastructure.Jira.Models;
using Refit;

namespace PRFactory.Infrastructure.Jira;

/// <summary>
/// High-level service for interacting with Jira.
/// Provides simplified operations with retry logic, error handling, and markdown conversion.
/// </summary>
public interface IJiraService
{
    /// <summary>
    /// Posts a comment to a Jira issue, converting markdown to ADF format.
    /// </summary>
    /// <param name="issueKey">The issue key (e.g., "PROJ-123").</param>
    /// <param name="markdownText">The comment text in markdown format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created comment.</returns>
    Task<JiraComment> PostCommentAsync(string issueKey, string markdownText, CancellationToken ct = default);

    /// <summary>
    /// Links a pull request to a Jira issue as a remote link.
    /// </summary>
    /// <param name="issueKey">The issue key to link the PR to.</param>
    /// <param name="prUrl">The pull request URL.</param>
    /// <param name="prTitle">The pull request title.</param>
    /// <param name="repositoryName">Optional repository name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LinkPullRequestAsync(string issueKey, string prUrl, string prTitle, string? repositoryName = null, CancellationToken ct = default);

    /// <summary>
    /// Updates a custom field on a Jira issue.
    /// </summary>
    /// <param name="issueKey">The issue key to update.</param>
    /// <param name="fieldKey">The field key (e.g., "customfield_10001").</param>
    /// <param name="value">The new value for the field.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateCustomFieldAsync(string issueKey, string fieldKey, object value, CancellationToken ct = default);

    /// <summary>
    /// Transitions a Jira issue to a different status.
    /// </summary>
    /// <param name="issueKey">The issue key to transition.</param>
    /// <param name="statusName">The target status name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TransitionToStatusAsync(string issueKey, string statusName, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a Jira issue by its key.
    /// </summary>
    /// <param name="issueKey">The issue key (e.g., "PROJ-123").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Jira issue.</returns>
    Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken ct = default);

    /// <summary>
    /// Updates the summary (title) of a Jira issue.
    /// </summary>
    /// <param name="issueKey">The issue key to update.</param>
    /// <param name="summary">The new summary text.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateSummaryAsync(string issueKey, string summary, CancellationToken ct = default);

    /// <summary>
    /// Adds labels to a Jira issue.
    /// </summary>
    /// <param name="issueKey">The issue key to update.</param>
    /// <param name="labels">The labels to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddLabelsAsync(string issueKey, string[] labels, CancellationToken ct = default);
}

/// <summary>
/// Implementation of IJiraService with retry policies and error handling.
/// </summary>
public class JiraService : IJiraService
{
    private readonly IJiraClient _client;
    private readonly ILogger<JiraService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraService"/> class.
    /// </summary>
    /// <param name="client">The Jira REST API client.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="configuration">Configuration for accessing settings.</param>
    public JiraService(
        IJiraClient client,
        ILogger<JiraService> logger,
        IConfiguration configuration)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<JiraComment> PostCommentAsync(string issueKey, string markdownText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        if (string.IsNullOrWhiteSpace(markdownText))
            throw new ArgumentException("Comment text cannot be null or empty.", nameof(markdownText));

        try
        {
            _logger.LogDebug("Posting comment to Jira issue {IssueKey}", issueKey);

            var request = new AddCommentRequest
            {
                Body = ConvertMarkdownToADF(markdownText)
            };

            var comment = await ExecuteWithRetryAsync(
                () => _client.AddCommentAsync(issueKey, request),
                $"post comment to {issueKey}",
                ct);

            _logger.LogInformation("Successfully posted comment to Jira issue {IssueKey}", issueKey);

            return comment;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to post comment to Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}",
                issueKey, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to post comment to issue {issueKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error posting comment to Jira issue {IssueKey}", issueKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task LinkPullRequestAsync(string issueKey, string prUrl, string prTitle, string? repositoryName = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        if (string.IsNullOrWhiteSpace(prUrl))
            throw new ArgumentException("Pull request URL cannot be null or empty.", nameof(prUrl));

        if (string.IsNullOrWhiteSpace(prTitle))
            throw new ArgumentException("Pull request title cannot be null or empty.", nameof(prTitle));

        try
        {
            _logger.LogDebug("Linking pull request {PrUrl} to Jira issue {IssueKey}", prUrl, issueKey);

            var request = RemoteLinkRequest.ForPullRequest(prUrl, prTitle, repositoryName);

            await ExecuteWithRetryAsync(
                () => _client.AddRemoteLinkAsync(issueKey, request),
                $"link PR to {issueKey}",
                ct);

            _logger.LogInformation("Successfully linked PR {PrUrl} to Jira issue {IssueKey}", prUrl, issueKey);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to link PR to Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}",
                issueKey, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to link PR to issue {issueKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error linking PR to Jira issue {IssueKey}", issueKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateCustomFieldAsync(string issueKey, string fieldKey, object value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        if (string.IsNullOrWhiteSpace(fieldKey))
            throw new ArgumentException("Field key cannot be null or empty.", nameof(fieldKey));

        try
        {
            _logger.LogDebug("Updating custom field {FieldKey} on Jira issue {IssueKey}", fieldKey, issueKey);

            var request = UpdateIssueRequest.UpdateCustomField(fieldKey, value);

            await ExecuteWithRetryAsync(
                () => _client.UpdateIssueAsync(issueKey, request),
                $"update field {fieldKey} on {issueKey}",
                ct);

            _logger.LogInformation("Successfully updated custom field {FieldKey} on Jira issue {IssueKey}", fieldKey, issueKey);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update custom field on Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}",
                issueKey, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to update custom field on issue {issueKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating custom field on Jira issue {IssueKey}", issueKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task TransitionToStatusAsync(string issueKey, string statusName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        if (string.IsNullOrWhiteSpace(statusName))
            throw new ArgumentException("Status name cannot be null or empty.", nameof(statusName));

        try
        {
            _logger.LogDebug("Transitioning Jira issue {IssueKey} to status {StatusName}", issueKey, statusName);

            // Note: In a real implementation, you would need to call the transitions endpoint first
            // to get the transition ID for the target status, then use that ID.
            // This is a simplified version assuming you have the transition ID.
            var request = TransitionRequest.ToTransitionByName(statusName);

            await ExecuteWithRetryAsync(
                () => _client.TransitionIssueAsync(issueKey, request),
                $"transition {issueKey} to {statusName}",
                ct);

            _logger.LogInformation("Successfully transitioned Jira issue {IssueKey} to status {StatusName}", issueKey, statusName);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to transition Jira issue {IssueKey} to {StatusName}. Status: {StatusCode}, Response: {Response}",
                issueKey, statusName, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to transition issue {issueKey} to {statusName}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error transitioning Jira issue {IssueKey} to {StatusName}", issueKey, statusName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        try
        {
            _logger.LogDebug("Retrieving Jira issue {IssueKey}", issueKey);

            var issue = await ExecuteWithRetryAsync(
                () => _client.GetIssueAsync(issueKey),
                $"get issue {issueKey}",
                ct);

            _logger.LogInformation("Successfully retrieved Jira issue {IssueKey}", issueKey);

            return issue;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to retrieve Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}",
                issueKey, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to retrieve issue {issueKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving Jira issue {IssueKey}", issueKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateSummaryAsync(string issueKey, string summary, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Summary cannot be null or empty.", nameof(summary));

        try
        {
            _logger.LogDebug("Updating summary on Jira issue {IssueKey}", issueKey);

            var request = UpdateIssueRequest.UpdateSummary(summary);

            await ExecuteWithRetryAsync(
                () => _client.UpdateIssueAsync(issueKey, request),
                $"update summary on {issueKey}",
                ct);

            _logger.LogInformation("Successfully updated summary on Jira issue {IssueKey}", issueKey);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update summary on Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}",
                issueKey, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to update summary on issue {issueKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating summary on Jira issue {IssueKey}", issueKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AddLabelsAsync(string issueKey, string[] labels, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key cannot be null or empty.", nameof(issueKey));

        if (labels == null || labels.Length == 0)
            throw new ArgumentException("Labels cannot be null or empty.", nameof(labels));

        try
        {
            _logger.LogDebug("Adding labels {Labels} to Jira issue {IssueKey}", string.Join(", ", labels), issueKey);

            // First, get existing labels
            var issue = await GetIssueAsync(issueKey, ct);
            var existingLabels = issue.Fields.Labels ?? new List<string>();

            // Combine with new labels (avoiding duplicates)
            var allLabels = existingLabels.Union(labels).ToArray();

            var request = UpdateIssueRequest.UpdateLabels(allLabels);

            await ExecuteWithRetryAsync(
                () => _client.UpdateIssueAsync(issueKey, request),
                $"add labels to {issueKey}",
                ct);

            _logger.LogInformation("Successfully added labels to Jira issue {IssueKey}", issueKey);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to add labels to Jira issue {IssueKey}. Status: {StatusCode}, Response: {Response}",
                issueKey, ex.StatusCode, ex.Content);
            throw new JiraServiceException($"Failed to add labels to issue {issueKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding labels to Jira issue {IssueKey}", issueKey);
            throw;
        }
    }

    /// <summary>
    /// Executes an async operation with exponential backoff retry policy.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">A descriptive name for logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken ct)
    {
        var retryPolicy = Policy
            .Handle<ApiException>(ex => IsTransientError(ex))
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} for operation '{OperationName}' after {Delay}ms",
                        retryCount, operationName, timeSpan.TotalMilliseconds);
                });

        return await retryPolicy.ExecuteAsync(async () => await operation());
    }

    /// <summary>
    /// Executes an async operation with exponential backoff retry policy (void return).
    /// </summary>
    private async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken ct)
    {
        var retryPolicy = Policy
            .Handle<ApiException>(ex => IsTransientError(ex))
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} for operation '{OperationName}' after {Delay}ms",
                        retryCount, operationName, timeSpan.TotalMilliseconds);
                });

        await retryPolicy.ExecuteAsync(async () => await operation());
    }

    /// <summary>
    /// Determines if an API exception represents a transient error that should be retried.
    /// </summary>
    /// <param name="ex">The API exception.</param>
    /// <returns>True if the error is transient and should be retried; otherwise, false.</returns>
    private bool IsTransientError(ApiException ex)
    {
        // Retry on:
        // - 429 (Too Many Requests)
        // - 500-599 (Server errors)
        // - 408 (Request Timeout)
        return ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
               ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
               ((int)ex.StatusCode >= 500 && (int)ex.StatusCode < 600);
    }

    /// <summary>
    /// Converts markdown text to Atlassian Document Format (ADF).
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>ADF-formatted content.</returns>
    /// <remarks>
    /// This is a simplified converter. For production use, consider using a library like
    /// Markdig with a custom ADF renderer for full markdown support.
    /// </remarks>
    private JiraContent ConvertMarkdownToADF(string markdown)
    {
        var content = new List<JiraContentNode>();

        // Split by double newlines to identify paragraphs/blocks
        var blocks = markdown.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var trimmedBlock = block.Trim();

            // Code blocks (```language)
            if (trimmedBlock.StartsWith("```"))
            {
                var lines = trimmedBlock.Split('\n');
                var language = lines[0].Length > 3 ? lines[0].Substring(3).Trim() : null;
                var code = string.Join("\n", lines.Skip(1).SkipLast(1));
                content.Add(JiraContentNode.CodeBlock(code, language));
            }
            // Headings (# or ## or ###)
            else if (trimmedBlock.StartsWith("#"))
            {
                var level = trimmedBlock.TakeWhile(c => c == '#').Count();
                var text = trimmedBlock.TrimStart('#').Trim();
                content.Add(JiraContentNode.Heading(text, Math.Min(level, 6)));
            }
            // Bullet lists (lines starting with - or *)
            else if (trimmedBlock.Split('\n').All(line => line.TrimStart().StartsWith("-") || line.TrimStart().StartsWith("*")))
            {
                var items = trimmedBlock.Split('\n')
                    .Select(line => line.TrimStart().TrimStart('-', '*').Trim())
                    .ToArray();
                content.Add(JiraContentNode.BulletList(items));
            }
            // Regular paragraphs
            else
            {
                content.Add(JiraContentNode.Paragraph(trimmedBlock));
            }
        }

        return new JiraContent
        {
            Content = content.Any() ? content : new List<JiraContentNode>
            {
                JiraContentNode.Paragraph(markdown)
            }
        };
    }
}

/// <summary>
/// Exception thrown when a Jira service operation fails.
/// </summary>
public class JiraServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public JiraServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public JiraServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
