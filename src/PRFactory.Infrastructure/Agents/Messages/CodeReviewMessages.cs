using System;
using System.Collections.Generic;

namespace PRFactory.Infrastructure.Agents.Messages;

/// <summary>
/// Message to trigger code review for a pull request
/// </summary>
public record ReviewCodeMessage(
    Guid TicketId,
    string PullRequestUrl,
    int PullRequestNumber,
    string BranchName,
    string PlanPath
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Message indicating code review is complete
/// </summary>
public record CodeReviewCompleteMessage(
    Guid TicketId,
    Guid CodeReviewResultId,
    bool HasCriticalIssues,
    List<string> CriticalIssues,
    List<string> Suggestions,
    DateTime CompletedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a code issue found during review
/// </summary>
public record CodeIssue(
    string Severity,
    string FilePath,
    int? LineNumber,
    string Description,
    string? SuggestedFix = null
);

/// <summary>
/// Message to fix code issues found during review
/// </summary>
public record FixCodeIssuesMessage(
    Guid TicketId,
    List<string> Issues,
    string ReviewFeedback
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Message to post review comments to pull request
/// </summary>
public record PostCommentsMessage(
    Guid TicketId,
    string PullRequestUrl,
    List<CodeIssue> Issues,
    string ReviewSummary
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Message indicating review approval
/// </summary>
public record ApprovalMessage(
    Guid TicketId,
    string PullRequestUrl,
    string ApprovalComment,
    DateTime ApprovedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
