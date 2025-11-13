namespace PRFactory.Domain.Entities;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the result of an AI code review performed on a pull request.
/// Stores review feedback, identified issues, suggestions, and praise.
/// </summary>
public class CodeReviewResult
{
    /// <summary>
    /// Unique identifier for this code review result
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket this code review is associated with
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// The pull request number being reviewed
    /// </summary>
    public int PullRequestNumber { get; private set; }

    /// <summary>
    /// URL of the pull request
    /// </summary>
    public string PullRequestUrl { get; private set; } = string.Empty;

    /// <summary>
    /// The LLM provider used for this review (e.g., "OpenAI", "Anthropic", "Google")
    /// </summary>
    public string LlmProviderName { get; private set; } = string.Empty;

    /// <summary>
    /// The specific model used (e.g., "gpt-4o", "claude-sonnet-4-5")
    /// </summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>
    /// Critical issues that must be fixed before approval
    /// Stored as JSON array of issue descriptions
    /// </summary>
    public List<string> CriticalIssues { get; private set; } = new();

    /// <summary>
    /// Suggested improvements (non-blocking)
    /// Stored as JSON array of suggestion descriptions
    /// </summary>
    public List<string> Suggestions { get; private set; } = new();

    /// <summary>
    /// Positive feedback and praise for well-written code
    /// Stored as JSON array of praise descriptions
    /// </summary>
    public List<string> Praise { get; private set; } = new();

    /// <summary>
    /// Full review content from the LLM
    /// </summary>
    public string FullReviewContent { get; private set; } = string.Empty;

    /// <summary>
    /// When the review was performed
    /// </summary>
    public DateTime ReviewedAt { get; private set; }

    /// <summary>
    /// When the review results were created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Retry attempt number (0 for first review, 1+ for re-reviews after fixes)
    /// </summary>
    public int RetryAttempt { get; private set; }

    /// <summary>
    /// Whether this review passed (no critical issues)
    /// </summary>
    public bool Passed => CriticalIssues.Count == 0;

    /// <summary>
    /// Total number of issues (critical + suggestions)
    /// </summary>
    public int TotalIssueCount => CriticalIssues.Count + Suggestions.Count;

    // Navigation properties
    public Ticket Ticket { get; private set; } = null!;

    // EF Core constructor
    private CodeReviewResult() { }

    /// <summary>
    /// Creates a new code review result
    /// </summary>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Entity constructor with many properties")]
    public CodeReviewResult(
        Guid ticketId,
        int pullRequestNumber,
        string pullRequestUrl,
        string llmProviderName,
        string modelName,
        List<string> criticalIssues,
        List<string> suggestions,
        List<string> praise,
        string fullReviewContent,
        int retryAttempt = 0)
    {
        if (string.IsNullOrWhiteSpace(pullRequestUrl))
            throw new ArgumentException("Pull request URL cannot be empty", nameof(pullRequestUrl));

        if (string.IsNullOrWhiteSpace(llmProviderName))
            throw new ArgumentException("LLM provider name cannot be empty", nameof(llmProviderName));

        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));

        Id = Guid.NewGuid();
        TicketId = ticketId;
        PullRequestNumber = pullRequestNumber;
        PullRequestUrl = pullRequestUrl;
        LlmProviderName = llmProviderName;
        ModelName = modelName;
        CriticalIssues = criticalIssues ?? [];
        Suggestions = suggestions ?? [];
        Praise = praise ?? [];
        FullReviewContent = fullReviewContent ?? string.Empty;
        RetryAttempt = retryAttempt;
        ReviewedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a critical issue to the review
    /// </summary>
    public void AddCriticalIssue(string issue)
    {
        if (string.IsNullOrWhiteSpace(issue))
            throw new ArgumentException("Issue cannot be empty", nameof(issue));

        if (!CriticalIssues.Contains(issue))
        {
            CriticalIssues.Add(issue);
        }
    }

    /// <summary>
    /// Adds a suggestion to the review
    /// </summary>
    public void AddSuggestion(string suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion))
            throw new ArgumentException("Suggestion cannot be empty", nameof(suggestion));

        if (!Suggestions.Contains(suggestion))
        {
            Suggestions.Add(suggestion);
        }
    }

    /// <summary>
    /// Adds praise to the review
    /// </summary>
    public void AddPraise(string praiseItem)
    {
        if (string.IsNullOrWhiteSpace(praiseItem))
            throw new ArgumentException("Praise cannot be empty", nameof(praiseItem));

        if (!Praise.Contains(praiseItem))
        {
            Praise.Add(praiseItem);
        }
    }

    /// <summary>
    /// Gets a summary of the review
    /// </summary>
    public string GetSummary()
    {
        if (Passed)
        {
            return $"✅ Review passed with {Suggestions.Count} suggestions and {Praise.Count} items of praise.";
        }
        else
        {
            return $"❌ Review identified {CriticalIssues.Count} critical issues and {Suggestions.Count} suggestions.";
        }
    }
}
